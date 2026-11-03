using II.RPA.Plugin.OutLook.Params.BaseParam;
using II.RPA.Plugin.OutLook.Params.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OutLook.Params
{
    public class ParamReplyMailModel : ParamModel
    {
        /// <summary>
        /// 回复模式
        /// </summary>
        public E_MailReplyModel MailReplyType { get; set; }
        /// <summary>
        /// 回复主题
        /// </summary>
        public string ReplyMailSubject { get; set; }
        /// <summary>
        /// 回复内容
        /// </summary>
        public string ReplyMailHTMLBody { get; set; }
        /// <summary>
        /// 多个附件用分号";"隔开
        /// </summary>
        public string ReplyMailAttachments { get; set; }
    }
}
