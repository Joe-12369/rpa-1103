using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.WinAutomation.CommonUtility.Utils
{
    public class RPAEnvironment : SingleInstance<RPAEnvironment>
    {
        private static ILogger logger = Logger.OfContext();
        /// <summary>
        /// 是否设置语言状态
        /// </summary>
        private bool ConfigDefined = false;

        private object ob_SetConfig = new object();

        public event EventHandler<LanguageChangedEventArgs> LanguageChanged;

        private void OnLanguageChanged(LanguageCultureEnum oldLang, LanguageCultureEnum newLang)
        {
            LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(oldLang, newLang));
        }

        public RPAEnvironment()
        {
            if (!ConfigDefined)
            {
                try
                {
                    ReadLocalConfig();
                }
                catch (Exception ex)
                {
                    logger.LogError($"ReadLocalConfig fail {ex.ToString()}");
                }
                finally
                {
                   
                }
            }
        }

        public Func<LanguageCultureEnum> SetLanguage;

        #region  当前语言
        private LanguageCultureEnum currentLanguage = LanguageCultureEnum.zh_CN;
        public LanguageCultureEnum CurrentLanguage
        {
            get
            {
                if (!ConfigDefined)
                {
                    try
                    {
                        ReadLocalConfig();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"ReadLocalConfig fail {ex.ToString()}");
                    }
                    finally
                    {
                       
                    }
                }
                return currentLanguage;
            }

            private set
            {
                currentLanguage = value;
                //lock (ob_SetConfig)
                //{
                //    ConfigDefined = true;
                //}
            }
        }
        #endregion

        #region  当前站点 region
        private RegionTypeEnum currentRegion = RegionTypeEnum.CN;
        public RegionTypeEnum CurrentRegion
        {
            get
            {
                if (!ConfigDefined)
                {
                    try
                    {
                        ReadLocalConfig();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"ReadLocalConfig fail {ex.ToString()}");
                    }
                    finally
                    {
                        
                    }
                }
                return currentRegion;
            }

            private set
            {
                currentRegion = value;
                //lock (ob_SetConfig)
                //{
                //    ConfigDefined = true;
                //}
            }
        }
        #endregion

        #region GetLanguageConfig 映射语言配置信息到枚举
        private void GetLanguageConfig(string strConfig)
        {
            if (string.IsNullOrEmpty(strConfig))
            {
                return;
            }

            switch (strConfig)
            {
                case "zh-CN":
                    CurrentLanguage = LanguageCultureEnum.zh_CN;
                    break;
                case "en-US":
                    CurrentLanguage = LanguageCultureEnum.en_US;
                    break;
                case "ja-JP":
                    CurrentLanguage = LanguageCultureEnum.ja_JP;
                    break;
                default:
                    CurrentLanguage = LanguageCultureEnum.zh_CN;
                    break;
            }
        }
        #endregion

        #region GetLanguageConfig 映射站点配置信息到枚举
        private void GetRegionConfig(string strConfig)
        {
            if (string.IsNullOrEmpty(strConfig))
            {
                return;
            }

            switch (strConfig)
            {
                case "CN":
                    CurrentRegion = RegionTypeEnum.CN;
                    break;
                case "JP":
                    CurrentRegion = RegionTypeEnum.JP;
                    break;
                default:
                    CurrentRegion = RegionTypeEnum.CN;
                    break;
            }
        }
        #endregion

        /// <summary>
        /// engine语言配置文件
        /// </summary>
        private string LocalConfigFilePath { get; set; } = "../../../Config/Config.json";

        #region  读取语言站点环境配置
        /// <summary>
        /// 读取语言站点环境配置
        /// </summary>
        public void ReadLocalConfig()
        {

            //GetLanguageConfig("ja-JP");
            //GetRegionConfig("JP");
            //ConfigDefined = true;
            //return;

            try
            {
                try
                {
                    logger.LogInformation($"LocalConfigFilePath: {Path.GetFullPath(LocalConfigFilePath)}");
                }
                catch { }

                if (string.IsNullOrEmpty(LocalConfigFilePath) || !File.Exists(LocalConfigFilePath))
                {
                    logger.LogError($"ReadLanguageConfig fail. Path:{LocalConfigFilePath}");
                    return;
                }

                using (StreamReader reader = new StreamReader(LocalConfigFilePath))
                {
                    string JsonString = reader.ReadToEnd();
                    if (!string.IsNullOrEmpty(JsonString))
                    {
                        JObject jobj = (JObject)JsonConvert.DeserializeObject(JsonString);
                        if (jobj != null)
                        {
                            string language = jobj["useLang"].ToString();
                            string region = jobj["region"].ToString();
                            GetLanguageConfig(language);
                            GetRegionConfig(region);
                            ConfigDefined = true;
                        }
                    }
                    else
                    {
                        logger.LogInformation($"Fail to read lang and region config Json");
                    }
                }
            }
            catch (Exception ex)
            {
                ConfigDefined = false;
                logger.LogInformation($"Fail to read lang and region config Json. {ex.ToString()}");
                CurrentLanguage = LanguageCultureEnum.zh_CN;
                CurrentRegion = RegionTypeEnum.CN;
            }
            finally
            {

            }
        }

        private string GetFullPath(string strRelativePath)
        {
            string strFullName = string.Empty;
            FileInfo fileInfo = new FileInfo(strRelativePath);
            strFullName = fileInfo.FullName;
            return string.IsNullOrEmpty(strFullName) ? strRelativePath : strFullName;
        }
        #endregion

    }
}
