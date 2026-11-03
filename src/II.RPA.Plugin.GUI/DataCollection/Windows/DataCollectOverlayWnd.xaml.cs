using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using Gma.UserActivityMonitor;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Logging.Formatting;
using II.RPA.Plugin.GUI.ElementHighlight;

namespace II.RPA.Plugin.GUI
{
    /// <summary>
    /// DataCollectOverlayWnd.xaml 的交互逻辑
    /// </summary>
    public partial class DataCollectOverlayWnd : Window
    {
        #region 字段、属性 成员

        private string packagesPath = string.Empty;
        private static ILogger logger = Logger.OfContext();
        private DateTime lastClickTime;
        internal string CaptureSelector = null;
        internal string SelectorError { get; set; }
        ParamStartCollectElementCaptureModel CaptureParam { get; set; }
        internal RetElementCaptureModel RetCaptureModel { get; set; }
        internal string CaptureError { get; set; }
        internal Exception CaptureException { get; set; } = null;
        private bool HasDoSelect { get; set; }
        private bool IsUseTableElement { get; set; }
        private static readonly object doSelect_objLock = new object();
        private DispatcherTimer inspectDispatcherTimer = new DispatcherTimer();
        private DispatcherTimer moveTipsDispatcherTimer = new DispatcherTimer();
        internal Action<DataCollectOverlayWnd> Callback = null;
        private string _collectType = string.Empty;
        /// <summary>
        /// 当前正处于高亮状态的元素
        /// </summary>
        internal IGuiNode CurrentHighlightElement;
        private bool CtrlKeyDown
        {
            get
            {
                return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            }
        }
        private string FilePath { get; set; }

        /// <summary>
        /// 判断当前元素是否为系统进程下元素相关；新UI相关
        /// </summary>
        ElementInfoDealHelper CurrentElementInfoDealHelper = new ElementInfoDealHelper();

        private ElementHighlighter _CurrentElementHighlighter = null;
        /// <summary>
        /// 元素选中效果UI操作对象
        /// </summary>
        public ElementHighlighter CurrentElementHighlighterManager
        {
            get
            {
                //if (_CurrentElementHighlighter == null)
                //{
                //    _CurrentElementHighlighter = new ElementHighlighter();
                //}
                return _CurrentElementHighlighter;
            }
            set
            {
                _CurrentElementHighlighter = value;
            }
        }

        #endregion

        #region 窗体构造、窗体事件

        /// <summary>
        /// 初始化运行环境
        /// </summary>
        public void InitialEnvironment()
        {
            CurrentElementInfoDealHelper.InitExplorerPIDTimer();//暂时不处理系统判断逻辑
        }

        /// <summary>
        /// 回收运行环境
        /// </summary>
        public void DisposeEnvironment()
        {
            CurrentElementInfoDealHelper.DisposeExplorerPIDTimers();//暂时不处理系统判断逻辑
            CurrentElementHighlighterManager?.Close();
        }

        public DataCollectOverlayWnd()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            this.SourceInitialized += CaptureScreenWnd_SourceInitialized;
            inspectDispatcherTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
            inspectDispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            inspectDispatcherTimer.Interval = TimeSpan.FromMilliseconds(30);

            //moveTipsDispatcherTimer.Tick -= new EventHandler(moveTipsDispatcherTimer_Tick);
            //moveTipsDispatcherTimer.Tick += new EventHandler(moveTipsDispatcherTimer_Tick);
            //moveTipsDispatcherTimer.Interval = TimeSpan.FromMilliseconds(15);

            InitialEnvironment();//新拾取UI相关
        }
        public void CaptureScreenWnd_SourceInitialized(object sender, EventArgs e)
        {
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
            SetPenetrate(true);
            //System.IntPtr handle = (new System.Windows.Interop.WindowInteropHelper(this)).Handle;
            //WinInterop.HwndSource.FromHwnd(handle).AddHook(new System.Windows.Interop.HwndSourceHook(WindowProc));

            IntPtr wptr = new WindowInteropHelper(this).Handle;
            HwndSource hs = HwndSource.FromHwnd(wptr);
            hs.AddHook(new HwndSourceHook(WndProc));


            //HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            //if (hwndSource != null)
            //{
            //    hwndSource.AddHook(new HwndSourceHook(WndProc));
            //}
        }
        public void SetPenetrate(bool transparent)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            uint extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            if (transparent)
            {
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TRANSPARENT);
            }
            else
            {
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle & ~NativeMethods.WS_EX_TRANSPARENT);
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                UninstallHook();
                DataCollectingCaptureTask.Instance.StopAfterExecute();
                inspectDispatcherTimer?.Stop();
                DisposeEnvironment();//新拾取UI相关
                //moveTipsDispatcherTimer?.Stop();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "关闭数据采集拾取窗异常");
            }
        }
        protected IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //if (msg == NativeMethods.WM_NCHITTEST)
            //{
            //    return (IntPtr)NativeMethods.HTTRANSPARENT;
            //}
            return (IntPtr.Zero);
            //return (IntPtr)NativeMethods.HTTRANSPARENT;
        }
        #endregion

        #region 拾取动作控制：开始拾取、取消拾取、暂停拾取、停止高亮（有回调）、停止拾取(无回调)

        internal async void StartCapture(ParamStartCollectElementCaptureModel captureParam)
        {
            SuspendCapture();
            RetCaptureModel = new RetElementCaptureModel();
            CaptureParam = captureParam;
            lastClickTime = DateTime.Now;
            packagesPath = captureParam.PackagePath;
            HasDoSelect = false;
            CaptureError = string.Empty;
            SelectorError = string.Empty;
            CurrentHighlightElement = null;
            CaptureException = null;
            IsUseTableElement = false;
            _collectType = captureParam.CollectType;
            var task = Task.Run(() =>
            {
                SetCaptureTips(CaptureParam.CaptureElementType);
                this.Dispatcher.Invoke(() =>
                {
                    GuiElement.ReadyToCapture();
                });
                StartCapture();
            });
            await task;
        }
        internal void StartCapture()
        {
            //moveTipsDispatcherTimer.Start();
            GuiElement.overlayForm.InitCancellationForCapture();

            inspectDispatcherTimer.Start();
            DataCollectingCaptureTask.Instance.Init();
            DataCollectingCaptureTask.Instance.Start();
            CurrentElementInfoDealHelper.StartExplorerPIDTimers();//新拾取UI相关//暂时不处理系统判断逻辑
            InstallHook();
        }
        /// <summary>
        /// 暂停拾取动作
        /// </summary>
        internal void SuspendCapture()
        {
            try
            {
                UninstallHook();
                //moveTipsDispatcherTimer.Stop();
                inspectDispatcherTimer.Stop();
                CurrentElementInfoDealHelper.StopExplorerPIDTimers();//新拾取UI相关//暂时不处理系统判断逻辑
                DataCollectingCaptureTask.Instance.StopAfterExecute();
                this.Dispatcher.Invoke(new Action(() =>
                {
                    MoveHighlightRect(System.Drawing.Rectangle.Empty);
                    HideHighLightWnd();//新拾取UI相关
                    border_Tips.Visibility = Visibility.Hidden;

                }));


            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "StopHighlight Exception");
            }
        }
        /// <summary>
        /// 设置拾取信息
        /// </summary>
        /// <param name="_CaptureElementType"></param>
        private void SetCaptureTips(CaptureElementType _CaptureElementType)
        {
            if (!sp_escTips.CheckAccess())
            {
                sp_escTips.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    SetCaptureTips(_CaptureElementType);
                });
                return;
            }
            switch (_CaptureElementType)
            {
                case CaptureElementType.PagingButtonElement:
                    sp_escTips.Visibility = Visibility.Visible;
                    tb_excTips.Text = eCommonMessageType.DataCollectOverlayWnd_xaml_cs_QuXiaoShiQuFanYeAnNiu.GetMessage();
                    tb_captureTypeTips.Text = eCommonMessageType.DataCollectOverlayWnd_xaml_cs_ShiQuFanYeAnNiu.GetMessage();
                    border_Tips.Width = 225;
                    break;
                case CaptureElementType.SimilarElement:
                    sp_escTips.Visibility = Visibility.Visible;
                    tb_excTips.Text = eCommonMessageType.DataCollectOverlayWnd_xaml_cs_QuXiaoShiQuTongLeiShuJu.GetMessage();
                    tb_captureTypeTips.Text = eCommonMessageType.DataCollectOverlayWnd_xaml_cs_ShiQuTongLeiShuJu.GetMessage();
                    border_Tips.Width = 225;
                    break;
                case CaptureElementType.DataCollectingElement:
                default:
                    sp_escTips.Visibility = Visibility.Collapsed;
                    tb_captureTypeTips.Text = eCommonMessageType.DataCollectOverlayWnd_xaml_HuoQuCaiJiShuJu.GetMessage();
                    border_Tips.Width = 208;
                    break;
            }
        }
        /// <summary>
        /// 停止高亮  hide窗口 有回调
        /// </summary>
        internal void StopHighlight()
        {
            logger.LogDebug("StopHighlight。。。。");
            try
            {
                SuspendCapture();
                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.Hide();
                }));
                HideHighLightWnd();//新拾取UI相关
                _ = Task.Run(() => DataCollectingManager.StartElementCapture_OnSelectored?.Invoke(this));
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "StopHighlight Exception");
            }
            logger.LogDebug("StopHighlight Eed");
        }
        /// <summary>
        /// 停止拾取 隐藏窗口 (无回调)
        /// </summary>
        internal void StopCapture()
        {
            logger.LogDebug("StopCapture。。。。");
            try
            {
                SuspendCapture();
                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.Hide();
                }));
                HideHighLightWnd();//新拾取UI相关
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "StopCapture Exception");
            }
            logger.LogDebug("StopCapture Eed");
        }
        /// <summary>
        /// 取消拾取
        /// </summary>
        private void DoCancel()
        {
            MessageHelper.DoCancle();
            this.RetCaptureModel.Code = (int)DataCollectionCaptureStatusType.UserCancel;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        UiElement_OnCancel();
                        StopHighlight();
                    }));
                }
                catch { }
            });
        }

        #endregion

        #region 键盘、鼠标勾子事件

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
            HookManager.MouseClickExt += DataCollectOverlayWnd_MouseClickExt;
            HookManager.KeyDown += DataCollectOverlayWnd_KeyDown;
        }

        internal void UninstallHook()
        {
            try
            {
                HookManager.MouseClickExt -= DataCollectOverlayWnd_MouseClickExt;
                HookManager.KeyDown -= DataCollectOverlayWnd_KeyDown;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"UninstallHook 出错，可能是无需卸载事件.");
            }
        }
        private void DataCollectOverlayWnd_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Escape)
            {
                e.Handled = true;
                if (CaptureParam.CaptureElementType == CaptureElementType.DataCollectingElement)
                {
                    this.CaptureException = new RPAException(eErrorMessageType.Error_USERCLOSE);
                }
                DoCancel();
            }
        }
        private void DataCollectOverlayWnd_MouseClickExt(object sender, MouseEventExtArgs e)
        {
            try
            {
                //MouseLastClicPoint = Cursor.Position;
                //logger.LogDebug("enter_MouseClickExt ");
                if (CurrentHighlightElement == null)
                {
                    return;
                }
                //logger.LogDebug("MouseClickExt");

                var span = (DateTime.Now - lastClickTime).TotalMilliseconds;
                lastClickTime = DateTime.Now;
                if (span < 200)
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
                        //inspectDispatcherTimer.Stop();
                        //DataCollectingCaptureTask.Instance.StopAfterExecute();
                        logger.LogDebug("MouseClickExt——start DoSelect Task.Run");
                        //UninstallHook();
                        SuspendCapture();
                        DataCollectingManager.ShowLoadWaiting(true);
                        _ = Task.Run(DoSelect);
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
                SelectorError = "拾取程序开小差了，请重试一下吧！".I18n("DataCollectOverlayWnd.xaml_9897E35A");
                StopHighlight();
            }
        }
        #endregion

        #region 高亮、鼠标提示等UI效果

        private void MoveHighlightRect(System.Drawing.Rectangle rect)
        {
            var rectXY = this.PointFromScreen(new System.Windows.Point(rect.Location.X, rect.Location.Y));
            var rectWH = this.PointFromScreen(new System.Windows.Point(rect.Width, rect.Height));
            lightRect.Width = (uint)rectWH.X;
            lightRect.Height = (uint)rectWH.Y;
            lightRect_Top.Height = (uint)rectXY.Y;
            lightRect_Left.Width = (uint)rectXY.X;
            lightRect_Right.Width = (uint)(this.Width - lightRect_Left.Width - lightRect.Width);
            lightRect_Bottom.Height = (uint)(this.Height - lightRect_Top.Height - lightRect.Height);
            if (string.IsNullOrEmpty(txt_tagTips.Text.Trim()))
            {
                if (border_tagTips.Visibility != Visibility.Hidden)
                {
                    border_tagTips.Visibility = Visibility.Hidden;
                }
                return;
            }
            if (border_tagTips.Visibility != Visibility.Visible)
            {
                border_tagTips.Visibility = Visibility.Visible;
            }
            Canvas.SetTop(border_tagTips, (uint)(lightRect_Top.Height - border_tagTips.ActualHeight + 2));
            if (border_Tips.ActualWidth < lightRect.Width + lightRect_Right.Width)
            {
                Canvas.SetLeft(border_tagTips, (uint)(lightRect_Left.Width));
            }
            else
            {
                Canvas.SetLeft(border_tagTips, (uint)(this.Width - border_tagTips.ActualWidth));
            }
        }

        /// <summary>
        /// 隐藏高亮框
        /// </summary>
        public void HideHighLightWnd()
        {
            CurrentElementHighlighterManager?.Close();
        }

        public void MoveHighlightRectNew(ElementHighLighterParam _ElementHighLighterParam)
        {
            //logger.LogDebug($"rect:{_ElementHighLighterParam.CRectangle} targetName:{_ElementHighLighterParam.CTagName} eleType:{_ElementHighLighterParam.CElementType.ToString()} aGuiType:{_ElementHighLighterParam.EGuiType} ");
            CurrentElementHighlighterManager.Show(_ElementHighLighterParam);

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                CurrentElementHighlighterManager.SetBackgroundTransparent(false);
            }
            else
            {
                CurrentElementHighlighterManager.SetBackgroundTransparent(true);
            }
        }

        private void MoveTips(System.Windows.Point point)
        {
            var offsetX = 8;
            var offsetY = 25;
            var x = point.X;
            var y = point.Y;
            if (x + offsetX + border_Tips.ActualWidth >= this.Width)
            {
                x = (int)(this.Width - border_Tips.ActualWidth);
            }
            else
            {
                x += offsetX;
            }
            if (y + offsetY + border_Tips.ActualHeight >= this.Height)
            {
                y = (int)(y - border_Tips.ActualHeight - 10);
            }
            else
            {
                y += offsetY;
            }
            Canvas.SetLeft(border_Tips, (uint)x);
            Canvas.SetTop(border_Tips, (uint)y);
            if (border_Tips.Visibility != Visibility.Visible)
            {
                border_Tips.Visibility = Visibility.Visible;
            }
            //this.Dispatcher.Invoke(new Action(() =>
            //{
            //    border_Tips.Visibility = Visibility.Visible;
            //}));
        }

        #endregion

        #region 元素拾取主功能，拾取timer、生成selecotr、selector处理、获取下一也按钮、等响应用户操作的行为
        Stopwatch sw = new Stopwatch();
        int timerCOunt = 0;
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            ShowElementNew();//新版本数据采集UI
        }

        public void ShowElementNew()
        {
            try
            {
                sw.Restart();
                this.Topmost = true;
                var point = System.Windows.Forms.Cursor.Position;

                if (this.CurrentHighlightElement == null)
                {
                    HideHighLightWnd();
                    border_Tips.Visibility = Visibility.Hidden;
                    return;
                }
                var wndPoint = this.PointFromScreen(new System.Windows.Point(point.X, point.Y));
                if (wndPoint.X < 0 || wndPoint.Y < 0)
                {
                    return;
                }
                MoveTips(wndPoint);
                //控制元素拾取频率
                timerCOunt++;
                if (timerCOunt % 2 == 0)
                {
                    timerCOunt = 0;
                    return;
                }
                if (CurrentHighlightElement == null)
                {
                    HideHighLightWnd();
                    return;
                }
                if (this.Visibility != Visibility.Visible)
                {
                    this.Visibility = Visibility.Visible;
                }

                ElementHighLighterParam _ElementHighLighterParam = CurrentElementInfoDealHelper.GetCurrentElementHighLighterParam(CurrentHighlightElement);
                _ElementHighLighterParam.CElementType = ElementType.Ordinary;
                MoveHighlightRectNew(_ElementHighLighterParam);
                sw.Stop();
            }
            catch (RPAException ex)
            {
                ex.ComponentType = eComponentType.Capture;
                CaptureError = ex.Message;
                CaptureException = ex;
                //Java Chrome 插件未安装异常导致拾取退出 
                StopHighlight();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                //ExceptionError = ex.Message;
                CaptureError = "拾取过程中发生异常，请重新拾取！".I18n("DataCollectOverlayWnd.xaml_1716270F");
                StopHighlight();
            }
            finally
            {

            }
        }

        public void ShowElementOld()
        {
            try
            {
                sw.Restart();
                this.Topmost = true;

                var point = System.Windows.Forms.Cursor.Position;

                if (this.CurrentHighlightElement == null)
                {
                    MoveHighlightRect(System.Drawing.Rectangle.Empty);
                    HideHighLightWnd();
                    border_Tips.Visibility = Visibility.Hidden;
                    return;
                }
                var wndPoint = this.PointFromScreen(new System.Windows.Point(point.X, point.Y));
                if (wndPoint.X < 0 || wndPoint.Y < 0)
                {
                    return;
                }
                MoveTips(wndPoint);
                //控制元素拾取频率
                timerCOunt++;
                if (timerCOunt % 2 == 0)
                {
                    timerCOunt = 0;
                    return;
                }
                //logger.LogDebug("1.根据坐标拾取元素");
                //CurrentHighlightElement = DataCollectingManager.InspectUI(point);
                if (CurrentHighlightElement == null)
                {
                    MoveHighlightRect(System.Drawing.Rectangle.Empty);
                    HideHighLightWnd();
                    return;
                }
                if (this.Visibility != Visibility.Visible)
                {
                    this.Visibility = Visibility.Visible;
                }
                //logger.LogDebug("3.开始获取当前高亮元素的区域信息");
                System.Drawing.Rectangle rect = CurrentHighlightElement.BoundingRectangle;
                StringBuilder lableTextSB = new StringBuilder();
                string targetName = CurrentHighlightElement.ControlType;
                if (!string.IsNullOrEmpty(targetName))
                {
                    //string controlType = CurrentHighlightElement.ControlType;
                    //if (CurrentHighlightElement is IGuiWebNode_690)
                    //{
                    //    if (!string.IsNullOrEmpty(controlType))
                    //    {
                    //        controlType = $"{controlType}/{(CurrentHighlightElement as IGuiWebNode_690).Attributes}";
                    //    }
                    //}
                    string name = II.RPA.WinAutomation.CommonUtility.TargrtXmlHelper.CreateInstance().GetTranslatedNameByCode(CurrentHighlightElement.GuiNodeControlName());
                    lableTextSB.Append(name);
                }
                txt_tagTips.Text = lableTextSB.ToString();
                //logger.LogDebug($"4.获取到当前高亮元素的区域信息{rect.ToString()}，显示高亮框");
                MoveHighlightRect(rect);
                sw.Stop();
                //logger.LogDebug($"============本次拾取结束,耗时：{sw.Elapsed},{sw.ElapsedMilliseconds}");
                //logger.LogDebug("");
                //logger.LogDebug("");
            }
            catch (RPAException ex)
            {
                ex.ComponentType = eComponentType.Capture;
                CaptureError = ex.Message;
                CaptureException = ex;
                //Java Chrome 插件未安装异常导致拾取退出 
                StopHighlight();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                //ExceptionError = ex.Message;
                CaptureError = "拾取过程中发生异常，请重新拾取！".I18n("DataCollectOverlayWnd.xaml_1716270F");
                StopHighlight();
            }
            finally
            {

            }
        }

        internal string DoSelect()
        {
            try
            {
                logger.LogDebug("DoSelect begin");
                lock (doSelect_objLock)
                {

                    string selector = null;
                    if (HasDoSelect)
                    {
                        return "";
                    }
                    HasDoSelect = true;
                    //this.Invoke(new Action(() =>
                    //{
                    //    this.Hide();
                    //}));
                    logger.LogDebug(LogBlock.NullCheck(CurrentHighlightElement, "CurrentHighlightElement"));

                    if (CurrentHighlightElement != null)
                    {
                        try
                        {
                            if (!(CurrentHighlightElement is IGuiNode_6_1_0))
                            {
                                var rpaex = new RPAException(eErrorMessageType.Error_DataCollection_GUINoSupport);
                                CaptureException = rpaex;
                                return "";
                            }
                            var node = CurrentHighlightElement as IGuiNode_6_1_0;
                            if (_collectType == "web" &&
                                !(eGuiType.Web360se == node.GuiType || eGuiType.IE == node.GuiType || eGuiType.Chrome == node.GuiType
                                || eGuiType.FireFox == node.GuiType || eGuiType.Edge == node.GuiType || eGuiType.SuperBrowser == node.GuiType
                                || eGuiType.Web360X == node.GuiType))
                            {
                                //MessageHelper.ShowDialog("选择采集类型与采集目标元素类型不符，无法采集此类元素数据。");
                                throw new RPAException(eErrorMessageType.Error_DataCollection_ElementTypeError);
                            }
                            if (_collectType == "software" &&
                               !(eGuiType.SAP == node.GuiType || eGuiType.Java == node.GuiType || eGuiType.Windows == node.GuiType || eGuiType.WdtErp == node.GuiType))
                            {
                                //MessageHelper.ShowDialog("选择采集类型与采集目标元素类型不符，无法采集此类元素数据。");
                                throw new RPAException(eErrorMessageType.Error_DataCollection_ElementTypeError);
                            }

                            if (CaptureParam.CaptureElementType != CaptureElementType.PagingButtonElement
                                && GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.type == "table")
                            {
                                var rpaex = new RPAException(eErrorMessageType.Error_DataCollection_CapturedTableElement);
                                CaptureException = rpaex;
                                return "";
                            }
                            var element = new GuiElement(CurrentHighlightElement);
                            //首次数据采集拾取时，判断是否是表格
                            if (CaptureParam.IsFirstCapture && this.CaptureParam.CaptureElementType == CaptureElementType.DataCollectingElement)
                            {
                                if (_collectType == "software")
                                {
                                    this.IsUseTableElement = element.ElementIsTable();
                                    if (!this.IsUseTableElement && node.GuiType != eGuiType.Windows)
                                    {
                                        throw new RPAException(eErrorMessageType.Error_DataCollection_ElementNotTableError);
                                    }
                                }
                                else
                                {
                                    this.IsUseTableElement = ElementIsTable(element);
                                }
                            }
                            string screenShotPath = string.Empty;//缩略图地址
                            if (!string.IsNullOrEmpty(packagesPath))
                            {
                                screenShotPath = DataCollectingManager.GetNewImgPath("res");
                                string screenShotFilePath = System.IO.Path.Combine(packagesPath, screenShotPath);
                                //隐藏窗体并截全屏，以便后期自己绘制红色标记矩形
                                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                                sw.Start();
                                //this.Dispatcher.Invoke(new Action(() => { this.Opacity = 0; }));
                                using (var currentDesktopScreenshot = UiCommon.UIAAutomation.GetDesktop().Capture())
                                {
                                    element.currentInformativeScreenshot = element.CaptureInformativeScreenshot(screenShotFilePath, currentDesktopScreenshot);
                                }
                                //this.Dispatcher.Invoke(new Action(() => { this.Opacity = 1; }));
                                element.screenShotPath = screenShotPath;
                                sw.Stop();
                            }
                            logger.LogDebug($"get selector UIElement_OnSelected");
                            UIElement_OnSelected(element);
                        }
                        catch (RPAException e)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "拾取元素操作失败");
                            SelectorError = e.ErrorMessage;
                            this.CaptureException = e;
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "完成拾取动作时发生了异常，本次拾取结束");
                            SelectorError = "拾取元素操作失败,请重试!".I18n("DataCollectOverlayWnd.xaml_80A22A39");
                            this.CaptureException = ex;
                        }
                        finally
                        {
                            StopHighlight();
                        }
                        return selector;
                    }
                    return "";
                }
            }
            catch (RPAException e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "拾取元素操作失败");
                SelectorError = e.ErrorMessage;
                return "";
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "DoSelect Error");
                SelectorError = "拾取元素操作失败,请重试!".I18n("DataCollectOverlayWnd.xaml_80A22A39");
                return "";
            }
        }
        private bool ElementIsTable(GuiElement element)
        {
            var isTable = element.ElementIsTable();
            if (isTable)
            {
                return MessageHelper.ShowDialog(eCommonMessageType.DataCollectOverlayWnd_xaml_cs_ChengGongShiBieBiaoGeNeiYuanSu.GetMessage());
            }
            return false;
        }
        private void UIElement_OnSelected(GuiElement element)
        {
            //AddFileAttrToSelector(element.Selector, element.screenShotPath);
            string orgJson = string.Empty;
            if (this.IsUseTableElement)
            {
                orgJson = element.TableSelector;
                //orgJson = element.TableSelector1;
            }
            else
            {
                bool innerTextFirst = CaptureParam.CaptureElementType == CaptureElementType.PagingButtonElement;
                orgJson = element.GetSelector(innerTextFirst);
                // orgJson = element.Selector1;
            }
            string filePath = element.screenShotPath;
            try
            {
                var common = orgJson.ToObject<RPACommon>();
                common.CommonProps.filePath = filePath;
                common.CommonProps.captureMode = (int)CaptureModeType.Ordinary;

                var selectorObject = common.Selector.ToObject<List<SelectorProps>>();
                var tabProps = selectorObject.Find(o => o.Name == ConstFields.DATACOLECTING_SELECTORPROPS_TAB);
                if (tabProps != null)
                {
                    selectorObject.Remove(tabProps);
                    var title = tabProps.Props.Find(p => p.Name == ConstFields.DATACOLECTING_PAGE_TITLE)?.Value ?? string.Empty;
                    var url = tabProps.Props.Find(p => p.Name == ConstFields.DATACOLECTING_PAGE_URL)?.Value ?? string.Empty;
                    if (!string.IsNullOrEmpty(url))
                    {
                        ///数据埋点
                        GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.collect_page_url = url;
                    }
                    string currentUrl = url;
                    if (!string.IsNullOrEmpty(url))
                    {
                        var count = url.IndexOf('?');
                        if (count > 0)
                            currentUrl = url.Substring(0, count);
                    }
                    bool isSamePage = false;
                    if (string.IsNullOrEmpty(this.CaptureParam.PageTitle))
                    {
                        this.RetCaptureModel.PageTitle = title;
                        isSamePage = true;
                    }
                    else if (this.CaptureParam.PageTitle == title)
                    {
                        isSamePage = true;
                    }
                    if (string.IsNullOrEmpty(this.CaptureParam.PageUrl))
                    {
                        this.RetCaptureModel.PageUrl = url;
                        isSamePage = true;
                    }
                    else if (this.CaptureParam.PageUrl == url && !isSamePage)
                    {
                        isSamePage = true;
                    }
                    else if (this.CaptureParam.PageUrl.Contains(currentUrl) && !isSamePage)
                    {
                        isSamePage = true;
                    }

                    if (!isSamePage)
                    {
                        var originUrl = this.CaptureParam.PageUrl;
                        var isSameUrl = CompareUrl(originUrl, url);
                        //页面不相同
                        if (!isSameUrl)
                        {
                            bool bRet = MessageHelper.ShowDialog(eErrorMessageType.Error_DataCollection_DifferentPages.GetMessage(), "知道了".I18n("DataCollectOverlayWnd.xaml_79AB06CD"));
                            RetCaptureModel.Code = (int)DataCollectionCaptureStatusType.NonSamePage;
                            return;
                        }
                    }
                }
                this.RetCaptureModel.GuiType = common.CommonProps.GUIType;
                this.RetCaptureModel.ScreenShotPath = filePath;
                //this.RetCaptureModel.OriginalSelector = common.ToJson();//selector原串

                //处理select 得到只有tag的selector
                ProcessSelector(element, selectorObject);
                //获取下一页元素
                GetNextElementBuildSelector(element);
                if (common.Selector == null)
                    return;

            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw ex;
            }
        }

        private bool CompareUrl(string url1, string url2)
        {
            try
            {
                Uri uri1 = new Uri(url1);
                Uri uri2 = new Uri(url2);
                if (uri1.Host == uri2.Host && uri1.AbsolutePath == uri2.AbsolutePath)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                logger.LogDebug("对比两个URL是否是同一个网址异常");
                return false;
            }
        }
        private void ProcessSelector(GuiElement element, List<SelectorProps> selectorObject)
        {
            try
            {

                List<SelectorProps> processedSelector = null;
                //分页按钮selector不需要自动处理,只要需要原串即可，不需要后续处理
                if (CaptureParam.CaptureElementType == CaptureElementType.PagingButtonElement)
                {
                    this.RetCaptureModel.NextPageBtnSelector = DataCollectingManager.SelectorValueAddStrEscape(new ParamSelectorValueAddStrEscapeModel
                    {
                        selector = selectorObject
                    });
                    return;
                }
                //相似元素拾取  对比生成
                else if (CaptureParam.CaptureElementType == CaptureElementType.SimilarElement)
                {
                    this.RetCaptureModel.OriginalSelector = selectorObject;
                    //获取处理之后的selector,即最终用来做数据采集的selector
                    processedSelector = element.CompareBulidCollectDataSelector(new ParamBulidCollectDataSelectorModel
                    {
                        OriginalSelector = CaptureParam.LastCaptureSelector.ToObject<List<SelectorProps>>(),//副本
                        CompareOriginalSelector = selectorObject
                    });

                }
                //数据采集  自动生成
                else if (CaptureParam.CaptureElementType == CaptureElementType.DataCollectingElement)
                {

                    if (this.IsUseTableElement)
                    {
                        RetCaptureModel.IsTable = true;
                        //var lastTableNodeIndex = selectorObject.FindLastIndex(so => so.Name == "table");
                        //if (lastTableNodeIndex > 0 && lastTableNodeIndex < selectorObject.Count - 1)
                        //{
                        //    selectorObject.RemoveRange(lastTableNodeIndex + 1, selectorObject.Count - lastTableNodeIndex - 1);
                        //}
                        processedSelector = selectorObject;
                    }
                    else if (_collectType == "software")
                    {
                        this.RetCaptureModel.OriginalSelector = selectorObject.ToObject<List<SelectorProps>>();//副本
                        processedSelector = selectorObject;
                    }
                    else
                    {
                        this.RetCaptureModel.OriginalSelector = selectorObject;

                        List<SelectorProps> onlyContainsTagsProps = new List<SelectorProps>();
                        SelectorProps _SelectorProps = null;
                        foreach (var sp in selectorObject)
                        {
                            _SelectorProps = new SelectorProps();
                            _SelectorProps.Name = sp.Name;
                            _SelectorProps.Type = sp.Type;
                            _SelectorProps.Props = new List<Props>();
                            if (sp.Name == ConstFields.DATACOLECTING_SELECTORPROPS_IFRAME ||
                                sp.Name == ConstFields.DATACOLECTING_SELECTORPROPS_FRAME ||
                                sp.Name == ConstFields.DATACOLECTING_SELECTORPROPS_FRAMESET)
                            {
                                _SelectorProps.Props.AddRange(sp.Props);
                                onlyContainsTagsProps.Add(_SelectorProps);
                                continue;
                            }
                            foreach (var prop in sp.Props)
                            {
                                if (prop.Name == ConstFields.DATACOLECTING_SELECTORPROPS_ID)
                                {
                                    _SelectorProps.Props.Add(prop);
                                }
                                else if (prop.Name == ConstFields.DATACOLECTING_SELECTORPROPS_INDEX2)
                                {
                                    continue;
                                }
                                else if (prop.Name == ConstFields.DATACOLECTING_PAGE_TITLE)
                                {
                                    _SelectorProps.Props.Add(new Props
                                    {
                                        //Name = prop.Name,
                                        //Value = ".*",
                                        //Pattern = Pattern.REGULAR.GetPatternAttribute(),
                                        //Accurate = "true"
                                        Name = prop.Name,
                                        Value = prop.Value,
                                        Pattern = Pattern.REGULAR.GetPatternAttribute(),
                                        Accurate = "true"
                                    });
                                }
                                else
                                {
                                    _SelectorProps.Props.Add(new Props
                                    {
                                        Name = prop.Name,
                                        Value = ".*",
                                        Pattern = Pattern.REGULAR.GetPatternAttribute(),
                                        Accurate = "true"
                                    });
                                }
                            }
                            onlyContainsTagsProps.Add(_SelectorProps);
                        }
                        //var onlyContainsTagsProps = selectorObject.Select(so => new SelectorProps
                        //{
                        //    Name = so.Name,
                        //    Type = so.Type,
                        //    Props = so.Props.Where(sop => so.Name == ConstFields.DATACOLECTING_SELECTORPROPS_IFRAME || sop.Name == ConstFields.DATACOLECTING_SELECTORPROPS_ID)?.ToList()
                        //});
                        //获取处理之后的selector,即最终用来做数据采集的selector
                        processedSelector = element.AutoBulidCollectDataSelector(new ParamBulidCollectDataSelectorModel
                        {
                            OriginalSelector = this.RetCaptureModel.OriginalSelector.ToObject<List<SelectorProps>>(),
                            OnlyContainsTagsSelector = onlyContainsTagsProps.ToList()
                        });
                    }
                }


                processedSelector = DataCollectingManager.SelectorValueAddStrEscape(new ParamSelectorValueAddStrEscapeModel
                {
                    selector = processedSelector
                });
                //获取处理之后的selector,即最终用来做数据采集的selector
                RetCaptureModel.ProcessedSelector = processedSelector;
                //MessageHelper.ShowWeakError("拾取数据成功");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "处理拾取得到的selector异常");
                //MessageHelper.ShowWeakError(ex, "拾取数据处理异常");
                throw ex;
            }
        }

        private void GetNextElementBuildSelector(GuiElement element)
        {
            try
            {

                //只有数据采集拾取，且为首次拾取才执行获取下一页逻辑
                if (!CaptureParam.IsFirstCapture || this.CaptureParam.CaptureElementType != CaptureElementType.DataCollectingElement)
                {
                    return;
                }
                string selector = element.GetNextElementBuildSelector();
                if (!string.IsNullOrEmpty(selector))
                {
                    var originalSelector = selector.ToObject<List<SelectorProps>>();
                    originalSelector.RemoveAll(o => o.Name == ConstFields.DATACOLECTING_SELECTORPROPS_TAB);
                    originalSelector = DataCollectingManager.SelectorValueAddStrEscape(new ParamSelectorValueAddStrEscapeModel
                    {
                        selector = originalSelector
                    });
                    this.RetCaptureModel.NextPageBtnSelector = originalSelector;
                    string screenShotPath = string.Empty;//缩略图地址
                    if (!string.IsNullOrEmpty(packagesPath))
                    {
                        screenShotPath = DataCollectingManager.GetNewImgPath("res");
                        string screenShotFilePath = System.IO.Path.Combine(packagesPath, screenShotPath);
                        //隐藏窗体并截全屏，以便后期自己绘制红色标记矩形
                        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                        sw.Start();
                        using (Stream sm = this.GetType().Assembly.GetManifestResourceStream($"II.RPA.Plugin.GUI.DataCollection.Assets.Images.TurnPageDefultImg_Embedded.png"))
                        {
                            using (var img = new Bitmap(sm))
                            {
                                img.Save(screenShotFilePath);
                            }
                        }
                        this.RetCaptureModel.NextPageBtnScreenShotPath = screenShotPath;
                        sw.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "自动获取翻页按钮逻辑发生异常，用户可以人为再次拾取，忽略此异常");
            }
        }
        private void AddFileAttrToSelector(string orgJson, string filePath)
        {
        }
        private void UiElement_OnCancel()
        {
            this.CaptureSelector = string.Empty;
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //新拾取UI相关
            CurrentElementHighlighterManager = new ElementHighlighter();
            CurrentElementInfoDealHelper.StartExplorerPIDTimers();//暂时不处理系统判断逻辑
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //新拾取UI相关
            if (this.Visibility == Visibility.Hidden || this.Visibility == Visibility.Collapsed)
            {
                CurrentElementHighlighterManager?.Close();
            }
        }
    }
}


