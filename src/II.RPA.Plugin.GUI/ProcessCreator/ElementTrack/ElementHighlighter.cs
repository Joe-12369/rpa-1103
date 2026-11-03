using II.RPA.Logging;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.ProcessCreator.ActionProvider;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementTrack
{
    public class ElementHighlighter : IDisposable
    {
        private const string CV_BORDER_COLOR = "#4ED0FF";
        private const string NATIVE_BORDER_COLOR = "#FE5402";
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
        private ElementHighlightBorder m_BoderWindow;
        private bool m_Disabled;
        private bool m_IsWindowTransparent = true;
        private bool m_IsBackgroundTransparent = true;
        private Dispatcher m_Dispatcher;
        private ElementType m_ElementType;
        private ElementHighlightTag m_TagWindow;
        private DispatcherTimer m_Timer;

        private Rectangle m_CurrentRectangle;
        private string m_CurrentTag;

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
            //DummyShow();
        }
        public Window BorderWindow => m_BoderWindow;
        public Dispatcher HighLightDispatcher
        {
            get => m_Dispatcher;
        }
        public bool IsEnable
        {
            get => !m_Disabled;
        }
        public void CloseWindowWithPostMessage()
        {
            IntPtr borderhwnd_win;
            borderhwnd_win = FindWindow(null, nameof(ElementHighlightBorder));
            SendMessage(borderhwnd_win, WM_CLOSE, 0, 0);

            IntPtr taghwnd_win;
            taghwnd_win = FindWindow(null, nameof(ElementHighlightTag));
            SendMessage(taghwnd_win, WM_CLOSE, 0, 0);
        }

        /// <summary>
        /// 设置window背景色 透明/不透明
        /// </summary>
        /// <param name="anIsTranparent"></param>
        public void SetBackgroundTransparent(bool anIsTranparent)
        {
            m_Dispatcher.Invoke(() =>
            {
                m_IsBackgroundTransparent = anIsTranparent;
                m_BoderWindow?.SetBackgroundTransparent(m_IsBackgroundTransparent);           
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
                if (m_ElementType == ElementType.Ordinary)
                {
                    m_TagWindow?.SetTagText(aTag);
                }
                else 
                {
                    m_TagWindow?.SetAITagText(aTag);
                }
            });
        }
        public bool IsCapturedActionPopup()
        {
            return m_Dispatcher.Invoke(() =>
            {
                return m_BoderWindow != null && m_BoderWindow.IsPopupWindowCapture();
            });
        }
        /// <summary>
        /// dispose 结束dispatcher
        /// </summary>
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
            //logger.LogDebug("Pause");
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
            });
        }
        /// <summary>
        /// 显示高亮框 重载
        /// </summary>
        /// <param name="aRectangle"></param>
        /// <param name="aTag"></param>
        /// <param name="elementType"></param>
        public bool ShowWithResult(Rectangle aRectangle,
            string aTag,
            ElementType elementType,
            eGuiType aGuiType,
            bool anIsExeplorer,
            HighlighterPolicy aHighlighterPolicy)
        {
            return Show(aRectangle, aTag, elementType, aHighlighterPolicy, aGuiType, anIsExeplorer, 0,IntPtr.Zero);
        }
        public bool ShowWithResult(Rectangle aRectangle,
            string aTag,
            ElementType elementType,
            eGuiType aGuiType,
            bool anIsExeplorer,
            HighlighterPolicy aHighlighterPolicy,
            IntPtr anOwnerHandle)
        {
            return Show(aRectangle, aTag, elementType, aHighlighterPolicy, aGuiType, anIsExeplorer, 0, anOwnerHandle);
        }
        /// <summary>
        /// 显示高亮框 重载
        /// </summary>
        /// <param name="aRectangle"></param>
        /// <param name="aTag"></param>
        /// <param name="elementType"></param>
        //public void Show(Rectangle aRectangle, 
        //    string aTag, 
        //    ElementType elementType, 
        //    eGuiType aGuiType,
        //    bool anIsExeplorer,
        //    HighlighterPolicy aHighlighterPolicy)
        //{
        //    Show(aRectangle, aTag, elementType, aHighlighterPolicy, aGuiType, anIsExeplorer, 0);
        //}
        /// <summary>
        /// 显示高亮框 全参数
        /// </summary>
        /// <param name="aRectangle"></param>
        /// <param name="aTag"></param>
        /// <param name="elementType"></param>
        /// <param name="aDurationInMs"></param>
        public bool Show(Rectangle aRectangle, 
            string aTag, 
            ElementType elementType, 
            HighlighterPolicy aHighlighterPolicy, 
            eGuiType aGuiType,
            bool anIsExeplorer,
            int aDurationInMs,
            IntPtr aOwnHandle)
        {
            if (m_Disabled || aRectangle.IsEmpty)
            {
                return false;
            }
            bool isShown = false;
            m_Dispatcher.Invoke(delegate ()
            {
                if (m_CurrentRectangle == aRectangle && m_CurrentTag == aTag)
                {
                    m_BoderWindow.Topmost = true;
                    isShown = false;
                    return;
                }
                //logger.LogInformation($"Rectangle:{aRectangle.X};{aRectangle.Y};{aRectangle.Width};{aRectangle.Height}");
                try
                {
                    m_ElementType = elementType;
                    if (aHighlighterPolicy == HighlighterPolicy.ProcessEdit)
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
                                case eGuiType.Web360X:
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
                    //logger.LogDebug($"ElementHighlighter_aRectangle:{aRectangle.Y},{aRectangle.X},{aRectangle.Height},{aRectangle.Width}");
                    if (m_BoderWindow == null)
                    {
                        m_BoderWindow = new ElementHighlightBorder(aRectangle,
                            m_HighlightBorderBrush,
                            m_IsWindowTransparent,
                            m_IsBackgroundTransparent,
                            aGuiType,
                            aDurationInMs);
                        m_BoderWindow.Show();
                    }
                    else
                    {
                        m_BoderWindow.SetCurrentElementRectBorder(aRectangle,aGuiType);
                    }

                    if (m_TagWindow == null)
                    {
                        m_TagWindow = new ElementHighlightTag(aRectangle, aTag, m_TagIcon, m_HighlightTagBrush);
                        m_TagWindow.Show();
                    }
                    else
                    {
                        m_TagWindow.SetHighlightTag(aRectangle, aTag, m_TagIcon, m_HighlightTagBrush);
                    }
                    m_CurrentRectangle = aRectangle;
                    m_CurrentTag = aTag;
                    //TagRect = new Rectangle((int)m_TagWindow.Left, (int)m_TagWindow.Top, (int)m_TagWindow.ActualWidth, (int)m_TagWindow.ActualHeight);
                    //BorderRect = new Rectangle((int)m_BoderWindow.Left, (int)m_BoderWindow.Top, (int)m_BoderWindow.ActualWidth, (int)m_BoderWindow.ActualHeight);
                    //StartCloseTimer(TimeSpan.FromMilliseconds(5000));
                    isShown = true;
                }
                catch (Exception ex)
                {
                    CloseInternal();
                    logger.LogError(ex);
                    return;
                }
            });
            return isShown;
        }
        public void SetBusy(bool isBusy,string aText = "")
        {
            m_Dispatcher.Invoke(delegate
            {
                m_TagWindow?.SetBusy(isBusy, aText,m_ElementType == ElementType.Ordinary);
            });
        }
        /// <summary>
        /// 关闭高亮框
        /// </summary>
        public void Close()
        {
            m_Dispatcher.InvokeAsync(() =>
            {
                CloseInternal();
            });
        }

        private void CloseInternal()
        {
            StopCloseTimer();
            m_BoderWindow?.Close();
            m_TagWindow?.Close();
            m_BoderWindow = null;
            m_TagWindow = null;
            m_CurrentRectangle = Rectangle.Empty;
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
        public void ShowActionPopup(ActionViewModelBase actionViewModelBase,IntPtr anOwnerHandle)
        {
            m_Dispatcher.InvokeAsync(() =>
            {
                if (m_BoderWindow != null)
                {
                    m_BoderWindow.ShowHoverWindow(actionViewModelBase, anOwnerHandle);
                }
            });
        }
        public void ShowActionPopupWithoutOwner(ActionViewModelBase anActionViewModel)
        {
            m_Dispatcher.InvokeAsync(() =>
            {
                
            });
        }
        #region 此二方法仅用于截图时 隐藏和取消隐藏高亮框
        public void Hide()
        {
            m_Dispatcher.Invoke(delegate ()
            {
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
                        //m_TagWindow?.SetBusy(true, eCommonMessageType.ElementHighlighter_cs_AIFenXi.GetMessage(),m_ElementType == ElementType.Ordinary);
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
                    //m_TagWindow?.SetBusy(false);
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

        private void DummyShow()
        {
            m_Dispatcher.Invoke(() => 
            {
                if (m_BoderWindow == null)
                {
                    m_BoderWindow = new ElementHighlightBorder(Rectangle.Empty,
                        m_HighlightBorderBrush,
                        m_IsWindowTransparent,
                        m_IsBackgroundTransparent,
                        eGuiType.Windows,
                        -1);
                    m_BoderWindow.Show();
                }
                if (m_TagWindow == null)
                {
                    m_TagWindow = new ElementHighlightTag(Rectangle.Empty, "", m_TagIcon, m_HighlightTagBrush);
                    m_TagWindow.Show();
                }
            });
        }
    }
    public enum HighlighterPolicy
    {
        ProcessEdit = 0,
        ElementInstead = 1,
    }
}
