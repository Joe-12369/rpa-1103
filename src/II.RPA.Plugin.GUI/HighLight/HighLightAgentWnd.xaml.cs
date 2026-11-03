using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.ElementHighlight;
using II.RPA.Plugin.GUI.HighLight.ViewModels;
using II.RPA.Plugin.GUI.ProcessCreator.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Plugin.GUI.Params;

namespace II.RPA.Plugin.GUI.HighLight
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class HighLightAgentWnd : Window, IHighlightWindow
    {
        private static ILogger logger = Logger.OfContext();
        HLAgentWndViewModel CHLWndViewModel;
        public HighLightAgentWnd()
        {
            InitializeComponent();
            initData();
            this.DataContext = CHLWndViewModel;
        }

        public void initData()
        {
            CHLWndViewModel = new HLAgentWndViewModel();
            CHLWndViewModel.TagIconCode = HighLightIconTraitor.WINDOWS_ICON;//默认高亮标签图标为windows 图标要改成异步刷新 避免初始图标为空显示变成方块
            LayOutAdjust(true);
        }

        public const uint WS_EX_TRANSPARENT = 0x20;
        public static readonly int GWL_EXSTYLE = -20; // Retrieves the extended window styles.

        //[DllImport("user32", EntryPoint = "SetWindowLong")]
        //public static extern uint SetWindowLong(
        //IntPtr hwnd,
        //int nIndex,
        //uint dwNewLong
        //);
        //[DllImport("user32", EntryPoint = "GetWindowLong")]

        //public static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

        /// <summary>
        /// 是否上下颠倒
        /// </summary>
        bool IsUpDownNormal { get; set; } = false;
        SolidColorBrush NativeBorderColor_NormalEle = null;
        SolidColorBrush NativeBorderColor_CVEle = null;
        public void LayOutAdjust(bool _IsUpDownNormal, bool _ElementExistFlash = false)
        {
            if (_ElementExistFlash)
            {
                this.CHLWndViewModel.TagRowID = 0;
                this.CHLWndViewModel.BorderRowID = 0;
                this.CHLWndViewModel.TagPanelVisibility = Visibility.Collapsed;
                return;
            }

            if (_IsUpDownNormal)
            {
                this.CHLWndViewModel.TagRowID = 0;
                this.CHLWndViewModel.BorderRowID = 1;
                this.CHLWndViewModel.ToolsRowID = 2;
            }
            else
            {
                this.CHLWndViewModel.TagRowID = 1;
                this.CHLWndViewModel.BorderRowID = 0;
            }
            IsUpDownNormal = _IsUpDownNormal;
        }

        public void SetTagContent(string strTagContent = "")
        {
            if (this.CHLWndViewModel.TagContent == strTagContent)
            {
                return;
            }

            if (string.IsNullOrEmpty(strTagContent))
            {
                this.CHLWndViewModel.TagContentVisibility = Visibility.Collapsed;
            }
            else
            {
                if (this.CHLWndViewModel.TagContentVisibility != Visibility.Visible)
                {
                    this.CHLWndViewModel.TagContentVisibility = Visibility.Visible;
                }
            }
            this.CHLWndViewModel.TagContent = strTagContent;
        }

        public void SetTagIconCode(string strIconString)
        {
            if (this.CHLWndViewModel.TagIconCode == strIconString)
            {
                return;
            }
            this.CHLWndViewModel.TagIconCode = strIconString;
        }
        public void SetBorderBorderBrush(ElementType _ElementType)
        {
            SolidColorBrush scb;
            if (_ElementType == ElementType.Intelligence)
            {
                scb = NativeBorderColor_CVEle ?? (NativeBorderColor_CVEle = (SolidColorBrush)this.FindResource("NativeBorderColor_Blue"));
            }
            else
            {
                scb = NativeBorderColor_NormalEle ?? (NativeBorderColor_NormalEle = (SolidColorBrush)this.FindResource("NativeBorderColor"));
            }
            if (this.CHLWndViewModel.BorderBorderBrush == scb)
            {
                return;
            }
            this.CHLWndViewModel.BorderBorderBrush = scb;
        }
        System.Drawing.Rectangle LastRectangle = new Rectangle();

        private readonly int ConstTagHeight = 26;

        /// <summary>
        /// 调整窗体位置
        /// </summary>
        /// <param name="CRectangle"></param>
        /// <param name="delta"></param>
        /// <param name="_ElementExistFlash"></param>
        public void SetPosionAndBorderSize(Rectangle CRectangle, bool _ElementExistFlash = false, int delta = 1)
        {
            if (LastRectangle.Equals(CRectangle))
            {
                return;
            }

            if (_ElementExistFlash)
            {
                LayOutAdjust(true, true);
                this.Top = CRectangle.Top;
                this.Left = CRectangle.Left;
                this.CHLWndViewModel.BorderWidth = CRectangle.Width + delta;
                this.CHLWndViewModel.BorderHeight = CRectangle.Height + delta;
                return;
            }

            if (CRectangle.Top < 40)
            {
                LayOutAdjust(false);
            }
            else
            {
                if (!IsUpDownNormal)
                {
                    LayOutAdjust(true);
                }
            }

            if (IsUpDownNormal)
            {
                this.Top = CRectangle.Top - ConstTagHeight + delta;
                this.Left = CRectangle.Left;
            }
            else
            {
                this.Top = CRectangle.Top;
                this.Left = CRectangle.Left;
            }

            this.CHLWndViewModel.BorderWidth = CRectangle.Width + delta;
            this.CHLWndViewModel.BorderHeight = CRectangle.Height + delta;
            LastRectangle = CRectangle;
            RefreshToolsRectPoint();
        }
        //计算工具栏显示位置
        public void RefreshToolsRectPoint()
        {
            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;
            // 获取当前窗口的 DPI 缩放系数
            this.Dispatcher.Invoke(() =>
            {
                PresentationSource source = PresentationSource.FromVisual(this); // this 是 Window
                if (source != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }
            });
            double toolBarHeight = 40; // 工具栏高度 + 预留间隙
            Thickness newMargin = new Thickness(0, 0, 0, 0);
            var targetRect = LastRectangle;
            var centerPoint = new System.Drawing.Point((int)(targetRect.Left + targetRect.Width / 2), (int)(targetRect.Top + targetRect.Height / 2));
            var screen = System.Windows.Forms.Screen.FromPoint(centerPoint);
            var workArea = screen.WorkingArea; // 获取工作区（排除任务栏）
            double screenRight = screen.Bounds.Width / dpiScaleX;
            double screenBottom = screen.Bounds.Height / dpiScaleY;

            bool bottomOverflow = (targetRect.Bottom + toolBarHeight) > screenBottom;
            //下边距
            if (bottomOverflow)
            {
                if (targetRect.Bottom + toolBarHeight > screenBottom)
                {
                    double pushUpOffset = targetRect.Bottom - screenBottom + toolBarHeight;
                    newMargin.Top = -pushUpOffset;
                    newMargin.Bottom = pushUpOffset;
                }
                else
                {
                    newMargin.Top = 5;
                }
            }
            else
            {
                newMargin.Top = 0;
            }
            // 右边距
            double rightOverflow = targetRect.Right - screenRight;
            if (rightOverflow > 0)
            {
                newMargin.Right = rightOverflow + 10;
            }
            else
            {
                newMargin.Right = 0;
            }
            this.CHLWndViewModel.ToolsMargin = newMargin;
        }

        public void RefreshStatus(HighLightParam _HighLightParam)
        {
            SetPosionAndBorderSize(_HighLightParam.m_Rectangle);
            SetTagIconCode(_HighLightParam.m_IconString);
            SetTagContent(_HighLightParam.m_TagContent);
        }

        public bool GetBorderRectAndTagRect(HighLightParam _HighLightParam, ref Rectangle BorderRect, ref Rectangle TagRect)
        {
            int delta = 1;
            BorderRect = HighlightUtils.ResizeRetangle(_HighLightParam.m_Rectangle, delta);
            //BorderRect = _HighLightParam.m_Rectangle;
            if (this.CHLWndViewModel.TagRowID == 0)
            {
                TagRect = new Rectangle((int)(this.Left + delta), (int)(this.Top), (int)this.ActualWidth, ConstTagHeight + 1);
            }
            else
            {
                TagRect = new Rectangle((int)(this.Left + delta), (int)(this.Top + this.ActualHeight - ConstTagHeight), (int)this.ActualWidth, ConstTagHeight + 1);
            }
            return true;
        }

        public void SetBackgroundActive(bool IsActive)
        {
            if (IsActive)
            {
                this.CHLWndViewModel.BorderBackground = new SolidColorBrush(Colors.Blue);
            }
            else
            {
                this.CHLWndViewModel.BorderBackground = new SolidColorBrush(Colors.Transparent);
            }
        }

        #region 设置窗体穿透
        /// <summary>
        /// 设置窗体的鼠标穿透效果
        /// </summary>
        /// <param name="IsActive"></param>
        public void SetWndPenetrate(bool IsActive)
        {
            SetPenetrate(IsActive);
        }

        IntPtr ThisWndHWnd = IntPtr.Zero;
        uint extendedStyle = 0;
        /// <summary>
        /// 穿透
        /// </summary>
        /// <param name="transparent"></param>
        private void SetPenetrate(bool IsPenetrate)
        {
            logger.LogDebug("设置高亮窗是否穿透");
            if (ThisWndHWnd == IntPtr.Zero)
            {
                ThisWndHWnd = new WindowInteropHelper(this).Handle;
                extendedStyle = NativeMethods.GetWindowLong(ThisWndHWnd, GWL_EXSTYLE);
            }

            if (IsPenetrate)
            {
                //SetBackgroundActive(false);
                NativeMethods.SetWindowLong(ThisWndHWnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
                logger.LogDebug("设置高亮窗穿透");
            }
            else
            {
                //SetBackgroundActive(true);
                NativeMethods.SetWindowLong(ThisWndHWnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
                logger.LogDebug("设置高亮窗不穿透");
            }
        }

        #endregion

        public void SetWndState(bool IsActive)
        {
            if (IsActive)
            {
                this.Visibility = Visibility.Visible;
            }
            else
            {
                this.Visibility = Visibility.Hidden;
            }
        }

        IntPtr wndHandle;
        public void SetTopMost()
        {
            NativeMethods.SetWindowPos(wndHandle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE);
        }

        private void HighlightBorder_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            wndHandle = wndHelper.Handle;
            SetPenetrate(true);
        }

        private void HighlightBorder_Deactivated(object sender, EventArgs e)
        {
            SetTopMost();
        }


        #region LineScanStory

        Storyboard LineScanStory = new Storyboard();

        public void StartAILoading()
        {
            //if (m_GuiType == eGuiType.Windows)
            //{

            //}

            //仅在windows元素类型下才需要使用cv元素
            //if (InnerPanel.Children == null || InnerPanel.Children.Count == 0)
            //{
            //    ScanAnimationControl scanAnimationControl = new ScanAnimationControl(this.CHLWndViewModel.BorderWidth, this.CHLWndViewModel.BorderHeight);
            //    InnerPanel.Children.Add(scanAnimationControl);
            //}

            BeginStory();
        }

        public void StopAILoading()
        {
            //InnerPanel?.Children?.Clear();
            StopStory();
        }

        public void BeginStory()
        {
            //if (this.CHLWndViewModel.BorderRowID == 1)
            //{
            //    this.LineScan.RenderTransform = new TranslateTransform(0,30);
            //}
            //else
            //{
            //    this.LineScan.RenderTransform = new TranslateTransform(0,0);
            //}

            LineScan.RenderTransform = new TranslateTransform();
            DoubleAnimation xAnimation = new DoubleAnimation
            {
                From = 0 - this.CHLWndViewModel.BorderHeight,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(1.5))
            };

            DependencyProperty[] propertyChain = new DependencyProperty[]
            {
                System.Windows.Shapes.Line.RenderTransformProperty,
                TranslateTransform.YProperty
            };
            LineScanStory.AutoReverse = false;
            LineScanStory.RepeatBehavior = RepeatBehavior.Forever;
            LineScanStory.Children.Clear();
            LineScanStory.Children.Add(xAnimation);

            Storyboard.SetTargetName(xAnimation, "LineScan");
            Storyboard.SetTargetProperty(xAnimation, new PropertyPath("(0).(1)", propertyChain));

            LineScan.Visibility = Visibility.Visible;

            LineScanStory.Begin(LineScan);//.Begin(LineScan);
        }

        public void StopStory()
        {
            LineScan.Visibility = Visibility.Hidden;
            LineScanStory?.Stop();
            LineScanStory?.Children.Clear();
        }

        #endregion

        #region Agent拾取逻辑
        /// <summary>
        /// 选择Agent基准元素后功能栏区域显示
        /// </summary>
        public void SetToolPanelVisible()
        {
            SetPenetrate(false);
            //this.CHLWndViewModel.TagPanelVisibility = Visibility.Hidden;
            this.CHLWndViewModel.ToolPanelVisibility = Visibility.Visible;
        }
        /// <summary>
        /// 查找父元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchParentElement_Click(object sender, MouseButtonEventArgs e)
        {
            GuiElement.OnSelectedParentElement?.Invoke();
        }
        /// <summary>
        /// 查找子元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchChildElement_Click(object sender, MouseButtonEventArgs e)
        {
            GuiElement.OnSelectedChildElement?.Invoke();
        }
        /// <summary>
        /// 取消拾取结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelCaptureElement_Click(object sender, MouseButtonEventArgs e)
        {
            this.CHLWndViewModel.ToolPanelVisibility = Visibility.Hidden;
            GuiElement.CancelAgentCaptureElement?.Invoke();
        }
        /// <summary>
        /// 确认拾取结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfirmCaptureElement_Click(object sender, MouseButtonEventArgs e)
        {
            GuiElement.ConfirmAgentCaptureElement?.Invoke();
        }

        public void SetZoomControlsShowStatus(bool isEnbale)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.SearchParent_Control.IsEnabled = isEnbale;
                this.SearchChild_Control.IsEnabled = isEnbale;

                SearchParent_Icon.Fill = isEnbale? (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#161923") : (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#55161923");
                SearchChild_Icon.Fill = isEnbale ? (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#161923") : (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#55161923");
            }));
        }

        #endregion
    }
}
