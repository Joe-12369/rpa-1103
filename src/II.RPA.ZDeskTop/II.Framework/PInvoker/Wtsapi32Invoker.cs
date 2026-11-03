using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.Framework.PInvoker
{
    public static class Wtsapi32Invoker
    {
        /// <summary>
        /// 打开子会话功能
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSEnableChildSessions(bool enable);
        /// <summary>
        /// 判断用户是否打开了子会话功能
        /// </summary>
        /// <param name="isEnabled"></param>
        /// <returns></returns>
        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSIsChildSessionsEnabled(out bool isEnabled);
        /// <summary>
        /// 获取子会话的SessionID
        /// </summary>
        /// <param name="SessionId"></param>
        /// <returns></returns>
        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSGetChildSessionId(out uint SessionId);
        /// <summary>
        /// 注销子会话
        /// </summary>
        /// <param name="SessionId"></param>
        /// <returns></returns>
        [DllImport("Wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSLogoffSession(IntPtr hServer, uint SessionId, bool bWait);

        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSDisconnectSession(IntPtr hServer, uint sessionId, bool bWait);


    }
    
}
