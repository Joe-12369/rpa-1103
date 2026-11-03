using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using II.RPA.Core.Server.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Interception.Utilities;

namespace II.RPA.Core.Server.Utilities
{
    public class EndPointProvider : IEndPointProvider
    {
        private static List<int> PortInUsePool = new List<int>();
        private const string PortReleaseGuid = "CE068CFE-E3C6-4A72-B19A-E2743E2B08C6";
        private readonly string m_BootSettingFile = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\boot.json";
        private readonly ILogger m_Logger;
        private List<BootSetting> m_BootSettings = null;
        public EndPointProvider(ILogger aLogger)
        {
            m_Logger = aLogger;

        }
        public void Initial(IEnumerable<IServiceBoot> aServiceBoots)
        {
            try
            {
                m_BootSettings = ReadBootSetting(m_BootSettingFile);
                aServiceBoots.ForEach(serviceBoot =>
                {
                    var bootSetting = m_BootSettings.FirstOrDefault(bs => bs.Client.Equals(CommandArgHelper.GetArgumentValue(CommonConstants.CLIENTID.KEY)));
                    if (bootSetting == null)
                    {
                        bootSetting = new BootSetting
                        {
                            Client = CommandArgHelper.GetArgumentValue(CommonConstants.CLIENTID.KEY),
                            Boots = new List<Boot>
                            {
                            new Boot
                            {
                                BootName = serviceBoot.BootName,
                                IP = "localhost",
                                Port = serviceBoot.Port
                            }
                            }

                        };
                        m_BootSettings.Add(bootSetting);
                    }
                    else
                    {
                        if (bootSetting.Boots == null || !bootSetting.Boots.Any())
                        {
                            bootSetting.Boots = new List<Boot>
                            {
                            new Boot
                            {
                                BootName = serviceBoot.BootName,
                                IP = "localhost",
                                Port = serviceBoot.Port
                            }
                            };
                        }
                        else
                        {
                            var currentBoot = bootSetting.Boots.FirstOrDefault(boot => boot.BootName == serviceBoot.BootName);
                            if (currentBoot == null)
                            {
                                bootSetting.Boots.Add(new Boot
                                {
                                    BootName = serviceBoot.BootName,
                                    IP = "localhost",
                                    Port = serviceBoot.Port,
                                });
                            }
                            else
                            {
                                currentBoot.Port = serviceBoot.Port;
                            }
                        }
                    }
                });
            }
            catch (Exception ex) 
            {
                m_Logger.LogError("EndPointProvider Initial:", ex);
            }
        }

        public void Update(string aClient, string aBootName, int aPort)
        {
            var bootSetting = m_BootSettings.FirstOrDefault(bs=>bs.Client == aClient);
            if (bootSetting != null)
            {
                bootSetting.Boots?.ForEach(b=>
                {
                    if (b.BootName == aBootName)
                    {
                        b.Port = aPort;
                    }
                });
                WriteBootSetting(m_BootSettings, m_BootSettingFile);
            }
        }
        private List<BootSetting> ReadBootSetting(string aSettingFile)
        {
            if (File.Exists(aSettingFile))
            {
                string jsonStr = File.ReadAllText(aSettingFile);
                if (string.IsNullOrEmpty(jsonStr))
                {
                    return new List<BootSetting>();
                }
                else
                {
                    m_Logger.LogDebug($"load boot json: {jsonStr}");
                    var bootSetttings = JsonConvert.DeserializeObject<List<BootSetting>>(jsonStr);
                    bootSetttings.Select(bs => bs.Boots.Select(b => b.Port)).ForEach(ports =>
                    {
                        PortInUsePool.AddRange(ports);
                    });
                    return bootSetttings;
                }
            }
            else
            {
                m_Logger.LogInfo($"The boot json file: {aSettingFile} does not exist.");
                return new List<BootSetting>();
            }
        }
        private void WriteBootSetting(List<BootSetting> bootSettings, string aSettingFile)
        {
            try
            {
                if (File.Exists(aSettingFile))
                {
                    File.Delete(aSettingFile);
                }
                var directory = Path.GetDirectoryName(aSettingFile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                using (var fileStream = File.Open(aSettingFile, FileMode.OpenOrCreate))
                {
                    using (var sw = new StreamWriter(fileStream))
                    {
                        using (var jw = new JsonTextWriter(sw))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Serialize(jw, bootSettings);
                            m_Logger.LogDebug($"update boot json: {JsonConvert.SerializeObject(bootSettings)}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                m_Logger.LogError(e);
            }
        }
        private int FindNextAvailableTCPPort(int startPort = 5000)
        {
            int port = startPort;
            bool isAvailable = true;

            var mutex = new Mutex(false,
                string.Concat("Global/", PortReleaseGuid));
            mutex.WaitOne();
            try
            {
                IPGlobalProperties ipGlobalProperties =
                    IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] endPoints =
                    ipGlobalProperties.GetActiveTcpListeners();
                do
                {
                    if (!isAvailable)
                    {
                        port++;
                        isAvailable = true;
                    }

                    foreach (IPEndPoint endPoint in endPoints)
                    {
                        if (endPoint.Port != port) continue;
                        isAvailable = false;
                        break;
                    }
                    if (PortInUsePool.Contains(port))
                    {
                        isAvailable = false;
                    }

                }
                while (!isAvailable && port < IPEndPoint.MaxPort);

                if (!isAvailable)
                    throw new ApplicationException("Not able to find a free TCP port.");

                PortInUsePool.Add(port);
                return port;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        /// <summary>
        /// 该方法有滞后性、无法用来准确地判断端口是否可用
        /// </summary>
        /// <param name="aPort"></param>
        /// <param name="aType"></param>
        /// <returns></returns>
        private bool IsPortInUse(int aPort, string aType = "tcp")
        {
            bool flag = false;
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipendpoints;
            if (aType.ToLower().Trim() == "tcp")
            {
                ipendpoints = properties.GetActiveTcpListeners();
            }
            else
            {
                ipendpoints = properties.GetActiveUdpListeners();
            }
            foreach (IPEndPoint ipendpoint in ipendpoints)
            {
                if (ipendpoint.Port == aPort)
                {
                    flag = true;
                    break;
                }
            }
            return flag;
        }

        public int GetAvailablePort(int aCurrentInvalidPort, string aBootName)
        {
            PortInUsePool?.Add(aCurrentInvalidPort);
            int availablePort = FindNextAvailableTCPPort(aCurrentInvalidPort + 1);
            RecordBootPort(aBootName, availablePort);
            return availablePort;
        }

        public int GetAvailablePort(int aCurrentInvalidPort)
        {
            PortInUsePool?.Add(aCurrentInvalidPort);
            int availablePort = FindNextAvailableTCPPort(aCurrentInvalidPort + 1);
            return availablePort;
        }

        private void EnsureBootSettingsLoaded()
        {
            if (m_BootSettings == null)
            {
                m_BootSettings = ReadBootSetting(m_BootSettingFile);
            }
        }

        private string GetCurrentClientId()
        {
            return CommandArgHelper.GetArgumentValue(CommonConstants.CLIENTID.KEY);
        }

        private BootSetting GetOrCreateBootSetting()
        {
            EnsureBootSettingsLoaded();
            var clientId = GetCurrentClientId();
            var bootSetting = m_BootSettings.FirstOrDefault(bs =>
                bs.Client != null && bs.Client.Equals(clientId, StringComparison.OrdinalIgnoreCase));
            if (bootSetting == null)
            {
                bootSetting = new BootSetting
                {
                    Client = clientId,
                    Boots = new List<Boot>()
                };
                m_BootSettings.Add(bootSetting);
            }
            if (bootSetting.Boots == null)
            {
                bootSetting.Boots = new List<Boot>();
            }
            return bootSetting;
        }

        private void RecordBootPort(string aBootName, int aPort)
        {
            if (string.IsNullOrEmpty(aBootName))
            {
                return;
            }

            try
            {
                var bootSetting = GetOrCreateBootSetting();
                var boot = bootSetting.Boots.FirstOrDefault(b => b.BootName == aBootName);
                if (boot == null)
                {
                    bootSetting.Boots.Add(new Boot
                    {
                        BootName = aBootName,
                        IP = "localhost",
                        Port = aPort
                    });
                }
                else
                {
                    boot.Port = aPort;
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning($"Record boot port [{aBootName}]={aPort} failed", ex);
            }
        }
    }
}
