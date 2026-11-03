using II.RPA.Core.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace II.RPA.Core.Provider.PluginLoaderProvider.Command
{
    /// <summary>
    /// 主线程调用
    /// </summary>
    public class CommandConsumer
    {
        private const int TIMEOUT_MILLISECONDS = 10000;
        private const int QUEUE_CAPACITY = 1000;
        private readonly BlockingCollection<IDispatchCommand> m_CommandCollection;
        private readonly ILogger m_Logger;

        private SynchronizationContext m_SyncContext = null;
        public event EventHandler<CommandEventAgr> BackEvent;
        public CommandConsumer(ILogger aLogger)
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
            m_SyncContext = SynchronizationContext.Current;
            m_Logger = aLogger;
            m_CommandCollection = new BlockingCollection<IDispatchCommand>(QUEUE_CAPACITY);
            if (m_SyncContext == null)
            {
                m_Logger.LogError("m_SyncContext is null");
            }
        }
        public void Add(IDispatchCommand command)
        {
            if (m_CommandCollection.TryAdd(command, TIMEOUT_MILLISECONDS))
            {
                m_Logger.LogDebug($"Added command to queue");
            }
            else
            {
                m_Logger.LogWarning($"Fail to add command!");
                m_Logger.LogWarning($"Queue sum: {m_CommandCollection.Count}");
            }
        }
        [STAThread]
        public async Task StartConsume()
        {
            m_Logger.LogDebug($"start command consuming");
            SendOrPostCallback sendDelegate = new SendOrPostCallback(Sendmethod);
            while (true)
            {
                await Task.Delay(10);
                if (m_CommandCollection.TryTake(out IDispatchCommand command))
                {
                    m_Logger.LogDebug("get command and executing");
                    CommandEventAgr eventAgr = new CommandEventAgr
                    {
                        Value = command
                    };
                    m_SyncContext.Send(sendDelegate, eventAgr);
                }
                else
                {
                    //m_Logger.LogDebug("waiting command...");
                }
            }
        }
        private void Sendmethod(object obj)
        {
            m_Logger.LogDebug($"BackEvent");
            BackEvent(this, (CommandEventAgr)obj); //触发事件
        }
    }
    public class CommandEventAgr : EventArgs
    {
        public IDispatchCommand Value { get; set; }
    }
}
