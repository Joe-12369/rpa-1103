using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OutLook.Params.Enum
{
    public enum E_MailOperateType
    {
        /// <summary>
        /// 标为已读
        /// </summary>
        MarkAsRead,
        /// <summary>
        /// 标为未读
        /// </summary>
        MarkAsUnRead,
        /// <summary>
        /// 下载附件
        /// </summary>
        DownLoadAttachments,
        /// <summary>
        /// 删除邮件
        /// </summary>
        DeleteMail,
        /// <summary>
        /// 移动邮件
        /// </summary>
        MoveMail
    }
}
