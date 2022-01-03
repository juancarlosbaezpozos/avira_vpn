using System;
using System.Collections.Generic;
using Avira.Acp;
using Avira.Acp.Extensions;
using Avira.Acp.Messages.JsonApi;
using Avira.Acp.Resources.Common;
using ServiceStack.Text;

namespace Avira.VPN.Acp
{
    public class QuickActionHandler
    {
        private readonly QuickActionRepository quickActionRepository;

        private readonly VpnActionRepository vpnActionRepository;

        private readonly Dictionary<string, Action<string>> actions = new Dictionary<string, Action<string>>();

        public QuickActionHandler(QuickActionRepository quickActionRepository, VpnActionRepository vpnActionRepository)
        {
            this.quickActionRepository = quickActionRepository;
            this.vpnActionRepository = vpnActionRepository;
            this.vpnActionRepository.Created += delegate(object s, CreatedEventArgs<VpnAction> a)
            {
                if (actions.ContainsKey(a.CreatedResource.Id))
                {
                    actions[a.CreatedResource.Id]?.Invoke(a.CreatedResource.Command);
                }
            };
        }

        public void Add(VpnQuickAction quickAction)
        {
            QuickAction quickAction2 = ToAcpQuickAction(quickAction);
            actions[quickAction.Id] = quickAction.Action;
            quickActionRepository.Add(quickAction.Id, quickAction2);
        }

        private QuickAction ToAcpQuickAction(VpnQuickAction quickAction)
        {
            return new QuickAction
            {
                ActionPayload = CreateActionPayload(quickAction.Id, quickAction.Action, quickAction.Command,
                    quickAction.Tag),
                Text = quickAction.Text,
                Order = 1,
                ActionVerb = "POST",
                ActionUri = "acp://vpn/vpnactions",
                Enabled = quickAction.Enabled
            };
        }

        public void Update(VpnQuickAction quickAction)
        {
            QuickAction quickAction2 = ToAcpQuickAction(quickAction);
            actions[quickAction.Id] = quickAction.Action;
            quickActionRepository.UpdateResource(quickAction.Id, quickAction2);
        }

        private JsonObject CreateActionPayload(string id, Action<string> action, string command, string tag)
        {
            return JsonObject.Parse(JsonSerializer.SerializeToString(new SingleResourceDocument<VpnAction>
            {
                Data = new Resource<VpnAction>
                {
                    Type = typeof(VpnAction).GetAcpTypeName(),
                    Attributes = new VpnAction
                    {
                        Command = command,
                        Id = id,
                        Tag = tag
                    }
                }
            }));
        }
    }
}