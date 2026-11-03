using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace II.RPA.Plugin.Core
{
    [Serializable]
    public class JMessage
    {
        public JMessage()
        {
            this.UnifyCode = "0";
            this.Code = 0;
            this.Message = "操作成功".I18n("JMessage_55D98C69");
            this.AllowRetry = ((int)AllowRetryType.Allow).ToString();
        }
        public JMessage(int code, string message)
        {
            this.UnifyCode = code.ToString();
            this.Code = code;
            this.Message = message;
            this.AllowRetry = ((int)AllowRetryType.Allow).ToString();
        }
        [JsonProperty("code")]
        public int Code { get; set; } = 0;    //返回状态,0成功，其他异常
        [JsonProperty("unifyCode")]
        public string UnifyCode { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty; //如果失败，返回失败原因
        [JsonProperty("info")]
        public string Info { get; set; } = string.Empty;             //非必填，只有当需要返回信息的接口时才需要
        [JsonProperty("extend")]
        public string Extend { get; set; } = string.Empty;
        /// <summary>
        /// 允许重试，1允许，0不允许 此字段长期有效
        /// </summary>
        [JsonProperty("allowRetry")]
        public string AllowRetry { get; set; }

        /// <summary>
        /// 【务必慎用】此属性会导致pluginaction 重复执行。在具有状态破坏性的action中，要确认状态无影响才可使用。
        /// </summary>
        [JsonIgnore]
        public bool RetryRunOnce { get; set; } = false;
        [JsonProperty("aiBuriedData")]
        public string AiBuriedData { get; set; }
    }

    public enum AllowRetryType
    {
        NotAllow = 0,
        Allow = 1
    }
}
