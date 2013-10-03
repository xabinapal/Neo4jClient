using System.Collections.Generic;

namespace Neo4jClient.Cypher
{
    interface ITransactionCoordinator
    {
        IDictionary<string, CypherTransaction> ActiveCypherTransactions { get; }
        void CommitTransaction(CypherTransaction transaction);
        void RollbackTransaction(CypherTransaction transaction);
    }
}
