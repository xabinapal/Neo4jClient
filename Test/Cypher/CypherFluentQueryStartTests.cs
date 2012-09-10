using NSubstitute;
using NUnit.Framework;
using Neo4jClient.Cypher;

namespace Neo4jClient.Test.Cypher
{
    public class CypherFluentQueryStartTests
    {
        [Test]
        public void NodeByIndexLookup()
        {
            // http://docs.neo4j.org/chunked/1.8.M07/query-start.html#start-node-by-index-lookup
            //START n=node:nodes(name = "A")
            //RETURN n

            var client = Substitute.For<IRawGraphClient>();
            var query = new CypherFluentQuery(client)
                .StartWithNodeIndexLookup("n", "nodes", "name", "A")
                .Return<object>("n")
                .Query;

            Assert.AreEqual("START n=node:nodes(name = {p0})\r\nRETURN n", query.QueryText);
            Assert.AreEqual("A", query.QueryParameters["p0"]);
        }

        [Test]
        public void NodeByIndexLookupWithConstantStringComparisonExpression()
        {
            // http://docs.neo4j.org/chunked/1.8.M07/query-start.html#start-node-by-index-lookup
            //START n=node:nodes(name = "A")
            //RETURN n

            var client = Substitute.For<IRawGraphClient>();
            var query = new CypherFluentQuery(client)
                .StartWithNodeIndexLookup<Foo>("n", "nodes", f => f.Name == "A")
                .Return<object>("n")
                .Query;

            Assert.AreEqual("START n=node:nodes(Name = {p0})\r\nRETURN n", query.QueryText);
            Assert.AreEqual("A", query.QueryParameters["p0"]);
        }

        [Test]
        public void NodeByIndexLookupWithFieldStringComparisonExpression()
        {
            // http://docs.neo4j.org/chunked/1.8.M07/query-start.html#start-node-by-index-lookup
            //START n=node:nodes(name = "A")
            //RETURN n

            var client = Substitute.For<IRawGraphClient>();
            var value = "A".ToUpperInvariant();
            var query = new CypherFluentQuery(client)
                .StartWithNodeIndexLookup<Foo>("n", "nodes", f => f.Name == value)
                .Return<object>("n")
                .Query;

            Assert.AreEqual("START n=node:nodes(Name = {p0})\r\nRETURN n", query.QueryText);
            Assert.AreEqual("A", query.QueryParameters["p0"]);
        }

        public class Foo
        {
            public string Name { get; set; }
        }
    }
}
