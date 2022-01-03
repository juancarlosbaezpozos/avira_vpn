using System.Collections.Generic;
using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public class ProductCatalogue
    {
        public class ProductInformation
        {
            [JsonProperty(PropertyName = "id")] public string StoreId { get; set; }

            [JsonProperty(PropertyName = "price")] public string LocalizedPrice { get; set; }

            [JsonProperty(PropertyName = "period")]
            public string Period { get; set; }

            [JsonProperty(PropertyName = "registrationNeeded")]
            public bool RegistrationNeeded { get; set; }

            [JsonProperty(PropertyName = "trial")] public bool Trial { get; set; }

            public string Currency { get; set; }

            public string Price { get; set; }

            public string AviraProductId { get; set; }

            public bool AlreadyPurchased { get; set; }
        }

        [JsonProperty(PropertyName = "productCatalogue")]
        public List<ProductInformation> Catalogue { get; set; }

        public ProductCatalogue()
        {
            Catalogue = new List<ProductInformation>();
        }
    }
}