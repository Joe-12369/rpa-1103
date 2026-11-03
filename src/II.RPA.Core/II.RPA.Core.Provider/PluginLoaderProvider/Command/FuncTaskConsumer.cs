using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Core.Log;

namespace II.RPA.Core.Provider.PluginLoaderProvider.Command
{
    public class FuncTaskConsumer : IDisposable
    {
        private const int TIMEOUT_MILLISECONDS = 10000;
        private const int QUEUE_CAPACITY = 1000;
        private readonly BlockingCollection<QueueItem> m_CommandCollection;
        private readonly ILogger m_Logger;
        private readonly CancellationTokenSource cts;
        private readonly CancellationToken ct;

        public FuncTaskConsumer(ILogger aLogger)
        {
            m_Logger = aLogger;
            m_CommandCollection = new BlockingCollection<QueueItem>(QUEUE_CAPACITY);
            cts = new CancellationTokenSource();
            ct = cts.Token;
            StartConsume();
        }

        public void Dispose()
        {
            m_CommandCollection.CompleteAdding();
            cts.Cancel();
        }

        public Task Run(Func<CancellationToken, Task> func, CancellationToken token)
        {
            var mergeSrc = CancellationTokenSource.CreateLinkedTokenSource(ct, token);
            var item = new QueueItem
            {
                Func = func,
                MergeSrc = mergeSrc,
                Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously),
            };

            if (m_CommandCollection.TryAdd(item, TIMEOUT_MILLISECONDS))
            {
                m_Logger.LogDebug("Added func to queue");
            }
            else
            {
                mergeSrc.Cancel();
                mergeSrc.Dispose();
                item.Completion.TrySetException(new TimeoutException("Fail to add func to queue"));
                m_Logger.LogWarning("Fail to add func!");
                m_Logger.LogWarning($"Queue sum: {m_CommandCollection.Count}");
            }

            return item.Completion.Task;
        }

        private void StartConsume()
        {
            Task.Factory.StartNew(
                ConsumeLoopAsync,
                ct,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
                .Unwrap();
        }

        private async Task ConsumeLoopAsync()
        {
            m_Logger.LogDebug("start func consuming");
            try
            {
                foreach (var item in m_CommandCollection.GetConsumingEnumerable(ct))
                {
                    try
                    {
                        m_Logger.LogDebug("FuncTaskConsumer entry func");
                        await item.Func(item.MergeSrc.Token).ConfigureAwait(false);
                        item.Completion.TrySetResult(true);
                    }
                    catch (OperationCanceledException) when (item.MergeSrc.Token.IsCancellationRequested)
                    {
                        item.Completion.TrySetCanceled(item.MergeSrc.Token);
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError("FuncTaskConsumer consume error", ex);
                        item.Completion.TrySetException(ex);
                    }
                    finally
                    {
                        item.MergeSrc.Dispose();
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // shutdown
            }
        }

        private sealed class QueueItem
        {
            public Func<CancellationToken, Task> Func { get; set; }
            public CancellationTokenSource MergeSrc { get; set; }
            public TaskCompletionSource<bool> Completion { get; set; }
        }
    }
}
