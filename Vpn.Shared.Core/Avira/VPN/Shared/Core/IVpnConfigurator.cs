using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avira.VPN.Shared.Core
{
    public interface IVpnConfigurator
    {
        IDictionary<string, object> DebugInformation { get; }

        Task ConfigureProfileAsync(RegionConnectionSettings connectionSettings, Credentials credentials);

        Task<ProfileAuthorizationStatus> GetProfileAuthorizationStatusAsync();
    }
}