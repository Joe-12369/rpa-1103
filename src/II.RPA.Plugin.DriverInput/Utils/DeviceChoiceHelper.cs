using II.RPA.Logging;
using II.RPA.Plugin.DriverInput.Config;
using InputInterceptorNS;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DriverInput.Utils
{
    public class DeviceChoiceHelper
    {
        private static ILogger logger = Logger.OfContext();
        public static void RecordDeviceList(List<DeviceData> deviceDatas)
        {
            string DeviceInfo = string.Empty;
            deviceDatas.ForEach(item =>
            {
                string nameStr = string.Empty;
                item.Names.ForEach(name =>
                {
                    nameStr += $"{name} ";
                });
                //DeviceInfo+=($"\r\n参数CompositeName:{item.CompositeName},  参数Device:{item.Device}, 参数Names------( 数量:{item.Names.Count} 名称拼接:{nameStr})");
            DeviceInfo += $"\r\n{item.CompositeName.Replace(@"\",@"/")}";
        });
            logger.LogInformation(DeviceInfo);
        }

        public static string ReadConfig()
        {
            string fileName = "DeviceConfig.txt";
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string directoryPath = System.IO.Path.GetDirectoryName(assemblyPath);
            string ConfigPath = Path.Combine(directoryPath, fileName);
            logger.LogInformation($"config:{ConfigPath}");
            if (File.Exists(ConfigPath))
            {
                string jsonContent = File.ReadAllText(ConfigPath);
                return jsonContent;
            }
            else
            {
                return "";
            }
        }

        public static int GetDeviceIndex(List<DeviceData> deviceDatas)
        {
            string configContent = Configurations.Info.CompositeName;
            logger.LogDebug($"Read CompositeName:{configContent}");
            if (string.IsNullOrEmpty(configContent) || deviceDatas.Count < 1)
            {
                return -1;
            }

            for (int index=0; index< deviceDatas.Count; index++)
            {
                logger.LogDebug($"CompositeName:{deviceDatas[index].CompositeName}; Read CompositeName:{configContent}");
                if (deviceDatas[index].CompositeName.Replace(@"\", @"/").Contains(configContent)) 
                {
                    return index;
                }
            }
            
            return -1;
        }

        public static int GetDeviceIndex(List<DeviceData> deviceDatas, string targetDeviceName)
        {
            if (string.IsNullOrEmpty(targetDeviceName) || deviceDatas.Count < 1)
            {
                return -1;
            }

            if (targetDeviceName == "默认")
            {
                return 0;
            }

            for (int index = 0; index < deviceDatas.Count; index++)
            {
                logger.LogDebug($"CompositeName:{deviceDatas[index].CompositeName}; Receive CompositeName:{targetDeviceName}");
                if (deviceDatas[index].CompositeName.Replace(@"\", @"/").Contains(targetDeviceName))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
