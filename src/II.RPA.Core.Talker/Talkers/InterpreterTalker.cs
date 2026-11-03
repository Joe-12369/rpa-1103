using II.RPA.Core.Log;
using II.RPA.Core.Talker.Processes;

namespace II.RPA.Core.Talker.Talkers
{
    public class InterpreterTalker : EngineTalker
    {
        private readonly InterpreterProcess _interpreterProcess;
        private readonly SemaphoreSlim _restartGate = new(1, 1);

        public InterpreterTalker(
            ITalkerManager talkerManager,
            ILogger logger,
            II.RPA.Core.Runtime.IRpaRuntimeContext runtimeContext) : base(talkerManager, logger)
        {
            _interpreterProcess = new InterpreterProcess(logger, runtimeContext);
        }

        public override string Name => "interpreter";

        public Models.ProcessStatus ProcessStatus => _interpreterProcess.Status;

        public override Task Start()
        {
            _interpreterProcess.Start();
            return Task.CompletedTask;
        }

        public override async Task Stop()
        {
            await _interpreterProcess.StopAsync();
            await base.Stop();
        }

        public async Task RestartAsync(CancellationToken cancellationToken = default)
        {
            await _restartGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await Stop().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                await Start().ConfigureAwait(false);
            }
            finally
            {
                _restartGate.Release();
            }
        }
    }
}
