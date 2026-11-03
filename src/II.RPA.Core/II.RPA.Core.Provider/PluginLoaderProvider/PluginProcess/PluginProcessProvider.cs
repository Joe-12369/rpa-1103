using DynamicData;
using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.IPCExtension.Message;
using II.RPA.Core.Log;
using II.RPA.Core.Models;
using II.RPA.Core.Provider.PluginLoaderProvider.Command;
using II.RPA.Core.Provider.PluginLoaderProvider.RunTimeHost;
using II.RPA.Core.Provider.SubProcessManage;
using II.RPA.Core.Provider.Utility;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.PluginProcess
{
    public class PluginProcessProvider : IPluginLoaderProvider
    {
        private readonly ILogger m_Logger;
        private readonly FuncTaskConsumer m_Consumer;
        private readonly PluginProcessFacade m_DefaultPluginProcessFacade;
        private readonly ISubscriber m_Subscriber;
        private readonly int minTimeout = 200;
        private bool m_NeedRestartProcess = true;
        //private volatile CtsTimeout m_CtsTimeout = null;
        private ConcurrentDictionary<string, CtsTimeout> m_CtsTimeoutMap;
        private readonly ConcurrentDictionary<string, PluginProcessFacade> m_PluginProcessFacades;
        private readonly JavaBridgeHostManager m_JavaBridgeHostManager;
        private readonly object m_InitLock = new object();
        private CtsTimeout m_CtsTimeout;
        private readonly ConcurrentDictionary<string, int> m_ToDisposeFacades;
        private const int WAIT_INIT_TIME = 8000;
        public PluginProcessProvider(ILogger aLogger,
            FuncTaskConsumer aConsumer,
            ISubscriber broker)
        {
            m_CtsTimeoutMap = new ConcurrentDictionary<string, CtsTimeout>();
            m_Logger = aLogger;
            m_Consumer = aConsumer;
            m_Subscriber = broker;
            m_DefaultPluginProcessFacade = new PluginProcessFacade(m_Logger, m_Subscriber);
            m_PluginProcessFacades = new ConcurrentDictionary<string, PluginProcessFacade>();
            m_ToDisposeFacades = new ConcurrentDictionary<string, int>();

            Task.Factory.StartNew(() =>
            {
                //m_DefaultPluginProcessFacade.Start(m_DefaultPluginProcessFacade.PluginProcessStartArg);
            })
            .ConfigureAwait(false)
            .GetAwaiter()
            .OnCompleted(() => { });

            m_JavaBridgeHostManager = new JavaBridgeHostManager(m_Logger);

            if (!int.TryParse(ConfigurationManager.AppSettings["MinTimeout"], out this.minTimeout))
            {
                string error = $"Can not find MinTimeout config!";
                m_Logger.LogError(error);
            }
        }
        public string CurrentPackagePath
        {
            get;
            set;
        }
        public string ZBrainServicePath
        {
            get;
            set;
        }
        public IMessageNotify MessageNofify
        {
            get;
            set;
        }
        public async Task PluginLoaderDispose(string aPluginPath, string aPackagePath, bool waitReady, bool anNeedRestart, CancellationToken cancellationToken = default)
        {
            if (anNeedRestart)
            {
                m_Logger.LogInfo($"PluginLoaderDispose:{aPackagePath}");
                if (m_PluginProcessFacades.TryRemove(aPackagePath, out PluginProcessFacade facade))
                {
                    m_Logger.LogDebug($"Remove facade {aPackagePath}");
                    if (m_ToDisposeFacades.TryRemove(aPackagePath, out _))
                    {
                        m_Logger.LogDebug($"Remove todispose facade:{aPackagePath}");
                    }
                    if (facade.CurrentPluginProcessId == m_DefaultPluginProcessFacade.CurrentPluginProcessId)
                    {
                        m_Logger.LogInfo($"Default facade {facade.CurrentPluginProcessId}.Restart!");
                        void RestartAction()
                        {
                            try
                            {
                                this.m_DefaultPluginProcessFacade.Restart();
                            }
                            catch (Exception ex)
                            {
                                this.m_Logger.LogError("PluginLoaderDispose Restart Error:", ex);
                            }
                        }

                        if (waitReady)
                        {
                            RestartAction();
                        }
                        else
                        {
                            Task.Factory.StartNew(() =>
                            {
                                RestartAction();
                            }, TaskCreationOptions.LongRunning)
                            .ConfigureAwait(false)
                            .GetAwaiter()
                            .OnCompleted(() => { });
                            await Task.Delay(1000);
                        }
                    }
                    else
                    {
                        m_Logger.LogInfo($"Extra facade {facade.CurrentPluginProcessId}.Stop only!");
                        facade.StopProcess();
                    }
                }
                else
                {
                    var contains = m_PluginProcessFacades.ContainsKey(aPackagePath);
                    m_Logger.LogWarning($"Remove facade fail:{aPackagePath};{contains}");
                    if (m_ToDisposeFacades.TryAdd(aPackagePath, 0))
                    {
                        m_Logger.LogWarning($"Add to ToDisposeFacades:{aPackagePath}");
                    }
                    else
                    {
                        m_Logger.LogWarning($"Add to ToDisposeFacades failed:{aPackagePath}");
                    }
                }
                Task.Run(() =>
                {
                    try
                    {
                        var processes = Process.GetProcessesByName("II.RPA.PluginProcess");
                        if (processes.Length > 1)
                        {
                            foreach (var process in processes)
                            {
                                using (process)
                                {
                                    using (var thisprocess = Process.GetCurrentProcess())
                                    {
                                        if (process.SessionId == thisprocess.SessionId)
                                        {
                                            m_Logger.LogWarning($"Extra PluginProcess:{process.Id}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        //ignore
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                // 不重启时需要将packagePath在m_PluginProcessFacades中的键值关系移除掉，
                if (m_PluginProcessFacades.TryRemove(aPackagePath, out PluginProcessFacade facade))
                {
                    m_Logger.LogDebug($"Remove facade {aPackagePath}");
                    if (m_ToDisposeFacades.TryRemove(aPackagePath, out _))
                    {
                        m_Logger.LogDebug($"Remove todispose facade:{aPackagePath}");
                    }
                }
                m_Logger.LogInfo("Dispose from engine.Ignore!");
            }
        }
        public void PluginLoaderDisposeFromFactory(string aPluginPath, string aPackagePath, CancellationToken cancellationToken = default)
        {
            m_Logger.LogDebug($"PluginLoaderDisposeFromFactory:{aPackagePath}");
            if (m_PluginProcessFacades.TryRemove(aPackagePath, out _))
            {
                m_Logger.LogDebug($"Remove facade {aPackagePath}");
            }
            else
            {
                m_Logger.LogWarning($"Remove facade fail: {aPackagePath}");
            }
            try
            {
                m_DefaultPluginProcessFacade.Close();
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
            }
        }
        
        public async Task<object> PluginLoaderInit(string aPluginPath, string aPackagePath, string anInstallationPath, CancellationToken cancellationToken = default)
        {
            m_Logger.LogInfo($"PluginLoaderInit-{aPackagePath}");
            if (m_ToDisposeFacades.TryRemove(aPackagePath, out _))
            {
                m_Logger.LogInfo($"Remove todispose facade:{aPackagePath}");
            }
            CurrentPackagePath = aPackagePath;
            Task.Run(() => { m_JavaBridgeHostManager.ProcessDecide(aPluginPath); });
            var isNotFirst = m_PluginProcessFacades.Any();
            var facade = EnsureProcessFacadeAvailable(aPackagePath, true);
            if (isNotFirst)
            {
                if (m_ToDisposeFacades.TryRemove(aPackagePath, out _))
                {
                    m_Logger.LogInfo($"Remove todispose facade:{aPackagePath} and cancel current init!");
                    facade.StopProcess();
                    return $"Init action has been cancelled.{aPackagePath}";
                }
            }
            try
            {
                m_NeedRestartProcess = true;
                var pluginInfos = PluginUtils.GetActivePluginInfo(aPluginPath);
                if (PluginUtils.IsMoblieProject(aPackagePath))
                {
                    m_NeedRestartProcess = false;
                    m_Logger.LogWarning("set m_NeedRestartProcess to false because of plugin:Mobile");
                }
                else if (pluginInfos.ContainsKey("Database") && !PluginUtils.CompareVersionNumber(pluginInfos["Database"].Version, "2.2.0.0"))
                {
                    m_NeedRestartProcess = false;
                    m_Logger.LogWarning("set m_NeedRestartProcess to false because of plugin:Database");
                }
                else if (pluginInfos.ContainsKey("Excel") && !PluginUtils.CompareVersionNumber(pluginInfos["Excel"].Version, "2.2.0.0"))
                {
                    m_NeedRestartProcess = false;
                    m_Logger.LogWarning("set m_NeedRestartProcess to false because of plugin:Excel");
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning("fail to get plugininfo!", ex);
            }
            PluginIPCInitializeRequest request = new PluginIPCInitializeRequest()
            {
                InstallDirPath = anInstallationPath,
                PluginPath = aPluginPath,
                PackagesPath = aPackagePath
            };
            if (facade.Status != ProcessStatus.Started)
            {
                try
                {
                    m_Logger.LogDebug($"Process status is {facade.Status}.Enter EnsureProcessStatus!");
                    facade.EnsureProcessStarted(TimeSpan.FromSeconds(10));
                    m_Logger.LogDebug($"Process status is {facade.Status}.End EnsureProcessStatus!");
                }
                catch
                {
                    m_Logger.LogError($"pluginprocess status is invalid:{facade.Status}.Id is {facade.CurrentPluginProcessId}");
                }
            }
            return await facade.Init(request);
        }

        public async Task<object> PluginLoaderRun(string aPluginPath, string aPackagePath, string aPluginName, string anAction, string aJsonParam, RunParamModel runParamModel, CancellationToken cancellationToken = default, bool anIsNeedCancel = true)
        {
            object result = null;
            await m_Consumer.Run(async token =>
            {
                //async void 异常无法throw
                try
                {
                    result = await PluginLoaderRunRaw(aPluginPath, aPackagePath, aPluginName, anAction, aJsonParam, runParamModel, token, anIsNeedCancel);
                }
                catch(Exception ex)
                {
                    m_Logger.LogError("Consumer.Run", ex);
                    result = ex;
                }
            }, cancellationToken);
            return result;
        }

        private async Task<object> PluginLoaderRunRaw(string aPluginPath, string aPackagePath, string aPluginName, string anAction, string aJsonParam, RunParamModel runParamModel, CancellationToken cancellationToken = default, bool anIsNeedCancel = true, bool anIsNeedAutoInit = true)
        {
            m_Logger.LogInfo($"PluginLoaderRunRaw:{aPluginName}.{anAction};RemainTime:{runParamModel.RemainTime};TotalTime:{runParamModel.TotalTime};NeedAutoInit:{anIsNeedAutoInit}");
            var facade = EnsureProcessFacadeAvailable(aPackagePath, Global.Client == ClientID.DeskTop, anIsNeedAutoInit);
            try
            {
                PluginIPCRequest request = new PluginIPCRequest()
                {
                    Action = anAction,
                    JsonParam = aJsonParam,
                    PluginName = aPluginName
                };
                if (facade.Status != ProcessStatus.Started && facade.Status != ProcessStatus.Ready)
                {
                    try
                    {
                        m_Logger.LogDebug($"Process status is {facade.Status}.Enter EnsureProcessStatus!");
                        facade.EnsureProcessStarted(TimeSpan.FromSeconds(10));
                        m_Logger.LogDebug($"Process status is {facade.Status}.End EnsureProcessStatus!");
                    }
                    catch
                    {
                        m_Logger.LogError($"pluginprocess status is invalid:{facade.Status}.Id is {facade.CurrentPluginProcessId}");
                    }
                }
                //如果是启动完毕状态，则需要进行初始化
                if (facade.Status == ProcessStatus.Started)
                {
                    m_Logger.LogInfo($"trigger auto init");
                    try
                    {
                        await PluginLoaderInit(aPluginPath, aPackagePath, string.Empty, cancellationToken);
                        if (File.Exists("token.json"))
                        {
                            string paramStr = File.ReadAllText("token.json");
                            m_Logger.LogInfo(paramStr);
                            var paramJO = JObject.Parse(paramStr);
                            if (paramJO[Global.ClientProcessId.ToString()] != null)
                            {
                                m_Logger.LogInfo($"Auto SetParam Start");
                                var paramStrDecode = SercurityUtils.Decrypt(paramJO[Global.ClientProcessId.ToString()].ToString());
                                await PluginLoaderSetParam(aPluginPath, aPackagePath, paramStrDecode);
                                m_Logger.LogInfo($"Auto SetParam End");
                            }
                            else
                            {
                                m_Logger.LogError($"ProcessId is not correct:{Global.ClientProcessId}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError(ex);
                    }
                }
                if (facade.Status != ProcessStatus.Ready)
                {
                    if (facade.WaitInit(WAIT_INIT_TIME))
                    {
                        m_Logger.LogWarning($"wait pluginprocess init to end");
                    }
                    else
                    {
                        m_Logger.LogError($"pluginprocess init execute exceed {WAIT_INIT_TIME}ms");
                        return JsonConvert.SerializeObject(new
                        {
                            code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRun_ServerStatusInvalid}",
                            message = "正在初始化、请稍后重试",
                            info = $"{facade.Status}",
                            extend = ""
                        });
                    }
                }

                if (!facade.IsSetToken)
                {
                    await ManualSetParam(aPluginPath, aPackagePath);
                }
                var aTimeOut = runParamModel.RemainTime;
                m_CtsTimeout = new CtsTimeout(aTimeOut, m_Logger);
                //增加运行唯一标识参数
                request.JsonParam = AddUniqueIDIntoJsonParam(request.JsonParam, m_CtsTimeout, runParamModel);
                ///无限超时
                if (aTimeOut == -1)
                {
                    try
                    {
                        return await facade.RunAsync(request);
                    }
                    catch (EndpointNotFoundException)
                    {
                        m_Logger.LogWarning("IPC connect lost.Retry...");
                        await Task.Delay(500);
                        var process = Process.GetProcessById(facade.CurrentPluginProcessId);
                        if (process != null)
                        {
                            process.Dispose();
                            return await facade.RunAsync(request);
                        }
                        else
                        {
                            m_Logger.LogWarning($"PluginProcess exited accidentally!{facade.CurrentPluginProcessId}");
                            throw;
                        }


                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError(ex);
                        return JsonConvert.SerializeObject(new
                        {
                            code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRunError}",
                            message = $"系统异常，请重试。若仍未解决，请联系运营人员。",
                            info = $"{facade.Status}",
                            extend = ""
                        });
                    }
                }
                else if (aTimeOut < this.minTimeout && (runParamModel.RemainTime == runParamModel.TotalTime || !runParamModel.EnableAIRepair))
                {
                    m_Logger.LogError($"{aTimeOut} is too short.Timeout directly!");
                    return JsonConvert.SerializeObject(new
                    {
                        code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRun_TimeOut}",
                        message = $"设置的超时时间过短，请重新调整超时时间",
                        info = $"{facade.Status}",
                        extend = $"{m_CtsTimeout != null && m_CtsTimeout.HasReset}"
                    });
                }
                else
                {
                    if (aTimeOut < this.minTimeout)
                    {
                        runParamModel.EnableNormal = false;
                    }
                    if (aTimeOut >= 10000)
                    {
                        //超时时间足够大的情况 +1000ms buffer
                        aTimeOut += 1000;
                    }
                    aTimeOut += minTimeout;
                    m_Logger.LogDebug($"Timeout Formated:{aTimeOut}");

                    m_CtsTimeout.Timeout = TimeSpan.FromMilliseconds(aTimeOut);
                    
                    try
                    {
                        m_CtsTimeoutMap.TryAdd(m_CtsTimeout.uniqeID, m_CtsTimeout);
                       

                        //组件执行委托
                        Func<Task<string>> runFunc = async () =>
                        {
                            try
                            {
                                Stopwatch sw = Stopwatch.StartNew();
                                var ret = await facade.RunAsync(request);
                                sw.Stop();
                                m_Logger.LogDebug($"PluginLoaderRun Running cost:{sw.ElapsedMilliseconds}ms!");
                                return ret;
                            }
                            catch (CommunicationException)
                            {
                                m_Logger.LogWarning($"CommunicationException was thrown.Timeout or PluginProcess crash!");
                                return JsonConvert.SerializeObject(new
                                {
                                    code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRunError}",
                                    message = $"执行失败，请重试。若仍未解决，请联系运营人员。",
                                    info = $"{facade.Status}",
                                    extend = ""
                                });
                            }
                        };
                        //超时回调委托
                        Func<TimeSpan, Task> onTimeoutAsync = (time) =>
                        {
                            return Task.Factory.StartNew(() =>
                            {
                                m_Logger.LogWarning($"onTimeoutAsync Timeout: {time.TotalMilliseconds} ms");
                                try
                                {
                                    if (anIsNeedCancel && m_NeedRestartProcess)
                                    {
                                        m_Logger.LogWarning("Restart process");
                                        facade.TimeOutRestart();
                                    }
                                    else
                                    {
                                        m_Logger.LogWarning($"no restart:{anIsNeedCancel};{m_NeedRestartProcess}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    this.m_Logger.LogError("PluginLoaderRun 2 Restart", ex);
                                }
                            }, TaskCreationOptions.LongRunning);
                        };
                        var retExec = await m_CtsTimeout.ExecuteAsync(runFunc, onTimeoutAsync);
                        this.m_Logger.LogInfo("ExecuteAsync End");

                        return retExec;
                    }
                    catch (TimeoutException)
                    {
                        m_Logger.LogWarning($"Catch TimeoutException!");
                        await Task.Delay(500);
                        m_Logger.LogInfo("执行超时,延迟返回,等待进程重启");
                        return JsonConvert.SerializeObject(new
                        {
                            code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRun_TimeOut}",
                            message = string.IsNullOrEmpty(m_CtsTimeout.TimeoutMsg) ? $"操作失败，已超时，请增加属性中的时间值。" : m_CtsTimeout.TimeoutMsg,
                            info = $"{facade.Status}",
                            extend = $"{m_CtsTimeout.HasReset}"
                        });
                    }
                    catch (Exception ex)
                    {
                        //todo：此处可删除
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(m_Logger, ex, "超时判断:");
                        if (m_CtsTimeout.IsTimeout)
                        {
                            //增加延迟：等待超时进程重启
                            await Task.Delay(500);
                            m_Logger.LogInfo("执行超时,延迟返回");
                            return JsonConvert.SerializeObject(new
                            {
                                code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRun_TimeOut}",
                                message = string.IsNullOrEmpty(m_CtsTimeout.TimeoutMsg) ? $"操作失败，已超时，请增加属性中的时间值。" : m_CtsTimeout.TimeoutMsg,
                                info = $"{facade.Status}",
                                extend = ""
                            });
                        }
                        else
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRunError}",
                                message = $"系统异常，请重试。若仍未解决，请联系运营人员。",
                                info = $"{facade.Status}",
                                extend = ""
                            });
                        }
                    }
                    finally
                    {
                        m_CtsTimeoutMap.TryRemove(m_CtsTimeout.uniqeID, out _);
                    }
                }
            }
            catch (EndpointNotFoundException)
            {
                m_Logger.LogError($"Catch EndpointNotFoundException");
                return JsonConvert.SerializeObject(new
                {
                    code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRunError}",
                    message = $"系统异常，请重试。若仍未解决，请联系运营人员。",
                    info = $"{facade.Status}",
                    extend = ""
                });
            }
            catch (CommunicationException)
            {
                m_Logger.LogError($"Catch CommunicationException");
                return JsonConvert.SerializeObject(new
                {
                    code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRunError}",
                    message = $"系统异常，请重试。若仍未解决，请联系运营人员。",
                    info = $"{facade.Status}",
                    extend = ""
                });
            }
            catch (IOException)
            {
                m_Logger.LogError($"Catch EndpointNotFoundException");
                return JsonConvert.SerializeObject(new
                {
                    code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRunError}",
                    message = $"系统异常，请重试。若仍未解决，请联系运营人员。",
                    info = $"{facade.Status}",
                    extend = ""
                });
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
                return JsonConvert.SerializeObject(new
                {
                    code = $"{(int)eGuiType.CoreServer}000{(int)eErrorMessageType.Error_PluginRunError}",
                    message = $"系统异常，请重试。若仍未解决，请联系运营人员。",
                    info = $"{facade.Status}",
                    extend = ""
                });
            }
        }

        public async Task<object> PluginLoaderRunByClient(string aPluginPath, string aPackagePath, string aPluginName, string anAction, string aJsonParam, bool anIsNeedAutoInit, int aTimeOutSecond, CancellationToken cancellationToken = default)
        {
            return await PluginLoaderRunRaw(aPluginPath, aPackagePath, aPluginName, anAction, aJsonParam, RunParamModel.DefaultValue, cancellationToken, anIsNeedAutoInit: anIsNeedAutoInit);
        }

        public async Task<object> PluginLoaderRunByClientSTA(string aPluginPath, string aPackagePath, string aPluginName, string anAction, string aJsonParam, int aTimeOutSecond, CancellationToken cancellationToken = default)
        {
            return await PluginLoaderRunRaw(aPluginPath, aPackagePath, aPluginName, anAction, aJsonParam, RunParamModel.DefaultValue, cancellationToken);
        }

        public async Task PluginLoaderSetParam(string aPluginPath, string aPackagePath, string aParam, CancellationToken cancellationToken = default)
        {
            m_Logger.LogInfo($"PluginLoaderSetParam-{aPackagePath}");
            PluginIPCShareValueRequest request = new PluginIPCShareValueRequest()
            {
                JsonParam = aParam
            };

            Task.Run(() =>
            {
                try
                {
                    if (File.Exists("token.json"))
                    {
                        File.Delete("token.json");
                    }
                    using (var fileStream = File.Open("token.json", FileMode.OpenOrCreate))
                    {
                        using (var sw = new StreamWriter(fileStream))
                        {
                            using (var jw = new JsonTextWriter(sw))
                            {
                                var paramEncrypt = SercurityUtils.Encrypt(aParam);
                                JsonSerializer serializer = new JsonSerializer();
                                serializer.Serialize(jw, new JObject
                            {
                                { Global.ClientProcessId.ToString(),paramEncrypt },
                            });
                                m_Logger.LogDebug($"save param: {paramEncrypt}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    m_Logger.LogError("保存token文件失败", ex);
                }
                return;
            }).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });
            var facade = EnsureProcessFacadeAvailable(aPackagePath, Global.Client == ClientID.DeskTop);
            await facade.ShareCommonValue(request);
            request.JsonParam = new JObject() { { "ClientType", (int)Global.Client } }.ToString();
            await facade.ShareCommonValue(request);
        }

        public void ResetPluginLoaderRunTimeout(string payloadMsg)
        {
            ResetPluginLoaderRunTimeoutPayload msg = JsonConvert.DeserializeObject<ResetPluginLoaderRunTimeoutPayload>(payloadMsg);
            if (!string.IsNullOrEmpty(msg.RunUniqueID)
                && m_CtsTimeoutMap.TryGetValue(msg.RunUniqueID, out var ctsTimtout))
            {
                long newTimeout = msg.NewTimeout;
                long timeRemaining = (long)ctsTimtout.Timeout.TotalMilliseconds - ctsTimtout.ElapsedMilliseconds;
                if (timeRemaining > newTimeout)
                {
                    newTimeout = timeRemaining;
                }
                ctsTimtout?.ResetTimeout(newTimeout, msg.TimeoutMsg);
            }
        }
        private async Task ManualSetParam(string aPluginPath, string aPackagePath)
        {
            try
            {
                if (File.Exists("token.json"))
                {
                    string paramStr = File.ReadAllText("token.json");
                    m_Logger.LogInfo(paramStr);
                    var paramJO = JObject.Parse(paramStr);
                    if (paramJO[Global.ClientProcessId.ToString()] != null)
                    {
                        m_Logger.LogInfo($"Auto SetParam Start");
                        var paramStrDecode = SercurityUtils.Decrypt(paramJO[Global.ClientProcessId.ToString()].ToString());
                        await PluginLoaderSetParam(aPluginPath, aPackagePath, paramStrDecode);
                        m_Logger.LogInfo($"Auto SetParam End");
                    }
                    else
                    {
                        m_Logger.LogError($"ProcessId is not correct:{Global.ClientProcessId}");
                    }
                }
                else
                {
                    m_Logger.LogError($"Fail to ManualSetParam! token.json does not exist.");
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError("读取token文件失败", ex);
            }
        }


        private string AddUniqueIDIntoJsonParam(string jsonParam, CtsTimeout aCtsTimeout, RunParamModel runParamModel)
        {
            try
            {
                JObject obj = JObject.Parse(jsonParam);
                obj.Add("runUniqueID", aCtsTimeout.uniqeID);
                obj.Add("runParam", JToken.FromObject(runParamModel));
                var matchType = ElementMatchType.None;
                if (runParamModel.EnableAIRepair)
                {
                    matchType = matchType | ElementMatchType.AiRepair;
                }
                else
                {
                    matchType = matchType & ~ElementMatchType.AiRepair;
                }
                if (runParamModel.EnableNormal)
                {
                    matchType = matchType | ElementMatchType.Normal;
                }
                else
                {
                    matchType = matchType & ~ElementMatchType.Normal;
                }
                obj.Add("matchType", JToken.FromObject(matchType));
                return obj.ToString();
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"AddUniqueIDIntoJsonParam Error", ex);
                return jsonParam;
            }
        }

        public bool IsVaildPackagePath(string aPackagePath)
        {
            if (!string.IsNullOrEmpty(aPackagePath))
            {
                return m_PluginProcessFacades.ContainsKey(aPackagePath);
            }
            return false;
        }

        public PluginProcessFacade EnsureProcessFacadeAvailable(string aPackagePath, bool aCanCreateNew = false, bool anAllowAutoInit = true)
        {
            try
            {
                if (!anAllowAutoInit)
                {
                    if (!m_PluginProcessFacades.ContainsKey(aPackagePath))
                    {
                        throw new InvalidOperationException($"Invalid PackagePath:{aPackagePath}.Please ensure it has been inited!");
                    }
                }
                lock (m_InitLock)
                {
                    if (!m_PluginProcessFacades.Any())
                    {
                        m_Logger.LogInfo($"PluginProcessFacades is empty.New PackagePath:{aPackagePath}.");
                        return m_PluginProcessFacades.GetOrAdd(aPackagePath, m_DefaultPluginProcessFacade);
                    }
                    else
                    {
                        if (aCanCreateNew)
                        {
                            return m_PluginProcessFacades.GetOrAdd(aPackagePath, _ =>
                            {
                                m_Logger.LogInfo($"New PackagePath:{aPackagePath}.");
                                var facade = new PluginProcessFacade(m_Logger, m_Subscriber);
                                facade.Start(facade.PluginProcessStartArg);
                                m_Logger.LogInfo($"New PluginProcessFacade:{aPackagePath}.");
                                return facade;
                            });
                        }
                        else
                        {
                            if (m_PluginProcessFacades.TryGetValue(aPackagePath, out var facade))
                            {
                                return facade;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Invalid PackagePath:{aPackagePath}.Please ensure it has been inited!");
                            }
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                m_Logger.LogError("EnsureProcessFacadeAvailable",ex);
                throw;
            }
        }


    }
}

