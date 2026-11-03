using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.Commons
{
    public class CtsTimeout
    {
        private readonly object cts_lock = new object();
        private CancellationTokenSource cts = null;
        private Stopwatch stopwatch;
        private TimeSpan timeout;
        private string Key { get; set; }
        public CtsTimeout(int milliseconds)
            : this(TimeSpan.FromMilliseconds(milliseconds))
        { }
        public CtsTimeout(TimeSpan timeout, string key = null)
        {
            stopwatch = new Stopwatch();
            this.timeout = timeout;
            if (string.IsNullOrEmpty(key))
            {
                key = Guid.NewGuid().ToString();
            }
            this.Key = key;
        }
        public void ResetTimeout(int milliseconds)
        {
            ResetTimeout(TimeSpan.FromMilliseconds(milliseconds));
        }
        public void ResetTimeout(TimeSpan timeSpan)
        {
            lock (cts_lock)
            {
                this.timeout = timeSpan;
                if (cts != null)
                {
                    cts.CancelAfter(timeSpan);
                }
            }
        }
        public Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, Func<TimeSpan, Task> onTimeoutAsync)
        {
            return ExecuteAsync((CancellationToken _) => action(), default(CancellationToken), onTimeoutAsync);
        }
        public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken token, Func<TimeSpan, Task> onTimeoutAsync)
        {
            cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                onTimeoutAsync(TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds));
            });
            stopwatch.Start();
            var result = action(token);
            cts?.Dispose();
            return result;
        }
    }
}
