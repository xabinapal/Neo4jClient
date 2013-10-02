namespace Neo4jClient.Cypher
{
    interface ITransactionCoordinator
    {
        void RollbackTransaction(CypherTransaction transaction);
    }
}
