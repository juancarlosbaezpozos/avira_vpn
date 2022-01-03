using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Avira.Acp.Resources.ConnectServices
{
    [DataContract]
    public class BrowserData
    {
        public Browser Browser { get; set; }

        [DataMember(Name = "name")] public string Name { get; set; }

        [DataMember(Name = "version")] public string Version { get; set; }

        [DataMember(Name = "default")] public string Default { get; set; }

        [PermissionSet(SecurityAction.LinkDemand)]
        public static BrowserData FromPath(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(path);
            return new BrowserData
            {
                Browser = GetBrowserByName(versionInfo.ProductName),
                Name = versionInfo.ProductName,
                Version = versionInfo.ProductVersion
            };
        }

        private static Browser GetBrowserByName(string productName)
        {
            return productName switch
            {
                "Google Chrome" => Browser.Chrome,
                "Firefox" => Browser.Firefox,
                "Internet Explorer" => Browser.InternetExplorer,
                "Opera Internet Browser" => Browser.Opera,
                _ => Browser.NoBrowser,
            };
        }
    }
}