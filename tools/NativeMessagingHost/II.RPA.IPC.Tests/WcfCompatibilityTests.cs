using II.RPA.IPC.Builder;
using II.RPA.IPC.Config;
using Xunit;

namespace II.RPA.IPC.Tests
{
    public class WcfCompatibilityTests
    {
        [Fact]
        public void LegacyPublicTypesKeepTheirQualifiedNames()
        {
            Assert.Equal("II.RPA.IPC.ClientGenerator", typeof(ClientGenerator).FullName);
            Assert.Equal("II.RPA.IPC.ServerGenerator", typeof(ServerGenerator).FullName);
            Assert.Equal("II.RPA.IPC.IMessageExchange", typeof(IMessageExchange).FullName);
            Assert.Equal("II.RPA.IPC.IRequestParams", typeof(IRequestParams).FullName);
            Assert.Equal("II.RPA.IPC.IResponseResult", typeof(IResponseResult).FullName);
            Assert.Equal("II.RPA.IPC.Builder.PipeServerBuilder", typeof(PipeServerBuilder).FullName);
            Assert.Equal("II.RPA.IPC.Config.PipeConfig", typeof(PipeConfig).FullName);
        }

        [Fact]
        public void LegacyAssemblyIdentityRemainsUnchanged()
        {
            Assert.Equal("II.RPA.IPC", typeof(ClientGenerator).Assembly.GetName().Name);
        }
    }
}
