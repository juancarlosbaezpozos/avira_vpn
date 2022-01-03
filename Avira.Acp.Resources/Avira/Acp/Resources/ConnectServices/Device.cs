using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectServices
{
    [DataContract(Name = "devices")]
    public class Device : ICloneable
    {
        [DataContract(Name = "others")]
        public class OthersData
        {
            [DataMember(Name = "motherboard")] public MotherBoard MotherBoard { get; set; }

            [DataMember(Name = "disks")] public IEnumerable<HardDiskInfo> Disks { get; set; }

            [DataMember(Name = "browsers")] public List<BrowserData> Browsers { get; set; }

            [DataMember(Name = "osver")] public string OsVersion { get; set; }

            [DataMember(Name = "agver")] public string AgentVersion { get; set; }

            [DataMember(Name = "memory")] public MemoryInfo Memory { get; set; }

            [DataMember(Name = "osType")] public string OsType { get; set; }

            [DataMember(Name = "gpus")] public IEnumerable<GraphicCardInfo> GraphicCards { get; set; }

            [DataMember(Name = "resolution")] public string ScreenResolution { get; set; }

            [DataMember(Name = "cpu")] public CPU Cpu { get; set; }

            [DataMember(Name = "macAddresses")] public List<string> MacAddresses { get; set; }

            [DataMember(Name = "experiment_id")] public string ExperimentId { get; set; }

            [DataMember(Name = "ucrt")] public bool Ucrt { get; set; }

            [DataMember(Name = "experiment_group")]
            public string ExperimentGroup { get; set; }
        }

        [DataMember(Name = "name")] public string Name { get; set; }

        [DataMember(Name = "alias")] public string Alias { get; set; }

        [DataMember(Name = "type")] public string Type { get; set; }

        [DataMember(Name = "brand")] public string Brand { get; set; }

        [DataMember(Name = "os")] public string Os { get; set; }

        [DataMember(Name = "os_version")] public string OsVersion { get; set; }

        [DataMember(Name = "model")] public string Model { get; set; }

        [DataMember(Name = "country")] public string Country { get; set; }

        [DataMember(Name = "hardware_id")] public string HardwareId { get; set; }

        [DataMember(Name = "state")] public string State { get; set; }

        [DataMember(Name = "hidden")] public bool? Hidden { get; set; }

        [DataMember(Name = "agent_version")] public string AgentVersion { get; set; }

        [DataMember(Name = "agent_language")] public string AgentLanguage { get; set; }

        [DataMember(Name = "download_source")] public string DownloadSource { get; set; }

        [DataMember(Name = "os_type")] public string OsType { get; set; }

        [DataMember(Name = "others")] public OthersData Others { get; set; } = new OthersData();


        [DataMember(Name = "locked")] public bool? Locked { get; set; }

        [DataMember(Name = "tracking_id")] public string TrackingId { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}