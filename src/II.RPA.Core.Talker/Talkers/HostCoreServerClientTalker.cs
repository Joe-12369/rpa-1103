using Grpc.Core;
using II.RPA.Core.Log;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Contracts.Talker.Engine;
using II.RPA.Core.Contracts.Talker.Engine.Message;
using II.RPA.Core.Talker.Processes;
using Rpa.Core.Worker;

namespace II.RPA.Core.Talker.Talkers
{
    public class HostCoreServerClientTalker : BaseTalker
    {
        private readonly ITalkerManager _talkerManager;

        public HostCoreServerClientTalker(ITalkerManager talkerManager, ILogger logger) : base(logger)
        {
            _talkerManager = talkerManager ?? throw new ArgumentNullException(nameof(talkerManager));
        }

        public override string Name => "hostcoreserver";

        protected override async Task HandleFrameStream(FrameMessage frameMessage)
        {
            try
            {
                var hostTalker = _talkerManager
                    .GetTalkers<HostTalker>(readyOnly: false)
                    .FirstOrDefault()
                    ?? throw new InvalidOperationException("Child CoreServer HostTalker 不存在");
                await hostTalker
                    .HandleIncomingFrameAsync(frameMessage)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError($"处理来自 Root CoreServer 的消息失败: {frameMessage.Id}", ex);
                throw;
            }
        }
    }
}
