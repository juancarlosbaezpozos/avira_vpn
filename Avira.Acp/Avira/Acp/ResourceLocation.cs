using System.Runtime.Serialization;

namespace Avira.Acp
{
    [DataContract]
    public class ResourceLocation
    {
        [DataMember(Name = "path")] public string Path { get; set; }

        [DataMember(Name = "host")] public string Host { get; set; }

        public ResourceLocation(string host, string path)
        {
            Host = host;
            Path = path;
        }

        public ResourceLocation()
        {
        }

        public static bool operator ==(ResourceLocation firstLocation, ResourceLocation secondLocation)
        {
            if ((object)firstLocation == secondLocation)
            {
                return true;
            }

            if ((object)firstLocation == null || (object)secondLocation == null)
            {
                return false;
            }

            return firstLocation.Equals(secondLocation);
        }

        public static bool operator !=(ResourceLocation firstLocation, ResourceLocation secondLocation)
        {
            return !(firstLocation == secondLocation);
        }

        public override int GetHashCode()
        {
            int num = Path?.GetHashCode() ?? 0;
            int num2 = Host?.GetHashCode() ?? 0;
            return num ^ num2;
        }

        public override string ToString()
        {
            return Host + Path;
        }

        public override bool Equals(object obj)
        {
            ResourceLocation resourceLocation = obj as ResourceLocation;
            if (resourceLocation == null)
            {
                return false;
            }

            if (Path != resourceLocation.Path)
            {
                return false;
            }

            if (Host != resourceLocation.Host)
            {
                return false;
            }

            return true;
        }

        public bool CheckMatch(ResourceLocation otherResourceLocation)
        {
            if (!IsValid() || otherResourceLocation == null || !otherResourceLocation.IsValid())
            {
                return false;
            }

            if (!Host.Equals(otherResourceLocation.Host))
            {
                return false;
            }

            if (MatchAnyPath())
            {
                return true;
            }

            if (!otherResourceLocation.Path.StartsWith(Path))
            {
                return false;
            }

            if (otherResourceLocation.Path.Length == Path.Length)
            {
                return true;
            }

            if (otherResourceLocation.Path[Path.Length] != '/')
            {
                return otherResourceLocation.Path[Path.Length] == '?';
            }

            return true;
        }

        public bool IsValid()
        {
            if (Host != null && Path != null)
            {
                if (!MatchAnyPath())
                {
                    return !Path.Contains("*");
                }

                return true;
            }

            return false;
        }

        private bool MatchAnyPath()
        {
            return Path == "*";
        }
    }
}