using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Neo4jClient.Cypher
{
    public class CypherStartBitWithNodeIndexLookup<T> : ICypherStartBit
    {
        readonly string identifier;
        readonly string indexName;
        readonly string key;
        readonly object value;

        public CypherStartBitWithNodeIndexLookup(string identifier, string indexName, Expression<Func<T, object>> expression)
        {
            this.identifier = identifier;
            this.indexName = indexName;
            var pair = DeriveKeyValueFromEqualityComparisonExpression(expression);
            key = pair.Key;
            value = pair.Value;
        }

        public string Identifier { get { return identifier; } }
        public string IndexName { get { return indexName; } }
        public string Key { get { return key; } }
        public object Value { get { return value; } }

        static KeyValuePair<string, object> DeriveKeyValueFromEqualityComparisonExpression(Expression<Func<T, object>> expression)
        {
            throw new NotImplementedException();
        }

        public string ToCypherText(Func<object, string> createParameterCallback)
        {
            var valueParameter = createParameterCallback(value);
            return string.Format("{0}=node:{1}({2} = {3})",
                identifier,
                indexName,
                key,
                valueParameter);
        }
    }
}
