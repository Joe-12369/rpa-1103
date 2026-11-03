using System;
using System.Collections.Generic;
using System.Diagnostics;
using II.RPA.IPC;
using II.RPA.IPC.Builder;
using II.RPA.IPC.Config;

namespace II.RPA.NativeMessageingStartup
{
    public class PipeHostService : IDisposable
    {
        private string pipeName;
        private IServer server;

        public static string GetPipeName(string serviceKey)
        {
            int processID = Process.GetCurrentProcess().Id;
            string pipePostName = $"{serviceKey}_{processID}";
            return $"net.pipe://localhost/SZ_RPAService/{pipePostName}";

        }

        public PipeHostService(string serviceKey)
        {
            this.pipeName = GetPipeName(serviceKey);
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
            PipeServerBuilder builder = new PipeServerBuilder(PipeConfig.DefaultPipe, this.pipeName, new List<Type>() { typeof(IMessageExchange) }, service);
            this.server = ServerGenerator.Instance.Generate(builder);
            this.server.Open();
        }
    }
}