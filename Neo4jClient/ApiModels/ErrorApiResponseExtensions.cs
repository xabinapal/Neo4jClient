using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo4jClient.ApiModels
{
    internal static class ErrorApiResponseExtensions
    {
        internal static Exception ToException(this IEnumerable<ErrorApiResponse> errors)
        {
            errors = errors == null ? new ErrorApiResponse[0] : errors.ToArray();

            if (!errors.Any())
                return null;

            var exceptions = errors
                .Select(e => (Exception)new NeoServerException(e.Code, e.Status, e.Message))
                .ToArray();

            return exceptions.Length == 1
                ? exceptions.Single()
                : new AggregateException("Multiple errors were returned from the Neo4j server", exceptions);
        }
    }
}
