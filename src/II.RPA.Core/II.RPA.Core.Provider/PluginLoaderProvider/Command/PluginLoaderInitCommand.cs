using II.RPA.Core.Log;
using System;
using System.Threading;

namespace II.RPA.Core.Provider.PluginLoaderProvider.Command
{
    public class PluginLoaderInitCommand:ConcreteCommand
    {
        private readonly string m_InstallationPath;
        public PluginLoaderInitCommand(
            PluginLoaderInstanceWrapper aPluginLoaderInstanceWrapper,
            string aPluginPath,
            string aPackagePath,
            string aInstallationPath,
            ILogger aLogger,
            int aTimeout = 0,
            Action<object> anAction = null):base(aPluginLoaderInstanceWrapper, aLogger, aTimeout, anAction)
        {
            PluginPath = aPluginPath;
            PackagePath = aPackagePath;
            m_InstallationPath = aInstallationPath;
        }
        /// <summary>
        /// useless
        /// </summary>
        public override void Execute()
        {
            m_Logger.LogDebug($"PluginLoader.Init Start");
            m_PluginLoaderInstanceWrapper.PluginLoader.Init(PluginPath, PackagePath, m_InstallationPath);
            m_Logger.LogDebug($"PluginLoader.Init End");

            m_PluginLoaderInstanceWrapper.HasFirstInit = true;
        }
        /// <summary>
        /// 由于使用了消费线程执行command、所以该方法的返回值无法直接使用、需要通过属性CommandResult获取
        /// </summary>
        /// <returns></returns>
        public override void ExecuteWithResult()
        {
            ExecuteWithReturnResult();
        }

        public override object ExecuteWithReturnResult()
        {
            m_Logger.LogDebug($"PluginLoader.Init Start");
            string initResult = "";
            try
            {
                initResult = m_PluginLoaderInstanceWrapper.PluginLoader.Init(PluginPath, PackagePath, m_InstallationPath);
            }
            catch (Exception ex)
            {
                m_Logger.LogError("Fail to Init", ex);
            }
            m_Logger.LogDebug($"PluginLoader.Init Result: {initResult}");
            m_Logger.LogDebug($"PluginLoader.Init End");
            m_PluginLoaderInstanceWrapper.HasFirstInit = true;
            CommandResult = initResult;
            return initResult;
        }
    }
}
