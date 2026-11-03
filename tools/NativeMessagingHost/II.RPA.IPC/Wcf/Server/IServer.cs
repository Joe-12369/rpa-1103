using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace II.RPA.IPC
{
    /// <summary>
    /// IPC服务，服务端
    /// </summary>
    public interface IServer : IDisposable
    {
        /// <summary>
        /// IPC服务通讯地址
        /// </summary>
        string Address { get; }

        /// <summary>
        /// 服务配置信息
        /// </summary>
        IServiceConfig Config { get; }

        /// <summary>
        /// 服务约定信息
        /// </summary>
        IEnumerable<Type> ImplementedContracts { get; }

        /// <summary>
        /// 服务实现
        /// </summary>
        Type ServiceType { get; }

        /// <summary>
        /// 通讯状态
        /// </summary>
        CommunicationState State { get; }

        /// <summary>
        /// 关闭服务
        /// </summary>
        void Close();

        /// <summary>
        /// 打开服务
        /// </summary>
        void Open();
    }
}