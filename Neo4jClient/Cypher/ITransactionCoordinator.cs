using System.Collections.Generic;

namespace Neo4jClient.Cypher
{
    interface ITransactionCoordinator
    {
        IDictionary<string, CypherTransaction> ActiveCypherTransactions { get; }
        void RollbackTransaction(CypherTransaction transaction);
    }
}
