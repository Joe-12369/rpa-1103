using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Plugin.Core;

namespace II.RPA.Core.Common
{
    internal enum WmiType
    {
        Win32_Processor,
        Win32_PerfFormattedData_PerfOS_Memory,
        Win32_PhysicalMemory,
        Win32_NetworkAdapterConfiguration,
        Win32_LogicalDisk
    }

    /// <summary>
    /// 获取操作系统、硬件参数
    /// </summary>
    public class SystemInfoUtility
    {
        static readonly Dictionary<string, ManagementObjectCollection> m_WmiDict = new Dictionary<string, ManagementObjectCollection>();
        static PowerSavingMonitor _powerMonitor;
        static SystemInfoUtility()
        {
            try
            {
                var names = Enum.GetNames(typeof(WmiType));
                foreach (string name in names)
                {
                    m_WmiDict.Add(name, new ManagementObjectSearcher("SELECT * FROM " + name).Get());
                }
            }
            catch
            {
                // 
            }
        }

        public static event EventHandler<PowerSavingStateChangedEventArgs> OnPowerSavingStateChangedEvent;

        public static void StartPowerSavingMonitor()
        {
            _powerMonitor = PowerSavingMonitor.CreateWithBackgroundMessageLoop();
            _powerMonitor.PowerSavingStateChanged += (s, e) => OnPowerSavingStateChangedEvent?.Invoke(s, e);
        }

        public static bool IsPowerSaverActive => _powerMonitor.IsPowerSaverActive;

        public static void StopPowerSavingMonitor()
        {
            _powerMonitor.Dispose();
        }

        /// <summary>
        /// 获取硬盘号码
        /// </summary>
        /// <returns></returns>
        public static string GetHardDiskNumber()
        {
            try
            {
                var query = m_WmiDict[WmiType.Win32_LogicalDisk.ToString()];
                //var collection = query.Get();

                string result = string.Empty;
                foreach (var obj in query)
                {
                    result = obj["VolumeSerialNumber"].ToString();
                    break;
                }

                return result;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取CPU号码
        /// </summary>
        /// <returns></returns>
        public static string GetCPUNumber()
        {
            try
            {
                var query = m_WmiDict[WmiType.Win32_Processor.ToString()];
                //var collection = query.Get();

                string result = string.Empty;
                foreach (var obj in query)
                {
                    result = obj["Processorid"].ToString();
                    break;
                }

                return result;
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// 获取内存编号
        /// </summary>
        /// <returns></returns>
        public static string GetMemoryNumber()
        {
            try
            {
                var query = m_WmiDict[WmiType.Win32_PhysicalMemory.ToString()];
                //var collection = query.Get();

                string result = string.Empty;
                foreach (var obj in query)
                {
                    result = obj["PartNumber"].ToString();
                    break;
                }
                return result;
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// 获取硬盘信息
        /// </summary>
        /// <returns></returns>
        public static string HardDiskInfo()
        {
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                StringBuilder sr = new StringBuilder();
                foreach (DriveInfo drive in drives)
                {
                    if (drive.IsReady)
                    {
                        var val1 = (double)drive.TotalSize / 1024 / 1024;
                        var val2 = (double)drive.TotalFreeSpace / 1024 / 1024;
                        sr.AppendFormat("{0}:{2}/{3}MB/{4}MB/{1}% Available;",
                            drive.Name,
                            string.Format("{0:F2}", val2 / val1 * 100),
                            drive.DriveFormat,
                            (long)val1,
                            (long)val2);
                    }
                }
                return sr.ToString();
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// 获取操作系统信息
        /// 这里获取到的操作系统版本是NT版本号
        /// Windows NT6.1-Windows 7
        /// Windows NT6.2-Windows 8
        /// Windows NT6.3-Windows 8.1
        /// Windows NT6.4、NT10.0-Windows 10、11
        /// windows10之后的NT版本号比较乱、有的6.4、有的是10.0
        /// windows11的主版本号和windows10一致
        /// </summary>
        /// <returns></returns>
        public static string OSInfo()
        {
            try
            {
                StringBuilder sr = new StringBuilder();
                sr.AppendFormat($"机器名:{Environment.MachineName};" +
                    $"{Environment.NewLine}操作系统:{Environment.OSVersion};" +
                    $"{Environment.NewLine}系统文件夹:{Environment.SystemDirectory};" +
                    $"{Environment.NewLine}语言:{CultureInfo.InstalledUICulture.EnglishName};" +
                    $"{Environment.NewLine}.NET CLR:{Environment.Version};" +
                    $"{Environment.NewLine}当前目录:{Environment.CurrentDirectory};" +
                    $"{Environment.NewLine}当前用户:{Environment.UserName};");
                return sr.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取网卡信息
        /// </summary>
        /// <returns></returns>
        public static string NetworkInfo()
        {
            try
            {
                StringBuilder sr = new StringBuilder();

                string host = Dns.GetHostName();
                IPHostEntry ipEntry = Dns.GetHostEntry(host);
                sr.Append("IPv4:" + ipEntry.AddressList[0] + "/");

                sr.Append("IPv6:");
                ipEntry = Dns.GetHostEntry(host);
                sr.Append("IPv6:" + ipEntry.AddressList[0] + ";");

                sr.Append("MAC:");
                var query = m_WmiDict[WmiType.Win32_NetworkAdapterConfiguration.ToString()];
                foreach (var obj in query)
                {
                    if (obj["IPEnabled"].ToString() == "True")
                        sr.Append(obj["MacAddress"] + ";");
                }

                return sr.ToString();
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// 获取内存信息
        /// </summary>
        /// <returns></returns>
        public static string MemoryInfo()
        {
            try
            {
                StringBuilder sr = new StringBuilder();
                long capacity = 0;
                var query = m_WmiDict[WmiType.Win32_PhysicalMemory.ToString()];
                int index = 1;
                foreach (var obj in query)
                {
                    sr.Append("内存" + index + "频率:" + obj["ConfiguredClockSpeed"] + ";");
                    capacity += Convert.ToInt64(obj["Capacity"]);
                    index++;
                }
                sr.Append("总物理内存:");
                sr.Append(capacity / 1024 / 1024 + "MB;");

                query = m_WmiDict[WmiType.Win32_PerfFormattedData_PerfOS_Memory.ToString()];
                sr.Append("总可用内存:");
                long available = 0;
                foreach (var obj in query)
                {
                    available += Convert.ToInt64(obj.Properties["AvailableMBytes"].Value);
                }
                sr.Append(available + "MB;");
                sr.AppendFormat("{0:F2}%可用; ", (double)available / (capacity / 1024 / 1024) * 100);

                return sr.ToString();
            }
            catch
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// 获取CPU信息
        /// </summary>
        /// <returns></returns>
        public static string CpuInfo()
        {
            try
            {
                StringBuilder sr = new StringBuilder();

                var query = m_WmiDict[WmiType.Win32_Processor.ToString()];
                foreach (var obj in query)
                {
                    sr.Append("厂商:" + obj["Manufacturer"] + ";");
                    sr.Append("产品名称:" + obj["Name"] + ";");
                    sr.Append("最大频率:" + obj["MaxClockSpeed"] + ";");
                    sr.Append("当前频率:" + obj["CurrentClockSpeed"] + ";");
                }

                return sr.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
        public static void Dispose()
        {
            try
            {
                m_WmiDict.ForEach(x => x.Value?.Dispose());
            }
            catch
            {
                //ignore
            }
        }

        #region Power Management

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetSystemPowerStatus(ref SYSTEM_POWER_STATUS sps);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
        }

        /// <summary>
        /// 获取系统是否进入节能模式
        /// 该方法不需要管理员权限
        /// </summary>
        /// <returns>true表示系统处于节能模式，false表示系统正常运行</returns>
        public static bool IsSystemInPowerSaveMode()
        {
            try
            {
                SYSTEM_POWER_STATUS sps = new SYSTEM_POWER_STATUS();
                if (GetSystemPowerStatus(ref sps))
                {
                    // SystemStatusFlag: 0=无节能模式, 1=节能模式开启
                    // 当值为1时，表示系统已进入节能模式
                    return sps.SystemStatusFlag == 1;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}