using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Common
{
    public static class PluginUtils
    {
        /// <summary>
        /// 组件信息检测
        /// </summary>
        /// <param name="aPackagesPath"></param>
        /// <returns></returns>
        public static Dictionary<string, PluginInfo> GetActivePluginInfo(string aPackagesPath)
        {
            List<string> pluginInfoFiles_temp = LoadPluginInfoFiles(aPackagesPath);
            Dictionary<string, PluginInfo> pluginInfoDic = new Dictionary<string, PluginInfo>();
            foreach (var pluginInfoFile in pluginInfoFiles_temp)
            {
                string pluginInfoJson = File.ReadAllText(pluginInfoFile);
                PluginInfo pulginInfo = pluginInfoJson.ToObject<PluginInfo>();
                pulginInfo.PluginDirectoryPath = Path.GetDirectoryName(pluginInfoFile);
                //避免保存重复版本
                if (!pluginInfoDic.ContainsKey(pulginInfo.Name))
                {
                    pluginInfoDic.Add(pulginInfo.Name, pulginInfo);
                }
                else
                {
                    //ignore
                    //检测到名字重复的组件信息、此处忽略
                }
            }
            return pluginInfoDic;
        }
        public static List<string> LoadPluginInfoFiles(string pluginsPath)
        {
            List<string> jsonFiles = new List<string>();
            if (!Directory.Exists(pluginsPath))
            {
                return jsonFiles;
            }
            var plugDirs = new DirectoryInfo(pluginsPath).GetDirectories();
            foreach (var dir in plugDirs)
            {
                var configFiles = dir.GetFiles("plugin.json", SearchOption.AllDirectories);
                if (configFiles.Length <= 0)
                {
                    configFiles = dir.GetFiles("plugin.json", SearchOption.AllDirectories);
                    if (configFiles.Length > 0)
                    {
                        jsonFiles.Add(configFiles.First().FullName);
                    }
                }
                else
                {
                    jsonFiles.AddRange(configFiles.Select(f => f.FullName));
                }
            }
            return jsonFiles;
        }
        /// <summary>
        /// 
        /// 
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns>version1>=version2 return true，version1 < version2 return false</returns>
        public static bool CompareVersionNumber(string version1, string version2)
        {
            bool version1Newer = true;
            if (version1.Trim() == version2.Trim())
            {
                return true;
            }
            var version1Numbers = Array.ConvertAll(version1.Split('.'), (s) => int.Parse(s));
            var version2Numbers = Array.ConvertAll(version2.Split('.'), (s) => int.Parse(s));
            if (version1Numbers.Length == version2Numbers.Length)
            {
                for (int i = 0; i < version1Numbers.Length; i++)
                {
                    if (version1Numbers[i] > version2Numbers[i])
                    {
                        break;
                    }
                    else if (version1Numbers[i] < version2Numbers[i])
                    {
                        version1Newer = false;
                        break;
                    }
                    if (i == version1Numbers.Length - 1)
                    {
                        version1Newer = false;
                    }
                }
            }
            else if (version1Numbers.Length < version2Numbers.Length)
            {
                version1Newer = false;
            }
            return version1Newer;
        }
        public static bool IsMoblieProject(string aPackagesPath)
        {
            try
            {
                var szrpafileName = Path.Combine(aPackagesPath, "szrpa.json");
                if (File.Exists(szrpafileName))
                {
                    using (FileStream fileStream = File.Open(szrpafileName, FileMode.Open))
                    {
                        using (StreamReader sw = new StreamReader(fileStream))
                        {
                            using (JsonTextReader jw = new JsonTextReader(sw))
                            {
                                JsonSerializer serializer = new JsonSerializer();
                                var jo = serializer.Deserialize<JObject>(jw);
                                var projectMode = jo["projectMode"];
                                if (projectMode != null)
                                {
                                    return projectMode.ToString().Equals("mobile");
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                //ignore
            }
            return false;
        }

    }
}
