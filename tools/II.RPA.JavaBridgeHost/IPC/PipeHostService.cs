using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.IPC;
using II.RPA.NamedPipeWrapper;
using Newtonsoft.Json;

namespace II.RPA.JavaBridgeHost.IPC
{
    /// <summary>
    /// net10 兼容实现：原实现基于 WCF ServiceHost（System.ServiceModel 服务端托管），
    /// 该 API 在 .NET Core / .NET 10 上不可用，此处改为 NamedPipeServerStream +
    /// MessageReader/MessageWriter（与 OldPipeServiceWraper 相同的 JSON 管道协议）承载 IMessageExchange 服务。
    /// </summary>
    public class PipeHostService : IDisposable
    {
        private readonly string pipeName;
        private readonly object sync = new object();
        private CancellationTokenSource cts;
        private NamedPipeServerStream server;

        public PipeHostService()
        {
            int processID = Process.GetCurrentProcess().Id;
            string pipePostName = $"c5e9b4c4ee837260dd147d34d1c7cfe4ee1e043b_{processID}";
            this.pipeName = $"SZ_JAVAService/{pipePostName}";
        }

        /// <summary>
        /// 关闭服务器容器
        /// </summary>
        public void CloseHostService()
        {
            lock (this.sync)
            {
                try
                {
                    this.cts?.Cancel();
                }
                catch (Exception)
                {
                    //TODO
                }
                try
                {
                    this.server?.Dispose();
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
        /// 使用指定服务实现单例启动服务容器
        /// </summary>
        /// <param name="service">指定服务实现单例</param>
        public void StartHostService(IMessageExchange service)
        {
            lock (this.sync)
            {
                this.CloseHostService();
                this.cts = new CancellationTokenSource();
            }
            Task.Factory.StartNew(() => this.Serve(service, this.cts.Token), this.cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void Serve(IMessageExchange service, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    NamedPipeServerStream pipe = new NamedPipeServerStream(this.pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    lock (this.sync)
                    {
                        this.server = pipe;
                    }
                    pipe.WaitForConnection();
                    using (MessageReader reader = new MessageReader(pipe))
                    using (MessageWriter writer = new MessageWriter(pipe))
                    {
                        while (!token.IsCancellationRequested)
                        {
                            JavaMessage request = reader.ReadMessage<JavaMessage>();
                            IResponseResult response = service.Request(new RequestParams() { Parameters = JsonConvert.SerializeObject(request) });
                            writer.WriteMessage(response?.Result ?? string.Empty);
                        }
                    }
                }
                catch (Exception)
                {
                    //TODO
                }
                Thread.Sleep(10);
            }
        }
    }
}
