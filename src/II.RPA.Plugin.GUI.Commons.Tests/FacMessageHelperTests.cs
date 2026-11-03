using System.Threading.Tasks;
using II.RPA.Plugin.Core;
using Newtonsoft.Json;
using Xunit;

namespace II.RPA.Plugin.GUI.Commons.Tests
{
    public class FacMessageHelperTests
    {
        [Fact]
        public async Task SendMessageToFactory_WithTimeout_ReturnsFactoryResponseInfo()
        {
            MessageService.SendMessageToTerminal = (terminalType, action, data) =>
                JsonConvert.SerializeObject(new JMessage
                {
                    Code = 0,
                    Message = "ok",
                    Info = "factory-info"
                });

            var result = await FacMessageHelper.SendMessageToFactory("unit.test.action", new { value = 1 }, 2000);

            Assert.Equal("factory-info", result);
        }
    }
}
