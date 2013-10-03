using System;
using Newtonsoft.Json;

namespace Neo4jClient.ApiModels.Cypher
{
    class CypherTransactionApiResponse
    {
        [JsonProperty("commit")]
        public Uri Commit { get; set; }
    }
}
