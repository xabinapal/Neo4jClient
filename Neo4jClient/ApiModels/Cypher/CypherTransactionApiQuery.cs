using System.Collections.Generic;
using System.Linq;
using Neo4jClient.Cypher;
using Newtonsoft.Json;

namespace Neo4jClient.ApiModels.Cypher
{
    class CypherTransactionApiQuery
    {
        readonly IEnumerable<CypherTransactionStatement> statements;

        public CypherTransactionApiQuery(params CypherQuery[] queries)
        {
            statements = queries
                .Select(q => new CypherTransactionStatement
                {
                    Statement = q.QueryText,
                    Parameters = q.QueryParameters
                })
                .ToArray();
        }

        [JsonProperty("statements")]
        public IEnumerable<CypherTransactionStatement> Statements
        {
            get { return statements; }
        }

        public class CypherTransactionStatement
        {
            [JsonProperty("statement")]
            public string Statement;

            [JsonProperty("parameters")]
            public IDictionary<string, object> Parameters;

            [JsonProperty("resultDataContents")]
            public IEnumerable<string> ResultDataContents
            {
                get
                {
                    return new[] { "REST" };
                }
            }
        }
    }
}
