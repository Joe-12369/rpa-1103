using System;
using System.ComponentModel;
using System.ServiceModel;

namespace II.RPA.IPC
{
    public interface IServiceConfig
    {
        // 摘要: 获取或设置在传输引发异常之前可用于关闭连接的时间间隔。
        //
        // 返回结果: System.TimeSpan，指定在超时前必须完成关闭操作的时间。 默认值为 1 分钟。
        //
        // 异常: T:System.ArgumentOutOfRangeException: 值小于零或太大。
        [DefaultValue(typeof(TimeSpan), "00:01:00")]
        TimeSpan CloseTimeout { get; }

        // 摘要: 获取或设置内存中用于对传入消息进行缓冲的最大字节数。
        //
        // 返回结果: 内存中用于对传入消息进行缓冲的最大字节数。 默认值为 524288 字节。
        [DefaultValue(524288)]
        long MaxBufferPoolSize { get; }

        // 摘要: 获取或设置内存中用于对传入消息进行缓冲的最大字节数。
        //
        // 返回结果: 内存中用于对传入消息进行缓冲的最大字节数。 默认值为 65,536 字节。
        //
        // 异常: T:System.ArgumentOutOfRangeException: 设置的值小于 0。
        [DefaultValue(65536)]
        int MaxBufferSize { get; }

        // 摘要: 获取或设置入站和出站的最大连接数，这些连接是配置了命名管道绑定的终结点所允许的。
        //
        // 返回结果: 该绑定所允许的命名管道连接的最大数目。 默认值为 10。
        int MaxConnections { get; }

        // 摘要: 获取或设置最大大小，以字节为单位，由绑定处理的接收消息。
        //
        // 返回结果: 绑定可处理的最大接收消息大小（以字节为单位）。 默认值为 65,536 字节。
        [DefaultValue(65536)]
        long MaxReceivedMessageSize { get; }

        // 摘要: 获取或设置在传输引发异常之前可用于打开连接的时间间隔。
        //
        // 返回结果: System.TimeSpan，指定在超时前必须完成打开操作的时间。 默认值为 1 分钟。
        //
        // 异常: T:System.ArgumentOutOfRangeException: 值小于零或太大。
        [DefaultValue(typeof(TimeSpan), "00:01:00")]
        TimeSpan OpenTimeout { get; }

        // 摘要: 获取或设置连接在撤消之前保持非活动状态的最大时间间隔，在此时间间隔内未接收任何应用程序消息。
        //
        // 返回结果: 一个 System.TimeSpan，指定在超时前应用程序接收消息的时间。 默认值为 10 分钟。
        //
        // 异常: T:System.ArgumentOutOfRangeException: 值小于零或太大。
        [DefaultValue(typeof(TimeSpan), "00:10:00")]
        TimeSpan ReceiveTimeout { get; }

        // 摘要: 获取或设置在传输引发异常之前可用于完成写入操作的时间间隔。
        //
        // 返回结果: System.TimeSpan，指定在超时前必须完成写入操作的时间。 默认值为 1 分钟。
        //
        // 异常: T:System.ArgumentOutOfRangeException: 值小于零或太大。
        [DefaultValue(typeof(TimeSpan), "00:01:00")]
        TimeSpan SendTimeout { get; }

        // 摘要: 获取或设置一个值，该值指示采用此绑定配置的服务是使用消息传输的流处理模式（单向或双向），还是缓冲模式。
        //
        // 返回结果: System.ServiceModel.TransferMode 值，该值指示通过此绑定配置的服务是使用消息传输的流处理模式（单向或双向）还是缓冲模式。 默认值为 System.ServiceModel.TransferMode.Buffered。
        [DefaultValue(TransferMode.Buffered)]
        TransferMode TransferMode { get; }
    }
}
