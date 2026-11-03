using System.Diagnostics;

namespace II.RPA.Core.IPCExtension
{
    /// <summary>
    /// 启动弹性插件服务承载的上下文环境，主要指的是常规的一些配置信息
    /// </summary>
    public class StartConfigContext
    {
        /// <summary>
        /// 管道名称根占位字符串
        /// </summary>
        public const string PipeNameRoot = "net.pipe://localhost/SZ_ResilienceGrpc/5F924192070C4EC78E782A90049207DF_{0}";

        /// <summary>
        /// 管道名称
        /// </summary>
        public string DefaultPipeName
        {
            get
            {
                return string.Format(PipeNameRoot, Process.GetCurrentProcess().Id);
            }
        }
    }
}