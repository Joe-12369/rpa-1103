using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.MobileDeviceManager.Common;
using II.RPA.MobileDeviceManager.CV;
using II.RPA.MobileDeviceManager.GrpcService.Service;
using II.RPA.MobileDeviceManager.Log;
using II.RPA.MobileDeviceManager.Models;
using II.RPA.MobileDeviceManager.Services;
using II.RPA.MobileDeviceManager.ViewModels;
using II.RPA.MobileDeviceManager.Views;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Remote;
using Rpa.Core.Mobileplugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Xml;
using Unity;
using Unity.Injection;

namespace II.RPA.MobileDeviceManager
{
    internal class MobileDeviceManager
    {
        private const int CaptureAdbScreenshotTimeoutMilliseconds = 3000;
        private const int ProviderManagementTimeoutMilliseconds = 30000;
        private const int ProviderConnectTimeoutMilliseconds = 60000;
        private const int GrpcDeadlineGraceMilliseconds = 5000;
        private static volatile MobileDeviceManager _instance;
        private static object _singletonLock = new object();
        public List<DeviceInfo> DeviceInfos;//当前环境下的设备列表信息
        public bool IsInstallPlugin;//是否安装手机插件
        public bool IsExistReadyMobile;//是否存在已插入电脑的设备
        //public AndroidDriver<AppiumWebElement> Driver;
        public string ServerPort;
        public string AppiumServerPort = "4723";
        public IReadOnlyList<MobileElement> CacheList => elementRepository.Elements;
        public bool IsPageSourceAdbDegraded => elementRepository.IsAdbDegraded;
        public MobileElement CurrentElement { get; private set; }
        public string BuildSelectorJson { get; private set; }
        private ILogger logger;
        public bool IsConnected;//当前是否存在连接得操作手机
        private string currentDeviceId;
        public bool IsResetNewDriver;
        public Action ParseElement;   //解析元素，两种方式CV和XML   
        public DeviceScreenView mobileForm;
        private readonly Subject<GlobalExceptionSubModel> _exceptionSubject;
        public MobileRotation MobileRotation = MobileRotation.Vertical;
        public MobilePluginService.MobilePluginServiceClient mobileServerClint;
        public double DpiX = 1.0;
        public double DpiY = 1.0;
        public bool IsConnecting;
        public Exception CvException;
        public Rect mobileDeviceRect;
        public object mobileDeviceSoultion;
        public Semaphore Semaphore;
        public MobileException CancelException;
        public Bitmap CurrentCaptureElementShot;
        public int CurrentPositionX;
        public int CurrentPositionY;
        public Exception CaptureException;
        private int captureModeCount;
        private int cvParseState;
        private int cvParseRequestVersion;
        private int cvCaptureGeneration;
        private bool isCvCaptureValue;
        private readonly MobileElementRepository elementRepository;
        private PageSourceRefreshService pageSourceRefreshService;
        private readonly object _pageSourceConnectionLock = new object();
        private long _pageSourceConnectionVersion;
        private readonly object _pageSourceDisplayLock = new object();
        private string _pageSourceDisplayDeviceId;
        private Rect _pageSourceDisplaySize;
        private DateTime _pageSourceDisplayUpdatedUtc;
        private readonly object _screenshotDisplayLock = new object();
        private long _screenshotConnectionVersion;
        private Rect _screenshotDisplaySize = Rect.Empty;
        private DateTime _screenshotDisplayUpdatedUtc;
        private int _activeScreenshots;

        public bool IsScreenshotInProgress => Volatile.Read(ref _activeScreenshots) > 0;

        public bool TryGetRecentScreenshotDisplaySize(string udid, out Rect size)
        {
            lock (_screenshotDisplayLock)
            {
                size = _screenshotDisplaySize;
                return string.Equals(currentDeviceId, udid, StringComparison.Ordinal)
                    && size.Width > 0 && size.Height > 0
                    && DateTime.UtcNow - _screenshotDisplayUpdatedUtc < TimeSpan.FromSeconds(2);
            }
        }

        private async Task<T> TrackScreenshotAsync<T>(Func<string, Task<T>> capture, Func<T, Rect> getSize)
        {
            string udid;
            long version;
            lock (_screenshotDisplayLock)
            {
                udid = currentDeviceId;
                version = _screenshotConnectionVersion;
            }
            Interlocked.Increment(ref _activeScreenshots);
            try
            {
                T result = await capture(udid);
                Rect size = getSize(result);
                lock (_screenshotDisplayLock)
                {
                    if (version == _screenshotConnectionVersion && size.Width > 0 && size.Height > 0)
                    {
                        _screenshotDisplaySize = size;
                        _screenshotDisplayUpdatedUtc = DateTime.UtcNow;
                    }
                }
                return result;
            }
            finally
            {
                Interlocked.Decrement(ref _activeScreenshots);
            }
        }

        public Task<Rect> GetCurrentDisplaySizeAsync(string udid)
        {
            return TrackScreenshotAsync(async deviceId =>
            {
                if (!string.Equals(deviceId, udid, StringComparison.Ordinal))
                    throw new InvalidOperationException("Connection changed; skipping orientation screenshot");
                return await AdbHelper.GetCurrentDisplaySizeAsync(deviceId);
            }, size => size);
        }


        public bool TryGetRecentPageSourceDisplaySize(string udid, out Rect size)
        {
            lock (_pageSourceDisplayLock)
            {
                size = _pageSourceDisplaySize;
                return string.Equals(_pageSourceDisplayDeviceId, udid, StringComparison.Ordinal)
                    && size.Width > 0 && size.Height > 0
                    && DateTime.UtcNow - _pageSourceDisplayUpdatedUtc < TimeSpan.FromSeconds(10);
            }
        }
        //public Action<MobileRotation> ChangeMobileRotation;
        public static MobileDeviceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_singletonLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new MobileDeviceManager();
                        }
                    }
                }
                return _instance;
            }
        }
        private static DateTime CreateProviderDeadline(int operationTimeoutMilliseconds)
        {
            return DateTime.UtcNow.AddMilliseconds(operationTimeoutMilliseconds + GrpcDeadlineGraceMilliseconds);
        }

        public MobileDeviceManager()
        {
            logger = App.Current.ServiceContainer.Resolve<ILogger>();
            _exceptionSubject = App.Current.ServiceContainer.Resolve<Subject<GlobalExceptionSubModel>>();
            elementRepository = new MobileElementRepository();
            pageSourceRefreshService = new PageSourceRefreshService(logger);
            //mobileServerClint = App.Current.MobileServiceClient;
            ParseElement = ParseXml;
            Semaphore = new Semaphore(initialCount: 1, maximumCount: 1);
        }
        public async Task StartAppiumServer()
        {
            if (AdbHelper.IsInstallMobileTools())
            {
                //AppiumServerPort = GetMobileServerPort();
                var retReps = await mobileServerClint.StartAppiumServerUnaryAsync(
                    new MobileRequest(),
                    deadline: CreateProviderDeadline(ProviderManagementTimeoutMilliseconds));
                if (retReps.Code != "0")
                {
                    logger.LogError($"打开appium服务异常:{retReps.Message}");
                }
                else
                {
                    if (string.IsNullOrEmpty(retReps.Info))
                    {
                        logger.LogError($"打开appium服务失败");
                    }
                    logger.LogInfo($"打开appium服务成功，端口号为：{retReps.Info}");
                    AppiumServerPort = retReps.Info;
                }
                await Task.Delay(3000);
            }
        }

        public async Task Connect(string deviceId, string platformVersion, bool forceReconnect = false)
        {
            await Task.Run(async () =>
            {
                long connectionVersion;
                lock (_pageSourceConnectionLock)
                {
                    IsConnected = false;
                    currentDeviceId = deviceId;
                    connectionVersion = ++_pageSourceConnectionVersion;
                    elementRepository.BeginConnection(deviceId, connectionVersion);
                    pageSourceRefreshService = new PageSourceRefreshService(logger);
                    CurrentElement = null;
                    BuildSelectorJson = null;
                }
                lock (_screenshotDisplayLock)
                {
                    _screenshotConnectionVersion++;
                    _screenshotDisplaySize = Rect.Empty;
                    _screenshotDisplayUpdatedUtc = DateTime.MinValue;
                }
                lock (_pageSourceDisplayLock)
                {
                    _pageSourceDisplayDeviceId = null;
                    _pageSourceDisplaySize = Rect.Empty;
                    _pageSourceDisplayUpdatedUtc = DateTime.MinValue;
                }
                try
                {
                    await App.Current.EnsureMobileProviderAvailableAsync();
                    var retReps = await mobileServerClint.MobileConnectUnaryAsync(new MobileRequest
                    {
                        Udid = deviceId,
                        PlatformName = "Android",
                        PlatformVersion = platformVersion,
                        TimeOut = ProviderConnectTimeoutMilliseconds,
                        InParameter = forceReconnect ? "ForceReconnect" : string.Empty
                    }, deadline: CreateProviderDeadline(ProviderConnectTimeoutMilliseconds));
                    if (retReps.Code != "0")
                    {
                        throw new Exception(Localize($"连接异常:{retReps.Message}", "controlType_MobileDeviceManager_AD6F59C8"));
                    }
                    lock (_pageSourceConnectionLock)
                    {
                        if (connectionVersion != _pageSourceConnectionVersion)
                            throw new InvalidOperationException("手机连接已切换，本次连接结果已过期");
                        IsConnected = true;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"手机Provider管道、连接或Appium会话异常，设备：{deviceId}，错误：{ex}");
                    throw;
                }
            });

        }

        public async Task<string> GetScreenshotBaseString()
        {
            var retReps = await mobileServerClint.MobileShotBaseStringGetUnaryAsync(new MobileRequest
            {
                Udid = currentDeviceId,
                TimeOut = ProviderManagementTimeoutMilliseconds
            }, deadline: CreateProviderDeadline(ProviderManagementTimeoutMilliseconds));
            if (retReps.Code != "0")
            {
                throw new Exception(Localize($"获取截屏异常:{retReps.Message}", "controlType_MobileDeviceManager_BCC99D99"));
            }
            return retReps.ShotBasestring;
        }
        public async Task<byte[]> GetScreenshotbyte()
        {
            return await TrackScreenshotAsync(async udid =>
            {
                var retReps = await mobileServerClint.MobileShotbyteGetUnaryAsync(new MobileRequest
                {
                    Udid = udid,
                    TimeOut = ProviderManagementTimeoutMilliseconds
                }, deadline: CreateProviderDeadline(ProviderManagementTimeoutMilliseconds));
                if (retReps.Code != "0")
                {
                    throw new Exception(Localize($"获取截屏异常:{retReps.Message}", "controlType_MobileDeviceManager_BCC99D99"));
                }
                return retReps.ShotData.ToArray();
            }, bytes => AdbHelper.GetPngDisplaySize(bytes));
        }

        public async Task<BitmapImage> GetScreenshot()
        {
            return await Task.Run(async () =>
             {
                 BitmapImage result = default(BitmapImage);
                 //byte[] bytes2 = Driver.GetScreenshot().AsByteArray;
                 return await TrackScreenshotAsync(async udid =>
                 {
                     var retReps = await mobileServerClint.MobileShotbyteGetUnaryAsync(new MobileRequest
                     {
                         Udid = udid,
                         TimeOut = ProviderManagementTimeoutMilliseconds
                     }, deadline: CreateProviderDeadline(ProviderManagementTimeoutMilliseconds));
                     if (retReps.Code != "0")
                     {
                         //当前界面无权限截图
                         if (retReps.Message.Contains("Failed to capture a screenshot")
                         || retReps.Message.Contains("UnableToCaptureScreen"))
                         {
                             return null;
                         }
                         throw new Exception(Localize($"获取截屏异常:{retReps.Message}", "controlType_MobileDeviceManager_BCC99D99"));
                     }
                     byte[] bytes2 = retReps.ShotData.ToArray();
                     result = CreateBitmapImage(bytes2);
                     return result;
                 }, bitmap => bitmap == null ? Rect.Empty : new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
             });

        }
        private BitmapImage CreateBitmapImage(byte[] bytes)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(bytes);
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }
        public void ParseXml()
        {
            RequestPageSourceRefresh(false, "Background");
        }

        public void MarkPageSourceDirty(string reason)
        {
            pageSourceRefreshService.MarkDirty(reason);
        }

        public bool IsCaptureMode => Interlocked.CompareExchange(ref captureModeCount, 0, 0) > 0;

        public async Task ResumeDesignTimeRefreshAsync(string reason, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string udid;
            long connectionVersion;
            lock (_pageSourceConnectionLock)
            {
                if (!IsConnected || string.IsNullOrWhiteSpace(currentDeviceId))
                    throw new InvalidOperationException("不存在连接的设备");
                udid = currentDeviceId;
                connectionVersion = _pageSourceConnectionVersion;
            }

            var response = await mobileServerClint.MobileEnterDesignTimeUnaryAsync(new MobileRequest
            {
                Udid = udid
            }, deadline: CreateProviderDeadline(ProviderManagementTimeoutMilliseconds), cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (response.Code != "0")
                throw new InvalidOperationException(response.Message);

            lock (_pageSourceConnectionLock)
            {
                if (!IsConnected || connectionVersion != _pageSourceConnectionVersion
                    || !string.Equals(currentDeviceId, udid, StringComparison.Ordinal))
                    throw new InvalidOperationException("手机连接已切换，无法恢复设计期刷新");
                pageSourceRefreshService.RequestRefresh(reason);
            }
        }

        public void BeginCaptureMode()
        {
            Interlocked.Increment(ref captureModeCount);
        }

        public void EndCaptureMode()
        {
            while (true)
            {
                int current = captureModeCount;
                if (current <= 0)
                {
                    return;
                }
                if (Interlocked.CompareExchange(ref captureModeCount, current - 1, current) == current)
                {
                    return;
                }
            }
        }

        public void ClearElementCache()
        {
            elementRepository.ReplaceElements(null);
        }

        private bool ShouldRefreshPageSourceBeforeCapture()
        {
            return !elementRepository.HasSnapshot || pageSourceRefreshService.ShouldRefreshForCapture();
        }

        public Task<bool> ForceRefreshPageSourceAsync(string reason)
        {
            bool waitForInFlightRefresh = !elementRepository.HasSnapshot;
            return Task.Run(() => RequestPageSourceRefresh(true, reason, waitForInFlightRefresh));
        }

        private async Task RefreshPageSourceForCaptureInBackgroundAsync()
        {
            try
            {
                bool refreshed = await ForceRefreshPageSourceAsync("CaptureBackground");
                logger.LogDebug($"ADB降级拾取后台PageSource刷新结束，结果[{refreshed}]，设备[{currentDeviceId}]");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"ADB降级拾取后台PageSource刷新异常，继续保留旧快照，设备[{currentDeviceId}]：{ex.Message}");
            }
        }

        private bool RequestPageSourceRefresh(bool force, string reason, bool waitForInFlightRefresh = true)
        {
            if (!IsConnected || IsResetNewDriver)
            {
                return false;
            }

            string requestDeviceId;
            long requestConnectionVersion;
            PageSourceRefreshService refreshService;
            lock (_pageSourceConnectionLock)
            {
                if (!IsConnected) return false;
                requestDeviceId = currentDeviceId;
                requestConnectionVersion = _pageSourceConnectionVersion;
                refreshService = pageSourceRefreshService;
            }
            return refreshService.TryRefresh(
                requestDeviceId,
                mobileServerClint,
                force,
                waitForInFlightRefresh,
                reason,
                (doc, pageSource, elapsedMilliseconds) =>
                {
                    lock (_pageSourceConnectionLock)
                    {
                        if (requestConnectionVersion != _pageSourceConnectionVersion
                            || !elementRepository.TryReplaceXmlSnapshot(doc, requestDeviceId, requestConnectionVersion))
                        {
                            logger.LogDebug($"忽略旧连接返回的PageSource，设备[{requestDeviceId}]，连接版本[{requestConnectionVersion}]，当前版本[{_pageSourceConnectionVersion}]");
                            return;
                        }
                        lock (_pageSourceDisplayLock)
                        {
                            // ADB dump dimensions may be inferred from wm size. Its screenshot
                            // is the fallback for display orientation when Appium Source is unavailable.
                            if (string.Equals(doc.DocumentElement?.GetAttribute("rpaSourceProvider"), "AdbDump", StringComparison.OrdinalIgnoreCase))
                            {
                                _pageSourceDisplayDeviceId = null;
                                _pageSourceDisplaySize = Rect.Empty;
                                _pageSourceDisplayUpdatedUtc = DateTime.MinValue;
                            }
                            else if (double.TryParse(doc.DocumentElement?.GetAttribute("width"), out double width)
                                && double.TryParse(doc.DocumentElement?.GetAttribute("height"), out double height)
                                && width > 0 && height > 0)
                            {
                                _pageSourceDisplayDeviceId = requestDeviceId;
                                _pageSourceDisplaySize = new Rect(0, 0, width, height);
                                _pageSourceDisplayUpdatedUtc = DateTime.UtcNow;
                            }
                        }
                    }
                },
                message => logger.LogDebug(message));
        }

        private async Task ResetNewDriver()
        {
            //IsResetNewDriver = true;
            //Driver?.Quit();
            //Driver?.Dispose();
            //Driver = null;
            //AdbHelper.ReleaseAppium(currentDeviceId);
            //Driver = await Task.Run(delegate
            //{
            //    try
            //    {
            //        AppiumOptions desiredCapabilities = new AppiumOptions();
            //        desiredCapabilities.AddAdditionalCapability(MobileCapabilityType.PlatformName, "Android");
            //        // desiredCapabilities.AddAdditionalCapability(MobileCapabilityType.PlatformVersion, device.platformVersion);
            //        desiredCapabilities.AddAdditionalCapability(MobileCapabilityType.DeviceName, currentDeviceId);
            //        desiredCapabilities.AddAdditionalCapability(MobileCapabilityType.AutomationName, "UiAutomator2");
            //        desiredCapabilities.AddAdditionalCapability(MobileCapabilityType.NewCommandTimeout, 60000);
            //        desiredCapabilities.AddAdditionalCapability(MobileCapabilityType.NoReset, true);
            //        desiredCapabilities.AddAdditionalCapability(MobileCapabilityType.Udid, currentDeviceId);
            //        var driver = new AndroidDriver<AppiumWebElement>(new Uri($"http://127.0.0.1:{AppiumServerPort}/wd/hub"), desiredCapabilities, TimeSpan.FromSeconds(60));
            //        //Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            //        driver.Settings = new Dictionary<string, object>()
            //        {
            //            ["waitForIdleTimeout"] = 100
            //        };
            //        var sessionDetails = driver.SessionDetails.ToList();//连接设备的参数信息
            //        return driver;
            //    }
            //    catch (Exception ex)
            //    {
            //        logger.LogError($"重新连接手机appium异常：{ex}");
            //        _exceptionSubject.OnNext(new GlobalExceptionSubModel(ExceptionType.AppiumEx, $"{ex.Message}"));
            //        return null;
            //    }
            //});
            //IsResetNewDriver = false;
        }

        public MobileElement GetElementFromPoint(System.Drawing.Point location)
        {
            try
            {
                CurrentElement = elementRepository.GetElementFromPoint(location);
                return CurrentElement;
            }
            catch
            {
                return null;
            }
        }
        public async Task<MobileElement> GetSelectedElementFromPoint(System.Drawing.Point location)
        {
            try
            {
                CurrentElement = await elementRepository.GetSelectedElementFromPointAsync(location);
                return CurrentElement;
            }
            catch
            {
                return null;
            }
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINTt pt);
        [StructLayout(LayoutKind.Sequential)]
        public struct POINTt
        {
            public int X;
            public int Y;



            public POINTt(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }
        /// <summary>
        /// 元素拾取和点击
        /// </summary>
        /// <param name="location"></param>
        /// <param name="isClick"></param>
        /// <returns></returns>
        public async Task<MobileElement> ClickElementWithPoint(System.Drawing.Point location, bool isClick, bool isSimilarSelector = false, string firstSelector = "")
        {
            Stopwatch captureStopwatch = Stopwatch.StartNew();
            MobileElement mobileElement = null;
            try
            {
                //元素拾取
                if (isClick)
                {
                    bool refreshedBeforePick = false;
                    if (ShouldRefreshPageSourceBeforeCapture())
                    {
                        Stopwatch refreshStopwatch = Stopwatch.StartNew();
                        if (elementRepository.HasSnapshot && elementRepository.IsAdbDegraded)
                        {
                            // ADB dump通常需要数秒。已有旧快照时让刷新继续在后台完成，当前拾取立即使用旧快照。
                            _ = RefreshPageSourceForCaptureInBackgroundAsync();
                            logger.LogDebug($"ADB降级拾取使用旧快照，PageSource刷新已转后台，快照版本[{elementRepository.SnapshotVersion}]，设备[{currentDeviceId}]");
                        }
                        else
                        {
                            refreshedBeforePick = await ForceRefreshPageSourceAsync("Capture");
                        }
                        refreshStopwatch.Stop();
                        logger.LogDebug($"拾取PageSource准备结束，等待耗时[{refreshStopwatch.ElapsedMilliseconds}ms]，同步刷新结果[{refreshedBeforePick}]，ADB降级[{elementRepository.IsAdbDegraded}]，设备[{currentDeviceId}]");
                    }
                    //mobileElement = GetElementFromPoint(location);
                    Stopwatch hitTestStopwatch = Stopwatch.StartNew();
                    mobileElement = await GetSelectedElementFromPoint(location);
                    hitTestStopwatch.Stop();
                    logger.LogDebug($"拾取鼠标命中结束，耗时[{hitTestStopwatch.ElapsedMilliseconds}ms]，是否命中[{mobileElement != null}]，设备[{currentDeviceId}]");
                    if (mobileElement == null && !refreshedBeforePick)
                    {
                        await ForceRefreshPageSourceAsync("CaptureRetry");
                        mobileElement = await GetSelectedElementFromPoint(location);
                    }
                    if (mobileElement != null)
                    {
                        IReadOnlyList<MobileElement> captureSnapshot;
                        if (!elementRepository.TryGetSnapshotForElement(mobileElement, out captureSnapshot))
                        {
                            // 后台刷新可能恰好在命中后完成，使用同一坐标从最新快照重选一次。
                            mobileElement = await GetSelectedElementFromPoint(location);
                            if (!elementRepository.TryGetSnapshotForElement(mobileElement, out captureSnapshot))
                            {
                                throw new Exception("拾取元素所属页面快照已更新，请重新拾取");
                            }
                        }
                        bool isDegradedElement = mobileElement.IsFromDegradedPageSource;
                        Stopwatch validationStopwatch = Stopwatch.StartNew();
                        BuildSelectorJson = MobileLocalSelectorService.BuildVerifiedSelector(
                            mobileElement,
                            captureSnapshot,
                            allowTargetAnchor: !isSimilarSelector,
                            referenceSelector: isSimilarSelector ? firstSelector : null);
                        if (string.IsNullOrEmpty(BuildSelectorJson))
                        {
                            string message = isSimilarSelector
                                ? "相似元素与原元素结构不一致或无法构建有效Selector"
                                : "本地构建元素Selector失败".I18n("controlType_MobileDeviceManager_C7AFBC25");
                            throw new Exception(message);
                        }
                        // Finish structural matching before screenshot awaits can replace the live cache.
                        mobileElement.CapturedSimilarSelector = isSimilarSelector && !string.IsNullOrWhiteSpace(firstSelector)
                            ? JsonHelper.ToJson(MobileLocalSelectorService.BuildSimilarSelector(
                                firstSelector, BuildSelectorJson, mobileElement, captureSnapshot))
                            : null;
                        logger.LogInfo($"已在DeviceManager当前快照中完成元素拾取校验，来源[{mobileElement.SourceProvider}]，相似拾取[{isSimilarSelector}]，设备[{currentDeviceId}]");
                        validationStopwatch.Stop();
                        logger.LogDebug($"拾取后元素本地校验结束，耗时[{validationStopwatch.ElapsedMilliseconds}ms]，来源[{mobileElement.SourceProvider}]，设备[{currentDeviceId}]");
                        Stopwatch screenshotStopwatch = Stopwatch.StartNew();
                        Bitmap targetBitMap = isDegradedElement
                            ? await CaptureAdbElementTemplateAsync(mobileElement.BoundRectangle)
                            : await CaptureElementTemplateAsync(mobileElement.BoundRectangle);
                        bool usedMirrorScreenshot = false;
                        #region Element screenshot fallback
                        int maxAttempts = 3;
                        if (targetBitMap == null || !IsBitmapValid(targetBitMap))
                        {
                            targetBitMap?.Dispose();
                            targetBitMap = null;
                            usedMirrorScreenshot = true;
                            for (int attempt = 0; attempt < maxAttempts; attempt++)
                            {
                                targetBitMap = CaptureMirrorElementTemplate(mobileElement.BoundRectangle);
                                if (IsBitmapValid(targetBitMap))
                                {
                                    logger.LogDebug("投屏元素截图有效");
                                    break;
                                }
                                targetBitMap?.Dispose();
                                targetBitMap = null;
                                await Task.Delay(100);
                            }
                        }
                        #endregion
                        screenshotStopwatch.Stop();
                        logger.LogDebug($"拾取元素截图结束，耗时[{screenshotStopwatch.ElapsedMilliseconds}ms]，ADB截图[{isDegradedElement}]，投屏降级[{usedMirrorScreenshot}]，截图有效[{IsBitmapValid(targetBitMap)}]，设备[{currentDeviceId}]");
                        mobileElement.currentInformativeScreenshot = targetBitMap;
                        CurrentCaptureElementShot = targetBitMap;
                        captureStopwatch.Stop();
                        logger.LogInfo($"手机元素拾取完成，总耗时[{captureStopwatch.ElapsedMilliseconds}ms]，PageSource来源[{mobileElement.SourceProvider}]，快照版本[{mobileElement.SourceSnapshotVersion}]，设备[{currentDeviceId}]");
                        return mobileElement;
                    }
                }
            }
            catch (NotFoundException ex)
            {
                return null;
                //throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.ClickMobileElement, ex);
            }
            catch (RPAException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                //if (ex.Message.Contains("secure"))
                //{
                //    if (mobileElement != null)
                //    {
                //        Assembly assembly = GetType().Assembly;
                //        //这时候的路径是：命名空间+文件路径，以点相隔
                //        System.IO.Stream streamSmall = assembly.GetManifestResourceStream("II.RPA.Plugin.Mobile.Resource.replaceimg.png");
                //        Image img = Image.FromStream(streamSmall);
                //        mobileElement.currentInformativeScreenshot = new Bitmap(img);
                //        return mobileElement;
                //    }
                //}
                //throw new RPAException(eErrorMessageType.Error_ClickError, eComponentType.ClickMobileElement, ex);
                //return null;
            }
            return null;
        }
        /// <summary>
        /// 检查位图是否有效（非全黑）
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        private bool IsBitmapValid(Bitmap bmp)
        {
            if (bmp == null)
            {
                return false;
            }
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                byte[] pixels = new byte[bmpData.Stride * bmpData.Height];
                Marshal.Copy(bmpData.Scan0, pixels, 0, pixels.Length);
                // 如果前100个像素不全是0，则认为有效
                for (int i = 0; i < Math.Min(100 * 4, pixels.Length); i += 4)
                {
                    if (pixels[i] != 0 || pixels[i + 1] != 0 || pixels[i + 2] != 0)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"判断截图是否全黑异常：{ex}");
                return true;
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }
        }
        private bool HasCurrentPageSourceSnapshot()
        {
            lock (_pageSourceConnectionLock)
            {
                return elementRepository.HasSnapshotFor(currentDeviceId, _pageSourceConnectionVersion);
            }
        }

        private async Task EnsurePageSourceSnapshotForValidationAsync()
        {
            PageSourceRefreshService refreshService = pageSourceRefreshService;
            long requestedVersion = refreshService.GetRequestedVersion();
            if (HasCurrentPageSourceSnapshot() && refreshService.HasRefreshedThrough(requestedVersion))
            {
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            logger.LogDebug($"元素校验等待当前PageSource快照刷新，设备[{currentDeviceId}]");
            bool refreshed = await Task.Run(() => RequestPageSourceRefresh(true, "ElementValidation", true));
            // 在途刷新可能早于校验请求；只等待覆盖本次请求的快照，不追逐后续刷新标记。
            if (refreshed && !refreshService.HasRefreshedThrough(requestedVersion))
                refreshed = await Task.Run(() => RequestPageSourceRefresh(true, "ElementValidationAfterInFlight", true));
            stopwatch.Stop();
            if (!ReferenceEquals(refreshService, pageSourceRefreshService)
                || !HasCurrentPageSourceSnapshot() || !refreshService.HasRefreshedThrough(requestedVersion))
            {
                throw new InvalidOperationException($"手机当前页面结构尚未刷新，无法校验元素，刷新结果[{refreshed}]，耗时[{stopwatch.ElapsedMilliseconds}ms]，设备[{currentDeviceId}]");
            }

            logger.LogDebug($"元素校验当前PageSource快照已就绪，耗时[{stopwatch.ElapsedMilliseconds}ms]，来源[{elementRepository.SourceProvider}]，快照版本[{elementRepository.SnapshotVersion}]，设备[{currentDeviceId}]");
        }
        public async Task<List<Rect>> GetSimilarElementsRectAsync(string selecter)
        {
            await EnsurePageSourceSnapshotForValidationAsync();
            Stopwatch stopwatch = Stopwatch.StartNew();
            IReadOnlyList<MobileElement> snapshot = elementRepository.Elements;
            List<Rect> rects = MobileLocalSelectorService.FindMatches(selecter, snapshot)
                .Where(elementRepository.IsCurrentSnapshotElement)
                .Select(element => new Rect(
                    element.BoundRectangle.X / mobileForm._dpiX,
                    element.BoundRectangle.Y / mobileForm._dpiY,
                    element.BoundRectangle.Width / mobileForm._dpiX,
                    element.BoundRectangle.Height / mobileForm._dpiY))
                .ToList();
            stopwatch.Stop();
            logger.LogDebug($"DeviceManager本地完成相似元素校验，匹配数量[{rects.Count}]，耗时[{stopwatch.ElapsedMilliseconds}ms]，来源[{elementRepository.SourceProvider}]，快照版本[{elementRepository.SnapshotVersion}]，设备[{currentDeviceId}]");
            return rects;
        }

        public async Task<Rect> GetElementRectAsync(string selecter)
        {
            await EnsurePageSourceSnapshotForValidationAsync();
            Stopwatch stopwatch = Stopwatch.StartNew();
            MobileElement localElement = MobileLocalSelectorService.FindMatches(selecter, elementRepository.Elements)
                .FirstOrDefault(elementRepository.IsCurrentSnapshotElement);
            stopwatch.Stop();
            if (localElement == null)
            {
                throw new NotFoundException($"当前PageSource快照中未找到目标元素，来源[{elementRepository.SourceProvider}]，快照版本[{elementRepository.SnapshotVersion}]");
            }

            logger.LogDebug($"DeviceManager本地完成普通元素校验，耗时[{stopwatch.ElapsedMilliseconds}ms]，来源[{elementRepository.SourceProvider}]，快照版本[{elementRepository.SnapshotVersion}]，设备[{currentDeviceId}]");
            return new Rect(
                localElement.BoundRectangle.X / mobileForm._dpiX,
                localElement.BoundRectangle.Y / mobileForm._dpiY,
                localElement.BoundRectangle.Width / mobileForm._dpiX,
                localElement.BoundRectangle.Height / mobileForm._dpiY);
        }
        public Task PressNavigationKeyAsync(MobileNavigationKey key)
        {
            return AdbHelper.PressNavigationKeyAsync(currentDeviceId, key);
        }
        public async Task PressKeyCode(string keyCode)
        {
            var retReps = await mobileServerClint.MobilePressKeyUnaryAsync(new MobileRequest
            {
                Udid = currentDeviceId,
                Action = keyCode,
                TimeOut = ProviderManagementTimeoutMilliseconds
            }, deadline: CreateProviderDeadline(ProviderManagementTimeoutMilliseconds));
            if (retReps.Code != "0")
            {
                throw new InvalidOperationException($"{"按键异常".I18n("controlType_MobileDeviceManager_502D7D52")}，设备[{currentDeviceId}]，按键[{keyCode}]，响应码[{retReps.Code}]，原因[{retReps.Message}]");
            }
        }

        public async Task TestSlid(string slidRectStr)
        {
            var retReps = await mobileServerClint.MobileTestSlidUnaryAsync(new MobileRequest
            {
                Udid = currentDeviceId,
                InParameter = slidRectStr,
                TimeOut = ProviderManagementTimeoutMilliseconds
            }, deadline: CreateProviderDeadline(ProviderManagementTimeoutMilliseconds));
            if (retReps.Code != "0")
            {
                throw new Exception("测试滑动异常".I18n("controlType_MobileDeviceManager_502D7D52"));
            }
        }

        #region Class
        public class ElementRect
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
        }
        #endregion
        #region CV
        public string CvUrl { get; set; }
        public bool isCvCapture
        {
            get => Volatile.Read(ref isCvCaptureValue);
            set
            {
                Volatile.Write(ref isCvCaptureValue, value);
                Interlocked.Increment(ref cvCaptureGeneration);
            }
        }
        public bool CvLoading { get; set; }

        public void ParseCV()
        {
            if (!isCvCapture)
            {
                return;
            }

            Interlocked.Increment(ref cvParseRequestVersion);
            TryStartCvParse();
        }

        private void TryStartCvParse()
        {
            if (Interlocked.CompareExchange(ref cvParseState, 1, 0) == 0)
            {
                _ = RunCvParseRequestsAsync();
            }
        }

        private async Task RunCvParseRequestsAsync()
        {
            int handledVersion = 0;
            CvLoading = true;

            try
            {
                while (isCvCapture)
                {
                    handledVersion = Volatile.Read(ref cvParseRequestVersion);
                    int generation = Volatile.Read(ref cvCaptureGeneration);
                    string cvUrl = CvUrl;
                    CvException = null;

                    try
                    {
                        var shotBaseString = await GetScreenshotBaseString();
                        if (!IsCurrentCvCapture(generation, handledVersion))
                        {
                            continue;
                        }

                        CVHelper cVHelper = new CVHelper(shotBaseString, cvUrl);
                        cVHelper.CVParse();

                        if (IsCurrentCvCapture(generation, handledVersion))
                        {
                            elementRepository.ReplaceElements(cVHelper.GetCVMobileElements());
                        }
                    }
                    catch (Exception ex)
                    {
                        if (IsCurrentCvCapture(generation, handledVersion))
                        {
                            CvException = ex;
                        }
                    }

                    if (handledVersion == Volatile.Read(ref cvParseRequestVersion))
                    {
                        return;
                    }
                }
            }
            finally
            {
                CvLoading = false;
                Interlocked.Exchange(ref cvParseState, 0);

                if (isCvCapture && handledVersion != Volatile.Read(ref cvParseRequestVersion))
                {
                    TryStartCvParse();
                }
            }
        }

        private bool IsCurrentCvCapture(int generation, int requestVersion)
        {
            return isCvCapture
                && generation == Volatile.Read(ref cvCaptureGeneration)
                && requestVersion == Volatile.Read(ref cvParseRequestVersion);
        }
        public async Task<MobileElement> ClickElementWithPointCV(System.Drawing.Point location, bool isClick)
        {
            return await Task.Run(async () =>
             {
                 try
                 {
                     //元素拾取
                     if (isClick)
                     {
                         MobileElement mobileElement = GetElementFromPoint(location);
                         if (mobileElement != null)
                         {
                             if (mobileElement is CVMobileElement cvElement)
                             {
                                 var shotbyte = await GetScreenshotbyte();
                                 //cvElement.SaveSelectImage(Driver.GetScreenshot().AsByteArray);
                                 cvElement.SaveSelectImage(shotbyte);
                                 return cvElement;
                             }
                         }
                     }
                     else
                     {
                         return null;
                     }


                 }
                 catch (NotFoundException ex)
                 {
                     return null;
                     //throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.ClickMobileElement, ex);
                 }
                 catch (Exception ex)
                 {
                     logger.LogError(ex);
                     throw new RPAException(eErrorMessageType.Error_ClickError, eComponentType.ClickMobileElement, ex);
                 }
                 return null;
             });
        }
        #endregion


        #region 后续弃用，全部挪到手机管理独立应用去做服务管理

        private static Process StartMobileServer(string port)
        {
            string serverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mobile/II.RPA.MobileServer.exe");
            string path = Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA", "mobile");
            List<string> envList = new List<string>();
            string androidHome = Path.Combine(path, "AndroidSDK");
            string javaHome = Path.Combine(path, "JavaSDK\\jdk1.8.0_241");
            string appiumHome = Path.Combine(path, "appium");
            envList.Add(appiumHome);
            envList.Add("%JAVA_HOME%\\bin");
            envList.Add("%ANDROID_HOME%\\platform-tools");
            envList.Add("%ANDROID_HOME%\\build-tools\\29.0.3");
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                Arguments = $"{port} {Process.GetCurrentProcess().Id}",
                FileName = serverPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            process.StartInfo.EnvironmentVariables["JAVA_HOME"] = javaHome;
            process.StartInfo.EnvironmentVariables["APPIUM_HOME"] = appiumHome;
            process.StartInfo.EnvironmentVariables["ANDROID_HOME"] = androidHome;
            process.StartInfo.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("path") + string.Join(";", envList);
            process.Start();
            return process;
        }
        private string GetAvailablePort()
        {
            int port = 4723;
            int procid = 0;
            for (int i = 0; i < 10; i++)
            {
                string matchResult = DosHelper.ExcuteCMD($"netstat -ano |findstr {port}");
                if (string.IsNullOrEmpty(matchResult))
                {
                    return port.ToString();
                }
                if (port == 4723)
                {
                    MatchCollection portMatches = Regex.Matches(matchResult, @"(\d+)(?!.*\d)");
                    procid = int.Parse(portMatches[0].Value);
                }
                port += 2;
            }
            if (procid != 0)
            {
                logger.LogInfo($"端口不够用,将主动杀掉4723占用的进程");
                DosHelper.ExcuteCMD($"taskkill /pid {procid} -t -f");
                string matchResult = DosHelper.ExcuteCMD($"netstat -ano |findstr {4723}");
                if (string.IsNullOrEmpty(matchResult))
                {
                    return "4723";
                }
            }
            throw new Exception("没可用端口，请结束占用4723端口的程序".I18n("controlType_MobileDeviceManager_4F2712F2"));
        }

        public string GetMobileServerPort()
        {
            string port = "4723";
            if (CheckProcessIsRunning("II.RPA.MobileServer.exe"))
            {
                if (CheckProcessIsRunning("node.exe"))
                {
                    string result = DosHelper.ExcuteCMD("tasklist /svc | findstr node.exe");
                    foreach (Match procid in new Regex("\\d+").Matches(result))
                    {
                        string portInfo = DosHelper.ExcuteCMD("netstat -ano | findstr " + procid.ToString());
                        MatchCollection portMatches = new Regex("TCP\\s*0.0.0.0:(?<port>\\d+)").Matches(portInfo);
                        if (portMatches.Count == 1)
                        {
                            return portMatches[0].Result("${port}");
                        }
                    }
                    throw new Exception("获取内置手机自动化服务端口失败".I18n("controlType_MobileDeviceManager_6DCE95DA"));
                }
                Process[] allProcessesByName = Common.ProcessHelper.GetAllProcessesByName("II.RPA.MobileServer.exe");
                for (int i = 0; i < allProcessesByName.Length; i++)
                {
                    allProcessesByName[i].Kill();
                }
                port = GetAvailablePort();
                StartMobileServer(port);
            }
            else
            {
                port = GetAvailablePort();
                StartMobileServer(port);
            }
            return port;
        }
        private static bool CheckProcessIsRunning(string pName)
        {
            return Common.ProcessHelper.GetAllProcessesByName(pName).Count() > 0;
        }
        public async Task<string> GetCurrentAppPackageNameAsync()
        {
            try
            {
                var appInfo = await GetCurrentAppInfoAsync();
                return appInfo.Item1;
            }
            catch (Exception ex)
            {
                logger.LogError($"获取当前包名异常", ex);
                return string.Empty;
            }
        }

        private Bitmap CaptureMirrorElementTemplate(System.Drawing.Rectangle elementBounds)
        {
            if (mobileForm == null || mobileForm.grid == null
                || mobileForm._bitmapImageWidth <= 0 || mobileForm._bitmapImageHeight <= 0)
                return null;

            var origin = mobileForm.grid.PointToScreen(new System.Windows.Point(0, 0));
            double scaleX = mobileForm.grid.ActualWidth * mobileForm._dpiX / mobileForm._bitmapImageWidth;
            double scaleY = mobileForm.grid.ActualHeight * mobileForm._dpiY / mobileForm._bitmapImageHeight;
            int left = (int)Math.Round(origin.X + elementBounds.Left * scaleX);
            int top = (int)Math.Round(origin.Y + elementBounds.Top * scaleY);
            int right = (int)Math.Round(origin.X + elementBounds.Right * scaleX);
            int bottom = (int)Math.Round(origin.Y + elementBounds.Bottom * scaleY);
            if (right <= left || bottom <= top) return null;
            return ImageHelper.Screenshot(left, top, right - left, bottom - top);
        }

        private async Task<Bitmap> CaptureAdbElementTemplateAsync(System.Drawing.Rectangle elementBounds)
        {
            try
            {
                byte[] shotBytes = await TrackScreenshotAsync(
                    udid => AdbHelper.CaptureScreenAsync(udid, CaptureAdbScreenshotTimeoutMilliseconds),
                    bytes => AdbHelper.GetPngDisplaySize(bytes));
                using (var source = new Bitmap(new MemoryStream(shotBytes)))
                {
                    System.Drawing.Rectangle cropRect = System.Drawing.Rectangle.Intersect(
                        elementBounds,
                        new System.Drawing.Rectangle(0, 0, source.Width, source.Height));
                    if (cropRect.Width <= 0 || cropRect.Height <= 0) return null;

                    var target = new Bitmap(cropRect.Width, cropRect.Height);
                    using (Graphics graphics = Graphics.FromImage(target))
                    {
                        graphics.DrawImage(
                            source,
                            new System.Drawing.Rectangle(0, 0, target.Width, target.Height),
                            cropRect,
                            GraphicsUnit.Pixel);
                    }
                    return target;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"ADB降级元素截图失败，将回退到当前投屏画面：{ex}");
                return null;
            }
        }
        private async Task<Bitmap> CaptureElementTemplateAsync(System.Drawing.Rectangle elementBounds)
        {
            try
            {
                byte[] shotBytes = await GetScreenshotbyte();
                using (Bitmap source = new Bitmap(new MemoryStream(shotBytes)))
                {
                    var cropRect = System.Drawing.Rectangle.Intersect(
                        elementBounds,
                        new System.Drawing.Rectangle(0, 0, source.Width, source.Height));
                    if (cropRect.Width <= 0 || cropRect.Height <= 0)
                    {
                        return null;
                    }

                    Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);
                    using (Graphics g = Graphics.FromImage(target))
                    {
                        g.DrawImage(source, new System.Drawing.Rectangle(0, 0, target.Width, target.Height), cropRect, GraphicsUnit.Pixel);
                    }
                    return target;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"手机元素模板截图失败：{ex}");
                return null;
            }
        }

        public Task<Tuple<string, string>> GetCurrentAppInfoAsync()
        {
            try
            {
                string packageContent = DosHelper.Execute($"{AdbHelper.adbPath} -s {currentDeviceId} shell dumpsys window | findstr /R \"mCurrentFocus mFocusedApp\"");
                foreach (string line in packageContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    Match match = Regex.Match(line, @"\s([A-Za-z0-9_.$]+)/([A-Za-z0-9_.$]+)");
                    if (match.Success)
                    {
                        string packageName = match.Groups[1].Value;
                        string activityName = match.Groups[2].Value;
                        if (activityName.StartsWith("."))
                        {
                            activityName = packageName + activityName;
                        }
                        return Task.FromResult(Tuple.Create(packageName, activityName));
                    }
                }
                return Task.FromResult(Tuple.Create(string.Empty, string.Empty));
            }
            catch (Exception ex)
            {
                logger.LogError($"获取当前手机界面信息异常", ex);
                return Task.FromResult(Tuple.Create(string.Empty, string.Empty));
            }
        }

        #endregion
    }
}
