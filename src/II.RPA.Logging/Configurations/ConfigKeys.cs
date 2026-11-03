namespace II.RPA.Logging.Configurations
{
    public static class ConfigKeys
    {
        /// <summary>
        /// 配置文件的AppSettings节点
        /// </summary>
        public const string AppSettingsSection = "appSettings";

        /// <summary>
        /// 配置Serilog的最低日志级别
        /// </summary>
        public const string MinLogLevel = "serilog:minimum-level";

        /// <summary>
        /// 日志的保留天数
        /// </summary>
        public const string LogHoldDays = "serilog:log-hold-days";

        /// <summary>
        /// 单日日志文件数量上限
        /// </summary>
        public const string RemainFileCount = "serilog:log-remain-file-count";

        /// <summary>
        /// 日志根目录
        /// </summary>
        public const string RootDir = "serilog:root-dir";
    }
}
