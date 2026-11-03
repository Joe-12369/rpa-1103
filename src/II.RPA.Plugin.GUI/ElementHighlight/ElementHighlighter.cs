using II.RPA.Logging;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace II.RPA.Plugin.GUI.ElementHighlight
{
    public class ElementHighlighter : IDisposable
    {
        private const string CV_BORDER_COLOR = "#4ED0FF";
        private const string NATIVE_BORDER_COLOR = "#FE5402";

        public string BorderBrushColorCode { get { return NATIVE_BORDER_COLOR; } }

        private const string CV_TAG_COLOR = "#4ED0FF";
        private const string NATIVE_TAG_COLOR = "#FE5402";
        private const string INSTEAD_TAG_COLOR = "#0B0F1B";

        private const string EXEPLORE_ICON = "\ue72d";
        private const string WINDOWS_ICON = "\ue722";
        private const string WEB_ICON = "\ue72e";
        private const string CV_ICON = "\ue72c";
        private const string INSTEAD_ICON = "\ue6ed";

        private SolidColorBrush CvBorderBrush;
        private SolidColorBrush NativeBorderBrush;
        private SolidColorBrush CvTagBrush;
        private SolidColorBrush NativeTagBrush;
        private SolidColorBrush InsteadTagBrush;

        private static readonly ILogger logger = Logger.OfContext();
        private SolidColorBrush m_HighlightBorderBrush;
        private SolidColorBrush m_HighlightTagBrush;
        private string m_TagIcon;
        private readonly ManualResetEventSlim m_StartedEvent = new ManualResetEventSlim(false);
        private readonly Thread m_UiThread;
        private HighlightBorder m_BoderWindow;
        private HighlightBorder m_SimpleBoderWindow;
        private bool m_Disabled;
        private bool m_IsWindowTransparent = true;
        private bool m_IsBackgroundTransparent = true;
        private Dispatcher m_Dispatcher;
        private ElementType m_ElementType;
        private HighlightTag m_TagWindow;
        private DispatcherTimer m_Timer;

        #region
        public const int WM_CLOSE = 0x10;
        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        #endregion

        public ElementHighlighter()
        {
            m_UiThread = new Thread(delegate ()
            {
                CvBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(CV_BORDER_COLOR));
                NativeBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(NATIVE_BORDER_COLOR));
                CvTagBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(CV_TAG_COLOR));
                NativeTagBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(NATIVE_TAG_COLOR));
                InsteadTagBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(INSTEAD_TAG_COLOR));

                m_HighlightBorderBrush = NativeBorderBrush;
                m_HighlightTagBrush = NativeTagBrush;

                m_TagIcon = WINDOWS_ICON;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                m_Dispatcher = Dispatcher.CurrentDispatcher;
                m_StartedEvent.Set();
                Dispatcher.Run();
            });
            m_UiThread.SetApartmentState(ApartmentState.STA);
            m_UiThread.IsBackground = true;
            m_UiThread.Start();
            logger.LogInformation($"高亮线程ID:{m_UiThread.ManagedThreadId}");
            logger.LogInformation($"当前线程ID:{Thread.CurrentThread.ManagedThreadId}");
            m_StartedEvent.Wait();

            InitTopmostDispatcher();
        }

        private void InitTopmostDispatcher()
        {
            m_dispatcherTimer = new DispatcherTimer();
            m_dispatcherTimer.Interval = TimeSpan.FromMilliseconds(2200);
            m_dispatcherTimer.Tick += M_dispatcherTimer_Tick;
            m_dispatcherTimer.Start();
            logger.LogInformation("开始置顶线程");
        }

        private void DisposeTopmostDispatcher()
        {
            m_dispatcherTimer.Tick -= M_dispatcherTimer_Tick;
            m_dispatcherTimer?.Stop();
            logger.LogInformation("关闭置顶线程");
        }

        private void M_dispatcherTimer_Tick(object sender, EventArgs e)
        {
            m_Dispatcher.Invoke(()=> {
                SetTopmost(m_CurrentGuiType);
            });
        }

        DispatcherTimer m_dispatcherTimer;
        eGuiType m_CurrentGuiType =  eGuiType.Windows;
          
        public Dispatcher HighLightDispatcher
        {
            get => m_Dispatcher;
        }
   
        private Rectangle m_CurrentRectangle=Rectangle.Empty;
        /// <summary>
        /// 当前元素高亮框
        /// </summary>
        public Rectangle CurrentHightlightRectangle
        {
            get { return m_CurrentRectangle; }
            set { m_CurrentRectangle = value; }
        }

        private Rectangle m_LastOriginRectangle = Rectangle.Empty;
        /// <summary>
        /// 上次要显示的高亮框框体
        /// </summary>
        public Rectangle LastOriginRectangle
        {
            get { return m_LastOriginRectangle; }
            set { m_LastOriginRectangle = value; }
        }

        private Rectangle m_CurrentTagRectangle = Rectangle.Empty;
        /// <summary>
        /// 当前标签框体
        /// </summary>
        public Rectangle CurrentTagRectangle
        {
            get { return m_CurrentTagRectangle; }
            set { m_CurrentTagRectangle = value; }
        }

        private string m_CurrentTag;
        /// <summary>
        /// 当前标签内容
        /// </summary>
        public string CurrentTag
        {
            get { return m_CurrentTag; }
            set { m_CurrentTag = value; }
        }

        public void CloseWindowWithPostMessage()
        {
            IntPtr borderhwnd_win;
            borderhwnd_win = FindWindow(null, nameof(HighlightBorder));
            SendMessage(borderhwnd_win, WM_CLOSE, 0, 0);

            IntPtr taghwnd_win;
            taghwnd_win = FindWindow(null, nameof(HighlightTag));
            SendMessage(taghwnd_win, WM_CLOSE, 0, 0);
        }

        /// <summary>
        /// 设置window背景色 透明/不透明
        /// </summary>
        /// <param name="anIsTranparent"></param>
        public void SetBackgroundTransparent(bool anIsTranparent)
        {
            m_Dispatcher.InvokeAsync(() =>
            {
                m_IsBackgroundTransparent = anIsTranparent;
                m_BoderWindow?.SetBackgroundTransparent(m_IsBackgroundTransparent);
            });
        }
        /// <summary>
        /// 设置window穿透/不穿透
        /// </summary>
        /// <param name="anIsTranparent"></param>
        public void SetWindowTransparent(bool anIsTranparent)
        {
            m_Dispatcher.InvokeAsync(() =>
            {
                m_IsWindowTransparent = anIsTranparent;
                m_BoderWindow?.SetWindowTransparent(m_IsWindowTransparent);
            });
        }
        public void ShowSyncText(string aText)
        {
            m_Dispatcher.InvokeAsync(() =>
            {
               //m_BoderWindow?.SetTagText(aText);
            });
        }
        public void SetTagText(string aTag)
        {
            m_Dispatcher.InvokeAsync(() =>
            {
                m_TagWindow?.SetTagText(aTag);
            });
        }

        public void Dispose()
        {
            Close();
            m_Dispatcher.InvokeShutdown();
            m_UiThread.Join(100);
        }

        /// <summary>
        /// 暂停显示高亮框
        /// </summary>
        public void Pause()
        {
            logger.LogDebug("Pause");
            m_Dispatcher.InvokeAsync(() =>
            {
                m_Disabled = true;
                CloseInternal();
            });
        }
        /// <summary>
        /// 暂停显示高亮框
        /// </summary>
        public void PauseOnly()
        {
            logger.LogDebug("PauseOnly");
            m_Dispatcher.InvokeAsync(() =>
            {
                m_Disabled = true;
            });
        }
        /// <summary>
        /// 恢复、对应暂停
        /// </summary>
        public void Resume()
        {
            m_Dispatcher.InvokeAsync(() => m_Disabled = false);
        }

        public void Show()
        {
            m_Dispatcher.Invoke(delegate
            {
                m_TagWindow?.Show();
                m_BoderWindow?.Show();
                m_SimpleBoderWindow?.Show();
            });
        }

        /// <summary>
        /// 显示高亮框 重载
        /// </summary>
        /// <param name="_ElementHighLighterParam"></param>
        public void Show(ElementHighLighterParam _ElementHighLighterParam)
        {
            Show(_ElementHighLighterParam.CRectangle, _ElementHighLighterParam.CTagName, _ElementHighLighterParam.CElementType, _ElementHighLighterParam.CHighlighterPolicy, _ElementHighLighterParam.EGuiType, _ElementHighLighterParam.IsExeplorer, _ElementHighLighterParam.DurationInMs, _ElementHighLighterParam.CHighLighterFlashParam);
        }

        
        /// <summary>
        /// 显示高亮框 重载
        /// </summary>
        /// <param name="aRectangle"></param>
        /// <param name="aTag"></param>
        /// <param name="elementType"></param>
        public void Show(Rectangle aRectangle,
            string aTag,
            ElementType elementType,
            eGuiType aGuiType,
            bool anIsExeplorer,
            HighlightPolicy aHighlighterPolicy)
        {
            Show(aRectangle, aTag, elementType, aHighlighterPolicy, aGuiType, anIsExeplorer, 0);
        }

        /// <summary>
        /// 显示高亮框 全参数
        /// </summary>
        /// <param name="aRectangle"></param>
        /// <param name="aTag"></param>
        /// <param name="elementType"></param>
        /// <param name="aDurationInMs"></param>
        public void Show(Rectangle aRectangle,
            string aTag,
            ElementType elementType,
            HighlightPolicy aHighlighterPolicy,
            eGuiType aGuiType,
            bool anIsExeplorer,
            int aDurationInMs, HighLighterFlashParam _HighLighterFlashParam = null)
        {
            if (m_Disabled || aRectangle.IsEmpty)
            {
                return;
            }
            //logger.LogDebug($"测试调用 m_Dispatcher.Invoke()");

            //m_Dispatcher.Invoke(delegate ()

            //TimeTrackHelper timeTrackHelper = new TimeTrackHelper();
            //timeTrackHelper.Start("HighLight");
            
            m_Dispatcher.BeginInvoke((Action)delegate ()
            {
                {
                    //logger.LogDebug($"进入m_Dispatcher.Invoke()方法体");
                    if (LastOriginRectangle == aRectangle && CurrentTag == aTag)
                    {
                        //logger.LogInformation("重复渲染 忽略");
                        RefreshCurrentEGuiType(aGuiType);
                        return;
                    }

                    TimeTrackHelper timeTrackHelperTemp = new TimeTrackHelper();
                    timeTrackHelperTemp.Start("RealHighLight",TimeTrackLogLevel.degbug);

                    //logger.LogDebug($"Rectangle:{aRectangle.X};{aRectangle.Y};{aRectangle.Width};{aRectangle.Height}");
                    try
                    {
                        m_ElementType = elementType;
                        if (aHighlighterPolicy == HighlightPolicy.ProcessEdit)
                        {
                            if (m_ElementType == ElementType.Ordinary)
                            {
                                switch (aGuiType)
                                {
                                    case eGuiType.Windows:
                                    case eGuiType.Java:
                                    case eGuiType.SAP:
                                    case eGuiType.WdtErp:
                                        m_TagIcon = WINDOWS_ICON;
                                        break;
                                    case eGuiType.IE:
                                    case eGuiType.Edge:
                                    case eGuiType.Chrome:
                                    case eGuiType.FireFox:
                                    case eGuiType.Sogou:
                                    case eGuiType.Web360se:
                                    case eGuiType.ZExplorer:
                                    case eGuiType.SuperBrowser:
                                    case eGuiType.Chromium:
                                        m_TagIcon = WEB_ICON;
                                        break;
                                    default:
                                        m_TagIcon = WINDOWS_ICON;
                                        break;
                                }
                                if (anIsExeplorer)
                                {
                                    m_TagIcon = EXEPLORE_ICON;
                                }
                                m_HighlightBorderBrush = NativeBorderBrush;
                                m_HighlightTagBrush = NativeTagBrush;
                            }
                            else
                            {
                                m_HighlightBorderBrush = NativeBorderBrush;
                                m_HighlightTagBrush = NativeTagBrush;
                                m_TagIcon = CV_ICON;
                            }
                        }
                        else
                        {
                            m_HighlightTagBrush = InsteadTagBrush;
                            m_TagIcon = INSTEAD_ICON;
                            if (m_ElementType == ElementType.Ordinary)
                            {
                                m_HighlightBorderBrush = NativeBorderBrush;
                            }
                            else
                            {
                                m_HighlightBorderBrush = NativeTagBrush;
                            }
                        }
                        m_HighlightTagBrush.Freeze();
                        m_HighlightBorderBrush.Freeze();
                        //CloseInternal();
                        if (m_BoderWindow == null)
                        {
                            logger.LogDebug("创建 高亮框开始");
                            m_BoderWindow = new HighlightBorder(aRectangle,
                                m_HighlightBorderBrush,
                                m_IsWindowTransparent,
                                m_IsBackgroundTransparent,
                                aGuiType,
                                aDurationInMs);
                            m_BoderWindow.Show();

                            logger.LogDebug("创建 高亮框结束");
                        }
                        else
                        {
                            //logger.LogDebug("移动 高亮框开始");
                            m_BoderWindow.SetCurrentElementRectBorder(aRectangle,
                               m_HighlightBorderBrush,
                               m_IsWindowTransparent,
                               m_IsBackgroundTransparent,
                               aGuiType,
                               aDurationInMs);

                            //logger.LogDebug("移动 高亮框结束");
                        }

                        if (m_TagWindow == null)
                        {
                            //logger.LogDebug("创建 高亮标签开始");
                            m_TagWindow = new HighlightTag(aRectangle, aTag, m_TagIcon, m_HighlightTagBrush);
                            m_TagWindow.Show();
                            //logger.LogDebug("创建 高亮标签结束");
                        }
                        else
                        {
                            //logger.LogDebug("移动 高亮标签开始");
                            m_TagWindow.SetHighlightTag(aRectangle, aTag, m_TagIcon, m_HighlightTagBrush);

                            //logger.LogDebug("移动 高亮标签结束");
                        }
                        //m_BoderWindow.SetWindowTransparent(true);
                        //m_TagWindow.SetWindowTransparent(true);

                        //m_BoderWindow.Show();
                        //m_TagWindow.Show();

                        //m_BoderWindow.SetWindowTransparent(true);
                        //m_TagWindow.SetWindowTransparent();

                        LastOriginRectangle = aRectangle;
                        CurrentHightlightRectangle = HighlightUtils.ResizeRetangle(aRectangle, 3);
                        CurrentTag = aTag;

                        var _CurrentTagRectangle = new Rectangle((int)m_TagWindow.Left, (int)m_TagWindow.Top, (int)m_TagWindow.DesiredSize.Width + 1, (int)m_TagWindow.ActualHeight + 1); ;
                        CurrentTagRectangle = HighlightUtils.UpScale(_CurrentTagRectangle, m_TagWindow.TransformToPixels(m_TagWindow).X / 96.0);
                        RefreshCurrentEGuiType(aGuiType);
                        //StartCloseTimer(TimeSpan.FromMilliseconds(5000));
                    }
                    catch (Exception ex)
                    {
                        CloseInternal();
                        logger.LogError(ex);
                        return;
                    }
                    finally
                    {
                        timeTrackHelperTemp.Pick("RealHighLight", TimeTrackLogLevel.degbug);
                        timeTrackHelperTemp.Stop("RealHighLight", TimeTrackLogLevel.degbug);
                    }
                }
            });

            //timeTrackHelper.Pick("HighLight");
            //timeTrackHelper.Stop("HighLight");
        }

        public void SimpleShow(Rectangle aRectangle)
        {
            m_Dispatcher.BeginInvoke((Action)delegate ()
            {
                if (m_SimpleBoderWindow == null)
                {
                    m_SimpleBoderWindow = new HighlightBorder(aRectangle,
                                    m_HighlightBorderBrush,
                                    m_IsWindowTransparent,
                                    m_IsBackgroundTransparent,
                                    eGuiType.None);
                    m_SimpleBoderWindow.Show();
                }
                else
                {
                    m_SimpleBoderWindow.SetCurrentElementRectBorderSimple(aRectangle);
                }

                LastOriginRectangle = aRectangle;
                CurrentHightlightRectangle = HighlightUtils.ResizeRetangle(aRectangle, 3);
            });
        }


        public void RefreshCurrentEGuiType(eGuiType aGuiType)
        {
            m_CurrentGuiType = aGuiType;
        }

        /// <summary>
        /// 置顶
        /// </summary>
        /// <param name="aGuiType"></param>
        public void SetTopmost(eGuiType aGuiType)
        {
            m_Dispatcher.Invoke(new Action(() => 
            {
                //logger.LogInformation($"置顶SetTopmost{aGuiType.ToString()}");

                if (m_SimpleBoderWindow != null)
                {
                    m_SimpleBoderWindow.Topmost = true;
                    //if (aGuiType == eGuiType.WdtErp)
                    {
                        m_SimpleBoderWindow.SetTopMost();
                    }
                }

                if (m_BoderWindow != null)
                {
                    m_BoderWindow.Topmost = true;
                    //if (aGuiType == eGuiType.WdtErp)
                    {
                        m_BoderWindow.SetTopMost();
                    }
                }
                //Thread.Sleep(50);

                if (m_TagWindow != null)
                {
                    m_TagWindow.Topmost = true;
                    //if (aGuiType == eGuiType.WdtErp)
                    {
                        //logger.LogInformation("进行tag置顶操作");
                        m_TagWindow.SetTopMost();
                    }
                }
            }));
        }


        public void ShowElementFlash(ElementHighLighterParam _ElementHighLighterParam)
        {
            if (!_ElementHighLighterParam.CHighLighterFlashParam.IsFlashEnable
                || _ElementHighLighterParam.CHighLighterFlashParam.FlashTimes < 1
                || _ElementHighLighterParam.CRectangle.IsEmpty)
            {
                return;
            }

            m_Dispatcher.Invoke(delegate ()
            {
                try
                {
                    for (int i=0; i < _ElementHighLighterParam.CHighLighterFlashParam.FlashTimes; i++)
                    {
                        FlashOneTime(_ElementHighLighterParam);
                        Thread.Sleep(_ElementHighLighterParam.CHighLighterFlashParam.FlashInterval);
                    }
                }
                catch (Exception ex)
                {
                    CloseInternal();
                    logger.LogError(ex);
                    return;
                }

            });
        }

        private void FlashOneTime(ElementHighLighterParam _ElementHighLighterParam)
        {
            m_HighlightBorderBrush = NativeBorderBrush;
            m_HighlightBorderBrush.Freeze();
            CloseInternal();
            m_BoderWindow = new HighlightBorder(_ElementHighLighterParam.CRectangle,
                      m_HighlightBorderBrush,
                      true,
                      true,
                      eGuiType.None,
                      0);
            m_BoderWindow.Show();
            CurrentHightlightRectangle = _ElementHighLighterParam.CRectangle;
            Thread.Sleep(_ElementHighLighterParam.CHighLighterFlashParam.FlashInterval);
            CloseInternal();
        }

        public void SetBusy(bool isBusy)
        {
            m_Dispatcher.Invoke(delegate
            {
                m_TagWindow?.SetBusy(isBusy);
            });
        }
        /// <summary>
        /// 关闭高亮框
        /// </summary>
        public void Close(bool IsFinalStop = false)
        {
            if (IsFinalStop)
            {
                DisposeTopmostDispatcher();
            }
            
            m_Dispatcher.InvokeAsync(() =>
            {
                CloseInternal();
            });
        }

        /// <summary>
        /// 关闭高亮框
        /// </summary>
        public void Close(HightLightWndHideParam hightLightWndHideParam)
        {
            if (hightLightWndHideParam.IsFinalStop)
            {
                DisposeTopmostDispatcher();
            }

            if (hightLightWndHideParam.IsAsync)
            {
                m_Dispatcher.InvokeAsync(() =>
                {
                    CloseInternal();
                });
            }
            else
            {
                m_Dispatcher.Invoke(() =>
                {
                    CloseInternal();
                });
            }
        }

        private void CloseInternal()
        {
            StopCloseTimer();
            m_SimpleBoderWindow?.Close();
            m_BoderWindow?.Close();
            m_TagWindow?.Close();
            m_SimpleBoderWindow = null;
            m_BoderWindow = null;
            m_TagWindow = null;
            CurrentHightlightRectangle = Rectangle.Empty;
            m_CurrentTag = string.Empty;
        }

        public void RegisterActionOverEventHandler(Action anAction)
        {
            m_Dispatcher.Invoke(() =>
            {
                if (m_BoderWindow != null)
                {
                    m_BoderWindow.ActionOverEvent = anAction;
                }
            });
        }
        #region 此二方法仅用于截图时 隐藏和取消隐藏高亮框
        public void Hide()
        {
            m_Dispatcher.Invoke(delegate ()
            {
                if (m_SimpleBoderWindow != null)
                {
                    m_SimpleBoderWindow.Visibility = Visibility.Hidden;
                }
                if (m_BoderWindow != null)
                {
                    m_BoderWindow.Visibility = Visibility.Hidden;
                }
                if (m_TagWindow != null)
                {
                    m_TagWindow.Visibility = Visibility.Hidden;
                }
                m_Disabled = true;
            });
        }

        public void UndoHide()
        {
            m_Dispatcher.Invoke(delegate ()
            {
                if (m_SimpleBoderWindow != null)
                {
                    m_SimpleBoderWindow.Visibility = Visibility.Visible;
                }
                if (m_BoderWindow != null)
                {
                    m_BoderWindow.Visibility = Visibility.Visible;
                }
                if (m_TagWindow != null)
                {
                    m_TagWindow.Visibility = Visibility.Visible;
                }
                m_Disabled = false;
            });
        }
        #endregion
        public void StartAILoading()
        {
            m_Dispatcher.Invoke(delegate ()
            {
                if (m_ElementType == ElementType.Ordinary && m_TagIcon != EXEPLORE_ICON)
                {
                    if (m_BoderWindow != null)
                    {
                        m_BoderWindow.StartAILoading();
                        m_TagWindow?.SetBusy(true, eCommonMessageType.ElementHighlighter_cs_AIFenXi.GetMessage() );
                    }
                }
            });
        }
        public void StopAILoading()
        {
            m_Dispatcher.Invoke(delegate ()
            {
                if (m_BoderWindow != null)
                {
                    m_BoderWindow.StopAILoading();
                }
            });
        }
        private void StartCloseTimer(TimeSpan closeTimeout)
        {
            m_Timer = new DispatcherTimer
            {
                Interval = closeTimeout
            };
            m_Timer.Tick += TimerTick;
            m_Timer.Start();
        }
        private void StopCloseTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer.Tick -= TimerTick;
            }
        }
        private void TimerTick(object sender, EventArgs e)
        {
            Close();
        }
    }
}
