using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Models
{
    public class ConnectEventSubModel
    {
        public ConnectEventSubModel(ConnectType connectType, string deviceId)
        {
            ConnectType = connectType;
            DeviceId = deviceId;
        }
        public ConnectType ConnectType
        {
            get;
            private set;
        }
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId
        {
            get;
            private set;
        }
    }
    public enum ConnectType
    {
        /// <summary>
        /// 连接
        /// </summary>
        Connect,
        /// <summary>
        /// 断开连接
        /// </summary>
        DisConnect
    }
}
