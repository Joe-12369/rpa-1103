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
using II.RPA.Plugin.GUI.DataCollection.V2.ViewModels;
using II.RPA.Plugin.GUI.HighLight;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using SharpVectors.Dom;
using static II.RPA.Plugin.GUI.ElementHighlight.HighlightUtils;
using System.Management;
using Gma.UserActivityMonitor;
using System.Windows.Forms;
using System.Diagnostics;
using II.RPA.Logging.Formatting;
using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Windows.ViewModels;
using II.RPA.Plugin.GUI.Tasks;

namespace II.RPA.Plugin.GUI.Windows
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WindowHighLightWnd : Window
    {
        private readonly int ConstTagHeight = 26;
        private static readonly object doSelect_objLock = new object();
        private static ILogger logger = Logger.OfContext();
        WindowHighLightViewModel WindowHLWndViewModel;
        ElementInfoDealHelper CurrentElementInfoDealHelper;
        ParamCaptureWindowModel CaptureParam { get; set; }
        internal Func<IGuiNode, ParamCaptureWindowModel, string> DoSelectAction = null;
        internal Action CancelDoSelectAction = null;
        private LastHighlightElementInfo latHighlightElementInfo = null;
        private bool hasDoSelect { get; set; } = false;
        public string WindowName { get; set; } = "";
        //System.Drawing.Rectangle LastHighlightElementInfo = new Rectangle();
        //private IGuiNode lastHighlightElement;
        /// <summary>
        /// 当前正处于高亮状态的元素
        /// </summary>
        internal IGuiNode currentHighlightElement;
        internal IGuiNode CurrentHighlightElement
        {
            get
            {
                return currentHighlightElement;
            }
            set
            {
                currentHighlightElement = value;
                logger.LogDebug($"Set CurrentHighlightElement");
                RefreshHighLight();
            }
        }
        internal Exception CaptureException { get; set; } = null;

        #region 窗口初始化

        public WindowHighLightWnd()
        {
            logger.LogDebug($"DataCollectHighLightWnd Ctor");
            InitializeComponent();
            this.SourceInitialized += DataCollectHighLightWnd_SourceInitialized;
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            initData();
            this.DataContext = WindowHLWndViewModel;
            CurrentElementInfoDealHelper = new ElementInfoDealHelper();
            logger.LogDebug($"DataCollectHighLightWnd Ctor end");
        }
        public void initData()
        {
            WindowHLWndViewModel = new WindowHighLightViewModel();
            LayOutAdjust(true);
            latHighlightElementInfo = new LastHighlightElementInfo();
        }
        public void DataCollectHighLightWnd_SourceInitialized(object sender, EventArgs e)
        {
            logger.LogDebug($"DataCollectHighLightWnd SourceInitialized");
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
            SetPenetrate(true);
            logger.LogDebug($"DataCollectHighLightWnd SourceInitialized end");
        }

        #endregion

        #region Ctrl 键按下状态
        private bool CtrlKeyDown
        {
            get
            {
                return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            }
        }
        #endregion

        #region 鼠标点击的时间间隔
        private long lastClickTime = Stopwatch.GetTimestamp();
        private long ClickTimestamp
        {
            get
            {
                var lastClickTime_temp = lastClickTime;
                lastClickTime = Stopwatch.GetTimestamp();
                return (long)((lastClickTime - lastClickTime_temp) * 1000D / Stopwatch.Frequency);
            }
        }
        #endregion

        #region 开始拾取
        internal void StartCapture(ParamCaptureWindowModel captureParam)
        {
            //SuspendCapture();
            CaptureParam = captureParam;
            hasDoSelect = false;
            //CaptureError = string.Empty;
            //SelectorError = string.Empty;
            CurrentHighlightElement = null;
            CaptureException = null;
            factor = -1;
            this.latHighlightElementInfo.UserDefinedID = string.Empty;//重置最后一个高亮元素
            InstallHook();
        }
        #endregion

        #region 键盘、鼠标勾子事件 注册卸载

        internal void InstallHook()
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(() =>
                {
                    InstallHook();
                });
                return;
            }
            UninstallHook();
            HookManager.MouseClickExt += DataCollectHighLightWnd_MouseClickExt;
            HookManager.KeyDown += DataCollectHighLightWnd_KeyDown;
        }

        internal void UninstallHook()
        {
            try
            {
                HookManager.MouseClickExt -= DataCollectHighLightWnd_MouseClickExt;
                HookManager.KeyDown -= DataCollectHighLightWnd_KeyDown;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"UninstallHook 出错，可能是无需卸载事件.");
            }
        }
        private void DataCollectHighLightWnd_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Escape)
            {
                UninstallHook();
                WindowCaptureTask.Instance.StopAfterExecute();
                CancelDoSelectAction?.Invoke();
            }
        }

        #endregion

        #region 鼠标单击事件

        private void DataCollectHighLightWnd_MouseClickExt(object sender, MouseEventExtArgs e)
        {
            try
            {
                //MouseLastClicPoint = Cursor.Position;
                //logger.LogDebug("enter_MouseClickExt ");
                if (CurrentHighlightElement == null)
                {
                    return;
                }
                if (this.ClickTimestamp < 200)
                {
                    e.Handled = this.CtrlKeyDown;
                    return;
                }
                switch (e.Button)
                {
                    case MouseButtons.Left:
                        if (!this.CtrlKeyDown)
                        {
                            e.Handled = false;
                            return;
                        }
                        logger.LogDebug("MouseClickExt——MouseButtons.Left");
                        logger.LogDebug("MouseClickExt——Keys.Control");
                        logger.LogDebug("MouseClickExt——start DoSelect Task.Run");
                        SuspendCapture();
                        //DataCollectingManager.ShowLoadWaiting(true);
                        lock (doSelect_objLock)
                        {
                            if (hasDoSelect)
                            {
                                return;
                            }
                            hasDoSelect = true;
                        }
                        _ = Task.Run(() => DoSelectAction?.Invoke(this.CurrentHighlightElement, CaptureParam));
                        logger.LogDebug("MouseClickExt——end DoSelect Task.Run");
                        e.Handled = true;
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "失去过程中 鼠标点击发生了异常");
            }
        }

        #endregion

        #region 暂停拾取动作
        /// <summary>
        /// 暂停拾取动作
        /// </summary>
        internal void SuspendCapture()
        {
            try
            {
                UninstallHook();
                WindowCaptureTask.Instance.StopAfterExecute();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "StopHighlight Exception");
            }
        }
        #endregion

        #region 元素拾取

        #endregion

        #region 刷新高亮框


        public async void RefreshHighLight()
        {
            await Task.Run(() =>
            {
                try
                {

                    var guiNode = this.CurrentHighlightElement;
                    if (guiNode == null)
                    {
                        //this.Dispatcher.Invoke(() =>
                        //{
                        //    if (this.IsActive || this.Visibility == System.Windows.Visibility.Visible)
                        //    {
                        //        this.Hide();
                        //    }
                        //});
                        return;
                    }
                    SetBackgroundActive();
                    if (RPAUtils.TryGet(() => guiNode.UserDefinedID, out var userDefinedID))
                    {
                        if (!string.IsNullOrEmpty(userDefinedID) && userDefinedID.Equals(this.latHighlightElementInfo.UserDefinedID))
                        {
                            return;
                        }
                    }


                    var rect = guiNode.GetRegion(3);
                    var guiType = (guiNode as IGuiNode_6_1_0).GuiType;
                    var tagContent = GetElementTagContent(guiNode.GuiNodeControlName());
                    tagContent = WindowName;
                    HighLightParam CHighLightParam = new HighLightParam(rect, tagContent, Params.ElementType.Ordinary, guiType);
                    CHighLightParam.m_Rectangle = this.GetVisiualRect(CHighLightParam.m_Rectangle);
                    this.latHighlightElementInfo.SetProps(userDefinedID, rect, guiType, tagContent);
                    RefreshHighLightStyles(CHighLightParam);
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"数据采集渲染高亮框时发生异常，{ex},以为发生在拾取刷新过程中，将忽略本次异常");
                }
            });
        }

        private string GetElementTagContent(string controlType)
        {
            if (!string.IsNullOrEmpty(controlType))
            {
                string tagContent = II.RPA.WinAutomation.CommonUtility.TargrtXmlHelper.CreateInstance().GetTranslatedNameByCode(controlType);
                if (string.IsNullOrEmpty(tagContent))
                {
                    tagContent = controlType;
                }
                return tagContent;
            }
            else
            {
                return "其他";
            }
        }
        public void RefreshHighLightStyles(HighLightParam _HighLightParam)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (!WindowCaptureTask.Instance.IsStart)
                {
                    return;
                }
                if (!this.IsActive || this.Visibility == System.Windows.Visibility.Hidden)
                {
                    this.Show();
                }
                SetPosionAndBorderSize(_HighLightParam.m_Rectangle);
                //SetTagIconCode(_HighLightParam.m_IconString);
                SetTagContent(_HighLightParam.m_TagContent);
            });
        }
        /// <summary>
        /// 调整窗体位置
        /// </summary>
        /// <param name="CRectangle"></param>
        /// <param name="delta"></param>
        /// <param name="_ElementExistFlash"></param>
        public void SetPosionAndBorderSize(Rectangle CRectangle, int delta = 1)
        {
            //if (latHighlightElementInfo != null && latHighlightElementInfo.Rectangle.Equals(CRectangle))
            //{
            //    return;
            //}
            if (CRectangle.Top < 40)
            {
                LayOutAdjust(false);
            }
            else if (!IsUpDownNormal)
            {
                LayOutAdjust(true);
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

            this.WindowHLWndViewModel.BorderWidth = CRectangle.Width + delta;
            this.WindowHLWndViewModel.BorderHeight = CRectangle.Height + delta;

        }
        public void SetTagContent(string strTagContent = "")
        {
            if (this.WindowHLWndViewModel.TagContent == strTagContent)
            {
                return;
            }
            if (string.IsNullOrEmpty(strTagContent))
            {
                this.WindowHLWndViewModel.TagContentVisibility = Visibility.Collapsed;
            }
            else
            {
                if (this.WindowHLWndViewModel.TagContentVisibility != Visibility.Visible)
                {
                    this.WindowHLWndViewModel.TagContentVisibility = Visibility.Visible;
                }
            }
            this.WindowHLWndViewModel.TagContent = strTagContent;
        }

        public void SetTagIconCode(string strIconString)
        {
            if (this.WindowHLWndViewModel.TagIconCode == strIconString)
            {
                return;
            }
            this.WindowHLWndViewModel.TagIconCode = strIconString;
        }
        #endregion

        #region 纠正wpf区域大小
        private double factor = -1d;
        private Rectangle GetVisiualRect(Rectangle rect)
        {
            return this.Dispatcher.Invoke(() =>
            {
                if (factor < 0)
                {
                    factor = 0.0;
                    Dpi? _Dpi = TransformToPixels(this);
                    if (_Dpi != null)
                    {
                        factor = ((Dpi)_Dpi).X / 96.0;
                    }
                }
                if (factor != 0)
                {
                    return GetVisiualRect(rect, factor);
                }
                return rect;
            });
        }


        /// <summary>
        /// 修正提供的框体大小（抵消缩放影响）
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        private Rectangle GetVisiualRect(Rectangle rect, double factor)
        {
            Rectangle rectangle2 = rect.Downscale(factor);

            //logger.LogDebug($"校验 RePositionAndResize{rectangle2.ToJson()} 缩放{factor}");
            if ((double)(rectangle2.Width + 4) <= SystemParameters.WorkArea.Width)
            {
                rectangle2.Inflate(4, 0);
            }
            if ((double)(rectangle2.Height + 4) <= SystemParameters.WorkArea.Height)
            {
                rectangle2.Inflate(0, 4);
            }

            return rectangle2;
        }
        private Dpi TransformToPixels(Visual aVisual)
        {
            Matrix matrix;
            var source = PresentationSource.FromVisual(aVisual);
            if (source != null)
            {
                matrix = source.CompositionTarget.TransformToDevice;
            }
            else
            {
                using (var src = new HwndSource(new HwndSourceParameters()))
                {
                    matrix = src.CompositionTarget.TransformToDevice;
                }
            }

            double pixelX = (int)(matrix.M11 * 96.0);
            double pixelY = (int)(matrix.M22 * 96.0);
            return new Dpi(pixelX, pixelY);
        }

        #endregion

        #region 设置标签提示 和 高亮框的上下位置
        /// <summary>
        /// 是否上下颠倒
        /// </summary>
        bool IsUpDownNormal { get; set; } = false;
        public void LayOutAdjust(bool _IsUpDownNormal)
        {
            if (_IsUpDownNormal)
            {
                this.WindowHLWndViewModel.TagRowID = 0;
                this.WindowHLWndViewModel.BorderRowID = 1;
            }
            else
            {
                this.WindowHLWndViewModel.TagRowID = 1;
                this.WindowHLWndViewModel.BorderRowID = 0;
            }
            IsUpDownNormal = _IsUpDownNormal;
        }
        #endregion


        #region 设置高亮框背景颜色

        public void SetBackgroundActive()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (this.CtrlKeyDown)
                {
                    this.WindowHLWndViewModel.BorderBackground = new SolidColorBrush(Colors.Blue);
                }
                else
                {
                    this.WindowHLWndViewModel.BorderBackground = new SolidColorBrush(Colors.Transparent);
                }
            });
        }
        #endregion

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
                extendedStyle = NativeMethods.GetWindowLong(ThisWndHWnd, NativeMethods.GWL_EXSTYLE);
            }

            if (IsPenetrate)
            {
                //SetBackgroundActive(false);
                NativeMethods.SetWindowLong(ThisWndHWnd, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TRANSPARENT);
                logger.LogDebug("设置高亮窗穿透");
            }
            else
            {
                //SetBackgroundActive(true);
                NativeMethods.SetWindowLong(ThisWndHWnd, NativeMethods.GWL_EXSTYLE, extendedStyle & ~NativeMethods.WS_EX_TRANSPARENT);
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

        private void DataCollectHighLightWnd_Loaded(object sender, RoutedEventArgs e)
        {
            logger.LogDebug($"DataCollectHighLightWnd Loaded");
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            wndHandle = wndHelper.Handle;
            SetPenetrate(true);
            logger.LogDebug($"DataCollectHighLightWnd Loaded end");
        }

        private void DataCollectHighLightWnd_Deactivated(object sender, EventArgs e)
        {
            SetTopMost();
        }


    }

    public class LastHighlightElementInfo
    {
        public string UserDefinedID { get; set; } = string.Empty;
        public Rectangle Rectangle { get; set; } = Rectangle.Empty;
        public eGuiType GuiType { get; set; }
        public string TargetName { get; set; }

        public void SetProps(string userDefinedID, Rectangle rect, eGuiType guiType, string targetName)
        {
            this.UserDefinedID = userDefinedID;
            this.Rectangle = rect;
            this.GuiType = guiType;
            this.TargetName = targetName;
        }
    }
}

