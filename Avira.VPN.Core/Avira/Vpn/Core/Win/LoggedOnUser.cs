using System;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Management;
using System.Security.Principal;

namespace Avira.VPN.Core.Win
{
    public class LoggedOnUser
    {
        public static string GetCurrentUserName()
        {
            return (string)new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem").Get()
                .Cast<ManagementBaseObject>().First()["UserName"];
        }

        public static string GetCurrentUserSid()
        {
            string currentUserName = GetCurrentUserName();
            if (!string.IsNullOrEmpty(currentUserName))
            {
                return ((SecurityIdentifier)new NTAccount(currentUserName).Translate(typeof(SecurityIdentifier)))
                    .ToString();
            }

            return string.Empty;
        }

        public static bool CurrentUserIsNotAdmin()
        {
            return !IsAdmin(GetCurrentUserName());
        }

        public static bool IsAdmin(string user)
        {
            if (string.IsNullOrEmpty(user))
            {
                return false;
            }

            if (user.Contains("\\"))
            {
                string[] array = user.Split('\\');
                using PrincipalContext context2 = new PrincipalContext(ContextType.Machine, null);
                using PrincipalContext context = CreatePrincipalContext(array[0]);
                using UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, array[1]);
                using GroupPrincipal group = GroupPrincipal.FindByIdentity(context2, IdentityType.Sid, "S-1-5-32-544");
                return userPrincipal.IsMemberOf(group);
            }

            return false;
        }

        private static PrincipalContext CreatePrincipalContext(string domainOrMachine)
        {
            PrincipalContext principalContext = null;
            if (!domainOrMachine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                principalContext = Catch.All(() => new PrincipalContext(ContextType.Domain, domainOrMachine), null);
            }

            if (principalContext == null)
            {
                principalContext = Catch.All(() => new PrincipalContext(ContextType.Machine, domainOrMachine), null);
            }

            return principalContext;
        }
    }
}