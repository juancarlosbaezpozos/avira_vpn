using System;
using System.Runtime.Serialization;

namespace Avira.Acp.Resources.Antivirus
{
    [DataContract]
    public class AboutModel
    {
        [DataMember(Name = "version")] public string Version { get; set; }

        [DataMember(Name = "product_name")] public string ProductName { get; set; }

        [DataMember(Name = "license_expiration_date")]
        public string RawLicenseExpirationDate { get; set; }

        public DateTime? LicenseExpirationDate
        {
            get { return ValueConverter.DateTimeFromAvDate(RawLicenseExpirationDate); }
            set { RawLicenseExpirationDate = ValueConverter.AvDateFromDateTime(value); }
        }

        [DataMember(Name = "license_type")] public string LicenseType { get; set; }

        [DataMember(Name = "vdf_date")] public string RawVdfDate { get; set; }

        public DateTime? VdfDate
        {
            get { return ValueConverter.DateTimeFromAvDate(RawVdfDate); }
            set { RawVdfDate = ValueConverter.AvDateFromDateTime(value); }
        }

        [DataMember(Name = "product_id")] public int ProductId { get; set; }

        [DataMember(Name = "license_serial")] public string LicenseSerial { get; set; }

        [DataMember(Name = "last_update")] public string RawLastUpdate { get; set; }

        public DateTime? LastUpdate
        {
            get { return ValueConverter.DateTimeFromAvDate(RawLastUpdate); }
            set { RawLastUpdate = ValueConverter.AvDateFromDateTime(value); }
        }

        [DataMember(Name = "install_dir")] public string InstallDir { get; set; }

        [DataMember(Name = "is_server_os")] public bool IsServerOs { get; set; }

        [DataMember(Name = "is_b2b_license")] public bool? IsB2BLicense { get; set; }
    }
}