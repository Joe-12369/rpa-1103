using II.RPA.Core.Abstract;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Services
{
    public sealed class AdbStartupSignal : IAdbStartupSignal
    {
        private readonly TaskCompletionSource<bool> _ready =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsReady => _ready.Task.IsCompletedSuccessfully;

        public void MarkReady()
        {
            _ready.TrySetResult(true);
        }

        public Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
        {
            return cancellationToken.CanBeCanceled
                ? _ready.Task.WaitAsync(cancellationToken)
                : _ready.Task;
        }
    }
}
