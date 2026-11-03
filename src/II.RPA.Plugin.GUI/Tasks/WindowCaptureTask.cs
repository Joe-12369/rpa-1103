using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility.Utils;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.DataCollection.V2;
using System.Drawing;
using II.RPA.Plugin.GUI.Windows;

namespace II.RPA.Plugin.GUI.Tasks
{
    internal class WindowCaptureTask : OrdinaryCaptureTask<WindowCaptureTask>
    {
        private static ILogger logger = Logger.OfContext();
        private string windowName = "";
        public WindowCaptureTask()
        {
            this.Tick = WindowCaptureTask_Tick;
        }
        internal override void Start()
        {
            Init();
            base.Start();
        }
        internal override void BackToUIThread(Action action)
        {
            WindowCaptureManager.Instance.WindowHighLightWnd?.Dispatcher.Invoke(() =>
            {
                action();
            });
        }

        internal override void CaptureTask_CaptureTaskThreadException(Exception ex)
        {
            if (WindowCaptureManager.Instance.WindowHighLightWnd == null)
            {
                return;
            }
            if (ex is RPAException)
            {
                var rpaex = (RPAException)ex;
                rpaex.ComponentType = eComponentType.Capture;
                //DataCollectingManagerV2.Instance.DataCollectHighLightWnd
                //DataCollectingManager.dataCollectOverlayWnd.CaptureError = rpaex.Message;

                if (rpaex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_SogouExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_SogouExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_JavaExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_ServerHubUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_BrowserPluginVersionTooLow)
                {
                    FacMessageHelper.SendExceptionToFactory("1", rpaex.Message);
                }
                else
                {
                    FacMessageHelper.SendExceptionToFactory("0", rpaex.Message);
                }
            }
            WindowCaptureManager.Instance.WindowHighLightWnd.CaptureException = ex;
            //Java Chrome 插件未安装异常导致拾取退出 
            WindowCaptureManager.Instance.StopCaptureWindow();
        }

        internal override void CaptureTask_TickOnceCallback(IGuiNode guiNode)
        {
            logger.LogDebug("CaptureTask_TickOnceCallback DataCollection");
            if (!this.IsStart)
            {
                return;
            }
            if (WindowCaptureManager.Instance.WindowHighLightWnd == null)
            {
                return;
            }
            if (guiNode == null)
            {
                WindowCaptureManager.Instance.WindowHighLightWnd.CurrentHighlightElement = null;
                return;
            }

            var rect = guiNode.BoundingRectangle;
            if (rect == Rectangle.Empty)
            {
                logger.LogDebug("得到一个无效元素");
                WindowCaptureManager.Instance.WindowHighLightWnd.CurrentHighlightElement = null;
            }
            else
            {

                this.ThrowIfCaptureTaskStop();
                WindowCaptureManager.Instance.WindowHighLightWnd.WindowName = windowName;
                WindowCaptureManager.Instance.WindowHighLightWnd.CurrentHighlightElement = guiNode;
            }
        }
        internal void WindowCaptureTask_Tick(object sender, EventArgs e)
        {
            logger.LogDebug("===============Start Ordinary CaptureTask_Tick==============");
            if (!IsTaskEnable)
            {
                Task.Delay(200).Wait();
                logger.LogDebug($"===============Ordinary CaptureTask_Tick isTaskEnable:{IsTaskEnable} 取消");
                return;
            }
            try
            {
                System.Drawing.Point screenPoint = System.Drawing.Point.Empty;
                AutomationElement rootElement = null;
                AutomationElement pointElement = null;
                bool inspectResult = false;
                IGuiNode inspectNode = null;
                if (!CaptureTask_GUICapturpWindowBefore() || !IsTaskEnable)
                {
                    Task.Delay(200).Wait();
                    logger.LogDebug($"===============Ordinary CaptureTask_Tick CaptureTask_GUICapturpWindowBefore 取消");
                    return;
                }
                this.ThrowIfCaptureTaskStop();
                this.GetPointAndWndInfo(out screenPoint, out rootElement, out pointElement, out List<IntPtr> hWndList);
                this.ThrowIfCaptureTaskStop();

                //if (rootElement == null || pointElement == null)
                //{
                //    logger.LogDebug($"===============Ordinary CaptureTask_Tick rootElement pointElement 为空 取消");
                //    return;
                //}
                //if (rootElement.Properties.ProcessId == CurrentProcessId || pointElement.Properties.ProcessId == CurrentProcessId)
                //{
                //    return;
                //}
                if (rootElement == null)
                {
                    logger.LogDebug($"SelectorIssueTrace -- rootElement is null");
                    TickOnceResult = null;
                    return;
                }
                this.ThrowIfCaptureTaskStop();
                if (!CaptureTask_GUICapturpWindowAfter() || !IsTaskEnable)
                {
                    logger.LogDebug($"===============Ordinary CaptureTask_Tick CaptureTask_GUICapturpWindowAfter 取消");
                    return;
                }
                int CurrentElementCapturedProcessID = rootElement?.Properties?.ProcessId;

                if (!this.IsStart)
                {
                    logger.LogDebug($"SelectorIssueTrace --IsStart is {IsStart}");
                    return;
                }

                if (!IsTaskEnable)
                {
                    logger.LogDebug($"SelectorIssueTrace --isTaskEnable is {IsTaskEnable}");
                    return;
                }

                this.ThrowIfCaptureTaskStop();
                windowName = rootElement.Name?.Length > 30 ? rootElement.Name.Substring(0, 30) + "..." : rootElement.Name;
                IGuiNode_6_6_0 CGuiNode = (IGuiNode_6_6_0)(GuiManager.CreateWindowsGuiNode(rootElement));
                CGuiNode.ProcessID = CurrentElementCapturedProcessID;
                //实例化一个新的Windows UIANode 作为默认的元素，尽可能少的使用
                TickOnceResult = CGuiNode;

            }
            catch (COMException ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "拾取程序d发生底层程序异常");
            }
            catch (Exception ex)
            {
                CaptureTickExceptionLog.Log(logger, ex, "拾取程序发生了异常");

                if (ex is RPAException)
                {
                    //只抛出插件相关的错误
                    RPAException rpaex = (RPAException)ex;
                    //GuiElement.overlayForm.CurrentCaptureMode != Params.CaptureModeType.Intelligence  &&

                    if (rpaex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_SogouExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_SogouExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_JavaExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_ServerHubUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnInstalled
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnopen
                          || rpaex.ErrorMessageType == eErrorMessageType.Error_BrowserPluginVersionTooLow)
                    {
                        throw ex;
                    }
                }
            }
        }
    }
}

