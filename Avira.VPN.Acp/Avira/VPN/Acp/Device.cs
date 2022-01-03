using Avira.Common.Acp.AppClient;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Acp
{
    public class Device : ResourceClient<DeviceData>
    {
        private string deviceId;

        public Device()
            : base(DiContainer.Resolve<IAcpCommunicator>(), "backend", "/v2/devices")
        {
            deviceId = GeneratedDeviceInfo.GetDeviceId();
        }

        public override DeviceData DeserializePayload(string payload)
        {
            JsonConvert.DeserializeObject<JObject>(payload)!.TryGetValue("data", out var value);
            JArray jArray = value as JArray;
            if (jArray != null)
            {
                return GetDevice(deviceId, jArray);
            }

            return (value as JObject).ToObject<DeviceData>();
        }

        private DeviceData GetDevice(string deviceId, JArray data)
        {
            foreach (JToken datum in data)
            {
                DeviceData deviceData = ((datum as JObject)?.GetValue("attributes") as JObject)?.ToObject<DeviceData>();
                if (deviceData != null && deviceData.hardware_id.ToUpper() == deviceId.ToUpper())
                {
                    deviceData.Id = (datum as JObject).GetValue("id")!.ToString();
                    return deviceData;
                }
            }

            return null;
        }
    }
}