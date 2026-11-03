using Grpc.Core;
using II.RPA.MobileDeviceManager.Log;
using II.RPA.WinAutomation.CommonUtility;
using Rpa.Core.Mobileplugin;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace II.RPA.MobileDeviceManager.Services
{
    internal class PageSourceRefreshService
    {
        private readonly ILogger _logger;
        private readonly object _dirtySync = new object();
        private readonly object _refreshSync = new object();
        private TaskCompletionSource<bool> _inFlightRefresh;
        private int _continuousFailures;
        private int _dirtyState;
        private long _dirtyVersion;
        private long _completedDirtyVersion = -1;
        private int _activeRefreshState;
        private DateTime _circuitOpenUntil = DateTime.MinValue;
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private DateTime _activeRefreshUntil = DateTime.MinValue;

        private const int GrpcDeadlineSeconds = 90;
        private const int ForceRefreshWaitSeconds = GrpcDeadlineSeconds + 1;
        private const int CircuitBreakFailureThreshold = 3;
        private const int CircuitBreakSeconds = 10;
        private const int SlowLogMilliseconds = 3000;
        private static readonly TimeSpan BackgroundInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ForceRefreshDedupInterval = TimeSpan.FromSeconds(1);

        public PageSourceRefreshService(ILogger logger)
        {
            _logger = logger;
        }

        public void MarkDirty(string reason)
        {
            lock (_dirtySync)
            {
                _dirtyVersion++;
                Interlocked.Exchange(ref _dirtyState, 1);
            }
            _activeRefreshUntil = DateTime.Now.AddSeconds(2);

            if (Interlocked.Exchange(ref _activeRefreshState, 1) == 0)
            {
                _ = ActiveRefreshWindowAsync(reason);
            }
        }

        public bool IsDirty()
        {
            return Interlocked.CompareExchange(ref _dirtyState, 0, 0) == 1;
        }

        public void RequestRefresh(string reason)
        {
            MarkDirtyOnly(reason);
        }

        public long GetRequestedVersion()
        {
            lock (_dirtySync)
            {
                return _dirtyVersion;
            }
        }

        public bool HasRefreshedThrough(long requestedVersion)
        {
            lock (_dirtySync)
            {
                return _completedDirtyVersion >= requestedVersion;
            }
        }

        public bool ShouldRefreshForCapture()
        {
            return IsDirty() || DateTime.Now <= _activeRefreshUntil;
        }

        public bool TryRefresh(
            string udid,
            MobilePluginService.MobilePluginServiceClient client,
            bool force,
            bool waitForInFlightRefresh,
            string reason,
            Action<XmlDocument, string, long> onSuccess,
            Action<string> onCircuitOpen)
        {
            if (string.IsNullOrEmpty(udid) || client == null)
            {
                return false;
            }

            DateTime now = DateTime.Now;
            if (now < _circuitOpenUntil)
            {
                onCircuitOpen?.Invoke($"PageSource刷新处于熔断期，原因[{reason}]，恢复时间[{_circuitOpenUntil:HH:mm:ss}]");
                return false;
            }

            if (force
                && Interlocked.CompareExchange(ref _dirtyState, 0, 0) == 0
                && now - _lastRefreshTime < ForceRefreshDedupInterval)
            {
                _logger.LogDebug($"跳过短时间内重复的PageSource强刷新，原因[{reason}]，距离上次成功刷新[{(now - _lastRefreshTime).TotalMilliseconds:0}ms]");
                return false;
            }

            if (!force && !ShouldBackgroundRefresh(now))
            {
                return false;
            }

            bool ownsRefresh;
            Task<bool> inFlightRefreshTask;
            lock (_refreshSync)
            {
                ownsRefresh = _inFlightRefresh == null;
                if (ownsRefresh)
                {
                    _inFlightRefresh = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                }
                inFlightRefreshTask = _inFlightRefresh.Task;
            }

            if (!ownsRefresh)
            {
                if (!force || !waitForInFlightRefresh)
                {
                    _logger.LogDebug($"PageSource刷新正在执行，本次请求不等待，原因[{reason}]");
                    return false;
                }

                return WaitForInFlightRefresh(inFlightRefreshTask, reason);
            }

            bool refreshSucceeded = false;
            long dirtyVersionAtStart;
            lock (_dirtySync)
            {
                dirtyVersionAtStart = _dirtyVersion;
            }

            try
            {
                Stopwatch totalStopwatch = Stopwatch.StartNew();
                Stopwatch requestStopwatch = Stopwatch.StartNew();
                _logger.LogDebug($"开始请求设计期PageSource，原因[{reason}]，force[{force}]，设备[{udid}]");
                var retReps = client.MobilePageSourceGetUnary(new MobileRequest
                {
                    Udid = udid,
                    Action = "DesignTimePageSource"
                }, deadline: DateTime.UtcNow.AddSeconds(GrpcDeadlineSeconds));
                requestStopwatch.Stop();

                if (retReps.Code == "3")
                {
                    _logger.LogDebug($"设计期PageSource本次暂缓，保留现有快照，触发原因[{reason}]，服务端原因[{retReps.Message}]，设备[{udid}]");
                    return false;
                }

                if (retReps.Code != "0")
                {
                    HandleFailure(udid, reason, retReps.Message);
                    return false;
                }


                string pageSource = retReps.Info;
                if (string.IsNullOrWhiteSpace(pageSource))
                {
                    HandleFailure(udid, reason, "PageSource返回内容为空");
                    return false;
                }

                Stopwatch parseStopwatch = Stopwatch.StartNew();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(pageSource);
                parseStopwatch.Stop();

                Stopwatch applyStopwatch = Stopwatch.StartNew();
                onSuccess?.Invoke(doc, pageSource, requestStopwatch.ElapsedMilliseconds);
                applyStopwatch.Stop();

                _lastRefreshTime = DateTime.Now;
                _continuousFailures = 0;
                _circuitOpenUntil = DateTime.MinValue;
                lock (_dirtySync)
                {
                    _completedDirtyVersion = dirtyVersionAtStart;
                    if (_dirtyVersion == dirtyVersionAtStart)
                    {
                        Interlocked.Exchange(ref _dirtyState, 0);
                    }
                    else
                    {
                        Interlocked.Exchange(ref _dirtyState, 1);
                    }
                }

                totalStopwatch.Stop();
                _logger.LogDebug($"设计期PageSource刷新结束，原因[{reason}]，请求耗时[{requestStopwatch.ElapsedMilliseconds}ms]，XML解析耗时[{parseStopwatch.ElapsedMilliseconds}ms]，元素树更新耗时[{applyStopwatch.ElapsedMilliseconds}ms]，总耗时[{totalStopwatch.ElapsedMilliseconds}ms]，设备[{udid}]");

                if (totalStopwatch.ElapsedMilliseconds >= SlowLogMilliseconds)
                {
                    _logger.LogWarning($"刷新手机元素节点耗时较长，原因[{reason}]，耗时[{totalStopwatch.ElapsedMilliseconds}ms]，XML长度[{pageSource.Length}]，设备[{udid}]");
                }
                refreshSucceeded = true;
                return true;
            }
            catch (RpcException ex)
            {
                HandleFailure(udid, reason, $"gRPC获取PageSource异常:{ex.StatusCode},{ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                HandleFailure(udid, reason, ex.Message);
                return false;
            }
            finally
            {
                CompleteRefresh(refreshSucceeded);
            }
        }

        private bool WaitForInFlightRefresh(Task<bool> inFlightRefreshTask, string reason)
        {
            if (!inFlightRefreshTask.Wait(TimeSpan.FromSeconds(ForceRefreshWaitSeconds)))
            {
                _logger.LogWarning($"等待在途PageSource刷新超时，原因[{reason}]，等待[{ForceRefreshWaitSeconds}s]");
                return false;
            }

            bool refreshSucceeded = inFlightRefreshTask.Result;
            _logger.LogDebug($"复用在途PageSource刷新结果，原因[{reason}]，结果[{refreshSucceeded}]");
            return refreshSucceeded;
        }

        private void CompleteRefresh(bool refreshSucceeded)
        {
            TaskCompletionSource<bool> refreshCompletion;
            lock (_refreshSync)
            {
                refreshCompletion = _inFlightRefresh;
                _inFlightRefresh = null;
            }
            refreshCompletion?.TrySetResult(refreshSucceeded);
        }

        private bool ShouldBackgroundRefresh(DateTime now)
        {
            if (Interlocked.CompareExchange(ref _dirtyState, 0, 0) == 1)
            {
                return true;
            }
            if (now <= _activeRefreshUntil)
            {
                return true;
            }
            return now - _lastRefreshTime >= BackgroundInterval;
        }

        private async System.Threading.Tasks.Task ActiveRefreshWindowAsync(string reason)
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(300);
                MarkDirtyOnly(reason);
                await System.Threading.Tasks.Task.Delay(700);
                MarkDirtyOnly(reason);
                await System.Threading.Tasks.Task.Delay(1000);
                MarkDirtyOnly(reason);
            }
            finally
            {
                Interlocked.Exchange(ref _activeRefreshState, 0);
            }
        }

        private void MarkDirtyOnly(string reason)
        {
            lock (_dirtySync)
            {
                _dirtyVersion++;
                Interlocked.Exchange(ref _dirtyState, 1);
            }
            _logger.LogDebug($"手机页面结构标记为待刷新，原因[{reason}]");
        }

        private void HandleFailure(string udid, string reason, string message)
        {
            _continuousFailures++;
            if (_continuousFailures == 1 || _continuousFailures % CircuitBreakFailureThreshold == 0)
            {
                _logger.LogWarning($"刷新手机元素节点失败，原因[{reason}]，连续失败[{_continuousFailures}]，设备[{udid}]，异常:{message}");
            }
            if (_continuousFailures >= CircuitBreakFailureThreshold)
            {
                _circuitOpenUntil = DateTime.Now.AddSeconds(CircuitBreakSeconds);
                _logger.LogWarning($"刷新手机元素节点进入熔断，暂停[{CircuitBreakSeconds}s]，设备[{udid}]");
            }
        }
    }
}
