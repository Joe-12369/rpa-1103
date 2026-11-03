using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility.Utils
{
    /// <summary>
    /// 耗时统计
    /// </summary>
    public class TimeTrackHelper
    {
        private static ILogger logger = Logger.OfContext();

        #region 成员变量
        Stopwatch stopwatch = null;
        //CancellationTokenSource cts = null;
        string guid = string.Empty;
        public static TimeTrackLogLevel CurrentTimeTrackLogLevel { get; set; } = TimeTrackLogLevel.none;

        public static bool IsActive = false;

        public static bool HasReadConfig = false;
        #endregion

        #region 初始化
        public TimeTrackHelper()
        {
            if (!HasReadConfig)
            {
                InitConfig();
                HasReadConfig = true;
            }

            if (!IsActive || CurrentTimeTrackLogLevel == TimeTrackLogLevel.none)
            {
                return;
            }
            stopwatch = new Stopwatch();
            guid = Guid.NewGuid().ToString("N");
        }

     
        public void InitConfig()
        {
            ConfigInfoReader configInfoReader = new ConfigInfoReader();
            string configJsonContent = string.Empty;
            if (configInfoReader.GetTimeTrackConfigJsonContent(ref configJsonContent))
            {
                try
                {
                    JObject jobj = (JObject)JsonConvert.DeserializeObject(configJsonContent);
                    if (jobj != null)
                    {
                        string isEnable = jobj["enable"].ToString();
                        string logLevel = jobj["loglevel"].ToString();

                        IsActive = (isEnable == "true")? true:false;

                        switch (logLevel)
                        {
                            case "info":
                                CurrentTimeTrackLogLevel = TimeTrackLogLevel.info;
                                break;
                            case "debug":
                                CurrentTimeTrackLogLevel = TimeTrackLogLevel.degbug;
                                break;
                            default:
                                CurrentTimeTrackLogLevel = TimeTrackLogLevel.none;
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogInformation($"TimeTrackLog 读取失败:{ex.Message}");
                }
            }
            else
            {
                IsActive = false;
                CurrentTimeTrackLogLevel = TimeTrackLogLevel.none;
            }
        }
        #endregion


        public void Start(string strMark = "", TimeTrackLogLevel timeTrackLogLevel = TimeTrackLogLevel.degbug)
        {
            if (!IsActive  || (int)timeTrackLogLevel < (int)CurrentTimeTrackLogLevel)
            {
                return;
            }

            if (stopwatch == null)
            {
                stopwatch = new Stopwatch();
                guid = Guid.NewGuid().ToString("N");
            }

            stopwatch?.Start();
            //cts = new CancellationTokenSource();
            //var taskDelay = Task.Delay(5000, cts.Token).ContinueWith(P =>
            //{
            //    stopwatch?.Stop();
            //    cts?.Cancel();
            //});//测试会导致性能消耗 拾取变卡 暂时注释 目前调用需保证start之后 必须调用stop

            logger.LogDebug($"统计耗时 启动 {strMark} XGUID:{guid}");
        }

        public void Pick(string strMark = "", TimeTrackLogLevel timeTrackLogLevel = TimeTrackLogLevel.degbug)
        {
            if (!IsActive || (int)timeTrackLogLevel< (int)CurrentTimeTrackLogLevel)
            {
                return;
            }

            logger.LogDebug($"统计耗时 {strMark} 时长:{stopwatch?.ElapsedMilliseconds}ms XGUID:{guid}");
        }

        public void Record(string strMark = "", TimeTrackLogLevel timeTrackLogLevel = TimeTrackLogLevel.degbug)
        {
            if (!IsActive || (int)timeTrackLogLevel < (int)CurrentTimeTrackLogLevel)
            {
                return;
            }
            logger.LogDebug($"统计耗时 {strMark} 耗时 {stopwatch?.ElapsedMilliseconds}ms XGUID:{guid}");
            stopwatch?.Restart();
        }

        public void Stop(string strMark = "", TimeTrackLogLevel timeTrackLogLevel = TimeTrackLogLevel.degbug)
        {
            if (!IsActive || (int)timeTrackLogLevel < (int)CurrentTimeTrackLogLevel)
            {
                return;
            }

            logger.LogDebug($"统计耗时 {strMark} 结束 XGUID:{guid}");
            stopwatch?.Stop();
            //cts?.Cancel();
            //cts?.Dispose();
        }
    }


    public enum TimeTrackLogLevel : int
    {
        /// <summary>
        /// 关闭
        /// </summary>
        none = 0,

        /// <summary>
        /// 打印debug及以上级别日志
        /// </summary>
        degbug = 1,

        /// <summary>
        /// 打印info及以上级别日志
        /// </summary>
        info = 2,
    }
}
