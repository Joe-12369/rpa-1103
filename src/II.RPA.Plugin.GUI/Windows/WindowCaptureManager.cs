using II.RPA.Plugin.GUI.DataCollection.V2.Windows;
using II.RPA.Plugin.GUI.DataCollection.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Tasks;
using System.Drawing;
using II.RPA.WinAutomation.CommonUtility;
using System.Diagnostics;

namespace II.RPA.Plugin.GUI.Windows
{
    internal class WindowCaptureManager
    {
        private static ILogger logger = Logger.OfContext();
        private readonly static Lazy<WindowCaptureManager> _instance = new Lazy<WindowCaptureManager>(() => new WindowCaptureManager());
        public static WindowCaptureManager Instance => _instance.Value;

        private readonly Thread m_UIThread;
        private Dispatcher m_UIDispatcher;

        private WindowHighLightWnd m_WindowHighLightWnd;
        internal WindowHighLightWnd WindowHighLightWnd => m_WindowHighLightWnd;

        internal bool ExecutingDataCollectionApi = false;

        internal string snappath;
        private Bitmap currentDesktopScreenshot;
        private string _selector = string.Empty;
        public string SelectorError { get; internal set; }

        public WindowCaptureManager()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();
            //ManualResetEventSlim m_StartedEvent = new ManualResetEventSlim(false);
            //m_UIThread = new Thread(() =>
            //{
            //    SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
            //    m_UIDispatcher = Dispatcher.CurrentDispatcher;
            //    m_StartedEvent.Set();
            //    Dispatcher.Run();
            //});
            //m_UIThread.SetApartmentState(ApartmentState.STA);
            //m_UIThread.IsBackground = true;
            //m_UIThread.Start();
            //m_StartedEvent.Wait(5000);
            //logger.LogDebug($"WindowCaptureManager {stopwatch.ElapsedMilliseconds}");
            //Task.Run(() =>
            //{
            //    Task.Delay(5000).Wait();
            //    m_StartedEvent.Dispose();
            //});
        }

        #region 开始采集拾取效果
        private ParamCaptureWindowModel CaptureParam { get; set; }
        internal void RestartCapture()
        {
            if (CaptureParam != null)
            {
                StartCaptureWindow(CaptureParam);//这里通过重调StartCapture 初始化拾取状态、初始效果等，或许会有多余操作，但便于统一维护
            }
        }
        public void StartCaptureWindow(ParamCaptureWindowModel paramModel)
        {
            CaptureParam = paramModel;
            Task.Run(() =>
            {
                GuiElement.ReadyToCapture();
                WindowCaptureTask.Instance.Start();
                logger.LogDebug($"StartCapture");
            });
            m_WindowHighLightWnd = new WindowHighLightWnd();
            m_WindowHighLightWnd.DoSelectAction = DoSelect;
            m_WindowHighLightWnd.CancelDoSelectAction = CancelDoSelect;
            m_WindowHighLightWnd.StartCapture(paramModel);
        }

        #endregion

        #region 停止采集效果

        public void StopCaptureWindow()
        {
            m_UIDispatcher.Invoke(new Action(() =>
            {
                m_WindowHighLightWnd?.SuspendCapture();
                RPAUtils.Retry(() =>
                {
                    m_WindowHighLightWnd?.Hide();
                    RPAUtils.Delay(50);
                    return (!m_WindowHighLightWnd.IsActive || m_WindowHighLightWnd.Visibility == System.Windows.Visibility.Hidden);
                }, 500);
            }));
        }

        #endregion

        #region 显示并置顶窗口 ShowWnd()
        public void ShowWnd()
        {
            if (!m_WindowHighLightWnd.IsActive || m_WindowHighLightWnd.Visibility == System.Windows.Visibility.Hidden)
            {
                m_WindowHighLightWnd.Show();
            }

            if (!m_WindowHighLightWnd.Topmost)
            {
                m_WindowHighLightWnd.Topmost = true;
            }
        }
        #endregion

        internal string DoSelect(IGuiNode highlightElement, ParamCaptureWindowModel captureParam)
        {
            string selector = null;
            if (highlightElement != null)
            {
                try
                {
                    var element = new GuiElement(highlightElement);
                    snappath = captureParam.filePath;
                    using (currentDesktopScreenshot = CaptureDesktop())
                    {
                        element.currentInformativeScreenshot = element.CaptureInformativeScreenshot(snappath, currentDesktopScreenshot);
                    }
                    element.screenShotPath = snappath;
                    try
                    {
                        logger.LogDebug($"get selector");
                        selector = GuiElement.OnSelected?.Invoke(element);
                        m_WindowHighLightWnd.Dispatcher.Invoke(() =>
                        {
                            m_WindowHighLightWnd?.Close();
                        });
                    }
                    catch (RPAException e)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "拾取元素操作失败");
                        SelectorError = e.ErrorMessage;
                    }
                    catch (Exception e)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "拾取元素操作失败");
                        //SelectorError = e.Message;
                        SelectorError = eCommonMessageType.DataCollectOverlayWnd_xaml_cs_ShiQuYuanSuCaoZuoShiBaiQing.GetMessage();
                    }
                }
                catch (RPAException e)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "拾取元素操作失败");
                    SelectorError = e.ErrorMessage;
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "完成拾动作时发生了异常，本次拾取结束");
                }
                finally
                {
                    //StopHighlight();
                    //inspectorStatus.StopCheckCursorLoop();
                }
                return selector;
            }
            return "";
        }
        static Bitmap CaptureDesktop()
        {
            try
            {
                return UiCommon.UIAAutomation.GetDesktop().Capture();
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "CaptureDesktop Error");
                return null;
            }
        }
        internal void CancelDoSelect()
        {
            GuiElement.OnCanceled?.Invoke();
            m_WindowHighLightWnd?.Dispatcher.Invoke(() =>
            {
                m_WindowHighLightWnd?.Close();
            });
        }
    }
}

