using System;
using System.Collections.Generic;
using System.Linq;
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

        [Test]
        public void ExecuteGetCypherResults_ShouldThrowNotSupportedExceptionIfTheresNoTransactionEndpoint()
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
                    Assert.Throws<NotSupportedException>(() => graphClient.ExecuteGetCypherResults<object>(cypherQuery));
                }
            }
        }

        [Test]
        public void ExecuteGetCypherResults_ShouldEstablishFirstCallWithinTransaction()
        {
            var cypherQuery = new CypherQuery("CREATE ( bike:Bike { weight: 10 } ) CREATE ( frontWheel:Wheel { spokes: 3 } ) CREATE ( backWheel:Wheel { spokes: 32 } ) CREATE p1 = bike -[:HAS { position: 1 } ]-> frontWheel CREATE p2 = bike -[:HAS { position: 2 } ]-> backWheel RETURN Bike, FrontWheel, BackWheel", new Dictionary<string, object>(), CypherResultMode.Projection);
            var cypherApiQuery = new CypherTransactionApiQuery(cypherQuery);

            using (var testHarness = new RestTestHarness
            {
                {
                    MockRequest.PostObjectAsJson("/transaction", cypherApiQuery),
                    MockResponse.Json(
                        HttpStatusCode.Created,
                        @"{'commit':'http://localhost:7474/db/data/transaction/6/commit','results':[{'columns':['Bike','FrontWheel','BackWheel'],'data':[{'rest':[{'paged_traverse':'http://localhost:7474/node/1214/paged/traverse/{returnType}{?pageSize,leaseTime}','outgoing_relationships':'http://localhost:7474/node/1214/relationships/out','labels':'http://localhost:7474/node/1214/labels','traverse':'http://localhost:7474/node/1214/traverse/{returnType}','all_typed_relationships':'http://localhost:7474/node/1214/relationships/all/{-list|&|types}','property':'http://localhost:7474/node/1214/properties/{key}','all_relationships':'http://localhost:7474/node/1214/relationships/all','self':'http://localhost:7474/node/1214','properties':'http://localhost:7474/node/1214/properties','outgoing_typed_relationships':'http://localhost:7474/node/1214/relationships/out/{-list|&|types}','incoming_relationships':'http://localhost:7474/node/1214/relationships/in','incoming_typed_relationships':'http://localhost:7474/node/1214/relationships/in/{-list|&|types}','create_relationship':'http://localhost:7474/node/1214/relationships','data':{'weight':10}},{'paged_traverse':'http://localhost:7474/node/1215/paged/traverse/{returnType}{?pageSize,leaseTime}','outgoing_relationships':'http://localhost:7474/node/1215/relationships/out','labels':'http://localhost:7474/node/1215/labels','traverse':'http://localhost:7474/node/1215/traverse/{returnType}','all_typed_relationships':'http://localhost:7474/node/1215/relationships/all/{-list|&|types}','property':'http://localhost:7474/node/1215/properties/{key}','all_relationships':'http://localhost:7474/node/1215/relationships/all','self':'http://localhost:7474/node/1215','properties':'http://localhost:7474/node/1215/properties','outgoing_typed_relationships':'http://localhost:7474/node/1215/relationships/out/{-list|&|types}','incoming_relationships':'http://localhost:7474/node/1215/relationships/in','incoming_typed_relationships':'http://localhost:7474/node/1215/relationships/in/{-list|&|types}','create_relationship':'http://localhost:7474/node/1215/relationships','data':{'spokes':3}},{'paged_traverse':'http://localhost:7474/node/1216/paged/traverse/{returnType}{?pageSize,leaseTime}','outgoing_relationships':'http://localhost:7474/node/1216/relationships/out','labels':'http://localhost:7474/node/1216/labels','traverse':'http://localhost:7474/node/1216/traverse/{returnType}','all_typed_relationships':'http://localhost:7474/node/1216/relationships/all/{-list|&|types}','property':'http://localhost:7474/node/1216/properties/{key}','all_relationships':'http://localhost:7474/node/1216/relationships/all','self':'http://localhost:7474/node/1216','properties':'http://localhost:7474/node/1216/properties','outgoing_typed_relationships':'http://localhost:7474/node/1216/relationships/out/{-list|&|types}','incoming_relationships':'http://localhost:7474/node/1216/relationships/in','incoming_typed_relationships':'http://localhost:7474/node/1216/relationships/in/{-list|&|types}','create_relationship':'http://localhost:7474/node/1216/relationships','data':{'spokes':32}}]}]}],'transaction':{'expires':'Thu, 03 Oct 2013 06:50:29 +0000'},'errors':[]}",
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
                    var results = graphClient
                        .ExecuteGetCypherResults<CreateBikeResult>(cypherQuery)
                        .ToArray();

                    Assert.AreEqual(1, results.Count());
                    Assert.AreEqual(10, results[0].Bike.Weight);
                    Assert.AreEqual(3, results[0].FrontWheel.Spokes);
                    Assert.AreEqual(32, results[2].BackWheel.Spokes);
                }
            }
        }

        [Test]
        public void ExecuteGetCypherResults_ShouldIncludeSecondCallWithinTransaction()
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
                    graphClient.ExecuteGetCypherResults<object>(cypherQuery1);
                    graphClient.ExecuteGetCypherResults<object>(cypherQuery2);
                }
            }
        }

        [Test]
        public void ExecuteGetCypherResults_ShouldRollbackTransactionIfTheTransactionIsNeverCompleted()
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
                    graphClient.ExecuteGetCypherResults<object>(cypherQuery);
                }
            }
        }

        [Test]
        public void ExecuteGetCypherResults_ShouldThrowExceptionWhenFirstStatementFails()
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
                    var neoServerException = Assert.Throws<NeoServerException>(() => graphClient.ExecuteGetCypherResults<object>(cypherQuery));
                    Assert.AreEqual("42001", neoServerException.Code);
                    Assert.AreEqual("STATEMENT_SYNTAX_ERROR", neoServerException.Status);
                    Assert.AreEqual("42001 STATEMENT_SYNTAX_ERROR Something broke", neoServerException.Message);
                }
            }
        }

        [Test]
        public void ExecuteGetCypherResults_ShouldThrowAggregateExceptionWhenFirstStatementFailsForMultipleReasons()
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
                    var aggregateException = Assert.Throws<AggregateException>(() => graphClient.ExecuteGetCypherResults<object>(cypherQuery));
                    Assert.AreEqual(2, aggregateException.InnerExceptions.Count);
                }
            }
        }

        [Test]
        public void ExecuteGetCypherResults_ShouldReleaseInternalTransactionWhenFirstStatementFails()
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
                        graphClient.ExecuteGetCypherResults<object>(cypherQuery);
                    }
                }
                catch (NeoServerException)
                { }

                Assert.AreEqual(0, ((ITransactionCoordinator)graphClient).ActiveCypherTransactions.Count);
            }
        }

        [Test]
        public void ExecuteGetCypherResults_ShouldThrowExceptionAndRollbackWhenSecondStatementFails()
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
                    var neoServerException = Assert.Throws<NeoServerException>(() => graphClient.ExecuteGetCypherResults<object>(cypherQuery2));
                    Assert.AreEqual("42001", neoServerException.Code);
                    Assert.AreEqual("STATEMENT_SYNTAX_ERROR", neoServerException.Status);
                    Assert.AreEqual("42001 STATEMENT_SYNTAX_ERROR Something broke", neoServerException.Message);
                }
            }
        }

        [Test]
        public void ExecuteGetCypherResults_ShouldCommitTransaction()
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
                    graphClient.ExecuteGetCypherResults<object>(cypherQuery);
                    transaction.Complete();
                }
            }
        }

        [Test]
        public void ExecuteGetCypherResults_ShouldReleaseInternalTransactionAfterRollback()
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
                graphClient.ExecuteGetCypherResults<object>(cypherQuery);
                Assert.AreEqual(1, ((ITransactionCoordinator)graphClient).ActiveCypherTransactions.Count);
            }

            Assert.AreEqual(0, ((ITransactionCoordinator)graphClient).ActiveCypherTransactions.Count);
        }

        [Test]
        public void ExecuteGetCypherResults_ShouldReleaseInternalTransactionAfterCommit()
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
                graphClient.ExecuteGetCypherResults<object>(cypherQuery);
                Assert.AreEqual(1, ((ITransactionCoordinator)graphClient).ActiveCypherTransactions.Count);
                transaction.Complete();
            }

            Assert.AreEqual(0, ((ITransactionCoordinator)graphClient).ActiveCypherTransactions.Count);
        }

        public class CreateBikeResult
        {
            public Bike Bike { get; set; }
            public Wheel FrontWheel { get; set; }
            public Wheel BackWheel { get; set; }
        }

        public class Bike
        {
            public double Weight { get; set; }
        }

        public class Wheel
        {
            public double Spokes { get; set; }
        }
    }
}
