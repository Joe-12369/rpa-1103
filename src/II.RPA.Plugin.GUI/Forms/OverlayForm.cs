using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.AutomationElements;
using Gma.UserActivityMonitor;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Text;
using Newtonsoft.Json.Linq;
using System.IO;
using II.RPA.Plugin.GUI.Commons;
using System.Diagnostics;
using II.RPA.Plugin.GUI.Params;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Logging.Formatting;
using II.RPA.Plugin.GUI.ElementHighlight;
using II.RPA.Plugin.GUI.HighLight;
using II.RPA.WinAutomation.CommonUtility.Utils;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.Plugin.Core.UI;
using II.RPA.Plugin.GUI.UIProviders.Tips;

namespace II.RPA.Plugin.GUI
{
    public partial class OverlayForm : Form
    {
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessageTimeout(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam, int fuFlags, int uTimeout, out UIntPtr lpdwResult);
        [DllImport("oleacc.dll", CharSet = CharSet.Auto, PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Interface)]
        public static extern object ObjectFromLresult(UIntPtr lResult, [MarshalAs(UnmanagedType.LPStruct)] Guid refiid, IntPtr wParam);
        //[DllImport("oleacc.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        //public static extern Int32 ObjectFromLresult(Int32 lResult, ref Guid riid, Int32 wParam, out IHTMLDocument2 ppvObject);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr WindowFromPoint(int xPoint, int yPoint);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string message);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int ObjectFromLresult(
        int lResult,
        ref System.Guid riid,
        int wParam,
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.Interface), System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] ref System.Object ppvObject
        //注意这个函数ObjectFromLresult的声明.
        );

        /// <summary>
        /// 判断当前元素是否为系统进程下元素相关；新UI相关
        /// </summary>
        ElementInfoDealHelper CurrentElementInfoDealHelper = new ElementInfoDealHelper();

        //private ElementHighlighter _CurrentElementHighlighter = null;

        ///// <summary>
        ///// 元素选中效果UI操作对象
        ///// </summary>
        //public ElementHighlighter CurrentElementHighlighterManager
        //{
        //    get
        //    {
        //        if (_CurrentElementHighlighter == null)
        //            _CurrentElementHighlighter = new ElementHighlighter();
        //        return _CurrentElementHighlighter;
        //    }
        //    set
        //    {
        //        _CurrentElementHighlighter = value;
        //    }
        //}

        private HighLightManager _CurrentElementHighlighter = null;

        private readonly UIProvider m_UIProvider;
        // InvokeRequired cannot identify the owner thread before a WinForms handle exists.
        // Keep the constructor's UI dispatcher for the entire input-hook lifecycle.
        private readonly Dispatcher _inputDispatcher = Dispatcher.CurrentDispatcher;
        private bool _mouseHookInstalled;

        /// <summary>
        /// 元素选中效果UI操作对象
        /// </summary>
        public HighLightManager CurrentElementHighlighterManager
        {
            get
            {
                if (_CurrentElementHighlighter == null)
                    _CurrentElementHighlighter = new HighLightManager();
                return _CurrentElementHighlighter;
            }
            set
            {
                _CurrentElementHighlighter = value;
            }
        }



        /// <summary>
        /// 初始化运行环境
        /// </summary>
        public void InitialEnvironment()
        {
            CurrentElementInfoDealHelper.InitExplorerPIDTimer();//暂时不处理本地系统判断逻辑
        }
        #region 新增拾取的初始化函数以及取消标记
        /// <summary>
        /// 拾取CTS 
        /// </summary>
        public CancellationTokenSource CancellationForCapture
        {
            get;
            private set;
        } = new CancellationTokenSource();

        /// <summary>
        /// 新增初始化函数 适应新启动方式（即信息框 overlayform不关闭）
        /// </summary>
        public void InitForStartCapture()
        {
            logger.LogInformation("InitForStartCapture 初始化");
            CancellationForCapture = new CancellationTokenSource();
            OverlayFormCaptureTask.Instance.Init();
            CVCaptureTask.Instance.Init();
        }
        #endregion


        /// <summary>
        /// 回收运行环境
        /// </summary>
        public void DisposeEnvironment()
        {
            CurrentElementInfoDealHelper.DisposeExplorerPIDTimers();//暂时不处理本地系统判断逻辑
        }

        public const int SMTO_ABORTIFHUNG = 0x2;
        public bool IsWindowHighlight { get; internal set; }
        public string IsPause { get; internal set; }
        public string SelectorError { get; internal set; }
        private bool enablePassThrough { get; set; }
        private static ILogger logger = Logger.OfContext();

        internal static DispatcherTimer dispatcherTimer = new DispatcherTimer();

        private static DispatcherTimer dealySelectDispatcherTimer = new DispatcherTimer();
        private static DispatcherTimer keyStateDispatcherTimer = new DispatcherTimer();
        private static readonly object _objLock = new object();
        private static readonly object _objLock_GetPointAndWndInfo = new object();
        public bool IsStartInspectUI = false;
        //public InspectorStatusForm inspectorStatus;
        public InspectInfoWnd inspectorStatus;
        /// <summary>
        /// 四个边框
        /// </summary>
        private ExtendedPanel panelBorderLeft, panelBorderTop, panelBorderRight, panelBorderBottom;
        /// <summary>
        /// 内部边框
        /// </summary>
        private ExtendedPanel panelInside;
        /// <summary>
        /// 当前正处于高亮状态的元素
        /// </summary>
        internal IGuiNode CurrentHighlightElement;

        /// <summary>
        /// 上一个正处于高亮状态的元素（不进行null赋值）
        /// </summary>
        internal IGuiNode LastHighlightElement = null;

        /// <summary>
        /// 正在被使用拾取动作的应用的进程名
        /// </summary>
        internal string InspectAppProcessName { get; set; }
        public Point CurrentHighlightElementRelativeClickPos { get; private set; }

        public bool HasDoSelect { get; private set; }
        /// <summary>
        /// 元素属性展示边框
        /// </summary>
        private ExtendedPanel panelTarget;
        private System.Windows.Forms.Label selectorlabel;
        private System.Windows.Forms.Label selector_element_type_label;

        #region  融合拾取 共享数据区域
        internal Stopwatch _mouseStopSW = null;
        internal Rectangle _mouseStopRectangle = Rectangle.Empty;
        /// <summary>
        /// 开始区域截图时候的坐标点
        /// </summary>
        internal Point MouseLastClicPoint { get; set; }

        internal string CaptureUnauthorizedAccessStr { get; set; } = string.Empty;
        /// <summary>
        /// 普通拾取得到的元素
        /// </summary>
        internal IGuiNode ElementCaptureNode { get; set; }

        internal CaptureOperationDataModel CaptureOperationData { get; set; } = new CaptureOperationDataModel();

        internal bool CaptureDialogFailed { get; private set; }

        internal void ShowCaptureDialog()
        {
            try
            {
                ShowDialog();
            }
            catch
            {
                // Do not reuse an instance whose modal initialization failed.
                CaptureDialogFailed = true;
                throw;
            }
        }

        internal void CleanupCapture()
        {
            // Cleanup must not mask the original capture exception or prevent releasing the gate.
            try
            {
                CancellationForCapture?.Cancel();
                StopHighlight();
                IsStartInspectUI = false;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Capture overlay cleanup failed");
            }

            try
            {
                if (inspectorStatus != null && !inspectorStatus.IsClosed)
                    inspectorStatus.SetWndClose();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Capture status cleanup failed");
            }
        }

        internal AutomationElement CaptureRootWnd
        {
            get
            {
                return OverlayFormCaptureTask.Instance.CaptureRootWnd;
            }
            set
            {
                OverlayFormCaptureTask.Instance.CaptureRootWnd = value;
            }
        }

        /// <summary>
        /// CV图像识别的根元素
        /// </summary>
        internal IGuiCVNode CvCaptureRootNode { get; set; }
        private CaptureModeType _CurrentCaptureMode { get; set; }
        internal CaptureModeType UseCaptureMode { get; set; }
        public bool IsCvSupport { get; set; } = false;
        /// <summary>
        /// 当前是拾取类型 0:智能,1:普通,2浮窗
        /// </summary>
        internal CaptureModeType CurrentCaptureMode
        {
            get
            {
                return _CurrentCaptureMode;
            }
            set
            {
                try
                {
                    this.ElementCaptureNode = null;
                    this.CvCaptureRootNode = null;
                    if (IsCVCapture && value == CaptureModeType.CV)
                    {
                        OverlayFormCaptureTask.Instance.StopAfterExecute();
                        //CVCaptureMonitorTask.Instance.Abort();
                        CVCaptureTask.Instance.StopAfterExecute();
                        _CurrentCaptureMode = CaptureModeType.CV;
                        return;
                    }
                    _CurrentCaptureMode = (CaptureModeType)((int)value % 3);
                    switch (_CurrentCaptureMode)
                    {
                        case CaptureModeType.Intelligence:
                            OverlayFormCaptureTask.Instance.Start();
                            //CVCaptureMonitorTask.Instance.Start();
                            Task.Run(async () =>
                            {
                                await Task.Delay(3000);
                                if (!this.CancellationForCapture.IsCancellationRequested)
                                {
                                    CVCaptureTask.Instance.Start();
                                }
                            }, this.CancellationForCapture.Token);

                            break;
                        case CaptureModeType.Ordinary:
                        case CaptureModeType.FloatWnd:
                            OverlayFormCaptureTask.Instance.Start();
                            //CVCaptureMonitorTask.Instance.Abort();
                            CVCaptureTask.Instance.StopAfterExecute();
                            break;
                    }

                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "切换模式异常");
                }
            }
        }
        /// <summary>
        /// 融合拾取异常
        /// </summary>
        internal Exception CaptureException { get; set; } = null;
        #endregion

        public string BrowserWndImageFilePath { get; internal set; }

        internal void GetCVRootWndInfo(out Point screenPoint, out AutomationElement rootElement, out AutomationElement pointElement)
        {
            lock (_objLock_GetPointAndWndInfo)
            {
                rootElement = null;
                pointElement = null;
                screenPoint = System.Windows.Forms.Cursor.Position;
                //得到自根窗口开始到点窗口（通过点直接获取到）的句柄集合
                //var hWndList = UiCommon.GetRootWindowList(screenPoint);
                List<IntPtr> parentLinked = UiCommon.GetRootWindowList(screenPoint);
                foreach (IntPtr hWnd in parentLinked)
                {
                    if (hWnd != IntPtr.Zero)
                    {
                        rootElement = UiCommon.UIAAutomation.FromHandle(hWnd);   //获取根窗体
                    }
                    if (rootElement == null)
                    {
                        return;
                    }
                    if (rootElement.BoundingRectangle.Contains(screenPoint))
                    {
                        break;
                    }
                }
            }
        }

        public void SetPenetrate()
        {
            try
            {
                if (!this.IsHandleCreated)
                {
                    logger.LogWarning("The handle of OverlayForm is not created!");
                }
                logger.LogInformation($"The handle of OverlayForm:{this.Handle}");
                this.TopMost = true;
                NativeMethods.SetWindowLong(this.Handle, NativeMethods.GWL_EXSTYLE, NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED);
                NativeMethods.SetLayeredWindowAttributes(this.Handle, 3, 10, NativeMethods.LWA_ALPHA);
                this.Opacity = 0.99;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }
        public OverlayForm()
        {
            InitializeComponent();
            logger.LogDebug("Overlayform 构造函数调用");
            this.Load += OverlayForm_Load;
            InitialEnvironment();
            m_UIProvider = new UIProvider();
            GuiElement.OnSelectedParentElement = ShowParentElement;
            GuiElement.OnSelectedChildElement = ShowChildElement;
            GuiElement.CancelAgentCaptureElement = CancelAgentCaptureElement;
            GuiElement.ConfirmAgentCaptureElement = ConfirmAgentCaptureElement;
        }

        #region Win32 API
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr ptr);
        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(
        IntPtr hdc, // handle to DC
        int nIndex // index of capability
        );
        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);
        #endregion

        #region DeviceCaps常量
        const int HORZRES = 8;
        const int VERTRES = 10;
        const int LOGPIXELSX = 88;
        const int LOGPIXELSY = 90;
        const int DESKTOPVERTRES = 117;
        const int DESKTOPHORZRES = 118;
        #endregion


        private void OverlayForm_Load(object sender, EventArgs e)
        {
            SetPenetrate();
            CurrentElementHighlighterManager = new HighLightManager();

        }



        public void Highlighting(Rectangle rectangle, ElementType eleType)
        {
            this.Invoke(new Action(() =>
            {
                this.MoveRect(rectangle, "", eleType);
            }));
        }
        public void Highlighting(List<Rectangle> rectangles, bool useTightBorder = false)
        {
            this.Invoke(new Action(() =>
            {
                MoveRectsNew(rectangles, useTightBorder);
            }));
        }

        private void JavaNode_EventCallback()
        {
            StopHighlight();
        }


        private void OverlayForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            logger.LogDebug("OverlayFormu 关闭！");
            DisposeEnvironment();
            e.Cancel = true;
        }

        private ExtendedPanel CreatePanel(Color color)
        {
            var panel = new ExtendedPanel
            {
                Size = new Size(0, 0),
                Parent = this,
                BackColor = color
            };
            panel.Show();
            return panel;
        }
        private ExtendedPanel CreatePanelWithlabel(Color color)
        {
            var panel = new ExtendedPanel
            {
                Size = new Size(1000, 30),
                Parent = this,
                BackColor = color
            };
            //在panel控件上添加label控件
            selectorlabel = new System.Windows.Forms.Label();
            panel.Controls.Add(selectorlabel);
            // selectorlabel.BackColor = Color.FromArgb(200,0, 0, 0);
            selectorlabel.BackColor = Color.FromArgb(179, 0, 0, 0);
            selectorlabel.ForeColor = Color.FromArgb(255, 255, 255);
            // selectorlabel.Dock = DockStyle.Left;
            selectorlabel.AutoSize = true;
            selectorlabel.TextAlign = ContentAlignment.MiddleLeft;
            //selectorlabel.Height = 30;
            selectorlabel.Font = new System.Drawing.Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            //selectorlabel.Size = new Size(50,30);
            selectorlabel.Location = new Point(0, 0);

            selector_element_type_label = new System.Windows.Forms.Label();
            panel.Controls.Add(selector_element_type_label);
            selector_element_type_label.BackColor = Color.FromArgb(179, 0, 0, 0);
            selector_element_type_label.ForeColor = Color.FromArgb(5, 220, 220, 220);
            selector_element_type_label.AutoSize = true;
            selector_element_type_label.TextAlign = ContentAlignment.MiddleLeft;
            selector_element_type_label.Font = new System.Drawing.Font("Microsoft YaHei,Helvetica Neue,Helvetica,Arial,sans-serif,Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            selector_element_type_label.Location = new Point(0, 0);

            panel.Size = new Size(1000, selectorlabel.Size.Height);

            panel.Show();
            return panel;
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;//避免ATL+TAB时显示该窗体
                cp.ExStyle |= NativeMethods.WS_EX_NOACTIVATE;//禁止激活，以便能够inspect菜单条目
                //cp.ExStyle |= WS_EX_TRANSPARENT;//禁止激活，以便能够inspect菜单条目
                //cp.ExStyle |= WS_EX_LAYERED;//禁止激活，以便能够inspect菜单条目
                return cp;
            }
        }

        public void KeyCancel()
        {
            DoCancel();
        }
        private void DoCancel()
        {
            CancellationForCapture?.Cancel();
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    this.Invoke(new Action(() =>
                    {
                        GuiElement.OnCanceled?.Invoke();
                        StopHighlight();
                    }));
                }
                catch { }
            });
        }
        private void DoDelaySelect()
        {
            logger.LogDebug("启动 DoDelaySelect");
            StopHighlight();
            dealySelectDispatcherTimer.Tick -= new EventHandler(DealySelectDispatcherTimer_Tick);
            dealySelectDispatcherTimer.Tick += new EventHandler(DealySelectDispatcherTimer_Tick);

            dealySelectDispatcherTimer.Interval = TimeSpan.FromSeconds(5);
            dealySelectDispatcherTimer.Stop();
            dealySelectDispatcherTimer.Start();
        }
        private void DealySelectDispatcherTimer_Tick(object sender, EventArgs e)
        {
            dealySelectDispatcherTimer.Tick -= new EventHandler(DealySelectDispatcherTimer_Tick);
            dealySelectDispatcherTimer.Stop();
            StartHighlight(snappath);
        }

        internal string snappath;
        private Bitmap currentDesktopScreenshot;
        private string _selector = string.Empty;
        internal void StartHighlight(string filePath, bool isCVCapture = false, string url = null, int InspectStatus = 0, string selector = null, CaptureModeType captureModeType = CaptureModeType.Intelligence, int isCvSupport = 0)
        {
            //Size desktopSize = NativeMethods.DESKTOP;
            //this.ClientSize = new Size(desktopSize.Width * Screen.AllScreens.Length, desktopSize.Height - 1);
            captureModeType = CaptureModeType.Intelligence;//6.3.0开始 只有智能拾取，忽略前端传递的类型
            Rectangle ret = Screen.PrimaryScreen.Bounds;
            this.ClientSize = new Size(ret.Width, ret.Height - 1);
            this.Location = new Point(ret.X, ret.Y);

            //this.Invoke(new Action(() =>
            //{
            isStartRemoteCapture = false;
            isSocket = false;
            isCheck = false;
            isCVParsing = false;
            IsStartInspectUI = false;
            //if (CurrentRemoteCaptureClient != null && CurrentRemoteCaptureClient.IsConnected)
            //    CurrentRemoteCaptureClient.Close();

            OverlayFormCaptureTask.Instance.Selector = selector;
            lastClickTime = DateTime.Now;
            HasDoSelect = false;
            ExceptionError = string.Empty;
            FindError = string.Empty;
            this.IsPause = string.Empty;
            if (inspectorStatus == null)
            {
                inspectorStatus = new InspectInfoWnd();
                inspectorStatus.SetWndShow();
            }

            inspectorStatus.InitCapture();

            inspectorStatus.IsPause = InspectStatus == 0 ? false : true;
            inspectorStatus.StartCheckCursorLoop();

            snappath = filePath;
            this.IsCVCapture = isCVCapture;
            this.TopMost = IsCVCapture;//覆盖桌面
            this.isRegionCaptureMode = false;
            this.IsCVSubCapture = false;
            this.CaptureRootWnd = null;
            CVServerURL = url;
            GlobleConfigurations.CVServer = url;
            hasTick = false;
            CurrentHighlightElement = null;
            this.ElementCaptureNode = null;
            this.CvCaptureRootNode = null;
            this.CaptureOperationData = new CaptureOperationDataModel();


            this.IsCvSupport = isCvSupport == 1;

            Task.Run(() =>
            {
                ReadyToCapture(isCVCapture);//刷新一些应用的元素状态 线程同步
                this.CurrentCaptureMode = captureModeType;
                inspectorStatus.ChangeCaptureStatusStyle();
            });


            //dispatcherTimer.Tick -= new EventHandler(DispatcherTimer_Tick);
            //dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);

            //dispatcherTimer.Interval = TimeSpan.FromMilliseconds(50);//3->30
            //dispatcherTimer.Start();

            StartHighLightFlow();
        }

        void ReadyToCapture(bool isCVCapture)
        {
            this.Invoke(() =>
            {
                logger.LogDebug("================Strat ReadyToCapture");
                if (!isCVCapture)
                {
                    GuiElement.ReadyToCapture();
                }
                logger.LogDebug("=================End ReadyToCapture");
            });

            //JavaNode.EnumJvms(true);//重刷JVM列表
            //this.Show();
            //currentDesktopScreenshot = CaptureDesktop();
        }
        bool isRegionCaptureMode = false;
        internal void StartRegionHighlight(string filePath)
        {
            //Size desktopSize = NativeMethods.DESKTOP;
            //this.ClientSize = new Size(desktopSize.Width * Screen.AllScreens.Length, desktopSize.Height - 1);
            Rectangle ret = Screen.PrimaryScreen.Bounds;
            this.ClientSize = new Size(ret.Width, ret.Height - 1);
            this.Location = new Point(ret.X, ret.Y);
            lastClickTime = DateTime.Now;
            isRegionCaptureMode = true;
            HasDoSelect = false;
            ExceptionError = string.Empty;
            FindError = string.Empty;
            this.IsPause = string.Empty;
            snappath = filePath;
            hasTick = false;
            IsStartInspectUI = false;
            isCVParsing = false;
            //InstallHook();
            CurrentHighlightElement = null;
            this.ElementCaptureNode = null;
            this.CvCaptureRootNode = null;
            if (inspectorStatus == null)
            {
                inspectorStatus = new InspectInfoWnd();
                inspectorStatus.SetWndShow();
            }
            inspectorStatus.InitCaptureByRegion("区域拾取");
            this.CaptureOperationData = new CaptureOperationDataModel();

            ///开启高亮线程
            StartHighLightFlow();
        }
        /***************/
        IGuiCVNode ParentContainer
        {
            get
            {
                return (IGuiCVNode)this.CvCaptureRootNode;
            }
            set
            {
                this.CvCaptureRootNode = value;
            }
        }

        internal bool IsCVCapture;        //CV Container拾取
        bool IsCVSubCapture;  //CV Container内子元素拾取
        string CVServerURL;
        internal bool isCVParsing;  //CV解析中，此时的鼠标移动不检测元素
        bool hasTick;
        internal bool IsAgentCapture; //agent tars助手模式下拾取
        internal Rectangle LastHighlightRect
        {
            get { return GuiElement.overlayForm.CurrentElementHighlighterManager.CurrentBorderRectangle; }
        }
        internal Rectangle LastTagInfoRect
        {
            get { return GuiElement.overlayForm.CurrentElementHighlighterManager.CurrentTagRectangle; }
        }


        public void InitCancellationForCapture()
        {
            if (CancellationForCapture != null)
            {
                if (!CancellationForCapture.IsCancellationRequested)
                {
                    CancellationForCapture.Cancel();
                }
                CancellationForCapture = null;
            }

            CancellationForCapture = new CancellationTokenSource();
        }

        /// <summary>
        /// 最新版本UI高亮线程 （主要功能未 根据普通元素拾取线程与CV元素拾取线程得到的结果 确定要高亮的元素 然后进行元素高亮）
        /// </summary>
        public void StartHighLightFlow()
        {
            InitCancellationForCapture();
            // Register before starting background work, including on cancel/reopen paths.
            InstallHook();
            IsStartInspectUI = true;
            logger.LogInformation("*** StartHighLightFlow Start ***");
            Task.Factory.StartNew(async () =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                while (true)
                {

                    if (GuiElement.overlayForm.CancellationForCapture.IsCancellationRequested)
                    {
                        break;
                    }
                    var tWait = Task.Delay(30);
                    //logger.LogDebug($"开始刷新高亮效果");
                    sw.Restart();
                    HighLightLoop();
                    //logger.LogDebug($"刷新高亮完成，总耗时：{sw.ElapsedMilliseconds}ms");
                    await tWait;
                    sw.Stop();
                    //logger.LogDebug($"刷新高亮后wait完成，总耗时：{sw.ElapsedMilliseconds}ms");
                }
            }, GuiElement.overlayForm.CancellationForCapture.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            logger.LogInformation("*** StartHighLightFlow End ***");
        }

        /// <summary>
        /// 最新版本 UI高亮逻辑
        /// </summary>
        #region HighLightLoop
        internal void HighLightLoop()
        {
            try
            {
                enablePassThrough = true;
                var screenPoint = System.Windows.Forms.Cursor.Position;
                inspectorStatus.MoveFromLocation(screenPoint);

                if (!IsCVCapture && !IsCVSubCapture && !isRegionCaptureMode)
                {
                    InspectUI(screenPoint);
                }
                else
                {
                    if (isCVParsing)
                    {
                        return;
                    }
                    if ((IsCVCapture == true && IsCVSubCapture == false) || isRegionCaptureMode == true)
                    {
                        //拾取背景
                        InspectCVHost(screenPoint, CVServerURL);
                    }
                    else
                    {
                        //拾取CV解析后的元素
                        if (!InspectCVElement(screenPoint, ParentContainer, Rectangle.Empty, out int _))
                        {
                            return;
                        }
                    }
                }
                if (CurrentHighlightElement == null)
                {
                    return;
                }

                ShowHighlightRect(screenPoint);
            }
            catch (RPAException ex)
            {
                ex.ComponentType = eComponentType.Capture;
                FindError = ex.Message;
                logger.LogInformation("UI线程异常 StopHighlight");
                //Java Chrome 插件未安装异常导致拾取退出 
                StopHighlight();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                ExceptionError = "未识别到元素，请重新拾取！".I18n("OverlayForm_507E922A");
            }
            finally
            {
                hasTick = true;
            }
        }
        #endregion


        /// <summary>
        /// 设置OverlayForm高亮框体内颜色 解决应用截图时 点击并拖拽时 背景依然深色的问题
        /// </summary>
        /// <param name="isActive"></param>
        #region 设置Overlayform高亮框体内框背景色
        public void SetPanelInsideBackGround(bool isActive)
        {

            //拾取CV解析后的元素
            this.Invoke(new Action(() =>
            {
                if (this.panelInside == null)
                {
                    return;
                }

                if (isActive)
                {
                    if (this.panelInside?.BackColor == Color.Transparent)
                    {
                        this.panelInside.BackColor = Color.FromArgb(126, 167, 255);//蓝色
                    }
                }
                else
                {
                    if (this.panelInside?.BackColor != Color.Transparent)
                    {
                        this.panelInside.BackColor = Color.Transparent;
                    }
                }
            }));
        }


        /// <summary>
        /// 设置OverlayForm高亮框体内颜色 解决应用截图时 点击并拖拽时 背景依然深色的问题
        /// </summary>
        /// <param name="isActive"></param>
        public void SetOpacity(double dOpacity)
        {
            //拾取CV解析后的元素
            this.Invoke(new Action(() =>
            {
                this.Opacity = dOpacity;
            }));
        }
        #endregion


        /// <summary>
        /// 高亮目标元素
        /// </summary>
        /// <param name="screenPoint"></param>
        #region ShowHighlightRect 高亮目标元素
        private void ShowHighlightRect(Point screenPoint)
        {
            try
            {
                ElementType eleType = ElementType.Ordinary;
                //logger.LogDebug("获取当前高亮元素区域");
                var rect = CurrentHighlightElement.BoundingRectangle;
                //logger.LogDebug("获取当前高亮元素区域 结束");

                //使用CV元素时需要考虑更元素偏移量
                if (!isRegionCaptureMode && CurrentHighlightElement is IGuiCVNode590 && ParentContainer != null)
                {
                    eleType = ElementType.Intelligence;
                    var rootWnd = GuiElement.overlayForm.CaptureRootWnd;
                    rect = new Rectangle(rect.X + rootWnd.BoundingRectangle.X, rect.Y + rootWnd.BoundingRectangle.Y, rect.Width, rect.Height);
                }
                //logger.LogDebug("CurrentHighlightElementRelativeClickPos 开始");
                //计算鼠标点击时的相对元素的偏移,以供后期有必要时使用
                CurrentHighlightElementRelativeClickPos = new Point(screenPoint.X - rect.Left, screenPoint.Y - rect.Top);
                //logger.LogInformation("获取ControlType");
                Task.Factory.StartNew(async () =>
                {
                    string _ControlName = string.Empty;
                    eGuiType _eGuiType = eGuiType.None;
                    GetCurrentElementParam(CurrentHighlightElement, eleType, ref _ControlName, ref _eGuiType);
                    RefreshHighLightTag(_ControlName, eleType, _eGuiType);
                }).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });


                GuiElement.overlayForm.LastHighlightElement = CurrentHighlightElement;

                HighLightParam CHighLightParam = new HighLightParam(rect, eleType);
                //TimeTrackHelper MoveRectTimeTrackHelper = new TimeTrackHelper();
                //MoveRectTimeTrackHelper.Start("MoveRect", TimeTrackLogLevel.info);
                //MoveRect(rect, CurrentHighlightElement.ControlType, eleType);
                MoveRectNew(CHighLightParam);
                //MoveRectTimeTrackHelper.Pick("MoveRect", TimeTrackLogLevel.info);
                //MoveRectTimeTrackHelper.Stop("MoveRect", TimeTrackLogLevel.info);
            }
            catch (Exception ex)
            {
                logger.LogError($"异常:{ex.ToString()}");
            }
        }
        #endregion

        /// <summary>
        /// 异步刷新高亮标签内容 避免卡UI线程
        /// </summary>
        /// <param name="_CurrentHighlightElement"></param>
        #region GetCurrentElementParam 获取高亮元素相关参数
        private void GetCurrentElementParam(IGuiNode _CurrentHighlightElement, ElementType eleType, ref string _ControlName, ref eGuiType _eGuiType)
        {
            _ControlName = _CurrentHighlightElement.ControlType;
            //logger.LogInformation("获取ControlType 结束");
            //TimeTrackHelper ShowHighlightRectTimeTrackHelper = new TimeTrackHelper();
            //ShowHighlightRectTimeTrackHelper.Start("GetGuiType", TimeTrackLogLevel.info);
            _eGuiType = GuiManager.GetGuiType(_CurrentHighlightElement);
            //ShowHighlightRectTimeTrackHelper.Pick("GetGuiType", TimeTrackLogLevel.info);
            //ShowHighlightRectTimeTrackHelper.Stop("GetGuiType", TimeTrackLogLevel.info);

            //logger.LogInformation("GetGuiType结束");
            //bool isExeplorer = CurrentElementInfoDealHelper.IsExplorer(_CurrentHighlightElement);//暂时不处理系统判断逻辑

            string _TargetName = string.Empty;
            //logger.LogInformation($"_TargetName:{_TargetName}");
            if (!string.IsNullOrEmpty(_ControlName))
            {
                _TargetName = CurrentElementInfoDealHelper.GetCNLabelForElement(_CurrentHighlightElement.GuiNodeControlName());
#if DEBUG
                _TargetName = $"{_TargetName} - 耗时:{OverlayFormCaptureTask.Instance.ElementCaptureTime}";
#endif
                logger.LogDebug($"");
                if (eleType != ElementType.Intelligence)
                {
                    if (!string.IsNullOrEmpty(OverlayFormCaptureTask.Instance.CaptureUnauthorizedAccessStr))
                    {
                        _TargetName = ($"{_TargetName} - {OverlayFormCaptureTask.Instance.CaptureUnauthorizedAccessStr}");
                    }
                }
            }

            _ControlName = _TargetName;
        }
        #endregion

        public void RefreshHighLightTag(string strControlName, ElementType _ElementType, eGuiType _eGuiType)
        {
            CurrentElementHighlighterManager.SetTagContent(strControlName);
            CurrentElementHighlighterManager.SetTagIcon(_ElementType, _eGuiType);
            CurrentElementHighlighterManager.SetBorderBorderBrush(_ElementType);
        }

        /// <summary>
        /// InspectUI
        /// </summary>
        /// <param name="screenPoint"></param>
        #region 根据当前点 确定目标高亮元素 InspectUI
        Stopwatch sw_InspectUI = null;
        private void InspectUI(Point screenPoint)
        {
            sw_InspectUI = sw_InspectUI ?? new Stopwatch();
            sw_InspectUI.Restart();
            bool useCVElement = true;
            Rectangle ecNodeRect = Rectangle.Empty;
            if (ElementCaptureNode != null)
            {
                ecNodeRect = ElementCaptureNode.BoundingRectangle;

                if (!ecNodeRect.IsEmpty && ecNodeRect.Width * ecNodeRect.Height <= Configurations.Info.CaptureElementChoose.MinWidth * Configurations.Info.CaptureElementChoose.MinHeight)
                {
                    useCVElement = false;
                }
                //logger.LogDebug($"++++++++++++++++ProcessName：{ElementCaptureNode.ProcessName}");
            }
            else if (CVElementProvider.Instance.CurrentCVCaptureNode == null)
            {
                CurrentHighlightElement = null;
                return;
            }

            var currentCVCaptureNode = CVElementProvider.Instance.CurrentCVCaptureNode;
            var normalContainsCVCount = CVElementProvider.Instance.NormalContainsCVCount;
            //按照一定的规则对比判断使用哪一种元素
            if (useCVElement && this.CvCaptureRootNode != null && currentCVCaptureNode != null)
            {
                //此范围内的CurrentHighlightElement 是最佳CV元素
                useCVElement = !ecNodeRect.Contains(screenPoint) || IsUseCVElement(ecNodeRect, currentCVCaptureNode, normalContainsCVCount);
                //不符合使用CV元素的规则，使用普通元素
                CurrentHighlightElement = useCVElement ? currentCVCaptureNode : ElementCaptureNode;
            }
            else
            {
                CurrentHighlightElement = ElementCaptureNode;
            }
            sw_InspectUI.Stop();
            //logger.LogDebug($"本次融合拾取元素挑选耗时：{sw_InspectUI.ElapsedMilliseconds}ms");
        }
        /// <summary>
        /// 是否使用CV元素作为高亮元素
        /// </summary>
        /// <param name="ecNodeRect"></param>
        /// <param name="cvGuiNode"></param>
        /// <param name="areaCount"></param>
        /// <returns></returns>
        private bool IsUseCVElement(Rectangle ecNodeRect, IGuiNode cvGuiNode, int areaCount)
        {
            logger.LogDebug($"开始执行元素挑选逻辑");
            bool useCVElement = true;
            int cvEleArea = 0, EleArea;
            Rectangle cheRect = Rectangle.Empty;
            if (cvGuiNode != null)
            {
                cheRect = cvGuiNode.BoundingRectangle;
                logger.LogDebug($"cvRect：{cheRect}");
                var rootWndBounding = GuiElement.overlayForm.CaptureRootWnd.BoundingRectangle;
                logger.LogDebug($"rootWndBounding：{cheRect}");
                cheRect.X += rootWndBounding.X;
                cheRect.Y += rootWndBounding.Y;
                cvEleArea = cheRect.Width * cheRect.Height;
            }
            EleArea = ecNodeRect.Width * ecNodeRect.Height;

            logger.LogDebug($"ecNodeRect：{ecNodeRect}");


            if (ecNodeRect.Width > 2 * Configurations.Info.CaptureElementChoose.MaxJammingPadding
                && ecNodeRect.Height > 2 * Configurations.Info.CaptureElementChoose.MaxJammingPadding)
            {
                //ecNodeRect.Inflate(-5, -5);
                ecNodeRect = ExcludeJammingArea(cheRect, ecNodeRect);
                logger.LogDebug($"ecNodeRect Inflate：{ecNodeRect}");

            }
            if (!ecNodeRect.IsEmpty)
            {
                EleArea = ecNodeRect.Width * ecNodeRect.Height;
            }
            var intersectArea = Rectangle.Intersect(cheRect, ecNodeRect);
            logger.LogDebug($"intersectArea：{intersectArea}");
            logger.LogDebug($"重叠区与去干扰区：{((1.0 * intersectArea.Width * intersectArea.Height) / EleArea)}");
            logger.LogDebug($"重叠区W与去干扰区W：{((1.0 * intersectArea.Width) / ecNodeRect.Width)}");
            logger.LogDebug($"重叠区H与去干扰区H：{((1.0 * intersectArea.Height) / ecNodeRect.Height)}");
            logger.LogDebug($"aceaCount:{areaCount}");

            if (areaCount == 0)
            {
                useCVElement = false;
                logger.LogDebug($"aceaCount == 0");
            }
            else if (!ecNodeRect.IsEmpty && cvEleArea >= EleArea)
            {
                useCVElement = false;
                logger.LogDebug($"cvEleArea >= EleArea");
            }
            else if (areaCount == 1)
            {
                //有60%以上 普通元素重叠区
                if (intersectArea.Width > ecNodeRect.Width * Configurations.Info.CaptureElementChoose.IntersectRatio)
                {
                    useCVElement = false;
                    logger.LogDebug($"intersectArea >= EleArea * {Configurations.Info.CaptureElementChoose.IntersectRatio}");

                }
                else if (intersectArea.Height > ecNodeRect.Height * Configurations.Info.CaptureElementChoose.IntersectRatio)
                {
                    useCVElement = false;
                    logger.LogDebug($"intersectArea >= EleArea * {Configurations.Info.CaptureElementChoose.IntersectRatio}");

                }
                else if (EleArea <= intersectArea.Width * intersectArea.Height * 3)
                {
                    useCVElement = false;
                    logger.LogDebug($"EleArea <= intersectArea * 3");
                }
            }
            logger.LogDebug($"useCVElement:{useCVElement}");
            return useCVElement;
        }
        private Rectangle ExcludeJammingArea(Rectangle cvEleRect, Rectangle normalEleRect)
        {
            int maxJammingPadding = Configurations.Info.CaptureElementChoose.MaxJammingPadding;
            int dX = cvEleRect.X - normalEleRect.X;
            int dY = cvEleRect.Y - normalEleRect.Y;
            int dR = normalEleRect.Right - cvEleRect.Right;
            int dB = normalEleRect.Bottom - cvEleRect.Bottom;
            dX = dX < 0 ? 0 : dX > maxJammingPadding ? maxJammingPadding : dX;
            dY = dY < 0 ? 0 : dY > maxJammingPadding ? maxJammingPadding : dY;
            dR = dR < 0 ? 0 : dR > maxJammingPadding ? maxJammingPadding : dR;
            dB = dB < 0 ? 0 : dB > maxJammingPadding ? maxJammingPadding : dB;
            normalEleRect.X += dX;
            normalEleRect.Y += dY;
            normalEleRect.Width -= dX + dR;
            normalEleRect.Height -= dY + dB;
            return normalEleRect;
        }
        #endregion


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

        internal void SetWindowType(string windowTypeName)
        {
            if (!inspectorStatus.CheckAccess())
            {
                //inspectorStatus.Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                //{
                //    inspectorStatus.WindowType(windowTypeName);
                //});
                inspectorStatus.Dispatcher.InvokeAsync(() =>
                {
                    inspectorStatus.WindowType(windowTypeName);
                });
            }
        }

        //public static RemoteCaptureClient CurrentRemoteCaptureClient = new RemoteCaptureClient();
        public static bool isStartRemoteCapture = false;
        public string address = string.Empty;
        public bool isSocket = false;

        private void InspectCVHost(Point screenPoint, string cvUrl)
        {
            /****************************/
            AutomationElement element = null;
            try
            {
                //this.Invoke(() =>
                //{
                //    if (!this.TopMost)
                //        this.TopMost = true;
                //});
                List<IntPtr> parentLinked = UiCommon.GetRootWindowList(screenPoint);
                foreach (IntPtr hWnd in parentLinked)
                {
                    if (hWnd != IntPtr.Zero)
                    {
                        element = UiCommon.UIAAutomation.FromHandle(hWnd);   //获取根窗体
                    }

                    CurrentHighlightElement = GuiManager.CreateGuiCVOperator().InspectNode(element);

                    if (element == null)
                    {
                        return;
                    }
                    if (element.BoundingRectangle.Contains(screenPoint))
                    {
                        //SetUIAMode(parentLinked.First(), screenPoint, element);
                        break;
                    }
                }
                //(CurrentHighlightElement).CacheRectangle = element.BoundingRectangle;
                GlobleConfigurations.CVServer = cvUrl;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }

        string selector = string.Empty;
        /****************/

        /// <summary>
        /// InspectCVElement 匹配符合鼠标点的CV元素
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="parentNode"></param>
        /// <param name="curOrdinaryRect"></param>
        /// <param name="areaCount"></param>
        /// <returns></returns>
        #region InspectCVElement
        private bool InspectCVElement(Point screenPoint, IGuiCVNode parentNode, Rectangle curOrdinaryRect, out int areaCount)
        {
            //logger.LogDebug("===========start 匹配符合鼠标点的CV元素");
            IGuiCVOperator650 guiCVOperator = GuiManager.CreateGuiCVOperator();
            areaCount = 0;
            Dictionary<string, object> extendParams = new Dictionary<string, object>();
            extendParams["areaRect"] = curOrdinaryRect;
            extendParams["areaCount"] = 0;
            if (GuiElement.overlayForm.CaptureRootWnd == null)
            {
                return false;
            }
            var cvGuiNode = GuiManager.CreateCVGuiNode(GuiElement.overlayForm.CaptureRootWnd);
            bool result = guiCVOperator.InspectNode(screenPoint, cvGuiNode, selector, extendParams, out IGuiNode inspectNode);
            logger.LogDebug($"areaCount=={extendParams["areaCount"]}");
            //logger.LogDebug("3-1-2");
            //logger.LogDebug("===========end 匹配符合鼠标点的CV元素");
            if (result == true && areaCount != 1)
            {
                CurrentHighlightElement = inspectNode;
                int.TryParse(extendParams["areaCount"].ToString(), out areaCount);
                return true;
            }
            return false;
        }
        #endregion


        /// <summary>
        /// StopHighlight 停止元素高亮
        /// </summary>
        #region StopHighlight 停止元素高亮
        internal void StopHighlight()
        {
            logger.LogDebug("StopHighlight。。。。");
            try
            {
                OverlayFormCaptureTask.Instance.StopAfterExecute();
                CVCaptureTask.Instance.StopAfterExecute();
                GuiElement.overlayForm.CvCaptureRootNode = null;
                GuiElement.overlayForm.ElementCaptureNode = null;

            }
            catch
            {

            }

            try
            {
                UninstallHook();
                GuiElement.overlayForm.CancellationForCapture?.Cancel();
                dispatcherTimer?.Stop();
                dealySelectDispatcherTimer.Stop();
                keyStateDispatcherTimer.Stop();

                this.Invoke(new Action(() =>
                {
                    //Console.WriteLine($"do hide form");
                    SetPanelInsideBackGround(true);
                    //panelBorderLeft.Size = panelBorderTop.Size = panelBorderRight.Size = panelBorderBottom.Size = panelInside.Size = new Size(0, 0);
                    CurrentElementHighlighterManager?.SetBackgroundActive(false);
                    CurrentElementHighlighterManager?.Close(true);
                    this.Hide();
                }));
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "StopHighlight Exception");
            }
            logger.LogDebug("StopHighlight Eed");
        }
        #endregion

        internal void UninstallHook()
        {
            try
            {
                _inputDispatcher.Invoke(() =>
                {
                    if (!_mouseHookInstalled)
                    {
                        return;
                    }
                    logger.LogDebug($"UninstallHook thread:{Thread.CurrentThread.ManagedThreadId}, ownerThread:{_inputDispatcher.Thread.ManagedThreadId}, IsHandleCreated:{IsHandleCreated}");
                    try
                    {
                        HookManager.MouseClickExt -= HookManager_MouseClickExt;
                    }
                    finally
                    {
                        // HookManager removes the subscriber and resets its native handle even
                        // when Windows reports that the hook was already invalidated.
                        _mouseHookInstalled = false;
                    }
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "UninstallHook error");
            }
        }
        static readonly object objLock = new object();
        internal string DoSelect()
        {
            try
            {
                logger.LogDebug("DoSelect begin");
                CancellationForCapture?.Cancel();
                lock (objLock)
                {
                    dispatcherTimer.Stop();
                    try
                    {
                        CVCaptureTask.Instance.StopAfterExecute();
                        OverlayFormCaptureTask.Instance.StopAfterExecute();
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "停止拾取服务异常");
                    }
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
                            var element = new GuiElement(CurrentHighlightElement)
                            {
                                RelativeClickPos = CurrentHighlightElementRelativeClickPos
                            };

                            //隐藏窗体并截全屏，以便后期自己绘制红色标记矩形
                            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                            sw.Start();
                            this.Invoke(new Action(() => { this.Opacity = 0; }));
                            //隐藏UI效果截图
                            CurrentElementHighlighterManager?.Close(true);

                            if (CurrentHighlightElement is IGuiCVNode)
                            {
                                element.currentInformativeScreenshot = ((IGuiCVNode)CurrentHighlightElement).CVSubElementToBitmap(snappath, ParentContainer.CacheImage);
                            }
                            else
                            {
                                using (currentDesktopScreenshot = CaptureDesktop())
                                {
                                    element.currentInformativeScreenshot = element.CaptureInformativeScreenshot(snappath, currentDesktopScreenshot);
                                }
                                //为ai智能修复保存数据，方法需包含内部容错
                                SaveAPPFaceImg();
                            }
                            sw.Stop();
                            element.screenShotPath = snappath;
                            try
                            {
                                logger.LogDebug($"get selector");
                                selector = GuiElement.OnSelected?.Invoke(element);
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
                            StopHighlight();
                            inspectorStatus.StopCheckCursorLoop();
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
                SelectorError = eCommonMessageType.DataCollectOverlayWnd_xaml_cs_ShiQuYuanSuCaoZuoShiBaiQing.GetMessage();
                return "";
            }
        }
        //internal void Client_DataReceived(object sender, DataEventArgs e)
        //{
        //    JArray ja = new JArray();
        //    string result = Encoding.UTF8.GetString(e.Data, 0, e.Length).Trim();
        //    try
        //    {
        //        logger.LogDebug("远程拾取功能返回值：" + result);
        //        //var msgStr = result.Substring(0, result.LastIndexOf('|'));
        //        JMessage msg = result.ToObject<JMessage>();
        //        if (msg.Code == 0)
        //        {
        //            var selector = RecoverViewData.RestoreViewData(msg.Info);
        //            ja = JArray.Parse(selector);
        //            try
        //            {
        //                ja[0]["remoteName"] = "mstsc";
        //                ja[0]["remoteAddress"] = address;
        //                var imgBase64 = ja[0]["filePath"]?.ToString();
        //                using (var img = ImageHelper.Base642Bitmap(imgBase64))
        //                {
        //                    string dirName = Path.GetDirectoryName(snappath);
        //                    if (!Directory.Exists(dirName))
        //                    {
        //                        Directory.CreateDirectory(dirName);
        //                    }
        //                    img.Save(this.snappath);
        //                }
        //                ja[0]["filePath"] = this.snappath;
        //            }
        //            catch (Exception)
        //            {
        //                logger.LogDebug("远程拾取图标base64转为图片异常");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex);
        //    }

        //    new Action<string>(DoRemoteSelect).BeginInvoke(ja.ToString(), null, null);
        //}
        //internal void DoRemoteSelect(string selector)
        //{
        //    logger.LogDebug("DoRemoteSelect begin");
        //    lock (objLock)
        //    {
        //        if (HasDoSelect)
        //        {
        //            return;
        //        }
        //        HasDoSelect = true;
        //        GuiElement.OnSelectored?.Invoke(selector);
        //        StopHighlight();
        //    }
        //}
        DateTime lastClickTime;
        private object obj = new object();
        private bool isCheck = false;
        private void HookManager_MouseClickExt(object sender, MouseEventExtArgs e)
        {
            try
            {
                MouseLastClicPoint = Cursor.Position;
                logger.LogDebug("enter_MouseClickExt ");
                if (CurrentHighlightElement == null)
                {
                    logger.LogDebug("CurrentHighlightElement is null");
                    if (Configurations.Info.EnableRemoteCapture && isStartRemoteCapture && Control.ModifierKeys == Keys.Control && e.Button == MouseButtons.Left)
                    {
                        lock (obj)
                        {
                            try
                            {
                                if (isCheck)
                                    return;
                                isCheck = true;
                                //CurrentRemoteCaptureClient.DataReceived -= Client_DataReceived;
                                //CurrentRemoteCaptureClient.DataReceived += Client_DataReceived;
                                logger.LogDebug("start DoRemoteCapture");
                                //CurrentRemoteCaptureClient.DoRemoteCapture(address);
                                logger.LogDebug("end DoRemoteCapture");
                            }
                            catch (Exception)
                            {
                                logger.LogDebug("发送远程拾取指令异常");
                            }
                        }
                        logger.LogDebug("_DoRemoteCapture ");
                        return;
                    }
                }
                logger.LogDebug("MouseClickExt");
                if (hasTick == false)
                {
                    return;
                }
                var span = (DateTime.Now - lastClickTime).TotalMilliseconds;
                lastClickTime = DateTime.Now;
                if (span < 200)
                {
                    e.Handled = Control.ModifierKeys == Keys.Control || Control.ModifierKeys == Keys.Shift;
                    return;
                }
                switch (e.Button)
                {
                    case MouseButtons.Left:

                        logger.LogDebug("MouseClickExt——MouseButtons.Left");
                        //if (ModifierKeys != Keys.Control && ModifierKeys != Keys.Shift)
                        //{
                        //    //是CV模式，且是CV的二次拾取,则不需要Ctrl
                        //    if (CurrentCaptureMode != CaptureModeType.CV || !IsCVSubCapture)
                        //    {
                        //        e.Handled = false;
                        //        return;
                        //    }
                        //    else
                        //    {
                        //        GuiElement.overlayForm.CaptureOperationData.trigger_condition = "单击";
                        //    }
                        //}
                        //else
                        //{
                        //    GuiElement.overlayForm.CaptureOperationData.trigger_condition = "Ctrl+单击";
                        //}
                        if (ModifierKeys == Keys.Control)
                        {
                            GuiElement.overlayForm.CaptureOperationData.trigger_condition = "Ctrl+单击";
                            UseCaptureMode = CaptureModeType.Intelligence;
                        }
                        else if (ModifierKeys == Keys.Shift)
                        {
                            GuiElement.overlayForm.CaptureOperationData.trigger_condition = "Shift+单击";
                            UseCaptureMode = CaptureModeType.FloatWnd;
                        }
                        else
                        {
                            e.Handled = false;
                            return;
                        }
                        logger.LogDebug("MouseClickExt——Keys.Control");
                        if (!IsCVCapture && !IsCVSubCapture && !isRegionCaptureMode)
                        {
                            if (!IsAgentCapture)
                            {
                                dispatcherTimer.Stop();
                                //logger.LogDebug("MouseClickExt——dispatcherTimer.Stop();");
                                //ElementCaptureTask.Instance.StopAfterExecute();
                                //logger.LogDebug("MouseClickExt——ElementCaptureTask.StopAfterExecute();");
                                //CVCaptureTask.Instance.StopAfterExecute();
                                //logger.LogDebug("MouseClickExt——CVCaptureTask.StopAfterExecute();");
                                logger.LogDebug("MouseClickExt——start DoSelect Task.Run");
                                _ = Task.Run(DoSelect);
                                logger.LogDebug("MouseClickExt——end DoSelect Task.Run");
                            }
                            //tars助手模式下拾取
                            else
                            {
                                dispatcherTimer.Stop();
                                logger.LogDebug("MouseClickExt——start DoAgentElementSelect Task.Run");
                                _ = Task.Run(DoAgentElementSelect);
                                logger.LogDebug("MouseClickExt——end DoAgentElementSelect Task.Run");
                            }
                        }
                        else if (isRegionCaptureMode)
                        {
                            logger.LogDebug("MouseClickExt——start DoCVTextSelect Task.Run");
                            _ = Task.Run(DoCVTextSelect);
                        }
                        else
                        {
                            if (IsCVCapture)
                            {
                                logger.LogDebug("MouseClickExt——start DoCVSelect Task.Run");

                                _ = Task.Run(DoCVSelect);
                                inspectorStatus.HideSwitchPanle();
                            }
                            else
                            {
                                logger.LogDebug("MouseClickExt——start DoCvElementSelect Task.Run");
                                _ = Task.Run(DoCvElementSelect);
                            }
                        }
                        e.Handled = true;
                        break;
                    //case MouseButtons.Right:
                    //    client.EndRemoteCapture();
                    //    DoCancel();
                    //    e.Handled = true;
                    //    break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取过程中 鼠标点击发生了异常");
                SelectorError = "拾取程序开小差了，请重试一下吧！".I18n("OverlayForm_9897E35A");
                StopHighlight();
            }
        }
        /*********************/
        private void DoCVTextSelect()
        {
            try
            {
                CancellationForCapture?.Cancel();

                lock (objLock)
                {
                    if (HasDoSelect)
                    {
                        return;
                    }
                    HasDoSelect = true;

                    if (CurrentHighlightElement != null)
                    {
                        var element = new GuiElement(CurrentHighlightElement)
                        {
                            RelativeClickPos = CurrentHighlightElementRelativeClickPos
                        };
                        this.Invoke(new Action(() =>
                        {
                            SetPanelInsideBackGround(false);
                            CurrentElementHighlighterManager?.SetBackgroundActive(true);
                            if (panelBorderLeft != null)
                            {
                                panelBorderLeft.Size = panelBorderTop.Size = panelBorderRight.Size = panelBorderBottom.Size = panelInside.Size = new Size(0, 0);

                            }
                        }));
                        ParentContainer = (IGuiCVNode)CurrentHighlightElement;
                        inspectorStatus.SetWndHide();
                        element.currentInformativeScreenshot = ParentContainer.ElementToBitmap();
                        inspectorStatus.SetWndShow();
                        ParentContainer.CacheImage = element.currentInformativeScreenshot;
                        GuiElement.OnSelected?.Invoke(element);
                        StopHighlight();
                        //isCVParsing = true;
                        //CV.CVScreenWnd wnd = new CVScreenWnd(ParentContainer.BoundingRectangle, element.currentInformativeScreenshot, snappath);
                        //if (wnd.ShowDialog() == DialogResult.OK)
                        //{
                        //    Rectangle r = new Rectangle();
                        //    Point start = wnd.startPoint;
                        //    Point end = wnd.endPoint;

                        //    r.X = start.X;
                        //    r.Y = start.Y;

                        //    r.Width = end.X - start.X;
                        //    r.Height = end.Y - start.Y;



                        //    JObject locationNode = new JObject();
                        //    locationNode.Add("x", r.X);
                        //    locationNode.Add("y", r.Y);
                        //    locationNode.Add("width", r.Width);
                        //    locationNode.Add("height", r.Height);

                        //    ParentContainer.Add(new SelectorItem(locationNode));


                        //}
                        //else
                        //{
                        //    DoCancel();
                        //}

                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "DoCVTextSelect error");
                SelectorError = "拾取程序开小差了，请重试一下吧！".I18n("OverlayForm_9897E35A");
            }
        }
        internal void SetWndHide()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        SetWndHide();
                    });
                    return;
                }
                this.Opacity = 0;
                //HightLightWndHideParam hightLightWndHideParam = new HightLightWndHideParam();
                //hightLightWndHideParam.IsAsync = false;
                //CurrentElementHighlighterManager?.Close(hightLightWndHideParam);
                CurrentElementHighlighterManager?.Hide();
            }
            catch (Exception ex)
            {
                logger.LogDebug("设置状态窗口隐藏异常，忽略");
            }
        }
        internal void SetWndShow()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        SetWndShow();
                    });
                    return;
                }
                //this.Opacity = 0.99;
                CurrentElementHighlighterManager?.UndoHide();
            }
            catch (Exception ex)
            {
                logger.LogDebug("设置状态窗口显示异常，忽略");
            }
        }
        private void DoCVSelect()
        {
            try
            {
                logger.LogDebug("enter DoCVSelect");
                isCVParsing = true;
                lock (objLock)
                {
                    if (HasDoSelect)
                    {
                        return;
                    }
                    HasDoSelect = true;

                    if (CurrentHighlightElement != null)
                    {
                        var element = new GuiElement(CurrentHighlightElement)
                        {
                            RelativeClickPos = CurrentHighlightElementRelativeClickPos
                        };
                        this.Invoke(new Action(() =>
                        {
                            SetPanelInsideBackGround(false);
                            CurrentElementHighlighterManager?.SetBackgroundActive(false);
                            if (panelBorderLeft != null)
                            {
                                panelBorderLeft.Size = panelBorderTop.Size = panelBorderRight.Size = panelBorderBottom.Size = panelInside.Size = new Size(0, 0);
                            }
                        }));
                        ParentContainer = (IGuiCVNode)CurrentHighlightElement;
                        inspectorStatus.SetWndHide();
                        element.currentInformativeScreenshot = CurrentHighlightElement.ElementToBitmap();
                        inspectorStatus.SetWndShow();
                        if (element.currentInformativeScreenshot == null)
                        {
                            ExceptionError = "CV元素解析失败，截图发生异常，请稍后重试".I18n("OverlayForm_E994705E");
                            StopHighlight();
                            isCVParsing = false;
                            return;
                        }
                        ParentContainer.CacheImage = element.currentInformativeScreenshot;
                        Thread t = new Thread(() =>
                            {
                                try
                                {
                                    if (((IGuiCVNode590)(CurrentHighlightElement)).CVParseAndWait(element.currentInformativeScreenshot, element.WindowHandle)) //CV解析
                                    {
                                        logger.LogDebug(" ========== DoCVSelect GetCVELements success");

                                        isCVParsing = false;//解析完成后将解析的文件加载到内存中
                                        IsCVCapture = false;
                                        IsCVSubCapture = true;
                                        HasDoSelect = false;
                                    }
                                    else
                                    {
                                        logger.LogDebug(" ========== DoCVSelect GetCVELements error");
                                        ExceptionError = eCommonMessageType.OverlayForm_cs_CVYuanSuJieXiShiBaiQing.GetMessage();
                                        StopHighlight();
                                        isCVParsing = false;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex);
                                    //CVException cvExcetion = null;
                                    //if (ex is CVException)
                                    //{
                                    //    cvExcetion = ex as CVException;
                                    //}
                                    //else
                                    //{
                                    //    cvExcetion = new CVException(eErrorMessageType.Error_CVCaptureError, eComponentType.Capture, ex);
                                    //}
                                    if (ex is RPAException)
                                    {
                                        ExceptionError = ((RPAException)ex).ErrorMessage;
                                    }
                                    else
                                    {
                                        ExceptionError = "未识别到元素，请重新拾取！".I18n("OverlayForm_507E922A");
                                    }
                                    //ExceptionError = ex.Message;
                                    //ExceptionError = "未识别到元素，请重新拾取！";
                                    StopHighlight();
                                    isCVParsing = false;
                                }
                                Thread.CurrentThread.Interrupt();
                                logger.LogDebug(" ========== DoCVSelect thread end");

                            })
                        {
                            IsBackground = true
                        };
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start();
                        t.Join();
                        logger.LogDebug($"DoCVSelect thread state:{t.ThreadState}");
                    }
                }
                logger.LogDebug(" ========== dispose objlock");

            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "DoCVSelect Error");
                SelectorError = "拾取执行异常，请重试".I18n("OverlayForm_39858057");
            }

            isCVParsing = false;
            logger.LogDebug("Exit DoCVSelect");

        }
        public string ExceptionError { get; set; }
        public string FindError { get; set; }

        private void DoCvElementSelect()
        {
            try
            {
                CancellationForCapture?.Cancel();
                logger.LogDebug("enter DoCvElementSelect");
                /*******************/
                lock (objLock)
                {
                    if (HasDoSelect)
                    {
                        return;
                    }
                    HasDoSelect = true;
                    if (CurrentHighlightElement != null)
                    {
                        logger.LogDebug("Start DoCvElementSelect");
                        var element = new GuiElement(CurrentHighlightElement);
                        element.RelativeClickPos = CurrentHighlightElementRelativeClickPos;
                        logger.LogDebug("Start CVSubElementToBitmap");
                        element.currentInformativeScreenshot = ((IGuiCVNode)CurrentHighlightElement).CVSubElementToBitmap(snappath, ParentContainer.CacheImage);
                        logger.LogDebug("end CVSubElementToBitmap");
                        element.screenShotPath = snappath;
                        logger.LogDebug("Start OnSelected");
                        GuiElement.OnSelected?.Invoke(element);
                        logger.LogDebug("End OnSelected");
                        StopHighlight();
                    }
                }

            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"DoCvElementSelect Error");
                SelectorError = "拾取元素执行异常，请重试".I18n("OverlayForm_9AF72306");
            }
        }

        /// <summary>
        /// agent tars模式下元素拾取
        /// </summary>
        /// <returns></returns>
        internal string DoAgentElementSelect()
        {
            try
            {
                logger.LogDebug("DoAgentElementSelect begin");
                CancellationForCapture?.Cancel();
                lock (objLock)
                {
                    dispatcherTimer.Stop();
                    try
                    {
                        CVCaptureTask.Instance.StopAfterExecute();
                        OverlayFormCaptureTask.Instance.StopAfterExecute();
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "停止拾取服务异常");
                    }
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
                            var element = new GuiElement(CurrentHighlightElement)
                            {
                                RelativeClickPos = CurrentHighlightElementRelativeClickPos
                            };

                            //隐藏窗体并截全屏，以便后期自己绘制红色标记矩形
                            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                            sw.Start();
                            this.Invoke(new Action(() => { this.Opacity = 0; }));
                            ////隐藏UI效果截图
                            //CurrentElementHighlighterManager?.Close(true);
                            //显示元素操作功能栏
                            // inspectorStatus.StopCheckCursorLoop();
                            CurrentElementHighlighterManager?.ShowTarsToolsPanel();
                            CurrentElementHighlighterManager?.SetZoomControlsEnableStatus(CurrentHighlightElement is ElementGuiNode);
                            if (CurrentHighlightElement is IGuiCVNode)
                            {
                                element.currentInformativeScreenshot = ((IGuiCVNode)CurrentHighlightElement).CVSubElementToBitmap(snappath, ParentContainer.CacheImage);
                            }
                            else
                            {
                                using (currentDesktopScreenshot = CaptureDesktop())
                                {
                                    element.currentInformativeScreenshot = element.CaptureInformativeScreenshot(snappath, currentDesktopScreenshot);
                                }
                                //为ai智能修复保存数据，方法需包含内部容错
                                SaveAPPFaceImg();
                            }
                            sw.Stop();
                            element.screenShotPath = snappath;
                            try
                            {
                                logger.LogDebug($"get selector");
                                elementsCache.Add(new NodeInfo { SelfNode = CurrentHighlightElement });
                                //agent拾取模式下不需要ctrl+单击就构建元素结构，只保留element对象即可
                                //selector = GuiElement.OnSelected?.Invoke(element);
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
                            //OverlayFormCaptureTask.Instance.Pause();
                            //CVCaptureTask.Instance.Pause();
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "DoAgentElementSelect Error");
                SelectorError = eCommonMessageType.DataCollectOverlayWnd_xaml_cs_ShiQuYuanSuCaoZuoShiBaiQing.GetMessage();
                return "";
            }
        }

        List<ExtendedPanel> left = new List<ExtendedPanel>();
        List<ExtendedPanel> top = new List<ExtendedPanel>();
        List<ExtendedPanel> right = new List<ExtendedPanel>();
        List<ExtendedPanel> bottom = new List<ExtendedPanel>();
        List<ExtendedPanel> Inside = new List<ExtendedPanel>();
        public void MoveRects(List<Rectangle> rectangles)
        {
            int margin = 2;
            selectorlabel.Text = "";
            panelTarget.Location = new Point(-100, -100);
            Color borderColor = Color.FromArgb(255, 255, 137, 16);
            Color insideColor = Color.FromArgb(128, 255, 192, 192);

            this.Opacity = 0.99;

            if (rectangles.Count > 0)
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                for (int i = 0; i < rectangles.Count; i++)
                {
                    var rect = rectangles[i];
                    left.Add(CreatePanel(borderColor));
                    left[i].Location = new Point(rect.Left, rect.Top);
                    left[i].Size = new Size(margin, rect.Height);
                    top.Add(CreatePanel(borderColor));
                    top[i].Location = new Point(rect.Left, rect.Top);
                    top[i].Size = new Size(rect.Width, margin);
                    right.Add(CreatePanel(borderColor));
                    right[i].Location = new Point((rect.Left + rect.Width - margin), rect.Top);
                    right[i].Size = new Size(margin, rect.Height);
                    bottom.Add(CreatePanel(borderColor));
                    bottom[i].Location = new Point(rect.Left, (rect.Top + rect.Height - margin));
                    bottom[i].Size = new Size(rect.Width, margin);
                    Inside.Add(CreatePanel(insideColor));
                    Inside[i].Location = new Point(rect.Left + margin, rect.Top + margin);
                    Inside[i].Size = new Size(rect.Width - margin * 2, rect.Height - margin * 2);
                }
                stopWatch.Stop();
                logger.LogDebug("元素范围渲染时长: " + stopWatch.Elapsed);
            }
            else
            {
                for (int i = 0; i < left.Count; i++)
                {
                    left[i].Location = new Point(0, 0);
                    left[i].Size = new Size(0, 0);

                    top[i].Location = new Point(0, 0);
                    top[i].Size = new Size(0, 0);

                    right[i].Location = new Point(0, 0);
                    right[i].Size = new Size(0, 0);

                    bottom[i].Location = new Point(0, 0);
                    bottom[i].Size = new Size(0, 0);

                    //Inside[i].Location = new Point(0, 0);
                    //Inside[i].Size = new Size(0, 0);
                }
            }
        }

        public void MoveRectsNew(List<Rectangle> rectangles, bool useTightBorder = false)
        {
            rectangles = rectangles.Where(r => r.Width > 0 && r.Height > 0).ToList();
            if (rectangles.Count < 1)
            {
                return;
            }
            ShowRectsWnd showRectsWnd = new ShowRectsWnd();
            showRectsWnd.Topmost = true;
            //Task.Factory.StartNew(() => {
            //    HighLighterFlashParam highLighterFlashParam = new HighLighterFlashParam();
            //    highLighterFlashParam.FlashTimes = 3;
            //    highLighterFlashParam.FlashInterval = 150;
            //    highLighterFlashParam.BorderBrushCode = CurrentElementHighlighterManager.BorderBrushColorCode;
            //    showRectsWnd.SetParamRectsAndFlash(rectangles, highLighterFlashParam);
            //});
            HighLighterFlashParam highLighterFlashParam = new HighLighterFlashParam();
            highLighterFlashParam.FlashTimes = 3;
            highLighterFlashParam.FlashInterval = 150;
            highLighterFlashParam.BorderBrushCode = GlobleConfigurations.IsAiRepaireOfVerify ? HighLightIconTraitor.AIREPAIRE_BORDER_COLOR : HighLightIconTraitor.NATIVE_BORDER_COLOR;
            highLighterFlashParam.UseTightBorder = useTightBorder;
            showRectsWnd.SetParamRectsAndFlash(rectangles, highLighterFlashParam);
            showRectsWnd.ShowDialog();
        }



        public void MoveRect(Rectangle rect, string targetName = "", ElementType eleType = ElementType.None)
        {
            //logger.LogDebug($"MoveRect 高亮框开始");

            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            logger.LogInformation("高亮框开始");
            TimeTrackHelper ShowHighlightRectTimeTrackHelper = new TimeTrackHelper();
            ShowHighlightRectTimeTrackHelper.Start("GetGuiType", TimeTrackLogLevel.info);
            eGuiType aGuiType = GuiManager.GetGuiType(CurrentHighlightElement);
            ShowHighlightRectTimeTrackHelper.Pick("GetGuiType", TimeTrackLogLevel.info);
            ShowHighlightRectTimeTrackHelper.Stop("GetGuiType", TimeTrackLogLevel.info);

            logger.LogInformation("GetGuiType结束");
            bool isExeplorer = CurrentElementInfoDealHelper.IsExplorer(CurrentHighlightElement);//暂时不处理系统判断逻辑

            string _TargetName = string.Empty;
            if (!string.IsNullOrEmpty(targetName))
            {
                _TargetName = CurrentElementInfoDealHelper.GetCNLabelForElement(CurrentHighlightElement.GuiNodeControlName());
#if DEBUG
                _TargetName = $"{_TargetName} - 耗时:{OverlayFormCaptureTask.Instance.ElementCaptureTime}";
#endif
                if (eleType != ElementType.Intelligence)
                {
                    if (!string.IsNullOrEmpty(OverlayFormCaptureTask.Instance.CaptureUnauthorizedAccessStr))
                    {
                        _TargetName = ($"{_TargetName} - {OverlayFormCaptureTask.Instance.CaptureUnauthorizedAccessStr}");
                    }
                }
            }

            //stopwatch.Stop();
            //logger.LogDebug($"先期耗时:{stopwatch.ElapsedMilliseconds}");
            //stopwatch.Restart();
            //logger.LogDebug($"rect:{rect.X} {rect.Y} {rect.Width} {rect.Height} targetName:{targetName} eleType:{eleType.ToString()} aGuiType:{aGuiType} ");

            //CurrentElementHighlighterManager.Show(rect,
            //_TargetName,
            //eleType,
            //aGuiType,
            //isExeplorer,
            //0);

            GuiElement.overlayForm.LastHighlightElement = CurrentHighlightElement;

            HighLightParam CHighLightParam = new HighLightParam(rect, _TargetName, eleType, aGuiType);
            CHighLightParam.UseTightBorder = aGuiType == eGuiType.SAP;
            logger.LogInformation("HighLight 开始");
            TimeTrackHelper HighLightTimeTrackHelper = new TimeTrackHelper();
            HighLightTimeTrackHelper.Start("HighLight", TimeTrackLogLevel.info);
            CurrentElementHighlighterManager.HighLight(CHighLightParam);
            HighLightTimeTrackHelper.Pick("HighLight", TimeTrackLogLevel.info);
            HighLightTimeTrackHelper.Stop("HighLight", TimeTrackLogLevel.info);
            logger.LogInformation("HighLight 结束");


            //TimeTrackHelper SetBackgroundActiveTimeTrackHelper = new TimeTrackHelper();
            //SetBackgroundActiveTimeTrackHelper.Start("SetBackgroundActive", TimeTrackLogLevel.info);

            if (ModifierKeys == Keys.Control || ModifierKeys == Keys.Shift)
            {
                CurrentElementHighlighterManager.SetBackgroundActive(true);
            }
            else
            {
                CurrentElementHighlighterManager.SetBackgroundActive(false);
            }
            //SetBackgroundActiveTimeTrackHelper.Pick("SetBackgroundActive", TimeTrackLogLevel.info);
            //SetBackgroundActiveTimeTrackHelper.Stop("SetBackgroundActive", TimeTrackLogLevel.info);

            logger.LogInformation("高亮框结束");
        }

        public void MoveRectNew(HighLightParam CHighLightParam)
        {

            //logger.LogInformation("HighLight 开始");
            //TimeTrackHelper HighLightTimeTrackHelper = new TimeTrackHelper();
            //HighLightTimeTrackHelper.Start("HighLight", TimeTrackLogLevel.info);
            CurrentElementHighlighterManager.HighLight(CHighLightParam);
            //HighLightTimeTrackHelper.Pick("HighLight", TimeTrackLogLevel.info);
            //HighLightTimeTrackHelper.Stop("HighLight", TimeTrackLogLevel.info);
            //logger.LogInformation("HighLight 结束");

            //TimeTrackHelper SetBackgroundActiveTimeTrackHelper = new TimeTrackHelper();
            //SetBackgroundActiveTimeTrackHelper.Start("SetBackgroundActive", TimeTrackLogLevel.info);

            if (ModifierKeys == Keys.Control || ModifierKeys == Keys.Shift)
            {
                CurrentElementHighlighterManager.SetBackgroundActive(true);
            }
            else
            {
                CurrentElementHighlighterManager.SetBackgroundActive(false);
            }
            //SetBackgroundActiveTimeTrackHelper.Pick("SetBackgroundActive", TimeTrackLogLevel.info);
            //SetBackgroundActiveTimeTrackHelper.Stop("SetBackgroundActive", TimeTrackLogLevel.info);

        }


        internal void InstallHook()
        {
            logger.LogDebug($"GlobleConfigurations.IsRemoteCaptureServer:{GlobleConfigurations.IsRemoteCaptureServer}");
            //如果是远程拾取服务端，不注册鼠标事件
            if (GlobleConfigurations.IsRemoteCaptureServer)
            {
                logger.LogDebug("远程拾取服务端，不注册鼠标事件");
                return;
            }
            _inputDispatcher.Invoke(() =>
            {
                if (_mouseHookInstalled)
                {
                    return;
                }
                // Create the handle on its owning UI thread, never on the highlighter worker.
                var windowHandle = this.Handle;
                logger.LogDebug($"InstallHook thread:{Thread.CurrentThread.ManagedThreadId}, ownerThread:{_inputDispatcher.Thread.ManagedThreadId}, IsHandleCreated:{IsHandleCreated}, windowHandle:{windowHandle}");
                HookManager.MouseClickExt += HookManager_MouseClickExt;
                _mouseHookInstalled = true;
                logger.LogDebug("InstallHook mouse subscription completed");
            });
        }
        internal void Invoke(Action action)
        {
            if (this.InvokeRequired)
            {
                this?.Invoke((Delegate)action);
            }
            else
            {
                action();
            }
        }

        private void OverlayForm_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible == false)
            {
                CurrentElementHighlighterManager?.Hide();
            }
        }
        #region 给特殊应用 刷一遍元素  ，目前有：千牛
        private void BrushAppElement(int processId, Point screenPoint)
        {
            screenPoint = new Point(-1, -1);//设置特殊坐标，此方法中最终使用特表坐标标识 刷所有节点
            try
            {
                var desktop = UiCommon.UIAAutomation.GetDesktop();
                foreach (var item in desktop.FindAllChildren())
                {
                    if (item.Properties.ProcessId.IsSupported)
                    {
                        if (processId == item.Properties.ProcessId)
                        {
                            BrushInnerChilds(item, screenPoint);
                        }
                    }
                }
            }
            catch
            {

            }
        }

        private void BrushInnerChilds(AutomationElement element, Point point)
        {
            try
            {
                var ele = UiCommon.TreeWalker.GetFirstChild(element);
                bool isContainsPoint = (point.X < 0 && point.Y < 0);
                while (ele != null)
                {
                    if (isContainsPoint || ele.BoundingRectangle.Contains(point))
                    {
                        BrushInnerChilds(ele, point);
                    }
                    ele = UiCommon.TreeWalker.GetNextSibling(ele);
                }
            }
            catch
            {

            }
        }
        #endregion
        #region 根据坐标点截取应用界面图片
        private void SaveAPPFaceImg()
        {
            try
            {
                logger.LogDebug("SaveAPPFaceImg Start");
                AutomationElement element = null;
                Rectangle wndRect = Rectangle.Empty;
                Rectangle elementRect = CurrentHighlightElement.BoundingRectangle;
                if (elementRect.IsEmpty)
                {
                    return;
                }
                var screenPoint = System.Windows.Forms.Cursor.Position;
                List<IntPtr> parentLinked = UiCommon.GetRootWindowList(screenPoint);
                foreach (IntPtr hWnd in parentLinked)
                {
                    if (hWnd != IntPtr.Zero)
                    {
                        element = UiCommon.UIAAutomation.FromHandle(hWnd);   //获取根窗体
                    }
                    //CurrentHighlightElement = GuiManager.CreateCVGuiNode(element);
                    if (element == null)
                    {
                        return;//空窗口元素 终止方法
                    }
                    if (element.Properties.BoundingRectangle.IsSupported && element.BoundingRectangle.Contains(screenPoint))
                    {
                        wndRect = element.BoundingRectangle;
                        elementRect.X -= wndRect.X;
                        elementRect.Y -= wndRect.Y;
                        break;
                    }
                }
                GuiElement.overlayForm.inspectorStatus.SetWndHide();
                //var cvNode = GuiManager.CreateCVGuiNode(element);
                using (var appFaceImg = element.Capture())
                {
                    string filePath = Path.Combine(GlobleConfigurations.PackageDirectory, ConstFields.RES_STATICFOLDER_NAME, GlobleConfigurations.CaptureUniqueID);
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }
                    string fileFullName = Path.Combine(filePath, ConstFields.RES_SELECTOR_APPFACEIMG_NAME);
                    appFaceImg.Save(fileFullName);
                }


                JObject jObj = new JObject();
                JObject elementDataObj = new JObject();
                elementDataObj.Add(new JProperty("x", elementRect.X));
                elementDataObj.Add(new JProperty("y", elementRect.Y));
                elementDataObj.Add(new JProperty("width", elementRect.Width));
                elementDataObj.Add(new JProperty("height", elementRect.Height));
                jObj.Add(new JProperty(ConstFields.RES_SELECTOR_STATICDATA_ELEMENTDATA, elementDataObj));
                //保存更窗口selector逻辑，暂时不启用
                //string eleSelector = GuiManager.CreateGuiWindowsOperator().BuilderSelector(element);
                //jObj.Add(new JProperty(ConstFields.RES_SELECTOR_STATICDATA_WINDOWSSELECTOR, eleSelector));

                UiCommon.SaveStaticSelectorData(GlobleConfigurations.CaptureUniqueID, ConstFields.RES_SELECTOR_STATICFILE_NAME, jObj.ToString());

                Dictionary<string, object> wndInfo = new Dictionary<string, object>();
                wndInfo.Add("handle", (int)element.Properties.NativeWindowHandle.Value);
                wndInfo.Add("area", new[] { wndRect.X, wndRect.Y, wndRect.Width, wndRect.Height });
                ZbrainAPIService.SendUIDToZBrain(wndInfo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            logger.LogDebug("SaveAPPFaceImg End");
        }
        #endregion

        #region agent模式下展示父子级元素区域UI
        public class NodeInfo
        {
            public IGuiNode SelfNode { get; set; }
            public IGuiNode ChildNode { get; set; }
            public IGuiNode ParentNode { get; set; }
        }
        private List<NodeInfo> elementsCache = new List<NodeInfo>();
        public void ShowParentElement()
        {
            if (CurrentHighlightElement is ElementGuiNode)
            {
                ElementType eleType = ElementType.Ordinary;
                var nodeInfo = elementsCache.FirstOrDefault(e => e.SelfNode.Equals(CurrentHighlightElement));
                if (elementsCache.Count < 1 || nodeInfo == null || nodeInfo.ParentNode == null)
                {
                    try
                    {
                        CurrentHighlightElement = ((ElementGuiNode)CurrentHighlightElement).GetParentNode();
                        nodeInfo.ParentNode = CurrentHighlightElement;
                        elementsCache.Add(new NodeInfo { SelfNode = CurrentHighlightElement, ChildNode = nodeInfo.SelfNode });
                    }
                    catch (Exception ex)
                    {
                        //后续调用前端的通知消息提醒框接口
                        logger.LogWarning($"获取当前元素父元素异常:{ex}");
                        m_UIProvider.ShowTips("", "未找到父元素".I18n("OverlayForm_382588D2"), TipLevel.ERROR);
                    }
                }
                else
                {
                    CurrentHighlightElement = nodeInfo.ParentNode;
                }
                Task.Factory.StartNew(async () =>
                {
                    string _ControlName = string.Empty;
                    eGuiType _eGuiType = eGuiType.None;
                    GetCurrentElementParam(CurrentHighlightElement, eleType, ref _ControlName, ref _eGuiType);
                    RefreshHighLightTag(_ControlName, eleType, _eGuiType);
                }).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });
                GuiElement.overlayForm.LastHighlightElement = CurrentHighlightElement;
                var rect = CurrentHighlightElement.BoundingRectangle;
                HighLightParam CHighLightParam = new HighLightParam(rect, ElementType.None);
                MoveRectNew(CHighLightParam);
            }
            else
            {
                //后续调用前端的通知消息提醒框接口
                logger.LogError($"定位父级元素失败");
                m_UIProvider.ShowTips("", "此类型元素暂不支持操作".I18n("OverlayForm_4F5F5D60"), TipLevel.ERROR);
                //SelectorError = eErrorMessageType.Error_NotMatchNodeType_Error.GetMessage();
                //StopHighlight();
            }
        }
        public void ShowChildElement()
        {
            if (CurrentHighlightElement is ElementGuiNode)
            {
                ElementType eleType = ElementType.Ordinary;
                var nodeInfo = elementsCache.FirstOrDefault(e => e.SelfNode.Equals(CurrentHighlightElement));
                if (elementsCache.Count < 1 || nodeInfo == null || nodeInfo.ChildNode == null)
                {
                    try
                    {
                        CurrentHighlightElement = ((ElementGuiNode)CurrentHighlightElement).GetChildNode();
                        nodeInfo.ChildNode = CurrentHighlightElement;
                        elementsCache.Add(new NodeInfo { SelfNode = CurrentHighlightElement, ParentNode = nodeInfo.SelfNode });
                    }
                    catch (Exception ex)
                    {
                        //后续调用前端的通知消息提醒框接口
                        m_UIProvider.ShowTips("", "未找到子元素".I18n("OverlayForm_60F76EB0"), TipLevel.ERROR);
                        logger.LogWarning($"获取当前元素子元素异常:{ex}");
                    }
                }
                else
                {
                    CurrentHighlightElement = nodeInfo.ChildNode;
                }
                GuiElement.overlayForm.LastHighlightElement = CurrentHighlightElement;
                var rect = CurrentHighlightElement.BoundingRectangle;
                HighLightParam CHighLightParam = new HighLightParam(rect, ElementType.None);
                MoveRectNew(CHighLightParam);
            }
            else
            {
                //后续调用前端的通知消息提醒框接口
                logger.LogError($"定位子级元素失败");
                m_UIProvider.ShowTips("", "此类型元素暂不支持操作".I18n("OverlayForm_4F5F5D60"), TipLevel.ERROR);
                //SelectorError = eErrorMessageType.Error_NotMatchNodeType_Error.GetMessage();
                //StopHighlight();
            }
        }

        public void CancelAgentCaptureElement()
        {
            HasDoSelect = false;
            CancellationForCapture = new CancellationTokenSource();
            OverlayFormCaptureTask.Instance.ReStartExecute();
            CVElementProvider.Instance.Start();
            OverlayFormCaptureTask.Instance.Resume();
            CVCaptureTask.Instance.Resume();
            //if (inspectorStatus == null)
            //{
            //    inspectorStatus = new InspectInfoWnd();
            //    inspectorStatus.SetWndShow();
            //}
            //inspectorStatus.InitCapture();
            ////inspectorStatus.IsPause = InspectStatus == 0 ? false : true;
            //inspectorStatus.StartCheckCursorLoop();
            GuiElement.overlayForm.inspectorStatus.SetWndShow();
            StartHighLightFlow();
        }

        public void ConfirmAgentCaptureElement()
        {
            try
            {
                if (!(CurrentHighlightElement is ElementGuiNode))
                {
                    var rpaExce = new RPAException(eErrorMessageType.Error_NotMatchNodeType_Error);
                    rpaExce.CustomMessage = "拾取失败，仅支持web元素";
                    throw rpaExce;
                }
                var element = new GuiElement(CurrentHighlightElement);
                using (currentDesktopScreenshot = CaptureDesktop())
                {
                    element.currentInformativeScreenshot = element.CaptureInformativeScreenshot(snappath, currentDesktopScreenshot);
                }
                //截取当前元素所在浏览器窗口截图
                var windowInfo = BrowserWindowProvider.GetFirstBrowserByZOrder();
                var browserWndElement = UiCommon.UIAAutomation.FromHandle(windowInfo.Handle);
                using (var appFaceImg = browserWndElement.Capture())
                {
                    string browserImageName = $"{Guid.NewGuid().ToString()}.png";
                    string filePath = Path.Combine(GlobleConfigurations.PackageDirectory, ConstFields.RES_STATICFOLDER_NAME, GlobleConfigurations.CaptureUniqueID);
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }
                    BrowserWndImageFilePath = Path.Combine(filePath, browserImageName);
                    appFaceImg.Save(BrowserWndImageFilePath);
                }
                ////为ai智能修复保存数据，方法需包含内部容错
                //SaveAPPFaceImg();
                element.screenShotPath = snappath;

                logger.LogDebug($"get selector");
                selector = GuiElement.OnSelected?.Invoke(element);
            }
            catch (RPAException e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "拾取元素操作失败");
                SelectorError = e.ErrorMessage;
            }
            finally
            {
                elementsCache = new List<NodeInfo>();
                StopHighlight();
            }
        }

        #endregion
    }
    /// <summary>
    /// 设置可穿透的Panel，以便标记框上的鼠标能选择下方窗体
    /// </summary>
    public class ExtendedPanel : Panel
    {
        private const int WM_NCHITTEST = 0x84;
        private const int HTTRANSPARENT = -1;
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WM_NCHITTEST)
                message.Result = (IntPtr)HTTRANSPARENT;
            else
                base.WndProc(ref message);
        }

    }
}


