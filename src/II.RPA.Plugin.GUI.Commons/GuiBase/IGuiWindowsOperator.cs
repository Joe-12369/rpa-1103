using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using System.Drawing;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiWindowsOperator : IGuiOperator
    {
        /// <summary>
        /// 根据坐标点匹配子元素
        /// </summary>
        /// <param name="screenPoint">光标屏幕坐标点</param>
        /// <param name="rootNode">指定要匹配子元素的元素</param>
        /// <param name="inspectNode"></param>
        /// <returns></returns>
        bool InspectNodeFromElement(Point screenPoint, AutomationElement rootNode, out IGuiNode inspectNode);

        /// <summary>
        /// 根据Element构建selector
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        string BuilderSelector(AutomationElement element);

        /// <summary>
        /// 根据selector构建Element
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        AutomationElement BuilderAutomationElement(string selector,WindowObject tragetWindow);
    }
}
