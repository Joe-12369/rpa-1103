using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.IPCExtension.Message
{
    public class IPCStatusMessage
    {
        /// <summary>
        /// 0默认，1已经启动IPC
        /// </summary>
        public int Status { get; set; }
    }
}
