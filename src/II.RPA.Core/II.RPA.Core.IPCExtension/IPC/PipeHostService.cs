using System;
using System.Collections.Generic;
using II.RPA.IPC;
using II.RPA.IPC.Builder;
using II.RPA.IPC.Config;

namespace II.RPA.Core.IPCExtension.IPC
{
    internal class PipeHostService : IDisposable
    {
        private string pipeName;
        private IServer server;

        public PipeHostService(string pipeName)
        {
            this.pipeName = pipeName;
        }

        /// <summary>
        /// 关闭服务器容器
        /// </summary>
        public void CloseHostService()
        {
            if (this.server != null)
            {
                try
                {
                    this.server.Close();
                }
                catch (Exception)
                {
                    //TODO
                }
                this.server = null;
            }
        }

        /// <summary>
        /// 强制释放资源
        /// </summary>
        public void Dispose()
        {
            this.CloseHostService();
        }

        /// <summary>
        /// 使用指定服务实现单列启动服务容器
        /// </summary>
        /// <param name="service">指定服务实现单列</param>
        public void StartHostService(IMessageExchange service)
        {
            var config = PipeConfig.DefaultPipeAsync as PipeConfig;
            config.MaxBufferPoolSize = int.MaxValue;
            config.MaxBufferSize = int.MaxValue;
            config.MaxReceivedMessageSize = int.MaxValue;
            PipeServerBuilder builder = new PipeServerBuilder(config, this.pipeName, new List<Type>() { typeof(IMessageExchange) }, service);
            this.server = ServerGenerator.Instance.Generate(builder);
            this.server.Open();
            //this.logger.LogInfo("IPC服务地址：" + this.pipeName);
        }
    }
}