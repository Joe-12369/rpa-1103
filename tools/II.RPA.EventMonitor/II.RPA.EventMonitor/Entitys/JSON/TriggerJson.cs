using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.EventMonitor.JSON
{

    public class TriggerJson
    {
        public TriggerJson()
        {
            this.isRepeat = 1;
            this.postBack = new PostBackJson();
            this.fileTriggers = new List<FileTriggerJson>();
            this.mailTriggers = new List<MailTriggerJson>();
            this.hotkeyTriggers = new List<HotKeyTriggerJson>();
            this.mouseTriggers = new List<MouseTriggerJson>();
        }

        internal string SessionID { get; set; }
        /// <summary>
        /// 
        /// </summary>
        //public string appId { get; set; }
        //public string source { get; set; }
        public PostBackJson postBack { get; set; }
        /// <summary>
        /// 是否重复执行 0 不重复 1重复
        /// </summary>
        public int isRepeat { get; set; }

        public int condition { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public List<FileTriggerJson> fileTriggers { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<HotKeyTriggerJson> hotkeyTriggers { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<MailTriggerJson> mailTriggers { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<MouseTriggerJson> mouseTriggers { get; set; }

    }
    public class PostBackJson
    {
        public PostBackJson()
        {
            this.appId = string.Empty;
            this.source = string.Empty;
            this.requestId = string.Empty;
            this.extendInfo = string.Empty;
            this.targetTriggers = new List<int>();
        }
        public string appId { get; set; }
        public string source { get; set; }
        public string requestId { get; set; }
        public string extendInfo { get; set; }
        public List<int> targetTriggers { get; set; }
        internal void AddTargetTriggerID(int triggerId)
        {
            if (!this.targetTriggers.Contains(triggerId))
            {
                this.targetTriggers.Add(triggerId);
            }
        }
    }
    public class FileTriggerJson
    {
        public int id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string filePath { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int containSubfolder { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string fileType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string monitorEvent { get; set; }

    }



    public class HotKeyTriggerJson
    {
        public int id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string auxiliaryKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string key { get; set; }

    }



    public class MailTriggerJson
    {
        public int id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 检测间隔 （分钟）
        /// </summary>
        public int interval { get; set; }

        /// <summary>
        ///  协议，Pop3  or IMAP
        /// </summary>
        public string protocol { get; set; }

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string serverAddress { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public int port { get; set; }

        /// <summary>
        /// 是否使用SSL
        /// </summary>
        public int isSSL { get; set; }

        /// <summary>
        /// 邮箱账号
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// 登录密码
        /// </summary>
        public string password { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int condition { get; set; }

        /// <summary>
        /// 发件人地址包含的内容
        /// </summary>
        public string sendAddrContent { get; set; }

        /// <summary>
        /// 收件人地址包含的内容
        /// </summary>
        public string recAddrContent { get; set; }

        /// <summary>
        /// 主题包含的内容
        /// </summary>
        public string themeContent { get; set; }

        /// <summary>
        /// 正文所包含的内容
        /// </summary>
        public string bodyContent { get; set; }

    }



    public class MouseTriggerJson
    {
        public int id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string auxiliaryKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int mouseType { get; set; }

    }


    internal enum eConditionType
    {
        AND = 0,
        OR = 1
    }
}
