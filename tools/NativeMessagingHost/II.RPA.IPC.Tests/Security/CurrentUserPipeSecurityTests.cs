using System.Linq;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using II.RPA.IPC.NamedPipe.Security;
using Xunit;

namespace II.RPA.IPC.Tests.Security
{
    public class CurrentUserPipeSecurityTests
    {
        [Fact]
        public void SecurityAllowsOnlyCurrentUser()
        {
            PipeSecurity security = CurrentUserPipeSecurity.Create();
            SecurityIdentifier currentUser = WindowsIdentity.GetCurrent().User;
            PipeAccessRule[] rules = security
                .GetAccessRules(true, false, typeof(SecurityIdentifier))
                .Cast<PipeAccessRule>()
                .ToArray();

            PipeAccessRule rule = Assert.Single(rules);
            Assert.Equal(currentUser, rule.IdentityReference);
            Assert.Equal(AccessControlType.Allow, rule.AccessControlType);
            Assert.True((rule.PipeAccessRights & PipeAccessRights.ReadWrite) == PipeAccessRights.ReadWrite);
        }
    }
}
