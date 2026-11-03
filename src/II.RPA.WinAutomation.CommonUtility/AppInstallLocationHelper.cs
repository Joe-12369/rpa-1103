using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility
{
    public class AppInstallLocationHelper
    {
        #region GetSoftWare  判断软件是否安装 并输出安装目录
        /// <summary>
        /// 判断软件是否安装 并输出安装目录（文件夹路径 非启动路径 启动路径可在此基础上进行拼接）
        /// </summary>
        /// <param name="SoftWareName"> 软件名称</param>
        /// <param name="SoftWarePath "> 安装路径</param>
        /// <returns> true or false </returns>
        public static bool GetSoftWare(string _SoftWareName, out string _SoftWarePath)
        {
            _SoftWarePath = null;
            try
            {
                List<RegistryKey> RegistryKeys = new List<RegistryKey>();
                RegistryKeys.Add(Registry.ClassesRoot);
                RegistryKeys.Add(Registry.CurrentConfig);
                RegistryKeys.Add(Registry.CurrentUser);
                RegistryKeys.Add(Registry.LocalMachine);
                RegistryKeys.Add(Registry.PerformanceData);
                RegistryKeys.Add(Registry.Users);
                Dictionary<string, string> Softwares = new Dictionary<string, string>();
                string SubKeyName = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
                foreach (RegistryKey Registrykey in RegistryKeys)
                {
                    using (RegistryKey RegistryKey1 = Registrykey.OpenSubKey(SubKeyName, false))
                    {
                        if (RegistryKey1 == null) // 判断对象不存在
                            continue;
                        if (RegistryKey1.GetSubKeyNames() == null)
                            continue;
                        string[] KeyNames = RegistryKey1.GetSubKeyNames();
                        foreach (string KeyName in KeyNames)// 遍历子项名称的字符串数组
                        {
                            using (RegistryKey RegistryKey2 = RegistryKey1.OpenSubKey(KeyName, false)) // 遍历子项节点
                            {
                                if (RegistryKey2 == null)
                                    continue;
                                string SoftwareName = RegistryKey2.GetValue("DisplayName", "").ToString(); // 获取软件名
                                string InstallLocation = RegistryKey2.GetValue("InstallLocation", "").ToString(); // 获取安装路径
                                if (!string.IsNullOrEmpty(InstallLocation)
                                 && !string.IsNullOrEmpty(SoftwareName))
                                {
                                    if (!Softwares.ContainsKey(SoftwareName))
                                        Softwares.Add(SoftwareName, InstallLocation);
                                }
                            }
                        }
                    }
                }
                if (Softwares.Count <= 0)
                    return false;
                foreach (string SoftwareName in Softwares.Keys)
                {
                    if (SoftwareName.Contains(_SoftWareName))
                    {
                        _SoftWarePath = Softwares[_SoftWareName];
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                //输出日志
            }

            return false;
        }
        #endregion

        #region GetAppStartUpPath  获取软件的启动路径
        /// <summary>
        /// 获取软件的启动路径
        /// </summary>
        /// <param name="_AppDisplayName">软件名称 比如：360安全浏览器</param>
        /// <param name="_StartUpAppName">软件启动文件名称 比如：360se.exe 或者启动路径与软件安装目录相差的部分</param>
        /// <param name="_StartUpAppPath">软件完整启动路径</param>
        /// <returns></returns>
        public static bool GetAppStartUpPath(string _AppDisplayName, string _StartUpAppName, ref string _StartUpAppPath)
        {
            _StartUpAppPath = string.Empty;
            string _SoftWarePath = string.Empty;
            if (!GetSoftWare(_AppDisplayName, out _SoftWarePath))
            {
                return false;
            }
            else
            {
                _StartUpAppPath = Path.Combine(_SoftWarePath, _StartUpAppName);
                if (File.Exists(_StartUpAppPath))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
