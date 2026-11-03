using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace II.RPA.Plugin.Browsers.Chromium.DownloaderBasic
{
    public class ChromiumDownloader : ElementDownloader
    {
        public BrowserTabPage TabPage;
        public override eGuiType GuiType => eGuiType.Chromium;
        public override Browser Browser => TabPage.Browser;
        public ChromiumDownloader(BrowserTabPage tabPage) : base(tabPage)
        {
            TabPage = tabPage;
        }

        public override string GetBrowserPreferencesPath()
        {
            try
            {
                string processName = this.Browser.BrowserProcessName;
                // 创建WMI查询
                string wmiQuery = $"SELECT * FROM Win32_Process WHERE Caption LIKE '%{processName}%'";
                // 使用ManagementObjectSearcher执行查询
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);
                // 遍历查询结果
                foreach (ManagementObject process in searcher.Get())
                {
                    string caption = Path.GetFileNameWithoutExtension(process["Caption"].ToString());
                    if (caption == processName)
                    {
                        string userDataDir = ExtractUserDataDir(process["CommandLine"].ToString());
                        if (!string.IsNullOrEmpty(userDataDir))
                        {
                            return Path.Combine(userDataDir, "Default\\Preferences");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"使用WMI获取进程启动参数异常，获取地址失败,这可能和环境、权限有关，此处属于chromium类浏览器的公共支持方法，此处不能确保100%成功");
            }
            return null;
        }

        string ExtractUserDataDir(string executablePath)
        {
            // 这里需要根据实际的Chrome启动参数来解析User Data目录
            // 由于Chrome的启动参数可能包含引号和空格，解析可能会比较复杂
            // 这里只是一个简单的示例，实际情况可能需要更复杂的解析逻辑
            int start = executablePath.IndexOf("--user-data-dir=");
            if (start != -1)
            {
                start = start + "--user-data-dir=".Length;
                int end = executablePath.IndexOf("User Data\"", start);
                if (end == -1) { return null; }
                end = end + "User Data\"".Length;
                if (end != -1)
                {
                    return executablePath.Substring(start, end - start).Trim('"');
                }
            }
            return null;
        }
    }
}

