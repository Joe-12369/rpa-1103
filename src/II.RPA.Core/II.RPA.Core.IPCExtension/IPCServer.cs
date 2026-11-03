using System;
using II.RPA.Core.IPCExtension.IPC;

namespace II.RPA.Core.IPCExtension
{
    public partial class IPCServer : IDisposable
    {
        private IActionService callBackService;
        private string currentPipeName;
        private PipeHostService serviceHost;
        private StartConfigContext startConfig;

        /// <summary>
        /// 使用指定参数创建IPC服务
        /// </summary>
        /// <param name="logger">日志接口</param>
        /// <param name="callBackService">回调函数</param>
        public IPCServer(IActionService callBackService) : this(callBackService, null)
        {
        }

        /// <summary>
        /// 使用指定参数创建IPC服务
        /// </summary>
        /// <param name="logger">日志接口</param>
        /// <param name="callBackService">回调函数</param>
        /// <param name="pipeName">管道名称</param>
        public IPCServer(IActionService callBackService, string pipeName)
        {
            if (callBackService == null)
            {
                throw new NullReferenceException("回调服务不能为空！");
            }

            this.startConfig = new StartConfigContext();
            this.callBackService = callBackService;
            if (!string.IsNullOrEmpty(pipeName))
            {
                this.currentPipeName = pipeName;
            }
            else
            {
                this.currentPipeName = this.startConfig.DefaultPipeName;
            }
        }

        public string PipeName
        {
            get
            {
                return this.currentPipeName;
            }
        }

        public void Close()
        {
            this.serviceHost?.CloseHostService();
        }

        public void Dispose()
        {
            this.Close();
        }

        /// <summary>
        /// 启动IPC服务器
        /// </summary>
        public void Start()
        {
            try
            {
                //this.logger.LogInfo("准备启动IPC服务器");
                //this.logger.LogInfo("启动上下文:" + JsonConvert.SerializeObject(this.startConfig));
                if (this.serviceHost == null)
                {
                    this.serviceHost = new PipeHostService(this.currentPipeName);
                }
                this.serviceHost.StartHostService(new PluginIPCService(this.callBackService));
                //this.logger.LogInfo("启动IPC服务器成功");
            }
            catch //(Exception ex)
            {
                //this.logger.LogError("启动IPC服务器失败");
                //this.logger.LogError(ex);
                throw;
            }
        }
    }
}