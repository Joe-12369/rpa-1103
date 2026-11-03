using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.MultiInstance;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Utils
{
    internal class GuiOperatorProvider
    {
        private static readonly Lazy<GuiOperatorProvider> instance = new Lazy<GuiOperatorProvider>(() => new GuiOperatorProvider());

        public static GuiOperatorProvider Instance => instance.Value;

        private GuiOperatorProvider() { }

        public IEnumerable<IGuiOperator> GuiOperatorEnumerable => BrowserContextProvider.Instance.GuiOperatorEnumerable.Concat(GuiManager.Info.GuiOperators);

        internal bool TryGetSupportedOperator(
                                            Point screenPoint,
                                            AutomationElement rootElement,
                                            AutomationElement pointElement,
                                            out IGuiOperator guiOperator)
        {
            guiOperator = null;

            if (rootElement.Properties.NativeWindowHandle.TryGetValue(out var hWnd))
            {
                var browserWindow = BrowserWindowProvider.MatchBrowserWindowByHandle(hWnd);
                guiOperator = GetContextGuiOperator(browserWindow);
                if (guiOperator != null && guiOperator.IsCanUse(screenPoint, rootElement, pointElement) == true)
                {
                    return true;
                }
                else
                {
                    _ = Task.Run(() => ChromiumBrowserDiscovery.TryDiscover(hWnd));
                }
            }

            guiOperator = GuiManager.Info.GuiOperators
                                    .FirstOrDefault(go => go.Sort >= 0 && go.IsCanUse(screenPoint, rootElement, pointElement));

            return guiOperator != null;
        }

        internal bool TryGetSupportedOperator(
                                            Point screenPoint,
                                            AutomationElement rootElement,
                                            AutomationElement pointElement,
                                            eGuiType guiType,
                                            out IGuiOperator guiOperator)
        {
            guiOperator = null;
            if (guiType.IsWebType())
            {
                if (!rootElement.Properties.NativeWindowHandle.TryGetValue(out var hWnd))
                    return false;
                var browserWindow = BrowserWindowProvider.MatchBrowserWindowByHandle(hWnd, guiType);
                if (browserWindow == null)
                    return false;
                guiOperator = GetContextGuiOperator(browserWindow);
            }
            else
            {
                guiOperator = GuiManager.Info.GuiOperators
                                        .FirstOrDefault(go => go.GuiType == guiType && go.Sort >= 0);
            }
            return guiOperator?.IsCanUse(screenPoint, rootElement, pointElement) == true;
        }


        #region 获取GuiOperator
        internal IGuiOperator GetGuiOperator(string guiTypeStr)
        {
            Enum.TryParse(guiTypeStr, true, out eGuiType guiType);
            return GetGuiOperator(guiType, null);
        }
        internal IGuiOperator GetGuiOperator(string guiTypeStr, WindowObject windowObject = null)
        {
            Enum.TryParse(guiTypeStr, true, out eGuiType guiType);
            return GetGuiOperator(guiType, windowObject);
        }
        internal IGuiOperator GetGuiOperator(eGuiType guiType, WindowObject windowObject = null)
        {
            if (!guiType.IsWebType())// || GuiManager.Info.MultiInstanceGuiTypes.Contains(guiType))
            {
                return GuiManager.Info.GuiOperators.First(go => go.GuiType == guiType);
            }
            if (windowObject == null)
            {
                windowObject = BrowserWindowProvider.GetFirstBrowserByZOrder(guiType);
                if (windowObject == null)
                {
                    ChromiumBrowserDiscovery.TryDiscoverFromNMH();
                    windowObject = BrowserWindowProvider.GetFirstBrowserByZOrder(guiType);
                    if (windowObject == null)
                        throw new Exception(Localize($"未找到浏览器窗口，Type:{guiType}", "controlType_GuiOperatorProvider_6C91D751"));
                }
            }

            var guiOperator = this.GetWebGuiOperator(windowObject);
            if (guiOperator.GuiType != guiType)
            {
                throw new Exception(Localize($"窗口对象匹配到的浏览器窗口非指定类型，wnd type:{guiOperator.GuiType},guiType:{guiType}", "controlType_GuiOperatorProvider_A5192ACC"));
            }
            return guiOperator;
        }
        internal IGuiOperator_6_9_0 GetWebGuiOperator(WindowObject windowObject)
        {
            BrowserWindow browserWindow;
            if (windowObject != null && !(windowObject is BrowserWindow))
            {
                browserWindow = BrowserWindowProvider.MatchBrowserWindowByHandle(windowObject.Handle);
                if (browserWindow == null)
                {
                    ChromiumBrowserDiscovery.TryDiscover(windowObject.Handle);
                    browserWindow = BrowserWindowProvider.MatchBrowserWindowByHandle(windowObject.Handle);
                }
            }
            else
            {
                browserWindow = (BrowserWindow)windowObject;
            }
            return GetWebGuiOperator(browserWindow);
        }
        internal IGuiOperator_6_9_0 GetWebGuiOperator(BrowserWindow browserWindow)
        {
            if (browserWindow == null)
            {
                browserWindow = BrowserWindowProvider.GetFirstBrowserByZOrder();
            }
            else if (browserWindow.WindowType == Commons.WindowMatch.WindowType.None)
            {
                browserWindow = BrowserWindowProvider.MatchBrowserWindowByHandle(browserWindow.Handle);
            }

            if (browserWindow == null || (browserWindow.HitMatchHandler == null && !BrowserWindowProvider.BrowserWindowTypeCheck(browserWindow)))
            {
                throw new Exception("未获取到已适配的浏览器窗口".I18n("controlType_GuiOperatorProvider_C64C8666"));
            }
            return GetContextGuiOperator(browserWindow);
        }

        private IGuiOperator_6_9_0 GetContextGuiOperator(BrowserWindow browserWindow)
        {
            var guiOperator = BrowserContextProvider.Instance.GetGuiOperator(browserWindow);
            return guiOperator ?? BrowserContextProvider.Instance.GetDefaultGuiOperator(browserWindow);
        }
        #endregion
    }
}
