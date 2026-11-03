using II.RPA.EventMonitor.JSON;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.EventMonitor
{
    public class JMessage
    {
        private int _status1 = 0;

        public JMessage()
        {
            status = 0;
            postBack = new PostBackJson();
        }
        public int status
        {
            get { return _status1; }
            set
            {
                this.message = "";
                _status1 = value;
            }
        }
        public string message { get; set; } = "";     //如果失败，返回失败原因
        public string info { get; set; } = "";
        public PostBackJson postBack { get; set; } //非必填，只有当需要返回信息的接口时才需要
    }

  
}
