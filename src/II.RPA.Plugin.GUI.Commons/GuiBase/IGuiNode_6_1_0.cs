using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.WinAutomation.CommonUtility;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Commons
{
    /// <summary>
    /// 数据采集需求 DataCollecting
    /// </summary>
    public interface IGuiNode_6_1_0 : IGuiWebNode, IGuiNode_MouseExtension, IGuiNode_InputExtension, IGuiNode_GetElementSelectExtension
    {
        eGuiType GuiType { get; }
        bool IsTable { get; }
        string TableNodeSelector { get; }
        /// <summary>
        /// 匹配下一页按钮选择器
        /// </summary>
        /// <param name="paramModel"></param>
        /// <returns></returns>
        List<SelectorProps> MatchNextButtonSelector(ParamDoElementEditorModel paramModel);
        /// <summary>
        /// 自动构建数据采集选择器
        /// </summary>
        /// <param name="paramModel"></param>
        /// <returns></returns>
        List<SelectorProps> AutoBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel);
        /// <summary>
        /// 对比构建数据采集选择器
        /// </summary>
        /// <param name="paramModel"></param>
        /// <returns></returns>
        List<SelectorProps> CompareBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel);
        /// <summary>
        /// 获取下一页按钮selector
        /// </summary>
        /// <returns></returns>
        string GetNextElementBuildSelector(List<string> cssSelectorList);

    }
}
