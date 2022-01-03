using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Avira.VPN.Core
{
    public class UpdateStatus
    {
        private const string StatusFileName = "update/updaterData";

        private IFileFactory fileFactory;

        private JObject status;

        public JObject Status
        {
            get
            {
                if (status == null)
                {
                    status = GetStatus();
                }

                return status;
            }
        }

        public UpdateStatus(IFileFactory fileFactory)
        {
            this.fileFactory = fileFactory;
        }

        public void StoreStatus()
        {
            status = new JObject
            {
                ["gui"] = (JToken)(IsGuiRunning() ? "running" : "not_running"),
                ["previousVersion"] = (JToken)DiContainer.Resolve<IProductSettings>().ProductVersion.ToString()
            };
            fileFactory.CreateApplicationDataFile("update/updaterData").WriteAllText(status.ToString());
        }

        public bool HasUpdated()
        {
            return fileFactory.CreateApplicationDataFile("update/updaterData").Exists().Result;
        }

        public bool IsGuiRunning()
        {
            return DiContainer.Resolve<IProductSettings>().IsGuiRunning();
        }

        public bool GuiWasNotRunning()
        {
            return Status.Property("gui")?.Value.ToString() == "not_running";
        }

        public string GetPreviousVersion()
        {
            return Status.Property("previousVersion")?.Value.ToString();
        }

        private JObject GetStatus()
        {
            try
            {
                return JsonConvert.DeserializeObject<JObject>(
                    fileFactory.CreateApplicationDataFile("update/updaterData").ReadAllText(),
                    new JsonSerializerSettings
                    {
                        Error = delegate(object sender, ErrorEventArgs e) { e.ErrorContext.Handled = true; }
                    });
            }
            catch
            {
                return new JObject();
            }
        }

        public void ClearStatus()
        {
            IFile file = fileFactory.CreateApplicationDataFile("update/updaterData");
            if (file.Exists().Result)
            {
                file.Delete();
            }
        }
    }
}