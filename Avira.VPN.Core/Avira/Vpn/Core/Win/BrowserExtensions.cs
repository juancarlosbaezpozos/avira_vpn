using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core.Win
{
    public class BrowserExtensions
    {
        private const string ListGuid = "7333909a-d049-46e9-bbaa-fa2cfb006686";

        private const string PasswordExtData =
            "Ti427A9gTujfgUl8YycZ3+jCIhn8tU6Kd/n7mRnz+J9ekrEvdBR2ZNvUwOM27VMsSWQoHKRDT/0ONuizn5zGl1VWoNZj+b2yAXhUZtNT0tu89oSI3NjPNRYRw0KpZFNGAtvn3urTTOsRSRTjp4H3SmTuHGrfVTlFE3oPcTkJ5Ma7E8x0aq1jvmZS0kv5l3R+5nsJkLEsVzGgMb7Qlkq6Z+XIv4XisAItVvvADVslJdikBZwse/raLTu3CC3JpHX2PE72icOlwqpJ/xzTT5+ZdHHbgliN4SWXOsVCzOf+J7zP87VHZzEJEcRkjgSQ7O12XKvzLQw4vewsqM4Ox6Wf6tN/lf4Td5u40wV5RRJ+Jbf6e9FguUE8/NL/nz9TO/lIr5BYbhMNzFyMYzepGsbS8ztX7VwR0gXPBSZYUQOgijkEwwWFRaTiTFIIIXOEaBkyEu8jxMbLz4GGoNEX5E3OiyAGNKunGkfGrb77ElnH0U77wqKDxNgIHtpko//bmPmn1a5TNOYatKXV3kNMk6y0jxKo1P1BQ3q+tJRoBt2/wzHmxhEOkneVR9bRtKojcluVOU7KYbFTsDjNvmV9tiILrcmdhGlKc1rU/EqZx+eh7JqwBPthgNKri3c9YjHQAoxO+FAM/wMmytrXDuMSOhCkLCtQAXR9BHmsPobIWxHjwZ2qjLfc/jZx4EY+D4Z+VeV+pHyABjOsyWahY9AX7DH4gf0mOshnEiNdB4B6yCGEctN8E9sR1L6G2aGjnOQ+peX9hPGlQVFPqxVxO+muBnZw6G05dkkIowHo2k7gPN153KwZnE5AzFNMTZX8i0T4VsEuAAHmn3q1lplaPUfeKPvdTm6ugBgWh2DKu3bagXgO2PNDO+DMrUnjPNuEAnCLBnkHEjK/KHzmTZgJ4cu2q0tXBTdng9DYkkfWKXRLSBvxiqtIMYdgghnBJ6AsS2QRwp+4nrTDgmTyAVs+hqbhYgQDEh8+aDaTliIJjPa0nJbfzZbOI86gejuii9O86JmY114eAbUvuZvUnPmu5dxXapQ2D4+bK0B2QIYxE5s75a4tQ7ohI05gVXH06NACo9v31IeUN4tGbd68TV0Kj2ifvOhSCNrptYEWxFybyfPZIwc/mM2fUF1dUYI0t2WmwX49janIP0fzjqgePNWm44wfegtedF/y08v5Gbz4HOFug0snd9NC5/UjmJzxz36oYl4vjhWDDtBlyUHXCEGGVg976Tx1sVoQCVddpTNui+WvfLKB2L9jzODUKUspVzXdc1pBIhVv9fu2CIMaakseFRfTyeQ7C6p06W2Js6aA7aMOlSrFHNAHg/QCNfaQHODo39HTKrnfYR7jvG/n1I6/XNUofPLD7xNLb01BJG7kS1q5AnzsSJRkNb635N/A+r3mm9uylxvx9T9bfJf6TGO7H7FDEdUL9046Vlrrwhb7IdVXf/IrWWp27kLms8a04wvw5yEFKX//te7wBJpH9bcn0/mXbecgHg==";

        private const string ChromeExtensionsSubpath = "AppData\\Local\\Google\\Chrome\\User Data\\Default\\Extensions";

        private const string FirefoxExtensionsSubpath = "AppData\\Roaming\\Mozilla\\Firefox\\Profiles";

        private readonly List<string> users;

        private readonly List<string> passwordManagers;

        public BrowserExtensions()
        {
            users = GetUsers();
            passwordManagers = InitExtensionList();
        }

        public bool ArePasswordManagersInstalled()
        {
            if (!users.Any((string user) => SearchForChromeExtension(user, passwordManagers)))
            {
                return users.Any((string user) => SearchForFirefoxExtension(user, passwordManagers));
            }

            return true;
        }

        private List<string> GetUsers()
        {
            DirectoryInfo parent = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            if (parent == null)
            {
                return new List<string>();
            }

            return new List<string>(Directory.EnumerateDirectories(Path.Combine(parent.FullName, "Users"), "*",
                SearchOption.TopDirectoryOnly));
        }

        private bool SearchForChromeExtension(string userDir, List<string> searchedExtensions)
        {
            try
            {
                foreach (var item in from file in Directory.EnumerateFiles(
                             Path.Combine(userDir, "AppData\\Local\\Google\\Chrome\\User Data\\Default\\Extensions"),
                             "manifest.json", SearchOption.AllDirectories)
                         select new
                         {
                             File = file
                         })
                {
                    JObject jObject = JObject.Parse(File.ReadAllText(item.File));
                    string text = (string?)jObject["name"];
                    string text2 = (string?)jObject["author"];
                    foreach (string searchedExtension in searchedExtensions)
                    {
                        if ((!string.IsNullOrEmpty(text) && text.Contains(searchedExtension)) ||
                            (!string.IsNullOrEmpty(text2) && text2.Contains(searchedExtension)))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        private bool SearchForFirefoxExtension(string userDir, List<string> searchedExtensions)
        {
            try
            {
                foreach (var item in from file in Directory.EnumerateFiles(
                             Path.Combine(userDir, "AppData\\Roaming\\Mozilla\\Firefox\\Profiles"), "extensions.json",
                             SearchOption.AllDirectories)
                         select new
                         {
                             File = file
                         })
                {
                    foreach (JToken item2 in (JArray)JObject.Parse(File.ReadAllText(item.File))["addons"])
                    {
                        string extensionName = (string?)item2["defaultLocale"]!["name"];
                        if (!string.IsNullOrEmpty(extensionName) &&
                            !string.IsNullOrEmpty(searchedExtensions.Find((string s) => extensionName.Contains(s))))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception)
            {
            }

            return false;
        }

        internal static List<string> InitExtensionList()
        {
            return new DataEncryption("7333909a-d049-46e9-bbaa-fa2cfb006686").Decrypt<List<string>>(
                "Ti427A9gTujfgUl8YycZ3+jCIhn8tU6Kd/n7mRnz+J9ekrEvdBR2ZNvUwOM27VMsSWQoHKRDT/0ONuizn5zGl1VWoNZj+b2yAXhUZtNT0tu89oSI3NjPNRYRw0KpZFNGAtvn3urTTOsRSRTjp4H3SmTuHGrfVTlFE3oPcTkJ5Ma7E8x0aq1jvmZS0kv5l3R+5nsJkLEsVzGgMb7Qlkq6Z+XIv4XisAItVvvADVslJdikBZwse/raLTu3CC3JpHX2PE72icOlwqpJ/xzTT5+ZdHHbgliN4SWXOsVCzOf+J7zP87VHZzEJEcRkjgSQ7O12XKvzLQw4vewsqM4Ox6Wf6tN/lf4Td5u40wV5RRJ+Jbf6e9FguUE8/NL/nz9TO/lIr5BYbhMNzFyMYzepGsbS8ztX7VwR0gXPBSZYUQOgijkEwwWFRaTiTFIIIXOEaBkyEu8jxMbLz4GGoNEX5E3OiyAGNKunGkfGrb77ElnH0U77wqKDxNgIHtpko//bmPmn1a5TNOYatKXV3kNMk6y0jxKo1P1BQ3q+tJRoBt2/wzHmxhEOkneVR9bRtKojcluVOU7KYbFTsDjNvmV9tiILrcmdhGlKc1rU/EqZx+eh7JqwBPthgNKri3c9YjHQAoxO+FAM/wMmytrXDuMSOhCkLCtQAXR9BHmsPobIWxHjwZ2qjLfc/jZx4EY+D4Z+VeV+pHyABjOsyWahY9AX7DH4gf0mOshnEiNdB4B6yCGEctN8E9sR1L6G2aGjnOQ+peX9hPGlQVFPqxVxO+muBnZw6G05dkkIowHo2k7gPN153KwZnE5AzFNMTZX8i0T4VsEuAAHmn3q1lplaPUfeKPvdTm6ugBgWh2DKu3bagXgO2PNDO+DMrUnjPNuEAnCLBnkHEjK/KHzmTZgJ4cu2q0tXBTdng9DYkkfWKXRLSBvxiqtIMYdgghnBJ6AsS2QRwp+4nrTDgmTyAVs+hqbhYgQDEh8+aDaTliIJjPa0nJbfzZbOI86gejuii9O86JmY114eAbUvuZvUnPmu5dxXapQ2D4+bK0B2QIYxE5s75a4tQ7ohI05gVXH06NACo9v31IeUN4tGbd68TV0Kj2ifvOhSCNrptYEWxFybyfPZIwc/mM2fUF1dUYI0t2WmwX49janIP0fzjqgePNWm44wfegtedF/y08v5Gbz4HOFug0snd9NC5/UjmJzxz36oYl4vjhWDDtBlyUHXCEGGVg976Tx1sVoQCVddpTNui+WvfLKB2L9jzODUKUspVzXdc1pBIhVv9fu2CIMaakseFRfTyeQ7C6p06W2Js6aA7aMOlSrFHNAHg/QCNfaQHODo39HTKrnfYR7jvG/n1I6/XNUofPLD7xNLb01BJG7kS1q5AnzsSJRkNb635N/A+r3mm9uylxvx9T9bfJf6TGO7H7FDEdUL9046Vlrrwhb7IdVXf/IrWWp27kLms8a04wvw5yEFKX//te7wBJpH9bcn0/mXbecgHg==");
        }
    }
}