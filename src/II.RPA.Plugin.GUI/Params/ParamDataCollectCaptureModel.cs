using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamDataCollectCaptureModel : ParamBase
    {
        /// <summary>
        /// 拾取相似元素的时候需要使用
        /// </summary>
        [JsonProperty("similarSelector")]
        public List<SelectorProps> SimilarSelector { get; set; } = new List<SelectorProps>();
        /// <summary>
        /// 动作指令
        /// </summary>
        [JsonProperty("actionCmd")]
        public string ActionCmd { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("pageInfo")]
        public PageInfo PageInfo { get; set; }
        [JsonProperty("isTable")]
        public bool IsTable { get; set; }

        //下列非入参
        public bool IsSimilarCapture => SimilarSelector?.Count > 0;
        public bool IsFirstCapture => PageInfo == null;

    }


}
