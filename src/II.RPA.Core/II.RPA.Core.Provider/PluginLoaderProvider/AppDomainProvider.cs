using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.Common.Generic;
using II.RPA.Core.Log;
using II.RPA.Core.Provider.PluginLoaderProvider.Command;
using II.RPA.Plugin.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider
{
    /// <summary>
    /// 目前使用AppDomainProvider作为PluginLoader的提供类
    /// </summary>
    [Serializable]
    public class AppDomainProvider : IPluginLoaderProvider,IDisposable
    {
        public IMessageNotify MessageNofify
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

        private readonly ILogger m_Logger;
        private readonly ConcurrentDictionary<string,IndeedAppDomain<PluginLoaderInstanceWrapper>> m_PluginLoaderDictionary;
        private ConcreteCommand m_CurrentCommand;
        private readonly CommandConsumer m_Consumer;

        #region 队列消费模式代码-暂时弃用
        /// <summary>
        /// 队列容量
        /// </summary>
        private const int MaxQueueCapacity = 100;
        /// <summary>
        ///ms、尽量大、目前pluginloader相关的超时都在ConcreteCommand里管理，排队超时目前没有实际意义
        /// </summary>
        private const int QueueOverTime = 90000000;
        private readonly BlockingCollection<ConcreteCommand> m_ActionCommandCollection;
        private readonly BlockingCollection<ConcreteCommand> m_ExecutionCommandCollection;
        private readonly Thread m_ConsumeActionThreadSTA;
        private readonly Thread m_ConsumeExecutionThreadSTA;
        #endregion
        public AppDomainProvider()
        { }

        public AppDomainProvider(ILogger aLogger,CommandConsumer aComsumer):this()
        {
            m_Logger = aLogger;
            m_Consumer = aComsumer;
            m_PluginLoaderDictionary = new ConcurrentDictionary<string, IndeedAppDomain<PluginLoaderInstanceWrapper>>();
            //Task.Run(async () =>
            //{
            //    await Task.Delay(1000);
            //    for (int i = 0; i <= 1000; i++)
            //    {
            //        await MessageNofify.NotifyEngineAndWaitReturn("aaaaaaaaaaaaaaaaaaaaa");
            //        await Task.Delay(3000);
            //    }
            //});
            //Task.Run(async () =>
            //{
            //    await Task.Delay(1000);
            //    for (int i = 0; i <= 1000; i++)
            //    {
            //        await MessageNofify.NotifyFactoryAndWaitReturn("bbbbbbbbbbbbbbbbbbbbbb");
            //        await Task.Delay(3000);
            //    }
            //});
        }
        public string CurrentPackagePath
        {
            get;
            set;
        }
        /// <summary>
        /// 卸载PluginLoader
        /// </summary>
        /// <param name="aPluginPath"></param>
        /// <param name="aPackagePath"></param>
        public async Task PluginLoaderDispose(string aPluginPath, string aPackagePath, CancellationToken aCancellationToken = default)
        {
            await Task.Run(async () => 
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
                    if (m_PluginLoaderDictionary.TryRemove(aPluginPath, out IndeedAppDomain<PluginLoaderInstanceWrapper> value))
                    {
                        m_Logger.LogDebug($"AppDomain: {value.DomainName} will be unloaded");
                        try
                        {
                            var needRestart = value.Instance.NeedRestart;
                            Task timeOutTask = Task.Delay(500);
                            Task unloadTask = Task.Run(() => value.Unload());
                            Task taskCompleted = await Task.WhenAny(timeOutTask, unloadTask);
                            if (taskCompleted == timeOutTask)
                            {
                                Task.Run(() =>
                                {
                                    m_Logger.LogError($"Restart becasue of unload time out");
                                    Environment.Exit(1);
                                });
                                return;
                            }
                            else
                            {
                                if (Global.Client == ClientID.Factory && needRestart)
                                {
                                    Task.Run(async () =>
                                    {
                                        m_Logger.LogInfo($"Restart becasue of factory dispose");
                                        await Task.Delay(100);
                                        Environment.Exit(1);
                                    });
                                    return;
                                }
                                m_Logger.LogInfo($"AppDomain: {value.DomainName} has been unloaded successfully");
                            }
                        }
                        catch (CannotUnloadAppDomainException ce)
                        {
                            m_Logger.LogError(ce);
                            value.Unload();
                        }
                        catch(Exception ex)
                        {
                            m_Logger.LogError($"AppDomain: {value.DomainName} failed to be unloaded",ex);
                            m_Logger.LogInfo($"Restart becasue of fail to be unloaded");
                            await Task.Delay(100);
                            Environment.Exit(1);
                        }
                    }
                    else
                    {
                        m_Logger.LogWarning($"Invalid PackagePath: {aPluginPath}");
                    }
                }
            }, aCancellationToken);
        }
        /// <summary>
        /// PluginLoaderInit
        /// </summary>
        /// <param name="aPluginPath"></param>
        /// <param name="aPackagePath"></param>
        /// <param name="anInstallationPath">useless</param>
        /// <returns></returns>
        public async Task<object> PluginLoaderInit(string aPluginPath, string aPackagePath, string anInstallationPath = "",CancellationToken aCanellationToken = default)
        {
            var currentPluginLoader = GetPluginLoaderToExecute(aPluginPath, aPackagePath);
            var command = new PluginLoaderInitCommand(currentPluginLoader, aPluginPath, aPackagePath, anInstallationPath, m_Logger);
            m_Consumer.Add(command);
            aCanellationToken.ThrowIfCancellationRequested();
            return await Task.FromResult(command.CommandResult);
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
        public async Task<object> PluginLoaderRun(string aPluginPath, string aPackagePath, string aPluginName, string anAction, string aJsonParam,CancellationToken cancellationToken = default,int aTimeOut = -1,bool anIsNeedCancel = true)
        {
            var currentPluginLoader = GetPluginLoaderToExecute(aPluginPath, aPackagePath);
            currentPluginLoader.NeedRestart = true;
            await AutoInit(currentPluginLoader, aPluginPath, aPackagePath);
            PluginLoaderRunCommand command = new PluginLoaderRunCommand(currentPluginLoader, aPluginPath, aPackagePath, aPluginName, anAction, aJsonParam, m_Logger);
            m_Consumer.Add(command);
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
            var command = new PluginLoaderRunCommand(currentPluginLoader, aPluginPath, aPackagePath, aPluginName, anAction, aJsonParam, m_Logger);        
            m_Consumer.Add(command);
            var commandResult = command.CommandResult;
            return await Task.FromResult(commandResult);
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
        public async Task<object> PluginLoaderRunByClientSTA(string aPluginPath, string aPackagePath, string aPluginName, string anAction, string aJsonParam, int aTimeOutSecond, CancellationToken aCancellationToken)
        {
            var currentPluginLoader = GetPluginLoaderToExecute(aPluginPath, aPackagePath);
            currentPluginLoader.NeedRestart = true;
            await AutoInit(currentPluginLoader, aPluginPath, aPackagePath);
            PluginLoaderRunCommand command = new PluginLoaderRunCommand(currentPluginLoader, aPluginPath, aPackagePath, aPluginName, anAction, aJsonParam, m_Logger);
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
        public async Task PluginLoaderSetParam(string aPluginPath, string aPackagePath, string aParam,CancellationToken aCancellationToken = default)
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
                m_Logger.LogError("保存token文件失败",ex);
            }
        }
        private PluginLoaderInstanceWrapper GetPluginLoaderToExecute(string aPluginPath, string aPackagePath)
        {
            try
            {
                PluginLoaderInstanceWrapper pluginLoaderInstanceWrapper = null;
                if (m_PluginLoaderDictionary.TryGetValue(aPluginPath, out var pluginLoader))
                {
                    pluginLoaderInstanceWrapper = pluginLoader.Instance;
                }
                else
                {
                    if (m_PluginLoaderDictionary.TryAdd(aPluginPath, new IndeedAppDomain<PluginLoaderInstanceWrapper>(m_Logger)))
                    {
                        if (m_PluginLoaderDictionary.TryGetValue(aPluginPath, out var newPluginLoader))
                        {
                            pluginLoaderInstanceWrapper = newPluginLoader.Instance;
                        }
                    }
                }
                if (pluginLoaderInstanceWrapper == null)
                {
                    throw new Exception("Can not get PluginLoader or create Domain");
                }
                m_Logger.LogDebug($"Domains in use:{Environment.NewLine}" +
                   $"{string.Join(Environment.NewLine, m_PluginLoaderDictionary?.Select(p => $"{p.Key}-{p.Value.DomainName}-{p.Value.Instance.HasFirstInit}"))}");
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
        
        private void CancelCommand(string aPluginPath)
        {
            if (m_CurrentCommand?.PluginPath == aPluginPath)
            {
                m_CurrentCommand?.CancelWaitResult();
            }
        }
        public string PluginLoaderCallBackHandler(string param)
        {
            return param;
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
        public void Dispose()
        {
            m_Logger.LogInfo($"{nameof(AppDomainProvider)} is disposed");
            if (m_PluginLoaderDictionary.Any())
            {
                foreach (var value in m_PluginLoaderDictionary.Values)
                {
                    value.Unload();
                }
            }
        }

        #region 队列消费模式代码-暂时弃用

        private void InitialAndStartThread(Thread aThread, ref BlockingCollection<ConcreteCommand> aCommands, int aStackCapacity = 1, CancellationToken aCancellationToken = default)
        {
            ///blockingcollection的容量设置为100、当集合中的元素达到100后、add()会阻塞
            aCommands = new BlockingCollection<ConcreteCommand>(MaxQueueCapacity);
            aThread = new Thread(new ParameterizedThreadStart(ConsumeCommandsSTA), aStackCapacity)
            {
                ///该线程必须为true、否则会造成Application.Exit()无法结束整个进程
                IsBackground = true
            };

            aThread.SetApartmentState(ApartmentState.STA);
            aThread.Start(new {Commands = aCommands, Token = aCancellationToken});
        }
        private async Task<bool> AddCommandToQueue(ConcreteCommand aCommand, BlockingCollection<ConcreteCommand> aCommands,CancellationToken aCancellationToken = default)
        {
            return await Task.Run(() =>
            {
                ///排队时间超过QueueOverTime则返回false
                var result = aCommands.TryAdd(aCommand, QueueOverTime, aCancellationToken);
                if (!result)
                {
                    m_Logger.LogInfo($"当前排队的Command达到上限 {aCommands.Count} , 并且等待时间超时 {QueueOverTime}");
                    m_Logger.LogError($"当前排队的Command达到上限 {aCommands.Count} , 并且等待时间超时 {QueueOverTime}");
                }
                m_Logger.LogInfo($"{nameof(aCommands)}: {aCommands.Count}");
                return result;
            }, aCancellationToken);
        }
        /// <summary>
        /// 消费线程的执行方法、目前所有pluginloader的command都在一个执行队列中
        /// 可以为每个pluginloader单独创建一个消费线程，视实际情况修改
        /// </summary>
        private void ConsumeCommandsSTA(dynamic aParameter)
        {
            if (aParameter.Commands is BlockingCollection<ConcreteCommand> aCommands)
            {
                CancellationToken cancellationToken = aParameter.Token;
                while (!cancellationToken.IsCancellationRequested)
                {
                    ///blocking
                    m_CurrentCommand = aCommands.Take(cancellationToken);
                    if (!string.IsNullOrEmpty(m_CurrentCommand.PackagePath) && m_PluginLoaderDictionary.ContainsKey(m_CurrentCommand.PackagePath))
                    {
                        m_CurrentCommand.ExecuteWithResult();
                    }
                    else
                    {
                        m_Logger.LogError($"ignore command: {m_CurrentCommand.GetType().Name} {m_CurrentCommand.PackagePath}");
                    }
                    m_CurrentCommand = null;
                    Thread.Sleep(50);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            else
            {
                m_Logger.LogError($"{aParameter} is not CommandCollection");
            }
        }

        public bool IsVaildPackagePath(string aPluginPath)
        {
            return true;
        }
        #endregion
    }
}
