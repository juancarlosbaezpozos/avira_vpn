using System.Collections.Generic;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Avira.Acp.Endpoints.NamedPipe
{
    public class PipeSecurityFactory
    {
        public static PipeSecurity CreatePipeSecurity(bool allowGuestsAccounts, bool adminOnly)
        {
            if (!adminOnly)
            {
                return CreateNonAdminPipeSecurity(allowGuestsAccounts);
            }

            return CreateAdminPipeSecurity();
        }

        private static PipeSecurity CreateNonAdminPipeSecurity(bool allowGuestsAccounts)
        {
            List<WellKnownSidType> list = new List<WellKnownSidType> { WellKnownSidType.AuthenticatedUserSid };
            if (allowGuestsAccounts)
            {
                list.Add(WellKnownSidType.BuiltinGuestsSid);
            }

            return CreatePipeSecurity(list);
        }

        private static PipeSecurity CreateAdminPipeSecurity()
        {
            return CreatePipeSecurity(WellKnownSidType.BuiltinAdministratorsSid);
        }

        private static PipeSecurity CreatePipeSecurity(WellKnownSidType wellKnownSidType)
        {
            return CreatePipeSecurity(new List<WellKnownSidType> { wellKnownSidType });
        }

        private static PipeSecurity CreatePipeSecurity(IEnumerable<WellKnownSidType> wellKnownSidTypes)
        {
            PipeSecurity pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,
                PipeAccessRights.FullControl, AccessControlType.Allow));
            foreach (WellKnownSidType wellKnownSidType in wellKnownSidTypes)
            {
                pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(wellKnownSidType, null),
                    PipeAccessRights.ReadWrite, AccessControlType.Allow));
            }

            return pipeSecurity;
        }
    }
}