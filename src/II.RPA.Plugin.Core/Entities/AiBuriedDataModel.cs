using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.Entities
{
    public class AiBuriedDataModel
    {
        public bool IsEnableAiRepaire { get; set; }
        public bool IsAiRepaireOfCache { get; set; }
        public AiRepaireStatus AiRepaireStatus { get; set; }
        public string ElementId { get; set; }
        public string ElementName { get; set; }
        public string TargetApp { get; set; }
        public AiRepaireType AiRepaireType { get; set; }
        public string AiRepairePoint { get; set; }
        public AiRepaireTriggerType AiRepaireTriggerType { get; set; }
        public string AiRepaireCode { get; set; }
        public string AiRepaireErrorMsg { get; set; }
    }
    public enum AiRepaireStatus
    {
        /// <summary>
        /// 未修复
        /// </summary>
        NoRepaire = 0,
        /// <summary>
        /// 修复成功
        /// </summary>
        RepaireSuccess = 1,
        /// <summary>
        /// 修复失败
        /// </summary>
        RepaireFail = 2,
    }
    public enum AiRepaireType
    {
        /// <summary>
        /// 普通智能修复
        /// </summary>
        Intelligent = 0,
        /// <summary>
        /// AI增强定位
        /// </summary>
        AiPlus = 1,
    }
    public enum AiRepaireTriggerType
    {
        /// <summary>
        /// 元素自身开启Ai增强
        /// </summary>
        ElementSelf = 0,
        /// <summary>
        /// 流程执行开启强制修复
        /// </summary>
        ExecutionEntrance = 1,
    }
}
