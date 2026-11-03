using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OutLook.Params
{
    public class ParamSendMailModel : BaseParam.ParamModel
    {
        /// <summary>
        /// 收件人，多个收件人用分号";"隔开
        /// </summary>
        public string MailTo { get; set; }

        /// <summary>
        /// 抄送，多个收件人用分号";"隔开
        /// </summary>
        public string MailCC { get; set; }

        /// <summary>
        /// 密送，多个收件人用分号";"隔开
        /// </summary>
        public string MailBCC { get; set; }

        /// <summary>
        /// 主题
        /// </summary>
        public string MailSubject { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        public string MailHTMLBody { get; set; }

        /// <summary>
        /// 多个附件用分号";"隔开
        /// </summary>
        public string MailAttachments { get; set; }
    }
}
