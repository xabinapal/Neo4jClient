using System.Collections.Generic;
using Neo4jClient.Cypher;
using Newtonsoft.Json;

namespace Neo4jClient.ApiModels.Cypher
{
    class CypherTransactionApiQuery
    {
        readonly string queryText;
        readonly IDictionary<string, object> queryParameters;

        public CypherTransactionApiQuery(CypherQuery query)
        {
            queryText = query.QueryText;
            queryParameters = query.QueryParameters ?? new Dictionary<string, object>();
        }

        public class CypherTransactionStatement
        {
            [JsonProperty("statement")]
            public string Statement;

            [JsonProperty("parameters")]
            public IDictionary<string, object> Parameters;
        }

        [JsonProperty("statements")]
        public IEnumerable<CypherTransactionStatement> Statements
        {
            get
            {
                return new[]
                {
                    new CypherTransactionStatement { Statement = queryText, Parameters = queryParameters }
                };
            }
        }
    }
}
