using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Plugin.Core.PluginBase;

namespace II.RPA.Plugin.Dialog.Params
{
    public class ParamConfigDialogModel : PluginParameterBase
    {
        [JsonProperty("info")]
        public List<InputModel> Info { get; set; }
        /// <summary>
        /// 流程包根目录
        /// </summary>
        [JsonProperty("folderPath")]
        public string FolderPath { get; set; }
        /// <summary>
        /// 1.执行（默认值），2.预览
        /// </summary>
        [JsonProperty("exeMode")]
        public string ExeMode { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("autoOut")]
        public AutoOut AutoOut { get; set; }
        /// <summary>
        /// 1:globalconfig;2:dialog;3:renji
        /// </summary>
        [JsonProperty("runBy")]
        public string RunBy { get; set; }
    }

    public class ParamConfigDialogSubModel
    {
        [JsonProperty("info")]
        public List<object> Info { get; set; }
        /// <summary>
        /// 流程包根目录
        /// </summary>
        [JsonProperty("folderPath")]
        public string FolderPath { get; set; }
        /// <summary>
        /// 1.执行（默认值），2.预览
        /// </summary>
        [JsonProperty("exeMode")]
        public string ExeMode { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("autoOut")]
        public AutoOut AutoOut { get; set; }
    }

    public class DialogOutPut
    {
        [JsonProperty("code")]
        public string Code { get; set; } = "";
        [JsonProperty("info")]
        public string Info { get; set; } = "";
        [JsonProperty("errorMsg")]
        public string ErrorMsg { get; set; } = "";
    }
    public class InputDialogOutPut
    {
        [JsonProperty("code")]
        public string Code { get; set; } = "";
        [JsonProperty("info")]
        public string Info { get; set; } = "";
    }

    public enum ExeMode
    {
        /// <summary>
        /// 执行
        /// </summary>
        Exe = 1,
        /// <summary>
        /// 预览
        /// </summary>
        Pre = 2,
    }

    #region 自动退出
    public class AutoOut
    {
        [JsonProperty("isEnable")]
        public EmOpenAutoOut IsEnable { get; set; }
        [JsonProperty("time")]
        public int Time { get; set; } = 3;
        [JsonProperty("outState")]
        public EmAutoOutStste OutState { get; set; }
    }

    public enum EmOpenAutoOut
    {
        Close = 0,
        Open = 1,
    }

    public enum EmAutoOutStste
    {
        Confirm = 0,
        Cancel = 1,
        Retry =2,
        Abort =3,
    }
    #endregion
}
