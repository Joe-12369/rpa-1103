using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using Castle.Core.Internal;
using II.RPA.Logging;
using II.RPA.MobileProvider.CV;
using II.RPA.MobileProvider.Params;
using II.RPA.MobileProvider.Provider;
using II.RPA.MobileProvider.Selecter;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.MobileProvider.Warn;
using System.Windows.Controls;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;

namespace II.RPA.MobileProvider.Service
{
    public class MobileAbilityServer
    {
        private readonly static ILogger _logger = Logger.OfContext();
        private const int PageSourceTimeoutMilliseconds = 7000;
        private const int DefaultSessionOperationTimeoutMilliseconds = 30000;
        private static readonly DesignTimePageSourceService DesignTimePageSourceService = new DesignTimePageSourceService();
        private static MobileSession ResolveMobile(string sessionId)
        {
            try
            {
                return MobileSessionCache.WithSessionLock(sessionId, () =>
                {
                    MobileAutomationStateSnapshot state = MobileAutomationStateStore.GetSnapshot(sessionId);
                    if (state.Mode == MobileAutomationMode.AdbDegraded || state.Mode == MobileAutomationMode.Unavailable)
                    {
                        MobileAutomationStateStore.MarkReconnecting(sessionId);
                    }
                    MobileSession mobileSession = MobileSessionCache.GetValidSession(sessionId);
                    if (mobileSession == null)
                    {
                        throw new RPAException(eErrorMessageType.Error_ConnectFail);
                    }
                    return mobileSession;
                });
            }
            catch (Exception ex)
            {
                throw new RPAException(eErrorMessageType.Error_ConnectFail, ex);
            }
        }
        /// <summary>
        /// 相似元素
        /// </summary>
        /// <param name="paramStr"></param>
        /// <returns></returns>
        public static async Task<string> GetSimilarElementList(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamGetSimilarElementInfoModel>(paramStr);
                return await RunWithTimeOutAsync(async ct =>
                {
                    return await Task.Run(() =>
                    {
                        //非元素结构模式
                        if (!string.IsNullOrEmpty(paramModel.QueryMode) && paramModel.QueryMode != "default")
                        {
                            return ResolveMobile(paramModel.sessionId).GetSimilarElementListByCondition(paramModel.QueryMode, paramModel.selector);
                        }
                        else
                        {
                            List<List<MobileItem>> elements = ResolveMobile(paramModel.sessionId).GetSimilarElementList(paramModel.selector);
                            _logger.LogInformation($"获得相似元素个数：{elements.Count}");
                            if (elements.Count < 1)
                            {
                                throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.MobileSimilarElement);
                            }
                            return ParseSimilarListToJson(elements, paramModel.selector);
                        }
                    }, ct);
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(GetSimilarElementList));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.MobileSimilarElement);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public static async Task<string> GetElement(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamGetElementModel>(paramStr);
                return await RunWithTimeOutAsync(async ct =>
                {
                    return await Task.Run(() =>
                    {
                        //xpath模式
                        if (!string.IsNullOrEmpty(paramModel.QueryMode) && paramModel.QueryMode != "default")
                        {
                            return ResolveMobile(paramModel.sessionId).GetElementByCondition(paramModel.QueryMode, paramModel.selector);
                        }
                        else
                        {
                            var element = ResolveMobile(paramModel.sessionId).FindElement(paramModel.selector, paramModel);
                            if (element != null)
                            {
                                return paramModel.selector;
                            }
                            else
                            {
                                throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.GetMobileElement);
                            }
                        }
                    }, ct);
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(GetElement));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.GetMobileElement);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 点击屏幕位置
        /// </summary>
        /// <param name="paramStr"></param>
        /// <returns></returns>
        public static async Task ClickScreen(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamClickScreenModel>(paramStr);
                await RunWithTimeOutAsync(async ct =>
                {
                    await Task.Run(() =>
                    {
                        ResolveMobile(paramModel.sessionId).ClickScreen(paramModel);
                    }, ct);
                    return true;
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(ClickScreen));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.ClickMobileScreen);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 触摸滑动
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task TouchSlid(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamTouchSlidModel>(paramStr);
                await RunSessionOperationWithTimeoutAsync(
                    paramModel.sessionId,
                    nameof(TouchSlid),
                    timeOut,
                    mobileSession =>
                    {
                        mobileSession.TouchSlid(paramModel);
                        return true;
                    });
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.TouchSlideMobile);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 触摸滑动V2
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task TouchSlid_V2(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamTouchSlidModel_V2>(paramStr);
await RunSessionOperationWithTimeoutAsync(
                    paramModel.sessionId,
                    nameof(TouchSlid_V2),
                    timeOut,
                    mobileSession =>
                    {
                        mobileSession.TouchSlid_V2(paramModel);
                        return true;
                    });
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.TouchSlideMobile);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 触摸滑动V3：普通直线滑动使用ADB，复杂连续手势使用W3C Actions。
        /// </summary>
        public static async Task TouchSlid_V3(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamTouchSlidModel_V2>(paramStr);
                await RunSessionOperationWithTimeoutAsync(
                    paramModel.sessionId,
                    nameof(TouchSlid_V3),
                    timeOut,
                    mobileSession =>
                    {
                        mobileSession.TouchSlid_V3(paramModel);
                        return true;
                    });
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.TouchSlideMobile);
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 截屏
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task<string> GetScreenshot(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamScreenshotModel>(paramStr);
                return await RunWithTimeOutAsync(async ct =>
                {
                    return await Task.Run(() =>
                    {
                        ResolveMobile(paramModel.sessionId).GetScreenshot(paramModel);
                        return paramModel.savePath + @"\" + paramModel.fileName;
                    }, ct);
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(GetScreenshot));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.GetMobileScreenshot);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 元素点击
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task ClickElement(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamClickElementModel>(paramStr);
                await RunWithTimeOutAsync(async ct =>
                {
                    await Task.Run(() =>
                    {
                        ResolveMobile(paramModel.sessionId).ClickElement(paramModel);
                    }, ct);
                    return true;
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(ClickElement));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.ClickMobileElement);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 元素截图
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task<string> GetElementScreenshot(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamElementScreenshotModel>(paramStr);
                return await RunWithTimeOutAsync(async ct =>
                {
                    return await Task.Run(() =>
                    {
                        ResolveMobile(paramModel.sessionId).GetElementScreenshot(paramModel);
                        return paramModel.savePath + @"\" + paramModel.fileName;
                    }, ct);
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(GetElementScreenshot));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.ElementScreenshot);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 点击手机功能按键
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task PressMobileKey(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamClickKeyButtonModel>(paramStr);
                await RunSessionOperationWithTimeoutAsync(
                    paramModel.sessionId,
                    nameof(PressMobileKey),
                    timeOut,
                    mobileSession =>
                    {
                        mobileSession.ClickKeyButton(paramModel);
                        return true;
                    });
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.PressMobileKey);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 获取屏幕方向
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task<string> GetOrientation(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamDeviceSessionModel>(paramStr);
                return await RunWithTimeOutAsync(async ct =>
                {
                    return await Task.Run(() =>
                    {
                        return ResolveMobile(paramModel.sessionId).GetOriention();
                    }, ct);
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(GetOrientation));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.GetMobileOrientation);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 设置屏幕方向
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task SetOrientation(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamSetOrientionModel>(paramStr);
                await RunWithTimeOutAsync(async ct =>
                {
                    await Task.Run(() =>
                    {
                        ResolveMobile(paramModel.sessionId).SetOriention(paramModel);
                    }, ct);
                    return true;
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(SetOrientation));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.SetMobileOrientation);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 获取子元素
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task<string> GetMobileElementChild(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamMobileElementTextModel>(paramStr);
                return await RunWithTimeOutAsync(async ct =>
                {
                    return await Task.Run(() =>
                    {
                        return ResolveMobile(paramModel.sessionId).GetElementChildInfo(paramModel);
                    }, ct);
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(GetMobileElementChild));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.GetElementChild);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 获取元素文本
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task<string> GetMobileElementText(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamMobileElementTextModel>(paramStr);
                return await RunWithTimeOutAsync(async ct =>
                {
                    return await Task.Run(() =>
                    {
                        return ResolveMobile(paramModel.sessionId).GetMobileElementText(paramModel);
                    }, ct);
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(GetMobileElementText));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.GetText);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 获取元素属性
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task<string> GetMobileElementAttribute(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamMobileElementAttributeModel>(paramStr);
                return await RunWithTimeOutAsync(async ct =>
                {
                    return await Task.Run(() =>
                    {
                        return ResolveMobile(paramModel.sessionId).GetElementAttributeInfo(paramModel);
                    }, ct);
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(GetMobileElementAttribute));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.GetElementAttribute);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 文本输入
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task<bool> SetMobileText(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamInputTextModel>(paramStr);
                return await RunWithTimeOutAsync(async ct =>
                {
                    return await Task.Run(() =>
                    {
                        return ResolveMobile(paramModel.sessionId).InputText(paramModel);
                    }, ct);
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(SetMobileText));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.SetMobileText);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 设置剪切板
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task SetMobileClipboard(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamSetClipboardModel>(paramStr);
                await RunWithTimeOutAsync(async ct =>
                {
                    await Task.Run(() =>
                    {
                        ResolveMobile(paramModel.sessionId).SetClipboard(paramModel);
                    }, ct);
                    return true;
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(SetMobileClipboard));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.SetMobileClipboard);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 获取剪切板内容
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task<string> GetMobileClipboard(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamDeviceSessionModel>(paramStr);
                return await RunWithTimeOutAsync(async ct =>
                {
                    return await Task.Run(() =>
                    {
                        return ResolveMobile(paramModel.sessionId).GetFromClipboard();
                    }, ct);
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(GetMobileClipboard));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.GetFromMobileClipboard);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        ///拨打电话
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task Dial(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamDialModel>(paramStr);
                await RunWithTimeOutAsync(async ct =>
                {
                    await Task.Run(() =>
                    {
                        ResolveMobile(paramModel.sessionId).Dial(paramModel);
                    }, ct);
                    return true;
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(Dial));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.Dial);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// CV点击
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task CVClick(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamCvClickModel>(paramStr);
                await RunWithTimeOutAsync(async ct =>
                {
                    await Task.Run(() =>
                    {
                        CVMobileElement cVMobileElement = new CVMobileElement(paramModel.selector);
                        ParamScreenshotModel paramScreenshotModel = new ParamScreenshotModel()
                        {
                            sessionId = paramModel.sessionId,
                        };
                        string screenShot = ResolveMobile(paramModel.sessionId).LoadNewScreenshot();
                        CVHelper cVHelper = new CVHelper(screenShot, paramModel.CVExecutionServiceURL);
                        cVMobileElement.LoadNewBoundRectangle(cVHelper);
                        var point = cVMobileElement.GetElementCenter();
                        paramModel.X = point.X + paramModel.offsetX;
                        paramModel.Y = point.Y + paramModel.offsetY;
                        //logger.LogInformation($"手机CV元素点击具体坐标{paramModel.X}+{paramModel.Y}");
                        ResolveMobile(paramModel.sessionId).ClickScreen(paramModel);
                    }, ct);
                    return true;
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(CVClick));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.MobileClickCV);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// CV点击
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task SetTextCV(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamCvSetTextModel>(paramStr);
                await RunWithTimeOutAsync(async ct =>
                {
                    await Task.Run(() =>
                    {
                        CVMobileElement cVMobileElement = new CVMobileElement(paramModel.selector);
                        ParamScreenshotModel paramScreenshotModel = new ParamScreenshotModel()
                        {
                            sessionId = paramModel.sessionId,
                        };
                        string screenShot = ResolveMobile(paramModel.sessionId).LoadNewScreenshot();
                        CVHelper cVHelper = new CVHelper(screenShot, paramModel.CVExecutionServiceURL);
                        cVMobileElement.LoadNewBoundRectangle(cVHelper);
                        var point = cVMobileElement.GetElementCenter();
                        //logger.LogInformation($"智能设置文本失败点击具体坐标{point.X}+{point.Y}");
                        ParamClickScreenModel paramClickScreenModel = new ParamClickScreenModel
                        {
                            X = point.X,
                            Y = point.Y,
                        };
                        ResolveMobile(paramModel.sessionId).ClickScreen(paramClickScreenModel);
                        AdbHelper.SetText(paramModel.text);
                    }, ct);
                    return true;
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(SetTextCV));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.MobileSetTextCV);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 获取手机连接信息
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task<string> GetConnectDetailInfo(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamDeviceSessionModel>(paramStr);
                return await RunWithTimeOutAsync(async ct =>
                 {
                     return await Task.Run(() =>
                     {
                         return ResolveMobile(paramModel.sessionId).GetConnectDeviceDetail(paramModel);
                     }, ct);
                 }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(GetConnectDetailInfo));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.GetConnectDetailInfo);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <param name="paramStr"></param>
        public static async Task DisconnectMobile(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamDeviceSessionModel>(paramStr);
                await RunWithTimeOutAsync(async ct =>
                {
                    await Task.Run(() =>
                    {
                        ResolveMobile(paramModel.sessionId).Close();
                    }, ct);
                    return true;
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(DisconnectMobile));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.DisconnectMobile);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public static async Task AnalogInputText(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamAnalogInputModel>(paramStr);
                await RunWithTimeOutAsync(async ct =>
                {
                    await Task.Run(() =>
                    {
                        string id = string.IsNullOrEmpty(paramModel.sessionId) ? "" : $"-s {paramModel.sessionId}";
                        AdbHelper.GetLastResultCmd("adb shell settings get secure default_input_method", out string defIme);
                        AdbHelper.SetDefaultIme(id, "com.android.adbkeyboard/.AdbIME");
                        Thread.Sleep(500);
                        AdbHelper.SetText(paramModel.text, id);
                        Thread.Sleep(500);
                        AdbHelper.SetDefaultIme(id, defIme);
                        if (paramModel.isEnter == 1)
                        {
                            AdbHelper.InputKeyEvent(id, keyCode: 66);
                        }
                    }, ct);
                    return true;
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(AnalogInputText), quarantineSessionOnTimeout: false);
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.MobileAnalogInput);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public static bool IsExistElement(string paramStr)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamCheckIsExistElementModel>(paramStr);
                if (paramModel.isCVCapture)
                {
                    CVMobileElement cVMobileElement = new CVMobileElement(paramModel.selector);
                    string screenShot = ResolveMobile(paramModel.sessionId).LoadNewScreenshot();
                    CVHelper cVHelper = new CVHelper(screenShot, paramModel.CVExecutionServiceURL);
                    cVMobileElement.LoadNewBoundRectangle(cVHelper);
                    var point = cVMobileElement.GetElementCenter();
                    if (point.X > 0 || point.Y > 0)
                    {
                        //存在
                        return true;
                    }
                    else
                    {
                        // 不存在
                        return false;
                    }

                }
                else
                {
                    var targetElement = ResolveMobile(paramModel.sessionId).FindElement(paramModel.selector, paramModel);
                    if (targetElement == null)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (RPAException rpaEx)
            {
                throw rpaEx;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public static async Task<bool> IsExistElementAsync(string paramStr, int timeOut = 30000)
        {
            var paramModel = JsonConvert.DeserializeObject<ParamCheckIsExistElementModel>(paramStr);
            try
            {
                return await RunWithTimeOutAsync(
                    ct => Task.Run(() => IsExistElement(paramStr), ct),
                    TimeSpan.FromMilliseconds(timeOut),
                    paramModel.sessionId,
                    nameof(IsExistElement));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.GetMobileElement);
            }
        }
        public static async Task ActivateApp(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamAppInfoModel>(paramStr);
                await RunWithTimeOutAsync(async ct =>
                {
                    await Task.Run(() =>
                    {
                        ResolveMobile(paramModel.sessionId).ActivateApp(paramModel);
                    }, ct);
                    return true;
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(ActivateApp));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.MobileActivateApp);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public static async Task CloseAppAsync(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamAppInfoModel>(paramStr);
                await RunWithTimeOutAsync(async ct =>
                {
                    await Task.Run(() =>
                    {
                        ResolveMobile(paramModel.sessionId).CloseApp(paramModel);
                    }, ct);
                    return true;
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(CloseAppAsync));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.MobileCloseApp);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static async Task<string> ElementShotClick(string paramStr, int timeOut = 30000)
        {
            var paramModel = JsonConvert.DeserializeObject<ParamElementShotClickModel>(paramStr);
            string directoryPath = Path.GetDirectoryName(paramModel.imgSrc);
            string fileName = Guid.NewGuid().ToString() + ".png";
            string tempSourcePath = Path.Combine(directoryPath, fileName);
            try
            {
                return await RunWithTimeOutAsync(async ct =>
                {
                    return await Task.Run(async () =>
                      {
                          int deviceWidth = 0;
                          int deviceHeight = 0;
                          //记得将生成的手机屏幕图片执行完删除（算法需要具体路径，所以要临时生成）
                          var shotImg = ResolveMobile(paramModel.sessionId).GetScreenshot(new ParamScreenshotModel
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
                          if (rectsData != null)
                          {
                              if (rectsData.Count == 1)
                              {
                                  var rectData = rectsData[0];
                                  //先实现点击中点，后续什么类型偏移再待实现
                                  ParamClickScreenByPointModel paramClickScreenModel = new ParamClickScreenByPointModel
                                  {
                                      screenX = (rectData[0][0] + rectData[1][0]) / 2,
                                      screenY = (rectData[0][1] + rectData[1][1]) / 2,
                                      offsetX = paramModel.offsetX,
                                      offsetY = paramModel.offsetY,
                                      isRoundClick = paramModel.isRoundClick,
                                      stayTime = paramModel.stayTime,
                                      clickType = paramModel.clickType
                                  };
                                  for (int i = 0; i < paramModel.clickTimes; i++)
                                  {
                                      ResolveMobile(paramModel.sessionId).ClickScreenByImagePoint(paramClickScreenModel);
                                      if (i != paramModel.clickTimes - 1)
                                      {
                                          await Task.Delay(paramModel.interval * 1000);
                                      }
                                  }
                                  return (new { rects = rectsData, status = 0 }).ToJson();
                              }
                              if (rectsData.Count > 1)
                              {
                                  //返回异常
                                  if (paramModel.moreImageProtype == (int)MoreImageProtype.Exception)
                                  {
                                      throw new Exception("存在多个目标图像区域，图像点击失败!".I18n("controlType_MobileAbilityServer_3672B668"));
                                  }
                                  else if (paramModel.moreImageProtype == (int)MoreImageProtype.ReturnMoreRect)
                                  {
                                      return (new { rects = rectsData, status = 0 }).ToJson();
                                  }
                                  else if (paramModel.moreImageProtype == (int)MoreImageProtype.PauseAndManual)
                                  {
                                      //进行弹框
                                      var daiologResult = "";
                                      MobileHostApplicationContext.SynchronizationContext.Send(_ =>
                                      {
                                          var WarningWnd = new WarningWnd();
                                          MessageResult _MessageResult = null;
                                          WarningWnd.Result += (s, e) =>
                                          {
                                              _MessageResult = e.Result;
                                          };
                                          WarningWnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                          WarningWnd.ShowDialog();
                                          if (_MessageResult.IsYes)
                                          {
                                              daiologResult = (new { status = 0 }).ToJson();
                                          }
                                          else
                                          {
                                              daiologResult = (new { status = 1 }).ToJson();
                                          }
                                      }, null);
                                      return daiologResult;
                                  }
                              }
                          }

                          throw new Exception("图像识别异常".I18n("controlType_MobileAbilityServer_5C540EF7"));

                      }, ct);
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(ElementShotClick));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.ClickMobileElement);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                if (File.Exists(tempSourcePath))
                {
                    try
                    {
                        File.Delete(tempSourcePath);
                    }
                    catch (Exception fileEx)
                    {
                        //能删除就删除,删除失败不报错，有可能算法还在占用
                        _logger.LogWarning($"删除临时生成的手机原图文件异常:{tempSourcePath}", fileEx);
                    }
                }
            }
        }

        public static async Task FileTransfer(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamFileTransmissionModel>(paramStr);
                await RunWithTimeOutAsync(async ct =>
                {
                    await Task.Run(async () =>
                    {
                        await ResolveMobile(paramModel.sessionId).FileTransfer(paramModel);
                    }, ct);
                    return true;
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(FileTransfer), quarantineSessionOnTimeout: false);
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.MobileActivateApp);
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public static async Task<TResult> RunWithTimeOutAsync<TResult>(
            Func<CancellationToken, Task<TResult>> taskFunc,
            TimeSpan timeout,
            string sessionId = null,
            string operationName = null,
            bool quarantineSessionOnTimeout = true,
            Func<MobileSession> timedOutSessionResolver = null,
            bool isDesignTimeReadOnly = false)
        {
            if (taskFunc == null)
            {
                throw new ArgumentNullException(nameof(taskFunc));
            }

            using (var operationCancellation = new CancellationTokenSource())
            {
                string effectiveOperationName = string.IsNullOrWhiteSpace(operationName) ? "MobileOperation" : operationName;
                Task<TResult> operationTask;
                if (quarantineSessionOnTimeout && !string.IsNullOrWhiteSpace(sessionId))
                {
                    using (MobileSessionCache.BeginOperationGenerationScope(sessionId))
                    {
                        operationTask = StartWithOperationLease(sessionId, effectiveOperationName, taskFunc, operationCancellation.Token, isDesignTimeReadOnly);
                    }
                }
                else
                {
                    operationTask = StartWithOperationLease(sessionId, effectiveOperationName, taskFunc, operationCancellation.Token, isDesignTimeReadOnly);
                }
                Task timeoutTask = Task.Delay(timeout, operationCancellation.Token);
                Task completedTask = await Task.WhenAny(operationTask, timeoutTask);
                if (completedTask == operationTask)
                {
                    operationCancellation.Cancel();
                    try
                    {
                        return await operationTask;
                    }
                    catch (Exception ex)
                    {
                        if (quarantineSessionOnTimeout)
                        {
                            MobileSession failedSession = timedOutSessionResolver?.Invoke();
                            if (failedSession != null)
                            {
                                failedSession.MarkOperationFailed(effectiveOperationName, ex);
                            }
                            else if (!isDesignTimeReadOnly)
                            {
                                MobileSessionCache.MarkOperationFailed(sessionId, effectiveOperationName, ex);
                            }
                        }
                        throw;
                    }
                }

                operationCancellation.Cancel();
                MobileSession timedOutSession = null;
                if (quarantineSessionOnTimeout)
                {
                    if (!isDesignTimeReadOnly) MobileSessionCache.InvalidateOperationGeneration(sessionId);
                    timedOutSession = timedOutSessionResolver?.Invoke();
                    if (timedOutSession != null)
                    {
                        timedOutSession.MarkOperationTimedOut(effectiveOperationName, isReadOnly: isDesignTimeReadOnly);
                    }
                    else if (!isDesignTimeReadOnly)
                    {
                        timedOutSession = MobileSessionCache.MarkOperationTimedOut(sessionId, effectiveOperationName);
                    }
                }
                _ = ObserveTimedOutOperationAsync(operationTask, timedOutSession, effectiveOperationName, isDesignTimeReadOnly);
                _logger.LogError($"手机操作超过时限[{timeout.TotalMilliseconds}ms]，操作[{effectiveOperationName}]，设备[{sessionId}]");
                throw new TimeoutException();
            }
        }

        private static Task<TResult> StartWithOperationLease<TResult>(
            string udid,
            string operationName,
            Func<CancellationToken, Task<TResult>> taskFunc,
            CancellationToken cancellationToken,
            bool isDesignTimeReadOnly)
        {
            IDisposable lease = string.IsNullOrWhiteSpace(udid) ? null
                : MobileAutomationStateStore.BeginOperationLease(udid, operationName, isDesignTimeReadOnly);
            try
            {
                return CompleteWithOperationLeaseAsync(taskFunc(cancellationToken), lease);
            }
            catch
            {
                lease?.Dispose();
                throw;
            }
        }

        private static async Task<TResult> CompleteWithOperationLeaseAsync<TResult>(Task<TResult> task, IDisposable lease)
        {
            try { return await task; }
            finally { lease?.Dispose(); }
        }
        private static async Task ObserveTimedOutOperationAsync<TResult>(
            Task<TResult> operationTask,
            MobileSession timedOutSession,
            string operationName,
            bool isDesignTimeReadOnly)
        {
            Exception completionException = null;
            try
            {
                await operationTask;
            }
            catch (Exception ex)
            {
                completionException = ex;
            }
            finally
            {
                timedOutSession?.MarkTimedOutOperationCompleted(operationName, completionException, isReadOnly: isDesignTimeReadOnly);
            }
        }


        #region 手机管理提供
        /// <summary>
        /// 截屏
        /// </summary>
        /// <param name="paramStr"></param>
        public static byte[] GetScreenshotbyte(string udid)
        {
            return GetScreenshotbyteCore(ResolveMobile(udid));
        }

        public static Task<byte[]> GetScreenshotbyteAsync(string udid, int timeOut = DefaultSessionOperationTimeoutMilliseconds, bool isDesignTimeReadOnly = false)
        {
            return RunSessionOperationWithTimeoutAsync(
                udid,
                nameof(GetScreenshotbyte),
                timeOut,
                mobileSession => GetScreenshotbyteCore(mobileSession), isDesignTimeReadOnly);
        }

        private static byte[] GetScreenshotbyteCore(MobileSession mobileSession)
        {
            return mobileSession.GetScreenShotbyte();
        }

        /// <summary>
        /// 截屏
        /// </summary>
        public static string GetScreenshotBaseString(string udid)
        {
            return GetScreenshotBaseStringCore(ResolveMobile(udid));
        }

        public static Task<string> GetScreenshotBaseStringAsync(string udid, int timeOut = DefaultSessionOperationTimeoutMilliseconds, bool isDesignTimeReadOnly = false)
        {
            return RunSessionOperationWithTimeoutAsync(
                udid,
                nameof(GetScreenshotBaseString),
                timeOut,
                mobileSession => GetScreenshotBaseStringCore(mobileSession), isDesignTimeReadOnly);
        }

        private static string GetScreenshotBaseStringCore(MobileSession mobileSession)
        {
            return mobileSession.GetScreenShotBaseString();
        }

        /// <summary>
        /// 获取元素树结构
        /// </summary>
        public static string GetPageSource(string udid)
        {
            return ResolveMobile(udid).GetPageSource();
        }

        public static async Task<string> GetPageSourceAsync(string udid, int timeOut = PageSourceTimeoutMilliseconds, bool isDesignTime = false)
        {
            if (isDesignTime)
            {
                MobilePageSourceResult result = await DesignTimePageSourceService.GetPageSourceAsync(udid);
                return result.PageSource;
            }
            try
            {
                return await RunSessionOperationWithTimeoutAsync(
                    udid,
                    nameof(GetPageSource),
                    timeOut,
                    mobileSession => mobileSession.GetPageSource());
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.GetMobileUiTree);
            }
        }

        public static string GetElementsRect(string udid, bool isSimilar, string selecter)
        {
            return GetElementsRectCore(ResolveMobile(udid), isSimilar, selecter);
        }

        public static Task<string> GetElementsRectAsync(
            string udid,
            bool isSimilar,
            string selecter,
            int timeOut = DefaultSessionOperationTimeoutMilliseconds,
            bool isDesignTimeReadOnly = false)
        {
            return RunSessionOperationWithTimeoutAsync(
                udid,
                nameof(GetElementsRect),
                timeOut,
                mobileSession => GetElementsRectCore(mobileSession, isSimilar, selecter), isDesignTimeReadOnly);
        }

        private static string GetElementsRectCore(MobileSession mobileSession, bool isSimilar, string selecter)
        {
            var eleList = new List<ElementRect>();
            if (isSimilar)
            {
                var elements = mobileSession.FindSimilarElement(selecter);
                foreach (var item in elements)
                {
                    var rectEle = item.Rect;
                    eleList.Add(new ElementRect
                    {
                        X = rectEle.Left,
                        Y = rectEle.Top,
                        Height = rectEle.Height,
                        Width = rectEle.Width
                    });
                }
            }
            else
            {
                var element = mobileSession.FindElement(selecter);
                var rectEle = element.Rect;
                eleList.Add(new ElementRect
                {
                    X = rectEle.Left,
                    Y = rectEle.Top,
                    Height = rectEle.Height,
                    Width = rectEle.Width
                });
            }
            return JsonConvert.SerializeObject(eleList);
        }

        public static string CheckElement(string udid, string selecter, string firstSelecter = "", int isSimilar = 0)
        {
            return CheckElementCore(ResolveMobile(udid), selecter, firstSelecter, isSimilar);
        }

        public static Task<string> CheckElementAsync(
            string udid,
            string selecter,
            string firstSelecter = "",
            int isSimilar = 0,
            int timeOut = DefaultSessionOperationTimeoutMilliseconds,
            bool isDesignTimeReadOnly = false)
        {
            return RunSessionOperationWithTimeoutAsync(
                udid,
                nameof(CheckElement),
                timeOut,
                mobileSession => CheckElementCore(mobileSession, selecter, firstSelecter, isSimilar), isDesignTimeReadOnly);
        }

        private static string CheckElementCore(
            MobileSession mobileSession,
            string selecter,
            string firstSelecter,
            int isSimilar)
        {
            string buildSelecter = "";
            if (isSimilar == 0)
            {
                var element = mobileSession.CheckElement(selecter, out buildSelecter);
                if (element != null)
                {
                    return buildSelecter;
                }
            }
            else
            {
                var element = mobileSession.CheckSimilarElement(selecter, firstSelecter, out buildSelecter);
                if (element != null)
                {
                    return buildSelecter;
                }
            }
            return buildSelecter;
        }

        private static Task<TResult> RunSessionOperationWithTimeoutAsync<TResult>(
            string udid,
            string operationName,
            int timeOut,
            Func<MobileSession, TResult> action,
            bool isDesignTimeReadOnly = false)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            var sessionHolder = new StrongBox<MobileSession>();
            int effectiveTimeout = timeOut > 0 ? timeOut : DefaultSessionOperationTimeoutMilliseconds;
            return RunWithTimeOutAsync(
                ct => Task.Run(() =>
                {
                    // 设计期只读请求不负责重建；会话异常由Source降级或显式连接统一处理。
                    MobileSession mobileSession = isDesignTimeReadOnly
                        ? MobileSessionCache.GetValidSession(udid, force: false, autoReConnect: false)
                        : ResolveMobile(udid);
                    if (mobileSession == null)
                        throw new InvalidOperationException($"当前会话不可用于设计期只读操作[{operationName}]，设备[{udid}]");
                    Volatile.Write(ref sessionHolder.Value, mobileSession);
                    return action(mobileSession);
                }, ct),
                TimeSpan.FromMilliseconds(effectiveTimeout),
                udid,
                operationName,
                quarantineSessionOnTimeout: true,
                timedOutSessionResolver: () => Volatile.Read(ref sessionHolder.Value),
                isDesignTimeReadOnly: isDesignTimeReadOnly);
        }

        public static async Task<string> GetMobileUiTree(string paramStr, int timeOut = 30000)
        {
            try
            {
                var paramModel = JsonConvert.DeserializeObject<ParamGetMobileUiTreeModel>(paramStr);
                return await RunWithTimeOutAsync(async ct =>
                {
                    return await Task.Run(() =>
                    {
                        string pageSource = ResolveMobile(paramModel.sessionId).GetPageSource();
                        string filePath = "";
                        if (paramModel.iSSaveAsFile)
                        {
                            string fileName = paramModel.fileName;
                            if (!string.IsNullOrEmpty(paramModel.savePath) && !string.IsNullOrEmpty(fileName))
                            {
                                if (!string.IsNullOrEmpty(Path.GetExtension(fileName)))
                                {
                                    fileName = Path.ChangeExtension(fileName, "xml");
                                }
                                else
                                {
                                    fileName = $"{fileName}.xml";
                                }
                            }
                            else
                            {
                                //文件名为空则参数异常
                                throw new Exception("Parameter exception !");
                            }
                            string targetFilePath = Path.Combine(paramModel.savePath, fileName);
                            if (File.Exists(targetFilePath))
                            {
                                if (!paramModel.iSCoverSameFile)
                                {
                                    fileName = Localize($"{Path.ChangeExtension(fileName, null)}(副本).xml", "MobileAbilityServer_F6BA6DB3");
                                    targetFilePath = Path.Combine(paramModel.savePath, fileName);
                                }
                            }
                            if (!Directory.Exists(paramModel.savePath))
                            {
                                Directory.CreateDirectory(paramModel.savePath);
                            }
                            filePath = targetFilePath;
                            File.WriteAllText(targetFilePath, pageSource);
                        }
                        return (new { uiTree = pageSource, filePath = filePath }).ToJson();
                    }, ct);
                }, TimeSpan.FromMilliseconds(timeOut), paramModel.sessionId, nameof(GetMobileUiTree));
            }
            catch (TimeoutException)
            {
                throw new RPAException(eErrorMessageType.Error_PluginRun_TimeOut, eComponentType.GetMobileUiTree);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion


        #region 方法
        public static string ParseSimilarListToJson(List<List<MobileItem>> items, string sourceSelector = null)
        {
            bool useCurrentRules = MobileSelectorRules.UsesCurrentRules(sourceSelector);
            List<RPACommon> retRpa = new List<RPACommon>();
            foreach (var item in items)
            {
                RPACommon rPACommon = new RPACommon();
                rPACommon.CommonProps = new CommonProps
                {
                    GUIType = eGuiType.Mobile.ToString(),
                    filePath = "",
                    captureMode = 1
                };
                if (useCurrentRules)
                    rPACommon.CommonProps.Version = MobileSelectorRules.CurrentVersion;
                rPACommon.ElementDetail = new ElementDetail
                {
                    ElementName = ""
                };
                JArray jArray = JArray.Parse(JsonHelper.ToJson(item));
                rPACommon.Selector = jArray;
                retRpa.Add(rPACommon);
            }
            return JsonConvert.SerializeObject(retRpa);
        }
        #endregion
        #region Class
        public class ElementRect
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
        }
        #endregion
    }
}
