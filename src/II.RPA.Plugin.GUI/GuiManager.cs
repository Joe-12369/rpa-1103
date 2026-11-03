using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace II.RPA.Plugin.GUI
{
    public class GuiManager
    {
        private static ILogger logger = Logger.OfContext();
        private static GuiRuntimeServices runtimeServices;

        private GuiManager()
        {
            if (runtimeServices == null) throw new InvalidOperationException("Element GUI services have not been initialized.");
            GuiOperators = runtimeServices.Operators.ToList();
            BrowserWindowProvider.OrderWindowMatchers(
                Configurations.Info.Category.Where(c => GuiElement.IsWebSelectorType(c.Name.Replace("GUI", "")))
                                                            .OrderBy(c => c.Sort)
                                                            .Select(c => c.Name.Replace("GUI", ""))
                                                            .ToList());
        }
        private static readonly object _lockObj = new object();
        private static GuiManager _guiManager = null;
        #region 数据
        public List<IGuiOperator> GuiOperators { get; set; }
        internal IDictionary<IntPtr, IGuiOperator> GuiOperatorsDic { get; set; } = new Dictionary<IntPtr, IGuiOperator>();
        internal IDictionary<string, string> GuiChatSelectorCacheDic { get; set; } = new Dictionary<string, string>();
        #endregion

        internal static GuiManager Info
        {
            get
            {
                if (_guiManager == null)
                {
                    lock (_lockObj)
                    {
                        if (_guiManager == null)
                        {
                            _guiManager = new GuiManager();
                        }
                    }
                }
                return _guiManager;
            }
        }

        internal static void Initialize(GuiRuntimeServices services)
        {
            runtimeServices = services ?? throw new ArgumentNullException(nameof(services));
            _guiManager = null;
        }


        /// <summary>
        /// 获取一个windows GuiNode
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        internal static IGuiNode CreateWindowsGuiNode(AutomationElement element)
        {
            return RequireRuntimeServices().WindowsNodeFactory.Create(element);
        }
        /// <summary>
        ///创建一个 GuiCVNode（即 CVNode）
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        internal static IGuiCVNode CreateCVGuiNode(AutomationElement element)
        {
            return RequireRuntimeServices().CVNodeFactory.Create(element);
        }
        /// <summary>
        ///创建一个 GuiCVOperator（即 GuiOperator）
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        internal static IGuiCVOperator650 CreateGuiCVOperator()
        {
            return RequireRuntimeServices().CVOperator;
        }

        internal static IGuiWindowsOperator WindowsOperator => RequireRuntimeServices().WindowsOperator;
        internal static IPluginInvoker PluginInvoker => RequireRuntimeServices().PluginInvoker;

        private static GuiRuntimeServices RequireRuntimeServices()
        {
            return runtimeServices ?? throw new InvalidOperationException("Element GUI services have not been initialized.");
        }


        #region 缓存窗口的GuiType
        internal void ToggleJavaCaptureMode()
        {
            lock (GuiOperatorsDic)
            {
                GuiOperators.First(value => value.GuiType == eGuiType.Swing).Sort *= -1;
                // Re-detect in both directions, including windows cached by the JAB operator.
                var javaWindows = GuiOperatorsDic.Where(entry =>
                    entry.Value.GuiType == eGuiType.Swing || entry.Value.GuiType == eGuiType.Java)
                    .Select(entry => entry.Key).ToArray();
                foreach (var handle in javaWindows)
                    GuiOperatorsDic.Remove(handle);
            }
        }

        internal IGuiOperator GetGuiType(IntPtr hWndFromPoint)
        {
            //hWndFromPoint = UiCommon.WindowFromPoint(screenPoint);
            lock (GuiOperatorsDic)
            {
                if (GuiOperatorsDic.TryGetValue(hWndFromPoint, out var guiOperator)
                    && guiOperator.Sort >= 0)
                    return guiOperator;
                return null;
            }
        }

        /// <summary>
        /// 获取eGuiType类型 (不确定是否可行)
        /// </summary>
        /// <param name="_IGuiNode"></param>
        /// <returns></returns>
        public static eGuiType GetGuiType(IGuiNode _IGuiNode)
        {
            eGuiType ReteGuiType = eGuiType.None;
            if (_IGuiNode is IGuiNode_6_1_0 guiNode)
            {
                ReteGuiType = guiNode.GuiType;
            }
            else if (_IGuiNode is IGuiNode_6_4_0 guiNode2)
            {
                ReteGuiType = guiNode2.GuiType;
            }
            return ReteGuiType;
        }

        internal void CacheWndGuiType(IntPtr hWnd, IGuiOperator guiOperator)
        {
            lock (GuiOperatorsDic)
            {
                if (guiOperator.Sort >= 0)
                    GuiOperatorsDic[hWnd] = guiOperator;
            }
        }
        internal void ClearWndGuiType()
        {
            lock (GuiOperatorsDic)
                GuiOperatorsDic.Clear();
        }
        internal void ClearWndGuiType(IntPtr hWnd)
        {
            lock (GuiOperatorsDic)
                GuiOperatorsDic.Remove(hWnd);
        }
        #endregion

    }
}
