using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.JavaAlter
{
    [Serializable]
    public class JMessage
    {
        public int code { get; set; } = 0;    //返回状态
        public string message { get; set; } = ""; //如果失败，返回失败原因
        public string info { get; set; } = "";             //非必填，只有当需要返回信息的接口时才需要
    }
    class Rect
    {
        public string x { get; set; }
        public string y { get; set; }
        public string width { get; set; }
        public string height { get; set; }
    }
}
