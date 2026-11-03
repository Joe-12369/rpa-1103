using II.RPA.Core.Contracts.Talker.Engine.Message;
using II.RPA.Core.Talker.Talkers;
using Newtonsoft.Json.Linq;
using Xunit;

namespace II.RPA.Core.Talker.Tests;

public sealed class RunDispatchFailureTests
{
    [Fact]
    public void DispatchFailureCorrelatesRequestAndEndsTheOriginalRun()
    {
        var request = new EngineMessage("run")
        {
            RequestId = "request-123",
            PayloadJobject = new JObject { ["id"] = "run-456" },
        };

        var response = HostTalker.CreateRunDispatchFailure(request, "No Executor became available within 30 seconds.");
        var wire = JObject.Parse(EngineMessage.Serialize(response));

        Assert.Equal("run", wire["action"]?.Value<string>());
        Assert.Equal("request-123", wire["requestId"]?.Value<string>());
        Assert.Equal("run-456", wire["payload"]?["id"]?.Value<string>());
        Assert.Equal("error", wire["payload"]?["status"]?.Value<string>());
        Assert.Equal("system", wire["payload"]?["consoleLogShowLevel"]?.Value<string>());
        Assert.Equal("No Executor became available within 30 seconds.", wire["payload"]?["msg"]?.Value<string>());
    }
}
