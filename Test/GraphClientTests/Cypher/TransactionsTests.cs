using System;
using System.Collections.Generic;
using System.Net;
using System.Transactions;
using Neo4jClient.ApiModels.Cypher;
using Neo4jClient.Cypher;
using NUnit.Framework;

namespace Neo4jClient.Test.GraphClientTests.Cypher
{
    [TestFixture]
    public class TransactionsTests
    {
        [Test]
        public void ExecuteCypher_ShouldThrowNotSupportedExceptionIfTheresNoTransactionEndpoint()
        {
            var cypherQuery = new CypherQuery("CYPHER", new Dictionary<string, object>(), CypherResultMode.Set);
            
            using (var testHarness = new RestTestHarness
            {
                {
                    MockRequest.Get(""),
                    MockResponse.Json(HttpStatusCode.OK, @"{
                        'cypher' : 'http://foo/db/data/cypher',
                        'batch' : 'http://foo/db/data/batch',
                        'node' : 'http://foo/db/data/node',
                        'node_index' : 'http://foo/db/data/index/node',
                        'relationship_index' : 'http://foo/db/data/index/relationship',
                        'reference_node' : 'http://foo/db/data/node/123',
                        'neo4j_version' : '1.5.M02',
                        'extensions_info' : 'http://foo/db/data/ext',
                        'extensions' : {
                            'GremlinPlugin' : {
                                'execute_script' : 'http://foo/db/data/ext/GremlinPlugin/graphdb/execute_script'
                            }
                        }
                    }")
                }
            })
            {
                var graphClient = testHarness.CreateAndConnectGraphClient();

                using (new TransactionScope())
                {
                    Assert.Throws<NotSupportedException>(() => graphClient.ExecuteCypher(cypherQuery));
                }
            }
        }

        [Test]
        public void ExecuteCypher_ShouldEstablishFirstCallWithinTransaction()
        {
            var cypherQuery = new CypherQuery("CYPHER", new Dictionary<string, object>(), CypherResultMode.Set);
            var cypherApiQuery = new CypherTransactionApiQuery(cypherQuery);

            using (var testHarness = new RestTestHarness
            {
                {
                    MockRequest.PostObjectAsJson("/transaction", cypherApiQuery),
                    MockResponse.Json(
                        HttpStatusCode.Created,
                        @"
                            {
                                'commit' : 'http://foo/db/data/transaction/6/commit',
                                'results' : [
                                    {
                                        'columns' : [ 'n' ],
                                        'data' : [ { 'row' : [ {'name':'My Node'} ] } ]
                                    }
                                ],
                                'transaction' : {
                                    'expires' : 'Tue, 10 Sep 2013 10:54:04 +0000'
                                },
                                'errors' : [ ]
                            }
                        ",
                        new Dictionary<string, string>
                        {
                            { "Location", "http://foo/db/data/transaction/6" }
                        }
                    )
                }
            })
            {
                var graphClient = testHarness.CreateAndConnectGraphClient();

                using (new TransactionScope())
                {
                    graphClient.ExecuteCypher(cypherQuery);
                }
            }
        }
    }
}
