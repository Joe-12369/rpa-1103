using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Models
{
    /// <summary>
    /// 全局事件实体类
    /// </summary>
    public class GlobalEventSubModel
    {
        public GlobalEventSubModel(EventType eventType, string additionMessage)
        {
            EventType = eventType;
            AdditionMessage = additionMessage;
        }

        /// <summary>
        /// EventType
        /// </summary>
        public EventType EventType
        {
            get;
            private set;
        }
        /// <summary>
        /// AdditionMessage
        /// </summary>
        public string AdditionMessage
        {
            get;
            private set;
        }
    }
    public enum EventType
    {
        /// <summary>
        /// 进程启动
        /// </summary>
        AppStarting,
        /// <summary>
        /// 手机连接中
        /// </summary>
        MoblieConnecting,
        /// <summary>
        /// 手机连接成功
        /// </summary>
        MoblieConnected,
        /// <summary>
        /// 手机断开连接
        /// </summary>
        MoblieDisconnected,
        /// <summary>
        /// 手机连接失败
        /// </summary>
        MoblieConnectFailed,
        /// <summary>
        /// 横屏
        /// </summary>
        HorizontalScreenOrientation,
        /// <summary>
        /// 竖屏
        /// </summary>
        verticalScreenOrientation,
        /// <summary>
        /// 检索设备中
        /// </summary>
        CheckMobileing,
        /// <summary>
        /// 等待设备授权中
        /// </summary>
        WaitUsbAuthorise,
    }
}
