using System;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace II.RPA.IPC.NamedPipe.Security
{
    public static class CurrentUserPipeSecurity
    {
        public static PipeSecurity Create()
        {
            SecurityIdentifier currentUser = WindowsIdentity.GetCurrent().User;
            if (currentUser == null)
            {
                throw new InvalidOperationException("The current Windows user does not have a security identifier.");
            }

            var security = new PipeSecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new PipeAccessRule(currentUser, PipeAccessRights.FullControl, AccessControlType.Allow));
            security.SetOwner(currentUser);
            return security;
        }
    }
}
