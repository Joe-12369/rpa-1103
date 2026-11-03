using FlaUI.Core.AutomationElements;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiOperator
    {
        /// <summary>
        /// 排序 
        /// </summary>
        int Sort { get; set; }
        /// <summary>
        /// GUI类型，窗口类型
        /// </summary>
        eGuiType GuiType { get; }
        /// <summary>
        /// 开始捕获元素前的准备工作,注意 不要阻塞拾取程序
        /// </summary>
        void ReadyToCapture();
        /// <summary>
        /// 释放相关资源，注意全局变量、注意考虑多线程
        /// </summary>
        void Release();
        /// <summary>
        /// 是否可以使用 当前GuiNode实现
        /// </summary>
        /// <param name="screenPoint">鼠标屏幕坐标点</param>
        /// <param name="rootElement">点对应的根元素</param>
        /// <param name="pointElement">通过点直接拾取的元素</param>
        /// <returns>可以使用返回Ture,否则False</returns>
        bool IsCanUse(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement);

        /// <summary>
        /// 拾取节点
        /// </summary>
        /// <param name="screenPoint">光标屏幕坐标点</param>
        /// <param name="rootElement">根元素（最外层元素）</param>
        /// <param name="pointElement"></param>
        /// <param name="inspectNode"></param>
        /// <returns></returns>
        bool InspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode);
        /// <summary>
        /// 窗体类型变化事件,用来拾取目标窗体的类型信息
        /// </summary>
        //Action<string> WindowTypeChange { get; set; }
        /// <summary>
        /// 更具selector获取元素
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="findOnce"></param>
        /// <returns></returns>
        IGuiNode GetElementFromSelector(GetElementFromSelectorParams args);

        string GetElementFromSelectorAll(GetElementFromSelectorParams args);
    }
}
