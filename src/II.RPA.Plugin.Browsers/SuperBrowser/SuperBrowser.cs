using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ProcessKiller = II.RPA.WebBrowser.Common.Utils.ProcessKiller;

namespace II.RPA.Plugin.Browsers.SuperBrowser
{
    public class SuperBrowser : Browser
    {
        #region 浏览器基础配置参数
        public override string NativeMessageHostProcessName => "II.RPA.NativeMessagingHost";
        public override string BrowserProcessName => "superbrowser";
        protected override string ServiceKey => "U2FsdGVkX19Xagmra6VZ0BB2FitqhBx8";
        protected override string ExtensionsRegistryKey => "Software\\Google\\Chrome\\NativeMessagingHosts";
        protected override string[] ExtensionIDs => new[] { "dkoieojamolcmboailagjcpjeibdiahd", "nfiffhdmjkohbbolbamdboplicimjnpm", "nenolbneempnllhhkenbnlgehjpgndpp" };


        public override eGuiType GuiType => eGuiType.SuperBrowser;
        protected override eErrorMessageType ExtensionUnInstalledErrorMessageType => eErrorMessageType.Error_ChromeExtensionUnInstalled;
        protected override eErrorMessageType ExtensionUnopenErrorMessageType => eErrorMessageType.Error_ChromeExtensionUnopen;
        public override bool IgorneCreateSetting => true;
        public override List<string> IgnoreUrl => new List<string>();
        #endregion
        public const string REG_PATH = "Software\\ZiXun_SuperBrowser\\SuperBrowser";
        public const string REG_PATH_SHELL = "Software\\Classes\\superbrowser\\shell\\open\\command";
        #region 单例 全局点
        //private readonly static object objBrowserLock = new object();
        //protected static Browser instance = null;
        //public static Browser Instance
        //{
        //    get
        //    {
        //        if (instance == null)
        //        {
        //            lock (objBrowserLock)
        //            {
        //                if (instance == null)
        //                {
        //                    instance = new SuperBrowser();
        //                }
        //            }
        //        }
        //        return instance;
        //    }
        //}
        #endregion

        #region 重写】判断本地消息主机是否已经启动
        /// <summary>
        /// 判断浏览器插件是否已经安装
        /// </summary>
        public override bool CheckBrowserExtensions()
        {
            //插件是否安装
            RegistryKey keyNode = Registry.CurrentUser;
            var nodeKey = keyNode.OpenSubKey(this.ExtensionsRegistryKey);
            if (nodeKey == null)
                throw new RPAException(eErrorMessageType.Error_ChromeExtensionUnInstalled);
            var node = nodeKey.GetSubKeyNames();
            if (node.Contains("ii.rpa.chromenativemsg"))
            {
                return true;
            }
            else
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_ChromeExtensionUnInstalled);
            }
        }
        #endregion

        #region 【重写】获取浏览器执行程序地址
        public override string GetBrowserAppPath()
        {
            try
            {
                if (!string.IsNullOrEmpty(BrowserAppPath))
                {
                    return BrowserAppPath;
                }
                var publicUser = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments));
                if (publicUser != null && publicUser.Exists)
                {
                    var superBrowserRoot = Path.Combine(publicUser.FullName, "SuperBrowser");
                    if (Directory.Exists(superBrowserRoot))
                    {
                        logger.LogDebug($"Find superbrowser root path:{superBrowserRoot}");
                        var subDirectoies = Directory.GetDirectories(superBrowserRoot);
                        if (subDirectoies != null && subDirectoies.Any())
                        {
                            logger.LogDebug($"Find superbrowser version path:{subDirectoies[0]}.sub count:{subDirectoies.Length}");
                            var exePath = Path.Combine(superBrowserRoot, subDirectoies[0], "Core", "Launcher.exe");
                            if (File.Exists(exePath))
                            {
                                return exePath;
                            }
                        }
                    }
                }
                string strStartUpPath = string.Empty;

                if (string.IsNullOrEmpty(strStartUpPath))
                {

                    using (RegistryKey key = Registry.CurrentUser)
                    {
                        using (RegistryKey software = key.OpenSubKey(REG_PATH, false))
                        {
                            if (software != null)
                            {
                                var value = software.GetValue("MainExeCoreFullPath");
                                if (value != null)
                                {
                                    var mainExePath = value.ToString();
                                    var mainExeDir = Path.GetDirectoryName(mainExePath);
                                    strStartUpPath = Path.Combine(mainExeDir, "Launcher.exe");
                                    if (File.Exists(strStartUpPath))
                                    {
                                        return strStartUpPath;
                                    }
                                }
                            }
                        }
                        using (RegistryKey software = key.OpenSubKey(REG_PATH_SHELL, false))
                        {
                            if (software != null)
                            {
                                var value = software.GetValue("");
                                if (value != null)
                                {
                                    var valueString = value.ToString();
                                    logger.LogInformation($"Reg:{valueString}");
                                    var valueStrings = valueString.Split(' ');
                                    var path = valueStrings[0].Trim('"');
                                    logger.LogInformation($"Reg:{path}");
                                    strStartUpPath = path;
                                    if (File.Exists(strStartUpPath))
                                    {
                                        return strStartUpPath;
                                    }
                                }
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(strStartUpPath))
                    {
                        logger.LogWarning("未找到紫鸟浏览器安装目录！");
                    }

                }
                else
                {
                    return strStartUpPath;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "紫鸟浏览器查找失败");
            }
            logger.LogWarning("未找到紫鸟浏览器安装目录！");
            return null;
        }
        #endregion

        #region 浏览器tab操作
        public override BrowserTabPage CreateBrowserTabPage(ParamCreateBrowserTabPageModel paramModel)
        {
            return new SuperBrowserTabPage(this, paramModel);
        }
        #endregion

        public override void CloseAll(bool killprocess, int timeout = 10000, bool isCloseAlert = false)
        {
            logger.LogDebug($"准备关闭 {this.BrowserProcessName} 浏览器");
            try
            {
                var startTime = Stopwatch.GetTimestamp();
                this.WebDataProvider.CloseAll(5000, killprocess, isCloseAlert);
                var endTime = Stopwatch.GetTimestamp();
                timeout -= Convert.ToInt32((endTime - startTime) * 1000D / Stopwatch.Frequency);
            }
            catch (Exception ex)
            {
                timeout -= 5000;
                logger.LogWarning(ex.Message, $"紫鸟浏览器 CloseAll 返回异常");
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            logger.LogDebug($"等待浏览器可视窗口消失 ");
            RPAUtils.Retry(() => BrowserWindowProvider.GetFirstBrowserByZOrder(null, this.GuiType) == null, 10000, 500, 500);

            logger.LogDebug($"开始检测浏览器进程");
            while (killprocess && UiCommon.GetProcessesBySessionId(this.BrowserProcessName).Length > 0 && stopWatch.Elapsed.TotalMilliseconds < timeout)
            {
                try
                {
                    var processAll = UiCommon.GetProcessesBySessionId(this.BrowserProcessName);
                    if (processAll.Length > 0)
                        ProcessKiller.KillProcessSimple(this.BrowserProcessName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
                Thread.Sleep(500);
            }
            stopWatch.Stop();
            if (stopWatch.Elapsed.TotalMilliseconds > timeout)
            {
                logger.LogDebug("关闭浏览器超时或异常");
            }
            else if (killprocess)
            {
                RPAUtils.Retry(this.IsNativeMessagingHostExited, timeout - (int)stopWatch.Elapsed.TotalMilliseconds);
            }
        }

        public bool IsCurrentParentBrowser(int aPid)
        {
            var process = ParentProcessUtilities.GetParentProcess(aPid);
            if (process.ProcessName == "cmd")
            {
                process = ParentProcessUtilities.GetParentProcess(process);
            }
            if (process.ProcessName.ToLower().Contains(BrowserProcessName))
            {
                return true;
            }
            return false;
        }

        #region 按照标题匹配浏览器窗口
        public override BrowserWindow GetFirstBrowserByZOrder(string title = "")
        {
            return BrowserWindowProvider.GetFirstBrowserByZOrder((wndInfo) => wndInfo.ToAutomationElement().Properties.Name.ValueOrDefault?.ContainsIgnoreCase(title) == true, this.GuiType);
        }
        public override List<BrowserWindow> GetAllBrowserByZOrder(string title = "")
        {
            return BrowserWindowProvider.GetAllBrowserByZOrder((wndInfo) => wndInfo.ToAutomationElement().Properties.Name.ValueOrDefault?.ContainsIgnoreCase(title) == true, this.GuiType);
        }
        #endregion
    }
}

