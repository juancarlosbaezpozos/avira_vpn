using System.Collections.Generic;
using Avira.Acp;
using Avira.Acp.Extensions;
using Avira.Acp.Messages.JsonApi;
using Avira.Acp.Resources.Common;

namespace Avira.VPN.Acp
{
    public class AppStatusRepository : BaseResourceRepository<AppStatus>
    {
        private readonly List<Resource<AppStatus>> repository = new List<Resource<AppStatus>>();

        public AppStatusRepository()
        {
            repository.Add(CreateAppStateResource(string.Empty, string.Empty));
        }

        public void Update(string newStatus, string newStatusText)
        {
            Resource<AppStatus> resource = CreateAppStateResource(newStatus, newStatusText);
            repository[0] = resource;
            OnUpdated(new UpdatedEventArgs<AppStatus>(resource.Attributes, resource.Id, resource.Type));
        }

        public override List<Resource<AppStatus>> ReadAll(string filter)
        {
            return repository;
        }

        private Resource<AppStatus> CreateAppStateResource(string status, string statusText)
        {
            return new Resource<AppStatus>
            {
                Id = "1",
                Type = typeof(AppStatus).GetAcpTypeName(),
                Attributes = new AppStatus
                {
                    Section = "app_state",
                    Value = new Dictionary<string, string>
                    {
                        { "status", status },
                        { "display_text", statusText }
                    }
                }
            };
        }
    }
}