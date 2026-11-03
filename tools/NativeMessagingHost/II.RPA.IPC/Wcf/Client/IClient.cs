using System;
using System.ServiceModel;

namespace II.RPA.IPC
{
    /// <summary>
    /// 通讯服务客户端，用于与目标节点进行通讯，此资源是非托管资源，使用完请注意释放资源 常用方式可以使用Using进行包含
    /// </summary>
    /// <typeparam name="T">远程服务协定</typeparam>
    public interface IClient<T> : IDisposable
    {
        /// <summary>
        /// 远程服务协定接口，用于获取远程端暴露的服务接口
        /// </summary>
        T Service { get; }

        /// <summary>
        /// 连接状态
        /// </summary>
        CommunicationState State { get; }
    }
}