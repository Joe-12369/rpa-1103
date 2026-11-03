using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility.Utils
{
    public class ConfigInfoReader
    {
        private static ILogger logger = Logger.OfContext();

        public readonly string ConfigDirectory = "../../../Config/";
        public readonly string TimeTrackConfigFileName = "TimeTrackConfig.json";

        #region 软件配置信息读取

        public bool GetTimeTrackConfigJsonContent(ref string strContent)
        {
            return GetConfigJsonContent(ConfigDirectory, TimeTrackConfigFileName, ref strContent);
        }

        public bool GetConfigJsonContent(string DirectoryOPath, string TargetFileName, ref string strContent)
        {
            string configFilePath = Path.Combine(DirectoryOPath, TargetFileName);
            if (string.IsNullOrWhiteSpace(configFilePath) || !File.Exists(configFilePath))
            {
                return false;
            }

            strContent = string.Empty;
            if (ReadFileContent(configFilePath, ref strContent))
            {
                return true;
            }

            return false;
        }

        ////{ "enable": "false", "loglevel": "info" }
        public bool ReadFileContent(string _FilePath, ref string _strContent)
        {
            if (string.IsNullOrWhiteSpace(_FilePath) || !File.Exists(_FilePath))
            {
                return false;
            }
            try
            {
                using (StreamReader reader = new StreamReader(_FilePath))
                {
                    _strContent = reader.ReadToEnd();
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("未检测到速度优化日志配置文件 默认为速度优化日志打印关闭");
            }
            return false;
        }
        #endregion
    }
}
