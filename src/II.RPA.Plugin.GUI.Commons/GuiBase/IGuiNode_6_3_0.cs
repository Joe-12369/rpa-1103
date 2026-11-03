using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.WinAutomation.CommonUtility;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Commons
{
    /// <summary>
    /// 数据采集需求 DataCollecting
    /// </summary>
    public interface IGuiNode_6_3_0 : IGuiNode_6_1_0
    {
        string GetSubElements(ParamGetSubElements param);
        string GetParent(ParamGetParent param);
        string GetSiblings(ParamGetSiblings param);
    }
    public class ParamGetSubElements
    {
        public string Filter { get; set; }
        /// <summary>
        /// 有为空串的情况
        /// </summary>
        public string IndexStr { get; set; }
    }
    public class ParamGetParent
    {
        public string Filter { get; set; }
        public int Level { get; set; }
    }
    public class ParamGetSiblings
    {
        public string Filter { get; set; }
        public int Position { get; set; }
    }
}
