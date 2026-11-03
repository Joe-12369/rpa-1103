using II.RPA.Plugin.Chrome.BrowserProfile;
using II.RPA.Plugin.Browsers.Config;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.NMH;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WebBrowser.Common.MultiInstance;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace II.RPA.Plugin.Browsers.Chrome
{
    public class ChromeBrowser : Browser
    {
        #region 浏览器基础配置参数
        public override bool IgorneCreateSetting => true;
        public override string NativeMessageHostProcessName => "II.RPA.NativeMessagingHost";
        public override string BrowserProcessName => "chrome";
        protected override string ServiceKey => "U2FsdGVkX19Xagmra6VZ0BB2FitqhBx8";
        protected override string ExtensionsRegistryKey => "Software\\Google\\Chrome\\Extensions";
        protected override string[] ExtensionIDs => new[]
        {
            "dkoieojamolcmboailagjcpjeibdiahd",
            "nfiffhdmjkohbbolbamdboplicimjnpm",
            "nenolbneempnllhhkenbnlgehjpgndpp",
            "pcopnibgkbdnlaeagepigbboebdfejmb",
        };



        public override eGuiType GuiType => eGuiType.Chrome;
        protected override eErrorMessageType ExtensionUnInstalledErrorMessageType => eErrorMessageType.Error_ChromeExtensionUnInstalled;
        protected override eErrorMessageType ExtensionUnopenErrorMessageType => eErrorMessageType.Error_ChromeExtensionUnopen;

        public override List<string> IgnoreUrl => new List<string>();
        public override bool NeedCareProfileOccupation => true;
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
        //                    instance = new ChromeBrowser();
        //                }
        //            }
        //        }
        //        return instance;
        //    }
        //}
        #endregion

        #region 设置扩展程序白名单（如IE Tab）
        public override void SetExtensionsWhitelist()
        {
            this.ExtensionsWhitelist = II.RPA.Plugin.Browsers.Config.Configurations.Info.ExtensionsWhitelist;
        }
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
            if (ExtensionIDs.Any(id => node.Contains(id)))
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
                var chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe");
                if (File.Exists(chromePath))
                {
                    BrowserAppPath = chromePath;
                }
                else
                {
                    //计算机\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe
                    RegistryKey key = Registry.LocalMachine;
                    RegistryKey software = key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\chrome.exe", false);
                    BrowserAppPath = software.GetValue("").ToString();
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Chrome未安装");
                BrowserAppPath = null;
            }
            return BrowserAppPath;
        }
        #endregion

        #region 浏览器tab操作
        public override BrowserTabPage CreateBrowserTabPage(ParamCreateBrowserTabPageModel paramModel)
        {
            return new ChromeBrowserTabPage(this, paramModel);
        }
        #endregion

        public override void CloseAll(bool killProcess, int timeout = 10000, bool isCloseAlert = false)
        {
            this.BrowserShutdownManager.CloseAll(killProcess, timeout, isCloseAlert);
        }
    }
}

