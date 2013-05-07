using System.Collections;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace Neo4jClient.Deserializer
{
    public class CustomJsonDeserializer
    {
        readonly CultureInfo culture = CultureInfo.InvariantCulture;

        public T Deserialize<T>(string content) where T : new()
        {
            content = CommonDeserializerMethods.ReplaceAllDateInstacesWithNeoDates(content);

            var reader = new JsonTextReader(new StringReader(content))
            {
                DateParseHandling = DateParseHandling.DateTimeOffset
            };

            var target = new T();

            var targetType = target.GetType();

            if (target is IList)
            {
                var json = JToken.ReadFrom(reader);
                return (T)CommonDeserializerMethods.BuildList(targetType, json.Root.Children(), culture, new TypeMapping[0], 0);
            }

            var root = JToken.ReadFrom(reader).Root;
            if (target is IDictionary)
            {
                var valueType = targetType.GetGenericArguments()[1];
                return (T)CommonDeserializerMethods.BuildDictionary(targetType, valueType, root.Children(), culture, new TypeMapping[0], 0);
            }

            CommonDeserializerMethods.Map(target, root, culture, new TypeMapping[0], 0);
            return target;
        }
    }
}
