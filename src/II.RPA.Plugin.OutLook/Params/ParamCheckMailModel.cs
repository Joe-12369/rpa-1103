using II.RPA.Plugin.OutLook.Params.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OutLook.Params
{
    public class ParamCheckMailModel : BaseParam.ParamModel
    {
        /// <summary>
        /// 邮箱文件夹类型
        /// </summary>
        public E_MailboxFolder BoxFolderType { get; set; }
        /// <summary>
        /// 文件夹名称
        /// </summary>
        public string BoxFolderName { get; set; }
        /// <summary>
        /// 查找范围
        /// </summary>
        public E_MailCheckScope CheckScopeType { get; set; }
        /// <summary>
        /// 收件人包含
        /// </summary>
        public string MailToContains { get; set; }
        /// <summary>
        /// 发件人包含
        /// </summary>
        public string SendToContains { get; set; }
        /// <summary>
        /// 主题包含
        /// </summary>
        public string MailSubjectContains { get; set; }
        /// <summary>
        /// 内容包含
        /// </summary>
        public string MailHTMLBodyContains { get; set; }
        /// <summary>
        /// 附件存在状态
        /// </summary>
        public E_MailAttachment MailAttachmentType { get; set; }
        /// <summary>
        /// 前几封
        /// </summary>
        public int TopCount { get; set; }
        public List<string> MailDateList { get; set; }
    }
}
