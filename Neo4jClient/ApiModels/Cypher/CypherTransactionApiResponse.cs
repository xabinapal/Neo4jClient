using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Neo4jClient.ApiModels.Cypher
{
    class CypherTransactionApiResponse
    {
        [JsonProperty("commit")]
        public Uri Commit { get; set; }

        [JsonProperty("errors")]
        public IEnumerable<ErrorApiResponse> Errors { get; set; }
    }
}
