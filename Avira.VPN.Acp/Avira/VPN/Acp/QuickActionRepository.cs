using System;
using System.Collections.Generic;
using Avira.Acp;
using Avira.Acp.Extensions;
using Avira.Acp.Messages.JsonApi;
using Avira.Acp.Resources.Common;
using ServiceStack.Text;

namespace Avira.VPN.Acp
{
    public class QuickActionRepository : BaseResourceRepository<QuickAction>
    {
        private readonly List<Resource<QuickAction>> quickActions;

        public QuickActionRepository()
        {
            quickActions = new List<Resource<QuickAction>>();
        }

        public override List<Resource<QuickAction>> ReadAll(string filter)
        {
            return quickActions;
        }

        public void Add(string id, QuickAction quickAction)
        {
            Resource<QuickAction> resource = new Resource<QuickAction>
            {
                Id = id,
                Type = typeof(QuickAction).GetAcpTypeName(),
                Attributes = quickAction
            };
            quickActions.Add(resource);
            OnCreated(new CreatedEventArgs<QuickAction>(quickAction, resource.Id, resource.Type));
        }

        private JsonObject CreateActionPayload(string actionText)
        {
            return JsonObject.Parse(JsonSerializer.SerializeToString(new SingleResourceDocument<VpnAction>
            {
                Data = new Resource<VpnAction>
                {
                    Type = typeof(VpnAction).GetAcpTypeName(),
                    Attributes = new VpnAction
                    {
                        Command = actionText
                    }
                }
            }));
        }

        public override void DeleteAll(string filter)
        {
            foreach (Resource<QuickAction> quickAction in quickActions)
            {
                OnDeleted(new DeletedEventArgs(quickAction.Id, typeof(VpnAction).GetAcpTypeName()));
            }

            quickActions.Clear();
        }

        public void UpdateResource(string id, QuickAction quickAction)
        {
            Resource<QuickAction> resource = quickActions.Find((Resource<QuickAction> a) => a.Id == id);
            if (resource == null)
            {
                throw new Exception("Resource not available for update");
            }

            resource.Attributes = quickAction;
            OnUpdated(new UpdatedEventArgs<QuickAction>(resource.Attributes, resource.Id, resource.Type));
        }
    }
}