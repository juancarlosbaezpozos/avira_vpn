using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectClient
{
    [DataContract(Name = "profiles")]
    public class ProfileInfoData
    {
        [DataMember(Name = "first_name")] public string FirstName { get; set; }

        [DataMember(Name = "last_name")] public string LastName { get; set; }

        [DataMember(Name = "email")] public string MailAddress { get; set; }

        [DataMember(Name = "gdpr_confirm")] public string GdprConfirm { get; set; }

        [DataMember(Name = "confirmation_dialog")]
        public bool ConfirmationDialog
        {
            get
            {
                if (!string.IsNullOrEmpty(MailAddress))
                {
                    return string.IsNullOrEmpty(GdprConfirm);
                }

                return false;
            }
        }
    }
}