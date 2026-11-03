using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace II.RPA.IPC.Config
{


    public sealed class PipeConfig : IServiceConfig
    {
        static PipeConfig pipeConfig = null;
        static object lockObj = new object();

        public static IServiceConfig DefaultPipe
        {
            get
            {
                lock (lockObj)
                {
                    if (pipeConfig == null)
                    {
                        pipeConfig = new PipeConfig()
                        {
                            CloseTimeout = new TimeSpan(0, 0, 30),
                            OpenTimeout = new TimeSpan(0, 0, 30),
                            ReceiveTimeout = new TimeSpan(0, 0, 300),
                            SendTimeout = new TimeSpan(0, 0, 60),
                            TransferMode = TransferMode.Buffered,
                            MaxBufferSize = int.MaxValue,
                            MaxBufferPoolSize = int.MaxValue,
                            MaxReceivedMessageSize = int.MaxValue,
                            MaxConnections = 32
                        };
                    }
                }

                return pipeConfig;
            }
        }
        public static IServiceConfig _DefaultPipeAsync;
        public static IServiceConfig DefaultPipeAsync
        {
            get
            {
                if (_DefaultPipeAsync == null)
                {
                    _DefaultPipeAsync = new PipeConfig()
                    {
                        CloseTimeout = new TimeSpan(0, 0, 30),
                        OpenTimeout = new TimeSpan(0, 0, 30),
                        ReceiveTimeout = TimeSpan.MaxValue,
                        SendTimeout = TimeSpan.MaxValue,
                        TransferMode = TransferMode.Buffered,
                        MaxBufferSize = int.MaxValue,
                        MaxBufferPoolSize = int.MaxValue,
                        MaxReceivedMessageSize = int.MaxValue,
                        MaxConnections = 32
                    };
                }

                return _DefaultPipeAsync;
            }
        }

        public PipeConfig()
        { }

        /// <summary>
        /// 关闭超时时间，默认为30秒
        /// </summary>
        public TimeSpan CloseTimeout { get; set; }

        public long MaxBufferPoolSize { get; set; }

        public int MaxBufferSize { get; set; }

        public int MaxConnections { get; set; }

        public long MaxReceivedMessageSize { get; set; }

        /// <summary>
        /// 打开超时时间，默认配置为30秒
        /// </summary>
        public TimeSpan OpenTimeout { get; set; }

        /// <summary>
        /// 接收超时时间，默认配置为30秒
        /// </summary>
        public TimeSpan ReceiveTimeout { get; set; }

        /// <summary>
        /// 发送超时时间，默认配置为30秒
        /// </summary>
        public TimeSpan SendTimeout { get; set; }


        public TransferMode TransferMode { get; set; }
    }
}
