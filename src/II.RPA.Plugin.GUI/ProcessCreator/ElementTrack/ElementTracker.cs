using FlaUI.Core.AutomationElements;
using Gma.UserActivityMonitor;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.UI;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.ProcessCreator.ActionProvider;
using II.RPA.Plugin.GUI.ProcessCreator.Screenshot;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Clipboard = System.Windows.Clipboard;
using ColorConverter = System.Windows.Media.ColorConverter;
using Control = System.Windows.Forms.Control;
using Cursor = System.Windows.Forms.Cursor;
using DataFormats = System.Windows.DataFormats;
using IDataObject = System.Windows.IDataObject;
using ImageHelper = II.RPA.WinAutomation.CommonUtility.ImageHelper;
using Point = System.Drawing.Point;
using TargrtXmlHelper = II.RPA.WinAutomation.CommonUtility.TargrtXmlHelper;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementTrack
{
    public class ElementTracker
    {
        #region Fields
        private static readonly ILogger logger = Logger.OfContext();
        /// <summary>
        /// tracking flag
        /// </summary>
        private volatile bool m_IsTracking;
        /// <summary>
        /// 
        /// </summary>
        private volatile bool m_IsPressed;
        /// <summary>
        /// 
        /// </summary>
        private volatile bool m_IsMoved;
        /// <summary>
        /// ImageTracker
        /// </summary>
        private readonly ImageTracker m_ImageTracker;
        /// <summary>
        /// ElementHighlighter instance
        /// </summary>
        private readonly ElementHighlighter m_ElementHighlighter;
        /// <summary>
        /// Concurrency Mouse Point Container
        /// </summary>
        private readonly ConcurrencyItemContainer<Point?> m_NextPoint = new ConcurrencyItemContainer<Point?>();
        /// <summary>
        /// current dispatcher
        /// </summary>
        private Dispatcher m_UIDispatcher;
        /// <summary>
        /// 
        /// </summary>
        private CancellationTokenSource m_TrackingThreadCancellation;
        /// <summary>
        /// custom ui thread
        /// </summary>
        private readonly Thread m_UIThread;
        /// <summary>
        /// CV相关
        /// </summary>
        private bool m_IsSwitchApp = false;
        private Rectangle m_CurWndArea = Rectangle.Empty;
        private Bitmap m_AppShotImg = null;
        private DateTime m_LastCapturedTime = DateTime.Now;
        /// <summary>
        /// cv截图中
        /// </summary>
        private volatile bool m_InCvShoting;

        private readonly ManualResetEventSlim m_StartedEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim m_TrackOnceTimeSemaphore = new ManualResetEventSlim(false);
        private ElementType m_EleType = ElementType.Ordinary;
        private eGuiType m_GuiType = eGuiType.Windows;
        /// <summary>
        /// 标识actionwindow当前是否打开
        /// </summary>
        private bool m_IsActionWindowOpen = false;
        /// <summary>
        /// 标识actionpopup当前是否打开
        /// </summary>
        private volatile bool m_IsActionPopupOpen = false;
        private volatile bool m_IsTrackOnceTime = false;
        private volatile bool m_IsEscapeDown = false;
        private volatile bool m_IsCancelbyUser = false;
        private readonly int m_CallThreadId;
        private readonly object m_Lock = new object();
        private readonly object m_TrackLock = new object();
        private string m_SpecificTagText;
        private bool m_IsCtrlDown = false;
        private Point lastPoint = new Point(-1, -1);
        private AutomationElement m_DesktopElement = UiCommon.UIAAutomation.GetDesktop();
        private readonly List<int> m_ExplorerPIDs = new List<int>();
        private List<IntPtr> m_CaptureHWndList;
        private Popup m_MouseTip;
        /// <summary>
        /// 对应普通拾取 F4 功能
        /// </summary>
        private bool m_IsRefresh = false;
        private readonly ZbrainService m_ZbrainService;
        private readonly ElementCacheProvider m_CacheProvider;
        private List<int> m_ExcelProcessIds = new List<int>();
        private readonly UIProvider m_UIProvider;
        private readonly ConcurrencyItemContainer<ActionParamModel?> m_NextAction;
        private readonly IPluginInvoker m_PluginInvoker;
        private PluginExecutionContext m_PluginExecutionContext;
        #endregion
        public ElementTracker(ElementHighlighter anElementHighlighter,
            ImageTracker anImageTracker,
            ZbrainService zbrainService,
            ElementCacheProvider anElementCacheProvider,
            IPluginInvoker pluginInvoker)
        {
            m_PluginInvoker = pluginInvoker ?? throw new ArgumentNullException(nameof(pluginInvoker));
            m_ImageTracker = anImageTracker;
            m_ZbrainService = zbrainService;
            m_ElementHighlighter = anElementHighlighter;
            m_CacheProvider = anElementCacheProvider;

            m_UIThread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                m_UIDispatcher = Dispatcher.CurrentDispatcher;
                m_StartedEvent.Set();
                Dispatcher.Run();
            });
            m_UIThread.SetApartmentState(ApartmentState.STA);
            m_UIThread.IsBackground = true;
            m_UIThread.Start();
            logger.LogDebug($"UI线程ID:{m_UIThread.ManagedThreadId}");
            m_CallThreadId = Thread.CurrentThread.ManagedThreadId;
            logger.LogDebug($"当前线程ID:{m_CallThreadId}");
            m_StartedEvent.Wait();
            m_UIDispatcher.UnhandledException += Dispatcher_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            m_UIProvider = new UIProvider();
            m_NextAction = new ConcurrencyItemContainer<ActionParamModel?>();

            #region Observables
            var mouserob = Observable.Interval(TimeSpan.FromMilliseconds(10))
            .Where(x => lastPoint != Control.MousePosition)
            .Where(x => m_IsTracking)
            .Subscribe(x =>
            {
                try
                {
                    lastPoint = Control.MousePosition;
                    if (m_IsTracking)
                    {
                        m_NextPoint.SetData(lastPoint);
                    }
                    if (m_IsTrackOnceTime && m_MouseTip != null)
                    {
                        if (m_ElementHighlighter.IsEnable)
                        {
                            m_UIDispatcher.InvokeAsync(() =>
                            {
                                m_MouseTip.IsOpen = false;
                                m_MouseTip.IsOpen = true;
                            });
                        }
                        else
                        {
                            m_UIDispatcher.InvokeAsync(() =>
                            {
                                m_MouseTip.IsOpen = false;
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "mouserob");
                }
            });

            var explorerob = Observable.Interval(TimeSpan.FromMilliseconds(3000))
            .Where(x => lastPoint != Control.MousePosition || !m_ExplorerPIDs.Any())
            .Subscribe(x =>
            {
                try
                {
                    //订阅任务管理器的进程变化
                    var processes = Process.GetProcessesByName("explorer");
                    if (processes != null)
                    {
                        var currentprocesses = processes.Where(p => p.SessionId == Process.GetCurrentProcess().SessionId);
                        if (currentprocesses != null && currentprocesses.Any())
                        {
                            currentprocesses.ForEach(cp =>
                            {
                                if (!m_ExplorerPIDs.Contains(cp.Id))
                                {
                                    m_ExplorerPIDs.Add(cp.Id);
                                }
                            });
                            //logger.LogDebug($"explorerob:m_ExplorerPID is {string.Join(";", m_ExplorerPIDs)}");
                        }
                        else
                        {
                            logger.LogError($"explorerob:currentprocess is null");
                        }
                    }
                    else
                    {
                        logger.LogError($"explorerob:explorer process is null");
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "explorerob");
                }
            });

            var factoryob = Observable.Interval(TimeSpan.FromMilliseconds(500))
           .Where(x => lastPoint != Control.MousePosition)
           .Subscribe(x =>
           {
               try
               {
                   //订阅实在设计器的进程变化
                   var factoryProcesses = Process.GetProcessesByName(LocalDataModels.FactoryProcessName);
                   if (factoryProcesses != null && factoryProcesses.Any())
                   {
                       factoryProcesses.ForEach(fp =>
                       {
                           if (!LocalDataModels.FactoryProcessIds.Contains(fp.Id))
                           {
                               logger.LogDebug($"new factory process {fp.Id}");
                               LocalDataModels.FactoryProcessIds.Add(fp.Id);
                           }
                       });
                   }
                   //订阅Excel进程变化
                   var excelProcesses = Process.GetProcessesByName("EXCEL");
                   if (excelProcesses != null && excelProcesses.Any())
                   {
                       excelProcesses.ForEach(fp =>
                       {
                           if (!m_ExcelProcessIds.Contains(fp.Id))
                           {
                               logger.LogDebug($"new excel process {fp.Id}");
                               m_ExcelProcessIds.Add(fp.Id);
                           }
                       });
                   }
               }
               catch (Exception ex)
               {
                   Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "factoryob");
               }
           });
            #endregion
        }

        internal void BindPluginInvocation(PluginExecutionContext executionContext)
        {
            m_PluginExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        }
        #region Properties
        private bool IsCtrlDown
        {
            get => m_IsCtrlDown;
            set
            {
                m_IsCtrlDown = value;
                //logger.LogDebug($"Set IsCtrlDown to {m_IsCtrlDown}");
            }
        }
        private bool IsExcelRecordRunning
        {
            get;
            set;
        }
        /// <summary>
        /// 内部判断用
        /// </summary>
        public bool IsTracking
        {
            get => m_TrackingThreadCancellation != null
                && !m_TrackingThreadCancellation.IsCancellationRequested;
        }
        public bool IsStart
        {
            get;
            set;
        }
        private bool IsNoAdmin
        {
            get => m_EleType == ElementType.Ordinary
                && !string.IsNullOrEmpty(m_SpecificTagText);
        }
        /// <summary>
        /// 拾取任务
        /// </summary>
        public Task TrackingTask
        {
            get;
            set;
        }
        public bool IsEnableCV
        {
            get;
            set;
        } = true;
        /// <summary>
        /// 查找相似元素需要的selector、备用
        /// </summary>
        internal string Selector
        {
            get;
            set;
        } = string.Empty;
        internal AutomationElement CurrentAutomationElement
        {
            get;
            set;
        }
        /// <summary>
        /// 普通拾取得到的元素
        /// </summary>
        internal IGuiNode CurrentNativeElementNode
        {
            get;
            set;
        }
        /// <summary>
        /// 当前正处于高亮状态的元素
        /// </summary>
        internal IGuiNode CurrentHighlightGuiNode
        {
            get;
            set;
        }
        /// <summary>
        /// 当前cv根元素
        /// </summary>
        internal IGuiCVNode CurrentCvCaptureRootNode
        {
            get;
            set;
        }
        /// <summary>
        /// 同上...
        /// </summary>
        internal IGuiCVNode ParentContainer
        {
            get
            {
                return CurrentCvCaptureRootNode;
            }
        }
        /// <summary>
        /// 当前高亮元素的相对点击位置
        /// </summary>
        public Point CurrentHighlightElementRelativeClickPos
        {
            get;
            private set;
        }
        private Bitmap AppShotImg
        {
            set
            {
                if (m_AppShotImg != null)
                {
                    m_AppShotImg.Dispose();
                }
                m_AppShotImg = value;
            }
            get
            {
                return m_AppShotImg;
            }
        }
        /// <summary>
        /// 当前根窗口元素对象
        /// </summary>
        internal AutomationElement CaptureRootWnd
        {
            get;
            set;
        } = null;
        internal IntPtr CurrentWndHandle
        {
            get;
            set;
        } = IntPtr.Zero;
        public bool IsNeedDeskTopToolDetect
        {
            get;
            set;
        } = false;
        #endregion
        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e.Exception, "TaskScheduler_UnobservedTaskException");
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e.Exception, "Dispatcher_UnhandledException");
        }
        #region Hook
        private void InstallHook()
        {
            UninstallHook();
            HookManager.KeyDown += HookManager_KeyDown;
            HookManager.KeyUp += HookManager_KeyUp;
            HookManager.MouseWheel += HookManager_MouseWheel;
            HookManager.MouseClickExt += HookManager_MouseClickExt;
            HookManager.MouseMoveExt += HookManager_MouseMoveExt;
            //HookManager.MouseDown += HookManager_MouseDown;
            //HookManager.MouseUp += HookManager_MouseUp;
            //HookManager.MouseMove += HookManager_MouseMove;
        }

        private void UninstallHook()
        {
            try
            {
                HookManager.KeyDown -= HookManager_KeyDown;
                HookManager.KeyUp -= HookManager_KeyUp;
                HookManager.MouseWheel -= HookManager_MouseWheel;
                HookManager.MouseClickExt -= HookManager_MouseClickExt;
                HookManager.MouseMoveExt -= HookManager_MouseMoveExt;
                //HookManager.MouseDown -= HookManager_MouseDown;
                //HookManager.MouseUp -= HookManager_MouseUp;
                //HookManager.MouseMove -= HookManager_MouseMove;
            }
            catch (Exception ex)
            {
                logger.LogError($"UninstallHook Error.{ex}");
            }
        }
        private void HookManager_MouseUp(object sender,
            System.Windows.Forms.MouseEventArgs e)
        {
            if (m_IsPressed && m_IsMoved && !m_IsActionPopupOpen)
            {
                m_UIDispatcher.InvokeAsync(() =>
                {
                    SendKeys.SendWait("^c");
                    SendKeys.Flush();
                    IDataObject iData = Clipboard.GetDataObject();
                    if (null != iData)
                    {
                        if (iData.GetDataPresent(DataFormats.Text)) //检查是否存在文本
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                string res = (String)iData.GetData(DataFormats.UnicodeText);
                                if (!string.IsNullOrWhiteSpace(res))
                                {
                                    //System.Windows.MessageBox.Show("res");
                                    //m_ElementHighlighter.Pause();
                                    //var vm = TrackOneElementForActionPopup(Cursor.Position, ActionOver);
                                    //if (vm != null)
                                    //{
                                    //    m_ElementHighlighter?.ShowActionPopupWithoutOwner(vm);
                                    //}
                                }
                            }
                        }
                    }
                    m_IsPressed = false;
                    m_IsMoved = false;
                });
            }
        }
        private void HookManager_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            m_IsPressed = true;
        }

        private void HookManager_MouseMoveExt(object sender, MouseEventExtArgs e)
        {
            m_NextPoint.SetData(new Point?(e.Location));
            //try
            //{
            //    var element = UiCommon.UIAAutomation.FromPoint(e.Location);
            //    m_CacheProvider.CacheElement(element);
            //}
            //catch
            //{
            //    //ignore
            //}

            if (m_IsPressed)
            {
                m_IsMoved = true;
            }
        }

        private void HookManager_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey && m_IsTracking && !m_IsActionPopupOpen)
            {
                m_ElementHighlighter.SetBackgroundTransparent(false);
            }
            //else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)
            //    && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)
            //    && e.KeyCode == Keys.A
            //    && !m_IsTrackOnceTime)
            //{
            //    Stop();
            //    m_ImageTracker.Start(this);
            //}
            IsCtrlDown = (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey);
            //m_NextPoint.SetData(new Point?(Cursor.Position));
        }
        private void HookManager_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (m_IsTrackOnceTime)
                {
                    m_IsEscapeDown = true;
                    m_TrackingThreadCancellation?.Cancel();
                    m_ElementHighlighter.Pause();
                    m_TrackOnceTimeSemaphore?.Set();
                    m_UIDispatcher.Invoke(() =>
                    {
                        if (m_MouseTip != null)
                        {
                            m_MouseTip.IsOpen = false;
                            m_MouseTip = null;
                        }
                    });
                    m_NextPoint.SetData(Cursor.Position);
                }
            }
            else if (e.KeyCode == Keys.LControlKey && m_IsTracking && !m_IsActionPopupOpen)
            {
                m_ElementHighlighter.SetBackgroundTransparent(true);
            }
            //else if (m_IsTracking && !IsNoAdmin)
            //{
            //    ElementTextSync();
            //}
            IsCtrlDown = IsCtrlDown && (e.KeyCode != Keys.LControlKey && e.KeyCode != Keys.RControlKey);
        }
        private void HookManager_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            m_NextPoint.SetData(new Point?(e.Location));
        }

        private void HookManager_MouseClickExt(object sender, MouseEventExtArgs e)
        {
            try
            {
                lock (m_Lock)
                {
                    //右键推荐逻辑
                    //if (e.Button == MouseButtons.Right && m_IsTracking && !m_IsActionWindowOpen)
                    //{
                    //    e.Handled = true;
                    //    PreShowActionPopupForRightButon();
                    //    return;
                    //}

                    if ((Keyboard.Modifiers.HasFlag(ModifierKeys.Control)
                    && e.Button == MouseButtons.Left)
                    && m_IsTracking && !m_IsActionWindowOpen)
                    {
                        #region Ctrl+leftbutton
                        //if (m_IsActionPopupOpen)
                        //{
                        //    m_ElementHighlighter.Close();
                        //}
                        if (CurrentNativeElementNode == null
                            || CurrentHighlightGuiNode == null
                            || IsNoAdmin)
                        {
                            logger.LogWarning("当前无有效元素");
                            return;
                        }
                        if (!IsHoverIgnoreElement(e.Location))
                        {
                            if (!IsHoverElementSelf(e.Location)
                                || m_IsActionPopupOpen)
                            {
                                //单次拾取
                                if (m_IsTrackOnceTime)
                                {
                                    logger.LogDebug("重新拾取元素...");
                                    e.Handled = true;
                                    m_IsEscapeDown = false;
                                    m_TrackingThreadCancellation?.Cancel();
                                    CancelMouseTip();
                                    m_TrackOnceTimeSemaphore.Set();
                                    return;
                                }
                                ////持续拾取
                                //else
                                //{
                                //    m_IsTracking = false;
                                //    m_ElementHighlighter.PauseOnly();
                                //    var viewModel = TrackOneElementForActionWindow(e.Location, ActionOver);
                                //    m_ElementHighlighter.ShowActionWindow(viewModel);
                                //    m_IsActionWindowOpen = true;
                                //    return;
                                //}
                            }
                        }
                        else
                        {
                            e.Handled = false;
                            logger.LogWarning("当前正在配置组件、忽略本次点击事件");
                        }
                        #endregion
                    }
                    else
                    {
                        //e.Handled = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                        logger.LogDebug("HookManager_MouseClickExt:SetData");
                        m_NextPoint.SetData(new Point?(e.Location));
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }
        #endregion

        #region Operation Control
        public Task Start(bool anIsNeedWaitCursor = false)
        {
            m_UIDispatcher.Invoke(() =>
            {
                foreach (IGuiOperator guiOperator in GuiOperatorProvider.Instance.GuiOperatorEnumerable)
                {
                    guiOperator.ReadyToCapture();
                }
            });
            if (!m_IsTracking)
            {
                SetupTracking();
                if (TrackingTask != null && !TrackingTask.IsCompleted)
                {
                    logger.LogError("There is uncomplete TrackingTask left!");
                    try
                    {
                        m_TrackingThreadCancellation?.Cancel();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("cancel TrackingTask error:", ex);
                    }
                }
                m_TrackingThreadCancellation = new CancellationTokenSource();
                StartGuiTypeRefresh();
                m_IsActionPopupOpen = false;

                logger.LogDebug($"IsNeedWaitCursor:{anIsNeedWaitCursor};IsTracking:{m_IsTracking}");

                TrackingTask = RunTracking(m_TrackingThreadCancellation, anIsNeedWaitCursor);
            }
            else
            {
                logger.LogError($"IsTracking is {m_IsTracking}");
            }
            Thread.Sleep(100);
            return TrackingTask;
        }
        public string StartOnceTime()
        {
            m_TrackOnceTimeSemaphore.Reset();
            m_IsTrackOnceTime = true;
            m_IsCancelbyUser = false;
            StartMouseTip();
            try
            {
                if (!m_IsTracking)
                {
                    SetupTracking();
                    if (TrackingTask != null && !TrackingTask.IsCompleted)
                    {
                        logger.LogError("There is uncomplete TrackingTask left!");
                        try
                        {
                            m_TrackingThreadCancellation?.Cancel();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError("Cancel TrackingTask error:", ex);
                        }
                    }
                    m_TrackingThreadCancellation = new CancellationTokenSource();

                    logger.LogDebug("Run tracking");
                    m_IsTracking = true;

                    TrackingTask = RunTracking(m_TrackingThreadCancellation, false, TaskCreationOptions.LongRunning);
                    m_TrackOnceTimeSemaphore.Wait();
                    if (m_IsEscapeDown || m_IsCancelbyUser)
                    {
                        m_IsTracking = false;
                        return "1";
                    }
                    else if (!m_IsTracking)
                    {
                        return "2";
                    }
                    else
                    {
                        return TrackOnceTime();
                    }
                }
                else
                {
                    logger.LogError("StartOnceTime:There is a running TrackingTask!");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            finally
            {
                m_IsTrackOnceTime = false;
                m_ElementHighlighter.Pause();
            }
            return string.Empty;
        }
        public void StopOnceTime()
        {
            if (m_IsTrackOnceTime)
            {
                CancelMouseTip();
                logger.LogDebug("StopOnceTime");
                m_IsCancelbyUser = true;
                m_TrackingThreadCancellation?.Cancel();
                m_TrackOnceTimeSemaphore?.Set();
            }
        }
        public void Stop()
        {
            try
            {
                logger.LogDebug("ElementTracker.Stop:start");
                m_ElementHighlighter.Pause();
                m_IsTracking = false;
                CleanupTracking();
                m_TrackOnceTimeSemaphore?.Set();
                m_TrackingThreadCancellation?.Cancel();
                m_ElementHighlighter.CloseWindowWithPostMessage();
                m_IsActionPopupOpen = false;
                //m_ElementHighlighter?.Dispose();
                //m_ElementHighlighter = new ElementHighlighter();               
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            finally
            {
                m_IsTracking = false;
                m_NextPoint.TakeData();
                logger.LogDebug("ElementTracker.Stop:over");
            }
        }

        private async Task RunTracking(CancellationTokenSource cancellationToken,
            bool anIsNeedWaitCursor = false,
            TaskCreationOptions creationOptions = TaskCreationOptions.LongRunning)
        {
            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    if (anIsNeedWaitCursor)
                    {
                        Thread.Sleep(500);
                        m_NextPoint.TakeData();
                        if (m_NextPoint.WaitForData(-1))
                        {
                            logger.LogDebug($"WaitForData:{m_NextPoint.WaitForData(-1)}");
                            if (!IsStart)
                            {
                                m_IsTracking = false;
                                Stop();
                                return;
                            }
                        }
                    }
                    logger.LogDebug("Run tracking");
                    m_IsTracking = true;
                    await TrackingLoop(cancellationToken, creationOptions);
                }
                catch (OperationCanceledException)
                {
                    logger.LogDebug("Cancel tracking");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }

            }, cancellationToken.Token, creationOptions, TaskScheduler.Default);
        }
        private void SetupTracking()
        {
            m_ElementHighlighter.Resume();
            m_UIDispatcher.Invoke(() => InstallHook());
        }
        private void CleanupTracking()
        {
            m_UIDispatcher.Invoke(() => UninstallHook());
            m_ElementHighlighter.Pause();
            CancelMouseTip();
        }
        #endregion
        private void StartMouseTip()
        {
            m_UIDispatcher.Invoke(() =>
            {
                m_MouseTip = new Popup
                {
                    Placement = PlacementMode.Mouse,
                    StaysOpen = true,
                    AllowsTransparency = true
                };
                Border border = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#EE000000")),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Colors.White)
                };
                Border border1 = new Border
                {
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#00000000")),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Colors.Gray)
                };
                Border border2 = new Border
                {
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#00000000")),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Colors.Gray),
                    Margin = new Thickness(2, 0, 0, 0)
                };
                TextBlock tb1 = new TextBlock
                {
                    Text = "Ctrl",
                    Foreground = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(2)
                };
                border1.Child = tb1;
                TextBlock tb2 = new TextBlock
                {
                    Text = "点击",
                    Foreground = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(2)
                };
                border2.Child = tb2;
                TextBlock tb3 = new TextBlock
                {
                    Text = "+",
                    Foreground = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(2, 2, 0, 0)
                };
                TextBlock tb4 = new TextBlock
                {
                    Text = "选择目标",
                    Foreground = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(3, 2, 0, 0)
                };
                StackPanel panel = new StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Margin = new Thickness(6)
                };
                panel.Children.Add(border1);
                panel.Children.Add(tb3);
                panel.Children.Add(border2);
                panel.Children.Add(tb4);
                border.Child = panel;
                m_MouseTip.Child = border;
                m_MouseTip.PopupAnimation = PopupAnimation.Slide;
                m_MouseTip.IsOpen = true;
            });
        }
        private void CancelMouseTip()
        {
            m_UIDispatcher.Invoke(() =>
            {
                if (m_MouseTip != null)
                {
                    m_MouseTip.IsOpen = false;
                    m_MouseTip = null;
                }
            });
        }
        /// <summary>
        /// 拾取一次
        /// </summary>
        /// <returns></returns>
        private string TrackOnceTime()
        {
            m_ElementHighlighter.PauseOnly();
            m_IsTracking = false;
            GlobleConfigurations.CaptureUniqueID = RandomHelper.RandomString(10);
            string elementShotPath = GetElementShotPath();
            m_ElementHighlighter.Hide();
            Thread.Sleep(1);
            GuiElement guiElement;
            if (CurrentHighlightGuiNode is IGuiCVNode cvnode)
            {
                //CV元素
                guiElement = new GuiElement(CurrentHighlightGuiNode)
                {
                    currentInformativeScreenshot = cvnode.CVSubElementToBitmap(elementShotPath, ParentContainer.CacheImage)
                };
            }
            else
            {
                //原生元素
                using (var currentDesktopScreenshot = m_DesktopElement.Capture())
                {
                    using (var elementshot = CaptureElementShot(currentDesktopScreenshot, CurrentHighlightGuiNode))
                    {
                        guiElement = new GuiElement(CurrentHighlightGuiNode);
                        guiElement.currentInformativeScreenshot = guiElement.CaptureInformativeScreenshot(elementShotPath,
                            currentDesktopScreenshot);

                        ActionParamModel actionParam = new ActionParamModel
                        {
                            PicturePath = elementShotPath,
                            GuiType = m_GuiType,
                            ControlType = CurrentHighlightGuiNode.ControlType,
                            CurrentGuiNode = CurrentHighlightGuiNode,
                            UIAElement = CurrentAutomationElement,
                            RectangleRegion = CurrentHighlightGuiNode.BoundingRectangle,
                            UIARootElement = CaptureRootWnd,
                            Attributes = (CurrentHighlightGuiNode is IGuiWebNode_690) ? (CurrentHighlightGuiNode as IGuiWebNode_690).Attributes : ""
                            //ElementSelector = doSelectResult
                        };
                        ConfigAIRepair(actionParam);
                    }
                }
            }
            var ret = DoSelect(elementShotPath, CurrentHighlightGuiNode, guiElement);
            return ret.Selector;
        }
        private ActionViewModelBase TrackOneElementForActionPopup(Point aMousePoint,
            Action anActionOverCallBack,
            Action<ActionParamModel> anActionDoneCallBack)
        {
            lock (m_TrackLock)
            {
                try
                {
                    //SomeUtils.TraceThreadPool("TrackOneElementForActionPopup start");
                    #region status control
                    m_IsActionPopupOpen = true;
                    #endregion

                    #region 截图方法前置
                    GlobleConfigurations.CaptureUniqueID = RandomHelper.RandomString(10);
                    string elementShotPath = GetElementShotPath();
                    string elementShotPath_WithOutHighLight = null;
                    string elementShotBase64 = string.Empty;
                    GuiElement guiElement;
                    m_ElementHighlighter.Hide();
                    Thread.Sleep(5);
                    if (CurrentHighlightGuiNode is IGuiCVNode cvnode)
                    {
                        //CV元素
                        guiElement = new GuiElement(CurrentHighlightGuiNode)
                        {
                            currentInformativeScreenshot = cvnode.CVSubElementToBitmap(elementShotPath, ParentContainer.CacheImage)
                        };
                    }
                    else
                    {
                        m_DesktopElement = UiCommon.UIAAutomation.GetDesktop();
                        using (var currentDesktopScreenshot = m_DesktopElement.Capture())
                        {
                            using (var elementshot_WithOutHighLight = CaptureElementShot(currentDesktopScreenshot, CurrentHighlightGuiNode, false))
                            {
                                elementShotPath_WithOutHighLight = GetElementShotPath();
                                try
                                {
                                    elementshot_WithOutHighLight?.Save(elementShotPath_WithOutHighLight);
                                }
                                catch (Exception ex)
                                {
                                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"shotpath:{elementShotPath_WithOutHighLight}");
                                }
                            }
                        }
                        //原生元素
                        using (var currentDesktopScreenshot = m_DesktopElement.Capture())
                        {
                            using (var elementshot = CaptureElementShot(currentDesktopScreenshot, CurrentHighlightGuiNode))
                            {
                                if (elementshot != null)
                                {
                                    elementShotBase64 = ToBase64(elementshot);
                                }
                                guiElement = new GuiElement(CurrentHighlightGuiNode);
                                guiElement.currentInformativeScreenshot = guiElement.CaptureInformativeScreenshot(elementShotPath,
                                    currentDesktopScreenshot);
                                //SaveAPPFaceImg(CurrentHighlightGuiNode.BoundingRectangle);
                            }
                        }

                    }
                    m_ElementHighlighter.UndoHide();
                    #endregion

                    #region build selector task
                    Task<SelectElementModel> doSelectTask = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            logger.LogDebug("DoSelectorTask start");
                            return DoSelect(elementShotPath, CurrentHighlightGuiNode, guiElement);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex);
                            return default;
                        }
                        finally
                        {
                            SomeUtils.TraceThreadPool("DoSelect over");
                        }
                    }, TaskCreationOptions.LongRunning);
                    doSelectTask.ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });
                    //SelectElementModel doSelectResult = new SelectElementModel
                    //{
                    //    IsValid = false
                    //};
                    //{
                    //    try
                    //    {
                    //        logger.LogInformation("DoSelector start");
                    //        doSelectResult = DoSelect(elementShotPath, CurrentHighlightGuiNode, guiElement);
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        logger.LogError(ex);
                    //    }
                    //}

                    #endregion

                    m_ElementHighlighter.SetBackgroundTransparent(false);

                    m_ElementHighlighter.RegisterActionOverEventHandler(() =>
                    {
                        logger.LogDebug("RegisterActionOverEventHandler callback");
                        m_IsActionPopupOpen = false;
                        anActionOverCallBack();
                        //m_ElementHighlighter.SetBusy(false);
                    });

                    #region build parameters
                    ActionParamModel actionParam = new ActionParamModel
                    {
                        MouseLocation = aMousePoint,
                        PicturePath = elementShotPath,
                        PicturePath_WithOutHighLight = elementShotPath_WithOutHighLight,
                        GuiType = m_GuiType,
                        ControlType = CurrentHighlightGuiNode.ControlType,
                        CurrentGuiNode = CurrentHighlightGuiNode,
                        ElementType = m_EleType,
                        ElementShotBase64 = elementShotBase64,
                        IsExplorer = IsExplorer(CurrentHighlightGuiNode),
                        UIAElement = CurrentAutomationElement,
                        RectangleRegion = CurrentHighlightGuiNode.BoundingRectangle,
                        UIARootElement = CaptureRootWnd,
                        Attributes = (CurrentHighlightGuiNode is IGuiWebNode_690) ? (CurrentHighlightGuiNode as IGuiWebNode_690).Attributes : ""
                        //ElementSelector = doSelectResult
                    };
                    #endregion

                    m_NextAction.SetData(actionParam);

                    #region get process name
                    try
                    {
                        if (CurrentAutomationElement.Properties.ProcessId.TryGetValue(out int processId))
                        {
                            actionParam.ProcessName = UiCommon.GetProcessNameById(processId);
                            logger.LogDebug($"CurrentAutomationElement ProcessId is {processId}");
                        }
                        else
                        {
                            logger.LogWarning($"CurrentAutomationElement ProcessId is unavaliable");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex);
                    }
                    #endregion

                    var actionPopupViewModel = ActionPopupViewModel.GetActionPopupViewModelSingleton(m_ElementHighlighter.HighLightDispatcher);
                    actionPopupViewModel.BindPluginInvocation(m_PluginInvoker, m_PluginExecutionContext);
                    actionPopupViewModel.ResolveOverEvent -= ActionPopupViewModel_ResolveOver;
                    actionPopupViewModel.ResolveOverEvent += ActionPopupViewModel_ResolveOver;
                    actionPopupViewModel.NeedHoldonEvent -= ActionPopupViewModel_NeedHoldonEvent;
                    actionPopupViewModel.NeedHoldonEvent += ActionPopupViewModel_NeedHoldonEvent;
                    SomeUtils.TraceThreadPool("BuildViewModel start");
                    actionPopupViewModel.BuildViewModel(doSelectTask,
                        actionParam,
                        false,
                        m_ZbrainService,
                        m_CacheProvider,
                        anActionDoneCallBack);
                    return actionPopupViewModel;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                    if (ex.InnerException != null)
                    {
                        logger.LogError(ex.InnerException);
                    }
                    return null;
                }
            }
        }
        private void ActionDoneCallBack(ActionParamModel actionParamModel)
        {
            if (actionParamModel.GuiType != eGuiType.CV)
            {
                logger.LogDebug("not cv element,start config ai repair.");
                ConfigAIRepair(actionParamModel);
            }
            m_NextAction.TakeData();
            logger.LogInformation("Generated a Action!");
        }

        private void ActionPopupViewModel_NeedHoldonEvent(bool obj)
        {
            logger.LogDebug($"ActionPopupViewModel_NeedHoldonEvent:{obj}");
            if (obj)
            {
                m_ElementHighlighter.PauseOnly();
                m_IsTracking = false;


            }
            else
            {
                if (IsStart)
                {
                    m_ElementHighlighter.Resume();
                    m_IsTracking = true;
                }
            }
        }

        private void ActionPopupViewModel_ResolveOver(string anArgs)
        {
            if (anArgs == "error")
            {
                m_ElementHighlighter.Pause();
                ActionOver();
            }
            else
            {
                m_ElementHighlighter.SetBusy(false, anArgs);
                //m_ElementHighlighter.SetTagText(anArgs);
            }
        }

        /// <summary>
        /// action menu关闭回调
        /// </summary>
        private void ActionOver()
        {
            logger.LogDebug("ActionOver start");
            m_EleType = ElementType.Ordinary;
            m_ElementHighlighter.SetBackgroundTransparent(true);
            if (m_TrackingThreadCancellation != null
                && m_TrackingThreadCancellation.IsCancellationRequested)
            {
                logger.LogDebug("tracking is over.ignore resume action");
            }
            else
            {
                if (IsStart)
                {
                    m_IsTracking = true;
                    m_ElementHighlighter.Resume();
                    m_NextPoint?.SetData(Cursor.Position);
                    logger.LogDebug($"Tracker has been resumed!");
                }
            }

            var action = m_NextAction.TakeData();
            if (action.HasValue)
            {
                try
                {
                    var picturePath = action.Value.PicturePath;
                    var picturePathWithoutHighlight = action.Value.PicturePath_WithOutHighLight;

                    if (!string.IsNullOrEmpty(picturePath) && File.Exists(picturePath))
                    {
                        logger.LogDebug($"Delete obsolete picture:{picturePath}");
                        File.Delete(picturePath);
                    }
                    if (!string.IsNullOrEmpty(picturePathWithoutHighlight) && File.Exists(picturePathWithoutHighlight))
                    {
                        logger.LogDebug($"Delete obsolete picture:{picturePathWithoutHighlight}");
                        File.Delete(picturePathWithoutHighlight);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }
            else
            {
                logger.LogDebug("Picture has been applied.");
            }
            logger.LogDebug("ActionOver end");
        }
        /// <summary>
        /// 元素截图
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="guiNode"></param>
        /// <returns></returns>
        public Bitmap CaptureElementShot(Bitmap bitmap,
            IGuiNode guiNode,
            bool aNeedHighLight = true)
        {
            Bitmap target = null;
            try
            {
                if (guiNode == null)
                {
                    return null;
                }
                if (guiNode.BoundingRectangle.IsEmpty)
                {
                    return null;
                }
                target = CaptureElementShotByRect(bitmap, guiNode.BoundingRectangle, aNeedHighLight);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "snap screen shot fail");
            }
            return target;
        }
        /// <summary>
        /// 元素截图：rect+desktopshot
        /// </summary>
        /// <param name="currentDesktopScreenshot"></param>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        private Bitmap CaptureElementShotByRect(Bitmap currentDesktopScreenshot,
            Rectangle rectangle,
            bool aNeedHighLight)
        {
            var ratio = NativeMethods.ScaleX;

            rectangle.Inflate(40, 40);

            Rectangle cropRect = new Rectangle((int)(rectangle.X * ratio),
                (int)(rectangle.Y * ratio),
                (int)(rectangle.Width * ratio),
                (int)(rectangle.Height * ratio));
            cropRect.Inflate(5, 5);
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(currentDesktopScreenshot,
                    new Rectangle(0, 0, target.Width, target.Height),
                    cropRect,
                    GraphicsUnit.Pixel);
            }
            return target;
            //return ImageHelper.CompressImage(target, 900, 900);
        }
        /// <summary>
        /// 干活的
        /// </summary>
        /// <param name="aCancellation"></param>
        /// <param name="creationOptions"></param>
        /// <returns></returns>
        private async Task TrackingLoop(CancellationTokenSource aCancellation,
            TaskCreationOptions creationOptions)
        {
            if (IsEnableCV)
            {
                StartCVCaptureTask(aCancellation).ConfigureAwait(false)
                    .GetAwaiter()
                    .OnCompleted(() => { });
            }
            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    logger.LogDebug("Enter trackingLoop");
                    bool flag = true;
                    while (true)
                    {
                        aCancellation.Token.ThrowIfCancellationRequested();
                        if (m_IsTracking)
                        {
                            if (m_NextPoint.TakeData().HasValue)
                            {
                                aCancellation.Token.ThrowIfCancellationRequested();
                                await CaptureFromPoint(Cursor.Position);
                                flag = true;
                            }
                            else if (IsNoAdmin)
                            {
                                aCancellation.Token.ThrowIfCancellationRequested();
                                m_NextPoint.SetData(Cursor.Position);
                                continue;
                            }
                            //else if (m_NextPoint.WaitForData(60) && m_IsTracking)
                            //{
                            //    aCancellation.Token.ThrowIfCancellationRequested();
                            //    flag = false;
                            //    continue;
                            //}
                            else if (IsNeedAILoading(CurrentHighlightGuiNode))
                            {
                                m_ElementHighlighter.StartAILoading();
                            }
                            else
                            {
                                aCancellation.Token.ThrowIfCancellationRequested();
                                if (m_NextPoint.WaitForData(500))
                                {
                                    continue;
                                }
                                else
                                {
                                    if (!m_IsTrackOnceTime
                                    && !m_IsActionPopupOpen
                                    && !IsCtrlDown
                                    && !m_IsMoved)
                                    {
                                        if (!PreShowActionPopup())
                                        {
                                            m_NextPoint.SetData(Cursor.Position);
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        //Keys.Modifiers.HasFlag(Keys.Control);
                                        //await CaptureFromPoint(Cursor.Position);
                                        logger.LogDebug($"popup is opening:{m_IsActionPopupOpen};{IsCtrlDown}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            aCancellation.Token.ThrowIfCancellationRequested();
                            await Task.Delay(500);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogDebug("Cancel trackingLoop");
                }
            }, aCancellation.Token, creationOptions, TaskScheduler.Default);
        }
        private bool PreShowActionPopup()
        {
            try
            {
                if (CurrentNativeElementNode == null
                    || CurrentHighlightGuiNode == null)
                {
                    logger.LogWarning("当前无有效元素");
                    return false;
                }
                if (!IsHoverFactory(Cursor.Position))
                {
                    if (!IsHoverIgnoreElement(Cursor.Position)
                    && !IsHoverElementSelf(Cursor.Position)
                    && IsNeedActionPopup(CurrentHighlightGuiNode))
                    {
                        m_ElementHighlighter.SetBusy(true);
                        var vm = TrackOneElementForActionPopup(Cursor.Position, ActionOver, ActionDoneCallBack);
                        if (vm != null)
                        {
                            if (m_DesktopElement.Properties.NativeWindowHandle.ValueOrDefault == CaptureRootWnd.Properties.NativeWindowHandle.ValueOrDefault)
                            {
                                m_ElementHighlighter?.ShowActionPopup(vm, CurrentAutomationElement.Properties.NativeWindowHandle.ValueOrDefault);
                            }
                            else
                            {
                                m_ElementHighlighter?.ShowActionPopup(vm, CaptureRootWnd.Properties.NativeWindowHandle.ValueOrDefault);
                            }
                            return true;
                        }
                    }
                    else
                    {
                        logger.LogWarning("Hover:self");
                    }
                }
                else
                {
                    m_ElementHighlighter.Pause();
                    logger.LogWarning("Hover:factory");
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "PreShowActionPopup Error");
            }
            return false;
        }
        /// <summary>
        /// 拾取入口
        /// </summary>
        /// <param name="aCursorPoint"></param>
        /// <returns></returns>
        private async Task CaptureFromPoint(Point aCursorPoint)
        {
            IGuiNode guiNode = await NativeCaptureExecute(aCursorPoint);
            if (guiNode != null)
            {
                CurrentNativeElementNode = guiNode;
                ComboElementCapture(IsEnableCV, CurrentNativeElementNode, CurrentCvCaptureRootNode);
            }
            else
            {
                //logger.LogDebug("NativeCaptureExecute return null");
            }
        }
        private IGuiOperator GetGuiType(Point screenPoint,
            AutomationElement rootElement,
            AutomationElement pointElement,
            IntPtr hWndFromPoint)
        {
            var guiOperator = GuiManager.Info.GetGuiType(hWndFromPoint);
            if (guiOperator == null)
            {
                if (GuiOperatorProvider.Instance.TryGetSupportedOperator(screenPoint, rootElement, pointElement, out guiOperator))
                {
                    try
                    {
                        if (guiOperator.GuiType != eGuiType.SAP)
                            GuiManager.Info.CacheWndGuiType(hWndFromPoint, guiOperator);
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"判断{guiOperator.GuiType} GUI 类型失败，将继续尝试使用其它GUI类型进行拾取");
                    }
                }

            }
            return guiOperator;
        }
        internal void StartGuiTypeRefresh()
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    await Task.Delay(100);
                    GuiManager.Info.ClearWndGuiType();
                    while (m_IsTracking)
                    {
                        await Task.Delay(100);
                        var currentCursor = Cursor.Position;
                        bool hasGuiTypeCanUse = false;

                        //以下是原来的逻辑，间隔100 和即时停止，影响应该不大，先确认
                        // await Task.Delay(100);
                        //if (!this.m_IsTracking)
                        //{
                        //    break;
                        //}
                        if ((currentCursor != Point.Empty && CaptureRootWnd != null && CurrentAutomationElement != null) && GuiOperatorProvider.Instance.TryGetSupportedOperator(currentCursor, CaptureRootWnd, CurrentAutomationElement, out var guiOperator))
                        {
                            try
                            {
                                logger.LogDebug("StartGuiTypeRefresh");
                                hasGuiTypeCanUse = true;
                                var guiType = guiOperator.GuiType;
                                GuiManager.Info.CacheWndGuiType(m_CaptureHWndList.Last(), guiOperator);
                            }
                            catch (Exception ex)
                            {
                                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"判断{guiOperator.GuiType} GUI 类型失败，将继续尝试使用其它GUI类型进行拾取");
                                GuiManager.Info.ClearWndGuiType(m_CaptureHWndList.Last());
                            }
                        }
                        if (!hasGuiTypeCanUse)
                        {
                            logger.LogWarning($"所有GUI类型均失败，重新获取该元素");
                            await NativeCaptureExecute(currentCursor);
                        }
                    }
                    logger.LogWarning("StartGuiTypeRefresh end!");
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "StartGuiTypeRefresh");
                }
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false)
            .GetAwaiter()
            .OnCompleted(() => { });
        }
        /// <summary>
        /// 启动Cv拾取Task
        /// </summary>
        /// <param name="aCancellation"></param>
        /// <returns></returns>
        private Task StartCVCaptureTask(CancellationTokenSource aCancellation)
        {
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (true)
                    {
                        await Task.Delay(300);
                        aCancellation.Token.ThrowIfCancellationRequested();
                        if (m_IsTracking && CurrentNativeElementNode != null && !m_IsActionPopupOpen)
                        {
                            aCancellation.Token.ThrowIfCancellationRequested();
                            CurrentCvCaptureRootNode = CVCaptureExecute(CurrentCvCaptureRootNode) as IGuiCVNode;
                            if (CurrentCvCaptureRootNode == null)
                            {
                                await Task.Delay(10);
                            }
                            else
                            {

                            }
                            await Task.Yield();
                        }
                        aCancellation.Token.ThrowIfCancellationRequested();
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogDebug("StartComboCaptureTask cancel");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }, aCancellation.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// 原生元素拾取
        /// </summary>
        /// <param name="aGuiNode"></param>
        /// <param name="anStopHighLighter"></param>
        /// <returns></returns>
        private async Task<IGuiNode> NativeCaptureExecute(Point aPoint)
        {
            return await Task.Factory.StartNew(() =>
            {
                m_ElementHighlighter.Resume();
                List<JObject> IsCanUseLog = new List<JObject>();
                IGuiNode nodeResult = null;
                //anStopHighLighter = false;
                try
                {
                    Point screenPoint = aPoint;
                    bool inspectResult = false;
                    IGuiNode inspectNode = null;
                    GetPointAndWndInfo(screenPoint, out AutomationElement rootElement, out AutomationElement pointElement);
                    CaptureRootWnd = rootElement;
                    if (rootElement == null)
                    {
                        return null;
                    }
                    if (IsHoverFactory(aPoint, rootElement))
                    {
                        m_UIDispatcher.Invoke(() =>
                        {
                            if (m_IsTrackOnceTime && m_MouseTip != null)
                            {
                                m_MouseTip.IsOpen = false;
                            }
                        });
                        m_ElementHighlighter.Pause();
                        return null;
                    }
                    //黑名单元素
                    if (IsHoverIgnoreElement(aPoint, rootElement))
                    {
                        return null;
                    }
                    var hWndFromPoint = m_CaptureHWndList.Last();
                    var guiOperator = GetGuiType(screenPoint, rootElement, pointElement, hWndFromPoint);
                    if (guiOperator == null)
                    {
                        inspectResult = false;
                        logger.LogWarning("guiType is null!");
                    }
                    try
                    {
                        if (guiOperator.GuiType == eGuiType.IE || guiOperator.GuiType == eGuiType.SAP)
                        {
                            m_UIDispatcher.Invoke(() =>
                            {
                                inspectResult = guiOperator.InspectNode(screenPoint, rootElement, pointElement, this.Selector, out inspectNode);
                            });

                        }
                        else
                        {
                            inspectResult = guiOperator.InspectNode(screenPoint, rootElement, pointElement, this.Selector, out inspectNode);
                        }
                        if (!inspectResult)
                        {
                            GuiManager.Info.ClearWndGuiType(hWndFromPoint);
                        }
                        m_SpecificTagText = string.Empty;
                    }
                    catch (RPAException ex)
                    {
                        if (ex.ErrorMessageType == eErrorMessageType.Error_CaptureUnauthorizedAccessError)
                        {
                            m_SpecificTagText = ex.Message;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        //ignore
                    }
                    catch (ManagementException mex)
                    {
                        logger.LogError(mex);
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "InspectNode failed, will use win32 element");
                    }
                    if (inspectResult)
                    {
                        nodeResult = inspectNode;
                        CurrentAutomationElement = pointElement;
                        return nodeResult;
                    }
                    logger.LogWarning("拾取失败, 使用win32元素");
                    // fallback 时优先用 FromPoint 命中更精确的元素，避免 SAP 弹窗等场景下
                    // pointElement 是整个 SAP 主窗口导致高亮全屏
                    var fallbackElement = UiCommon.UIAAutomation.FromPoint(screenPoint);
                    if (fallbackElement == null)
                        fallbackElement = pointElement;
                    nodeResult = GuiManager.CreateWindowsGuiNode(fallbackElement);
                    CurrentAutomationElement = fallbackElement;
                    return nodeResult;
                }
                catch (COMException ex)
                {
                    logger.LogDebug($"拾取程序发生COM异常：{ex}");
                    return null;
                }
                catch (Exception ex)
                {
                    logger.LogError($"拾取程序发生了异常：{ex}");

                    if (ex is RPAException rpaex)
                    {
                        if (rpaex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnInstalled
                            || rpaex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnopen
                            || rpaex.ErrorMessageType == eErrorMessageType.Error_JavaExtensionUnInstalled
                            || rpaex.ErrorMessageType == eErrorMessageType.Error_ServerHubUnopen
                            || rpaex.ErrorMessageType == eErrorMessageType.Error_BrowserPluginVersionTooLow)
                        {
                            logger.LogError($"插件未安装:{rpaex.ErrorMessageType}");
                        }
                    }
                    //m_ElementHighlighter?.Close();
                    return null;
                }
            }, TaskCreationOptions.LongRunning);
        }
        /// <summary>
        /// CV拾取
        /// </summary>
        /// <param name="aCurrentCvCaptureRootNode"></param>
        /// <returns></returns>
        private IGuiNode CVCaptureExecute(IGuiCVNode aCurrentCvCaptureRootNode)
        {
            //return await Task.Run(async () =>
            //{
            IGuiNode TickOnceResult = null;
            try
            {
                //bool isNeedForceShowAppWnd = true;
                if (CaptureRootWnd == null)
                {
                    return null;
                }

                IntPtr eleHandle = IntPtr.Zero;
                var cvRootElement = GuiManager.CreateCVGuiNode(CaptureRootWnd);
                eleHandle = cvRootElement.WindowHandle;
                var eleHandleForSimilarity = CurrentAutomationElement.Properties.NativeWindowHandle.ValueOrDefault;
                //logger.LogDebug($"handle:{eleHandle};{aCurrentCvCaptureRootNode?.WindowHandle}");
                if (aCurrentCvCaptureRootNode != null && !m_IsSwitchApp && AppShotImg != null)
                {
                    if (eleHandle != IntPtr.Zero && aCurrentCvCaptureRootNode.WindowHandle == eleHandleForSimilarity)
                    {
                        var isRecapture = DetectionSimilarity(GuiManager.CreateCVGuiNode(CurrentAutomationElement));
                        if (!isRecapture)
                        {
                            TickOnceResult = aCurrentCvCaptureRootNode;
                            return TickOnceResult;
                        }
                        else
                        {

                        }
                    }
                    else
                    {
                        logger.LogDebug("Switch window");
                        Thread.Sleep(300);
                        m_IsSwitchApp = true;
                        return CVCaptureExecute(aCurrentCvCaptureRootNode);
                    }
                }
                m_IsSwitchApp = false;
                m_InCvShoting = true;
                if (ShotAppfaceImg(cvRootElement))
                {
                    m_InCvShoting = false;
                    //logger.LogDebug("ShotAppfaceImg ok");
                    ((IGuiCVNode590)cvRootElement).StopCVServerCall();
                    if (((IGuiCVNode650)cvRootElement).CVParse(cvRootElement.CacheImage, eleHandle))
                    {
                        //logger.LogDebug("CVParse ok");
                        TickOnceResult = cvRootElement;
                        if (m_IsTracking)
                        {
                            m_NextPoint.SetData(Cursor.Position);
                        }
                        m_ElementHighlighter.StopAILoading();
                    }
                    else
                    {
                        TickOnceResult = null;
                    }
                    m_LastCapturedTime = DateTime.Now;

                    //if (CaptureRootWnd.Properties.NativeWindowHandle.IsSupported)
                    //{
                    //    this.CurrentWndHendler = CaptureRootWnd.Properties.NativeWindowHandle;
                    //}
                    //else
                    //{
                    //    CurrentWndHendler = IntPtr.Zero;
                    //}
                }
                else
                {
                    logger.LogDebug($"截图失败、取消CV解析");
                    return null;
                }

            }
            catch (RPAException ex)
            {
                logger.LogDebug($"CV识别检测服务发生一次异常：{ex.ErrorMessage}");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"CV拾取程序发生了异常");
            }
            if (TickOnceResult != null)
            {
                var old_CvCaptureRootNode = aCurrentCvCaptureRootNode;
                m_CurWndArea = TickOnceResult.BoundingRectangle;
                if (old_CvCaptureRootNode != null && old_CvCaptureRootNode.CacheImage != null)
                {
                    old_CvCaptureRootNode.CacheImage.Dispose();
                }
            }
            return TickOnceResult;
            //});

        }
        /// <summary>
        /// 融合拾取-基于原生和CV
        /// </summary>
        /// <param name="anIsEnableCV"></param>
        /// <param name="aNativeGuiNode"></param>
        /// <param name="aCvRootNode"></param>
        private void ComboElementCapture(bool anIsEnableCV, IGuiNode aNativeGuiNode, IGuiCVNode aCvRootNode)
        {
            var screenPoint = Cursor.Position;
            if (anIsEnableCV)
            {
                if (TryUseCvElement(screenPoint, aNativeGuiNode, aCvRootNode, out IGuiNode aFinalGuiNode))
                {
                    CurrentHighlightGuiNode = aFinalGuiNode;
                }
                else
                {
                    CurrentHighlightGuiNode = aNativeGuiNode;
                }
            }
            else
            {
                CurrentHighlightGuiNode = aNativeGuiNode;
            }
            try
            {
                if (CurrentAutomationElement.Properties.ProcessId.TryGetValue(out int currentprocessid))
                {
                    if (LocalDataModels.FactoryProcessIds.Contains(currentprocessid))
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "ProcessId.TryGetValue error. Ignore.");
            }
            if (IsStart || m_IsTrackOnceTime)
            {
                ElementHighlight(screenPoint, CurrentHighlightGuiNode);
            }
            else
            {
                logger.LogWarning("IsStart is False.Ignore Highlight!");
            }
        }
        /// <summary>
        /// cv元素使用条件判断
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="aNativeGuiNode"></param>
        /// <param name="aCvCaptureRootNode"></param>
        /// <param name="aFinalGuiNode"></param>
        /// <returns></returns>
        private bool TryUseCvElement(Point screenPoint,
            IGuiNode aNativeGuiNode,
            IGuiCVNode aCvCaptureRootNode,
            out IGuiNode aFinalGuiNode)
        {
            if (aCvCaptureRootNode != null)
            {
                try
                {
                    bool useCVElement = true;
                    Rectangle ecNodeRect = Rectangle.Empty;
                    if (m_ExplorerPIDs.Contains(CurrentAutomationElement.Properties.ProcessId))
                    {
                        aFinalGuiNode = aNativeGuiNode;
                        return false;
                    }
                    if (aNativeGuiNode != null)
                    {
                        ecNodeRect = aNativeGuiNode.BoundingRectangle;
                        if (!ecNodeRect.IsEmpty && ecNodeRect.Width * ecNodeRect.Height <= Configurations.Info.CaptureElementChoose.MinWidth * Configurations.Info.CaptureElementChoose.MinHeight)
                        {
                            useCVElement = false;
                        }
                    }
                    if (ecNodeRect.Contains(screenPoint)
                        && useCVElement
                        && TryInspectCVElement(screenPoint, aCvCaptureRootNode, ecNodeRect, out int aceaCount, out IGuiNode aCvGuiNode))
                    {
                        if (aCvGuiNode != null)
                        {
                            useCVElement = IsUseCVElement(ecNodeRect, aCvGuiNode, aceaCount);
                            if (useCVElement)
                            {
                                aFinalGuiNode = aCvGuiNode;
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }
            aFinalGuiNode = aNativeGuiNode;
            return false;
        }
        /// <summary>
        /// 是否使用CV元素作为高亮元素
        /// </summary>
        /// <param name="ecNodeRect"></param>
        /// <param name="cvGuiNode"></param>
        /// <param name="aceaCount"></param>
        /// <returns></returns>
        private bool IsUseCVElement(Rectangle ecNodeRect, IGuiNode cvGuiNode, int aceaCount)
        {
            bool useCVElement = true;
            int cvEleArea = 0, EleArea;
            Rectangle cheRect = Rectangle.Empty;
            if (cvGuiNode != null)
            {
                cheRect = cvGuiNode.BoundingRectangle;
                var rootWndBounding = CaptureRootWnd.BoundingRectangle;
                cheRect.X += rootWndBounding.X;
                cheRect.Y += rootWndBounding.Y;
                cvEleArea = cheRect.Width * cheRect.Height;
            }
            EleArea = ecNodeRect.Width * ecNodeRect.Height;
            if (ecNodeRect.Width > 2 * Configurations.Info.CaptureElementChoose.MaxJammingPadding && ecNodeRect.Height > 2 * Configurations.Info.CaptureElementChoose.MaxJammingPadding)
            {
                ecNodeRect = GetExcludeJammingArea(cheRect, ecNodeRect);
            }
            if (!ecNodeRect.IsEmpty)
            {
                EleArea = ecNodeRect.Width * ecNodeRect.Height;
            }
            var intersectArea = Rectangle.Intersect(cheRect, ecNodeRect);
            if (aceaCount == 0)
            {
                useCVElement = false;
            }
            else if (!ecNodeRect.IsEmpty && cvEleArea >= EleArea)
            {
                useCVElement = false;
            }
            else if (aceaCount == 1)
            {
                //有60%以上 普通元素重叠区
                if (intersectArea.Width > ecNodeRect.Width * Configurations.Info.CaptureElementChoose.IntersectRatio)
                {
                    useCVElement = false;

                }
                else if (intersectArea.Height > ecNodeRect.Height * Configurations.Info.CaptureElementChoose.IntersectRatio)
                {
                    useCVElement = false;

                }
                else if (EleArea <= intersectArea.Width * intersectArea.Height * 3)
                {
                    useCVElement = false;
                }
            }
            return useCVElement;
        }
        /// <summary>
        /// 获取去干扰范围
        /// </summary>
        /// <param name="cvEleRect"></param>
        /// <param name="normalEleRect"></param>
        /// <returns></returns>
        private Rectangle GetExcludeJammingArea(Rectangle cvEleRect, Rectangle normalEleRect)
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
        /// <summary>
        /// 显示元素高亮框
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="aElementToHighlight"></param>
        private void ElementHighlight(Point screenPoint, IGuiNode aElementToHighlight)
        {
            try
            {
                m_GuiType = eGuiType.Windows;
                m_EleType = ElementType.Ordinary;
                if (aElementToHighlight == null)
                {
                    return;
                }
                var rect = aElementToHighlight.BoundingRectangle;
                string tag = string.Empty;
                if (aElementToHighlight is IGuiCVNode)
                {
                    //todo 
                }
                if (aElementToHighlight is IGuiCVNode590 && ParentContainer != null)
                {
                    m_EleType = ElementType.Intelligence;
                    var rootWnd = this.CaptureRootWnd;
                    rect = new Rectangle(rect.X + rootWnd.BoundingRectangle.X,
                       rect.Y + rootWnd.BoundingRectangle.Y,
                       rect.Width, rect.Height);
                    //rect = new Rectangle(rect.X + ParentContainer.BoundingRectangle.X,
                    //    rect.Y + ParentContainer.BoundingRectangle.Y,
                    //    rect.Width, rect.Height);
                }
                if (aElementToHighlight is IGuiNode_6_1_0 guiNode)
                {
                    m_GuiType = guiNode.GuiType;
                }
                //CurrentHighlightElementRelativeClickPos = new Point(screenPoint.X - rect.Left, screenPoint.Y - rect.Top);
                tag = GetElementControlText(aElementToHighlight.GuiNodeControlName(), m_EleType, m_SpecificTagText);
                bool isExeplorer = IsExplorer(aElementToHighlight);

                if (IsNeedDeskTopToolDetect)
                {
                    bool isBigEnough = false;
                    if (m_ElementHighlighter.BorderWindow != null)
                    {
                        try
                        {
                            var rectangle = SomeExtensions.GetCurrentDesktopSize(m_ElementHighlighter.BorderWindow);
                            isBigEnough = aElementToHighlight.BoundingRectangle.Height > rectangle.Height * 0.95
                                && aElementToHighlight.BoundingRectangle.Width > rectangle.Width * 0.95;
                            logger.LogDebug($"desktop rectangle:{rectangle.Height},{rectangle.Width}");
                            if (isBigEnough)
                            {
                                var handle = aElementToHighlight.WindowHandle;
                                NativeMethods.GetWindowThreadProcessId(handle, out uint processid);
                                var processes = Configurations.Info.DeskTopTools;
                                using (var process = Process.GetProcessById((int)processid))
                                {
                                    var processName = string.Empty;
                                    if (processes.Contains(process.ProcessName))
                                    {
                                        processName = process.ProcessName;

                                    }
                                    else if (m_ExplorerPIDs.Contains((int)processid) && IsNoAdmin)
                                    {
                                        processName = processes.FirstOrDefault(p =>
                                        {
                                            var ps = Process.GetProcessesByName(p);
                                            if (ps != null && ps.Any())
                                            {
                                                ps.ForEach(pt => pt.Dispose());
                                                return true;
                                            }
                                            return false;
                                        });
                                    }
                                    logger.LogDebug($"DesktopToolName = {processName}");
                                    if (!string.IsNullOrEmpty(processName))
                                    {
                                        if (m_IsTracking)
                                        {
                                            m_IsTracking = false;
                                        }
                                        m_UIProvider.ShowDeskTopToolWindow(processName,
                                            e
                                            =>
                                            {
                                                IsNeedDeskTopToolDetect = !e;
                                                m_IsTracking = IsStart;
                                            });
                                    }
                                }
                                logger.LogDebug($"CurrentAutomationElementProcessId = {processid}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "DeskTopToolDetect");
                        }
                    }

                }
                if (m_ElementHighlighter.ShowWithResult(rect,
                    tag,
                    m_EleType,
                    m_GuiType,
                    isExeplorer,
                    m_IsTrackOnceTime ? HighlighterPolicy.ElementInstead : HighlighterPolicy.ProcessEdit,
                    aElementToHighlight.WindowHandle))
                {
                    m_IsActionPopupOpen = false;
                }
                m_ElementHighlighter?.StopAILoading();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ElementHighlight Error:");
            }
        }
        private bool IsExplorer(IGuiNode aElementToHighlight)
        {
            if (!(aElementToHighlight is IGuiCVNode))
            {
                try
                {
                    if (m_ExplorerPIDs.Contains(CurrentAutomationElement.Properties.ProcessId))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                    return false;
                }
            }
            return false;
        }
        private void ElementTextSync()
        {
            if (CurrentHighlightGuiNode != null)
            {
                string text = null;
                try
                {
                    text = CurrentHighlightGuiNode.GetText();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
                if (!string.IsNullOrEmpty(text))
                {
                    m_ElementHighlighter.ShowSyncText(text);
                }
            }
        }
        private string GetElementControlText(string aControlType, ElementType anEleType, string aSpecificTagText)
        {
            string controlTypeText = string.Empty;
            if (!string.IsNullOrEmpty(aControlType))
            {
                if (anEleType != ElementType.Intelligence)
                {
                    controlTypeText = TargrtXmlHelper.CreateInstance().GetTranslatedNameByCode(aControlType);
                    if (string.IsNullOrEmpty(controlTypeText))
                    {
                        logger.LogWarning($"GetElementControlText:{aControlType} can not be translated!");
                        controlTypeText = aControlType;
                    }
                    if (!string.IsNullOrEmpty(aSpecificTagText))
                    {
                        controlTypeText += $"-{aSpecificTagText}";
                    }
                }
                else
                {
                    controlTypeText = "智能";
                }
            }
            else
            {
                controlTypeText = "其他";
            }
            return controlTypeText;
        }
        /// <summary>
        /// 调用flaUI获取元素
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="rootElement"></param>
        /// <param name="pointElement"></param>
        private void GetPointAndWndInfo(Point screenPoint,
            out AutomationElement rootElement,
            out AutomationElement pointElement)
        {
            rootElement = null;
            pointElement = null;

            try
            {
                var hWndList = UiCommon.GetRootWindowList(screenPoint);
                //hWndList = hWndList?.Where(h=> UiCommon.UIAAutomation.FromHandle(h)?.AutomationId != "WAOverlayWindow").ToList();
                if (hWndList.Count > 0)
                {
                    foreach (IntPtr hWnd in hWndList)
                    {
                        if (hWnd != IntPtr.Zero)
                        {
                            UiCommon.GetWindowThreadProcessId(hWnd, out var processId1);
                            if (processId1 != GlobleConfigurations.CurrentProcessId)
                            {
                                rootElement = UiCommon.UIAAutomation.FromHandle(hWnd);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        if (rootElement == null)
                        {
                            break;
                        }

                        if (rootElement.Properties.BoundingRectangle.IsSupported
                            && rootElement.BoundingRectangle.Contains(screenPoint))
                        {
                            //logger.LogWarning("find rootElement");
                            break;
                        }
                    }
                    pointElement = UiCommon.UIAAutomation.FromHandle(hWndList.Last());
                    m_CaptureHWndList = hWndList;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                logger.LogError($"获取屏幕元素时发生一次异常，将忽略本次异常继续拾取");
            }
        }

        private bool TryInspectCVElement(Point screenPoint,
            IGuiCVNode parentNode,
            Rectangle curOrdinaryRect,
            out int areaCount,
            out IGuiNode aCvGuiNode)
        {
            IGuiCVOperator650 guiCVOperator = GuiManager.CreateGuiCVOperator();
            areaCount = 0;
            Dictionary<string, object> extendParams = new Dictionary<string, object>
            {
                ["areaRect"] = curOrdinaryRect,
                ["areaCount"] = 0
            };

            var cvRootElement = GuiManager.CreateCVGuiNode(CaptureRootWnd);
            bool result = guiCVOperator.InspectNode(screenPoint, cvRootElement, Selector, extendParams, out IGuiNode inspectNode);
            //bool result = guiCVOperator.InspectNode(screenPoint, parentNode, Selector, extendParams, out IGuiNode inspectNode);
            if (extendParams["areaCount"] != null)
            {
                areaCount = int.Parse(extendParams["areaCount"].ToString());
                //logger.LogDebug($"areaCount = {extendParams["areaCount"]}");
            }
            if (result == true)
            {
                aCvGuiNode = inspectNode;
                return true;
            }
            aCvGuiNode = null;
            return false;
        }
        /// <summary>
        /// 无效元素判断：实在设计器、actionpopupwindow
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool IsHoverIgnoreElement(Point screenPoint, AutomationElement element = null)
        {
            try
            {
                if (m_IsActionPopupOpen
                && m_ElementHighlighter.IsCapturedActionPopup())
                {
                    logger.LogDebug($"ActionPopup capture Mouse");
                    return true;
                }
                if (element == null)
                {
                    GetPointAndWndInfo(screenPoint, out AutomationElement rootElement, out AutomationElement pointElement);
                    element = rootElement;
                }
                if (element != null)
                {
                    if (LocalDataModels.FactoryProcessIds.Contains(element.Properties.ProcessId.ValueOrDefault))
                    {
                        //logger.LogDebug($"Capture Factory:Match processid:{element.Properties.ProcessId.ValueOrDefault}");
                        return true;
                    }
                    if (element.Properties.ProcessId.ValueOrDefault == Process.GetCurrentProcess().Id
                        || element.Properties.Name.ValueOrDefault == "流程编辑")
                    {
                        logger.LogWarning($"Capture ActionPopupWindow");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "IsHoverIgnoreElement Error:");
                return false;
            }
        }
        private bool IsHoverFactory(Point screenPoint, AutomationElement element = null)
        {
            try
            {
                if (element == null)
                {
                    GetPointAndWndInfo(screenPoint, out AutomationElement rootElement, out AutomationElement pointElement);
                    element = rootElement;
                }
                if (element != null)
                {
                    if (LocalDataModels.FactoryProcessIds.Contains(element.Properties.ProcessId.ValueOrDefault))
                    {
                        //logger.LogError($"Capture Factory:Match processid");
                        return true;
                    }
                    if (IsExcelRecordRunning && m_ExcelProcessIds.Contains(element.Properties.ProcessId.ValueOrDefault))
                    {
                        logger.LogWarning($"Capture Excel while IsExcelRecordRunning is true!");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "IsHoverFactory");
                return false;
            }
        }
        /// <summary>
        /// 不需要弹出actionpopup元素判断
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool IsNeedActionPopup(IGuiNode guiNode)
        {
            try
            {
                if (!(guiNode is IGuiCVNode))
                {
                    if (m_DesktopElement == null)
                    {
                        logger.LogWarning("m_DesktopElement is null,re-get");
                        m_DesktopElement = UiCommon.UIAAutomation.GetDesktop();
                    }
                    if (guiNode.BoundingRectangle
                        == m_DesktopElement.BoundingRectangle)
                    {
                        return false;
                    }
                    //if (guiNode.BoundingRectangle.Width >= m_DesktopElement.ActualWidth * 0.95
                    //    && guiNode.BoundingRectangle.Height >= m_DesktopElement.ActualHeight * 0.95)
                    //{
                    //    return false;
                    //}
                    //if (!string.IsNullOrEmpty(guiNode.ProcessName) && guiNode.ProcessName.ToLower().StartsWith("explorer"))
                    //{
                    //    logger.LogWarning($"{guiNode.ProcessName} is explorer");
                    //    return false;
                    //}
                    var currentarea = guiNode.BoundingRectangle.Width * guiNode.BoundingRectangle.Height;
                    var rootarea = CaptureRootWnd.BoundingRectangle.Width * CaptureRootWnd.BoundingRectangle.Height;
                    if (currentarea > 0
                        && rootarea > 0
                        && currentarea > 0.5 * rootarea
                        && currentarea <= rootarea)
                    {
                        logger.LogWarning($"IsNeedActionPopup:{CaptureRootWnd.Properties.Name.ValueOrDefault}:{rootarea};{currentarea}");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                //Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "IsNeedActionPopup Error");
                return true;
            }
        }
        /// <summary>
        /// 不需要弹出actionpopup元素判断
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool IsNeedAILoading(IGuiNode guiNode)
        {
            try
            {
                if (m_IsActionPopupOpen || !string.IsNullOrEmpty(m_SpecificTagText) || m_InCvShoting)
                {
                    return false;
                }
                if (!(guiNode is IGuiCVNode))
                {
                    if (!(guiNode is IGuiNode_6_1_0))
                    {
                        return false;
                    }
                    if (m_GuiType != eGuiType.Windows)
                    {
                        return false;
                    }
                    if (guiNode != null
                        && m_DesktopElement != null
                        && guiNode.BoundingRectangle == m_DesktopElement.BoundingRectangle)
                    {
                        return false;
                    }
                    var currentProcessId = CurrentAutomationElement.Properties.ProcessId.ValueOrDefault;
                    //logger.LogInformation($"processexe:{currentProcessId}");
                    if (m_ExplorerPIDs.Contains(currentProcessId))
                    {
                        return false;
                    }
                    if (CurrentCvCaptureRootNode != null
                      && CurrentCvCaptureRootNode.WindowHandle != IntPtr.Zero
                      && CurrentCvCaptureRootNode.WindowHandle == CaptureRootWnd.Properties.NativeWindowHandle.ValueOrDefault)
                    {
                        return false;
                    }
                    var currentarea = guiNode.BoundingRectangle.Width * guiNode.BoundingRectangle.Height;
                    var rootarea = CaptureRootWnd.BoundingRectangle.Width * CaptureRootWnd.BoundingRectangle.Height;
                    if (rootarea > 100
                        && currentarea > 0.95 * rootarea)
                    {
                        logger.LogWarning($"{CaptureRootWnd.Properties.Name.ValueOrDefault}:{rootarea};{currentarea}");
                        if (Cursor.Position.X - CaptureRootWnd.BoundingRectangle.X > 100
                            && Cursor.Position.Y - CaptureRootWnd.BoundingRectangle.Y > 100)
                        {
                            if (CurrentCvCaptureRootNode == null
                                || CurrentCvCaptureRootNode.WindowHandle == IntPtr.Zero
                                || CurrentCvCaptureRootNode.WindowHandle != CaptureRootWnd.Properties.NativeWindowHandle.ValueOrDefault)
                            {
                                logger.LogDebug($"IsNeedAILoading:{guiNode.GetType().FullName},{m_GuiType}");
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    return false;
                    //if (CurrentCvCaptureRootNode != null
                    //    && (CurrentCvCaptureRootNode.WindowHandle == IntPtr.Zero
                    //  || CurrentCvCaptureRootNode.WindowHandle != CaptureRootWnd.Properties.NativeWindowHandle.ValueOrDefault))
                    //{
                    //    //logger.LogInformation("IsNeedAILoading:cv element but root node is null");
                    //    return true;
                    //}
                }
                return false;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "IsNeedAILoading Error");
                return false;
            }
        }
        private bool IsHoverElementSelf(Point screenPoint, AutomationElement element = null)
        {
            try
            {
                if (element == null)
                {
                    GetPointAndWndInfo(screenPoint, out AutomationElement rootElement, out AutomationElement pointElement);
                    element = rootElement;
                }
                if (element != null)
                {
                    if (element.Properties.ProcessId.ValueOrDefault == Process.GetCurrentProcess().Id
                        || element.Properties.Name.ValueOrDefault == "流程编辑")
                    {
                        logger.LogError($"Capture Myself");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError("IsHoverElementSelf Error", ex);
                return false;
            }
        }
        /// <summary>
        /// 图片相似度检测
        /// </summary>
        /// <param name="cvRootElement"></param>
        /// <returns></returns>
        private bool DetectionSimilarity(IGuiCVNode cvRootElement)
        {
            var rootElementBoundRect = cvRootElement.BoundingRectangle;
            //宽高相同
            if (m_CurWndArea.Width == rootElementBoundRect.Width && m_CurWndArea.Height == rootElementBoundRect.Height)
            {
                //var span = (DateTime.Now - m_LastCapturedTime).TotalMilliseconds;
                //if (span < Configurations.Info.AppFaceProximityMonitorInterval)
                //{
                //    return false;
                //}
                try
                {
                    using (var eleShotImg = ElementHandleShot(cvRootElement.WindowHandle))
                    {
                        if (eleShotImg == null)
                        {
                            logger.LogDebug("eleShotImg == null");
                            return false;
                        }
                        if (IsCtrlDown)
                        {
                            logger.LogDebug("按下Ctrl 不对比界面变化");
                            return false;
                        }
                        //eleShotImg.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"shot", $"eleShotImg-{DateTime.Now.ToString("HHmmss")}.png"));
                        //AppShotImg.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "shot", $"AppShotImg-{DateTime.Now.ToString("HHmmss")}.png"));
                        float sim = ZbrainAPIService.ImageSimilarity(eleShotImg, AppShotImg, cvRootElement.WindowHandle);
                        logger.LogDebug($"界面相似度检测结果：{sim},阈值:{Configurations.Info.AppFaceProximity}");
                        if (sim == -9999 || sim >= Configurations.Info.AppFaceProximity)
                        {
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
                finally
                {

                }
            }
            else
            {
                //isNeedForceShowAppWnd = false;
                logger.LogDebug("鼠标所在的窗口没有发生变化，但窗口的大小发生了变化，重新检测");
            }
            return true;
        }
        private Bitmap ElementHandleShot(IntPtr aWindowHandle)
        {
            using (CaptureProvider cs = new CaptureProvider())
            {
                if (!cs.Capture(aWindowHandle, out Bitmap bitmap))
                {
                    return null;
                }
                return bitmap;
            }
        }
        private bool ShotAppfaceImg(IGuiCVNode cvRootElement)
        {
            try
            {
                if (m_IsActionPopupOpen)
                {
                    return false;
                }
                else
                {
                    m_ElementHighlighter.Hide();
                    ZbrainAPIService.NotifyGetWindowImg(cvRootElement.WindowHandle);
                    using (var rootEleImg = cvRootElement.ElementToBitmap())
                    {
                        if (rootEleImg == null)
                        {
                            return false;
                        }
                        //AppShotImg = ImageHelper.CaptureImage(rootEleImg);
                        AppShotImg = ElementHandleShot(cvRootElement.WindowHandle);
                        cvRootElement.CacheImage = ImageHelper.CaptureImage(rootEleImg);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"CV解析截图失败：");
            }
            finally
            {
                m_ElementHighlighter.UndoHide();
                #region 保存要识别的原图，与算法联调使用

                //try
                //{
                //    if (AppShotImg != null)
                //    {
                //        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempimg");
                //        if (!Directory.Exists(path))
                //        {
                //            Directory.CreateDirectory(path);
                //        }
                //        AppShotImg.Save(Path.Combine(path, DateTime.Now.Ticks.ToString() + ".png"));
                //    }
                //}
                //catch (Exception)
                //{

                //}

                #endregion
            }
            return false;
        }

        #region 生成selector
        public SelectElementModel DoSelect(string anElementShotPath,
            IGuiNode aCurrentHighlightElement,
            GuiElement aGuiElement,
            ManualResetEventSlim aManualResetEventSlim = null)
        {
            //lock (m_SelectorLock)
            try
            {
                var sw = Stopwatch.StartNew();
                logger.LogDebug("DoSelect start");

                SelectElementModel selectModel = new SelectElementModel
                {
                    IsCvElement = false,
                    IsValid = false,
                };
                string selector = null;

                if (aCurrentHighlightElement != null)
                {
                    try
                    {
                        aGuiElement.RelativeClickPos = CurrentHighlightElementRelativeClickPos;
                        aGuiElement.screenShotPath = anElementShotPath;
                        //var taskCompletionSource = new TaskCompletionSource<bool>();
                        try
                        {
                            Thread thread = new Thread(() =>
                            {
                                try
                                {
                                    selector = AddFileAttrToSelector(aGuiElement.PureSelector, aGuiElement.screenShotPath);
                                    if (aCurrentHighlightElement is IGuiCVNodeAddition addition)
                                    {
                                        logger.LogDebug("IGuiCVNodeAddition");
                                        selectModel.CvElementAdditionJO = addition.CVResultJson;
                                        selectModel.IsCvElement = true;
                                    }
                                    //taskCompletionSource.SetResult(true);
                                }
                                catch (Exception ex)
                                {
                                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "AddFileAttrToSelector");
                                }
                            });
                            thread.SetApartmentState(ApartmentState.STA);
                            thread.IsBackground = true;
                            thread.Start();
                            thread.Join();
                            thread = null;
                        }
                        catch (Exception e)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, $"Selector thread error");
                        }
                        if (!string.IsNullOrEmpty(selector))
                        {
                            if (SelectorFormat(selector, out string aSelectorFormated))
                            {
                                selectModel.Selector = aSelectorFormated;
                                selectModel.IsValid = true;
                            }
                            else
                            {
                                selectModel.Selector = selector;
                            }
                        }
                        else
                        {
                            logger.LogError($"DoSelect Error:Original selector is null");
                        }
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"DoSelect Error:");
                    }
                }
                else
                {
                    logger.LogError("DoSelect:aCurrentHighlightElement is null");
                }
                logger.LogDebug($"DoSelect cost:{sw.ElapsedMilliseconds}");
                sw.Stop();
                return selectModel;
            }
            catch
            {
                return default;
            }
            finally
            {
                //SomeUtils.TraceThreadPool("DoSelect End");
            }
        }
        private string ToBase64(Bitmap bmp)
        {
            try
            {
                //bmp.Save(@"C:\Users\Easley\Desktop\elementshot.png",ImageFormat.Png);
                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    byte[] arr = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(arr, 0, (int)ms.Length);
                    ms.Close();
                    string strbaser64 = Convert.ToBase64String(arr);
                    return strbaser64;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Fail toBase64: {ex.Message}");
                return "";
            }
        }
        private string AddFileAttrToSelector(string orgJson, string filePath)
        {
            try
            {
                var common = JsonConvert.DeserializeObject<RPACommon>(orgJson);
                string fileContent = filePath;
                common.CommonProps.filePath = fileContent;
                common.CommonProps.captureMode = 0;
                common.CommonProps.ResDirPath = $@"res\{GlobleConfigurations.CaptureUniqueID}";
                common.CommonProps.isSmartControl = "false";
                common.CommonProps.DiscernHidden = "false";
                //统一设置Selector版本，selector的版本升级是跟着大版本统一处理，如后期有更细致的版本管理规则，改为在各依赖包内部指定版本号
                common.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;
                try
                {
                    if (common.ElementDetail.CategoryName != "Windows桌面")
                    {
                        var icon = System.Drawing.Icon.ExtractAssociatedIcon(common.ElementDetail.CategoryIconFile);
                        Bitmap bmp = icon.ToBitmap();
                        var iconfile = Path.GetDirectoryName(Path.GetDirectoryName(filePath));
                        var iconfileName = Path.GetFileNameWithoutExtension(common.ElementDetail.CategoryIconFile);
                        iconfile = Path.Combine(iconfile, iconfileName + ".ico");
                        bmp.Save(iconfile, System.Drawing.Imaging.ImageFormat.Icon);
                        common.ElementDetail.CategoryIcon = iconfileName + ".ico";
                    }
                    else
                    {
                        common.ElementDetail.CategoryIcon = "";
                    }
                    common.ElementDetail.ElementId = GlobleConfigurations.CaptureUniqueID;
                }
                catch (Exception e)
                {
                    logger.LogWarning(e);
                    common.ElementDetail.CategoryIcon = "";
                    common.ElementDetail.CategoryIconFile = "";
                    common.ElementDetail.CategoryName = "";
                    common.ElementDetail.WindowName = "";
                }
                return JsonConvert.SerializeObject(common);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return orgJson;
            }
        }
        #endregion
        #region selector format
        private bool SelectorFormat(string aSelectorOriginal, out string aSelectorFormated)
        {
            string _picturePath;
            aSelectorFormated = aSelectorOriginal;
            if (string.IsNullOrEmpty(aSelectorOriginal))
            {
                return false;
            }
            try
            {
                JObject uiSelector = JToken.Parse(aSelectorOriginal) as JObject;
                if (uiSelector["commonProps"]["filePath"] != null)
                {
                    var picturePath = uiSelector["commonProps"]["filePath"].ToString();
                    _picturePath = Path.Combine("res", GlobleConfigurations.CaptureUniqueID, Path.GetFileName(picturePath));
                    uiSelector["commonProps"]["filePath"] = _picturePath;
                }
                //uiSelector["shot"] = RPicturePath;
                if (uiSelector["elementDetail"] != null)
                {
                    var icon_name = uiSelector["elementDetail"]["categoryIcon"];
                    try
                    {
                        if (icon_name != null && !string.IsNullOrEmpty(icon_name.ToString()))
                        {
                            uiSelector["elementDetail"]["categoryIcon"] = Path.Combine("res", icon_name.ToString());
                        }
                        else
                        {
                            uiSelector["elementDetail"]["categoryIcon"] = icon_name?.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex);
                        aSelectorFormated = string.Empty;
                        return false;
                    }
                }
                aSelectorFormated = uiSelector.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return true;
        }
        private string GetElementShotPath()
        {
            var pictureRandomName = $"{new Random().Next(10000, 99999)}-{ToUnixTimeSeconds(new DateTimeOffset(DateTime.UtcNow))}.png".Trim().Replace(" ", string.Empty);
            return Path.Combine(ActionDataModels.PackagePath, "res", GlobleConfigurations.CaptureUniqueID, pictureRandomName);
        }
        public long ToUnixTimeSeconds(DateTimeOffset dateTimeOffset)
        {
            long num = dateTimeOffset.UtcDateTime.Ticks / 10000000;
            return num - 62135596800L;
        }
        #endregion


        public void IgnoreExcelElement()
        {
            IsExcelRecordRunning = true;
        }
        public void ConsiderExcelElement()
        {
            IsExcelRecordRunning = false;
        }
        /// <summary>
        /// 智能修复
        /// </summary>
        /// <param name="anElementRect"></param>
        private void ConfigAIRepair(ActionParamModel paramModel)
        {
            Task.Run(() =>
            {
                logger.LogDebug("SaveAPPFaceImg Start");
                try
                {
                    logger.LogDebug($"SaveAPPFaceImg {paramModel.ProcessName}");
                    if (paramModel.CurrentGuiNode == null)
                    {
                        logger.LogWarning($"SaveAPPFaceImg paramModel.CurrentGuiNode == null");
                    }
                    Rectangle anElementRect = CurrentHighlightGuiNode.BoundingRectangle;
                    if (anElementRect != null && anElementRect.IsEmpty)
                    {
                        return;
                    }
                    var rootwindow = paramModel.UIARootElement;
                    if (rootwindow == null)
                    {
                        logger.LogError($"SaveAPPFaceImg rootwindow == null");
                    }
                    anElementRect.X -= rootwindow.BoundingRectangle.X;
                    anElementRect.Y -= rootwindow.BoundingRectangle.Y;

                    var file = new FileInfo(paramModel.PicturePath);
                    {
                        string filePath = Path.Combine(GlobleConfigurations.PackageDirectory, ConstFields.RES_STATICFOLDER_NAME, GlobleConfigurations.CaptureUniqueID);
                        if (!Directory.Exists(filePath))
                        {
                            Directory.CreateDirectory(filePath);
                        }
                        string fileFullName = Path.Combine(filePath, ConstFields.RES_SELECTOR_APPFACEIMG_NAME);
                        file.CopyTo(fileFullName);
                    }
                    JObject jObj = new JObject();
                    JObject elementDataObj = new JObject
                {
                    new JProperty("x", anElementRect.X),
                    new JProperty("y", anElementRect.Y),
                    new JProperty("width", anElementRect.Width),
                    new JProperty("height", anElementRect.Height)
                };
                    jObj.Add(new JProperty(ConstFields.RES_SELECTOR_STATICDATA_ELEMENTDATA, elementDataObj));
                    UiCommon.SaveStaticSelectorData(GlobleConfigurations.CaptureUniqueID, ConstFields.RES_SELECTOR_STATICFILE_NAME, jObj.ToString());
                    Dictionary<string, object> wndInfo = new Dictionary<string, object>
                {
                    { "handle", (int)rootwindow.Properties.NativeWindowHandle.ValueOrDefault },
                    { "area", new[] { anElementRect.X, anElementRect.Y, anElementRect.Width, anElementRect.Height } }
                };
                    ZbrainAPIService.SendUIDToZBrain(wndInfo);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
                logger.LogDebug("SaveAPPFaceImg End");
            }).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });

        }
    }
}

