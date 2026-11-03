using II.RPA.WebBrowser.Common.Basic;
using Xunit;

namespace II.RPA.WebBrowser.Common.Tests
{
    public class SmokeTests
    {
        [Fact]
        public void TestAssemblyCanLoadCommon()
        {
            Assert.Equal("II.RPA.WebBrowser.Common", typeof(ConnectionFactory).Assembly.GetName().Name);
        }
    }
}
