using II.RPA.Core.Log;
using System;

namespace II.RPA.Core.Provider.PluginLoaderProvider.Command
{
    public class PluginLoaderSetParamCommand : ConcreteCommand
    {
        private readonly string m_RuntimeParam;

        public PluginLoaderSetParamCommand(
            PluginLoaderInstanceWrapper aPluginLoaderInstanceWrapper,
            string aPluginPath,
            string aPackagePath,
            string aRuntimeParam,
            ILogger aLogger,
            int aTimeout = 0,
            Action<object> anActionAfterExecute = null) : base(aPluginLoaderInstanceWrapper, aLogger, aTimeout, anActionAfterExecute)
        {
            m_RuntimeParam = aRuntimeParam;
            PluginPath = aPluginPath;
            PackagePath = aPackagePath;
        }
        public override void Execute()
        {
            m_Logger.LogDebug($"PluginLoader.SetSharedVariable Start");
            m_PluginLoaderInstanceWrapper.PluginLoader.SetSharedVariable(m_RuntimeParam);
            m_Logger.LogDebug($"PluginLoader.SetSharedVariable End");
        }
        public override void ExecuteWithResult()
        {
            Execute();
            CommandResult = true;
        }

        public override object ExecuteWithReturnResult()
        {
            Execute();
            return true;
        }
    }
}
