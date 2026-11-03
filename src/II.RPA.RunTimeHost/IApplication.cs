using II.RPA.Communication.Hosting;
using System;
using System.Threading;

namespace II.RPA.RuntimeHost
{
    /// <summary>
    /// 此接口后续迁移到基础库内、并且需要一个全局单例、用于线程调度 
    /// todo:合入主分支后再迁移
    /// </summary>
    public interface IApplication
    {
        /// <summary>
        /// 主线程同步上下文
        /// </summary>
        SynchronizationContext MainUISynchronizationContext
        {
            get;
        }

        /// <summary>
        /// 自定义UI线程上下文
        /// </summary>
        Func<SynchronizationContext> CustomUISynchronizationContextBuilder
        {
            get;
        }

    }
}
