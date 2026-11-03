using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace II.RPA.Logging.Configurations
{
    public class LogSettingModel
    {
        [JsonProperty("serilog:log-remain-file-count")]
        public int RemainFileCount
        {
            get;
            set;
        } = 50;

        [JsonProperty("serilog:minimum-level")]
        public LogLevel Level
        {
            get;
            set;
        } = LogLevel.Information;

        /// <summary>
        /// https://devops.aliyun.com/projex/bug/ZOCO-25618# 《【设计器+日志清理】定时策略为“不清理日志”，启动设计器时会自动清理日志》
        /// 此单子要求C#端去掉 日志自动清理逻辑、但考虑到更多的可控性和测试支持，保留此配置项，默认值从7改为999
        /// </summary>
        [JsonProperty("serilog:log-hold-days")]
        public int RemainDay
        {
            get;
            set;
        } = 7;

        [JsonProperty("serilog:root-dir")]
        public string RootPath
        {
            get;
            set;
        } = string.Empty;

        public string LevelString
        {
            get
            {
                return Level.ToString();
            }
        }

        public static LogSettingModel Default => new LogSettingModel();
    }
}
