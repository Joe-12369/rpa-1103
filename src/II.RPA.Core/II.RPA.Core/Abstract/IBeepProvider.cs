using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Abstract
{
    public interface IBeepProvider
    {
        void SetBeepOverTimeSpan(TimeSpan aTimeSpan);
        void StartBeepDetect();
        Dictionary<string, BeepClientInfo> GetAllAliveClient();
        Action BeepOfflineAction
        { 
            get; 
            set; 
        }
        Action BeepOnlineAction
        { 
            get; 
            set; 
        }
    }
    /// <summary>
    /// 暂时放这里
    /// </summary>
    public class BeepClientInfo
    {
        private readonly Task m_Task;
        private readonly Stopwatch m_Stopwatch;
        private CancellationTokenSource m_CancellationTokenSource;
        public BeepClientInfo()
        {
            m_CancellationTokenSource = new CancellationTokenSource(BeepMaxInterval);
            m_Stopwatch = new Stopwatch();
            m_Task = new Task(async ()=>
            {
                while (!m_CancellationTokenSource.IsCancellationRequested)
                {
                    await Task.Delay(100);
                    if (m_Stopwatch.ElapsedMilliseconds > BeepMaxInterval.TotalMilliseconds)
                    {
                        IsAlive = false;
                        BeepLost();
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }, m_CancellationTokenSource.Token);
        }
        public void BeepGet()
        {
            if (FisrtBeepTime == default)
            {
                m_Task.RunSynchronously();
                m_Stopwatch.Start();
                FisrtBeepTime = DateTime.Now;
            }
            LastBeepTime = DateTime.Now;
            IsAlive = true;
            m_BeepTimes++;
        }
        public bool IsAlive
        {
            get;
            private set;
        }
        public TimeSpan BeepMaxInterval
        {
            get;
            set;
        }
        public DateTime FisrtBeepTime 
        { 
            get; 
            private set; 
        }
        public DateTime LastBeepTime 
        { 
            get;
            private set; 
        }
        private ulong m_BeepTimes = 0;
        public ulong BeepTimes 
        {
            get
            {
                return m_BeepTimes;
            }
            private set
            {
                m_BeepTimes = value;
                m_Stopwatch.Reset();
                m_Stopwatch.Restart();
            }
        }
        public void BeepLost()
        {
            m_CancellationTokenSource.Cancel();
            m_Task?.Dispose();
            m_Stopwatch.Reset();
            m_Stopwatch.Stop();
        }
    }
}
