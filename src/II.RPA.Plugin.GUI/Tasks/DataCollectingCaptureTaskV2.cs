using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.DataCollection.V2;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace II.RPA.Plugin.GUI
{
    class DataCollectingCaptureTaskV2 : OrdinaryCaptureTask<DataCollectingCaptureTaskV2>
    {
        private static ILogger logger = Logger.OfContext();
        protected override string Name => "数据采集普通元素拾取任务v2";//用于日志
        internal new int Interval { get; set; } = 0;
        internal override void Start()
        {
            Init();
            base.Start();
        }
        internal override void BackToUIThread(Action action)
        {
            //action();
            DataCollectingManagerV2.Instance.DataCollectHighLightWnd?.Dispatcher.Invoke(() =>
            {
                action();
            });
        }
        /// <summary>
        /// 在拾取过程中，GUI类型变化时 执行，虚方法 无具体动作
        /// </summary>
        /// <param name="guiType"></param>
        internal override void CaptureTask_GuiTypeChanged(eGuiType guiType)
        {

        }
        /// <summary>
        /// 拾取窗口前动作（每次）
        /// </summary>
        /// <returns>返回false 则本次不拾取</returns>
        internal override bool CaptureTask_GUICapturpWindowBefore()
        {
            if (DataCollectingManagerV2.Instance.ExecutingDataCollectionApi)
            {
                return false;
            }
            if (DataCollectingManagerV2.Instance.DataCollectHighLightWnd == null)
            {
                return false;
            }
            bool isNonCapturePostion = DataCollectingManagerV2.Instance.IsNonCapturePosition();
            if (isNonCapturePostion)
            {
                DataCollectingManagerV2.Instance.DataCollectHighLightWnd.CurrentHighlightElement = null;
                return false;
            }
            return true;
        }
        internal override void CaptureTask_TickOnceCallback(IGuiNode guiNode)
        {
            logger.LogDebug("CaptureTask_TickOnceCallback DataCollection");
            if (!this.IsStart)
            {
                return;
            }
            if (DataCollectingManagerV2.Instance.DataCollectHighLightWnd == null)
            {
                return;
            }
            if (guiNode == null)
            {
                DataCollectingManagerV2.Instance.DataCollectHighLightWnd.CurrentHighlightElement = null;
                return;
            }

            var rect = guiNode.BoundingRectangle;
            if (rect == Rectangle.Empty)
            {
                logger.LogDebug("得到一个无效元素");
                DataCollectingManagerV2.Instance.DataCollectHighLightWnd.CurrentHighlightElement = null;
            }
            else
            {

                this.ThrowIfCaptureTaskStop();
                DataCollectingManagerV2.Instance.DataCollectHighLightWnd.CurrentHighlightElement = guiNode;
            }
        }
        List<IntPtr> hWndListbak = new List<IntPtr>();
        internal override void GetPointAndWndInfo(out Point screenPoint, out AutomationElement rootElement, out AutomationElement pointElement, out List<IntPtr> hWndList)
        {
            lock (_objLock_GetPointAndWndInfo)
            {

                rootElement = null;
                pointElement = null;
                hWndList = new List<IntPtr>();
                try
                {
                    screenPoint = System.Windows.Forms.Cursor.Position;
                    //得到自根窗口开始到点窗口（通过点直接获取到）的句柄集合
                    hWndList = UiCommon.GetRootWindowListNew(screenPoint);
                    //hWndList = UiCommon.GetRootWindowList(screenPoint);

                    if (!IsTaskEnable)
                    {
                        logger.LogDebug($"GetPointAndWndInfo pause");
                        return;
                    }
                    if (hWndList.Count > 0)
                    {
                        UiCommon.GetWindowThreadProcessId(hWndList.Last(), out var processId);
                        if (processId == GlobleConfigurations.CurrentProcessId)
                        {
                            hWndList = hWndListbak;
                        }
                        else
                        {
                            hWndListbak = hWndList;
                        }

                        foreach (IntPtr hWnd in hWndList)
                        {
                            if (hWnd != IntPtr.Zero)
                            {
                                UiCommon.GetWindowThreadProcessId(hWnd, out var processId1);
                                if (processId1 != GlobleConfigurations.CurrentProcessId)
                                {
                                    rootElement = UiCommon.UIAAutomation.FromHandle(hWnd);   //获取根窗体
                                }
                            }
                            if (rootElement == null)
                            {
                                //this.CaptureRootWnd = null;
                                break;
                            }
                            if (rootElement.Properties.BoundingRectangle.IsSupported && rootElement.BoundingRectangle.Contains(screenPoint))
                            {
                                this.CaptureRootWnd = rootElement;
                                break;
                            }
                        }
                        pointElement = UiCommon.UIAAutomation.FromHandle(hWndList.Last());
                        this.CapturePointWnd = pointElement;
                        this.CaptureScreenPoint = screenPoint;
                        this.CaptureHWndList = hWndList;
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"获取屏幕元素时发生一次异常，将忽略本次异常继续拾取");
                    screenPoint = Point.Empty;
                }
            }
        }
        internal override void CaptureTask_CaptureTaskThreadException(Exception ex)
        {
            if (DataCollectingManagerV2.Instance.DataCollectHighLightWnd == null)
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
            DataCollectingManagerV2.Instance.DataCollectHighLightWnd.CaptureException = ex;
            //Java Chrome 插件未安装异常导致拾取退出 
            DataCollectingManagerV2.Instance.StopDataCollect();

        }
    }
}

