
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using Google.Protobuf;
using Grpc.Core;
using II.RPA.Logging;
using II.RPA.MobileProvider.Params;
using II.RPA.MobileProvider.Provider;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Rpa.Core.Mobileplugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace II.RPA.MobileProvider.Service
{
    public class MobilePluginGRPCService : MobilePluginService.MobilePluginServiceBase
    {
        private readonly static ILogger _logger = Logger.OfContext();
        public override async Task<MobileResponse> PluginActionInvokeUnary(MobileRequest request, ServerCallContext context)
        {
            string actionName = request.Action;
            _logger.LogInformation($"手机组件{actionName}入参：{request.InParameter}");
            string retInfo = "";
            try
            {
                switch (actionName)
                {
                    case "Mobile.ConnectMobile":
                        try
                        {
                            var paramM = JsonConvert.DeserializeObject<ParamDeviceInfoModel>(request.InParameter);
                            var mobileInfo = new MobileInfo
                            {
                                Udid = paramM.deviceName,
                                PlatformName = paramM.platformName,
                                PlatformVersion = paramM.platformVersion
                            };
                            string sessionId = await MobileServer.CreateConnectSession(mobileInfo, request.TimeOut);
                            return new MobileResponse
                            {
                                Code = "0",
                                Message = "连接成功".I18n("MobilePluginGRPCService_3AB0749"),
                                Info = sessionId
                            };
                        }
                        catch (RPAException ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"{actionName}");
                            return new MobileResponse
                            {
                                Code = ex.ErrorCode.ToString(),
                                Message = ex.ErrorMessage
                            };
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"{actionName}");
                            return new MobileResponse
                            {
                                Code = "501",
                                Message = ErrorMessageConfig.GetMessage(eErrorMessageType.Error_MobileConnect)
                            };
                        }
                    case "Mobile.ConnectMobile_V2":
                        var connectInfo = JsonConvert.DeserializeObject<ParamConnectInfo>(request.InParameter);
                        List<string> connectSuccessIdArry = new List<string>();
                        List<string> connectFailIdArry = new List<string>();
                        //手机组件超时目前只添加连接手机组件，其他暂时不考虑，设计器组件层面有些有有些没有，后续和产品沟通怎么统一
                        //using (var timeoutCancellationTokenSource = new CancellationTokenSource())
                        //{
                        //    var delayTask = Task.Delay(request.TimeOut, timeoutCancellationTokenSource.Token);
                        //    var task = Task.Factory.StartNew(() =>
                        //    {
                        var connectType = connectInfo.connectType;
                        var devices = connectInfo.devices;
                        if (connectType == ConnectType.Refer || connectType == ConnectType.ManualSelect)
                        {
                            foreach (var device in devices)
                            {
                                MobileAutomationStateStore.AcquireRuntimeOwnership(device.deviceName);
                                _logger.LogInformation($"Runtime手机使用权开始接管，设备[{device.deviceName}]");
                                try
                                {
                                    var mobileInfo = new MobileInfo
                                    {
                                        Udid = device.deviceName,
                                        PlatformName = device.platformName,
                                        PlatformVersion = device.platformVersion,
                                        IsAutoUnlock = connectInfo.isAutoUnlock,
                                        UnlockType = connectInfo.unlockType,
                                        UnlockPw = connectInfo.unlockPw
                                    };
                                    string udid = await MobileServer.CreateConnectSession(mobileInfo, request.TimeOut);
                                    connectSuccessIdArry.Add(udid);
                                }
                                catch (Exception ex)
                                {
                                    MobileAutomationStateStore.ReleaseRuntimeOwnership(device.deviceName);
                                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"{actionName}:连接失败的手机:{device.deviceName}");
                                    connectFailIdArry.Add(device.deviceName);
                                }
                            }
                        }
                        else if (connectType == ConnectType.AnyOne)
                        {
                            List<DeviceInfo> mobileList = new List<DeviceInfo>();
                            foreach (var item in connectInfo.devices)
                            {
                                mobileList.Add(new DeviceInfo { Udid = item.deviceName });
                            }
                            var device = DeviceManager.GetAnyOneReadyDevice(mobileList);
                            if (device != null)
                            {
                                MobileAutomationStateStore.AcquireRuntimeOwnership(device.Udid);
                                _logger.LogInformation($"Runtime手机使用权开始接管，设备[{device.Udid}]");
                                try
                                {
                                    var mobileInfo = new MobileInfo
                                    {
                                        Udid = device.Udid,
                                        PlatformName = device.PlatformName,
                                        PlatformVersion = device.Version,
                                        IsAutoUnlock = connectInfo.isAutoUnlock,
                                        UnlockType = connectInfo.unlockType,
                                        UnlockPw = connectInfo.unlockPw
                                    };
                                    string udid = await MobileServer.CreateConnectSession(mobileInfo, request.TimeOut);
                                    connectSuccessIdArry.Add(udid);
                                }
                                catch (Exception ex)
                                {
                                    MobileAutomationStateStore.ReleaseRuntimeOwnership(device.Udid);
                                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"{actionName}:连接失败的手机:{device.Udid}");
                                    connectFailIdArry.Add(device.Udid);
                                }
                            }
                        }
                        else if (connectType == ConnectType.All)
                        {
                            List<DeviceInfo> mobileList = new List<DeviceInfo>();
                            foreach (var item in connectInfo.devices)
                            {
                                mobileList.Add(new DeviceInfo { Udid = item.deviceName });
                            }
                            var readyDevices = DeviceManager.GetAllReadyDevice(mobileList);
                            foreach (var device in readyDevices)
                            {
                                MobileAutomationStateStore.AcquireRuntimeOwnership(device.Udid);
                                _logger.LogInformation($"Runtime手机使用权开始接管，设备[{device.Udid}]");
                                try
                                {
                                    var mobileInfo = new MobileInfo
                                    {
                                        Udid = device.Udid,
                                        PlatformName = device.PlatformName,
                                        PlatformVersion = device.Version,
                                        IsAutoUnlock = connectInfo.isAutoUnlock,
                                        UnlockType = connectInfo.unlockType,
                                        UnlockPw = connectInfo.unlockPw
                                    };
                                    string udid = await MobileServer.CreateConnectSession(mobileInfo, request.TimeOut);
                                    connectSuccessIdArry.Add(udid);
                                }
                                catch (Exception ex)
                                {
                                    MobileAutomationStateStore.ReleaseRuntimeOwnership(device.Udid);
                                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"{actionName}:连接失败的手机:{device.Udid}");
                                    connectFailIdArry.Add(device.Udid);
                                }
                            }
                        }
                        var retResp = new MobileResponse
                        {
                            Info = new List<List<string>>() { connectSuccessIdArry, connectFailIdArry }.ToJson()
                        };
                        if (connectSuccessIdArry.Count < 1)
                        {
                            _logger.LogError($"未连接成功目标手机");
                            retResp.Code = "1";
                            retResp.Message = "连接失败".I18n("MobilePluginGRPCService_C26CBAEC");
                        }
                        else
                        {
                            retResp.Code = "0";
                            retResp.Message = "连接成功".I18n("MobilePluginGRPCService_3AB0749");
                        }
                        _logger.LogInformation($"{actionName} return:{retResp.ToJson()}");
                        return retResp;
                    //    });
                    //    if (await Task.WhenAny(task, delayTask) == task)
                    //    {
                    //        timeoutCancellationTokenSource.Cancel();
                    //        return await task;
                    //    }
                    //    else
                    //    {
                    //        return await Task.FromResult(new MobileResponse
                    //        {
                    //            Code = "500",
                    //            Message = ErrorMessageConfig.GetMessage(eErrorMessageType.Error_PluginRun_TimeOut)
                    //        });
                    //    }
                    //}
                    case "Mobile.GetSimilarElementList":
                        retInfo = await MobileAbilityServer.GetSimilarElementList(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "拾取成功".I18n("MobilePluginGRPCService_7654EBE6"),
                            Info = retInfo
                        });
                    case "Mobile.ClickMobileScreen":
                        retInfo = "{}";
                        await MobileAbilityServer.ClickScreen(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "点击成功".I18n("MobilePluginGRPCService_351A880E"),
                            Info = retInfo
                        });
                    case "Mobile.TouchSlideMobile":
                        retInfo = "{}";
                        await MobileAbilityServer.TouchSlid(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.GetMobileScreenshot":
                        retInfo = "{}";
                        retInfo = await MobileAbilityServer.GetScreenshot(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.ClickMobileElement":
                        retInfo = "{}";
                        await MobileAbilityServer.ClickElement(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.GetMobileElementScreenshot":
                        retInfo = "{}";
                        retInfo = await MobileAbilityServer.GetElementScreenshot(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.PressMobileKey":
                        retInfo = "{}";
                        await MobileAbilityServer.PressMobileKey(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.GetOrientation":
                        retInfo = "{}";
                        retInfo = await MobileAbilityServer.GetOrientation(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.SetOrientation":
                        retInfo = "{}";
                        await MobileAbilityServer.SetOrientation(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.GetMobileElementChild":
                        retInfo = "{}";
                        retInfo = await MobileAbilityServer.GetMobileElementChild(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.GetMobileElementText":
                        retInfo = "{}";
                        retInfo = await MobileAbilityServer.GetMobileElementText(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.GetMobileElementAttribute":
                        retInfo = "{}";
                        retInfo = await MobileAbilityServer.GetMobileElementAttribute(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.SetMobileText":
                        retInfo = "{}";
                        var verify = await MobileAbilityServer.SetMobileText(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = (new { isVerify = verify }).ToJson()
                        });
                    case "Mobile.SetMobileClipboard":
                        retInfo = "{}";
                        await MobileAbilityServer.SetMobileClipboard(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.GetFromMobileClipboard":
                        retInfo = "{}";
                        retInfo = await MobileAbilityServer.GetMobileClipboard(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.Dial":
                        retInfo = "{}";
                        await MobileAbilityServer.Dial(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.CVClick":
                        retInfo = "{}";
                        await MobileAbilityServer.CVClick(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.SetTextCV":
                        retInfo = "{}";
                        await MobileAbilityServer.SetTextCV(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.GetConnectDetailInfo":
                        retInfo = "{}";
                        retInfo = await MobileAbilityServer.GetConnectDetailInfo(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.DisconnectMobile":
                        retInfo = "{}";
                        ParamDeviceSessionModel disconnectModel = JsonConvert.DeserializeObject<ParamDeviceSessionModel>(request.InParameter);
                        string disconnectUdid = disconnectModel?.sessionId;
                        _logger.LogInformation($"发起关闭客户端类型为:{CommandParamsHelper.ClientType.ToString()}，设备[{disconnectUdid}]");
                        try
                        {
                            if (CommandParamsHelper.ClientType == ClientType.Bot)
                            {
                                await MobileAbilityServer.DisconnectMobile(request.InParameter, request.TimeOut);
                            }
                            return await Task.FromResult(new MobileResponse
                            {
                                Code = "0",
                                Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                                Info = retInfo
                            });
                        }
                        finally
                        {
                            MobileAutomationStateStore.ReleaseRuntimeOwnership(disconnectUdid);
                            _logger.LogInformation($"Runtime手机使用权已释放，设备[{disconnectUdid}]");
                        }
                    case "Mobile.AnalogInput":
                        retInfo = "{}";
                        await MobileAbilityServer.AnalogInputText(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.IsElementExist":
                        var isExist = await MobileAbilityServer.IsExistElementAsync(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = isExist ? "1" : "0"
                        });
                    case "Mobile.ActivateApp":
                        retInfo = "{}";
                        await MobileAbilityServer.ActivateApp(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.CloseApp":
                        retInfo = "{}";
                        _logger.LogInformation($"超时时间：{request.TimeOut}ms");
                        await MobileAbilityServer.CloseAppAsync(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.GetElement":
                        retInfo = "{}";
                        retInfo = await MobileAbilityServer.GetElement(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.GetMobileUiTree":
                        retInfo = "{}";
                        retInfo = await MobileAbilityServer.GetMobileUiTree(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.ElementShotClick":
                        retInfo = "{}";
                        retInfo = await MobileAbilityServer.ElementShotClick(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.FileTransfer":
                        retInfo = "{}";
                        await MobileAbilityServer.FileTransfer(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.TouchSlideMobile_V2":
                        retInfo = "{}";
                        await MobileAbilityServer.TouchSlid_V2(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    case "Mobile.TouchSlideMobile_V3":
                        retInfo = "{}";
                        await MobileAbilityServer.TouchSlid_V3(request.InParameter, request.TimeOut);
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = retInfo
                        });
                    default:
                        return await Task.FromResult(new MobileResponse
                        {
                            Code = "0",
                            Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                            Info = "{}"
                        });
                }
            }
            catch (RPAException ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"{actionName}");
                return await Task.FromResult(new MobileResponse
                {
                    Code = ex.ErrorCode.ToString(),
                    Message = ex.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"{actionName}");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "500",
                    Message = ex.Message ?? "未知异常"
                });
            }
        }

        public override async Task<MobileResponse> StartAppiumServerUnary(MobileRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"开启手机appium服务：StartAppiumServer");
                //打开手机appium服务，此处目前为插件安装完成后打开
                MobileServerManager mobileServerManager = new MobileServerManager();
                mobileServerManager.InitMobileServer();
                string appiumServerPort = GlobleConfig.AppiumServerPort;
                return await Task.FromResult(new MobileResponse
                {
                    Code = "0",
                    Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                    Info = appiumServerPort
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"StartAppiumServerUnary");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = ex.Message
                });
            }
        }
        public override Task<MobileResponse> MobileEnterDesignTimeUnary(MobileRequest request, ServerCallContext context)
        {
            try
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(request.Udid))
                    throw new ArgumentException("恢复设计期刷新时设备标识不能为空");
                if (!MobileAutomationStateStore.TryEnterDesignTime(request.Udid))
                    throw new InvalidOperationException($"Runtime手机操作或会话清理仍在执行，暂不能切换到设计期，设备[{request.Udid}]");

                _logger.LogInformation($"手机请求切换为设计期使用，设备[{request.Udid}]");
                return Task.FromResult(new MobileResponse
                {
                    Code = "0",
                    Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69")
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "MobileEnterDesignTimeUnary");
                return Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = ex.Message
                });
            }
        }

        public override async Task<MobileResponse> MobileConnectUnary(MobileRequest request, ServerCallContext context)
        {
            try
            {
                if (!MobileAutomationStateStore.TryEnterDesignTime(request.Udid))
                {
                    MobileAutomationStateSnapshot state = MobileAutomationStateStore.GetSnapshot(request.Udid);
                    _logger.LogWarning($"Design-time connection deferred: Mode[{state.Mode}], RuntimeOwned[{state.IsRuntimeOwned}], RuntimeLease[{state.RuntimeLeaseCount}], ConnectionRecovering[{state.IsConnectionRecovering}], SessionGeneration[{state.SessionGeneration}], Device[{request.Udid}]");
                    throw new InvalidOperationException($"Runtime手机操作或会话清理仍在执行，暂不能切换到设计期，设备[{request.Udid}]");
                }
                _logger.LogInformation($"手机连接入口切换为设计期使用，设备[{request.Udid}]");
                var mobileInfo = new MobileInfo
                {
                    Udid = request.Udid,
                    PlatformName = request.PlatformName,
                    PlatformVersion = request.PlatformVersion,
                    ForceNewSession = string.Equals(
                        request.InParameter,
                        "ForceReconnect",
                        StringComparison.OrdinalIgnoreCase)
                };
                string sessionId = await MobileServer.CreateConnectSession(mobileInfo, request.TimeOut);
                return await Task.FromResult(new MobileResponse
                {
                    Code = "0",
                    Message = "连接成功".I18n("MobilePluginGRPCService_3AB0749"),
                    Info = sessionId
                });
            }
            catch (RPAException ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"MobileConnectUnary");
                return await Task.FromResult(new MobileResponse
                {
                    Code = ex.ErrorCode.ToString(),
                    Message = ex.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"MobileConnectUnary");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = ex.Message
                });
            }
        }
        public override async Task<MobileResponse> MobileCheckElementUnary(MobileRequest request, ServerCallContext context)
        {
            try
            {
                var retInfo = await MobileAbilityServer.CheckElementAsync(
                    request.Udid,
                    request.Selecter,
                    request.OriginSelecter,
                    request.IsSimilar,
                    request.TimeOut,
                    isDesignTimeReadOnly: true);
                return await Task.FromResult(new MobileResponse
                {
                    Code = "0",
                    Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                    Info = retInfo
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"MobileCheckElementUnary");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = ex.Message
                });
            }
        }
        public override async Task<MobileResponse> MobilePageSourceGetUnary(MobileRequest request, ServerCallContext context)
        {
            try
            {
                bool isDesignTime = string.Equals(request.Action, "DesignTimePageSource", StringComparison.Ordinal);
                var retInfo = await MobileAbilityServer.GetPageSourceAsync(request.Udid, isDesignTime: isDesignTime);
                return await Task.FromResult(new MobileResponse
                {
                    Code = "0",
                    Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                    Info = retInfo
                });
            }
            catch (DesignTimePageSourceDeferredException ex)
            {
                _logger.LogDebug(ex.Message);
                return await Task.FromResult(new MobileResponse
                {
                    Code = "3",
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                //刷节点异常不作为手机端开异常处理，只做记录
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"MobilePageSourceGetUnary");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "2",
                    Message = ex.Message
                });
            }
        }
        public override async Task<MobileShotbyteResponse> MobileShotbyteGetUnary(MobileRequest request, ServerCallContext context)
        {

            try
            {
                var retInfo = await MobileAbilityServer.GetScreenshotbyteAsync(request.Udid, request.TimeOut, isDesignTimeReadOnly: true);
                return await Task.FromResult(new MobileShotbyteResponse
                {
                    Code = "0",
                    Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                    ShotData = ByteString.CopyFrom(retInfo)
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"MobileShotbyteGetUnary");
                return await Task.FromResult(new MobileShotbyteResponse
                {
                    Code = "1",
                    Message = ex.Message
                });
            }
        }
        public override async Task<MobileShotBaseStringResponse> MobileShotBaseStringGetUnary(MobileRequest request, ServerCallContext context)
        {
            try
            {
                var retInfo = await MobileAbilityServer.GetScreenshotBaseStringAsync(request.Udid, request.TimeOut, isDesignTimeReadOnly: true);
                return await Task.FromResult(new MobileShotBaseStringResponse
                {
                    Code = "0",
                    Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                    ShotBasestring = retInfo
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"MobileShotBaseStringGetUnary");
                return await Task.FromResult(new MobileShotBaseStringResponse
                {
                    Code = "1",
                    Message = ex.Message
                });
            }
        }
        public override async Task<MobileResponse> MobileElementsRectUnary(MobileRequest request, ServerCallContext context)
        {
            try
            {
                bool isSimilar = request.Action == "1" ? true : false;
                var retInfo = await MobileAbilityServer.GetElementsRectAsync(request.Udid, isSimilar, request.Selecter, request.TimeOut, isDesignTimeReadOnly: true);
                return await Task.FromResult(new MobileResponse
                {
                    Code = "0",
                    Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                    Info = retInfo
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"MobileElementsRectUnary");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = ex.Message
                });
            }
        }
        public override async Task<MobileResponse> MobilePressKeyUnary(MobileRequest request, ServerCallContext context)
        {
            try
            {
                var data = new
                {
                    sessionId = request.Udid,
                    key = int.Parse(request.Action)
                };
                await MobileAbilityServer.PressMobileKey(data.ToJson(), request.TimeOut);
                return await Task.FromResult(new MobileResponse
                {
                    Code = "0",
                    Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                    Info = "{}"
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"MobilePressKeyUnary");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = ex.Message
                });
            }
        }
        public override async Task<MobileResponse> CheckServiceHeartbeatUnary(MobileRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"CheckServiceHeartbeatUnarying...");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "0",
                    Message = "服务状态正常".I18n("MobilePluginGRPCService_C8F31A69"),
                    Info = "{}"
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"CheckServiceHeartbeatUnary");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = ex.Message
                });
            }
        }
        public override async Task<MobileResponse> MobileTestSlidUnary(MobileRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"测试滑动组件入参：{request.InParameter}");
                ParamTouchSlidModel_TestSild param = JsonConvert.DeserializeObject<ParamTouchSlidModel_TestSild>(request.InParameter);
                var data = new ParamTouchSlidModel_V2
                {
                    sessionId = request.Udid,
                    slidType = EmSlidType.Location,
                    startType = 2,
                    pressDrag = false,
                    slidRects = new List<SlidRect>
                    {
                        new SlidRect
                        {
                            startX = param.startX,
                            startY = param.startY,
                            endX = param.endX,
                            endY = param.endY,
                            slidTime = param.slidTime * 1000,
                            waitAfterSlidTime = param.waitAfterSlidTime * 1000
                        }
                    }
                };
                int timeout = request.TimeOut > 0 ? request.TimeOut : 30000;
                await MobileAbilityServer.TouchSlid_V2(data.ToJson(), timeout);

                return new MobileResponse
                {
                    Code = "0",
                    Message = "操作成功".I18n("MobilePluginGRPCService_55D98C69"),
                    Info = "{}"
                };
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"MobileTestSlidUnary");
                return new MobileResponse
                {
                    Code = "1",
                    Message = ex.Message
                };
            }
        }
    }
}

