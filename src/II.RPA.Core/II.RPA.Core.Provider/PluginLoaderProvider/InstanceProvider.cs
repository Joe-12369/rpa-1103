using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using II.RPA.Core.Provider.PluginLoaderProvider.Command;
using II.RPA.Core.Provider.SubProcessManage;
using II.RPA.Plugin.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace II.RPA.Core.Provider.PluginLoaderProvider
{
    /// <summary>
    /// 暂时不用、选择AppDomainProvider
    /// </summary>
    public class InstanceProvider : IPluginLoaderProvider,IMessageService
    {
        public IMessageNotify MessageNofify
        {
            get;
            set;
        }
        public IMessageNotify VisualDataMessageNofify
        {
            get;
            set;
        }
        /// <summary>
        /// engine执行中需要使用STA线程的pluginName
        /// </summary>
        private readonly List<string> m_NeedSTAPluginNames = new List<string>()
        {
           ///自定义对话框
           "Dialog",
           "Element.IsSimilarElementExist",
           "Mobile.CheckMobileSelector",
           "Element.IsElementExist",
           "Element.CaptureSimilar",
        };
        /// <summary>
        /// 允许并发的插件名字,目前主要针对新版数据采集
        /// 允许并发请求的插件要自身保证在UI线程执行
        /// </summary>
        private readonly List<string> m_ConcurrencyPluginNames = new List<string>()
        {
           "GUIFireFox.FireFoxRelease",
           "Element.IsSimilarElementExist",
           "GUIChrome.ChromeRelease",
           "Mouse.MouseRelease",
           "Element.IsElementExist",
           "Element.ProcessCreator",
        };

        private readonly ILogger m_Logger;
        private readonly ConcurrentDictionary<string, PluginLoaderInstanceWrapper> m_PluginLoaderDictionary;
        private ConcreteCommand m_CurrentCommand;
        private readonly CommandConsumer m_Consumer;
        private readonly JavaBridgeHostManager m_JavaBridgeHostManager;

        public InstanceProvider()
        {

        }

        public string CurrentPackagePath 
        { 
            get; 
            set; 
        }
        public InstanceProvider(ILogger aLogger, CommandConsumer aComsumer) : this()
        {
            m_Logger = aLogger;
            m_Consumer = aComsumer;
            m_PluginLoaderDictionary = new ConcurrentDictionary<string, PluginLoaderInstanceWrapper>();
            m_JavaBridgeHostManager = new JavaBridgeHostManager(m_Logger);
        }
        /// <summary>
        /// 卸载PluginLoader
        /// </summary>
        /// <param name="aPluginPath"></param>
        /// <param name="aPackagePath"></param>
        public async Task PluginLoaderDispose(string aPluginPath, string aPackagePath, CancellationToken aCancellationToken = default)
        {
            aCancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(aPluginPath))
            {
                m_Logger.LogInfo($"Ignore null PackagePath");
            }
            else
            {
                m_Logger.LogInfo($"{aPluginPath}");
                //取消正在等待结果的Command
                CancelCommand(aPluginPath);
                //卸载指定的应用程序域
                if (m_PluginLoaderDictionary.TryRemove(aPluginPath, out PluginLoaderInstanceWrapper value))
                {
                    try
                    {
                        if (Global.Client == ClientID.Factory)
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                m_Logger.LogInfo($"Restart becasue of factory dispose");
                                await Task.Delay(10);
                                Environment.Exit(1);
                            });
                            return;
                        }
                    }
                    catch
                    {
                        m_Logger.LogInfo($"Restart becasue of fail to be unloaded");
                        await Task.Delay(10);
                        Environment.Exit(1);
                    }
                }
                else
                {
                    m_Logger.LogWarning($"PluginLoaderDispose Invalid PackagePath: {aPluginPath}");
                }
            }
        }
        /// <summary>
        /// PluginLoaderInit
        /// </summary>
        /// <param name="aPluginPath"></param>
        /// <param name="aPackagePath"></param>
        /// <param name="anInstallationPath">useless</param>
        /// <returns></returns>
        public async Task<object> PluginLoaderInit(string aPluginPath, string aPackagePath, string anInstallationPath = "", CancellationToken aCanellationToken = default)
        {
            Task.Run(() => { m_JavaBridgeHostManager.ProcessDecide(aPluginPath); });
            CurrentPackagePath = aPackagePath;
            var currentPluginLoader = GetPluginLoaderToExecute(aPluginPath, aPackagePath);
            var initcommand = new PluginLoaderInitCommand(currentPluginLoader, aPluginPath, aPackagePath, anInstallationPath, m_Logger);
            m_Consumer.Add(initcommand);
            aCanellationToken.ThrowIfCancellationRequested();
            return await Task.FromResult(initcommand.CommandResult);
        }
        /// <summary>
        /// engine调用PluginLoader Run()
        /// </summary>
        /// <param name="aPluginPath"></param>
        /// <param name="aPackagePath"></param>
        /// <param name="aPluginName"></param>
        /// <param name="anAction"></param>
        /// <param name="aJsonParam"></param>
        /// <returns></returns>
        public async Task<object> PluginLoaderRun(string aPluginPath, string aPackagePath, string aPluginName, string anAction, string aJsonParam, CancellationToken cancellationToken = default,int aTimeOut = -1,bool anIsNeedCancel = true)
        {
            var currentPluginLoader = GetPluginLoaderToExecute(aPluginPath, aPackagePath);
            if (currentPluginLoader.IsRunning)
            {
                m_Logger.LogWarning($"Additional run request {aPluginName} while other plugin is running.");
                if (m_ConcurrencyPluginNames.Contains(aPluginName))
                {
                    return await Task.Run(() => currentPluginLoader.PluginLoader.Run(aPluginName, anAction, aJsonParam));
                }
                else
                {
                    m_Logger.LogError($"{aPluginName} need queue.");
                }
            }
            currentPluginLoader.NeedRestart = true;
            await AutoInit(currentPluginLoader, aPluginPath, aPackagePath);
            PluginLoaderRunCommand command = new PluginLoaderRunCommand(currentPluginLoader, aPluginPath, aPackagePath, aPluginName, anAction, aJsonParam, m_Logger);
            command.ExecuteWithResult();
            var commandResult = command.CommandResult;
            return await Task.FromResult(commandResult);
        }
        /// <summary>
        /// 普通方式执行action
        /// </summary>
        /// <param name="aPluginPath"></param>
        /// <param name="aPackagePath"></param>
        /// <param name="aPluginName"></param>
        /// <param name="anAction"></param>
        /// <param name="aJsonParam"></param>
        /// <returns></returns>
        public async Task<object> PluginLoaderRunByClient(string aPluginPath, string aPackagePath, string aPluginName, string anAction, string aJsonParam, int aTimeOutSecond, CancellationToken cancellationToken = default)
        {
            var currentPluginLoader = GetPluginLoaderToExecute(aPluginPath, aPackagePath);
            currentPluginLoader.NeedRestart = true;
            await AutoInit(currentPluginLoader, aPluginPath, aPackagePath);
            //var command = new PluginLoaderRunCommand(currentPluginLoader, aPackagePath, aPluginName, anAction, aJsonParam, m_Logger, aTimeOutSecond);
            var result = currentPluginLoader.PluginLoader.Run(aPluginName, anAction, aJsonParam);
            return await Task.FromResult(result);
            //m_Consumer.Add(command);
            //var commandResult = command.CommandResult;
            //return await Task.FromResult(commandResult);
        }
        /// <summary>
        /// 以STA线程运行执行action
        /// </summary>
        /// <param name="aPluginPath"></param>
        /// <param name="aPackagePath"></param>
        /// <param name="aPluginName"></param>
        /// <param name="anAction"></param>
        /// <param name="aJsonParam"></param>
        /// <returns></returns>
        public async Task<object> PluginLoaderRunByClientSTA(string aPluginPath, string aPackagePath, string aPluginName, string anAction, string aJsonParam, int aTimeOutSecond, CancellationToken aCancellationToken = default)
        {
            var currentPluginLoader = GetPluginLoaderToExecute(aPluginPath, aPackagePath);
            currentPluginLoader.NeedRestart = true;
            await AutoInit(currentPluginLoader, aPluginPath, aPackagePath);
            PluginLoaderRunCommand command = new PluginLoaderRunCommand(currentPluginLoader, aPluginPath, aPackagePath, aPluginName, anAction, aJsonParam, m_Logger,aTimeOutSecond);
            m_Consumer.Add(command);
            var commandResult = command.CommandResult;
            return await Task.FromResult(commandResult);
        }
        /// <summary>
        /// 参数设置
        /// </summary>
        /// <param name="aPluginPath"></param>
        /// <param name="aPackagePath"></param>
        /// <param name="aParam"></param>
        /// <returns></returns>
        public async Task PluginLoaderSetParam(string aPluginPath, string aPackagePath, string aParam, CancellationToken aCancellationToken = default)
        {
            var currentPluginLoader = GetPluginLoaderToExecute(aPluginPath, aPackagePath);
            await AutoInit(currentPluginLoader, aPluginPath, aPackagePath);
            var command = new PluginLoaderSetParamCommand(currentPluginLoader, aPluginPath, aPackagePath, aParam, m_Logger);
            aCancellationToken.ThrowIfCancellationRequested();
            command.ExecuteWithResult();
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
                            m_Logger.LogInfo($"save param: {paramEncrypt}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError("保存token文件失败", ex);
            }
        }
        private PluginLoaderInstanceWrapper GetPluginLoaderToExecute(string aPluginPath, string aPackagePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(aPackagePath))
                {
                    m_Logger.LogWarning($"PackagePath is null.CurrentPackagePath is {CurrentPackagePath}");
                    aPackagePath = CurrentPackagePath;
                }
                PluginLoaderInstanceWrapper pluginLoaderInstanceWrapper = null;
                pluginLoaderInstanceWrapper = m_PluginLoaderDictionary.GetOrAdd(aPluginPath, (_) => new PluginLoaderInstanceWrapper());
                if (pluginLoaderInstanceWrapper == null)
                {
                    throw new Exception($"Can not get PluginLoader : {aPluginPath},{string.Join(";",m_PluginLoaderDictionary.Keys.ToArray())}");
                }
                pluginLoaderInstanceWrapper.PluginLoader.RegisterMessageServiceSendMessage(SendMessageHandler);
                pluginLoaderInstanceWrapper.PluginLoader.RegisterMessageServicePostMessage(PostMessageHandler);
                return pluginLoaderInstanceWrapper;
            }
            catch (OutOfMemoryException OfmEx)
            {
                m_Logger.LogError("Core server will be retarted because of OutOfMemoryException", OfmEx);
                PluginLoaderDispose(aPluginPath, aPackagePath);
                throw;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
                throw;
            }
        }
        public string SendMessageHandler(TerminalType aTerminalType,string anAction, object aParam)
        {
            try
            {
                m_Logger.LogDebug($"dll send message: {aTerminalType} {anAction} {aParam}");
                string jsonText = "null";
                if (aTerminalType == TerminalType.Engine)
                {
                    var result = MessageNofify.NotifyEngineAndWaitReturn(anAction, aParam.ToString()).Result;
                    jsonText = result.JsonMessage;
                    var code = result.StatusCode;
                    return JsonConvert.SerializeObject(new JMessage
                    {
                        Code = code,
                        Message = jsonText,
                        Info = jsonText,
                    });
                }
                else if (aTerminalType == TerminalType.Factory)
                {
                    jsonText = MessageNofify.NotifyFactoryAndWaitReturn(anAction, aParam.ToString()).Result;
                    return JsonConvert.SerializeObject(new JMessage
                    {
                        Code = 0,
                        Message = jsonText,
                        Info = jsonText,
                    });
                }
                else if (aTerminalType == TerminalType.CoreServer)
                {
                    try
                    {
                        m_Logger.LogInfo($"{anAction}:{aParam}");

                        var ret = MessageNofify.ServerMessagePublishSync(anAction, JsonConvert.SerializeObject(aParam));
                        return JsonConvert.SerializeObject(new JMessage
                        {
                            Code = 0,
                            Message = "",
                            Info = ret,
                        });
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError(ex);
                        return JsonConvert.SerializeObject(new JMessage
                        {
                            Code = 1,
                            Message = ex.Message,
                            Info = "",
                        });
                    }
                }
                else
                {
                    return JsonConvert.SerializeObject(new JMessage
                    {
                        Code = 1,
                        Message = $"Invalid terminal {aTerminalType}",
                        Info = $"Invalid terminal {aTerminalType}",
                    });
                }
            }
            catch (TimeoutException ex)
            {
                m_Logger.LogError(ex);
                return JsonConvert.SerializeObject(new JMessage
                {
                    Code = 1,
                    Message = "time out",
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new JMessage
                {
                    Code = 1,
                    Message = ex.Message,
                });
            }
        }
        public bool PostMessageHandler(TerminalType aTerminalType, string anAction, object aParam, Action<object> aResultAction)
        {
            m_Logger.LogDebug($"dll post message: {aTerminalType} {anAction} {aParam}");
            Task.Run(async() =>
            {
                JMessage message = new JMessage();
                try
                {
                    if (aTerminalType == TerminalType.Engine)
                    {
                        var result = await MessageNofify.NotifyEngineAndWaitReturn(anAction, aParam.ToString());
                        message.Info = result.JsonMessage;
                        message.Code = result.StatusCode;
                        if (message.Code == 1)
                        {
                            message.Message = message.Info;
                        }
                    }
                    else if (aTerminalType == TerminalType.Factory)
                    {
                        message.Info = await MessageNofify.NotifyFactoryAndWaitReturn(anAction, aParam.ToString());
                        message.Code = string.IsNullOrEmpty(message.Info) ? 1 : 0;
                        message.Message = string.IsNullOrEmpty(message.Info) ? "selector from factory is null or empty" : "ok";
                    }
                    else if (aTerminalType == TerminalType.CoreServer)
                    {
                        if (anAction == "visualdata.excel")
                        {
                            if (VisualDataMessageNofify != null)
                            {
                                m_Logger.LogInfo($"{anAction}:{aParam}");
                                var ret = await VisualDataMessageNofify.ServerMessagePublish(anAction, JsonConvert.SerializeObject(aParam));
                                aResultAction?.Invoke(ret);
                            }
                            else
                            {
                                m_Logger.LogError("VisualDataMessageNofify is null");
                            }
                        }
                        else
                        {
                            m_Logger.LogInfo($"{anAction}:{aParam}");
                            var ret = await MessageNofify.ServerMessagePublish(anAction, JsonConvert.SerializeObject(aParam));
                            aResultAction?.Invoke(ret);
                        }
                    }
                    else
                    {
                        //todo
                    }
                }
                catch (TimeoutException)
                {
                    m_Logger.LogError(nameof(TimeoutException));
                    message.Message = "time out";
                    message.Info = "time out";
                    message.Code = 1;
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex);
                    message.Message = ex.Message;
                    message.Info = ex.Message;
                    message.Code = 1;
                }
                finally
                {
                    aResultAction.Invoke(JsonConvert.SerializeObject(message));
                }
            }).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });
            return true;
        }
        private void CancelCommand(string aPluginPath)
        {
            if (m_CurrentCommand?.PluginPath == aPluginPath)
            {
                m_CurrentCommand?.CancelWaitResult();
            }
        }
        private async Task AutoInit(PluginLoaderInstanceWrapper currentPluginLoader, string aPluginPath, string aPackagePath)
        {
            if (!currentPluginLoader.HasFirstInit)
            {
                m_Logger.LogWarning($"HasFirstInit is False.AutoInit Start...");
                await PluginLoaderInit(aPluginPath, aPackagePath);
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
                            var command = new PluginLoaderSetParamCommand(currentPluginLoader, aPluginPath, aPackagePath, paramStrDecode, m_Logger);
                            command.ExecuteWithResult();
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
                    m_Logger.LogError("读取token文件失败", ex);
                }
            }
        }

        public bool IsVaildPackagePath(string aPluginPath)
        {
            if (!string.IsNullOrEmpty(aPluginPath))
            {
                return m_PluginLoaderDictionary.ContainsKey(aPluginPath);
            }
            return false;
        }
    }
}
