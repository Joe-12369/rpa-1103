using Grpc.Core;
using II.RPA.Core.Talker.Talkers;
using Xunit;

namespace II.RPA.Core.Talker.Tests
{
    public class ChildCoreServerConnectionFailureTests
    {
        [Fact]
        public void IsNotReady_TrueForMissingPortFile()
        {
            Assert.True(ChildCoreServerConnectionFailure.IsNotReady(new FileNotFoundException("未找到端口文件")));
        }

        [Fact]
        public void IsNotReady_TrueForInvalidPortFile()
        {
            Assert.True(ChildCoreServerConnectionFailure.IsNotReady(new InvalidDataException("端口文件内容无效")));
        }

        [Fact]
        public void IsNotReady_TrueForUnavailableGrpc()
        {
            var unavailable = new RpcException(new Status(StatusCode.Unavailable, "failed to connect to all addresses"));

            Assert.True(ChildCoreServerConnectionFailure.IsNotReady(unavailable));
        }

        [Fact]
        public void IsNotReady_FalseForOtherFailures()
        {
            Assert.False(ChildCoreServerConnectionFailure.IsNotReady(new RpcException(new Status(StatusCode.Internal, "boom"))));
            Assert.False(ChildCoreServerConnectionFailure.IsNotReady(new InvalidOperationException("talker not started")));
        }
    }
}
