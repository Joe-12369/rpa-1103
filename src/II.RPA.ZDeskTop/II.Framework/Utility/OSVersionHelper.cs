using II.Framework.PInvoker;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.Framework.Utility
{
    public static class OSVersionHelper
    {
        private static Dictionary<string, object> s_VersionObjectDictionary = null;
        public static Dictionary<string, object> VersionObjectDictionary
        {
            get
            {
                if (s_VersionObjectDictionary == null)
                {
                    s_VersionObjectDictionary = WMIHelper.ExecuteWqlObjectQuery("SELECT * FROM Win32_OperatingSystem");
                }
                return s_VersionObjectDictionary;
            }
        }
        public static string FullOSVersionInfo
        {
            get
            {
                return $"{VersionObjectDictionary["Caption"]} - {VersionObjectDictionary["OSArchitecture"]} - {VersionObjectDictionary["Version"]}";
            }
        }
        public static string OSVersionCaption
        {
            get
            {
                return $"{VersionObjectDictionary["Caption"]}";
            }
        }
        public static string OSVersionInner
        {
            get
            {
                return $"{VersionObjectDictionary["Version"]}";
            }
        }
        public static bool IsWin10CreatorsOrHigher
        {
            get
            {
                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                {
                    return false;
                }
                RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion");
                Version version = new Version(string.Concat(registryKey.GetValue("CurrentVersion"), ".", registryKey.GetValue("CurrentBuild")));
                return version >= new Version(6, 3, 15063);
            }
        }
        public static bool IsWin8OrWS2012OrHigher
        {
            get
            {
                OperatingSystem oSVersion = Environment.OSVersion;
                if (oSVersion.Platform != PlatformID.Win32NT)
                {
                    return false;
                }
                return oSVersion.Version >= new Version(6, 2, 9200);
            }
        }
        public static bool IsHomeEdition
        {
            get
            {
                return PowerOSVersionHelper.Edition.ToLower().Contains("home");
            }
        }
        public static bool IsWindows11()
        {
            // 使用托管 Environment.OSVersion 而非 RtlGetVersion P/Invoke：
            // 原实现的结构体只有 5 个字段（20 字节），而原生 OSVERSIONINFOEX 为 284 字节，
            // RtlGetVersion 会写入完整结构导致 264 字节越界、破坏托管内存，
            // 引发 .NET 10 x86 下的确定性启动崩溃（0xC0000005）。
            // .NET Core 起 Environment.OSVersion 在 Windows 上返回真实系统版本，不受清单影响。
            var version = Environment.OSVersion.Version;
            return version.Major == 10 &&
                   version.Build >= 22000;
        }
        [DllImport("ShlwApi.dll", CharSet = CharSet.Auto)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern bool IsOS(OS os);

        private static bool? IsSupported = null;
        public static bool IsSupportedOsVersion()
        {
            if (IsSupported == null)
            {
                IsSupported = IsSupportedOsVersionCore();
            }
            return IsSupported.Value;
        }
        private static bool IsSupportedOsVersionCore()
        {
            PlatformID platform = Environment.OSVersion.Platform;
            Version curOsVersion = Environment.OSVersion.Version;
            if (platform == PlatformID.Win32NT && curOsVersion >= new Version(6, 2, 9200, 0))
            {
                return true;
            }
            if ((bool)IsOS(OS.OS_ANYSERVER) && (curOsVersion.Major > 6 || (curOsVersion.Major == 6 && curOsVersion.Minor >= 2)))
            {
                return true;
            }
            return false;
        }
    }
    public enum OS : uint
    {
        OS_WINDOWS = 0u,
        OS_NT = 1u,
        OS_WIN95ORGREATER = 2u,
        OS_NT4ORGREATER = 3u,
        OS_WIN98ORGREATER = 5u,
        OS_WIN98_GOLD = 6u,
        OS_WIN2000ORGREATER = 7u,
        OS_WIN2000PRO = 8u,
        OS_WIN2000SERVER = 9u,
        OS_WIN2000ADVSERVER = 10u,
        OS_WIN2000DATACENTER = 11u,
        OS_WIN2000TERMINAL = 12u,
        OS_EMBEDDED = 13u,
        OS_TERMINALCLIENT = 14u,
        OS_TERMINALREMOTEADMIN = 15u,
        OS_WIN95_GOLD = 16u,
        OS_MEORGREATER = 17u,
        OS_XPORGREATER = 18u,
        OS_HOME = 19u,
        OS_PROFESSIONAL = 20u,
        OS_DATACENTER = 21u,
        OS_ADVSERVER = 22u,
        OS_SERVER = 23u,
        OS_TERMINALSERVER = 24u,
        OS_PERSONALTERMINALSERVER = 25u,
        OS_FASTUSERSWITCHING = 26u,
        OS_WELCOMELOGONUI = 27u,
        OS_DOMAINMEMBER = 28u,
        OS_ANYSERVER = 29u,
        OS_WOW6432 = 30u,
        OS_WEBSERVER = 31u,
        OS_SMALLBUSINESSSERVER = 32u,
        OS_TABLETPC = 33u,
        OS_SERVERADMINUI = 34u,
        OS_MEDIACENTER = 35u,
        OS_APPLIANCE = 36u
    }
}
