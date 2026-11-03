using II.RPA.Plugin.OutLook.Params.BaseParam;
using II.RPA.Plugin.OutLook.Params.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OutLook.Params
{
    public class ParamOperateMailModel: ParamModel
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public E_MailOperateType MailOperateType { get; set; }
        /// <summary>
        /// 下载附件保存路径
        /// </summary>
        public string AttachmentsSaveAsFolder { get; set; }
        /// <summary>
        /// 邮箱文件夹类型
        /// </summary>
        public E_MailboxFolder BoxFolderType { get; set; }
        /// <summary>
        /// 文件夹名称
        /// </summary>
        public string BoxFolderName { get; set; }
    }
}
