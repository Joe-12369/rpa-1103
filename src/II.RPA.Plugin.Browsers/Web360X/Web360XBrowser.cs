using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace II.RPA.Plugin.Browsers.Web360X
{
    public class Web360XBrowser : Browser
    {
        #region 浏览器基础配置参数
        public override string CreateProcessArgs { get; set; } = " --window --silent-debugger-extension-api";
        public override bool IgorneCreateSetting => true;
        /// <summary>
        /// 本息消息主机进程名
        /// </summary>
        public override string NativeMessageHostProcessName => "II.RPA.NativeMessagingHost360X";
        /// <summary>
        /// 本息消息主机进程ID
        /// </summary>
        public override string BrowserProcessName => "360ChromeX";

        public override string[] BrowserProcessNames => new string[] { "360chrome", "360ChromeX" };
        /// <summary>
        /// 通信服务的key,作为各种通信管道地址的基础数据
        /// </summary>
        protected override string ServiceKey => "1b9199950f8edc7d0d09200a516cdf0fc78c4c17";
        /// <summary>
        /// 浏览器扩展程序的注册表信息地址,例（chrome）:Software\\Google\\Chrome\\Extensions
        /// </summary>
        //protected override string ExtensionsRegistryKey => @"Software\\360Chrome\\Chrome\\Extensions";
        protected override string ExtensionsRegistryKey => @"SOFTWARE\\360ChromeX\\Chrome\\Extensions";
        /// <summary>
        /// 通信服务的key,作为各种通信管道地址的基础数据
        /// </summary>
        protected override string[] ExtensionIDs => new[] { "leabadfdkgaidioombodahloaffmjidi" };

        public override eGuiType GuiType => eGuiType.Web360X;

        protected override eErrorMessageType ExtensionUnInstalledErrorMessageType => eErrorMessageType.Error_360XExtensionUnInstalled;
        protected override eErrorMessageType ExtensionUnopenErrorMessageType => eErrorMessageType.Error_360XExtensionUnopen;

        public override List<string> IgnoreUrl => new List<string>() { "https://hao.360.com/" };
        #endregion 

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
        //                    instance = new Web360XBrowser();
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
                nodeKey = keyNode.OpenSubKey("Software\\360Chrome\\Chrome\\Extensions");
            if (nodeKey == null)
                throw new RPAException(eErrorMessageType.Error_360XExtensionUnInstalled);
            var node = nodeKey.GetSubKeyNames();
            if (ExtensionIDs.Any(id => node.Contains(id)))
            {
                return true;
            }
            else
            {
                throw new RPAException(eErrorMessageType.Error_360XExtensionUnInstalled);
            }
        }
        #endregion

        #region 获取浏览器执行程序地址 
        public override string GetBrowserAppPath()
        {
            try
            {
                if (!string.IsNullOrEmpty(BrowserAppPath))
                {
                    return BrowserAppPath;
                }
                var se360Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"360ChromeX\Chrome\Application\360ChromeX.exe");
                if (File.Exists(se360Path))
                {
                    return se360Path;
                }
                var se360LocalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"360ChromeX\Chrome\Application\360ChromeX.exe");
                if (File.Exists(se360LocalPath))
                {
                    return se360LocalPath;
                }
                se360Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"360Chrome\Chrome\Application\360chrome.exe");
                if (File.Exists(se360Path))
                {
                    return se360Path;
                }
                se360LocalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"360Chrome\Chrome\Application\360chrome.exe");
                if (File.Exists(se360LocalPath))
                {
                    return se360LocalPath;
                }
                else
                {
                    string strStartUpPath = string.Empty;

                    //计算机\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Edge.exe
                    RegistryKey key = Registry.LocalMachine;
                    RegistryKey software = key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\360chrome.exe", false);
                    if (software != null)
                    {
                        strStartUpPath = software.GetValue("").ToString();
                    }
                    else
                    {
                        RegistryKey keyUser = Registry.CurrentUser;
                        RegistryKey softwareUser = keyUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\360chrome.exe", false);
                        if (softwareUser != null)
                        {
                            strStartUpPath = softwareUser.GetValue("").ToString();
                        }
                    }

                    if (string.IsNullOrEmpty(strStartUpPath))
                    {
                        if (AppInstallLocationHelper.GetAppStartUpPath("360极速浏览器", "360chrome.exe", ref strStartUpPath))
                        {
                            logger.LogInformation("定位到360极速浏览器");
                            return strStartUpPath;
                        }
                        else
                        {
                            logger.LogInformation("360极速浏览器定位失败");
                        }
                    }
                    else
                    {
                        return strStartUpPath;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"360极速查找失败 {ex.ToString()}");
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "360极速未安装");
                return null;
            }

            logger.LogWarning("未获取360极速浏览器启动目录");
            return null;
        }
        #endregion

        #region 浏览器tab操作
        public override BrowserTabPage CreateBrowserTabPage(ParamCreateBrowserTabPageModel paramModel)
        {
            return new Web360XBrowserTabPage(this, paramModel);
        }
        #endregion

        public override bool IsRunBrowser
        {
            get
            {
                foreach (var processName in this.BrowserProcessNames)
                {
                    if (RPAUtils.GetProcessesBySessionId(processName).Length > 0)
                        return true;
                }
                return false;
            }
        }
    }
}

