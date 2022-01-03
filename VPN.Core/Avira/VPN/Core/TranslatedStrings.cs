using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public class TranslatedStrings
    {
        private readonly JObject translatedStrings;

        public TranslatedStrings(JObject translatedStrings)
        {
            this.translatedStrings = translatedStrings;
        }

        public string Get(string key)
        {
            JProperty jProperty = translatedStrings.Property(key);
            if (jProperty != null)
            {
                return jProperty.Value.ToString();
            }

            return key;
        }
    }
}