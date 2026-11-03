using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.Commons
{
    public class RunningStateManager
    {
        private ManualResetEvent runningEvent; 
        private RunningState curRunningState = RunningState.None;
        public RunningState CurRunningState => curRunningState;

        public RunningStateManager()
        {
            runningEvent = new ManualResetEvent(false); 
            SetRunningState(RunningState.Running);// 默认为Running状态
        }
        public void SetRunningState(RunningState runningState)
        {
            this.curRunningState = runningState;
            switch (curRunningState)
            {
                case RunningState.Running:
                    runningEvent.Set(); // 设置事件
                    break;
                case RunningState.Paused:
                    runningEvent.Reset(); // 重置事件
                    break;
            }
        }

        public void WaitForRunning()
        {
            if (CurRunningState != RunningState.Running)
            {
                runningEvent.WaitOne(); // 阻塞等待，直到A状态被设置
            }
        }
    }
}
