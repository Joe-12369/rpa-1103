using II.RPA.Logging;
using II.RPA.MobileProvider.CV;
using II.RPA.MobileProvider.Params;
using II.RPA.MobileProvider.Selecter;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Resource;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.MultiTouch;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;

namespace II.RPA.MobileProvider.Provider
{
    public class MobileSession
    {
        private AndroidDriver<AndroidElement> _driver;
        private readonly static ILogger _logger = Logger.OfContext();
        public string Id => _driver.SessionId.ToString();
        private List<MobileElement> CacheList = new List<MobileElement>();
        private readonly object _cacheListLock = new object();
        private readonly object _parseXmlLock = new object();
        private readonly object _pageSourceSync = new object();
        public string PageSouce = "";
        private readonly object driverCommandLock = new object();
        private const int SlowDriverCommandLogMilliseconds = 3000;
        private const int ScreenStateAdbTimeoutMilliseconds = 5000;
        private static readonly TimeSpan ElementLookupPageSourceTtl = TimeSpan.FromSeconds(2);
        private DateTime _lastPageSourceRefreshTime = DateTime.MinValue;
        private long _pageSourceVersion;
        private long _parsedPageSourceVersion = -1;
        private readonly object _timeoutStateLock = new object();
        private int _timedOutOperationCount;
        private int _readOnlyTimedOutOperationCount;
        // 连接流程在每设备会话锁内维护；清理结束前缓存仍保留旧实例，阻止其他路径重建。
        internal Lazy<Task> ReadOnlySessionRecovery { get; set; }
        internal bool HasOnlyReadOnlyTimedOutOperations
        {
            get
            {
                lock (_timeoutStateLock)
                    return _timedOutOperationCount > 0 && _timedOutOperationCount == _readOnlyTimedOutOperationCount;
            }
        }
        private int _requiresRecovery;
        private readonly string _udid;
        public long Generation { get; }
        public int SystemPort { get; }
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public DateTime LastSuccessfulCommandAt { get; private set; } = DateTime.MinValue;

        public bool HasTimedOutOperation => Volatile.Read(ref _timedOutOperationCount) > 0;
        public bool RequiresRecovery => Volatile.Read(ref _requiresRecovery) == 1;
        public string Udid => !string.IsNullOrWhiteSpace(_udid) ? _udid : _driver.Capabilities["udid"].ToString();
        public MobileSession(AndroidDriver<AndroidElement> driver, string udid, long generation, int systemPort)
        {
            _driver = driver;
            _udid = udid;
            Generation = generation;
            SystemPort = systemPort;
        }

        private T ExecuteDriverCommand<T>(string commandName, Func<T> action, bool allowDuringRecovery = false)
        {
            EnsureCommandAvailable(commandName, allowDuringRecovery);
            Stopwatch stopwatch = Stopwatch.StartNew();
            lock (driverCommandLock)
            {
                EnsureCommandAvailable(commandName, allowDuringRecovery);
                try
                {
                    T result = action();
                    LastSuccessfulCommandAt = DateTime.UtcNow;
                    MobileAutomationStateStore.MarkCommandSuccess(GetSafeUdid(), Generation);
                    return result;
                }
                finally
                {
                    stopwatch.Stop();
                    if (stopwatch.ElapsedMilliseconds >= SlowDriverCommandLogMilliseconds)
                    {
                        _logger.LogWarning($"手机Appium命令耗时较长，命令[{commandName}]，耗时[{stopwatch.ElapsedMilliseconds}ms]，设备[{GetSafeUdid()}]");
                    }
                }
            }
        }
        private void ExecuteDriverCommand(string commandName, Action action, bool allowDuringRecovery = false)
        {
            ExecuteDriverCommand<object>(commandName, () =>
            {
                action();
                return null;
            }, allowDuringRecovery);
        }

        private void EnsureCommandAvailable(string commandName, bool allowDuringRecovery)
        {
            if (allowDuringRecovery)
            {
                return;
            }
            if (HasTimedOutOperation)
            {
                throw new InvalidOperationException($"手机会话存在尚未结束的超时命令，暂不接受新命令[{commandName}]，设备[{GetSafeUdid()}]");
            }
            if (RequiresRecovery)
            {
                throw new InvalidOperationException($"手机会话发生Driver级异常后需要恢复，暂不接受新命令[{commandName}]，设备[{GetSafeUdid()}]");
            }
        }

        private enum MobileFailureCategory
        {
            BusinessOrElement,
            CommandTimeout,
            DriverCommand,
            SessionUnavailable
        }

        public void MarkOperationTimedOut(string operationName, bool isReadOnly = false)
        {
            int count;
            lock (_timeoutStateLock)
            {
                count = ++_timedOutOperationCount;
                if (isReadOnly) _readOnlyTimedOutOperationCount++;
            }
            _logger.LogWarning($"手机命令已超时并临时隔离当前会话，等待后台命令最终结果，命令[{operationName}]，未结束超时命令数[{count}]，设备[{GetSafeUdid()}]");
        }

        public void MarkOperationFailed(string operationName, Exception exception)
        {
            MobileFailureCategory category = ClassifyFailure(exception);
            if (category == MobileFailureCategory.SessionUnavailable)
            {
                Interlocked.Exchange(ref _requiresRecovery, 1);
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(
                    _logger,
                    exception,
                    $"手机命令发生Session级异常，当前会话已标记为需要恢复，命令[{operationName}]，分类[{category}]，设备[{GetSafeUdid()}]");
                return;
            }

            _logger.LogDebug($"手机命令失败但不触发Session重建，命令[{operationName}]，分类[{category}]，异常[{exception?.GetType().Name}]，设备[{GetSafeUdid()}]");
        }

        public void MarkTimedOutOperationCompleted(string operationName, Exception completionException, bool isReadOnly = false)
        {
            MobileFailureCategory category = completionException == null
                ? MobileFailureCategory.DriverCommand
                : ClassifyFailure(completionException);

            // Session级异常必须先设置恢复标记，再解除在途隔离，避免并发请求短暂复用异常Session。
            if (completionException != null && category == MobileFailureCategory.SessionUnavailable)
            {
                Interlocked.Exchange(ref _requiresRecovery, 1);
            }

            int remaining;
            lock (_timeoutStateLock)
            {
                remaining = _timedOutOperationCount = Math.Max(0, _timedOutOperationCount - 1);
                if (isReadOnly) _readOnlyTimedOutOperationCount = Math.Max(0, _readOnlyTimedOutOperationCount - 1);
            }

            if (completionException == null)
            {
                _logger.LogWarning($"手机超时命令已在后台成功结束，当前会话可在无其他隔离原因时继续复用，命令[{operationName}]，剩余[{remaining}]，设备[{GetSafeUdid()}]");
                return;
            }

            if (category == MobileFailureCategory.SessionUnavailable)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(
                    _logger,
                    completionException,
                    $"手机超时命令后台结束时确认Session级异常，当前会话已标记为需要恢复，命令[{operationName}]，分类[{category}]，剩余[{remaining}]，设备[{GetSafeUdid()}]");
                return;
            }

            Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(
                _logger,
                completionException,
                $"手机超时命令已在后台异常结束，但异常不要求重建Session，命令[{operationName}]，分类[{category}]，剩余[{remaining}]，设备[{GetSafeUdid()}]");
        }
        private static MobileFailureCategory ClassifyFailure(Exception exception)
        {
            if (exception == null)
            {
                return MobileFailureCategory.DriverCommand;
            }

            // 先检查完整异常链中的会话故障，不能被外层元素异常或等待超时提前截断。
            bool hasElementFailure = false;
            bool hasCommandTimeout = false;
            for (Exception current = exception; current != null; current = current.InnerException)
            {
                string typeName = current.GetType().Name;
                if (string.Equals(typeName, "InvalidSessionIdException", StringComparison.Ordinal)
                    || string.Equals(typeName, "NoSuchDriverException", StringComparison.Ordinal)
                    || string.Equals(typeName, "UnreachableBrowserException", StringComparison.Ordinal))
                {
                    return MobileFailureCategory.SessionUnavailable;
                }

                if (current is NoSuchElementException
                    || current is StaleElementReferenceException
                    || current is InvalidSelectorException
                    || string.Equals(typeName, "NotFoundException", StringComparison.Ordinal)
                    || string.Equals(typeName, "ElementNotInteractableException", StringComparison.Ordinal)
                    || string.Equals(typeName, "ElementClickInterceptedException", StringComparison.Ordinal))
                {
                    hasElementFailure = true;
                }

                if (current is TimeoutException
                    || current is OperationCanceledException
                    || string.Equals(typeName, "WebDriverTimeoutException", StringComparison.Ordinal))
                {
                    hasCommandTimeout = true;
                }
            }

            string message = BuildExceptionMessage(exception).ToLowerInvariant();
            if (message.Contains("remote webdriver server") && message.Contains("timed out after"))
            {
                // HTTP层超时表示命令结果已不可确认。有副作用的操作不能自动重试，但当前Session也不能继续复用。
                return MobileFailureCategory.SessionUnavailable;
            }
            if (ContainsAny(message,
                "invalid session id",
                "session deleted",
                "session does not exist",
                "a session is either terminated or not started",
                "no such driver",
                "socket hang up",
                "could not proxy command to the remote server",
                "cannot proxy command to the remote server",
                "uiautomator2 server is not running",
                "instrumentation process is not running",
                "instrumentation process cannot be initialized",
                "connection refused",
                "connection reset",
                "econnrefused",
                "econnreset",
                "unable to connect to the remote server",
                "forcibly closed by the remote host",
                "远程主机强迫关闭",
                "目标计算机积极拒绝",
                "无法连接到远程服务器"))
            {
                return MobileFailureCategory.SessionUnavailable;
            }

            // 没有会话级故障证据时，保留普通元素异常和命令超时的分类。
            if (hasElementFailure)
            {
                return MobileFailureCategory.BusinessOrElement;
            }
            if (hasCommandTimeout)
            {
                return MobileFailureCategory.CommandTimeout;
            }

            if (ContainsAny(message,
                "no such element",
                "stale element reference",
                "invalid selector",
                "element not interactable",
                "element click intercepted"))
            {
                return MobileFailureCategory.BusinessOrElement;
            }

            if (ContainsAny(message,
                "operation has timed out",
                "command timeout",
                "request timed out",
                "task was canceled",
                "timed out after"))
            {
                return MobileFailureCategory.CommandTimeout;
            }

            return MobileFailureCategory.DriverCommand;
        }

        private static string BuildExceptionMessage(Exception exception)
        {
            var messages = new List<string>();
            for (Exception current = exception; current != null; current = current.InnerException)
            {
                if (!string.IsNullOrWhiteSpace(current.Message))
                {
                    messages.Add(current.Message);
                }
            }
            return string.Join(" | ", messages);
        }

        private static bool ContainsAny(string value, params string[] candidates)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            foreach (string candidate in candidates)
            {
                if (value.IndexOf(candidate, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private string RefreshPageSource(string caller)
        {
            return RefreshPageSource(caller, true).PageSource;
        }

        private (bool Refreshed, string PageSource) RefreshPageSource(string caller, bool force)
        {
            return ExecuteDriverCommand("PageSource-" + caller, () =>
            {
                lock (_pageSourceSync)
                {
                    if (!force
                        && !string.IsNullOrWhiteSpace(PageSouce)
                        && DateTime.UtcNow - _lastPageSourceRefreshTime < ElementLookupPageSourceTtl)
                    {
                        return (false, PageSouce);
                    }
                }

                string pageSource = _driver.PageSource;
                if (string.IsNullOrWhiteSpace(pageSource))
                {
                    throw new InvalidOperationException("Appium返回的PageSource为空");
                }

                lock (_pageSourceSync)
                {
                    PageSouce = pageSource;
                    _lastPageSourceRefreshTime = DateTime.UtcNow;
                    _pageSourceVersion++;
                    return (true, PageSouce);
                }
            });
        }

        private string GetSafeUdid()
        {
            try
            {
                return Udid;
            }
            catch
            {
                return string.Empty;
            }
        }


        /// <summary>
        /// 点击元素
        /// </summary>
        /// <param name="eleModel"></param>
        public void ClickElement(ParamClickElementModel eleModel)
        {
            ExecuteDriverCommand(nameof(ClickElement), () => ClickElementCore(eleModel));
        }

        private void ClickElementCore(ParamClickElementModel eleModel)
        {
            AppiumWebElement element = null;
            try
            {
                element = FindElement(eleModel.selector, eleModel);
                var offsetX = eleModel.offsetX;
                var offsetY = eleModel.offsetY;
                if (element != null)
                {
                    var rect = element.Rect;
                    _logger.LogDebug($"rect:{rect.ToJson()}");
                    if (eleModel.cursorPos == MouseParamDef.CUR_POS_LEFT_TOP)
                    {
                        rect.X = rect.X + 1;
                        rect.Y = rect.Y + 1;
                    }
                    else if (eleModel.cursorPos == MouseParamDef.CUR_POS_RIGHT_TOP)
                    {
                        rect.X = rect.X - 1;
                        rect.Y = rect.Y + 1;
                    }
                    else if (eleModel.cursorPos == MouseParamDef.CUR_POS_LEFT_BOTTOM)
                    {
                        rect.X = rect.X + 1;
                        rect.Y = rect.Y - 1;
                    }
                    else if (eleModel.cursorPos == MouseParamDef.CUR_POS_RIGHT_BOTTOM)
                    {
                        rect.X = rect.X - 1;
                        rect.Y = rect.Y - 1;
                    }
                    else if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER_TOP)
                    {
                        rect.Y = rect.Y + 1;
                    }
                    else if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER_LEFT)
                    {
                        rect.X = rect.X + 1;
                    }
                    else if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER_RIGHT)
                    {
                        rect.X = rect.X - 1;
                    }
                    else if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER_BOTTOM)
                    {
                        rect.Y = rect.Y - 1;
                    }
                    Actions action = new Actions(_driver);
                    TouchAction clickAction = new TouchAction(_driver);
                    if (eleModel.clickType == (int)EumClickType.SingleClick)
                    {
                        if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER && offsetX == 0 && offsetY == 0)
                        {
                            _logger.LogInformation("判断元素是否是可启用状态");
                            bool eleEnabled = element.Enabled;
                            int tryCount = 0;
                            while (!eleEnabled && tryCount < 3)
                            {
                                _logger.LogInformation($"元素判定为不可用，进行第{tryCount + 1}次重试");
                                Thread.Sleep(500);
                                eleEnabled = element.Enabled;
                                tryCount++;
                            }
                            if (!eleEnabled)
                            {
                                throw new InvalidElementStateException("元素当前不可用，无法执行点击");
                            }
                        }

                        Point clickPoint = GetSingleClickPoint(rect, eleModel.cursorPos, offsetX, offsetY);
                        ExecuteAdbTap(clickPoint.X, clickPoint.Y);
                    }                    else if (eleModel.clickType == (int)EumClickType.DoubleClick)
                    {
                        if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER)
                        {
                            if (offsetX == 0 && offsetY == 0)
                            {
                                action.DoubleClick(element).Perform();
                            }
                            else
                            {
                                clickAction.Tap(rect.X + rect.Width / 2 + offsetX, rect.Y + rect.Height / 2 + offsetY).Release().Perform();
                                clickAction = new TouchAction(_driver);
                                clickAction.Tap(rect.X + rect.Width / 2 + offsetX, rect.Y + rect.Height / 2 + offsetY).Release().Perform();
                            }
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_LEFT_TOP)
                        {
                            clickAction.Tap(rect.X + offsetX, rect.Y + offsetY).Release().Perform();
                            clickAction = new TouchAction(_driver);
                            clickAction.Tap(rect.X + offsetX, rect.Y + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_RIGHT_TOP)
                        {
                            clickAction.Tap(rect.X + rect.Width + offsetX, rect.Y + offsetY).Release().Perform();
                            clickAction = new TouchAction(_driver);
                            clickAction.Tap(rect.X + rect.Width + offsetX, rect.Y + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_LEFT_BOTTOM)
                        {
                            clickAction.Tap(rect.X + offsetX, rect.Y + rect.Height + offsetY).Release().Perform();
                            clickAction = new TouchAction(_driver);
                            clickAction.Tap(rect.X + offsetX, rect.Y + rect.Height + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_RIGHT_BOTTOM)
                        {
                            clickAction.Tap(rect.X + rect.Width + offsetX, rect.Y + rect.Height + offsetY).Release().Perform();
                            clickAction = new TouchAction(_driver);
                            clickAction.Tap(rect.X + rect.Width + offsetX, rect.Y + rect.Height + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_RANDOM)
                        {
                            System.Random rand = new System.Random(Guid.NewGuid().GetHashCode());
                            int rectX = rand.Next(rect.X, rect.X + rect.Width);
                            int rectY = rand.Next(rect.Y, rect.Y + rect.Height);
                            clickAction.Tap(rectX, rectY).Release().Perform();
                            clickAction = new TouchAction(_driver);
                            clickAction.Tap(rectX, rectY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER_TOP)
                        {
                            clickAction.Tap(rect.X + rect.Width / 2 + offsetX, rect.Y + offsetY).Release().Perform();
                            clickAction = new TouchAction(_driver);
                            clickAction.Tap(rect.X + rect.Width / 2 + offsetX, rect.Y + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER_LEFT)
                        {
                            clickAction.Tap(rect.X + offsetX, rect.Y + rect.Height / 2 + offsetY).Release().Perform();
                            clickAction = new TouchAction(_driver);
                            clickAction.Tap(rect.X + offsetX, rect.Y + rect.Height / 2 + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER_RIGHT)
                        {
                            clickAction.Tap(rect.X + rect.Width + offsetX, rect.Y + rect.Height / 2 + offsetY).Release().Perform();
                            clickAction = new TouchAction(_driver);
                            clickAction.Tap(rect.X + rect.Width + offsetX, rect.Y + rect.Height / 2 + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER_BOTTOM)
                        {
                            clickAction.Tap(rect.X + rect.Width / 2 + offsetX, rect.Y + rect.Height + offsetY).Release().Perform();
                            clickAction = new TouchAction(_driver);
                            clickAction.Tap(rect.X + rect.Width / 2 + offsetX, rect.Y + rect.Height + offsetY).Release().Perform();
                        }
                    }
                    else if (eleModel.clickType == (int)EumClickType.LongPress)
                    {
                        if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER)
                        {
                            if (offsetX == 0 && offsetY == 0)
                            {
                                clickAction.LongPress(element).Release().Perform();
                            }
                            else
                            {
                                clickAction.LongPress(rect.X + rect.Width / 2 + offsetX, rect.Y + rect.Height / 2 + offsetY).Release().Perform();
                            }
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_LEFT_TOP)
                        {
                            clickAction.LongPress(rect.X + offsetX, rect.Y + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_RIGHT_TOP)
                        {
                            clickAction.LongPress(rect.X + rect.Width + offsetX, rect.Y + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_LEFT_BOTTOM)
                        {
                            clickAction.LongPress(rect.X + offsetX, rect.Y + rect.Height + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_RIGHT_BOTTOM)
                        {
                            clickAction.LongPress(rect.X + rect.Width + offsetX, rect.Y + rect.Height + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_RANDOM)
                        {
                            System.Random rand = new System.Random(Guid.NewGuid().GetHashCode());
                            int rectX = rand.Next(rect.X, rect.X + rect.Width);
                            int rectY = rand.Next(rect.Y, rect.Y + rect.Height);
                            clickAction.LongPress(rectX, rectY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER_TOP)
                        {
                            clickAction.LongPress(rect.X + rect.Width / 2 + offsetX, rect.Y + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER_LEFT)
                        {
                            clickAction.LongPress(rect.X + offsetX, rect.Y + rect.Height / 2 + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER_RIGHT)
                        {
                            clickAction.LongPress(rect.X + rect.Width + offsetX, rect.Y + rect.Height / 2 + offsetY).Release().Perform();
                        }
                        else if (eleModel.cursorPos == MouseParamDef.CUR_POS_CENTER_BOTTOM)
                        {
                            clickAction.LongPress(rect.X + rect.Width / 2 + offsetX, rect.Y + rect.Height + offsetY).Release().Perform();
                        }
                    }
                }

            }
            catch (NotFoundException ex)
            {
                throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.ClickMobileElement, ex);
            }
            catch (RPAException rpaEx)
            {
                throw new RPAException(eErrorMessageType.Error_ClickError, eComponentType.ClickMobileElement, rpaEx.CustomMessage);
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_ClickError, eComponentType.ClickMobileElement, ex);
            }
        }

        private static Point GetSingleClickPoint(Rectangle rect, int cursorPos, int offsetX, int offsetY)
        {
            int x;
            int y;
            if (cursorPos == MouseParamDef.CUR_POS_LEFT_TOP)
            {
                x = rect.X + 1 + offsetX;
                y = rect.Y + 1 + offsetY;
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_TOP)
            {
                x = rect.X - 1 + rect.Width + offsetX;
                y = rect.Y + 1 + offsetY;
            }
            else if (cursorPos == MouseParamDef.CUR_POS_LEFT_BOTTOM)
            {
                x = rect.X + 1 + offsetX;
                y = rect.Y + rect.Height + offsetY;
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_BOTTOM)
            {
                x = rect.X - 1 + rect.Width + offsetX;
                y = rect.Y + rect.Height + offsetY;
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RANDOM)
            {
                var random = new Random(Guid.NewGuid().GetHashCode());
                x = random.Next(rect.X, rect.X + Math.Max(1, rect.Width));
                y = random.Next(rect.Y, rect.Y + Math.Max(1, rect.Height));
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_TOP)
            {
                x = rect.X + rect.Width / 2 + offsetX;
                y = rect.Y + 1 + offsetY;
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_LEFT)
            {
                x = rect.X + offsetX;
                y = rect.Y + rect.Height / 2 + offsetY;
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_RIGHT)
            {
                x = rect.X + rect.Width + offsetX;
                y = rect.Y + rect.Height / 2 + offsetY;
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_BOTTOM)
            {
                x = rect.X + rect.Width / 2 + offsetX;
                y = rect.Y + rect.Height + offsetY;
            }
            else
            {
                x = rect.X + rect.Width / 2 + offsetX;
                y = rect.Y + rect.Height / 2 + offsetY;
            }

            return new Point(Math.Max(0, x), Math.Max(0, y));
        }

        private void ExecuteAdbTap(int x, int y)
        {
            const int adbTapTimeoutMilliseconds = 5000;
            string udid = GetSafeUdid();
            Stopwatch stopwatch = Stopwatch.StartNew();
            _logger.LogDebug($"开始执行ADB坐标点击，坐标[{x},{y}]，设备[{udid}]");
            try
            {
                AdbDeviceHelper.ExecuteAdbCommandWithTimeoutAsync(
                    $"-s \"{udid.Replace("\"", "\\\"")}\" shell input tap {x} {y}",
                    adbTapTimeoutMilliseconds).GetAwaiter().GetResult();
                _logger.LogDebug($"ADB坐标点击执行完成，坐标[{x},{y}]，耗时[{stopwatch.ElapsedMilliseconds}ms]，设备[{udid}]");
            }
            catch (Exception ex)
            {
                // 点击具有副作用。即使ADB返回超时，也不能再次补点。
                throw new InvalidOperationException($"ADB坐标点击执行结果无法确认，坐标[{x},{y}]，耗时[{stopwatch.ElapsedMilliseconds}ms]，设备[{udid}]", ex);
            }
        }
        public void ClickScreenByImagePoint(ParamClickScreenByPointModel imageModel)
        {
            ExecuteDriverCommand(nameof(ClickScreenByImagePoint), () => ClickScreenByImagePointCore(imageModel));
        }

        private void ClickScreenByImagePointCore(ParamClickScreenByPointModel imageModel)
        {
            try
            {
                Rectangle rect = new Rectangle { X = imageModel.screenX, Y = imageModel.screenY };
                var offsetX = 0;
                var offsetY = 0;
                if (!imageModel.isRoundClick)
                {
                    offsetX = imageModel.offsetX;
                    offsetY = imageModel.offsetY;
                }
                Actions action = new Actions(_driver);
                TouchAction clickAction = new TouchAction(_driver);
                if (imageModel.clickType == (int)EumClickType.SingleClick)
                {
                    clickAction.Tap(rect.X + offsetX, rect.Y + offsetY).Release().Perform();
                }
                else if (imageModel.clickType == (int)EumClickType.LongPress)
                {
                    clickAction.LongPress(rect.X + offsetX, rect.Y + offsetY).Wait(imageModel.stayTime * 1000).Release().Perform();
                }
            }
            catch (NotFoundException ex)
            {
                throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.ClickMobileElement, ex);
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_ClickError, eComponentType.ClickMobileElement, ex);
            }
        }

        public string GetElementChildInfo(ParamMobileElementTextModel textModel)
        {
            try
            {
                AppiumWebElement element = FindElement(textModel.selector, textModel);
                AppiumWebElement childElement = element.FindElementsByXPath("/*").FirstOrDefault();
                if (childElement == null)
                {
                    return string.Empty;
                }

                RPACommon selectorAll = JsonHelper.ToObject<RPACommon>(textModel.selector);
                List<MobileItem> selectorItems = JsonHelper.ToObject<List<MobileItem>>(selectorAll.Selector.ToString());
                selectorItems.Add(new MobileItem
                {
                    name = childElement.GetAttribute("className"),
                    accurate = "true",
                    props = new List<Props>
                    {
                        new Props
                        {
                            name = "class",
                            value = childElement.GetAttribute("className"),
                            pattern = "equal",
                            accurate = "false"
                        },
                        new Props
                        {
                            name = "index",
                            value = "0",
                            pattern = "equal",
                            accurate = "true"
                        }
                    }
                });
                return JsonHelper.ToJson(selectorItems);
            }
            catch (NotFoundException ex)
            {
                throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetElementChild, ex);
            }
            catch (RPAException rpaEx)
            {
                throw new RPAException(eErrorMessageType.Error_GetElementChild, eComponentType.GetElementChild, rpaEx.CustomMessage);
            }
            catch (Exception ex)
            {
                throw new RPAException(eErrorMessageType.Error_GetElementChild, eComponentType.GetElementChild, ex);
            }
        }
        /// <summary>
        /// 获取相似元素
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public List<List<MobileItem>> GetSimilarElementList(string selector)
        {
            try
            {
                if (MobileSelectorRules.UsesCurrentRules(selector))
                {
                    return ExecuteDriverCommand(nameof(GetSimilarElementList), () =>
                    {
                        // Runtime list lookup reuses the existing short TTL, not an indefinitely populated cache.
                        RefreshPageSource(nameof(GetSimilarElementList), force: false);
                        ParseXml();
                        return BuildSimilarElementListFromCache(selector);
                    });
                }

                bool refreshedBeforeAnalysis = EnsureSimilarElementPageSourceSnapshot();
                if (refreshedBeforeAnalysis)
                {
                    return BuildSimilarElementListFromCache(selector);
                }

                try
                {
                    List<List<MobileItem>> result = BuildSimilarElementListFromCache(selector);
                    if (result.Count > 0)
                    {
                        return result;
                    }
                    _logger.LogInformation($"相似元素缓存分析未命中，受控刷新PageSource后重试，设备[{GetSafeUdid()}]");
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(
                        _logger,
                        ex,
                        $"使用现有PageSource缓存分析相似元素失败，将受控刷新后重试，设备[{GetSafeUdid()}]");
                }

                RefreshPageSource(nameof(GetSimilarElementList) + "Retry");
                ParseXml();
                return BuildSimilarElementListFromCache(selector);
            }
            catch (NotFoundException ex)
            {
                throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.MobileSimilarElement, ex);
            }
            catch (Exception ex)
            {
                throw new RPAException(eErrorMessageType.Error_GetSimilarElementList, eComponentType.MobileSimilarElement, ex);
            }
        }

        private bool EnsureSimilarElementPageSourceSnapshot()
        {
            try
            {
                ParseXml();
                if (CountCachedElements(_ => true) > 0)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(
                    _logger,
                    ex,
                    $"解析相似元素现有PageSource缓存失败，将受控刷新后重试，设备[{GetSafeUdid()}]");
            }

            RefreshPageSource(nameof(GetSimilarElementList));
            ParseXml();
            return true;
        }

        private List<List<MobileItem>> BuildSimilarElementListFromCache(string selector)
        {
            RPACommon selectorAll = JsonHelper.ToObject<RPACommon>(selector);
            List<MobileItem> selectorItems = JsonHelper.ToObject<List<MobileItem>>(selectorAll?.Selector?.ToString());
            if (selectorItems == null || selectorItems.Count == 0)
            {
                return new List<List<MobileItem>>();
            }

            List<MobileSelectorNode> rules = MobileSelectorRules.Parse(selector);
            if (MobileSelectorRules.UsesCurrentRules(selector))
                return BuildSimilarElementListWithCurrentRules(rules);

            List<List<MobileItem>> candidates = new List<List<MobileItem>>
            {
                CloneSelectorItems(selectorItems)
            };

            for (int pathLevel = 0; pathLevel < selectorItems.Count; pathLevel++)
            {
                MobileItem sourceItem = selectorItems[pathLevel];
                if (sourceItem?.props?.Any(prop => prop.name == "index") == true)
                {
                    continue;
                }

                candidates = ExpandSimilarCandidatesAtLevel(candidates, sourceItem, pathLevel);
                if (candidates.Count == 0)
                {
                    return candidates;
                }
            }

            List<List<MobileItem>> result = candidates
                .Where(IsAnyCurrent)
                .ToList();
            foreach (List<MobileItem> selectorItem in result)
            {
                SynchronizeNodeAccurate(selectorItem);
            }
            return result;
        }

        private List<List<MobileItem>> BuildSimilarElementListWithCurrentRules(List<MobileSelectorNode> rules)
        {
            MobileSelectorRules.BuildXPath(rules);
            var result = new List<List<MobileItem>>();
            List<MobileElement> snapshot = GetCachedElementsSnapshot(_ => true).Distinct().ToList();
            IReadOnlyList<MobileElement>[] paths = snapshot.Select(element =>
            {
                var path = new List<MobileElement>();
                for (MobileElement current = element; current != null; current = current.ParentElement)
                    path.Add(current);
                path.Reverse();
                return (IReadOnlyList<MobileElement>)path;
            }).ToArray();
            for (int elementIndex = 0; elementIndex < snapshot.Count; elementIndex++)
            {
                MobileElement element = snapshot[elementIndex];
                IReadOnlyList<MobileElement> path = paths[elementIndex];
                if (!MobileSelectorRules.MatchesPath(rules, path, node => node.ClassName, GetSimilarPropertyValue))
                    continue;

                // Keep one result per snapshot element, without a list-wide ordinal.
                string boundSelector = BuildVerifiedSelector(element.GetSelector());
                if (string.IsNullOrWhiteSpace(boundSelector))
                    throw new InvalidOperationException("Cannot bind matched similar element");
                List<MobileItem> items = JsonHelper.ToObject<List<MobileItem>>(boundSelector);
                List<MobileSelectorNode> boundRules = MobileSelectorRules.Parse(JsonHelper.ToJson(items));
                boundRules = MobileSelectorRules.BindUnique(boundRules, paths, elementIndex,
                    node => node.ClassName, GetSimilarPropertyValue);
                items = JsonHelper.ToObject<List<MobileItem>>(JsonHelper.ToJson(boundRules));
                int boundMatchCount = 0;
                foreach (IReadOnlyList<MobileElement> candidatePath in paths)
                {
                    if (MobileSelectorRules.MatchesPath(boundRules, candidatePath, node => node.ClassName, GetSimilarPropertyValue))
                        boundMatchCount++;
                    if (boundMatchCount > 1) break;
                }
                if (boundMatchCount != 1)
                    throw new InvalidOperationException("Matched similar element cannot be uniquely bound in the current snapshot");
                if (!MobileSelectorRules.MatchesPath(boundRules, path, node => node.ClassName, GetSimilarPropertyValue))
                    throw new InvalidOperationException("Bound selector does not match its source element");
                result.Add(items);
            }
            return result;
        }

        private static string GetSimilarPropertyValue(MobileElement element, string name)
        {
            switch (name)
            {
                case "class": return element.ClassName;
                case "id": return element.Id;
                case "description": return element.Desc;
                case "text": return element.Text;
                case "index": return element.NthChildIndex;
                default: throw new ArgumentException("Unsupported mobile selector property: " + name);
            }
        }

        private List<List<MobileItem>> ExpandSimilarCandidatesAtLevel(
            IEnumerable<List<MobileItem>> candidates,
            MobileItem sourceItem,
            int pathLevel)
        {
            string className = GetMobilePropertyValue(sourceItem, "class");
            if (string.IsNullOrWhiteSpace(className))
            {
                return new List<List<MobileItem>>();
            }

            bool hasIdProperty = sourceItem.props?.Any(prop => prop.name == "id") == true;
            string id = GetMobilePropertyValue(sourceItem, "id");
            List<List<MobileItem>> expandedCandidates = new List<List<MobileItem>>();
            HashSet<string> candidateKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (List<MobileItem> candidate in candidates)
            {
                IEnumerable<MobileElement> matchingElements;
                if (pathLevel == 0)
                {
                    matchingElements = GetCachedElementsSnapshot(element =>
                        element.Depth == 0
                        && element.ClassName == className
                        && (!hasIdProperty || element.Id == id));
                }
                else
                {
                    if (!TryResolveCachedElement(candidate, pathLevel, out MobileElement parentElement))
                    {
                        continue;
                    }
                    matchingElements = parentElement.Children.Where(element =>
                        element.Depth == pathLevel
                        && element.ClassName == className
                        && (!hasIdProperty || element.Id == id));
                }

                foreach (MobileElement element in matchingElements
                    .GroupBy(item => item.NthChildIndex, StringComparer.Ordinal)
                    .Select(group => group.First()))
                {
                    List<MobileItem> expanded = CloneSelectorItems(candidate);
                    MobileItem expandedItem = expanded[pathLevel];
                    if (expandedItem.props == null)
                    {
                        expandedItem.props = new List<Props>();
                    }
                    expandedItem.props.Add(new Props
                    {
                        name = "index",
                        accurate = "true",
                        value = element.NthChildIndex,
                        pattern = "equal"
                    });

                    string candidateKey = BuildSelectorPathKey(expanded);
                    if (candidateKeys.Add(candidateKey))
                    {
                        expandedCandidates.Add(expanded);
                    }
                }
            }

            _logger.LogInformation($"第{pathLevel}层级匹配的相似元素路径数量:{expandedCandidates.Count}");
            return expandedCandidates;
        }

        private static List<MobileItem> CloneSelectorItems(IEnumerable<MobileItem> items)
        {
            return items.Select(item => new MobileItem
            {
                name = item.name,
                type = item.type,
                accurate = item.accurate,
                props = item.props?.Select(prop => new Props
                {
                    name = prop.name,
                    value = prop.value,
                    pattern = prop.pattern,
                    accurate = prop.accurate
                }).ToList() ?? new List<Props>()
            }).ToList();
        }

        private static string BuildSelectorPathKey(IEnumerable<MobileItem> items)
        {
            return string.Join("\u001F", items.Select(item =>
                $"{GetMobilePropertyValue(item, "class")}\u001E{GetMobilePropertyValue(item, "index")}"));
        }

        private static string GetMobilePropertyValue(MobileItem item, string propertyName)
        {
            return item?.props?.FirstOrDefault(prop => prop.name == propertyName)?.value;
        }
        public string GetSimilarElementListByCondition(string conditionType, string conditionValue)
        {
            return ExecuteDriverCommand(nameof(GetSimilarElementListByCondition), () => GetSimilarElementListByConditionCore(conditionType, conditionValue));
        }

        private string GetSimilarElementListByConditionCore(string conditionType, string conditionValue)
        {
            try
            {
                //预留类型口子，后续可能会有 id name等条件的查询元素情况
                ReadOnlyCollection<AndroidElement> elements;
                string selectorExpression = conditionValue + "[{0}]";
                if (conditionType == "id")
                {
                    elements = _driver.FindElementsById(conditionValue);
                }
                else if (conditionType == "description")
                {
                    elements = (ReadOnlyCollection<AndroidElement>)_driver.FindElementsByAccessibilityId(conditionValue);
                }
                else if (conditionType == "name")
                {
                    elements = _driver.FindElementsByName(conditionValue);
                }
                else
                {
                    elements = _driver.FindElementsByXPath(conditionValue);
                    selectorExpression = "(" + conditionValue + ")" + "[{0}]";
                }
                int startIndex = 1;
                List<RPACommon> rpaCommonList = new List<RPACommon>();
                foreach (var element in elements)
                {
                    RPACommon rpaCommon = new RPACommon();
                    rpaCommon.CommonProps.GUIType = "mobile";
                    rpaCommon.CommonProps.QueryMode = conditionType;
                    rpaCommon.CommonProps.CssSelector = string.Format(selectorExpression, startIndex);// $"({conditionValue})[{startIndex}]";
                    WinAutomation.CommonUtility.Parse.SelectorProps selectorProps = new WinAutomation.CommonUtility.Parse.SelectorProps();
                    selectorProps.Accurate = "true";
                    selectorProps.Name = "mobile";
                    selectorProps.Type = "mobile";
                    selectorProps.Props = new List<WinAutomation.CommonUtility.Parse.Props>();
                    List<WinAutomation.CommonUtility.Parse.SelectorProps> selectorPropsList = new List<WinAutomation.CommonUtility.Parse.SelectorProps>();
                    selectorPropsList.Add(selectorProps);
                    rpaCommon.Selector = JArray.Parse(JsonConvert.SerializeObject(selectorPropsList));
                    rpaCommonList.Add(rpaCommon);
                    startIndex++;
                }
                if (rpaCommonList.Count > 0)
                {
                    return JsonConvert.SerializeObject(rpaCommonList);
                }
                throw new RPAException(eErrorMessageType.Error_GetSimilarElementList, eComponentType.MobileSimilarElement);
            }
            catch (NotFoundException ex)
            {
                throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.MobileSimilarElement, ex);
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetSimilarElementList, eComponentType.MobileSimilarElement, ex);
            }
        }

        public string GetElementByCondition(string conditionType, string conditionValue)
        {
            return ExecuteDriverCommand(nameof(GetElementByCondition), () => GetElementByConditionCore(conditionType, conditionValue));
        }

        private string GetElementByConditionCore(string conditionType, string conditionValue)
        {
            try
            {
                //预留类型口子，后续可能会有 id name等条件的查询元素情况
                AndroidElement element = null;
                if (conditionType == "id")
                {
                    element = _driver.FindElementById(conditionValue);
                }
                else if (conditionType == "description")
                {
                    element = _driver.FindElementByAccessibilityId(conditionValue);
                }
                else if (conditionType == "name")
                {
                    element = _driver.FindElementByName(conditionValue);
                }
                else
                {
                    element = _driver.FindElementByXPath(conditionValue);
                }
                if (element != null)
                {
                    RPACommon rpaCommon = new RPACommon();
                    rpaCommon.CommonProps.GUIType = "mobile";
                    rpaCommon.CommonProps.QueryMode = conditionType;
                    rpaCommon.CommonProps.CssSelector = $"{conditionValue}";
                    WinAutomation.CommonUtility.Parse.SelectorProps selectorProps = new WinAutomation.CommonUtility.Parse.SelectorProps();
                    selectorProps.Accurate = "true";
                    selectorProps.Name = "mobile";
                    selectorProps.Type = "mobile";
                    selectorProps.Props = new List<WinAutomation.CommonUtility.Parse.Props>();
                    List<WinAutomation.CommonUtility.Parse.SelectorProps> selectorPropsList = new List<WinAutomation.CommonUtility.Parse.SelectorProps>();
                    selectorPropsList.Add(selectorProps);
                    rpaCommon.Selector = JArray.Parse(JsonConvert.SerializeObject(selectorPropsList));
                    return JsonConvert.SerializeObject(rpaCommon);
                }
                throw new RPAException(eErrorMessageType.Error_GetMobileElement, eComponentType.GetMobileElement);
            }
            catch (NotFoundException ex)
            {
                throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetMobileElement, ex);
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetMobileElement, eComponentType.GetMobileElement, ex);
            }
        }

        /// <summary>
        /// 输入文本
        /// </summary>
        /// <param name="txtModel"></param>
        public bool InputText(ParamInputTextModel txtModel)
        {
            return ExecuteDriverCommand(nameof(InputText), () => InputTextCore(txtModel));
        }

        private bool InputTextCore(ParamInputTextModel txtModel)
        {
            AppiumWebElement element = null;
            try
            {
                element = FindElement(txtModel.selector, txtModel);

                if (element != null)
                {
                    ////非追加
                    //if (txtModel.appendInput == 0)
                    //{
                    //    element.Clear();
                    //    element.SendKeys(txtModel.text);
                    //}
                    //else
                    //{
                    //    element.SendKeys(txtModel.text);
                    //}
                    AndroidElement androidElement = (AndroidElement)element;
                    bool inputResult = true;
                    string inputValue = "";
                    //非追加
                    if (txtModel.appendInput == 0)
                    {
                        //element.Clear();
                        //element.SendKeys(txtModel.text);
                        androidElement.ReplaceValue(txtModel.text);
                        inputValue = txtModel.text;
                    }
                    else
                    {
                        //element.SendKeys(txtModel.text);
                        string text2 = androidElement.Text;
                        androidElement.Clear();
                        string text3 = androidElement.Text;
                        if (string.IsNullOrEmpty(text3))
                        {
                            androidElement.ReplaceValue(text2 + txtModel.text);
                            inputValue = text2 + txtModel.text;
                        }
                        else if (text3 == text2)
                        {
                            androidElement.ReplaceValue(txtModel.text);
                            inputValue = txtModel.text;
                        }
                        else
                        {
                            androidElement.ReplaceValue(text2 + txtModel.text);
                            inputValue = text2 + txtModel.text;
                        }
                    }
                    if (txtModel.isVerify == 1)
                    {
                        inputResult = androidElement.Text == inputValue;
                    }
                    if (txtModel.isEnter == 1)
                    {
                        try
                        {
                            _driver.PressKeyCode(AndroidKeyCode.Enter);
                        }
                        catch (Exception ex)
                        {
                            //快捷键部分机型可能不支持，会报错，做容错处理
                            Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"回车按键执行异常");
                        }
                    }
                    return inputResult;
                }
                return false;
            }
            catch (NotFoundException ex)
            {
                throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.SetMobileText, ex);
            }
            catch (RPAException rpaEx)
            {
                throw new RPAException(eErrorMessageType.Error_SetTextError, eComponentType.SetMobileText, rpaEx.CustomMessage);
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_SetTextError, eComponentType.SetMobileText, ex);
            }
        }
        /// <summary>
        /// 点击指定按键
        /// </summary>
        /// <param name="keyBtnModel"></param>
        public void ClickKeyButton(ParamClickKeyButtonModel keyBtnModel)
        {
            ExecuteDriverCommand(nameof(ClickKeyButton), () => ClickKeyButtonCore(keyBtnModel));
        }

        private void ClickKeyButtonCore(ParamClickKeyButtonModel keyBtnModel)
        {
            try
            {
                switch (keyBtnModel.key)
                {
                    case (int)EumKeyType.Home:
                        _driver.PressKeyCode(AndroidKeyCode.Home);
                        break;
                    case (int)EumKeyType.Back:
                        _driver.PressKeyCode(AndroidKeyCode.Back);
                        break;
                    case (int)EumKeyType.Switch:
                        _driver.PressKeyCode(AndroidKeyCode.Keycode_APP_SWITCH);
                        break;
                    case (int)EumKeyType.Enter:
                        _driver.PressKeyCode(AndroidKeyCode.Enter);
                        break;
                    case (int)EumKeyType.Revolve:
                        if (_driver.Orientation == ScreenOrientation.Landscape)
                        {
                            _driver.Orientation = ScreenOrientation.Portrait;
                        }
                        else if (_driver.Orientation == ScreenOrientation.Portrait)
                        {
                            _driver.Orientation = ScreenOrientation.Landscape;
                        }
                        break;
                    case (int)EumKeyType.Del:
                        _driver.PressKeyCode(AndroidKeyCode.Del);
                        break;
                }
            }
            catch (Exception ex)
            {
                // logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_PressKey, eComponentType.PressMobileKey, ex);
            }
        }

        /// <summary>
        /// 点击屏幕 -坐标
        /// </summary>
        /// <param name="screenModel"></param>
        public void ClickScreen(ParamClickScreenModel screenModel)
        {
            ExecuteDriverCommand(nameof(ClickScreen), () => ClickScreenCore(screenModel));
        }

        private void ClickScreenCore(ParamClickScreenModel screenModel)
        {
            try
            {
                int index = 0;
                while (index < 3)
                {
                    try
                    {
                        TouchAction touchAction = new TouchAction(_driver);
                        if (screenModel.clickType == (int)EumClickType.SingleClick)
                        {
                            touchAction.Tap(screenModel.X, screenModel.Y).Release().Perform();
                        }
                        else if (screenModel.clickType == (int)EumClickType.DoubleClick)
                        {
                            touchAction.Tap(screenModel.X, screenModel.Y).Release().Perform();
                            touchAction = new TouchAction(_driver);
                            touchAction.Tap(screenModel.X, screenModel.Y).Release().Perform();
                        }
                        else if (screenModel.clickType == (int)EumClickType.LongPress)
                        {
                            touchAction.LongPress(screenModel.X, screenModel.Y).Release().Perform();
                        }
                        return;
                    }
                    catch (Exception e)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, e, $"第{index + 1}次点击屏幕异常");
                        if (index < 2)
                        {
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            throw;
                        }
                    }
                    index++;
                }
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_ClickError, eComponentType.ClickMobileScreen, ex);
            }
        }
        /// <summary>
        /// 包含元素的滑动
        /// </summary>
        /// <param name="touchSlidModel"></param>
        /// <exception cref="RPAException"></exception>
        public void TouchSlid_WithSelector(ParamTouchSlidModel touchSlidModel)
        {
            ExecuteDriverCommand(nameof(TouchSlid_WithSelector), () => TouchSlid_WithSelectorCore(touchSlidModel));
        }

        private void TouchSlid_WithSelectorCore(ParamTouchSlidModel touchSlidModel)
        {
            try
            {
                TouchAction touchaction = new TouchAction(_driver);
                double startX, startY, endX, endY;
                //方向
                if (touchSlidModel.slidType == ((int)EmSlidType.Direction).ToString())
                {
                    var size = _driver.Manage().Window.Size;
                    int width = size.Width;
                    int height = size.Height;
                    //向上滑动
                    if (touchSlidModel.slidDir == ((int)EmSlidDir.DownToUp).ToString())
                    {
                        startX = width * 0.5;
                        startY = height * 0.75;
                        endX = width * 0.5;
                        endY = height * 0.25;
                    }
                    //向下滑动
                    else if (touchSlidModel.slidDir == ((int)EmSlidDir.UpToDown).ToString())
                    {
                        startX = width * 0.5;
                        startY = height * 0.25;
                        endX = width * 0.5;
                        endY = height * 0.75;
                    }
                    //向左滑动
                    else if (touchSlidModel.slidDir == ((int)EmSlidDir.LeftToRight).ToString())
                    {
                        startX = width * 0.25;
                        startY = height * 0.5;
                        endX = width * 0.75;
                        endY = height * 0.5;
                    }
                    //向右滑动
                    else
                    {
                        startX = width * 0.75;
                        startY = height * 0.5;
                        endX = width * 0.25;
                        endY = height * 0.5;
                    }
                }
                //通过坐标
                else
                {
                    startX = touchSlidModel.startX;
                    startY = touchSlidModel.startY;
                    endX = touchSlidModel.endX;
                    endY = touchSlidModel.endY;
                }
                //触摸屏幕
                if (touchSlidModel.slidArea == ((int)EmSlidArea.EntireScreen).ToString())
                {
                    touchaction.Press(startX, startY);
                    touchaction.Wait(touchSlidModel.slidTime);//延时
                    touchaction.MoveTo(endX, endY);
                }
                //触摸选定元素
                else
                {
                    AppiumWebElement element = null;
                    element = FindElement(touchSlidModel.selector, touchSlidModel);
                    if (element != null)
                    {
                        touchaction.LongPress(element, startX, startY);
                        touchaction.Wait(touchSlidModel.slidTime);//延时
                        touchaction.MoveTo(element, endX, endY);
                    }
                    else
                    {
                        throw new Exception("选择的元素未找到".I18n("controlType_MobileSession_A094A664"));
                    }
                }
                touchaction.Release();
                touchaction.Perform();

            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_TouchSlid, eComponentType.TouchSlideMobile, ex);
            }
        }
        /// <summary>
        /// 滑动
        /// </summary>
        /// <param name="touchSlidModel"></param>
        /// <exception cref="RPAException"></exception>
        public void TouchSlid(ParamTouchSlidModel touchSlidModel)
        {
            ExecuteDriverCommand(nameof(TouchSlid), () => TouchSlidCore(touchSlidModel));
        }

        private void TouchSlidCore(ParamTouchSlidModel touchSlidModel)
        {
            try
            {
                TouchAction touchaction = new TouchAction(_driver);
                double startX, startY, endX, endY;
                var size = _driver.Manage().Window.Size;
                int width = size.Width;
                int height = size.Height;
                startX = touchSlidModel.startX;
                startY = touchSlidModel.startY;
                if (touchSlidModel.slidType == ((int)EmSlidType.Location).ToString())
                {
                    endX = touchSlidModel.endX;
                    endY = touchSlidModel.endY;
                    touchaction.LongPress(startX, startY);
                }
                else
                {
                    //向上滑动
                    if (touchSlidModel.slidDir == ((int)EmSlidDir.DownToUp).ToString())
                    {
                        endX = startX;
                        endY = 0;
                    }
                    //向下滑动
                    else if (touchSlidModel.slidDir == ((int)EmSlidDir.UpToDown).ToString())
                    {
                        endX = startX;
                        endY = height - 1;
                    }
                    //向左滑动
                    else if (touchSlidModel.slidDir == ((int)EmSlidDir.RightToLeft).ToString())
                    {
                        endX = 0;
                        endY = startY;
                    }
                    //向右滑动
                    else
                    {
                        endX = width - 1;
                        endY = startY;
                    }
                    touchaction.Press(startX, startY);
                    touchaction.Wait(touchSlidModel.slidTime);//延时
                }
                //touchaction.Wait(touchSlidModel.slidTime);//延时
                touchaction.MoveTo(endX, endY);
                touchaction.Release();
                touchaction.Perform();

            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_TouchSlid, eComponentType.TouchSlideMobile, ex);
            }
        }
        #region 滑动

        public void TouchSlid_V2(ParamTouchSlidModel_V2 touchSlidModel)
        {
            ExecuteDriverCommand(nameof(TouchSlid_V2), () => TouchSlid_V2Core(touchSlidModel));
        }

        private void TouchSlid_V2Core(ParamTouchSlidModel_V2 touchSlidModel)
        {
            try
            {
                TouchAction touchaction = new TouchAction(_driver);
                double startX = 0, startY = 0, endX = 0, endY = 0;
                var size = _driver.Manage().Window.Size;
                int width = size.Width;
                int height = size.Height;
                //坐标
                if (touchSlidModel.slidType == EmSlidType.Location)
                {
                    // 普通单段滑动一次提交，避免旧TouchAction分开发送down/move/up造成误触。
                    if (!touchSlidModel.pressDrag
                        && !touchSlidModel.simulatedSlidingPath
                        && !touchSlidModel.simulatedSlidingRate
                        && touchSlidModel.slidRects != null
                        && touchSlidModel.slidRects.Count == 1)
                    {
                        _logger.LogDebug($"V2普通单段坐标滑动使用连续ADB手势，不发送独立按下请求，设备[{Udid}]");
                        PerformLocationSlideV3(touchSlidModel, width, height);
                        return;
                    }
                    startX = touchSlidModel.slidRects.First().startX;
                    startY = touchSlidModel.slidRects.First().startY;
                    if (touchSlidModel.pressDrag)
                    {
                        touchaction.LongPress(startX, startY);
                    }
                    else
                    {
                        touchaction.Press(startX, startY);
                        touchaction.Wait(100);
                    }
                    touchSlidModel.slidRects.ForEach(rect =>
                    {
                        endX = rect.endX;
                        endY = rect.endY;
                        touchaction.MoveTo(Math.Min(endX, width), Math.Min(endY, height));//终点坐标边界范围限制
                        touchaction.Wait((int)rect.waitAfterSlidTime);//延时
                    });
                    touchaction.Release();
                    touchaction.Perform();
                }
                //方向
                else
                {
                    // 需要等待目标时，先检查目标是否已存在
                    if (touchSlidModel.slidWaitEleShow)
                    {
                        if (CheckTargetExists(touchSlidModel))
                        {
                            _logger.LogDebug("目标元素/图像已存在，无需滑动");
                            return;
                        }
                    }
                    int slidIndex = 0;
                    do
                    {
                        _logger.LogDebug($"进行第{slidIndex + 1}次方向滑动");
                        //起点坐标
                        (startX, startY) = GetStartPosition(touchSlidModel);
                        //终点坐标
                        (endX, endY) = DeviceManager.CalculateEndPositionWithBounds(startX, startY, touchSlidModel.slidingPixel, touchSlidModel.angular, width, height);
                        _logger.LogDebug($"滑动的起点坐标[{startX},{startY}],终点坐标[{endX},{endY}]");
                        touchaction = new TouchAction(_driver);
                        // 执行滑动
                        PerformMultiSegmentSlide(
                            touchaction,
                            startX, startY,
                            endX, endY,
                            segmentCount: 3,// 三段滑动
                            maxOffset: 10,// 偏移幅度0-10像素
                            width, height,
                            touchSlidModel.pressDrag,
                            touchSlidModel.simulatedSlidingPath
                        );
                        // 检查目标是否存在
                        if (touchSlidModel.slidWaitEleShow)
                        {
                            if (CheckTargetExists(touchSlidModel))
                            {
                                _logger.LogDebug("目标元素/图像已出现，滑动成功");
                                return;
                            }
                            else
                            {
                                _logger.LogDebug("目标元素/图像未出现，继续尝试滑动");
                            }
                        }
                        else
                        {
                            _logger.LogDebug("无需等待目标图像或元素出现，滑动成功");
                            return;
                        }
                        slidIndex++;
                    } while (slidIndex < touchSlidModel.slidMaxTimes);
                    throw new RPAException(eErrorMessageType.Error_NotMatch_Image_Or_Element, eComponentType.TouchSlideMobile);
                }
            }
            catch (RPAException rpaEx)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_TouchSlid, eComponentType.TouchSlideMobile, ex);
            }
        }
        public void TouchSlid_V3(ParamTouchSlidModel_V2 touchSlidModel)
        {
            ExecuteDriverCommand(nameof(TouchSlid_V3), () => TouchSlid_V3Core(touchSlidModel));
        }

        private void TouchSlid_V3Core(ParamTouchSlidModel_V2 touchSlidModel)
        {
            try
            {
                if (touchSlidModel == null)
                {
                    throw new ArgumentNullException(nameof(touchSlidModel));
                }

                var size = _driver.Manage().Window.Size;
                int width = size.Width;
                int height = size.Height;
                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException($"无效的手机屏幕尺寸[{width}x{height}]");
                }

                if (touchSlidModel.slidType == EmSlidType.Location)
                {
                    PerformLocationSlideV3(touchSlidModel, width, height);
                    return;
                }

                PerformDirectionSlideV3(touchSlidModel, width, height);
            }
            catch (RPAException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "V3滑动执行失败");
                throw new RPAException(eErrorMessageType.Error_TouchSlid, eComponentType.TouchSlideMobile, ex);
            }
        }

        private void PerformLocationSlideV3(ParamTouchSlidModel_V2 touchSlidModel, int width, int height)
        {
            if (touchSlidModel.slidRects == null || touchSlidModel.slidRects.Count == 0)
            {
                throw new ArgumentException("坐标滑动轨迹不能为空", nameof(touchSlidModel.slidRects));
            }

            SlidRect firstRect = touchSlidModel.slidRects[0];
            var points = new List<(int x, int y)>
            {
                ClampPoint(firstRect.startX, firstRect.startY, width, height)
            };
            var durations = new List<int>();
            var pauses = new List<int>();
            foreach (SlidRect rect in touchSlidModel.slidRects)
            {
                points.Add(ClampPoint(rect.endX, rect.endY, width, height));
                durations.Add(NormalizeSlideDuration((int)rect.slidTime, touchSlidModel.slidTime));
                pauses.Add(Math.Max(0, (int)rect.waitAfterSlidTime));
            }
            EnsureEffectiveSlide(points[0], points[points.Count - 1]);

            bool useAdb = !touchSlidModel.pressDrag &&
                !touchSlidModel.simulatedSlidingPath &&
                !touchSlidModel.simulatedSlidingRate &&
                touchSlidModel.slidRects.Count == 1;
            if (useAdb)
            {
                PerformAdbSwipe(points[0], points[1], durations[0]);
                if (pauses[0] > 0)
                {
                    Thread.Sleep(pauses[0]);
                }
                return;
            }

            PerformW3cGesture(points, durations, pauses, touchSlidModel.pressDrag);
        }

        private void PerformDirectionSlideV3(ParamTouchSlidModel_V2 touchSlidModel, int width, int height)
        {
            if (touchSlidModel.slidWaitEleShow && CheckTargetExistsV3(touchSlidModel))
            {
                _logger.LogDebug("目标元素/图像已存在，无需滑动");
                return;
            }

            int maxSlideTimes = touchSlidModel.slidWaitEleShow
                ? Math.Max(1, touchSlidModel.slidMaxTimes)
                : 1;
            for (int slideIndex = 0; slideIndex < maxSlideTimes; slideIndex++)
            {
                (double rawStartX, double rawStartY) = GetStartPosition(touchSlidModel);
                var start = ClampPoint(rawStartX, rawStartY, width, height);
                (double rawEndX, double rawEndY) = DeviceManager.CalculateEndPositionWithBounds(
                    start.x,
                    start.y,
                    touchSlidModel.slidingPixel,
                    touchSlidModel.angular,
                    width - 1,
                    height - 1);
                var end = ClampPoint(rawEndX, rawEndY, width, height);
                EnsureEffectiveSlide(start, end);

                int duration = NormalizeSlideDuration(touchSlidModel.slidTime, 100);
                bool useAdb = !touchSlidModel.pressDrag &&
                    !touchSlidModel.simulatedSlidingPath &&
                    !touchSlidModel.simulatedSlidingRate;
                _logger.LogDebug($"V3进行第{slideIndex + 1}次滑动，执行器[{(useAdb ? "ADB" : "W3C")}],起点[{start.x},{start.y}],终点[{end.x},{end.y}],时长[{duration}ms]");

                if (useAdb)
                {
                    PerformAdbSwipe(start, end, duration);
                }
                else
                {
                    List<(int x, int y)> points = BuildDirectionGesturePoints(
                        start,
                        end,
                        touchSlidModel.simulatedSlidingPath,
                        touchSlidModel.simulatedSlidingRate,
                        width,
                        height);
                    List<int> durations = BuildSegmentDurations(duration, points.Count - 1, touchSlidModel.simulatedSlidingRate);
                    PerformW3cGesture(points, durations, null, touchSlidModel.pressDrag);
                }

                if (!touchSlidModel.slidWaitEleShow)
                {
                    return;
                }
                if (CheckTargetExistsV3(touchSlidModel))
                {
                    _logger.LogDebug("目标元素/图像已出现，滑动成功");
                    return;
                }
            }

            throw new RPAException(eErrorMessageType.Error_NotMatch_Image_Or_Element, eComponentType.TouchSlideMobile);
        }

        private void PerformAdbSwipe((int x, int y) start, (int x, int y) end, int durationMilliseconds)
        {
            int adbTimeout = Math.Max(5000, durationMilliseconds + 3000);
            AdbDeviceHelper.ExecuteAdbCommandWithTimeoutAsync(
                $"-s {Udid} shell input swipe {start.x} {start.y} {end.x} {end.y} {durationMilliseconds}",
                adbTimeout).GetAwaiter().GetResult();
        }

        private void PerformW3cGesture(
            IList<(int x, int y)> points,
            IList<int> durations,
            IList<int> pauses,
            bool pressDrag)
        {
            if (points == null || points.Count < 2 || durations == null || durations.Count != points.Count - 1)
            {
                throw new ArgumentException("W3C滑动轨迹参数无效");
            }

            var finger = new PointerInputDevice(PointerKind.Touch, "finger");
            var sequence = new ActionSequence(finger, 0);
            sequence.AddAction(finger.CreatePointerMove(
                CoordinateOrigin.Viewport,
                points[0].x,
                points[0].y,
                TimeSpan.Zero));
            sequence.AddAction(finger.CreatePointerDown(MouseButton.Left));
            if (pressDrag)
            {
                sequence.AddAction(finger.CreatePause(TimeSpan.FromMilliseconds(500)));
            }

            for (int index = 1; index < points.Count; index++)
            {
                sequence.AddAction(finger.CreatePointerMove(
                    CoordinateOrigin.Viewport,
                    points[index].x,
                    points[index].y,
                    TimeSpan.FromMilliseconds(durations[index - 1])));
                if (pauses != null && index - 1 < pauses.Count && pauses[index - 1] > 0)
                {
                    sequence.AddAction(finger.CreatePause(TimeSpan.FromMilliseconds(pauses[index - 1])));
                }
            }
            sequence.AddAction(finger.CreatePointerUp(MouseButton.Left));
            _driver.PerformActions(new List<ActionSequence> { sequence });
        }

        private List<(int x, int y)> BuildDirectionGesturePoints(
            (int x, int y) start,
            (int x, int y) end,
            bool simulatedPath,
            bool simulatedRate,
            int width,
            int height)
        {
            var points = new List<(int x, int y)> { start };
            if (simulatedPath || simulatedRate)
            {
                const int segmentCount = 3;
                for (int index = 1; index < segmentCount; index++)
                {
                    double ratio = (double)index / segmentCount;
                    double x = start.x + (end.x - start.x) * ratio;
                    double y = start.y + (end.y - start.y) * ratio;
                    if (simulatedPath)
                    {
                        (x, y) = GenerateMidPointWithOffset(x, y, 10, width - 1, height - 1);
                    }
                    points.Add(ClampPoint(x, y, width, height));
                }
            }
            points.Add(end);
            return points;
        }

        private static List<int> BuildSegmentDurations(int totalDuration, int segmentCount, bool simulatedRate)
        {
            if (segmentCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(segmentCount));
            }

            totalDuration = Math.Max(totalDuration, segmentCount);
            var weights = new double[segmentCount];
            double totalWeight = 0;
            for (int index = 0; index < segmentCount; index++)
            {
                weights[index] = simulatedRate && (index == 0 || index == segmentCount - 1) ? 1.25 : 1.0;
                totalWeight += weights[index];
            }

            var result = new List<int>(segmentCount);
            int allocated = 0;
            for (int index = 0; index < segmentCount; index++)
            {
                int duration = index == segmentCount - 1
                    ? Math.Max(1, totalDuration - allocated)
                    : Math.Max(1, (int)Math.Round(totalDuration * weights[index] / totalWeight));
                result.Add(duration);
                allocated += duration;
            }
            return result;
        }

        private bool CheckTargetExistsV3(ParamTouchSlidModel_V2 touchSlidModel)
        {
            try
            {
                if (touchSlidModel.targetType == 0)
                {
                    return FindElement(touchSlidModel.targetSelector, touchSlidModel) != null;
                }
                if (touchSlidModel.targetType == 1)
                {
                    var rectDatas = GetImageRects(new ParamElementShotClickModel
                    {
                        imgSrc = touchSlidModel.targetShotInfo.imgSrc,
                        offsetX = touchSlidModel.targetShotInfo.offsetX,
                        offsetY = touchSlidModel.targetShotInfo.offsetY,
                        thr = touchSlidModel.targetShotInfo.thr,
                        deviceResolution = touchSlidModel.targetShotInfo.deviceResolution,
                        scopeRect = touchSlidModel.targetShotInfo.scopeRect,
                    });
                    return rectDatas != null && rectDatas.Count > 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                if (ClassifyFailure(ex) == MobileFailureCategory.SessionUnavailable)
                {
                    throw;
                }
                return false;
            }
        }

        private static (int x, int y) ClampPoint(double x, double y, int width, int height)
        {
            int maxX = Math.Max(0, width - 1);
            int maxY = Math.Max(0, height - 1);
            return (
                Math.Max(0, Math.Min(maxX, (int)Math.Round(x))),
                Math.Max(0, Math.Min(maxY, (int)Math.Round(y))));
        }

        private static int NormalizeSlideDuration(int duration, int fallback)
        {
            int effectiveDuration = duration > 0 ? duration : fallback;
            return Math.Max(1, Math.Min(60000, effectiveDuration > 0 ? effectiveDuration : 100));
        }

        private static void EnsureEffectiveSlide((int x, int y) start, (int x, int y) end)
        {
            if (start.x == end.x && start.y == end.y)
            {
                throw new ArgumentException($"滑动起点和终点相同[{start.x},{start.y}]");
            }
        }
        /// <summary>
        /// 生成带随机偏移的中间点，确保不超出屏幕边界
        /// </summary>
        private (double x, double y) GenerateMidPointWithOffset(
            double originalX, double originalY,
            double maxOffset, int width, int height)
        {
            var random = new Random();
            double offsetX = (random.NextDouble() * 2 - 1) * maxOffset;
            double offsetY = (random.NextDouble() * 2 - 1) * maxOffset;

            double newX = Math.Max(0, Math.Min(width, originalX + offsetX));
            double newY = Math.Max(0, Math.Min(height, originalY + offsetY));

            return (newX, newY);
        }

        /// <summary>
        /// 分段滑动核心逻辑
        /// </summary>
        private void PerformMultiSegmentSlide(
            TouchAction touchaction,
            double startX, double startY,
            double endX, double endY,
            int segmentCount,// 3表示三段滑动（两段中间点）
            double maxOffset,// 最大偏移幅度
            int width, int height,
            bool pressDrag,
            bool simulatedSlidingPath)
        {
            var points = new List<(double x, double y)> { (startX, startY) };
            if (simulatedSlidingPath)
            {
                // 在起点和终点之间生成中间点
                for (int i = 1; i < segmentCount; i++)
                {
                    double t = (double)i / segmentCount; // 0.33, 0.66
                    double midX = startX + (endX - startX) * t;
                    double midY = startY + (endY - startY) * t;

                    var offsetPoint = GenerateMidPointWithOffset(midX, midY, maxOffset, width, height);
                    points.Add(offsetPoint);
                }
            }
            points.Add((endX, endY));
            _logger.LogDebug($"滑动过程坐标信息：{points.ToJson()}");
            //长按滑动
            if (pressDrag)
            {
                touchaction.LongPress(points[0].x, points[0].y);
            }
            else
            {
                touchaction.Press(points[0].x, points[0].y);
                touchaction.Wait(100);
            }
            // 依次移动到每个点
            for (int i = 1; i < points.Count; i++)
            {
                touchaction.MoveTo(points[i].x, points[i].y);
            }
            touchaction.Release();
            touchaction.Perform();
        }
        /// <summary>
        /// 获取起点位置
        /// </summary>
        private (double x, double y) GetStartPosition(ParamTouchSlidModel_V2 touchSlidModel)
        {
            double startX, startY;

            if (touchSlidModel.startType == 0) // 元素
            {
                var element = FindElement(touchSlidModel.startSelector, touchSlidModel);
                if (element != null)
                {
                    var eleRect = element.Rect;
                    startX = eleRect.X + eleRect.Width / 2;
                    startY = eleRect.Y + eleRect.Height / 2;
                }
                else
                {
                    throw new Exception("未找到起始位置元素");
                }
            }
            else if (touchSlidModel.startType == 1) // 图像
            {
                var paramImage = new ParamElementShotClickModel
                {
                    imgSrc = touchSlidModel.startShotInfo.imgSrc,
                    offsetX = touchSlidModel.startShotInfo.offsetX,
                    offsetY = touchSlidModel.startShotInfo.offsetY,
                    thr = touchSlidModel.startShotInfo.thr,
                    deviceResolution = touchSlidModel.startShotInfo.deviceResolution,
                    scopeRect = touchSlidModel.startShotInfo.scopeRect,
                };
                var rectDatas = GetImageRects(paramImage);
                if (rectDatas != null && rectDatas.Count > 0)
                {
                    var rectData = rectDatas[0];
                    startX = (rectData[0][0] + rectData[1][0]) / 2;
                    startY = (rectData[0][1] + rectData[1][1]) / 2;
                }
                else
                {
                    throw new Exception("未找到起始图像");
                }
            }
            else if (touchSlidModel.startType == 2) // 坐标
            {
                startX = touchSlidModel.startX;
                startY = touchSlidModel.startY;
            }
            else
            {
                throw new Exception("未知的起点类型");
            }

            return (startX, startY);
        }
        #endregion



        private bool CheckTargetExists(ParamTouchSlidModel_V2 touchSlidModel)
        {
            if (touchSlidModel.targetType == 0)
            {
                try
                {
                    var element = FindElement(touchSlidModel.targetSelector, touchSlidModel);
                    return element != null;
                }
                catch
                {
                    return false;
                }
            }
            else if (touchSlidModel.targetType == 1)
            {
                var rectDatas = GetImageRects(new ParamElementShotClickModel
                {
                    imgSrc = touchSlidModel.targetShotInfo.imgSrc,
                    offsetX = touchSlidModel.targetShotInfo.offsetX,
                    offsetY = touchSlidModel.targetShotInfo.offsetY,
                    thr = touchSlidModel.targetShotInfo.thr,
                    deviceResolution = touchSlidModel.targetShotInfo.deviceResolution,
                    scopeRect = touchSlidModel.targetShotInfo.scopeRect,
                });
                return rectDatas != null && rectDatas.Count > 0;
            }
            return false;
        }

        /// <summary>
        /// 获取元素截图
        /// </summary>
        /// <param name="screenModel"></param>
        /// <returns></returns>
        /// <exception cref="RPAException"></exception>
        public Screenshot GetElementScreenshot(ParamElementScreenshotModel screenModel)
        {
            AppiumWebElement element = null;
            Screenshot screenshot = null;
            try
            {
                if (!Directory.Exists(screenModel.savePath))
                {
                    throw new ArgumentException("截屏保存的文件路径不存在".I18n("controlType_MobileSession_EB867DD8"));
                }
                element = FindElement(screenModel.selector, screenModel);

                if (element != null)
                {
                    screenshot = element.GetScreenshot();
                    screenshot.SaveAsFile(screenModel.savePath + @"\" + screenModel.fileName);
                }
                return screenshot;
            }
            catch (NotFoundException ex)
            {
                throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetMobileElementScreenshot, ex);
            }
            catch (RPAException rpaEx)
            {
                throw new RPAException(eErrorMessageType.Error_GetScreenshot, eComponentType.GetMobileElementScreenshot, rpaEx.CustomMessage);
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetScreenshot, eComponentType.GetMobileElementScreenshot, ex);
            }
        }
        /// <summary>
        /// 获取屏幕截图
        /// </summary>
        /// <param name="screenModel"></param>
        /// <returns></returns>
        /// <exception cref="RPAException"></exception>
        public Screenshot GetScreenshot(ParamScreenshotModel screenModel)
        {
            Screenshot screenshot = null;
            try
            {
                if (!Directory.Exists(screenModel.savePath))
                {
                    throw new ArgumentException("截屏保存的文件路径不存在".I18n("controlType_MobileSession_EB867DD8"));
                }
                screenshot = ExecuteDriverCommand("GetScreenshot", () => _driver.GetScreenshot());
                screenshot.SaveAsFile(screenModel.savePath + @"\" + screenModel.fileName);
                return screenshot;
            }
            catch (Exception ex)
            {
                // logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetScreenshot, eComponentType.GetMobileScreenshot, ex);
            }
        }

        public string GetFromClipboard()
        {
            string msg = string.Empty;
            try
            {
                msg = _driver.GetClipboardText();
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetClipboard, eComponentType.GetFromMobileClipboard, ex);
            }
            return msg;
        }

        public void SetClipboard(ParamSetClipboardModel model)
        {
            ExecuteDriverCommand(nameof(SetClipboard), () => SetClipboardCore(model));
        }

        private void SetClipboardCore(ParamSetClipboardModel model)
        {
            try
            {
                string encodedStr = Convert.ToBase64String(Encoding.UTF8.GetBytes(model.text));
                _driver.SetClipboard(OpenQA.Selenium.Appium.Enums.ClipboardContentType.PlainText, encodedStr);
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_SetClipboard, eComponentType.SetMobileClipboard, ex);
            }
        }

        public string GetOriention()
        {
            string msg = string.Empty;
            try
            {
                msg = _driver.Orientation == ScreenOrientation.Landscape ? "Landscape" : "Portrait";
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetOrientation, eComponentType.GetMobileOrientation, ex);
            }
            return msg;
        }

        public void SetOriention(ParamSetOrientionModel model)
        {
            ExecuteDriverCommand(nameof(SetOriention), () => SetOrientionCore(model));
        }

        private void SetOrientionCore(ParamSetOrientionModel model)
        {
            try
            {
                if (model.oriention == "Landscape")
                {
                    _driver.Orientation = ScreenOrientation.Landscape;

                }
                else if (model.oriention == "Portrait")
                {
                    _driver.Orientation = ScreenOrientation.Portrait;
                }
                else
                {
                    throw new ArgumentException("入参错误；只能设置为横屏或竖屏".I18n("controlType_MobileSession_54BF48CA"));
                }
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_SetOrientation, eComponentType.SetMobileOrientation, ex);
            }
        }

        public string GetUISource()
        {
            string result = string.Empty;
            try
            {
                result = RefreshPageSource(nameof(GetUISource));
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetUiTree, eComponentType.GetMobileUiTree, ex);

            }
            return result;
        }

        public void PushFileToMobile(ParamPushFileToMobileModel fileModel)
        {
            try
            {
                if (!File.Exists(fileModel.source))
                {
                    throw new FileNotFoundException("本地文件不存在".I18n("controlType_MobileSession_453B1B1B"));
                }
                _driver.PushFile(fileModel.target, new FileInfo(fileModel.source));
            }
            catch (FileNotFoundException ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_FileNotFound, eComponentType.PushFileToMobile);
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_PushFile, eComponentType.PushFileToMobile, ex);
            }
        }

        public void GetFileFromMobile(ParamGetFileFromMobileModel fileModel)
        {
            try
            {
                byte[] fileBase64 = _driver.PullFile(fileModel.source);
                using (FileStream fs = File.Open(fileModel.target, FileMode.OpenOrCreate))
                {
                    fs.Write(fileBase64, 0, fileBase64.Length);
                    fs.Flush();
                }
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetFileFromMobile, eComponentType.GetFileFromMobile, ex);
            }
        }


        public void Dial(ParamDialModel model)
        {
            ExecuteDriverCommand(nameof(Dial), () => DialCore(model));
        }

        private void DialCore(ParamDialModel model)
        {
            try
            {
                if (model.mobileBrand.Contains("华为") || model.mobileBrand.Contains("荣耀"))
                {
                    this.HuaWeiDial(model);
                }
                _driver.PressKeyCode(AndroidKeyCode.Home);  //home键
                                                            //var element=   driver.FindElementByXPath("android.widget.ImageView[@content-desc=\"电话\"]");
                                                            // element.Click();
                _driver.PressKeyCode(AndroidKeyCode.Keycode_CALL);   //拨号键
                int index = 0;
                while (true)
                {
                    _driver.PressKeyCode(AndroidKeyCode.Keycode_DEL);   //退格键
                    index++;
                    if (index > 20)
                    {
                        break;
                    }
                }
                foreach (var chr in model.diaNumber)
                {
                    int key = 7;
                    switch (chr)    //这里需要装换，不能只是数字，还要加入+*#
                    {
                        case '0':
                            key = 7;
                            break;
                        case '1':
                            key = 8;
                            break;
                        case '2':
                            key = 9;
                            break;
                        case '3':
                            key = 10;
                            break;
                        case '4':
                            key = 11;
                            break;
                        case '5':
                            key = 12;
                            break;
                        case '6':
                            key = 13;
                            break;
                        case '7':
                            key = 14;
                            break;
                        case '8':
                            key = 15;
                            break;
                        case '9':
                            key = 16;
                            break;
                        case '+':
                            key = 81;   // KEYCODE_PLUS
                            break;
                        case '*':
                            key = 17;   // KEYCODE_STAR
                            break;
                        case '#':
                            key = 18;   // KEYCODE_POUND
                            break;
                    }
                    _driver.PressKeyCode(key);
                }
                _driver.PressKeyCode(AndroidKeyCode.Keycode_CALL);
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_Dial, eComponentType.Dial, ex);
            }
        }

        public void HuaWeiDial(ParamDialModel model)
        {
            ExecuteDriverCommand(nameof(HuaWeiDial), () => HuaWeiDialCore(model));
        }

        private void HuaWeiDialCore(ParamDialModel model)
        {
            try
            {
                _driver.PressKeyCode(AndroidKeyCode.Home);  //home键
                _driver.PressKeyCode(AndroidKeyCode.Keycode_CALL);   //拨号键
                                                                     //切到拨号tab
                var contract = _driver.FindElementsById("androidhwext:id/topIcon");
                if (contract != null && contract.Count > 0)
                {
                    contract[0].Click();
                }
                int index = 0;
                string numKeyId = "";
                string dailKeyId = "";
                AppiumWebElement delElement;
                try
                {
                    delElement = _driver.FindElementById("com.huawei.contacts:id/deleteButton");
                    numKeyId = "com.huawei.contacts:id/contacts_dialpad_key_number";
                    dailKeyId = "com.huawei.contacts:id/dialButton";
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("could not be located on the page"))
                    {
                        delElement = _driver.FindElementById("com.android.contacts:id/deleteButton");
                        numKeyId = "com.android.contacts:id/contacts_dialpad_key_number";
                        dailKeyId = "com.android.contacts:id/dialButton";
                    }
                    else
                        throw;
                }
                while (true)
                {
                    delElement?.Click();   //退格键
                    index++;
                    if (index > 15)
                    {
                        break;
                    }
                }
                //拨号
                foreach (var chr in model.diaNumber)
                {
                    ClickDailNum(chr.ToString(), numKeyId);
                }
                //拨打
                try
                {
                    var dailElement = _driver.FindElementById(dailKeyId);//找不到元素直接报异常
                    if (dailElement != null)
                    {
                        dailElement.Click();
                    }
                    #region //报异常就不进入为NULL条件 代码先放在这，以防查不出来不报异常只是为NULL
                    else
                    {
                        if (dailKeyId.Contains("huawei"))
                            dailKeyId = "com.huawei.contacts:id/button_text_switcher";
                        else
                            dailKeyId = "com.android.contacts:id/button_text_switcher";
                        dailElement = _driver.FindElementById(dailKeyId);
                        dailElement.Click();
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    //拨号键元素存在不同情况
                    if (ex.Message.Contains("could not be located on the page"))
                    {
                        if (dailKeyId.Contains("huawei"))
                        {
                            _driver.FindElementById("com.huawei.contacts:id/button_text_switcher")?.Click();
                        }
                        else if (dailKeyId.Contains("android"))
                        {
                            _driver.FindElementById("com.android.contacts:id/button_text_switcher")?.Click();
                        }
                    }
                    else
                        throw;
                }
            }
            catch (Exception ex)
            {
                // logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_Dial, eComponentType.Dial, ex);
            }
        }
        public void ClickDailNum(string num, string numKeyId)
        {
            ExecuteDriverCommand(nameof(ClickDailNum), () => ClickDailNumCore(num, numKeyId));
        }

        private void ClickDailNumCore(string num, string numKeyId)
        {
            _driver.FindElementByAndroidUIAutomator($"new UiSelector().resourceId(\"{numKeyId}\").text(\"{num}\")")?.Click();
        }


        public string GetMobileElementText(ParamMobileElementTextModel textModel)
        {
            AppiumWebElement element = null;
            try
            {
                element = FindElement(textModel.selector, textModel);

                if (element != null)
                {
                    return element.Text;
                }
                return "";
            }
            catch (NotFoundException ex)
            {
                throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetText, ex);
            }
            catch (RPAException rpaEx)
            {
                throw new RPAException(eErrorMessageType.Error_GetTextError, eComponentType.GetMobileElementScreenshot, rpaEx.CustomMessage);
            }
            catch (Exception ex)
            {
                // logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetTextError, eComponentType.GetText, ex);
            }
        }

        public string GetConnectDeviceDetail(ParamDeviceSessionModel textModel)
        {
            string result = string.Empty;
            try
            {
                result = _driver.SessionDetails.ToJson();
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetConnectDetail, eComponentType.GetConnectDetail, ex);

            }
            return result;
        }

        public string GetElementAttributeInfo(ParamMobileElementAttributeModel textModel)
        {
            AppiumWebElement element = null;
            try
            {
                element = FindElement(textModel.selector, textModel);
                if (element != null)
                {
                    return element.GetAttribute(textModel.attribute);
                }
                return "";
            }
            catch (NotFoundException ex)
            {
                throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetElementAttribute, ex);
            }
            catch (RPAException rpaEx)
            {
                throw new RPAException(eErrorMessageType.Error_GetAttribute, eComponentType.GetElementAttribute, rpaEx.CustomMessage);
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetAttribute, eComponentType.GetElementAttribute, ex);
            }
        }

        public void ActivateApp(ParamAppInfoModel appModel)
        {
            ExecuteDriverCommand(nameof(ActivateApp), () => ActivateAppCore(appModel));
        }

        private void ActivateAppCore(ParamAppInfoModel appModel)
        {
            try
            {
                if (appModel.isCloseBeforeOpen)
                {
                    try
                    {
                        _driver.TerminateApp(appModel.packageName);
                    }
                    catch
                    {
                    }
                }
                _driver.ActivateApp(appModel.packageName);
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_ActivateApp, eComponentType.MobileActivateApp, ex);
            }
        }
        public void CloseApp(ParamAppInfoModel appModel)
        {
            ExecuteDriverCommand(nameof(CloseApp), () => CloseAppCore(appModel));
        }

        private void CloseAppCore(ParamAppInfoModel appModel)
        {
            try
            {
                _driver.TerminateApp(appModel.packageName);
            }
            catch (Exception ex)
            {
                //logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_CloseApp, eComponentType.MobileCloseApp, ex);
            }
        }

        #region 手机文件传输
        public async Task FileTransfer(ParamFileTransmissionModel transferFileModel)
        {
            try
            {
                string fileName = "";
                string targetPath = "";
                string result = "";
                // 使用ADB命令保持屏幕常亮
                await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {transferFileModel.sessionId} shell settings put system screen_off_timeout 2147483647");
                // 唤醒屏幕
                await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {transferFileModel.sessionId} shell input keyevent KEYCODE_WAKEUP");
                //检查文件操作权限
                switch (transferFileModel.transmissionType)
                {
                    case TransmissionType.MobileToMac:
                        #region 检查文件操作权限
                        // 检查手机文件是否存在
                        var mobileFileCheck = await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {transferFileModel.sessionId} shell ls -l \"{transferFileModel.mobileFilePath}\"");
                        if (mobileFileCheck.Contains("No such file or directory") || string.IsNullOrWhiteSpace(mobileFileCheck))
                        {
                            _logger.LogDebug($"手机文件不存在: {transferFileModel.mobileFilePath}");
                            throw new RPAException(eErrorMessageType.Error_MobileFilePathNotExists, eComponentType.MobileTransferFile);
                        }
                        // 检查电脑目录是否存在，如果不存在则根据设置创建
                        if (transferFileModel.macDirPath != null && !Directory.Exists(transferFileModel.macDirPath))
                        {
                            if (transferFileModel.isAutoCreatePath)
                            {
                                Directory.CreateDirectory(transferFileModel.macDirPath);
                                _logger.LogDebug($"已创建电脑目录: {transferFileModel.macDirPath}");
                            }
                            else
                            {
                                _logger.LogDebug($"电脑目录不存在: {transferFileModel.macDirPath}");
                                throw new RPAException(eErrorMessageType.Error_MobileFilePathNotExists, eComponentType.MobileTransferFile);
                            }
                        }
                        #endregion
                        #region 进行文件传输的具体逻辑
                        fileName = Path.GetFileName(transferFileModel.mobileFilePath ?? "");
                        targetPath = Path.Combine(transferFileModel.macDirPath ?? "", fileName);
                        if (File.Exists(targetPath) && !transferFileModel.isReplaceSameNameFile)
                        {
                            _logger.LogDebug($"目标文件已存在: {targetPath}");
                            throw new RPAException(eErrorMessageType.Error_MobileFileAlreadyExists, eComponentType.MobileTransferFile);
                        }
                        // 使用ADB pull命令传输文件
                        string pullCommand = $"-s {transferFileModel.sessionId} pull \"{transferFileModel.mobileFilePath}\" \"{transferFileModel.macDirPath}\"";
                        result = await AdbDeviceHelper.ExecuteAdbCommandAsync(pullCommand);

                        if (!string.IsNullOrWhiteSpace(result) && result.Contains("error"))
                        {
                            _logger.LogDebug($"ADB pull失败: {result}");
                            throw new Exception(Localize($"ADB pull失败: {result}", "controlType_MobileSession_A9AEDFD2"));
                        }
                        // 验证文件是否成功传输
                        if (!File.Exists(targetPath))
                        {
                            _logger.LogDebug("文件传输验证失败，目标文件不存在");
                            throw new Exception("文件传输验证失败，目标文件不存在".I18n("controlType_MobileSession_2F2E7B85"));
                        }
                        // 删除源文件（如果设置了删除）
                        if (transferFileModel.isDelSourceFile)
                        {
                            await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {transferFileModel.sessionId} shell rm \"{transferFileModel.mobileFilePath}\"");
                            _logger.LogDebug($"已删除手机源文件: {transferFileModel.mobileFilePath}");
                        }
                        _logger.LogInformation($"文件从手机传输到电脑成功: {transferFileModel.mobileFilePath} -> {targetPath}");
                        #endregion
                        break;

                    case TransmissionType.MacToMobile:
                        #region 检查文件操作权限
                        if (!File.Exists(transferFileModel.macFilePath))
                        {
                            _logger.LogDebug($"电脑文件不存在: {transferFileModel.macFilePath}");
                            throw new RPAException(eErrorMessageType.Error_MobileFilePathNotExists, eComponentType.MobileTransferFile);
                        }
                        // 检查手机目录是否存在，如果不存在则根据设置创建
                        var mobileDirCheck = await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {transferFileModel.sessionId} shell ls -ld \"{transferFileModel.mobileDirPath}\"");
                        if (mobileDirCheck.Contains("No such file or directory") || string.IsNullOrWhiteSpace(mobileDirCheck))
                        {
                            if (transferFileModel.isAutoCreatePath)
                            {
                                await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {transferFileModel.sessionId} shell mkdir -p \"{transferFileModel.mobileDirPath}\"");
                                _logger.LogDebug($"已创建手机目录: {transferFileModel.mobileDirPath}");
                            }
                            else
                            {
                                _logger.LogDebug($"手机目录不存在: {transferFileModel.mobileDirPath}");
                                throw new RPAException(eErrorMessageType.Error_MobileFilePathNotExists, eComponentType.MobileTransferFile);
                            }
                        }
                        #endregion
                        #region 进行文件传输的具体逻辑

                        fileName = Path.GetFileName(transferFileModel.macFilePath);
                        targetPath = $"{transferFileModel.mobileDirPath}{fileName}";
                        // 检查目标文件是否已存在
                        if (transferFileModel.isReplaceSameNameFile)
                        {
                            // 如果允许替换，先删除目标文件
                            await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {transferFileModel.sessionId} shell rm -f \"{targetPath}\"");
                        }
                        else
                        {
                            // 检查目标文件是否存在
                            var checkResult = await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {transferFileModel.sessionId} shell ls -l \"{targetPath}\"");
                            if (!string.IsNullOrWhiteSpace(checkResult) && !checkResult.Contains("No such file"))
                            {
                                _logger.LogDebug($"目标文件已存在: {targetPath}");
                                throw new RPAException(eErrorMessageType.Error_MobileFileAlreadyExists, eComponentType.MobileTransferFile);
                            }
                        }
                        // 使用ADB push命令传输文件
                        string pushCommand = $"-s {transferFileModel.sessionId} push \"{transferFileModel.macFilePath}\" \"{transferFileModel.mobileDirPath}\"";
                        result = await AdbDeviceHelper.ExecuteAdbCommandAsync(pushCommand);

                        if (!string.IsNullOrWhiteSpace(result) && result.Contains("error"))
                        {
                            _logger.LogDebug($"ADB push失败: {result}");
                            throw new Exception(Localize($"ADB push失败: {result}", "controlType_MobileSession_E7BBAB97"));
                        }
                        // 验证文件是否成功传输
                        var verifyResult = await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {transferFileModel.sessionId} shell ls -l \"{targetPath}\"");
                        if (string.IsNullOrWhiteSpace(verifyResult) || verifyResult.Contains("No such file"))
                        {
                            _logger.LogDebug($"文件传输验证失败，目标文件不存在");
                            throw new Exception("文件传输验证失败，目标文件不存在".I18n("controlType_MobileSession_2F2E7B85"));
                        }
                        // 删除源文件（如果设置了删除）
                        if (transferFileModel.isDelSourceFile)
                        {
                            File.Delete(transferFileModel.macFilePath);
                            _logger.LogDebug($"已删除电脑源文件: {transferFileModel.macFilePath}");
                        }
                        _logger.LogInformation($"文件从电脑传输到手机成功: {transferFileModel.macFilePath} -> {targetPath}");
                        #endregion
                        break;
                }
                _logger.LogInformation($"文件传输完成: {transferFileModel.transmissionType}");
            }
            catch (RPAException rpaEx)
            {
                throw;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "文件传输失败");
                throw new RPAException(eErrorMessageType.Error_MobileTransferFile, eComponentType.MobileTransferFile, ex);
            }
        }

        #endregion

        #region 手机解锁
        private int _screenWidth = 1080;
        private int _screenHeight = 1920;
        private bool _isScreenInfoInitialized = false;
        private string _deviceId = "";
        /// <summary>
        /// 执行解锁操作
        /// </summary>
        public async Task<bool> UnlockAsync(ParamUnlockModel unlockModel)
        {
            bool unLockResult = false;
            try
            {
                _deviceId = Udid;
                //唤醒屏幕
                await WakeScreenIfNeededAsync();
                // 检查屏幕是否已解锁
                if (await IsScreenUnlockedAsync())
                {
                    _logger.LogInformation($"{_deviceId}:屏幕已处于解锁状态，无需解锁");
                    unLockResult = true;
                    return unLockResult;
                }
                //需要解锁的话先滑动触发解锁界面
                await PerformInitialSwipeAsync();
                // 根据解锁类型执行相应解锁逻辑
                switch (unlockModel.unlockType)
                {
                    case UnlockType.Pin:
                        unLockResult = await PerformPinUnlockAsync(unlockModel.unlockPw, unlockModel.unlockTimeout);
                        break;

                    case UnlockType.Pattern:
                        unLockResult = await PerformPatternUnlockAsync(unlockModel.unlockPw, unlockModel.unlockTimeout);
                        break;

                    case UnlockType.Alphanumeric:
                        unLockResult = await PerformAlphanumericUnlockAsync(unlockModel.unlockPw, unlockModel.unlockTimeout);
                        break;
                }

                if (unLockResult)
                {
                    _logger.LogInformation($"解锁成功，类型: {unlockModel.unlockType}");
                }
                else
                {
                    _logger.LogError("解锁失败，可能的原因：密码错误、超时或其他异常");
                    unLockResult = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"解锁过程中发生异常: {ex.Message}");
                unLockResult = false;
            }
            return unLockResult;
        }

        /// <summary>
        /// 在发起具体解锁动作前，做一次向上滑动以唤出解锁交互界面（适配需要滑动唤出的锁屏）。
        /// </summary>
        private async Task PerformInitialSwipeAsync()
        {
            try
            {
                await InitializeScreenInfoAsync();

                // 从屏幕下方向上滑动，唤出 PIN/图案/密码 输入界面或解锁页面
                var startX = _screenWidth / 2;
                var startY = (int)(_screenHeight * 0.85);
                var endX = _screenWidth / 2;
                var endY = (int)(_screenHeight * 0.35);
                var duration = 300; // ms

                await AdbDeviceHelper.ExecuteAdbCommandAsync($"shell input swipe {startX} {startY} {endX} {endY} {duration}");
                await Task.Delay(350); // 等待界面响应
                _logger.LogDebug("已执行初始向上滑动以唤出解锁界面");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"执行初始滑动失败: {ex.Message}");
            }
        }


        /// <summary>
        /// 初始化屏幕信息
        /// </summary>
        private async Task InitializeScreenInfoAsync()
        {
            if (_isScreenInfoInitialized) return;
            try
            {
                // 获取屏幕分辨率
                var sizeResult = await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {_deviceId} shell wm size");
                // 输出格式: Physical size: 1080x1920
                if (!string.IsNullOrEmpty(sizeResult))
                {
                    // 使用正则提取宽高，兼容多种 adb 输出格式，避免 Split 重载歧义
                    var match = System.Text.RegularExpressions.Regex.Match(sizeResult, @"Physical size:\s*(\d+)x(\d+)");
                    if (match.Success)
                    {
                        if (int.TryParse(match.Groups[1].Value, out var width) &&
                            int.TryParse(match.Groups[2].Value, out var height))
                        {
                            _screenWidth = width;
                            _screenHeight = height;
                            _logger.LogDebug($"检测到屏幕分辨率: {_screenWidth}x{_screenHeight}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"获取屏幕分辨率失败，使用默认值1080x1920: {ex.Message}");
                // 使用默认值
                _screenWidth = 1080;
                _screenHeight = 1920;
            }

            _isScreenInfoInitialized = true;
        }

        /// <summary>
        /// 获取图案解锁坐标（动态适配不同分辨率）
        /// </summary>
        private (int x, int y)[] GetPatternUnlockCoordinates()
        {
            // 使用基于屏幕中心的自适应网格计算，适配图案控件在屏幕中间或偏上的常见布局
            // gridSide 表示左右两个最外点之间的距离（中心到中心）
            var centerX = _screenWidth / 2;
            var centerY = (int)(_screenHeight * 0.45); // 一般图案区偏上

            // gridSide 取屏宽的 60% 或屏高的 50% 的较小值，避免超出可见区域
            var gridSide = (int)Math.Min(_screenWidth * 0.6, _screenHeight * 0.5);
            if (gridSide <= 0) gridSide = Math.Min(_screenWidth, _screenHeight) / 2;

            // 间距为最外两个点之间距离的半值（因为有两段间隔）
            double step = gridSide / 2.0;

            var coordinates = new (int x, int y)[9];
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    double x = centerX + (col - 1) * step;
                    double y = centerY + (row - 1) * step;
                    coordinates[row * 3 + col] = ((int)Math.Round(x), (int)Math.Round(y));
                }
            }

            _logger.LogDebug($"计算图案9宫格中心: center=({_screenWidth}x{_screenHeight})->({centerX},{centerY}), gridSide={gridSide}");
            return coordinates;
        }


        /// <summary>
        /// 检查屏幕是否需要唤醒
        /// </summary>
        private async Task<bool> WakeScreenIfNeededAsync()
        {
            try
            {
                // 使用ADB命令检查屏幕状态
                var screenState = await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {_deviceId} shell dumpsys power | grep 'Display Power'");

                // 如果屏幕关闭，则唤醒屏幕
                if (!screenState.Contains("ON") || screenState.Contains("OFF"))
                {
                    await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {_deviceId} shell input keyevent KEYCODE_WAKEUP");
                    await Task.Delay(500); // 等待屏幕唤醒
                    _logger.LogDebug("屏幕已唤醒");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"检查/唤醒屏幕失败: {ex.Message}");
                // 尝试强制唤醒
                try
                {
                    await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {_deviceId} shell input keyevent KEYCODE_WAKEUP");
                    await Task.Delay(500);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 检查屏幕是否已解锁（优先检测锁屏遮挡窗口）
        /// </summary>
        private async Task<bool> IsScreenUnlockedAsync()
        {
            try
            {
                _logger.LogDebug($"检查手机屏幕是否处于解锁状态");
                string output = await AdbDeviceHelper.ExecuteAdbCommandWithTimeoutAsync($"-s {_deviceId} shell dumpsys window", ScreenStateAdbTimeoutMilliseconds);
                // 1. 优先检查小米最直接的 keyguard 状态位
                if (output.Contains("isKeyguardShowing=true"))
                {
                    return false;
                }
                // 2. 备选方案：检查 KeyguardServiceDelegate 内部的显示状态
                // 注意：有些版本可能是 showing=true，带空格或换行
                if (output.Contains("showing=true") && output.Contains("KeyguardServiceDelegate"))
                {
                    return false;
                }
                // 3. 备选方案：检查 KeyguardStateMonitor
                if (output.Contains("mIsShowing=true"))
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"检查屏幕解锁状态失败: {ex.Message}");
                // 出错时保守判断为未解锁
                return false;
            }
        }
        /// <summary>
        /// 执行PIN码解锁
        /// </summary>
        private async Task<bool> PerformPinUnlockAsync(string pinCode, int timeoutSeconds)
        {
            try
            {
                _logger.LogDebug("开始PIN码解锁...");

                // 初始化屏幕信息
                await InitializeScreenInfoAsync();

                // 使用按键事件输入 PIN（避免坐标点击带来的错位）
                foreach (char digitChar in pinCode)
                {
                    if (!char.IsDigit(digitChar))
                    {
                        throw new ArgumentException(Localize($"PIN码只能包含数字字符: {digitChar}", "controlType_MobileSession_64BF843F"));
                    }
                    var keycodeName = $"KEYCODE_{digitChar}";
                    await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {_deviceId} shell input keyevent {keycodeName}");
                    await Task.Delay(200); // 每个数字间的延迟
                }

                // 按确认键（部分锁屏需要）
                await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {_deviceId} shell input keyevent KEYCODE_ENTER");
                await Task.Delay(1000);

                // 验证是否解锁成功
                return await WaitForUnlockAsync(timeoutSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"PIN码解锁失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行图案解锁
        /// </summary>
        private async Task<bool> PerformPatternUnlockAsync(string patternCode, int timeoutSeconds)
        {
            try
            {
                _logger.LogDebug("开始图案解锁...");
                int[] patternSequence = patternCode.Select(c => int.Parse(c.ToString()) - 1).ToArray();

                if (patternSequence.Length < 4 || patternSequence.Length > 9)
                {
                    throw new ArgumentException("图案序列长度必须在4-9之间".I18n("controlType_MobileSession_1B0F6C9"));
                }

                // 初始化屏幕信息
                await InitializeScreenInfoAsync();

                // 获取图案解锁坐标
                var patternCoordinates = GetPatternUnlockCoordinates();

                // 验证所有点索引都有效
                foreach (int pointIndex in patternSequence)
                {
                    if (pointIndex < 0 || pointIndex >= patternCoordinates.Length)
                    {
                        throw new ArgumentException(Localize($"无效的图案点索引: {pointIndex}，必须在0-8范围内", "controlType_MobileSession_CB0DD080"));
                    }
                }

                // 方法1：使用连续的tap和swipe命令模拟真实的手势
                // 首先点击第一个点开始
                var firstPoint = patternCoordinates[patternSequence[0]];
                await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {_deviceId} shell input tap {firstPoint.x} {firstPoint.y}");
                await Task.Delay(100);

                // 然后依次滑动到后续的点
                for (int i = 1; i < patternSequence.Length; i++)
                {
                    var currentPoint = patternCoordinates[patternSequence[i - 1]];
                    var nextPoint = patternCoordinates[patternSequence[i]];

                    // 计算滑动时长（根据两点间的距离）
                    var distance = Math.Sqrt(Math.Pow(nextPoint.x - currentPoint.x, 2) +
                                            Math.Pow(nextPoint.y - currentPoint.y, 2));
                    var swipeDuration = Math.Max(100, (int)(distance * 0.8)); // 根据距离计算时长

                    // 执行滑动
                    await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {_deviceId} shell input swipe {currentPoint.x} {currentPoint.y} {nextPoint.x} {nextPoint.y} {swipeDuration}");

                    // 短暂延迟，确保滑动完成
                    await Task.Delay(50);
                }

                await Task.Delay(1000); // 等待图案绘制完成

                // 验证是否解锁成功
                return await WaitForUnlockAsync(timeoutSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"图案解锁失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行字母数字混合解锁
        /// </summary>
        private async Task<bool> PerformAlphanumericUnlockAsync(string password, int timeoutSeconds)
        {
            try
            {
                _logger.LogDebug("开始字母数字混合解锁...");

                // 输入密码
                await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {_deviceId} shell input text \"{password}\"");
                await Task.Delay(500);

                // 按下确认键
                await AdbDeviceHelper.ExecuteAdbCommandAsync($"-s {_deviceId} shell input keyevent KEYCODE_ENTER");
                await Task.Delay(1000); // 等待解锁完成

                // 验证是否解锁成功
                return await WaitForUnlockAsync(timeoutSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"字母数字混合解锁失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 等待解锁完成
        /// </summary>
        private async Task<bool> WaitForUnlockAsync(int timeoutSeconds)
        {
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                if (await IsScreenUnlockedAsync())
                {
                    return true;
                }
                await Task.Delay(500);
            }
            return false;
        }


        #endregion

        #region 手机管理使用


        internal bool TryGetCachedPageSource(out string pageSource)
        {
            lock (_pageSourceSync)
            {
                pageSource = PageSouce;
                return !string.IsNullOrWhiteSpace(pageSource);
            }
        }

        internal void MarkRequiresRecovery(string reason)
        {
            Interlocked.Exchange(ref _requiresRecovery, 1);
            _logger.LogWarning($"手机会话已标记为需要恢复，原因[{reason}]，设备[{GetSafeUdid()}]");
        }

        public string GetPageSource()
        {
            try
            {
                return RefreshPageSource(nameof(GetPageSource));
            }
            catch (Exception ex)
            {
                throw new RPAException(eErrorMessageType.Error_MobileConnect, eComponentType.GetMobileUiTree, ex);
            }
        }
        public byte[] GetScreenShotbyte()
        {
            try
            {
                return ExecuteDriverCommand("GetScreenShotbyte", () => _driver.GetScreenshot().AsByteArray);
            }
            catch (Exception ex)
            {
                throw new RPAException(eErrorMessageType.Error_ElementShot_OperationTomeout_Error, eComponentType.ScreenShot, ex);
            }
        }
        public string GetScreenShotBaseString()
        {
            try
            {
                return ExecuteDriverCommand("GetScreenShotBaseString", () => _driver.GetScreenshot().AsBase64EncodedString);
            }
            catch (Exception ex)
            {
                throw new RPAException(eErrorMessageType.Error_ElementShot_OperationTomeout_Error, eComponentType.ScreenShot, ex);
            }
        }

        private static bool TryBuildStructuralXPath(List<MobileItem> secList, out string xpath)
        {
            xpath = null;

            if (secList == null || secList.Count == 0)
            {
                return false;
            }

            StringBuilder builder = new StringBuilder("/hierarchy");
            foreach (var sec in secList)
            {
                var classNode = sec.props?.FirstOrDefault(a => a.name == "class");
                if (classNode == null || string.IsNullOrEmpty(classNode.value))
                {
                    return false;
                }

                var indexNode = sec.props?.FirstOrDefault(a => a.name == "index");
                if (indexNode == null || string.IsNullOrEmpty(indexNode.value))
                {
                    builder.Append($"/{classNode.value}[@index=0]");
                }
                else
                {
                    builder.Append($"/{classNode.value}[@index={indexNode.value}]");
                }
            }

            xpath = builder.ToString();
            return true;
        }

        private bool TryGetUniqueStableAnchor(string id, string description, out string anchorProperty)
        {
            anchorProperty = null;

            if (!string.IsNullOrEmpty(id) && CountCachedElements(a => a.Id == id) == 1)
            {
                anchorProperty = "id";
                return true;
            }

            if (!string.IsNullOrEmpty(description) && CountCachedElements(a => a.Desc == description) == 1)
            {
                anchorProperty = "description";
                return true;
            }

            return false;
        }

        private bool TryBuildStableAnchorXPath(MobileItem item, bool allowTextAnchor, out string xpath, out string anchorProperty)
        {
            xpath = null;
            anchorProperty = null;

            if (item?.props == null)
            {
                return false;
            }

            string className = item.props.FirstOrDefault(a => a.name == "class")?.value;
            string id = item.props.FirstOrDefault(a => a.name == "id")?.value;
            string description = item.props.FirstOrDefault(a => a.name == "description")?.value;
            string text = item.props.FirstOrDefault(a => a.name == "text")?.value;
            string xpathClassName = string.IsNullOrEmpty(className) ? "*" : className;

            if (!string.IsNullOrEmpty(id) && CountCachedElements(a => a.Id == id) == 1)
            {
                xpath = $"//{xpathClassName}[@resource-id={ToXPathLiteral(id)}]";
                anchorProperty = "id";
                return true;
            }

            if (!string.IsNullOrEmpty(description) && CountCachedElements(a => a.Desc == description) == 1)
            {
                xpath = $"//{xpathClassName}[@content-desc={ToXPathLiteral(description)}]";
                anchorProperty = "description";
                return true;
            }

            if (allowTextAnchor && !string.IsNullOrEmpty(className) && !string.IsNullOrEmpty(text)
                && CountCachedElements(a => a.ClassName == className && a.Text == text) == 1)
            {
                xpath = $"//{xpathClassName}[@text={ToXPathLiteral(text)}]";
                anchorProperty = "text";
                return true;
            }

            return false;
        }

        /// <summary>
        /// 以已经确定的锚点为起点，使用下级节点的结构信息补齐目标路径。
        /// id/description 可以作为目标节点或祖先锚点，text 只允许作为祖先上下文锚点。
        /// index 只用于锚点之后的结构下钻。
        /// </summary>
        private static bool TryBuildAnchorRelativeXPath(List<MobileItem> secList, out string xpath, out int anchorIndex)
        {
            xpath = null;
            anchorIndex = -1;

            if (secList == null || secList.Count == 0)
            {
                return false;
            }

            MobileItem anchorItem = null;
            Props anchorProperty = null;
            for (int i = 0; i < secList.Count; i++)
            {
                var property = secList[i].props?.FirstOrDefault(a =>
                    a.accurate == "true"
                    && (a.name == "id"
                        || a.name == "description"
                        || (a.name == "text" && i < secList.Count - 1)));
                if (property != null)
                {
                    anchorItem = secList[i];
                    anchorProperty = property;
                    anchorIndex = i;
                    break;
                }
            }

            if (anchorItem == null || anchorProperty == null)
            {
                return false;
            }

            string className = anchorItem.props.FirstOrDefault(a => a.name == "class")?.value;
            string xpathClassName = string.IsNullOrEmpty(className) ? "*" : className;
            string anchorType = anchorProperty.name == "id"
                ? "resource-id"
                : anchorProperty.name == "description" ? "content-desc" : "text";
            StringBuilder builder = new StringBuilder(
                $"//{xpathClassName}[@{anchorType}={ToXPathLiteral(anchorProperty.value)}]");

            for (int i = anchorIndex + 1; i < secList.Count; i++)
            {
                var item = secList[i];
                string itemClassName = item.props.FirstOrDefault(a => a.name == "class")?.value;
                if (string.IsNullOrEmpty(itemClassName))
                {
                    return false;
                }

                var indexProperty = item.props.FirstOrDefault(a => a.name == "index");
                string index = string.IsNullOrEmpty(indexProperty?.value) ? "0" : indexProperty.value;
                builder.Append($"/{itemClassName}[@index={index}]");
            }

            xpath = builder.ToString();
            return true;
        }

        public AppiumWebElement CheckElement(string selector, out string buildSelecter)
        {
            // 拾取阶段传入的是尚未包装 CommonProps/ElementDetail 的当前节点数组，
            // 它不是历史 AndroidJsonItem selector。将其包装为当前 RPACommon 后，
            // 与流程执行统一复用 FindElementCore 的定位逻辑。
            var captureSelector = new RPACommon
            {
                Selector = JArray.Parse(selector)
            };
            var element = FindElementCore(captureSelector);
            if (element == null)
            {
                buildSelecter = "";
                return null;
            }

            buildSelecter = BuildVerifiedSelector(selector);

            return element;
        }

        private string BuildVerifiedSelector(string selector, bool allowTargetAnchor = true)
        {
            try
            {
                // 唯一性判断依赖 Provider 侧的 CacheList。这里只解析最近一次已经获取的
                // PageSource 快照，不再向 Appium 发起 PageSource 请求，避免拾取时强刷新。
                ParseXml();
                List<MobileItem> secList = JsonHelper.ToObject<List<MobileItem>>(selector);
                MobileItem lastItem = secList.LastOrDefault();
                if (lastItem?.props == null)
                {
                    return "";
                }

                var lastProps = lastItem.props;
                string Id = lastProps.FirstOrDefault(a => a.name == "id")?.value;
                string desc = lastProps.FirstOrDefault(a => a.name == "description")?.value;

                if (allowTargetAnchor)
                {
                    if (!string.IsNullOrEmpty(Id) && CountCachedElements(a => a.Id == Id) == 1)
                    {
                        lastProps.First(a => a.name == "id").accurate = "true";
                        return SerializeSelectorWithSynchronizedNodeAccurate(secList);
                    }

                    if (!string.IsNullOrEmpty(desc) && CountCachedElements(a => a.Desc == desc) == 1)
                    {
                        lastProps.First(a => a.name == "description").accurate = "true";
                        return SerializeSelectorWithSynchronizedNodeAccurate(secList);
                    }

                    if (TryGetUniqueStableAnchor(Id, desc, out string anchorProperty))
                    {
                        lastProps.ForEach(a =>
                        {
                            if (a.name == anchorProperty)
                                a.accurate = "true";
                        });
                        return SerializeSelectorWithSynchronizedNodeAccurate(secList);
                    }
                }

                secList.Reverse();
                var needList = secList.Where(a => a.props.Any(b =>
                    (b.name == "id" || b.name == "description" || b.name == "text")
                    && !string.IsNullOrEmpty(b.value)));
                MobileItem startItem = null;
                string startAnchorProperty = null;
                foreach (var idli in needList)
                {
                    bool isTargetItem = ReferenceEquals(idli, lastItem);
                    if (!allowTargetAnchor && isTargetItem)
                    {
                        continue;
                    }
                    bool allowTextAnchor = !isTargetItem;
                    if (TryBuildStableAnchorXPath(idli, allowTextAnchor, out _, out string candidateAnchorProperty))
                    {
                        startItem = idli;
                        startAnchorProperty = candidateAnchorProperty;
                        break;
                    }
                }
                secList.Reverse();

                if (startItem != null)
                {
                    bool isStart = false;
                    foreach (var sec in secList)
                    {
                        if (!isStart)
                        {
                            if (sec.Equals(startItem))
                            {
                                sec.props.ForEach(a =>
                                {
                                    if (a.name == startAnchorProperty)
                                        a.accurate = "true";
                                });
                                isStart = true;
                            }
                        }
                        else
                        {
                            var indexItem = sec.props.FirstOrDefault(a => a.name == "index");
                            if (indexItem != null)
                                indexItem.accurate = "true";
                        }
                    }
                }
                else
                {
                    foreach (var sec in secList)
                    {
                        var indexItem = sec.props.FirstOrDefault(a => a.name == "index");
                        if (indexItem != null)
                            indexItem.accurate = "true";
                    }
                }

                return SerializeSelectorWithSynchronizedNodeAccurate(secList);
            }
            catch
            {
                return "";
            }
        }
        private static string SerializeSelectorWithSynchronizedNodeAccurate(List<MobileItem> selectorItems)
        {
            SynchronizeNodeAccurate(selectorItems);
            return JsonHelper.ToJson(selectorItems);
        }

        private static void SynchronizeNodeAccurate(IEnumerable<MobileItem> selectorItems)
        {
            if (selectorItems == null)
            {
                return;
            }

            foreach (MobileItem item in selectorItems)
            {
                item.accurate = item.props?.Any(prop =>
                    string.Equals(prop.accurate, "true", StringComparison.OrdinalIgnoreCase)) == true
                    ? "true"
                    : "false";
            }
        }

        public AppiumWebElement CheckSimilarElement(string selector, string firstSelector, out string buildSelecter)
        {
            string capturedBuildSelecter = "";
            var element = ExecuteDriverCommand(nameof(CheckSimilarElement), () => CheckSimilarElementCore(selector, firstSelector, out capturedBuildSelecter));
            buildSelecter = capturedBuildSelecter;
            return element;
        }

        private AppiumWebElement CheckSimilarElementCore(string selector, string firstSelector, out string buildSelecter)
        {
            try
            {
                List<MobileItem> selectorItems = JsonHelper.ToObject<List<MobileItem>>(selector);
                EnsureSameSimilarElementStructure(selectorItems, firstSelector);

                // 相似元素需要保留目标节点的可变空间，因此不允许以目标自身作为唯一锚点；
                // 祖先稳定锚点和后续结构路径仍与普通元素使用同一套构建规则。
                buildSelecter = BuildVerifiedSelector(selector, allowTargetAnchor: false);
                if (string.IsNullOrWhiteSpace(buildSelecter))
                {
                    return null;
                }

                var captureSelector = new RPACommon
                {
                    Selector = JArray.Parse(buildSelecter)
                };
                return FindElementCore(captureSelector);
            }
            catch (RPAException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "校验相似元素异常");
                throw;
            }
        }

        private static void EnsureSameSimilarElementStructure(List<MobileItem> selectorItems, string firstSelector)
        {
            if (string.IsNullOrEmpty(firstSelector))
            {
                return;
            }

            RPACommon firstSelectorAll = JsonHelper.ToObject<RPACommon>(firstSelector);
            List<MobileItem> firstSelectorItems = JsonHelper.ToObject<List<MobileItem>>(firstSelectorAll.Selector.ToString());
            bool isSameStructure = selectorItems != null
                && firstSelectorItems != null
                && selectorItems.Count == firstSelectorItems.Count
                && selectorItems.Select(item => item.name).SequenceEqual(firstSelectorItems.Select(item => item.name));
            if (!isSameStructure)
            {
                throw new RPAException(
                    eErrorMessageType.Error_GetSimilarElementInfo,
                    eComponentType.MobileCaptureSimilarElement,
                    "不属于同一种类型元素".I18n("controlType_MobileSession_3FF7B92C"));
            }
        }
        public IEnumerable<AppiumWebElement> FindSimilarElement(string selector)
        {
            return ExecuteDriverCommand(nameof(FindSimilarElement), () => FindSimilarElementCore(selector));
        }

        private IEnumerable<AppiumWebElement> FindSimilarElementCore(string selector)
        {
            try
            {
                List<MobileSelectorNode> rules = MobileSelectorRules.Parse(selector);
                if (MobileSelectorRules.UsesCurrentRules(selector))
                    return _driver.FindElementsByXPath(MobileSelectorRules.BuildXPath(rules));

                RPACommon selectorAll = JsonHelper.ToObject<RPACommon>(selector);
                List<MobileItem> selectorItems = JsonHelper.ToObject<List<MobileItem>>(selectorAll.Selector.ToString());
                var accurateItems = selectorItems
                    .Where(item => item.props.Any(prop => prop.accurate == "true"))
                    .ToList();

                if (accurateItems.Count == 1)
                {
                    foreach (MobileItem item in selectorItems)
                    {
                        foreach (Props property in item.props.Where(prop => prop.name == "class"))
                        {
                            property.accurate = "true";
                        }
                    }
                    accurateItems = selectorItems
                        .Where(item => item.props.Any(prop => prop.accurate == "true"))
                        .ToList();
                }

                string xpath = BuildSelectorXPath(
                    selectorItems,
                    accurateItems,
                    useIndexZeroWhenMissing: false);
                return !string.IsNullOrEmpty(xpath)
                    ? _driver.FindElementsByXPath(xpath)
                    : null;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "查找相似元素异常");
                throw;
            }
        }
        public AppiumWebElement FindElement(string selector, MobileElementValidationOptions validationOptions = null)
        {
            RPACommon selectorAll = JsonHelper.ToObject<RPACommon>(selector);
            if (selectorAll?.CommonProps == null || selectorAll.Selector == null)
            {
                throw new ArgumentException("不支持的手机元素结构，请重新拾取元素", nameof(selector));
            }

            MobileElementValidationHelper.ValidatePage(
                _driver,
                selectorAll.ElementDetail,
                validationOptions,
                _logger);

            AppiumWebElement element = FindElementCore(selectorAll);
            if (element != null)
            {
                MobileElementValidationHelper.ValidateElementImage(
                    _driver,
                    element,
                    selectorAll.CommonProps,
                    validationOptions,
                    _logger);
            }
            return element;
        }
        private AppiumWebElement FindElementCore(RPACommon selectorAll)
        {
            try
            {
                IEnumerable<AppiumWebElement> appiumElements;
                CommonProps commonProps = selectorAll.CommonProps;
                string queryMode = commonProps.QueryMode;

                if (string.Equals(commonProps.GUIType, "mobile", StringComparison.OrdinalIgnoreCase))
                {
                    if (queryMode == "xpath")
                    {
                        return _driver.FindElementByXPath(commonProps.CssSelector);
                    }
                    if (queryMode == "id")
                    {
                        var selectorInfo = GetSelectorValueByExtractPrefix(commonProps.CssSelector);
                        _logger.LogInformation($"取出id:{selectorInfo}");
                        if (selectorInfo != null)
                        {
                            var elements = _driver.FindElementsById(selectorInfo.Value.SelectorValue);
                            return elements != null && elements.Count > selectorInfo.Value.Index
                                ? elements.ElementAt(selectorInfo.Value.Index)
                                : null;
                        }
                        return _driver.FindElementById(commonProps.CssSelector);
                    }
                    if (queryMode == "description")
                    {
                        var selectorInfo = GetSelectorValueByExtractPrefix(commonProps.CssSelector);
                        _logger.LogInformation($"取出description:{selectorInfo}");
                        if (selectorInfo != null)
                        {
                            var elements = _driver.FindElementsByAccessibilityId(selectorInfo.Value.SelectorValue);
                            return elements != null && elements.Count > selectorInfo.Value.Index
                                ? elements.ElementAt(selectorInfo.Value.Index)
                                : null;
                        }
                        return _driver.FindElementByAccessibilityId(commonProps.CssSelector);
                    }
                    if (queryMode == "name")
                    {
                        var selectorInfo = GetSelectorValueByExtractPrefix(commonProps.CssSelector);
                        _logger.LogInformation($"name:{selectorInfo}");
                        if (selectorInfo != null)
                        {
                            var elements = _driver.FindElementsByName(selectorInfo.Value.SelectorValue);
                            return elements != null && elements.Count > selectorInfo.Value.Index
                                ? elements.ElementAt(selectorInfo.Value.Index)
                                : null;
                        }
                        return _driver.FindElementByName(commonProps.CssSelector);
                    }
                }

                List<MobileSelectorNode> rules = MobileSelectorRules.Parse(selectorAll.Selector.ToString());
                if (MobileSelectorRules.UsesCurrentRules(selectorAll.CommonProps))
                    return _driver.FindElementByXPath(MobileSelectorRules.BuildXPath(rules));

                List<MobileItem> selectorItems = JsonHelper.ToObject<List<MobileItem>>(selectorAll.Selector.ToString());
                var accurateItems = selectorItems
                    .Where(item => item.props.Any(prop => prop.accurate == "true"))
                    .ToList();

                bool hasAccurateAnchor = TryBuildAnchorRelativeXPath(
                    selectorItems,
                    out string anchorXPath,
                    out int anchorIndex);
                if (hasAccurateAnchor && anchorIndex < selectorItems.Count - 1)
                {
                    return _driver.FindElementByXPath(anchorXPath);
                }
                if (!hasAccurateAnchor && TryBuildStructuralXPath(selectorItems, out string structuralXPath))
                {
                    return _driver.FindElementByXPath(structuralXPath);
                }

                if (accurateItems.Count == 1)
                {
                    MobileItem node = accurateItems[0];
                    var accurateProps = node.props
                        .Where(prop => prop.accurate == "true" && prop.name != "class" && prop.name != "name")
                        .ToList();
                    string className = node.props.First(prop => prop.name == "class").value;

                    if (accurateProps.Count == 1)
                    {
                        Props property = accurateProps[0];
                        string propertyName = property.name;
                        string pattern = property.pattern;

                        if (propertyName == "id")
                        {
                            try
                            {
                                appiumElements = _driver.FindElementsById(property.value);
                                if (appiumElements.Count() == 1)
                                {
                                    return appiumElements.FirstOrDefault();
                                }
                            }
                            catch
                            {
                            }
                            return _driver.FindElementByAndroidUIAutomator(
                                $"new UiSelector().resourceId(\"{EscapeUiAutomatorLiteral(property.value)}\").className(\"{EscapeUiAutomatorLiteral(className)}\")");
                        }
                        if (propertyName == "description")
                        {
                            switch (pattern.ToLower())
                            {
                                case "regular":
                                case "contain":
                                case "begin":
                                case "end":
                                    string uiAutomator = "new UiSelector()" +
                                        BuildUiAutomatorTextSelectorFragment("description", pattern, property.value);
                                    return _driver.FindElementByAndroidUIAutomator(uiAutomator);
                                default:
                                    return _driver.FindElementByAccessibilityId(property.value);
                            }
                        }
                        if (propertyName == "text")
                        {
                            string uiAutomator = "new UiSelector()" +
                                BuildUiAutomatorTextSelectorFragment("text", pattern, property.value);
                            return _driver.FindElementByAndroidUIAutomator(uiAutomator);
                        }
                    }
                    else
                    {
                        string uiSelector = BuildUiAutomatorSelector(className, accurateProps);
                        return _driver.FindElementByAndroidUIAutomator(uiSelector);
                    }
                }

                if (accurateItems.Count > 1)
                {
                    string xpath = BuildSelectorXPath(
                        selectorItems,
                        accurateItems,
                        useIndexZeroWhenMissing: true);
                    if (!string.IsNullOrEmpty(xpath))
                    {
                        return _driver.FindElementByXPath(xpath);
                    }
                }
                return null;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "查找元素异常");
                throw new NotFoundException(ex.Message, ex);
            }
        }

        #endregion

        public void Close()
        {
            CloseCore(releaseFromCache: true);
        }

        internal void CloseDetached()
        {
            CloseCore(releaseFromCache: false);
        }

        internal void CloseDetachedForRecovery()
        {
            CloseCore(releaseFromCache: false, requireConfirmedClose: true);
        }

        private void CloseCore(bool releaseFromCache, bool requireConfirmedClose = false)
        {
            string udid = GetSafeUdid();
            if (releaseFromCache)
            {
                MobileSessionCache.Release(udid);
            }
            try
            {
                ExecuteDriverCommand("Quit", () => _driver.Quit(), allowDuringRecovery: true);
            }
            catch (WebDriverException ex) when (requireConfirmedClose
                && (ex.Message.IndexOf("invalid session", StringComparison.OrdinalIgnoreCase) >= 0
                    || ex.Message.IndexOf("session deleted", StringComparison.OrdinalIgnoreCase) >= 0
                    || ex.Message.IndexOf("a session is either terminated or not started", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                // 服务端已确认旧Session不存在，无需再次删除。
                _logger.LogDebug($"旧会话已不存在，完成只读故障清理，设备[{udid}]");
            }
            catch (Exception ex) when (!requireConfirmedClose)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"关闭手机会话失败，设备[{udid}]");
            }
        }
        /// <summary>
        /// 仅在连接复用缓存会话时，读取屏幕方向以验证 Appium 命令链路。
        /// </summary>
        public void VerifyReusableSession()
        {
            ExecuteDriverCommand(nameof(VerifyReusableSession), () => { var orientation = _driver.Orientation; });
        }

        /// <summary>
        /// 仅根据本地会话状态判断是否可继续复用，不主动向Appium发送探测命令。
        /// Driver命令异常或超时会通过现有隔离标记触发后续恢复。
        /// </summary>
        public bool IsSessionReusable()
        {
            return !HasTimedOutOperation && !RequiresRecovery;
        }

        public string LoadNewScreenshot()
        {
            return ExecuteDriverCommand("LoadNewScreenshot", () => _driver.GetScreenshot().AsBase64EncodedString);
        }
        private List<MobileElement> GetCachedElementsSnapshot(Func<MobileElement, bool> predicate)
        {
            lock (_cacheListLock)
            {
                return CacheList.Where(predicate).ToList();
            }
        }

        private int CountCachedElements(Func<MobileElement, bool> predicate)
        {
            lock (_cacheListLock)
            {
                return CacheList.Count(predicate);
            }
        }

        private MobileElement FindCachedElement(Func<MobileElement, bool> predicate)
        {
            lock (_cacheListLock)
            {
                return CacheList.FirstOrDefault(predicate);
            }
        }

        /// <summary>
        /// 对 UiAutomator Java 字符串字面量中的普通值做转义（反斜杠与双引号）。
        /// </summary>
        private static string EscapeUiAutomatorLiteral(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>
        /// 对 UiAutomator 正则值做转义（仅双引号，保留正则的反斜杠语义）。
        /// </summary>
        private static string EscapeUiAutomatorRegex(string value)
        {
            return (value ?? string.Empty).Replace("\"", "\\\"");
        }

        /// <summary>
        /// 构建 UiAutomator 文本类属性片段（text/description），返回带前导点的链式片段。
        /// pattern: regular=Matches(正则) contain=Contains begin=StartsWith end=Matches(".*x$") 其他=精确等于。
        /// </summary>
        private static string BuildUiAutomatorTextSelectorFragment(string propType, string pattern, string value)
        {
            switch (pattern.ToLower())
            {
                case "regular":
                    return $".{propType}Matches(\"{EscapeUiAutomatorRegex(value)}\")";
                case "contain":
                    return $".{propType}Contains(\"{EscapeUiAutomatorLiteral(value)}\")";
                case "begin":
                    return $".{propType}StartsWith(\"{EscapeUiAutomatorLiteral(value)}\")";
                case "end":
                    return $".{propType}Matches(\".*{EscapeUiAutomatorRegex(value)}$\")";
                default:
                    return $".{propType}(\"{EscapeUiAutomatorLiteral(value)}\")";
            }
        }

        /// <summary>
        /// 构建多属性 UiAutomator 选择器（className 前缀 + 若干已启用属性），统一转义。
        /// </summary>
        private static string BuildUiAutomatorSelector(string className, IEnumerable<Props> props)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"new UiSelector().className(\"{EscapeUiAutomatorLiteral(className)}\")");
            foreach (var pros in props)
            {
                var prosName = pros.name;
                var prosValue = pros.value;
                var pattern = pros.pattern;
                if (prosName == "class" || prosName == "name")
                {
                    continue;
                }
                if (prosName == "id")
                {
                    sb.Append($".resourceId(\"{EscapeUiAutomatorLiteral(prosValue)}\")");
                }
                else if (prosName == "description" || prosName == "text")
                {
                    sb.Append(BuildUiAutomatorTextSelectorFragment(prosName, pattern, prosValue));
                }
                else if (prosName == "index")
                {
                    sb.Append($".index({prosValue})");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 将值转义为 XPath 1.0 字符串字面量：无单引号用单引号，含单引号用双引号，两者都含则用 concat 拼接。
        /// 保证值含引号时不破坏 XPath 表达式。
        /// </summary>
        private static string ToXPathLiteral(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "''";
            }
            if (!value.Contains("'"))
            {
                return $"'{value}'";
            }
            if (!value.Contains("\""))
            {
                return $"\"{value}\"";
            }
            var parts = value.Split('\'');
            StringBuilder sb = new StringBuilder("concat(");
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", \"'\", ");
                }
                sb.Append($"'{parts[i]}'");
            }
            sb.Append(")");
            return sb.ToString();
        }

        /// <summary>
        /// 构建 XPath 谓词内层条件（不含 [、] 或 and 前缀）。
        /// type=index 时输出不带引号的数字比较；其余按 pattern 选择 matches/contains/starts-with/ends-with/精确等于。
        /// </summary>
        private static string BuildXPathPredicate(string type, string pattern, string value)
        {
            if (type == "index")
            {
                return $"@{type}={value}";
            }
            switch (pattern.ToLower())
            {
                case "regular":
                    return $"matches(@{type}, {ToXPathLiteral(value)})";
                case "contain":
                    return $"contains(@{type}, {ToXPathLiteral(value)})";
                case "begin":
                    return $"starts-with(@{type}, {ToXPathLiteral(value)})";
                case "end":
                    return $"ends-with(@{type}, {ToXPathLiteral(value)})";
                default:
                    return $"@{type}={ToXPathLiteral(value)}";
            }
        }

        /// <summary>
        /// 将 props.name 映射为 XPath 属性名（id→resource-id、description→content-desc，其余同名），未知属性返回空串。
        /// </summary>
        private static string MapXPathAttributeName(string propName)
        {
            if (propName == "id")
            {
                return "resource-id";
            }
            if (propName == "class")
            {
                return "class";
            }
            if (propName == "text")
            {
                return "text";
            }
            if (propName == "description")
            {
                return "content-desc";
            }
            if (propName == "index")
            {
                return "index";
            }
            return "";
        }

        /// <summary>
        /// 构建单个节点的 class+index 结构路径片段（如 /android.widget.Button[@index=2]）。
        /// useIndexZeroWhenMissing=true：index 缺失时输出 [@index=0]（FindElement/CheckSimilarElement 全路径用）；
        /// false：index 缺失时省略 index 条件（FindSimilarElement 用）。
        /// onIndexMark 命中 index 节点时回调（CheckSimilarElement 用它标记 accurate 以便生成 buildSelecter）。
        /// </summary>
        private static string BuildStructuralSegment(MobileItem sec, bool useIndexZeroWhenMissing, Action<Props> onIndexMark = null)
        {
            string className = sec.props.First(a => a.name == "class").value;
            var indexItem = sec.props.FirstOrDefault(a => a.name == "index");
            if (indexItem == null)
            {
                return useIndexZeroWhenMissing ? $"/{className}[@index=0]" : $"/{className}";
            }
            onIndexMark?.Invoke(indexItem);
            return $"/{className}[@index={indexItem.value}]";
        }

        private static string BuildSelectorXPath(
            List<MobileItem> selectorItems,
            List<MobileItem> accurateItems,
            bool useIndexZeroWhenMissing)
        {
            if (selectorItems == null || accurateItems == null || accurateItems.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder xpath = new StringBuilder();
            if (selectorItems.Count == accurateItems.Count)
            {
                xpath.Append("/hierarchy");
                foreach (MobileItem item in selectorItems)
                {
                    xpath.Append(BuildStructuralSegment(item, useIndexZeroWhenMissing));
                }
            }
            else
            {
                xpath.Append("/" + BuildPredicatePath(accurateItems));
            }
            return xpath.ToString();
        }

        /// <summary>
        /// 构建多节点精确属性匹配的部分路径：/className[第一个谓词] and 后续谓词...
        /// 从 needList 中取 accurate==true 的属性，name/class 忽略，index 走无引号数字比较。
        /// </summary>
        private static string BuildPredicatePath(IEnumerable<MobileItem> needList)
        {
            StringBuilder xpath = new StringBuilder();
            foreach (var sec in needList)
            {
                string className = sec.name;
                //过滤出accurate属性为true的属性
                var items = sec.props.Where(a => a.accurate == "true");
                StringBuilder pathStr = new StringBuilder();
                foreach (var item in items)
                {
                    if (item.name == "name" || item.name == "class")
                    {
                        continue;
                    }
                    string type = MapXPathAttributeName(item.name);
                    if (pathStr.Length > 0)
                    {
                        pathStr.Append($" and {BuildXPathPredicate(type, item.pattern, item.value)}");
                    }
                    else
                    {
                        pathStr.Append($"[{BuildXPathPredicate(type, item.pattern, item.value)}");
                    }
                }
                if (pathStr.Length > 0)
                {
                    xpath.Append($"/{className}{pathStr.ToString()}]");
                }
                else
                {
                    xpath.Append($"/{className}");
                }
            }
            return xpath.ToString();
        }

        public void ParseXml()
        {
            lock (_parseXmlLock)
            {
                string pageSource;
                long pageSourceVersion;
                lock (_pageSourceSync)
                {
                    pageSource = PageSouce;
                    pageSourceVersion = _pageSourceVersion;
                }
                if (string.IsNullOrWhiteSpace(pageSource))
                {
                    lock (_cacheListLock)
                    {
                        CacheList = new List<MobileElement>();
                        _parsedPageSourceVersion = -1;
                    }
                    return;
                }

                lock (_cacheListLock)
                {
                    if (_parsedPageSourceVersion == pageSourceVersion && CacheList.Count > 0)
                    {
                        return;
                    }
                }

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(pageSource);
                List<MobileElement> parsedElements = new List<MobileElement>();
                XmlElement root = doc.DocumentElement;
                if (root != null && string.Equals(root.Name, "hierarchy", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (XmlNode childNode in root.ChildNodes)
                    {
                        XmlElement childElement = childNode as XmlElement;
                        if (childElement != null) RecursionChildren(childElement, 0, null, parsedElements);
                    }
                }
                else if (root != null)
                {
                    RecursionChildren(root, 0, null, parsedElements);
                }

                lock (_pageSourceSync)
                {
                    if (_pageSourceVersion != pageSourceVersion)
                    {
                        return;
                    }
                    lock (_cacheListLock)
                    {
                        CacheList = parsedElements;
                        _parsedPageSourceVersion = pageSourceVersion;
                    }
                }
            }
        }
        private void RecursionChildren(XmlElement element, int depth, MobileElement parent, List<MobileElement> parsedElements)
        {
            if (element == null || string.Equals(element.Name, "hierarchy", StringComparison.OrdinalIgnoreCase)) return;
            MobileElement mobileElement = new MobileElement(element, depth, parent);
            if (parent != null) parent.AddChild(mobileElement);
            parsedElements.Add(mobileElement);

            foreach (XmlNode childNode in element.ChildNodes)
            {
                XmlElement childElement = childNode as XmlElement;
                if (childElement != null) RecursionChildren(childElement, depth + 1, mobileElement, parsedElements);
            }
        }
        private bool IsAnyCurrent(List<MobileItem> items)
        {
            return TryResolveCachedElement(items, items?.Count ?? 0, out _);
        }

        private bool TryResolveCachedElement(
            IReadOnlyList<MobileItem> items,
            int itemCount,
            out MobileElement currentElement)
        {
            currentElement = null;
            if (items == null || itemCount <= 0 || itemCount > items.Count)
            {
                return false;
            }

            for (int pathLevel = 0; pathLevel < itemCount; pathLevel++)
            {
                MobileItem item = items[pathLevel];
                string className = GetMobilePropertyValue(item, "class");
                string index = GetMobilePropertyValue(item, "index");
                if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(index))
                {
                    currentElement = null;
                    return false;
                }

                currentElement = pathLevel == 0
                    ? FindCachedElement(element =>
                        element.Depth == 0
                        && element.ClassName == className
                        && element.NthChildIndex == index)
                    : currentElement?.Children.FirstOrDefault(element =>
                        element.Depth == pathLevel
                        && element.ClassName == className
                        && element.NthChildIndex == index);
                if (currentElement == null)
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 获取非元素结构下的定位方式值
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static (string SelectorValue, int Index)? GetSelectorValueByExtractPrefix(string input)
        {
            string pattern = @"^([^[]+)\[(\d+)\]$";
            Match match = Regex.Match(input, pattern);
            if (match.Success)
            {
                string selectorValue = match.Groups[1].Value;
                int index = int.Parse(match.Groups[2].Value);
                return (selectorValue, index - 1);
            }
            return null;
        }


        public List<List<List<int>>> GetImageRects(ParamElementShotClickModel paramModel)
        {
            string directoryPath = Path.GetTempPath();
            string fileName = $"II.RPA.MobileProvider-{Guid.NewGuid():N}.png";
            string tempSourcePath = Path.Combine(directoryPath, fileName);
            try
            {
                return GetImageRectsCore(paramModel, directoryPath, fileName, tempSourcePath);
            }
            finally
            {
                //算法需要具体路径，所以要临时生成；执行完删除，防止临时文件残留
                try
                {
                    if (File.Exists(tempSourcePath))
                    {
                        File.Delete(tempSourcePath);
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"清理图像识别临时文件失败: {tempSourcePath}");
                }
            }
        }

        private List<List<List<int>>> GetImageRectsCore(ParamElementShotClickModel paramModel, string directoryPath, string fileName, string tempSourcePath)
        {
            int deviceWidth = 0;
            int deviceHeight = 0;
            //记得将生成的手机屏幕图片执行完删除（算法需要具体路径，所以要临时生成）
            var shotImg = this.GetScreenshot(new ParamScreenshotModel
            {
                fileName = fileName,
                savePath = directoryPath
            });
            if (paramModel.scopeRect == null)
            {
                using (Bitmap image = new Bitmap(tempSourcePath))
                {
                    deviceWidth = image.Width;
                    deviceHeight = image.Height;
                }
            }
            CVImageHelper cVImageHelper = new CVImageHelper();
            var param = new ImageMatchingParams
            {
                Source = tempSourcePath,
                Template = paramModel.imgSrc,
                Thr = paramModel.thr,
                DomainArea = paramModel.scopeRect == null ?
              new List<int> {
                            0,
                            0,
                            deviceWidth,
                            deviceHeight,
              } :
              new List<int> {
                            paramModel.scopeRect.startX,
                            paramModel.scopeRect.startY,
                            paramModel.scopeRect.endX- paramModel.scopeRect.startX,
                            paramModel.scopeRect.endY- paramModel.scopeRect.startY,
              },
                OriginalPix = paramModel.deviceResolution
            };
            string imageRectResult = cVImageHelper.InspectImageByCV(param);
            //区域坐标格式 [[[x1,y1],[x2,y2]],[[x3,y3],[x4,y4]]]
            var rectsData = JsonConvert.DeserializeObject<List<List<List<int>>>>(imageRectResult);
            return rectsData;
        }

    }
}

