using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace II.RPA.Plugin.Core
{
    public class PluginInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("type")]
        public string PluginType { get; set; }
        [JsonProperty("InitOrder")]
        public int InitOrder
        {
            get
            {
                switch (Name)
                {
                    case "CommonDLLBase":
                        return 100;
                    case "Element":
                        return 200;
                    default:
                        return 0;
                }
            }
        } //大于0时 默认加载插件 并且会按照值 从低到高 依次加载能力并进行插件初始化
        /// <summary>
        /// 插件文件所在的目录
        /// </summary>
        [JsonIgnore]
        public string PluginDirectoryPath { get; set; }
        /// <summary>
        /// 程序集文件地址；未显式赋值时根据 ConfigFilePath、PluginDirectoryPath 惰性解析。
        /// </summary>
        [JsonIgnore]
        public string AssemblyFilePath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_assemblyFilePath))
                {
                    return _assemblyFilePath;
                }
                if (_assemblyFilePathResolved)
                {
                    return _assemblyFilePath;
                }
                _assemblyFilePathResolved = true;
                ResolveAssemblyFilePath();
                return _assemblyFilePath;
            }
            set
            {
                _assemblyFilePath = value;
                _assemblyFilePathResolved = !string.IsNullOrWhiteSpace(value);
            }
        }

        private string _assemblyFilePath;
        private bool _assemblyFilePathResolved;
        /// <summary>
        /// config.json文件所在的目录
        /// </summary>
        [JsonIgnore]
        public string ConfigFilePath { get; set; }
        /// <summary>
        /// config文件对应的JObject信息，在存储至依赖包内的configuration对象后清空
        /// </summary>
        [JsonIgnore]
        public JObject ConfigJObject { get; set; }
        /// <summary>
        /// 程序集
        /// </summary>
        [JsonIgnore]
        public Assembly Assembly { get; set; }

        /// <summary>
        /// 插件是否已初始化
        /// </summary>
        [JsonIgnore]
        public bool HasInit { get; set; }

        /// <summary>
        /// 能力类型字典
        /// </summary>
        [JsonIgnore]
        public RPAConcurrentDictionary<string, Type> AbilityTypeMap { get; set; }
        public PluginInfo()
        {
            Assembly = null;
            AbilityTypeMap = new RPAConcurrentDictionary<string, Type>();
        }

        private void ResolveAssemblyFilePath()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ConfigFilePath) || string.IsNullOrWhiteSpace(PluginDirectoryPath))
                {
                    return;
                }
                string pluginConfigJson = File.ReadAllText(ConfigFilePath);
                JObject configJObject = JObject.Parse(pluginConfigJson);
                ConfigJObject = configJObject;
                string assemblyPath = Path.Combine(PluginDirectoryPath, configJObject.GetValue<string>("fileName"));
                if (!string.IsNullOrWhiteSpace(assemblyPath) && File.Exists(assemblyPath))
                {
                    _assemblyFilePath = assemblyPath;
                }
            }
            catch
            {
            }
        }
    }
}
