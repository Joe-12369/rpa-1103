using System;
using System.Runtime.Caching;
using System.Threading.Tasks;
using II.RPA.IPC;
using II.RPA.IPC.Config;

namespace II.RPA.Core.IPCExtension
{
    public static class IPCClient
    {
        /// <summary>
        /// 判断是否可通讯
        /// </summary>
        /// <param name="pipeName">管道名称</param>
        /// <param name="timeout">超时时间默认为3秒</param>
        /// <returns>是否畅通</returns>
        public static bool Pending(string pipeName, int timeout = 3000)
        {
            if (string.IsNullOrEmpty(pipeName))
            {
                throw new ArgumentException("管道名称不能为Null或者为空！");
            }

            var config = PipeConfig.DefaultPipeAsync;
            PipeConfig tempConfig = new PipeConfig()
            {
                CloseTimeout = config.CloseTimeout,
                MaxBufferPoolSize = int.MaxValue,
                MaxBufferSize = int.MaxValue,
                MaxConnections = config.MaxConnections,
                MaxReceivedMessageSize = int.MaxValue,
                OpenTimeout = config.OpenTimeout,
                ReceiveTimeout = config.ReceiveTimeout,
                SendTimeout = new TimeSpan(0, 0, 0, 0, timeout),
                TransactionFlow = config.TransactionFlow,
                TransactionProtocol = config.TransactionProtocol,
                TransferMode = config.TransferMode
            };
            try
            {
                using (var remoteClient = ClientGenerator.Instance.Generate<IMessageExchange>(tempConfig, pipeName))
                {
                    remoteClient.Service.Pending();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 使用指定参数发送字符串数据
        /// </summary>
        /// <param name="pipeName">管道名称</param>
        /// <param name="action">发送动作类型</param>
        /// <param name="data">发送数据</param>
        /// <param name="sendTimeout">发送超时，单位毫秒</param>
        /// <returns>返回结果</returns>
        public static IResponseResult Send(string pipeName, string action, string data, int sendTimeout)
        {
            if (string.IsNullOrEmpty(pipeName))
            {
                throw new ArgumentException("管道名称不能为Null或者为空！");
            }

            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException("发送数据不能为空！");
            }

            var config = PipeConfig.DefaultPipeAsync;
            PipeConfig tempConfig = new PipeConfig()
            {
                CloseTimeout = config.CloseTimeout,
                MaxBufferPoolSize = int.MaxValue,
                MaxBufferSize = int.MaxValue,
                MaxConnections = config.MaxConnections,
                MaxReceivedMessageSize = int.MaxValue,
                OpenTimeout = config.OpenTimeout,
                ReceiveTimeout = config.ReceiveTimeout,
                SendTimeout = TimeSpan.MaxValue,
                TransactionFlow = config.TransactionFlow,
                TransactionProtocol = config.TransactionProtocol,
                TransferMode = config.TransferMode
            };
            using (var remoteClient = ClientGenerator.Instance.Generate<IMessageExchange>(tempConfig, pipeName))
            {
                RequestActionParams param = new RequestActionParams()
                {
                    Action = action,
                    Parameters = data
                };
                var ret = remoteClient.Service.Request(param);
                return ret;
            }
        }

        /// <summary>
        /// 使用指定参数发送字符串数据，默认超时时间为60秒
        /// </summary>
        /// <param name="pipeName">管道名称</param>
        /// <param name="action">发送动作类型</param>
        /// <param name="data">发送数据，单位毫秒</param>
        /// <returns>返回结果</returns>
        public static IResponseResult Send(string pipeName, string action, string data)
        {
            return Send(pipeName, action, data, int.MaxValue);
        }

        /// <summary>
        /// 使用指定管道名称内存缓存键值参数发送字符串数据
        /// </summary>
        /// <param name="storeKey">管道名称内存缓存键值</param>
        /// <param name="action">发送动作类型</param>
        /// <param name="data">发送数据</param>
        /// <param name="sendTimeout">发送超时，单位毫秒</param>
        /// <returns>返回结果</returns>
        public static IResponseResult SendBy(string storeKey, string action, string data, int sendTimeout)
        {
            string pipeName = MemoryCache.Default.Get(storeKey) as string;
            return Send(pipeName, action, data, sendTimeout);
        }
    }

    public static class IPCClientASync
    {
        /// <summary>
        /// 使用指定参数发送字符串数据
        /// </summary>
        /// <param name="pipeName">管道名称</param>
        /// <param name="action">发送动作类型</param>
        /// <param name="data">发送数据</param>
        /// <param name="sendTimeout">发送超时，单位毫秒</param>
        /// <returns>返回结果</returns>
        public static async Task<IResponseResult> Send(string pipeName, string action, string data, int sendTimeout)
        {
            return await Task<IResponseResult>.Run(() =>
            {
                return IPCClient.Send(pipeName, action, data, sendTimeout);
            });
        }

        /// <summary>
        /// 使用指定参数发送字符串数据，默认超时时间为60秒
        /// </summary>
        /// <param name="pipeName">管道名称</param>
        /// <param name="action">发送动作类型</param>
        /// <param name="data">发送数据，单位毫秒</param>
        /// <returns>返回结果</returns>
        public static async Task<IResponseResult> Send(string pipeName, string action, string data)
        {
            return await Send(pipeName, action, data, (int)PipeConfig.DefaultPipeAsync.SendTimeout.TotalMilliseconds);
        }

        /// <summary>
        /// 使用指定管道名称内存缓存键值参数发送字符串数据
        /// </summary>
        /// <param name="storeKey">管道名称内存缓存键值</param>
        /// <param name="action">发送动作类型</param>
        /// <param name="data">发送数据</param>
        /// <param name="sendTimeout">发送超时，单位毫秒</param>
        /// <returns>返回结果</returns>
        public static async Task<IResponseResult> SendBy(string storeKey, string action, string data, int sendTimeout)
        {
            string pipeName = MemoryCache.Default.Get(storeKey) as string;
            return await Send(pipeName, action, data, sendTimeout);
        }
    }
}