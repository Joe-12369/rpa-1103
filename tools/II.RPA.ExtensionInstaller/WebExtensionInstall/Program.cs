using System;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace WebExtensionInstall
{
    internal class Program
    {
        public static string GetProcessOwner(int processId)
        {
            try
            {
                foreach (ManagementObject obj in new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID = " + processId).Get())
                {
                    string[] argList = new string[2]
                    {
                    string.Empty,
                    string.Empty
                    };
                    object[] args = argList;
                    if (Convert.ToInt32(obj.InvokeMethod("GetOwner", args)) == 0)
                    {
                        return argList[0];
                    }
                }
            }
            catch (Exception e)
            {
                Common.LogError(e);
                return "NO OWNER";
            }
            return "NO OWNER";
        }
        public static Process[] GetProcessesByName(string processName, bool onlyCurrentUser)
        {
            try
            {
                if (!onlyCurrentUser)
                {
                    return Process.GetProcessesByName(processName);
                }
                return (from m in Process.GetProcessesByName(processName)
                        where GetProcessOwner(m.Id) == Environment.UserName
                        select m).ToArray();
            }
            catch (Exception e)
            {
                Common.LogError(e);
            }
            return null;
        }
        private static void KillPrevHost()
        {
            Process[] processesByName = GetProcessesByName("WebExtensionInstall", onlyCurrentUser: true);
            int curPid = Process.GetCurrentProcess().Id;
            Process[] array = processesByName;
            foreach (Process process in array)
            {
                if (process.Id != curPid)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            var methodName = args[0].Split('=')[1];
            //var methodName = "DetectChromiumBrowser";
            switch (methodName)
            {
                case "IsInstallChromeExtension":
                    ExtensionInstall.IsInstallChromeExtension();
                    break;
                case "InstallChromeExtension":
                    ExtensionInstall.InstallChromeExtension();
                    break;
                case "IsInstallSogouExtension":
                    ExtensionInstall.IsInstallSogouExtension();
                    break;
                case "InstallSogouExtension":
                    ExtensionInstall.InstallSogouExtension();
                    break;
                case "IsInstallFireFoxExtension":
                    ExtensionInstall.IsInstallFireFoxExtension();
                    break;
                case "InstallFireFoxExtension":
                    ExtensionInstall.InstallFireFoxExtension();
                    break;
                case "IsInstallEdgeExtension":
                    ExtensionInstall.IsInstallEdgeExtension();
                    break;
                case "InstallEdgeExtension":
                    ExtensionInstall.InstallEdgeExtension();
                    break;
                case "IsInstall360seExtension":
                    ExtensionInstall.IsInstall360seExtension();
                    break;
                case "Install360seExtension":
                    ExtensionInstall.Install360seExtension();
                    break;
                case "IsInstall360XExtension":
                    ExtensionInstall.IsInstall360XExtension();
                    break;
                case "Install360XExtension":
                    ExtensionInstall.Install360XExtension();
                    break;
                case "CloseChromeBrowser":
                    ExtensionInstall.CloseChromeBrowser();
                    break;
                case "CloseSogouBrowser":
                    ExtensionInstall.CloseSogouBrowser();
                    break;
                case "CloseFirefoxBrowser":
                    ExtensionInstall.CloseFirefoxBrowser();
                    break;
                case "CloseEdgeBrowser":
                    ExtensionInstall.CloseEdgeBrowser();
                    break;
                case "Close360seBrowser":
                    ExtensionInstall.Close360seBrowser();
                    break;
                case "Close360XBrowser":
                    ExtensionInstall.Close360XBrowser();
                    break;
                case "IsInstallSuperBrowserExtension":
                    ExtensionInstall.IsInstallSuperBrowserExtension();
                    break;
                case "InstallSuperBrowserExtension":
                    ExtensionInstall.InstallSuperBrowserExtension();
                    break;
                case "CloseSuperBrowser":
                    ExtensionInstall.CloseSuperBrowser();
                    break;
                case "DetectChromiumBrowser":
                    ExtensionInstall.DetectChromiumBrowser(args);
                    break;
                case "IsChromiumBrowser":
                    ExtensionInstall.IsChromiumBrowser(args);
                    break;
                case "InstallChromiumExtension":
                    ExtensionInstall.InstallChromiumExtension(args);
                    break;
                case "CloseChromiumBrowser":
                    ExtensionInstall.CloseChromiumBrowser(args);
                    break;
                default:
                    Common.LogError(new ArgumentException(string.Join(" ", args)));
                    return;
            }
        }
    }
}
