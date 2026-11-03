using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Windows.Threading;

namespace II.RPA.Core.Provider.PluginLoaderProvider.Command
{
    public class PluginLoaderRunCommand : ConcreteCommand
    {
        private readonly string m_PluginName;
        private readonly string m_Action;
        private readonly string m_JsonParam;
        public PluginLoaderRunCommand(
            PluginLoaderInstanceWrapper aPluginLoaderInstanceWrapper,
            string aPluginPath,
            string aPackagePath,
            string aPluginName,
            string anAction,
            string aJsonParam,
            ILogger aLogger,
            int aTimeout = 0,
            Action<object> anActionAfterExecute = null) : base(aPluginLoaderInstanceWrapper, aLogger, aTimeout, anActionAfterExecute)
        {
            PluginPath = aPluginPath;
            PackagePath = aPackagePath;
            m_PluginName = aPluginName;
            m_Action = anAction;
            m_JsonParam = aJsonParam;
        }
        public override void Execute()
        {
            RunInternal();
        }

        public override void ExecuteWithResult()
        {
            RunInternal();
        }

        public override object ExecuteWithReturnResult()
        {
            return RunInternal();
        }

        private object RunInternal()
        {
            m_PluginLoaderInstanceWrapper.IsRunning = true;
            m_Logger.LogDebug($"PluginLoader.Run Start");
            m_Logger.LogInfo($"pluginName:{m_PluginName};pluginAction:{m_Action}");
            string runResult = string.Empty;
            try
            {
                runResult = m_PluginLoaderInstanceWrapper.PluginLoader.Run(m_PluginName, m_Action, m_JsonParam);
                var code = JObject.Parse(runResult).Value<string>("code");
                switch (code)
                {
                    ///此逻辑在5.9.5中已被删除,蛮吉建议保留
                    ///针对所有PluginLoader.Run()的返回统一处理,由于处理逻辑中涉Init()方法，而且与Action类型无关，所以加在此处比较合理
                    ///其他action相关的入参、出参、预处理都应该在各自的ActionProvider中完成
                    case "400000974":
                        m_Logger.LogDebug($"code from run result:400000974.try to re-init and re-run");
                        m_PluginLoaderInstanceWrapper.PluginLoader.Init(PluginPath, PackagePath, "");
                        CommandResult = m_PluginLoaderInstanceWrapper.PluginLoader.Run(m_PluginName, m_Action, m_JsonParam);
                        break;
                    default:
                        CommandResult = runResult;
                        m_Logger.LogDebug($"code from run result:{code}");
                        break;
                }

            }
            catch (Exception ex)
            {
                m_Logger.LogError("PluginLoader.Run Failed: ", ex);
                CommandResult = new JObject
                {
                    {"code",1},
                    {"info","fail"},
                    {"message","执行失败,请重试!"},
                };
            }
            m_Logger.LogDebug($"PluginLoader.Run End");
            m_PluginLoaderInstanceWrapper.IsRunning = false;
            return runResult;
        }
    }
}
