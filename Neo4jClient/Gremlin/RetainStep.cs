namespace Neo4jClient.Gremlin
{
    public static class RetainStep
    {
        public static IGremlinNodeQuery<TNode> RetainV<TNode>(this IGremlinQuery query, string label)
        {
            var newQuery = query.AddBlock(".retain({0})", label);
            newQuery = newQuery.PrependToBlock("{0} = [];", label);
            return new GremlinNodeEnumerable<TNode>(newQuery);
        }

        public static IGremlinRelationshipQuery RetainE(this IGremlinQuery query, string label)
        {
            var newQuery = query.AddBlock(".retain({0})", label);
            newQuery = newQuery.PrependToBlock("{0} = [];", label);
            return new GremlinRelationshipEnumerable(newQuery);
        }

        public static IGremlinRelationshipQuery<TData> RetainE<TData>(this IGremlinQuery query, string label)
            where TData : class, new()
        {
            var newQuery = query.AddBlock(".retain({0})", label);
            newQuery = newQuery.PrependToBlock("{0} = [];", label);
            return new GremlinRelationshipEnumerable<TData>(newQuery);
        }
    }
}