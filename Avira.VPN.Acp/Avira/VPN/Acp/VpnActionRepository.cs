using Avira.Acp;
using Avira.Acp.Messages.JsonApi;

namespace Avira.VPN.Acp
{
    public class VpnActionRepository : BaseResourceRepository<VpnAction>
    {
        public override Resource<VpnAction> Create(Resource<VpnAction> resource)
        {
            OnCreated(new CreatedEventArgs<VpnAction>(resource.Attributes, resource.Id, resource.Type));
            return resource;
        }
    }
}