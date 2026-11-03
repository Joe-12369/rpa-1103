using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;

namespace II.RPA.Plugin.Core.PluginBase
{
    public abstract class ConfigurationBase<T> : INeedReset where T : ConfigurationBase<T>
    {
        private static ILogger logger = Logger.OfContext();
        private static readonly object _lockObj = new object();
        private static T _Configurations = null;

        protected ConfigurationBase()
        {

        }
        public void Reset(bool isSamePackage)
        {
            if (isSamePackage)
            {
                return;
            }
            NeedResetObjectManager.RemoveNeedResetObject(_Configurations);
            _Configurations = null;
            _ = Info;
        }
        public static T Info
        {
            get
            {
                if (_Configurations == null)
                {
                    lock (_lockObj)
                    {
                        if (_Configurations == null)
                        {
                            LoadConfigInfo();
                            if (_Configurations != null)
                            {
                                NeedResetObjectManager.AddNeedResetObject(_Configurations);
                            }
                        }
                    }
                }
                return _Configurations;
            }
        }
        #region 公共属性
        [JsonProperty("fileName")]
        public string FileName { get; set; }
        #endregion

        private static void LoadConfigInfo()
        {
            string pluginName = Directory.GetParent(typeof(T).Assembly.Location).Name;
            try
            {
                logger.LogDebug($"开始加载插件配置文件：{pluginName}");
                string configJsonText = PluginConfiguration.ConfigFileManager.Instance.TryGetPluginConfigJsonTextForLoad(pluginName);
                if (string.IsNullOrEmpty(configJsonText))
                {
                    _Configurations = default(T);
                    return;
                }
                _Configurations = configJsonText.ToObject<T>();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"插件配置文件加载异常：{pluginName}");
                _Configurations = "{}".ToObject<T>();
            }
            logger.LogDebug($"完成加载插件配置文件：{pluginName}");
        }


    }
}

