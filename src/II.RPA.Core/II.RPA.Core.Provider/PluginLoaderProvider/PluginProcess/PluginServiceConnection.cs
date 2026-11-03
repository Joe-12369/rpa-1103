using II.RPA.Core.IPCExtension.IPC;
using II.RPA.Core.Log;

namespace II.RPA.Core.Provider.PluginLoaderProvider.PluginProcess
{
    public class PluginServiceConnection
    {
        private bool IsStarted = false;
        private object lockObj = new object();
        private ILogger logger;
        private string pipeName = string.Empty;
        private IPCExtension.IPCServer server;

        public PluginServiceConnection(ILogger logger, IActionService callBackService) 
        {
            this.logger = logger;
            this.server = new IPCExtension.IPCServer(callBackService, null);
        }

        /// <summary>
        /// 使用指定参数创建IPC服务
        /// </summary>
        /// <param name="logger">日志接口</param>
        /// <param name="callBackService">回调函数</param>
        /// <param name="pipeName">管道名称</param>
        //public PluginServiceConnection(ILogger logger, IActionService callBackService, string pipeName)
        //{
        //    this.logger = logger;
        //    this.server = new IPCExtension.IPCServer(callBackService, pipeName);
        //}

        public void StartServer()
        {
            lock (lockObj)
            {
                if (!this.IsStarted)
                {
                    this.logger.LogInfo("准备启动IPC服务器");
                    this.logger.LogInfo("启动上下文:" + this.server.PipeName);
                    this.server.Start();
                    this.IsStarted = true;
                    this.logger.LogInfo("启动IPC服务器成功");
                }
            }
        }

        public void Stop()
        {
            lock (lockObj)
            {
                this.logger.LogInfo("准备关闭IPC服务器");
                this.logger.LogInfo("关闭上下文:" + this.server.PipeName);
                this.server.Close();
                this.logger.LogInfo("已经关闭IPC服务器");
            }
        }
    }
}