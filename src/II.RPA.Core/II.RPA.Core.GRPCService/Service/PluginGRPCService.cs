using CommonServiceLocator;
using FlaUI.Core.Input;
using Grpc.Core;
using Grpc.Core.Interceptors;
using II.RPA.Core.Contracts.Talker.Engine.Message;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.GRPCService.Extension;
using II.RPA.Core.GRPCService.Util;
using II.RPA.Core.Log;
using II.RPA.Core.Models;
using II.RPA.Core.Provider.PluginInterfere;
using II.RPA.Core.Provider.PluginLoaderProvider.ClientAction;
using II.RPA.Core.Provider.PluginProvider;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rpa.Core.Plugin;
using Rpa.Core.Worker.Zbrain;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Google.Protobuf.WireFormat;

namespace II.RPA.Core.GRPCService.Service
{
    public class PluginGRPCService : PluginService.PluginServiceBase, IGRPCService
    {
        private const string OK_VALUE = "OK";

        private readonly Interceptor m_ServicePollyInterceptor;
        /// <summary>
        /// service类中基本不需要输出日志、都在拦截器中做日志记录
        /// </summary>
        private readonly ILogger m_Logger;
        private readonly ClientActionProviderFactory m_ProviderFactory;
        private readonly IRuntimeHostController m_RuntimeHostController;
        private readonly IMobileProviderController m_MobileProviderController;
        private readonly IRuntimeStateAccessor m_RuntimeStateAccessor;
        private readonly IWorkerControlController m_WorkerControlController;
        private readonly ScreenSaver screenSaver;
        private readonly MobileDeviceManagerGRPCService m_MobileDeviceManagerGRPCService;
        private bool _enableProcessRepairControl;
        private PluginService.PluginServiceClient _pluginServiceClient;
        private GrpcClient _childPluginGrpcClient;
        private int _childPluginClientPort;
        private const int ChildSwitchPythonEnvTimeoutSeconds = 10;

        public PluginGRPCService(ILogger aLogger, Interceptor anInterceptor, ClientActionProviderFactory aClientActionProviderFactory, IRuntimeHostController aRuntimeHostController, IMobileProviderController aMobileProviderController, IRuntimeStateAccessor aRuntimeStateAccessor, IWorkerControlController aWorkerControlController, MobileDeviceManagerGRPCService aMobileDeviceManagerGRPCService)
        {
            m_Logger = aLogger;
            screenSaver = new ScreenSaver(m_Logger);
            m_ServicePollyInterceptor = anInterceptor;
            m_ProviderFactory = aClientActionProviderFactory;
            m_RuntimeHostController = aRuntimeHostController;
            m_MobileProviderController = aMobileProviderController;
            m_RuntimeStateAccessor = aRuntimeStateAccessor;
            m_WorkerControlController = aWorkerControlController;
            m_MobileDeviceManagerGRPCService = aMobileDeviceManagerGRPCService;
        }

        public override async Task<BaseResponse> SetRunPopWinStateUnary(SetRunPopWinStateRequest request, ServerCallContext context)
        {
            m_Logger.LogInfo($"SetRunPopWinStateUnary: {JsonConvert.SerializeObject(request)}");
            var setWindowRequest = new SetWindowRequest()
            {
                WinState = request.WinState,
                ClientId = request.ClientId,
                WinType = WinType.RunPopWindow
            };

            return await OperatorWindow(setWindowRequest, context.CancellationToken);
        }

        public override async Task<BaseResponse> SetClientWindowStateUnary(SetWindowRequest request, ServerCallContext context)
        {
            return await OperatorWindow(request, context.CancellationToken);
        }

        public override Task<EnvironmentDiagnosticResponse> EnvironmentDiagnosticUnary(EnvironmentDiagnosticRequest request, ServerCallContext context)
        {
            try
            {
                var isPowerSaveMode = SystemInfoUtility.IsSystemInPowerSaveMode();
                var payloadData = new PayloadEnvironmentDiagnostic() { BatterySaverOptions = isPowerSaveMode ? BatterySaverOptions.Opened : BatterySaverOptions.Closed };
                return Task.FromResult(new EnvironmentDiagnosticResponse() { Code = 0, PayloadData = payloadData });
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning("", ex);
                var payloadData = new PayloadEnvironmentDiagnostic() { BatterySaverOptions = BatterySaverOptions.Unkonw };
                return Task.FromResult(new EnvironmentDiagnosticResponse() { Code = 0, PayloadData = payloadData });
            }
        }

        public override async Task<BaseResponse> SwitchPythonEnvUnary(SwitchPythonEnvRequest request, ServerCallContext context)
        {
            m_Logger.LogInfo($"SwitchPythonEnvUnary: {JsonConvert.SerializeObject(request)}");
            var response = new BaseResponse() { Code = 0, Message = OK_VALUE, Result = "", };
            try
            {
                var targetVersion = string.IsNullOrEmpty(request.Payload.Version) ? "3.6.8" : request.Payload.Version;
                if (targetVersion == "3.6.8" || PythonEnvironmentCompatibility.SupportsPython311())
                {
                    if (request.WithinZdesktop)
                    {
                        response = await SwitchZDesktopPythonEnvAsync(request, targetVersion, context).ConfigureAwait(false);
                        return response;
                    }
                    else
                    {
                        if (Global.PythonEnvVersion != targetVersion)
                        {
                            var previousVersion = Global.PythonEnvVersion;
                            m_Logger.LogInfo(
                                $"SwitchPythonEnv restart requested; previous={previousVersion}; target={targetVersion}; requested={request.Payload.Version}");

                            await PythonEnvironmentVersionTransaction.ApplyAsync(
                                request.Payload.Version,
                                () => Global.PythonEnvVersion,
                                value => Global.PythonEnvVersion = value,
                                () => m_WorkerControlController.RestartEngineWorkersAsync(
                                    request.ClientId == "Z-Factory",
                                    context.CancellationToken));

                            m_Logger.LogInfo(
                                $"SwitchPythonEnv restart completed; previous={previousVersion}; target={Global.PythonEnvVersion}");
                        }
                        response = new BaseResponse() { Code = 0, Message = OK_VALUE, Result = "", };
                        return response;
                    }
                }
                else
                {
                    response = new BaseResponse() { Code = 40009992, Message = "流程Python版本过高，当前设备系统版本不支持", Result = "{}", };
                    return response;
                }
            }
            finally
            {
                m_Logger.LogInfo($"####### response: {JsonConvert.SerializeObject(response)}###########");
            }
        }

        public override async Task<BaseResponse> PluginRunUnary(RunPluginRequest request, ServerCallContext context)
        {
            using (WindowRpcDiagnostic.Begin(request.Payload.PluginName))
            {
                long start = Stopwatch.GetTimestamp();
                WindowRpcDiagnostic.Mark("core_engine_request_enter");
                try
                {
                    return await PluginRunUnaryTraced(request, context);
                }
                finally
                {
                    WindowRpcDiagnostic.Mark("core_engine_method_exit", start);
                }
            }
        }

        private async Task<BaseResponse> PluginRunUnaryTraced(RunPluginRequest request, ServerCallContext context)
        {
            long diagnosticStart = Stopwatch.GetTimestamp();

            string param = request.Payload.PluginParamBase64;
            string packagePath = request.Payload.PackagePath;
            string pluginAction = request.Payload.PluginAction;
            string pluginName = request.Payload.PluginName;
            string timeOutStr = request.Payload.PluginRunTimeSpan;
            string timeOutJson = request.Payload.PluginRunTimeJson;
            string retryContext = request.Payload.PluginRunRetryContext;

            bool isNeedCancel = true;
            m_Logger.LogInfo($"PluginRunUnary:{pluginName},{pluginAction}");
            m_Logger.LogInfo($"Time out str:{timeOutStr} ms");
            m_Logger.LogInfo($"Time out json:{timeOutJson}");
            m_Logger.LogInfo($"Retry Context json:{retryContext}");

            if (pluginName.Contains("ScreenSaver"))
            {
                m_Logger.LogInfo($"ScreenSaver:{param}");
                int effect = 2;
                var paramJO = JObject.Parse(param);
                try
                {
                    if (paramJO.ContainsKey("effect"))
                    {
                        effect = paramJO.GetValue<int>("effect");
                    }
                }
                catch
                {
                    effect = 2;
                }
                if (pluginName.EndsWith("ShowSaveWindow"))
                {
                    string customPwd = paramJO.TryGetValue("customPwd", out string customPwdStr) ? customPwdStr : null;
                    m_Logger.LogInfo($"use custom password:{!string.IsNullOrEmpty(customPwd)}");

                    await screenSaver.ShowSaveWindow(paramJO.GetValue<string>("tip"),
                        paramJO.GetValue<bool>("autoUnlock"),
                        customPwd,
                        effect: effect);
                }
                else if (pluginName.EndsWith("StopSaveWindow"))
                {
                    await screenSaver.StopSaveWindow(null);
                }
                var result = new JObject
                {
                    {"code",0},
                    {"info","fail"},
                    {"message",""},
                };
                return new BaseResponse
                {
                    Code = 0,
                    Message = OK_VALUE,
                    Result = result.ToString()
                };


            }
            RunParamModel runparam;

            int timeOutMs;
            try
            {
                runparam = JsonConvert.DeserializeObject<RunParamModel>(timeOutJson);
                runparam.EntryTime = DateTime.Now;
                timeOutMs = runparam.RemainTime;
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex.Message);
                if (int.TryParse(timeOutStr, out timeOutMs))
                {

                }
                else
                {
                    timeOutMs = -1;
                }
                runparam = new RunParamModel();
            }

            PluginRunRetryInfo pluginRunRetryInfo = null;
            if (!string.IsNullOrEmpty(retryContext))
            {
                try
                {
                    pluginRunRetryInfo = JsonConvert.DeserializeObject<PluginRunRetryInfo>(retryContext);
                    timeOutMs = pluginRunRetryInfo.TimeOut;
                }
                catch
                {
                    m_Logger.LogDebug($"Serialize pluginRunRetryInfo fail");
                }
            }


            if (timeOutMs <= 0)
            {
                timeOutMs = -1;
            }

            if (pluginAction == "captureSimilarElements")
            {
                m_Logger.LogDebug("captureSimilarElements time out handle");
                isNeedCancel = false;
                timeOutMs = -1;
            }
            else if ((pluginName == "verifySelector"
                    || pluginName == "verifySelectorSimilar"
                    || pluginName == "Element.IsSimilarElementExist")
                && timeOutMs == -1)
            {
                m_Logger.LogDebug($"{pluginName} time out handle");
                if (int.TryParse(ConfigurationManager.AppSettings["VerifyActionTimeoutSecond"], out int aVerifyActionTimeoutSecond))
                {
                    m_Logger.LogDebug($"Read VerifyActionTimeoutSecond from config. {aVerifyActionTimeoutSecond}s");
                    timeOutMs = aVerifyActionTimeoutSecond * 1000;
                }
                else
                {
                    timeOutMs = 80000;
                }
                m_Logger.LogInfo($"New timeout. {timeOutMs}s");
                isNeedCancel = true;
            }
            else if (pluginName == "Element.DataCollect" && pluginAction == "Excute")
            {
                //数据采集组件要求无限超时
                timeOutMs = -1;
            }

            #region 手机相似元素拾取和手机元素校验-沿用之前插件调用方式-特殊处理
            else if (pluginName == "Mobile.GetMobileSimilarElementInfo" && pluginAction == "Excute")
            {
                m_Logger.LogInfo($"手机相似元素拾取action:【Mobile.GetMobileSimilarElementInfo】");
                var paramResolvedObj = JObject.Parse(param);
                var retval = await m_MobileDeviceManagerGRPCService.CaptureMobileUnary(new Rpa.Core.Mobile.MobileRequest
                {
                    //FilePath = paramResolvedObj["filepath"].ToString(),
                    IsSssimilar = "2",
                    IsCvmodel = "0",
                    CvUrl = "",
                    InParameter = paramResolvedObj["selector"].ToString(),
                    TimeOut = int.Parse(paramResolvedObj["Timeout"].ToString())
                }, context);
                if (retval != null)
                {
                    var result = new JObject
                    {
                        {"code",int.Parse(retval.Code)},
                        {"info",retval.Info},
                        {"message",retval.Message},
                    };
                    return new BaseResponse
                    {
                        Code = 0,
                        Message = OK_VALUE,
                        Result = result.ToString()
                    };
                }
                else
                {
                    m_Logger.LogFatal("【Mobile.GetMobileSimilarElementInfo】PluginLoaderRun return retval = null!");
                    return new BaseResponse
                    {
                        Code = 40009992,
                        Message = eCommonMessageType.PluginGRPCService_cs_XiTongYiChangQingZhongShiRuo.GetMessage(),
                        Result = "{}",
                    };
                }
            }
            else if (pluginName == "Mobile.CheckMobileSelector" && pluginAction == "Excute")
            {
                m_Logger.LogInfo($"手机元素校验action:【Mobile.CheckMobileSelector】");
                var paramResolvedObj = JObject.Parse(param);
                var retval = await m_MobileDeviceManagerGRPCService.CheckMobileUnary(new Rpa.Core.Mobile.MobileRequest
                {
                    //FilePath = paramResolvedObj["filepath"].ToString(),
                    IsSssimilar = paramResolvedObj["isSimilar"] == null ? "0" : paramResolvedObj["isSimilar"].ToString(),
                    IsCvmodel = bool.Parse(paramResolvedObj["isCVCapture"] == null ? "false" : paramResolvedObj["isCVCapture"].ToString()) ? "1" : "0",
                    CvUrl = paramResolvedObj["CVExecutionServiceURL"] == null ? "" : paramResolvedObj["CVExecutionServiceURL"].ToString(),
                    InParameter = paramResolvedObj["selector"].ToString(),
                    TimeOut = int.Parse(paramResolvedObj["timeOut"].ToString())
                }, context);
                if (retval != null)
                {
                    var retResp = new JObject
                    {
                        {"code",int.Parse(retval.Code)},
                        {"info",retval.Info},
                        {"message",retval.Message},
                    };
                    return new BaseResponse
                    {
                        Code = 0,
                        Message = OK_VALUE,
                        Result = retResp.ToString()
                    };
                }
                else
                {
                    m_Logger.LogFatal("【Mobile.CheckMobileSelector】PluginLoaderRun return retval = null!");
                    return new BaseResponse
                    {
                        Code = 40009992,
                        Message = eCommonMessageType.PluginGRPCService_cs_XiTongYiChangQingZhongShiRuo.GetMessage(),
                        Result = "{}",
                    };
                }
            }
            //手机组件执行（原走插件化部分全走这里）
            else if (pluginName.StartsWith("Mobile.") &&
                pluginName != "Mobile.CheckMobileSelector" &&
                pluginName != "Mobile.GetMobileSimilarElementInfo" &&
                pluginAction == "Excute")
            {

                int timeOutMobile;
                var paramResolvedObj = JObject.Parse(param);
                string timeOut = paramResolvedObj["timeOut"] == null ? timeOutMs.ToString() : paramResolvedObj["timeOut"].ToString();
                if (int.TryParse(timeOut, out timeOutMobile))
                {

                }
                else
                {
                    timeOutMobile = -1;
                }
                var retval = await m_MobileProviderController.InvokeAsync(
                    pluginName,
                    param,
                    timeOutMobile,
                    context.CancellationToken).ConfigureAwait(false);

                if (retval != null)
                {
                    var retResp = new JObject
                    {
                        {"code",int.Parse(retval.Code)},
                        {"info",retval.Info},
                        {"message",retval.Message},
                    };
                    return new BaseResponse
                    {
                        Code = 0,
                        Message = OK_VALUE,
                        Result = retResp.ToString()
                    };
                }
                else
                {
                    m_Logger.LogFatal("【Mobile.Plugin】PluginLoaderRun return retval = null!");
                    return new BaseResponse
                    {
                        Code = 40009992,
                        Message = eCommonMessageType.PluginGRPCService_cs_XiTongYiChangQingZhongShiRuo.GetMessage(),
                        Result = "{}",
                    };
                }
            }
            #endregion

            if (timeOutMs <= 0)
            {
                try
                {
                    ElementPlugin elementPlugin = new ElementPlugin(param, pluginName, pluginAction);
                    m_Logger.LogInfo($"流程级别元素修复开关：{_enableProcessRepairControl}");
                    if (elementPlugin.IsCurrentPlugin() && (elementPlugin.IsSmartControl() || _enableProcessRepairControl))
                    {
                        runparam.EnableAIRepair = true;
                        runparam.EnableNormal = true;
                    }
                }
                catch (Exception e)
                {
                    m_Logger.LogError(e);
                }
            }
            try
            {
                param = ReplaceZbrainSericeDomain(param);
                m_Logger.LogInfo($"Final timeout value:{timeOutMs}ms");
                runparam.RemainTime = timeOutMs;
                var retval = await RunRuntimeHostAsync(
                    packagePath,
                    pluginName,
                    pluginAction,
                    param,
                    runparam,
                    context.CancellationToken,
                    isNeedCancel,
                    request.Payload.RunMode);
                WindowRpcDiagnostic.Mark("core_provider_await_completed", diagnosticStart);
                if (retval != null)
                {
                    var retvalJO = JObject.Parse(retval.ToString());
                    if (retvalJO != null)
                    {
                        try
                        {
                            if (pluginRunRetryInfo != null
                                && pluginRunRetryInfo.TotalTimes == pluginRunRetryInfo.RemainTimes
                                && retvalJO["code"] != null
                                && (retvalJO["code"].ToString() != "0"
                                || (pluginName == "Element.IsElementExist" && (retvalJO["info"] == null || retvalJO["info"].ToString() == "0"))))
                            {
                                m_Logger.LogDebug("New Start ai repair condition match...");
                                m_Logger.LogInfo($"流程级别元素修复开关：{_enableProcessRepairControl}");
                                ElementPlugin elementPlugin = new ElementPlugin(param, pluginName, pluginAction);
                                if (!pluginName.EqualIgnoreCase("Element.IsElementExist") && elementPlugin.NeedHandleAfterTimeOut(_enableProcessRepairControl))
                                {
                                    var repairParam = new RunParamModel
                                    {
                                        CurrentAttempt = pluginRunRetryInfo.TotalTimes + 1,
                                        RemainTime = pluginRunRetryInfo.TimeOut,
                                        TotalTime = pluginRunRetryInfo.TimeOut,
                                        StartTimeStamp = Stopwatch.GetTimestamp().ToString(),
                                        EnableAIRepair = true,
                                        EntryTime = DateTime.Now,
                                    };
                                    m_Logger.LogDebug("Start ai repair retry...");
                                    retval = await RunRuntimeHostAsync(
                                                   packagePath,
                                                   pluginName,
                                                   pluginAction,
                                                   param,
                                                   repairParam,
                                                   context.CancellationToken,
                                                   isNeedCancel,
                                                   request.Payload.RunMode);
                                }
                            }
                            else if (retvalJO["code"] != null
                                && retvalJO["code"].ToString() == $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRun_TimeOut}"
                                && runparam.TotalTime > 200
                                && retvalJO["extend"] != null
                                && !retvalJO.Value<bool>("extend"))
                            {
                                m_Logger.LogDebug("Old Start ai repair condition match...");
                                m_Logger.LogInfo($"流程级别元素修复开关：{_enableProcessRepairControl}");
                                ElementPlugin elementPlugin = new ElementPlugin(param, pluginName, pluginAction);
                                if (!pluginName.EqualIgnoreCase("Element.IsElementExist") && elementPlugin.NeedHandleAfterTimeOut(_enableProcessRepairControl))
                                {
                                    var repairParam = new RunParamModel
                                    {
                                        CurrentAttempt = runparam.CurrentAttempt + 1,
                                        RemainTime = 30000,
                                        TotalTime = runparam.TotalTime,
                                        StartTimeStamp = Stopwatch.GetTimestamp().ToString(),
                                        EnableAIRepair = true,
                                        EntryTime = DateTime.Now,
                                    };
                                    m_Logger.LogDebug("Start ai repair retry...");
                                    retval = await RunRuntimeHostAsync(
                                                   packagePath,
                                                   pluginName,
                                                   pluginAction,
                                                   param,
                                                   repairParam,
                                                   context.CancellationToken,
                                                   isNeedCancel,
                                                   request.Payload.RunMode);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            m_Logger.LogError(ex);
                        }
                    }
                    return new BaseResponse
                    {
                        Code = 0,
                        Message = OK_VALUE,
                        Result = retval.ToString()
                    };
                }
                else
                {
                    m_Logger.LogFatal("PluginLoaderRun return retval = null!");
                    return new BaseResponse
                    {
                        Code = 40009992,
                        Message = eCommonMessageType.PluginGRPCService_cs_XiTongYiChangQingZhongShiRuo.GetMessage(),
                        Result = "{}",
                    };
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
                var result = new JObject
                {
                    {"code",1},
                    {"info","fail"},
                    {"message",ex.Message},
                };
                return await Task.FromResult(new BaseResponse
                {
                    Code = 40009992,
                    Message = eCommonMessageType.PluginGRPCService_cs_XiTongYiChangQingZhongShiRuo.GetMessage(),
                    Result = result.ToString(),
                });
            }
        }

        public override async Task<BaseResponse> PluginSetParamUnary(SetParamPluginRequest request, ServerCallContext context)
        {
            try
            {
                var payload = request.Payload;
                string packagePath = payload.PackagePath;

                switch (Global.Client)
                {
                    case ClientID.Factory:
                        if (NeedRefreshCache(payload.ParamBase64))
                        {
                            m_Logger.LogDebug("Factory: Refreshed param cache");
                            Global.UserInfoParam = ProcessingParam(payload.ParamBase64, packagePath);
                            await SetRuntimeHostParametersAsync(
                                packagePath,
                                Global.UserInfoParam).ConfigureAwait(false);
                        }
                        else
                        {
                            m_Logger.LogDebug("Factory: Skipped param refresh");
                            var param = MergeFromParam(Global.UserInfoParam, payload.ParamBase64);
                            await SetRuntimeHostParametersAsync(
                                packagePath,
                                param).ConfigureAwait(false);
                        }

                        if (!m_RuntimeHostController.IsIpaInitialized)
                        {
                            _ = IPAInit(packagePath);
                        }
                        break;

                    case ClientID.DeskTop:
                        if (NeedRefreshCache(payload.ParamBase64))
                        {
                            m_Logger.LogDebug("ZDeskTop: Refreshed param cache");
                            Global.UserInfoParam = ProcessingParam(Global.UserInfoParam, packagePath);
                            await InitAndSetParamAsync(packagePath, Global.UserInfoParam, false);
                        }
                        else
                        {
                            m_Logger.LogDebug("ZDeskTop: Skipped param refresh");
                            var param = MergeFromParam(Global.UserInfoParam, payload.ParamBase64);
                            await InitAndSetParamAsync(packagePath, param, false);
                        }
                        break;

                    default: // Bot & Others
                        if (NeedRefreshCache(payload.ParamBase64))
                        {
                            m_Logger.LogDebug("Bot/Others: Refreshed param cache");
                            Global.UserInfoParam = ProcessingParam(payload.ParamBase64, packagePath);
                            await InitAndSetParamAsync(packagePath, Global.UserInfoParam, false);
                        }
                        else
                        {
                            m_Logger.LogDebug("Bot/Others: Skipped param refresh");
                            var param = MergeFromParam(Global.UserInfoParam, payload.ParamBase64);
                            await InitAndSetParamAsync(packagePath, param, false);
                        }
                        break;
                }

                await SyncUserInfoToChildCoreServerAsync();

                return new BaseResponse
                {
                    Code = 0,
                    Message = OK_VALUE,
                    Result = string.Empty,
                };
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
                return await Task.FromResult(new BaseResponse
                {
                    Code = 40009993,
                    Message = eCommonMessageType.PluginGRPCService_cs_XiTongYiChangQingZhongShiRuo.GetMessage(),
                    Result = "",
                });
            }
        }

        public override async Task<BaseResponse> PluginDisposeUnary(DisposePluginRequest request, ServerCallContext context)
        {
            try
            {
                if ((request.ClientId == "rpa-engine.exe" || request.ClientId == "Z-Factory")
                    && Global.RootClientString == CommonConstants.VALUE_FACTORY)
                {
                    request.Payload.ReuseRuntime = true;
                }
                m_Logger.LogInfo($"Dispose Clientid:{request.ClientId}");
                m_Logger.LogInfo($"Dispose Path:{request.Payload.PackagePath}");
                m_Logger.LogInfo($"Reuse Runtime:{request.Payload.ReuseRuntime}");
                m_Logger.LogInfo($"Wait Ready:{request.Payload.WaitReady}");
                m_Logger.LogInfo($"IsProcessCreatorRunning:{Global.IsProcessCreatorRunning}");
                if (screenSaver.IsShowing)
                {
                    await screenSaver.StopSaveWindow("实在RPA已结束");
                }

                await m_RuntimeHostController.DisposeAsync(
                    request.Payload.PackagePath,
                    context.CancellationToken).ConfigureAwait(false);

                return await Task.FromResult(new BaseResponse
                {
                    Code = 0,
                    Message = OK_VALUE,
                    Result = string.Empty,
                });
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
                return await Task.FromResult(new BaseResponse
                {
                    Code = 40009994,
                    Message = eCommonMessageType.PluginGRPCService_cs_XiTongYiChangQingZhongShiRuo.GetMessage(),
                    Result = "",
                });
            }
        }

        public override async Task<ClientResponse> PluginRunFromClientUnary(RunPluginFromClientRequest request, ServerCallContext context)
        {
            if (request.WithinZdesktop || (Global.IsMiniModeActive && request.ClientId == "Z-Brain"))
            {
                m_Logger.LogDebug("transfer to child coreserver");
                var childPluginClient = GetChildPluginClientOrThrow();
                var headers = new Metadata
                {
                    { "pid", $"{Global.CurrentPId}" },
                    { "role", "coreserver" },
                    { "name", "coreserver" }
                };
                // 转发请求始终等待子核返回，避免默认 30 秒期限截断长时间操作；仍响应调用方取消。
                var callOptions = new CallOptions(headers, deadline: DateTime.MaxValue, cancellationToken: context.CancellationToken);
                request.WithinZdesktop = false;
                return await childPluginClient.PluginRunFromClientUnaryAsync(request, callOptions);
            }

            string param = request.Payload.PluginParam;
            string packagePath = request.Payload.PackagePath;
            string pluginAction = request.Payload.PluginAction;
            try
            {
                var provider = m_ProviderFactory.ResolveProvider(pluginAction, m_RuntimeHostController);
                provider.PackagePath = packagePath;
                var action = provider.ResolveAction(param);
                var paramResolved = ReplaceZbrainSericeDomain(provider.ResolveReqVal(param));
                var preRetVal = provider.PreResolveRetVal(paramResolved, param);

                if (preRetVal.Code != "0")
                {
                    return await Task.FromResult(new ClientResponse
                    {
                        Code = int.Parse(preRetVal.Code),
                        Message = preRetVal.Message,
                        Result = preRetVal.Result,
                        PluginAction = pluginAction,
                    });
                }
                else
                {
                    #region 超时时间处理
                    //int _timeoutSeconds = provider.TimeoutSeconds;
                    int _timeoutSeconds = -1;
                    #endregion
                    object retval;
                    m_Logger.LogDebug($"前端调用action:{pluginAction}");
                    if (pluginAction == "phoneSelector")
                    {
                        m_Logger.LogInfo($"手机普通拾取Action");
                        var paramResolvedObj = JObject.Parse(paramResolved);
                        retval = await m_MobileDeviceManagerGRPCService.CaptureMobileUnary(new Rpa.Core.Mobile.MobileRequest
                        {
                            FilePath = paramResolvedObj["filepath"].ToString(),
                            IsSssimilar = paramResolvedObj["isSimilar"] == null ? "0" : paramResolvedObj["isSimilar"].ToString(),
                            IsCvmodel = "0",
                            CvUrl = "",
                            TimeOut = int.Parse(paramResolvedObj["timeout"].ToString())
                        }, context);
                        m_Logger.LogInfo($"拾取返回：{JsonConvert.SerializeObject(retval)}");
                    }
                    else if (pluginAction == "phoneSelectorCv")
                    {
                        m_Logger.LogInfo($"手机CV拾取Action");
                        var paramResolvedObj = JObject.Parse(paramResolved);
                        retval = await m_MobileDeviceManagerGRPCService.CaptureMobileUnary(new Rpa.Core.Mobile.MobileRequest
                        {
                            FilePath = paramResolvedObj["filepath"].ToString(),
                            IsSssimilar = "0",
                            IsCvmodel = "1",
                            CvUrl = paramResolvedObj["CVExecutionServiceURL"].ToString(),
                            TimeOut = int.Parse(paramResolvedObj["timeout"].ToString())
                        }, context);
                    }
                    else if (pluginAction == "mouseRecordPhone")
                    {
                        m_Logger.LogInfo($"手机坐标拾取Action");
                        var paramResolvedObj = JObject.Parse(paramResolved);
                        retval = await m_MobileDeviceManagerGRPCService.GetPointUnary(new Rpa.Core.Mobile.MobileRequest
                        {
                        }, context);
                    }

                    else
                    {
                        if (provider.NeedSTA)
                        {
                            retval = await RunRuntimeHostByClientAsync(
                                packagePath,
                                action.PluginName,
                                action.PluginAction,
                                paramResolved,
                                anIsNeedAutoInit: true,
                                timeoutSecond: _timeoutSeconds,
                                pluginRunOptions: new PluginRunOptions
                                {
                                    IsAsync = false,
                                    NeedUiThread = true,
                                },
                                cancellationToken: context.CancellationToken);
                        }
                        else
                        {
                            retval = await RunRuntimeHostByClientAsync(
                                packagePath,
                                action.PluginName,
                                action.PluginAction,
                                paramResolved,
                                anIsNeedAutoInit: true,
                                timeoutSecond: _timeoutSeconds,
                                pluginRunOptions: null,
                                cancellationToken: context.CancellationToken);
                        }
                    }
                    var retResolved = provider.ResolveRetVal(retval?.ToString());

                    return await Task.FromResult(new ClientResponse
                    {
                        Code = int.Parse(retResolved.Code),
                        Message = retResolved.Message,
                        Result = retResolved.Result,
                        PluginAction = pluginAction
                    });
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
                return await Task.FromResult(new ClientResponse
                {
                    Code = 40009995,
                    Message = eCommonMessageType.PluginGRPCService_cs_QingQiuShiBaiQingZhongShiRuo.GetMessage(),
                    Result = "",
                    PluginAction = pluginAction,
                });
            }
        }

        public override async Task<BaseResponse> PluginClearUnary(ClearPluginRequest request, ServerCallContext context)
        {
            try
            {
                if (screenSaver.IsShowing)
                {
                    await screenSaver.StopSaveWindow("实在RPA已结束");
                }

                if (!await m_RuntimeHostController.TryRestartAfterRunEndAsync(context.CancellationToken).ConfigureAwait(false))
                {
                    object retval = "{}";
                    var releases = new Dictionary<string, string>
                    {
                        { "Excel.Run", "Dispose" },
                        { "Element.ReleaseGuiResources", "ReleaseGuiResources" }
                    };

                    foreach (var item in releases)
                    {
                        retval = await RunRuntimeHostAsync(request.PackagePath, item.Key, "Excute", JsonConvert.SerializeObject(new
                        {
                            Method = item.Value,
                            Paras = "{}",
                        }),
                        RunParamModel.DefaultValue,
                        context.CancellationToken);
                    }
                }

                return new BaseResponse
                {
                    Code = 0,
                    Message = OK_VALUE,
                    Result = "{}"
                };
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
                return await Task.FromResult(new BaseResponse
                {
                    Code = 40009992,
                    Message = eCommonMessageType.PluginGRPCService_cs_XiTongYiChangQingZhongShiRuo.GetMessage(),
                    Result = "",
                });
            }
        }

        public override async Task<BaseResponse> PluginClearAsyncUnary(ClearPluginRequest request, ServerCallContext context)
        {
            try
            {
                MouseRelease.ReleaseMouseKey();

                return new BaseResponse
                {
                    Code = 0,
                    Message = OK_VALUE,
                    Result = ""
                };
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
                return await Task.FromResult(new BaseResponse
                {
                    Code = 40009992,
                    Message = eCommonMessageType.PluginGRPCService_cs_XiTongYiChangQingZhongShiRuo.GetMessage(),
                    Result = "",
                });
            }
        }

        public override Task<BaseResponse> PluginDataTransferUnary(DataTransferRequest request, ServerCallContext context)
        {
            try
            {
                string aSettingFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Core", "component.json");
                if (System.IO.File.Exists(aSettingFile))
                {
                    System.IO.File.Delete(aSettingFile);
                }
                using var fileStream = System.IO.File.Open(aSettingFile, FileMode.OpenOrCreate);
                using var sw = new StreamWriter(fileStream);
                using var jw = new JsonTextWriter(sw);
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(jw, JArray.Parse(request.DataJson));
            }
            catch (Exception ex)
            {
                m_Logger.LogError("PluginDataTransferUnary Error", ex);
                return Task.FromResult(new BaseResponse
                {
                    Code = 40009992,
                    Message = ex.Message,
                    Result = "",
                });
            }
            return Task.FromResult(new BaseResponse
            {
                Code = 0,
                Message = OK_VALUE,
                Result = "",
            });
        }
        /// <summary>
        /// 手动停止流程
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<BaseResponse> PluginStopUnary(StopPluginRequest request, ServerCallContext context)
        {
            try
            {
                m_Logger.LogInfo($"PluginStopUnary:{request.ClientId}-{request.PackagePath}");
                try
                {
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        m_Logger.LogInfo($"Release control");
                        Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                    }
                }
                catch { }

                if (screenSaver.IsShowing)
                {
                    await screenSaver.StopSaveWindow("实在RPA已停止");
                }

                await m_RuntimeHostController.RestartAsync(
                    dueToTimeout: false,
                    cancellationToken: context.CancellationToken).ConfigureAwait(false);
                return await Task.FromResult(new BaseResponse
                {
                    Code = 0,
                    Message = OK_VALUE,
                    Result = string.Empty,
                });
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
                return await Task.FromResult(new BaseResponse
                {
                    Code = 40009994,
                    Message = eCommonMessageType.PluginGRPCService_cs_XiTongYiChangQingZhongShiRuo.GetMessage(),
                    Result = "",
                });
            }
        }
        /// <summary>
        /// BindingToService
        /// </summary>
        /// <returns></returns>
        public ServerServiceDefinition BindingToService()
        {
            return PluginService.BindService(this);
        }

        private string ReplaceZbrainSericeDomain(string jsonParam)
        {
            try
            {
                var zbrainServicePath = m_RuntimeHostController.ZBrainServicePath;
                if (string.IsNullOrEmpty(zbrainServicePath))
                {
                    zbrainServicePath = Global.ZBrainServiceBaseUrl;
                }
                List<string> paramKey = new List<string> { "cvExecutionServiceURL", "cvserver", "server" };
                var key = paramKey.FirstOrDefault(s => jsonParam.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
                if (string.IsNullOrEmpty(key))
                {
                    return jsonParam;
                }
                JObject obj = JObject.Parse(jsonParam);
                string url = obj[key].ToString();
                var ub = new UriBuilder(url);
                var ub_zb = new UriBuilder(zbrainServicePath);
                ub.Host = ub_zb.Host;
                ub.Port = ub_zb.Port;
                obj[key] = ub.ToString();
                return obj.ToString();
            }
            catch
            {
                return jsonParam;
            }
        }

        private string ProcessingParam(string paramString, string packagePath)
        {
            JObject obj = JObject.Parse(paramString);
            _enableProcessRepairControl = obj.TryGetValue("EnableAiEnhancedScreenshot", StringComparison.OrdinalIgnoreCase, out var repairControl)
                && bool.TryParse(repairControl?.ToString(), out var enableRepair)
                && enableRepair;
            if (!obj.ContainsKey("CoreServerPort"))
            {
                obj.Add("CoreServerPort", Global.ServerPort);
            }
            if (!obj.ContainsKey("PackagePath"))
            {
                obj.Add("PackagePath", packagePath);
            }
            else
            {
                obj["PackagePath"] = packagePath;
            }
            //缓存一个Zbrain的地址
            if (obj.ContainsKey("ZBrainServicePath"))
            {
                if (obj.TryGetValue("ZBrainServicePath", StringComparison.OrdinalIgnoreCase, out var zbPath))
                {
                    m_RuntimeHostController.ZBrainServicePath = zbPath.ToString();
                }
            }
            if (m_RuntimeStateAccessor.TryGetSharedJson(
                    EngineMessageActions.GetUserInfo,
                    out var userInfoJson))
            {
                var userInfos = JObject.Parse(userInfoJson);
                if (!obj.ContainsKey("CLIENT-USERID"))
                {
                    obj.Add("CLIENT-USERID", userInfos["CLIENT-USERID"]?.Value<string>());
                }
                if (!obj.ContainsKey("CLIENT-TENANTCODE"))
                {
                    obj.Add("CLIENT-TENANTCODE", userInfos["CLIENT-TENANTCODE"]?.Value<string>());
                }
            }

            if (m_RuntimeStateAccessor.TryGetSharedJson(
                    EngineMessageActions.SetServiceInfo,
                    out var serviceInfoJson))
            {
                var serviceInfos = JObject.Parse(serviceInfoJson);
                if (serviceInfos.TryGetValue("tarsAgent", out var tarsServiceInfo))
                {
                    if (!obj.ContainsKey("TarsAgentServiceUrl"))
                    {
                        obj.Add("TarsAgentServiceUrl", tarsServiceInfo["url"]?.Value<string>());
                    }
                    if (!obj.ContainsKey("TarsAgentServiceToken"))
                    {
                        obj.Add("TarsAgentServiceToken", tarsServiceInfo["token"]?.Value<string>());
                    }
                }
                else
                {
                    m_Logger.LogWarning("There is no tarsAgent info found!");
                }
            }
            else
            {
                m_Logger.LogWarning("There is no tarsAgent info found1!");
            }
            return obj.ToString();
        }

        private string MergeFromParam(string originalParams, string newParams)
        {
            JObject originalObj = JObject.Parse(originalParams);
            JObject newObj = JObject.Parse(newParams);

            m_Logger.LogDebug($"original:{Environment.NewLine}{originalParams} " +
                $"{Environment.NewLine} " +
                $"new:{Environment.NewLine}{newParams}");

            originalObj.Merge(newObj, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace,
                MergeNullValueHandling = MergeNullValueHandling.Merge
            });

            return originalObj.ToString();
        }

        private async Task SyncUserInfoToChildCoreServerAsync()
        {
            try
            {
                var userInfo = string.IsNullOrEmpty(Global.UserInfoParam)
                    ? Global.BuildUserInforParam()
                    : Global.UserInfoParam;

                if (string.IsNullOrWhiteSpace(userInfo))
                {
                    m_Logger.LogWarning("Skip sync user info to child CoreServer: user info is empty.");
                    return;
                }

                if (await m_WorkerControlController.SyncUserInfoToChildCoreServerAsync(userInfo))
                {
                    m_Logger.LogDebug("Synced user info to child CoreServer.");
                }
                else
                {
                    m_Logger.LogDebug("Skip sync user info to child CoreServer: child connection is not ready.");
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning("Sync user info to child CoreServer failed.", ex);
            }
        }

        /// <summary>
        /// 虚拟运行（小窗）场景下切换 Python 环境。
        /// 先记录期望版本，再尝试下发给子 CoreServer；若小窗/子核尚未就绪则不下发也不报错，
        /// 由小窗启动参数（--python_env）或子核就绪后的补发保证最终生效。
        /// </summary>
        private async Task<BaseResponse> SwitchZDesktopPythonEnvAsync(
            SwitchPythonEnvRequest request,
            string targetVersion,
            ServerCallContext context)
        {
            ZDesktopPythonEnvState.SetDesired(targetVersion);

            if (!TryGetChildCoreServerPort(out var port))
            {
                m_Logger.LogInfo($"Child CoreServer 端口文件不存在（小窗可能尚未启动），SwitchPythonEnv 暂存待应用；version={targetVersion}");
                return new BaseResponse() { Code = 0, Message = OK_VALUE, Result = "deferred", };
            }

            try
            {
                var childPluginClient = GetChildPluginClient(port);
                var headers = new Metadata
                {
                    { "pid", $"{Global.CurrentPId}" },
                    { "role", "coreserver" },
                    { "name", "coreserver" }
                };
                var callOptions = new CallOptions(headers, deadline: DateTime.UtcNow.AddSeconds(ChildSwitchPythonEnvTimeoutSeconds), cancellationToken: context.CancellationToken);

                request.WithinZdesktop = false;
                var childResponse = await childPluginClient.SwitchPythonEnvUnaryAsync(request, callOptions).ConfigureAwait(false);
                if (childResponse.Code == 0)
                {
                    ZDesktopPythonEnvState.MarkApplied(targetVersion);
                }

                return childResponse;
            }
            catch (RpcException ex)
            {
                // 子核连接不可用：清掉缓存的 client，等子核就绪后由 Talker 补发。
                InvalidateChildPluginClient();
                m_Logger.LogWarning($"下发 SwitchPythonEnv 到 Child CoreServer 失败，暂存待子核就绪后补发；port={port}; version={targetVersion}", ex);
                return new BaseResponse() { Code = 0, Message = OK_VALUE, Result = "deferred", };
            }
        }

        /// <summary>
        /// 读取子 CoreServer 端口。端口文件由子 CoreServer 启动后写入，
        /// 文件不存在即表示小窗/子核尚未就绪（不再回退到固定端口）。
        /// </summary>
        private static bool TryGetChildCoreServerPort(out int port)
        {
            port = 0;
            var fileName = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Z-Core",
                "Z-DeskTop.txt");
            if (!File.Exists(fileName))
            {
                return false;
            }

            var content = File.ReadAllText(fileName).Trim();
            return int.TryParse(content, out port) && port > 0 && port <= 65535;
        }

        /// <summary>
        /// 获取（并缓存）指向子 CoreServer 的 PluginService client。
        /// client 与端口绑定，端口变化或连接失败后需要重建。
        /// </summary>
        private PluginService.PluginServiceClient GetChildPluginClient(int port)
        {
            if (_pluginServiceClient != null && _childPluginClientPort == port)
            {
                return _pluginServiceClient;
            }

            InvalidateChildPluginClient();

            _childPluginGrpcClient = new GrpcClient(new ClientOptions
            {
                Endpoint = new TcpEndpoint("127.0.0.1", port)
            });
            _childPluginClientPort = port;
            _pluginServiceClient = _childPluginGrpcClient.CreateClient(invoker => new PluginService.PluginServiceClient(invoker));
            return _pluginServiceClient;
        }

        /// <summary>
        /// 获取子 CoreServer 的 PluginService client；子核未就绪（端口文件缺失或非法）时
        /// 抛出明确的 UNAVAILABLE 错误，避免像以前那样回退到固定端口再报
        /// "failed to connect to all addresses" 这种无从定位的异常。
        /// </summary>
        private PluginService.PluginServiceClient GetChildPluginClientOrThrow()
        {
            if (!TryGetChildCoreServerPort(out var port))
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "Child CoreServer 端口文件不存在，章鱼桌面可能未启动"));
            }

            return GetChildPluginClient(port);
        }

        /// <summary>子核不可用/端口变化时释放旧连接，避免继续使用已失效的 channel。</summary>
        private void InvalidateChildPluginClient()
        {
            _pluginServiceClient = null;
            _childPluginClientPort = 0;

            var grpcClient = _childPluginGrpcClient;
            _childPluginGrpcClient = null;
            if (grpcClient != null)
            {
                _ = grpcClient.DisposeAsync();
            }
        }

        private bool NeedRefreshCache(string paramString)
        {
            if (string.IsNullOrWhiteSpace(paramString))
                return false;

            try
            {
                var paramObj = JObject.Parse(paramString);
                if (paramObj.TryGetValue("RefreshCache", StringComparison.OrdinalIgnoreCase, out var refreshCache))
                {
                    return refreshCache.Type == JTokenType.Boolean && refreshCache.Value<bool>();
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
            }
            return true;
        }

        private async Task InitAndSetParamAsync(string packagePath, string userInfoParam, bool needAutoInit = true)
        {
            m_Logger.LogDebug("InitAndSetParam: PluginLoaderSetParam (runtime 由各端准备)");
            await SetRuntimeHostParametersAsync(packagePath, userInfoParam, autoInitialize: needAutoInit)
                .ConfigureAwait(false);
        }

        private async Task IPAInit(string packagePath)
        {
            try
            {
                m_Logger.LogDebug("IPA pre-init start.");
                var provider = m_ProviderFactory.ResolveProvider("ProcessCreator", m_RuntimeHostController);
                provider.PackagePath = packagePath;
                var paramStr = "{\"action\":99,\"type\":1,\"elementTypes\":[\"normal\",\"cv\"],\"componentList\":[]}";
                var action = provider.ResolveAction(paramStr);
                var paramResolved = provider.ResolveReqVal(paramStr);
                var preRetVal = provider.PreResolveRetVal(paramResolved, paramStr);

                await RunRuntimeHostByClientAsync(
                    packagePath,
                    action.PluginName,
                    action.PluginAction,
                    paramResolved,
                    anIsNeedAutoInit: false,
                    timeoutSecond: -1).ConfigureAwait(false);
                m_RuntimeHostController.IsIpaInitialized = true;
                m_Logger.LogDebug("IPA pre-init over.");
            }
            catch (Exception ex)
            {
                m_Logger.LogError("IPA pre-init error", ex);
            }
        }

        private Task<object> RunRuntimeHostAsync(
            string packagePath,
            string pluginName,
            string action,
            string jsonParameters,
            RunParamModel runParameters,
            CancellationToken cancellationToken = default,
            bool isNeedCancel = true,
            string runMode = "")
            => m_RuntimeHostController.RunAsync(new RuntimeHostRunRequest
            {
                PackagePath = packagePath,
                PluginName = pluginName,
                Action = action,
                JsonParameters = jsonParameters,
                RunParameters = runParameters,
                CancelRuntimeOnTimeout = isNeedCancel,
                RunMode = runMode ?? string.Empty,
                ExecutionMode = RuntimeHostExecutionMode.Queued
            }, cancellationToken);

        private Task<object> RunRuntimeHostByClientAsync(
            string packagePath,
            string pluginName,
            string action,
            string jsonParameters,
            bool anIsNeedAutoInit,
            int timeoutSecond,
            PluginRunOptions pluginRunOptions = null,
            CancellationToken cancellationToken = default)
            => m_RuntimeHostController.RunAsync(new RuntimeHostRunRequest
            {
                PackagePath = packagePath,
                PluginName = pluginName,
                Action = action,
                JsonParameters = jsonParameters,
                RunParameters = RunParamModel.DefaultValue,
                AutoInitialize = anIsNeedAutoInit,
                Options = pluginRunOptions,
                ExecutionMode = RuntimeHostExecutionMode.Direct
            }, cancellationToken);

        private Task SetRuntimeHostParametersAsync(
            string packagePath,
            string jsonParameters,
            bool autoInitialize = true,
            CancellationToken cancellationToken = default)
            => m_RuntimeHostController.SetParametersAsync(new RuntimeHostSetParametersRequest
            {
                PackagePath = packagePath,
                JsonParameters = jsonParameters,
                AutoInitialize = autoInitialize
            }, cancellationToken);

        private async Task<BaseResponse> OperatorWindow(
            SetWindowRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var received = await m_WorkerControlController.RequestOperatorWindowAsync(
                    new OperatorWindowRequest
                    {
                        WindowType = (int)request.WinType,
                        WindowState = (int)request.WinState,
                        Timeout = TimeSpan.FromMilliseconds(500)
                    },
                    cancellationToken);
                if (received)
                {
                    return new BaseResponse() { Code = 0, Message = OK_VALUE, Result = "", };
                }
                return new BaseResponse() { Code = 40009992, Message = "未收到应答", Result = "", };
            }
            catch (Exception ex)
            {
                m_Logger.LogError("SetRunPopWinStateUnary 异常", ex);
                return new BaseResponse() { Code = 40009992, Message = ex.Message, Result = "", };
            }
        }
    }
}

