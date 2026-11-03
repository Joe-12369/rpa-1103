using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.ProcessCreator.ViewModel;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementTrack
{
    public class ElementRectHighlighter
    {
        private System.Windows.Media.Color m_HighlightColor
            = System.Windows.Media.Color.FromArgb(255, 234, 141, 44);
        private readonly ManualResetEventSlim m_StartedEvent = new ManualResetEventSlim(false);
        private readonly Thread m_UiThread;
        private Rectangle m_CurrRect;
        private bool m_Disabled;
        private bool m_IsWindowTransparent = true;
        private bool m_IsBackgroundTransparent = true;
        private Dispatcher m_Dispatcher;
        private OverlayWindow m_CurrOverlayWindow;

        public ElementRectHighlighter(OverlayWindow anOverlayWindow)
        {

            m_UiThread = new Thread(delegate ()
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                m_Dispatcher = Dispatcher.CurrentDispatcher;
                m_CurrOverlayWindow = anOverlayWindow;
                m_CurrOverlayWindow.Show();
                m_StartedEvent.Set();
                Dispatcher.Run();
            });
            m_UiThread.SetApartmentState(ApartmentState.STA);
            m_UiThread.IsBackground = true;
            m_UiThread.Start();
            m_StartedEvent.Wait();
        }
        /// <summary>
        /// tag rect
        /// </summary>
        public Rectangle TagRect
        {
            get;
            private set;
        }
        /// <summary>
        /// 设置window 穿透/不穿透
        /// </summary>
        /// <param name="anIsTranparent"></param>
        public void SetWindowTransparent(bool anIsTranparent)
        {
            m_Dispatcher.InvokeAsync(() =>
            {
                m_IsWindowTransparent = anIsTranparent;
            });
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
            });
        }

        /// <summary>
        /// dispose 结束dispatcher
        /// </summary>
        public void Dispose()
        {
            m_Dispatcher.Invoke(() =>
            {
                m_CurrOverlayWindow?.Close();
            });
            m_Dispatcher.InvokeShutdown();
            m_UiThread.Join(1000);
        }

        /// <summary>
        /// 暂停显示高亮框
        /// </summary>
        public void Pause()
        {
            m_Dispatcher.InvokeAsync(() =>
            {
                m_Disabled = true;
            });
        }
        /// <summary>
        /// 暂停显示高亮框
        /// </summary>
        public void PauseOnly()
        {
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

        /// <summary>
        /// 备用重载
        /// </summary>
        /// <param name="aRectangle"></param>
        public void Show(Rectangle aRectangle)
        {
            Show(aRectangle, string.Empty, 0);
        }

        /// <summary>
        /// 备用重载
        /// </summary>
        /// <param name="aRectangle"></param>
        /// <param name="aTag"></param>
        public void Show(Rectangle aRectangle, string aTag)
        {
            Show(aRectangle, aTag, 0);
        }

        /// <summary>
        /// 显示高亮框 重载
        /// </summary>
        /// <param name="aRectangle"></param>
        /// <param name="aTag"></param>
        /// <param name="elementType"></param>
        public void Show(Rectangle aRectangle, string aTag, ElementType elementType)
        {
            Show(aRectangle, aTag, elementType, 0);
        }

        /// <summary>
        /// 显示高亮框 全参数
        /// </summary>
        /// <param name="aRectangle"></param>
        /// <param name="aTag"></param>
        /// <param name="elementType"></param>
        /// <param name="aDurationInMs"></param>
        public void Show(Rectangle aRectangle, string aTag, ElementType elementType, int aDurationInMs)
        {
            if (m_Disabled)
            {
                return;
            }
            if (aRectangle.IsEmpty)
            {
                return;
            }
            m_Dispatcher.Invoke(delegate ()
            {
                {
                    try
                    {
                        if (elementType == ElementType.Ordinary)
                        {
                            m_HighlightColor = System.Windows.Media.Color.FromArgb(255, 255, 137, 16);
                        }
                        else
                        {
                            m_HighlightColor = System.Windows.Media.Color.FromArgb(255, 0, 133, 255);
                        }
                        m_CurrOverlayWindow.ShowHighLightRect(aRectangle, aTag, m_HighlightColor);
                        //TagRect = m_CurrOverlayWindow.TagRect;
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
            });
        }

        /// <summary>
        /// 关闭高亮框
        /// </summary>
        public void Close()
        {
            m_Dispatcher.InvokeAsync(delegate ()
            {
                m_CurrOverlayWindow?.StopHighLightRect();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="anActionWindow"></param>
        public void ShowActionWindow(ActionViewModelBase anActionViewModel)
        {
            m_Dispatcher.InvokeAsync(() =>
            {
                m_CurrOverlayWindow?.ShowActionWindow(anActionViewModel);
            });
        }

        public void RegisterActionOverEventHandler(Action anAction)
        {
            m_Dispatcher.InvokeAsync(delegate ()
            {

            });
        }
        public void LongHover()
        {
            m_Dispatcher.Invoke(async () =>
            {

            });
        }
        public void CloseContextMenu()
        {
            m_Dispatcher.InvokeAsync(async () =>
            {

            });
        }
        #region 此二方法仅用于截图时 隐藏和取消隐藏高亮框
        public void Hide()
        {
            m_Dispatcher.Invoke(delegate ()
            {
                m_CurrOverlayWindow.Visibility = Visibility.Hidden;
            });
        }

        public void UndoHide()
        {
            m_Dispatcher.Invoke(delegate ()
            {
                m_CurrOverlayWindow.Visibility = Visibility.Visible;
            });
        }
        #endregion
    }
}
