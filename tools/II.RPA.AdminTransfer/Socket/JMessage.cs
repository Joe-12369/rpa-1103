using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.AdminTransfer
{
    [Serializable]
    public class JMessage
    {
        public int status { get; set; } = 1;    //返回状态
        public string message { get; set; } = ""; //如果失败，返回失败原因
        public string info { get; set; } = "";             //非必填，只有当需要返回信息的接口时才需要
    }

    public enum MessageStatusType : int
    {
        FAIL_STATUS = 0,
        SUCCESS_STATUS = 1
    }
}
