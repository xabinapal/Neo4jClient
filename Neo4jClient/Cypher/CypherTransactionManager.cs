using System;
using System.Transactions;

namespace Neo4jClient.Cypher
{
    class CypherTransactionManager : IEnlistmentNotification
    {
        public Action CompleteCallback { get; set; }
        public Uri Endpoint { get; set; }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
        }

        public void Commit(Enlistment enlistment)
        {
        }

        public void Rollback(Enlistment enlistment)
        {
        }

        public void InDoubt(Enlistment enlistment)
        {
        }
    }
}
