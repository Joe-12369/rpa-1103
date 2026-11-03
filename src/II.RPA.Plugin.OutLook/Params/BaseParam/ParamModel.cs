using II.RPA.Plugin.OutLook.Params.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OutLook.Params.BaseParam
{
    public class ParamModel : II.RPA.Plugin.Core.PluginBase.PluginParameterBase
    {
        /// <summary>
        /// 邮箱账户,可能不需要
        /// </summary>
        public string MailAccount { get; set; }
        /// <summary>
        /// 邮件对象类型 （0-邮件主题 1-邮件对象）
        /// </summary>
        public int MailType { get; set; }
        /// <summary>
        /// 待操作邮件
        /// </summary>
        public string AwaitingOperateMail { get; set; }

    }
}
