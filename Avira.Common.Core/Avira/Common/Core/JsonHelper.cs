using Newtonsoft.Json.Linq;

namespace Avira.Common.Core
{
    public static class JsonHelper
    {
        public static JToken RemoveEmptyChildren(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                {
                    JObject jObject = new JObject();
                    {
                        foreach (JProperty item in token.Children<JProperty>())
                        {
                            JToken jToken2 = item.Value;
                            if (jToken2.HasValues)
                            {
                                jToken2 = RemoveEmptyChildren(jToken2);
                            }

                            if (!IsEmpty(jToken2))
                            {
                                jObject.Add(item.Name, jToken2);
                            }
                        }

                        return jObject;
                    }
                }
                case JTokenType.Array:
                {
                    JArray jArray = new JArray();
                    {
                        foreach (JToken item2 in token.Children())
                        {
                            JToken jToken = item2;
                            if (jToken.HasValues)
                            {
                                jToken = RemoveEmptyChildren(jToken);
                            }

                            if (!IsEmpty(jToken))
                            {
                                jArray.Add(jToken);
                            }
                        }

                        return jArray;
                    }
                }
                default:
                    return token;
            }
        }

        public static bool IsEmpty(JToken token)
        {
            return token.Type == JTokenType.Null;
        }
    }
}