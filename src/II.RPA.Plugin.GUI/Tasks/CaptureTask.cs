using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.WinAutomation.CommonUtility.Utils;
using II.RPA.Plugin.GUI.Tasks;

namespace II.RPA.Plugin.GUI
{
    class CaptureTask<T, RT> where T : class where RT : class
    {
        private static ILogger logger = Logger.OfContext();

        protected virtual string Name { get; }
        /// <summary>
        /// 元素捕获耗时
        /// </summary>
        internal long ElementCaptureTime { get; private set; }
        /// <summary>
        /// 定时执行的事件
        /// </summary>
        protected EventHandler Tick = null;
        /// <summary>
        /// 每次执行后的回调
        /// </summary>
        protected Action<RT> TickOnceCallback = null;
        /// <summary>
        /// 拾取任务执行异常信息
        /// </summary>
        internal Action<Exception> CaptureTaskThreadException = null;
        private volatile AutoResetEvent resetEvent;

        private static readonly object obj_threadLock = new object();
        private static readonly object _obj_InstanceLock = new object();
        private volatile Thread _thread = null;
        //private volatile bool _isStart = false;


        #region Task有效性控制
        /// <summary>
        /// 决定当前Trick是否执行 
        /// </summary>

        private bool isTaskEnable;

        public bool IsTaskEnable
        {
            get { return isTaskEnable; }
        }

        public virtual void Init()
        {
            isTaskEnable = true;
        }

        public virtual void Pause()
        {
            isTaskEnable = false;
        }

        public virtual void Resume()
        {
            isTaskEnable = true;
        }
        #endregion

        private volatile CancellationTokenSource _threadCTS = null;

        private CancellationTokenSource TaskCancellationTokenSource
        {
            get
            {
                return _threadCTS;
            }
            set
            {
                CancellationTokenSource old_threadCTS = _threadCTS;
                _threadCTS = value;
                try
                {
                    if (old_threadCTS != null)
                    {
                        logger.LogDebug($"old_threadCTS.Token.CanBeCanceled:{old_threadCTS.Token.CanBeCanceled}, !old_threadCTS.IsCancellationRequested:{!old_threadCTS.IsCancellationRequested}");
                        if (!old_threadCTS.IsCancellationRequested)
                        {
                            logger.LogDebug("old_threadCTS.Cancel();");
                            old_threadCTS.Cancel();
                        }
                        logger.LogDebug($"old_threadCTS.Token.CanBeCanceled:{old_threadCTS.Token.CanBeCanceled}, !old_threadCTS.IsCancellationRequested:{!old_threadCTS.IsCancellationRequested}");
                        old_threadCTS?.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"重新赋值TaskCancellationTokenSource异常");
                }
            }
        }
        internal bool IsStart
        {
            get
            {
                if (TaskCancellationTokenSource == null)
                {
                    return false;
                }
                return !TaskCancellationTokenSource.IsCancellationRequested;
            }
        }
        internal void ThrowIfCaptureTaskStop()
        {
            if (!IsStart)
            {
                throw new CaptureTaskStopedException(Localize($"拾取任务：{this.Name}， 已停止", "controlType_CaptureTask_1CA0B72E"));
            }
        }
        private bool _isAborting = false;
        private RT _tickOnceResult;
        protected RT TickOnceResult
        {
            get
            {
                return _tickOnceResult;
            }
            set
            {
                _tickOnceResult = value;
            }
        }
        /// <summary>
        /// 执行间隔
        /// </summary>
        internal int Interval { get; set; } = 30;
        private ExecutionContext executionContext = null;

        internal ExecutionContext ExecutionContext
        {
            get
            {
                return executionContext;
            }
        }
        /// <summary>
        /// 实例
        /// </summary>
        private static T _Instance = null;
        public static T Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_obj_InstanceLock)
                    {
                        if (_Instance == null)
                        {
                            //_Instance = new T();
                            _Instance = (T)Activator.CreateInstance(typeof(T), nonPublic: true);
                        }
                    }
                }
                return _Instance;
            }
        }
        protected CaptureTask()
        {
            resetEvent = new AutoResetEvent(false);
        }
        /// <summary>
        /// 重启，其实和Start是一样的(单例)，只用于辅助阅读
        /// </summary>
        internal virtual void ReStart()
        {
            Start();
        }
        /// <summary>
        /// 启动
        /// </summary>
        internal virtual void Start()
        {
            try
            {
                lock (obj_threadLock)
                {
                    logger.LogDebug($"启动拾取任务：{this.Name}");
                    TaskCancellationTokenSource = new CancellationTokenSource();
                    if (_thread == null)
                    {
                        logger.LogDebug($"初始化拾取线程：{this.Name}");
                        executionContext = ExecutionContext.Capture();
                        _thread = new Thread(new ThreadStart(() =>
                        {
                            Run();
                        }));
                        _thread.IsBackground = true;
                        //_thread.SetApartmentState(ApartmentState.STA);
                        _thread.Start();
                    }
                    resetEvent.Set();
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"开始启动拾取任务：{this.Name} 失败");
            }
        }

        bool bIsFirstTime = true;

        private void Run()
        {
            try
            {
                TimeTrackHelper timeTrackHelper = null;
                Stopwatch CaptureStopwatch = new Stopwatch();
                logger.LogDebug($"拾取任务开始执行：{this.Name}");
                while (true)
                {
                    while (IsStart)
                    {
                        var t = Task.Delay(this.Interval);
                        CaptureStopwatch.Restart();
                        try
                        {

                            if (bIsFirstTime)
                            {
                                timeTrackHelper = new TimeTrackHelper();
                                timeTrackHelper.Start("FirstCaptureRun", TimeTrackLogLevel.info);
                            }
                            _tickOnceResult = default(RT);
                            Tick?.Invoke(this, EventArgs.Empty);
                            logger.LogDebug($"本次{this.Name} 获取元素耗时：{CaptureStopwatch.ElapsedMilliseconds}ms");
                            this.ThrowIfCaptureTaskStop();
                            TickOnceCallback?.Invoke(_tickOnceResult);
                            logger.LogDebug($"本次{this.Name}回调通知元素拾取结果耗时：{CaptureStopwatch.ElapsedMilliseconds}ms");
                            this.ElementCaptureTime = CaptureStopwatch.ElapsedMilliseconds;
                            logger.LogDebug($"本次{this.Name} 执行耗时：{CaptureStopwatch.ElapsedMilliseconds}ms");
                        }
                        catch (CaptureTaskStopedException ex)
                        {
                            logger.LogInformation(ex.Message);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex);
                            if (!ex.Message.Contains("事件无法调用任何订户")
                                 && !ex.Message.Contains("0x80040201")
                                 && !(ex is CaptureTaskStopedException))
                            {
                                CaptureTaskThreadException?.Invoke(ex);
                                break;
                            }
                        }
                        finally
                        {
                            if (bIsFirstTime)
                            {
                                timeTrackHelper.Pick("FirstCaptureRun", TimeTrackLogLevel.info);
                                timeTrackHelper.Stop("FirstCaptureRun", TimeTrackLogLevel.info);
                                bIsFirstTime = false;
                            }
                        }
                        //if (this.Interval > 0)
                        t.Wait();
                        CaptureStopwatch.Stop();
                        logger.LogDebug($"本次{this.Name} 执行 Wait 耗时：{CaptureStopwatch.ElapsedMilliseconds}ms");
                    }
                    resetEvent.WaitOne();
                }
            }
            catch (ThreadAbortException ex)
            {
                logger.LogDebug($"拾取任务：{this.Name} 被强制停止了");
                // 此处单独捕获应终止拾取任务带来的异常，且不作处理
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                if (!ex.Message.Contains("事件无法调用任何订户") &&
              !ex.Message.Contains("0x80040201"))
                {
                    CaptureTaskThreadException?.Invoke(ex);
                }
            }
        }
        /// <summary>
        /// 通过使用flag粗粒度的控制线程结束，正在执行的任务依旧会执行
        /// </summary>
        internal virtual void StopAfterExecute()
        {
            try
            {
                logger.LogDebug($"即将停止拾取任务：{this.Name} ");
                lock (obj_threadLock)
                {
                    if (TaskCancellationTokenSource != null && !TaskCancellationTokenSource.IsCancellationRequested)
                    {
                        logger.LogDebug($"停止拾取任务：{this.Name} ，CTS.Cancel");
                        TaskCancellationTokenSource?.Cancel();
                    }
                    logger.LogDebug($"停止拾取任务：{this.Name}  方法执行结束 ");

                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"停止拾取任务：{this.Name} 发生了异常，在停止CTS的时候。");
            }
        }
        internal virtual void ReStartExecute()
        {
            this.Start();
        }
        /// <summary>
        /// 使用thead.Abort终止线程。可直接停止线程，但可能会发生未知异常
        /// </summary>
        internal virtual void Abort()
        {
            try
            {
                logger.LogDebug($"开始停止拾取任务：{this.Name}");
                _isAborting = true;
                this.StopAfterExecute();
            }
            catch (Exception ex)
            {
                logger.LogWarning($"结束任务线程异常,线程{_thread}：{_thread?.ThreadState.ToString()}");
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"结束任务线程异常");
            }
            finally
            {
                //if (_thread != null)
                //    _thread = null;
                _isAborting = false;
            }
        }
    }
}

