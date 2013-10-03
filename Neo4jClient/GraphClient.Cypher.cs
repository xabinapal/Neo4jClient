using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Transactions;
using Neo4jClient.ApiModels;
using Neo4jClient.ApiModels.Cypher;
using Neo4jClient.Cypher;
using Neo4jClient.Serialization;

namespace Neo4jClient
{
    public partial class GraphClient : ITransactionCoordinator
    {
        readonly ConcurrentDictionary<string, CypherTransaction> activeCypherTransactions =
            new ConcurrentDictionary<string, CypherTransaction>();

        [Obsolete("This method is for use by the framework internally. Use IGraphClient.Cypher instead, and read the documentation at https://bitbucket.org/Readify/neo4jclient/wiki/cypher. If you really really want to call this method directly, and you accept the fact that YOU WILL LIKELY INTRODUCE A RUNTIME SECURITY RISK if you do so, then it shouldn't take you too long to find the correct explicit interface implementation that you have to call. This hurdle is for your own protection. You really really should not do it. This signature may be removed or renamed at any time.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual IEnumerable<TResult> ExecuteGetCypherResults<TResult>(CypherQuery query)
        {
            throw new NotImplementedException();
        }

        IEnumerable<TResult> IRawGraphClient.ExecuteGetCypherResults<TResult>(CypherQuery query)
        {
            var task = ((IRawGraphClient) this).ExecuteGetCypherResultsAsync<TResult>(query);
            Task.WaitAll(task);
            return task.Result;
        }

        Task<IEnumerable<TResult>> IRawGraphClient.ExecuteGetCypherResultsAsync<TResult>(CypherQuery query)
        {
            CheckRoot();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            return
                SendHttpRequestAsync(
                    HttpPostAsJson(RootApiResponse.Cypher, new CypherApiQuery(query)),
                    string.Format("The query was: {0}", query.QueryText),
                    HttpStatusCode.OK)
                .ContinueWith(responseTask =>
                {
                    var response = responseTask.Result;
                    var deserializer = new CypherJsonDeserializer<TResult>(this, query.ResultMode);
                    var results = deserializer
                        .Deserialize(response.Content.ReadAsString())
                        .ToList();

                    stopwatch.Stop();
                    OnOperationCompleted(new OperationCompletedEventArgs
                    {
                        QueryText = query.QueryText,
                        ResourcesReturned = results.Count(),
                        TimeTaken = stopwatch.Elapsed
                    });

                    return (IEnumerable<TResult>)results;
                });
        }

        void IRawGraphClient.ExecuteCypher(CypherQuery query)
        {
            CheckRoot();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var currentTransaction = Transaction.Current;
            if (currentTransaction == null)
            {
                SendHttpRequest(
                    HttpPostAsJson(RootApiResponse.Cypher, new CypherApiQuery(query)),
                    string.Format("The query was: {0}", query.QueryText),
                    HttpStatusCode.OK);
            }
            else
            {
                var localIdentifier = currentTransaction.TransactionInformation.LocalIdentifier;
                CypherTransaction cypherTransaction;
                var cypherTransactionAlreadyEstablished = activeCypherTransactions.TryGetValue(localIdentifier, out cypherTransaction);

                if (!cypherTransactionAlreadyEstablished)
                {
                    if (RootApiResponse.Transaction == null)
                        throw new NotSupportedException("You're attempting to execute Cypher within a .NET transaction, however the Neo4j server you are talking to does not support this capability. This is available from Neo4j 2.0 onwards.");

                    var transactionResponseMessage = SendHttpRequest(
                        HttpPostAsJson(RootApiResponse.Transaction, new CypherTransactionApiQuery(query)),
                        string.Format("Established new transaction for query: {0}", query.QueryText),
                        HttpStatusCode.Created);

                    var responseBody = transactionResponseMessage.Content.ReadAsJson<CypherTransactionApiResponse>(JsonConverters);
                    if (responseBody.Errors.Any()) throw responseBody.Errors.ToException();

                    RegisterTransaction(localIdentifier, transactionResponseMessage, responseBody, currentTransaction);
                }
                else
                {
                    var responseBody = SendHttpRequestAndParseResultAs<CypherTransactionApiResponse>(
                        HttpPostAsJson(cypherTransaction.Endpoint.AbsoluteUri, new CypherTransactionApiQuery(query)),
                        string.Format("In existing transaction {0}, ran query: {1}", cypherTransaction.Endpoint, query.QueryText),
                        HttpStatusCode.OK);

                    if (responseBody.Errors.Any()) throw responseBody.Errors.ToException();
                }
            }

            stopwatch.Stop();
            OnOperationCompleted(new OperationCompletedEventArgs
            {
                QueryText = query.QueryText,
                ResourcesReturned = 0,
                TimeTaken = stopwatch.Elapsed
            });
        }

        IDictionary<string, CypherTransaction> ITransactionCoordinator.ActiveCypherTransactions
        {
            get { return activeCypherTransactions; }
        }

        void RegisterTransaction(
            string localIdentifier,
            HttpResponseMessage transactionResponseMessage,
            CypherTransactionApiResponse transactionResponseBody,
            Transaction currentTransaction)
        {
            var cypherTransaction = new CypherTransaction(this, localIdentifier)
            {
                Endpoint = transactionResponseMessage.Headers.Location,
                CommitEndpoint = transactionResponseBody.Commit
            };
            activeCypherTransactions.TryAdd(localIdentifier, cypherTransaction);
            currentTransaction.EnlistVolatile(cypherTransaction, EnlistmentOptions.None);
        }

        void ReleaseTransaction(CypherTransaction transaction)
        {
            activeCypherTransactions.TryRemove(transaction.LocalIdentifier, out transaction);
        }

        void ITransactionCoordinator.RollbackTransaction(CypherTransaction transaction)
        {
            SendHttpRequest(
                HttpDelete(transaction.Endpoint.AbsoluteUri),
                string.Format("Rolled back transaction {0}", transaction.Endpoint),
                HttpStatusCode.OK);
            ReleaseTransaction(transaction);
        }

        void ITransactionCoordinator.CommitTransaction(CypherTransaction transaction)
        {
            SendHttpRequest(
                HttpPostAsJson(transaction.CommitEndpoint.AbsoluteUri, new CypherTransactionApiQuery()),
                string.Format("Committed transaction {0}", transaction.Endpoint),
                HttpStatusCode.OK);
            ReleaseTransaction(transaction);
        }
    }
}
