using II.RPA.Core.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.Utility
{
    public class CtsTimeout
    {
        private readonly object cts_lock = new object();
        private CancellationTokenSource cts = null;
        private Stopwatch stopwatch;
        private readonly ILogger m_Logger;
        public TimeSpan Timeout { get; set; }
        public bool IsTimeout { get; set; } = false;
        public string uniqeID { get; set; }
        public string TimeoutMsg { get; set; }
        public CtsTimeout(int millisecond, ILogger aLogger)
            : this(TimeSpan.FromMilliseconds(millisecond), aLogger, null)
        { }
        public CtsTimeout(TimeSpan timeout, ILogger aLogger, string key = null)
        {
            m_Logger = aLogger;
            stopwatch = new Stopwatch();
            this.Timeout = timeout;
            if (string.IsNullOrEmpty(key))
            {
                key = Guid.NewGuid().ToString();
            }
            this.uniqeID = key;
        }

        public bool HasReset
        {
            get;
            private set;
        } = false;

        public void ResetTimeout(long milliseconds, string timeOutMsg = null)
        {
            ResetTimeout(TimeSpan.FromMilliseconds(milliseconds), timeOutMsg);
        }
        public void ResetTimeout(TimeSpan timeSpan, string timeOutMsg = null)
        {
            lock (cts_lock)
            {
                try
                {
                    var currentCts = cts;
                    if (currentCts == null)
                    {
                        //组件已执行结束，令牌源已被回收，无需再次重置
                        m_Logger.LogDebug($"执行已经结束，无需再次重置超时时间");
                        return;
                    }
                    this.TimeoutMsg = timeOutMsg;
                    this.Timeout = timeSpan;
                    if (currentCts.IsCancellationRequested)
                    {
                        m_Logger.LogDebug($"执行已经结束，无需再次重置超时时间");
                    }
                    if (!currentCts.IsCancellationRequested)
                    {
                        stopwatch.Restart();
                        HasReset = true;
                        m_Logger.LogDebug($"重置超时时间值为：{timeSpan.TotalMilliseconds} ms");
                    }
                }
                catch (Exception ex)
                {
                    m_Logger.LogWarning($"重置超时时间产生异常,Token  {cts}，{ex}");
                }
            }
        }
        public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, Func<TimeSpan, Task> onTimeoutAsync)
        {
            return await ExecuteAsync(async (CancellationToken _) => await action(), default(CancellationToken), onTimeoutAsync);
        }
        public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken token, Func<TimeSpan, Task> onTimeoutAsync)
        {
            try
            {
                var currentCts = new CancellationTokenSource();
                cts = currentCts;
                stopwatch.Start();
                var actionTask = action(token);
                var timeoutTask = WaitForWallClockTimeoutAsync(currentCts.Token);
                var completed = await Task.WhenAny(timeoutTask, actionTask).ConfigureAwait(false);

                if (completed == actionTask)
                {
                    var result = await actionTask.ConfigureAwait(false);
                    if (IsWallClockExpired())
                    {
                        await RaiseTimeoutAsync(onTimeoutAsync).ConfigureAwait(false);
                    }
                    return result;
                }

                if (IsWallClockExpired())
                {
                    await RaiseTimeoutAsync(onTimeoutAsync).ConfigureAwait(false);
                }

                //此处异常即为bug
                throw new Exception("internal error!");
            }
            catch
            {
                throw;
            }
            finally 
            {
                DisposeCts();
            }
        }

        private bool IsWallClockExpired()
        {
            lock (cts_lock)
            {
                return stopwatch.ElapsedMilliseconds >= Timeout.TotalMilliseconds;
            }
        }

        private async Task WaitForWallClockTimeoutAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (IsWallClockExpired())
                {
                    return;
                }

                long remainingMs;
                lock (cts_lock)
                {
                    remainingMs = (long)Math.Max(1, Timeout.TotalMilliseconds - stopwatch.ElapsedMilliseconds);
                }

                var pollInterval = (int)Math.Min(500, remainingMs);
                try
                {
                    await Task.Delay(pollInterval, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task RaiseTimeoutAsync(Func<TimeSpan, Task> onTimeoutAsync)
        {
            IsTimeout = true;
            long elapsed;
            lock (cts_lock)
            {
                elapsed = stopwatch.ElapsedMilliseconds;
            }
            m_Logger.LogWarning($"ExecuteAsync wall-clock timeout after {elapsed} ms (limit {Timeout.TotalMilliseconds} ms)");
            await onTimeoutAsync(TimeSpan.FromMilliseconds(elapsed)).ConfigureAwait(false);
            throw new TimeoutException();
        }
        /// <summary>
        /// 启动或重置超时时间后，已运行的时间
        /// </summary>
        public long ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;
        private void DisposeCts()
        {
            //原子取走令牌源引用，保证并发或重入调用时只取消、释放一次
            var toDispose = Interlocked.Exchange(ref cts, null);
            lock (cts_lock)
            {
                stopwatch.Stop();
            }
            if (toDispose == null)
            {
                return;
            }
            //取消与释放必须在 cts_lock 之外执行：Cancel 会同步触发已注册的延续
            //（令牌回调、Task.Delay 取消、异步状态机续体等），若持锁执行，
            //延续中一旦再次访问 cts_lock 就会与当前线程互相等待，导致组件执行结束后无法返回
            try
            {
                toDispose.Cancel();
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning($"取消令牌源产生异常,Token  {toDispose}，{ex}");
            }
            try
            {
                toDispose.Dispose();
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning($"释放令牌源产生异常,Token  {toDispose}，{ex}");
            }
        }
        public bool IsCancellationRequested
        {
            get
            {
                var currentCts = cts;
                if (currentCts == null)
                {
                    //令牌源已回收（组件执行已结束），视为已取消，避免释放后访问抛异常
                    return true;
                }
                return currentCts.IsCancellationRequested;
            }
        }
    }
}
