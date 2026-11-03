using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.NativeMessageingStartup.wndMapping
{
    public sealed class KeepAliveManager : IDisposable
    {
        private readonly Func<CancellationToken, Task> _sendKeepAlive;
        private readonly int _intervalMs;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly Task _loopTask;
        private long _lastTicks;

        public KeepAliveManager(Func<CancellationToken, Task> sendKeepAlive,
                              int intervalMs = 3000)
        {
            _sendKeepAlive = sendKeepAlive;
            _intervalMs = intervalMs;
            _loopTask = Task.Run(() => LoopAsync(_cts.Token));
        }

        /// <summary>
        /// 业务线程每次发送消息后调用，重置沉默计时器
        /// </summary>
        public void Reset() => Interlocked.Exchange(ref _lastTicks, Stopwatch.GetTimestamp());

        // 返回距离上次 Reset 过去了多少毫秒
        private double ElapsedMs()
        {
            var delta = Stopwatch.GetTimestamp() - Volatile.Read(ref _lastTicks);
            return delta * 1000.0 / Stopwatch.Frequency;
        }
        public void Dispose()
        {
            _cts.Cancel();
            try { _loopTask.Wait(); }
            catch (AggregateException ae)
            { ae.Handle(e => e is OperationCanceledException); }
            _cts.Dispose();
        }

        private async Task LoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                double elapsed = ElapsedMs();
                if (elapsed < _intervalMs)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(_intervalMs - elapsed), ct).ConfigureAwait(false);
                }

                await _sendKeepAlive(ct).ConfigureAwait(false);
                Reset();
            }
        }
    }
}
