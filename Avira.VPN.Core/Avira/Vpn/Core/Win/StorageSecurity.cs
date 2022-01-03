using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Avira.VPN.Core.Win
{
    public class StorageSecurity : IStorageSecurity
    {
        private static FileSystemAccessRule LocalSystemRule => new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), FileSystemRights.FullControl,
            AccessControlType.Allow);

        private static FileSystemAccessRule ServiceRule => new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.ServiceSid, null), FileSystemRights.FullControl,
            AccessControlType.Allow);

        private static FileSystemAccessRule BuiltinAdministratorsRule => new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), FileSystemRights.FullControl,
            AccessControlType.Allow);

        private static FileSystemAccessRule BuiltinUsersRule => new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), FileSystemRights.ReadAndExecute,
            AccessControlType.Allow);

        public void AdjustSecurityForSecureDirectoryContainer(string path, bool allowReadForUsers = false)
        {
            if (Directory.Exists(path))
            {
                DirectorySecurity accessControl = Directory.GetAccessControl(path);
                SetRules(allowReadForUsers, accessControl);
                Directory.SetAccessControl(path, accessControl);
            }
        }

        public void AdjustSecurityForSecureFileContainer(string path, bool allowReadForUsers = false)
        {
            if (File.Exists(path))
            {
                FileSecurity accessControl = File.GetAccessControl(path);
                SetRules(allowReadForUsers, accessControl);
                File.SetAccessControl(path, accessControl);
            }
        }

        private void SetRules(bool allowReadForUsers, FileSystemSecurity fileSystemSecurity)
        {
            fileSystemSecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            foreach (FileSystemAccessRule item in fileSystemSecurity
                         .GetAccessRules(includeExplicit: true, includeInherited: true, typeof(NTAccount))
                         .OfType<FileSystemAccessRule>())
            {
                fileSystemSecurity.RemoveAccessRule(item);
            }

            fileSystemSecurity.AddAccessRule(LocalSystemRule);
            fileSystemSecurity.AddAccessRule(ServiceRule);
            fileSystemSecurity.AddAccessRule(BuiltinAdministratorsRule);
            if (allowReadForUsers)
            {
                fileSystemSecurity.AddAccessRule(BuiltinUsersRule);
            }
        }
    }
}