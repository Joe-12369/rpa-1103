using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebExtensionInstall
{
    internal class Message
    {
        public int status { get; set; } = 1;    //返回状态
        public int code { get; set; } = 0;// 返回错误码
        public string message { get; set; } = ""; //如果失败，返回失败原因
        public string info { get; set; } = "";             //非必填，只有当需要返回信息的接口时才需要
    }
}
