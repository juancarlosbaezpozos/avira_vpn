using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectServices
{
    [DataContract(Name = "apps")]
    public class App
    {
        [DataContract]
        public class AppResource
        {
            [DataContract]
            public class Platform
            {
                [DataMember(Name = "os")] public List<string> Os { get; set; }

                [DataMember(Name = "browsers")] public List<string> Browsers { get; set; }
            }

            [DataContract]
            public class Screenshot
            {
                [DataMember(Name = "img_src")] public string ImageSource { get; set; }
            }

            [DataContract]
            public class OeFeature
            {
                [DataMember(Name = "title")] public string Title { get; set; }

                [DataMember(Name = "description")] public string Description { get; set; }
            }

            [DataMember(Name = "icon")] public string Icon { get; set; }

            [DataMember(Name = "oeName")] public string Name { get; set; }

            [DataMember(Name = "upgrade")] public string UpgradeText { get; set; }

            [DataMember(Name = "connectDisplayName")]
            public string ConnectDisplayName { get; set; }

            [DataMember(Name = "wsLargeImageUrl")] public string ImageUrl { get; set; }

            [DataMember(Name = "shortDescription")]
            public string ShortDescription { get; set; }

            [DataMember(Name = "longDescription")] public string LongDescription { get; set; }

            [DataMember(Name = "privacyPolicy")] public string PrivacyPolicy { get; set; }

            [DataMember(Name = "screenshots")] public List<Screenshot> Screenshots { get; set; }

            [DataMember(Name = "oeFeatures")] public List<OeFeature> Features { get; set; }

            [DataMember(Name = "platforms")] public Platform Platforms { get; set; }

            [DataMember(Name = "oneLiner")] public string OneLiner { get; set; }
        }

        public string Id { get; set; }

        [DataMember(Name = "name")] public string Name { get; set; }

        [DataMember(Name = "prototype")] public string Prototype { get; set; }

        [DataMember(Name = "service")] public string Service { get; set; }

        [DataMember(Name = "type")] public AppType Type { get; set; }

        [DataMember(Name = "resource")] public AppResource Resource { get; set; }

        [DataMember(Name = "is_upgradeable")] public bool IsUpgradeable { get; set; }

        [DataMember(Name = "language")] public string Language { get; set; }
    }
}