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
                },
                {
                    MockRequest.Delete("/transaction/6"),
                    MockResponse.Json(
                        HttpStatusCode.OK,
                        @"{ 'results':[], 'errors':[] }"
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

        [Test]
        public void ExecuteCypher_ShouldIncludeSecondCallWithinTransaction()
        {
            var cypherQuery1 = new CypherQuery("CYPHER", new Dictionary<string, object>(), CypherResultMode.Set);
            var cypherApiQuery1 = new CypherTransactionApiQuery(cypherQuery1);

            var cypherQuery2 = new CypherQuery("CYPHER2", new Dictionary<string, object>(), CypherResultMode.Set);
            var cypherApiQuery2 = new CypherTransactionApiQuery(cypherQuery2);

            using (var testHarness = new RestTestHarness
            {
                {
                    MockRequest.PostObjectAsJson("/transaction", cypherApiQuery1),
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
                },
                {
                    MockRequest.PostObjectAsJson("/transaction/6", cypherApiQuery2),
                    MockResponse.Json(
                        HttpStatusCode.OK,
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
                        "
                    )
                },
                {
                    MockRequest.Delete("/transaction/6"),
                    MockResponse.Json(
                        HttpStatusCode.OK,
                        @"{ 'results':[], 'errors':[] }"
                    )
                }
            })
            {
                var graphClient = testHarness.CreateAndConnectGraphClient();

                using (new TransactionScope())
                {
                    graphClient.ExecuteCypher(cypherQuery1);
                    graphClient.ExecuteCypher(cypherQuery2);
                }
            }
        }

        [Test]
        public void ExecuteCypher_ShouldRollbackTransactionIfTheTransactionIsNeverCompleted()
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
                },
                {
                    MockRequest.Delete("/transaction/6"),
                    MockResponse.Json(
                        HttpStatusCode.OK,
                        @"{ 'results':[], 'errors':[] }"
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

        [Test]
        public void ExecuteCypher_ShouldThrowExceptionWhenFirstStatementFails()
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
                                'errors' : [
                                    {'code':42001,'status':'STATEMENT_SYNTAX_ERROR','message':'Something broke'}
                                ],
                                'results' : []
                            }
                        "
                    )
                }
            })
            {
                var graphClient = testHarness.CreateAndConnectGraphClient();

                using (new TransactionScope())
                {
                    var neoServerException = Assert.Throws<NeoServerException>(() => graphClient.ExecuteCypher(cypherQuery));
                    Assert.AreEqual("42001", neoServerException.Code);
                    Assert.AreEqual("STATEMENT_SYNTAX_ERROR", neoServerException.Status);
                    Assert.AreEqual("42001 STATEMENT_SYNTAX_ERROR Something broke", neoServerException.Message);
                }
            }
        }

        [Test]
        public void ExecuteCypher_ShouldThrowAggregateExceptionWhenFirstStatementFailsForMultipleReasons()
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
                                'errors' : [
                                    {'code':42001,'status':'STATEMENT_SYNTAX_ERROR','message':'Something broke'},
                                    {'code':56789,'status':'STATEMENT_FOO_ERROR','message':'Something else broke'}
                                ],
                                'results' : []
                            }
                        "
                    )
                }
            })
            {
                var graphClient = testHarness.CreateAndConnectGraphClient();

                using (new TransactionScope())
                {
                    var aggregateException = Assert.Throws<AggregateException>(() => graphClient.ExecuteCypher(cypherQuery));
                    Assert.AreEqual(2, aggregateException.InnerExceptions.Count);
                }
            }
        }

        [Test]
        public void ExecuteCypher_ShouldReleaseInternalTransactionWhenFirstStatementFails()
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
                                'errors' : [
                                    {'code':42001,'status':'STATEMENT_SYNTAX_ERROR','message':'Something broke'}
                                ],
                                'results' : []
                            }
                        "
                    )
                }
            })
            {
                var graphClient = testHarness.CreateAndConnectGraphClient();

                try
                {
                    using (new TransactionScope())
                    {
                        graphClient.ExecuteCypher(cypherQuery);
                    }
                }
                catch (NeoServerException)
                {}

                Assert.AreEqual(0, ((ITransactionCoordinator)graphClient).ActiveCypherTransactions.Count);
            }
        }

        [Test]
        public void ExecuteCypher_ShouldThrowExceptionAndRollbackWhenSecondStatementFails()
        {
            var cypherQuery1 = new CypherQuery("CYPHER", new Dictionary<string, object>(), CypherResultMode.Set);
            var cypherApiQuery1 = new CypherTransactionApiQuery(cypherQuery1);

            var cypherQuery2 = new CypherQuery("CYPHER2", new Dictionary<string, object>(), CypherResultMode.Set);
            var cypherApiQuery2 = new CypherTransactionApiQuery(cypherQuery2);

            using (var testHarness = new RestTestHarness
            {
                {
                    MockRequest.PostObjectAsJson("/transaction", cypherApiQuery1),
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
                },
                {
                    MockRequest.PostObjectAsJson("/transaction/6", cypherApiQuery2),
                    MockResponse.Json(
                        HttpStatusCode.OK,
                        @"
                            {
                                'commit' : 'http://foo/db/data/transaction/6/commit',
                                'errors' : [
                                    {'code':42001,'status':'STATEMENT_SYNTAX_ERROR','message':'Something broke'}
                                ],
                                'results' : []
                            }
                        "
                    )
                },
                {
                    MockRequest.Delete("/transaction/6"),
                    MockResponse.Json(
                        HttpStatusCode.OK,
                        @"{ 'results':[], 'errors':[] }"
                    )
                }
            })
            {
                var graphClient = testHarness.CreateAndConnectGraphClient();

                using (new TransactionScope())
                {
                    graphClient.ExecuteCypher(cypherQuery1);
                    var neoServerException = Assert.Throws<NeoServerException>(() => graphClient.ExecuteCypher(cypherQuery2));
                    Assert.AreEqual("42001", neoServerException.Code);
                    Assert.AreEqual("STATEMENT_SYNTAX_ERROR", neoServerException.Status);
                    Assert.AreEqual("42001 STATEMENT_SYNTAX_ERROR Something broke", neoServerException.Message);
                }
            }
        }

        [Test]
        public void ExecuteCypher_ShouldCommitTransaction()
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
                },
                {
                    MockRequest.PostObjectAsJson("/transaction/6/commit", new CypherTransactionApiQuery()),
                    MockResponse.Json(
                        HttpStatusCode.OK,
                        @"{ 'results':[], 'errors':[] }"
                    )
                }
            })
            {
                var graphClient = testHarness.CreateAndConnectGraphClient();

                using (var transaction = new TransactionScope())
                {
                    graphClient.ExecuteCypher(cypherQuery);
                    transaction.Complete();
                }
            }
        }

        [Test]
        public void ExecuteCypher_ShouldReleaseInternalTransactionAfterRollback()
        {
            var cypherQuery = new CypherQuery("CYPHER", new Dictionary<string, object>(), CypherResultMode.Set);
            var cypherApiQuery = new CypherTransactionApiQuery(cypherQuery);

            var testHarness = new RestTestHarness
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
                            {"Location", "http://foo/db/data/transaction/6"}
                        }
                        )
                },
                {
                    MockRequest.Delete("/transaction/6"),
                    MockResponse.Json(
                        HttpStatusCode.OK,
                        @"{ 'results':[], 'errors':[] }"
                        )
                }
            };

            var graphClient = testHarness.CreateAndConnectGraphClient();

            using (new TransactionScope())
            {
                graphClient.ExecuteCypher(cypherQuery);
                Assert.AreEqual(1, ((ITransactionCoordinator)graphClient).ActiveCypherTransactions.Count);
            }

            Assert.AreEqual(0, ((ITransactionCoordinator)graphClient).ActiveCypherTransactions.Count);
        }

        [Test]
        public void ExecuteCypher_ShouldReleaseInternalTransactionAfterCommit()
        {
            var cypherQuery = new CypherQuery("CYPHER", new Dictionary<string, object>(), CypherResultMode.Set);
            var cypherApiQuery = new CypherTransactionApiQuery(cypherQuery);

            var testHarness = new RestTestHarness
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
                            {"Location", "http://foo/db/data/transaction/6"}
                        }
                        )
                },
                {
                    MockRequest.PostObjectAsJson("/transaction/6/commit", new CypherTransactionApiQuery()),
                    MockResponse.Json(HttpStatusCode.OK, @"{ 'results':[], 'errors':[] }")
                }
            };

            var graphClient = testHarness.CreateAndConnectGraphClient();

            using (var transaction = new TransactionScope())
            {
                graphClient.ExecuteCypher(cypherQuery);
                Assert.AreEqual(1, ((ITransactionCoordinator)graphClient).ActiveCypherTransactions.Count);
                transaction.Complete();
            }

            Assert.AreEqual(0, ((ITransactionCoordinator)graphClient).ActiveCypherTransactions.Count);
        }
    }
}
