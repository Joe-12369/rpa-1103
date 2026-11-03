using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace II.RPA.Plugin.Browsers.FireFox
{
    public class FireFoxBrowser : Browser
    {
        #region 浏览器基础配置参数
        public override bool IgorneCreateSetting => true;
        /// <summary>
        /// 本息消息主机进程名
        /// </summary>
        public override string NativeMessageHostProcessName => "II.RPA.NativeMessagingHostFireFox";
        /// <summary>
        /// 本息消息主机进程ID
        /// </summary>
        public override string BrowserProcessName => "firefox";
        /// <summary>
        /// 通信服务的key,作为各种通信管道地址的基础数据
        /// </summary>
        protected override string ServiceKey => "66819f50ea4d7ec659d7013416a623f1fd8ca6da";
        /// <summary>
        /// 浏览器扩展程序的注册表信息地址,例（chrome）:Software\\Google\\Chrome\\Extensions
        /// </summary>
        protected override string ExtensionsRegistryKey => @"Software\\Mozilla\\NativeMessagingHosts\\ii.rpa.chromenativemsg";
        /// <summary>
        /// 通信服务的key,作为各种通信管道地址的基础数据
        /// </summary>
        protected override string[] ExtensionIDs => new[] { "" };

        public override eGuiType GuiType => eGuiType.FireFox;
        protected override eErrorMessageType ExtensionUnInstalledErrorMessageType => eErrorMessageType.Error_FireFoxExtensionUnInstalled;
        protected override eErrorMessageType ExtensionUnopenErrorMessageType => eErrorMessageType.Error_FireFoxExtensionUnopen;

        public override List<string> IgnoreUrl => new List<string>() { "https://home.firefoxchina.cn" };
        #endregion

        public FireFoxBrowser()
        {
            ConnectionConfig connConfig = this.GetConnectionConfig();
            this.WebDataProvider = new FirefoxWebDataProvider(connConfig);
        }

        #region 单例 全局点

        #endregion

        #region 【重写】判断本地消息主机是否已经启动
        /// <summary>
        /// 判断浏览器插件是否已经安装
        /// </summary>
        public override bool CheckBrowserExtensions()
        {
            //插件是否安装
            RegistryKey keyNode = Registry.CurrentUser;
            var nodeKey = keyNode.OpenSubKey(this.ExtensionsRegistryKey);
            if (nodeKey == null)
                throw new RPAException(eErrorMessageType.Error_FireFoxExtensionUnInstalled);
            var node = nodeKey.GetSubKeyNames();
            if (node != null)
            {
                return true;
            }
            else
            {
                throw new RPAException(eErrorMessageType.Error_FireFoxExtensionUnInstalled);
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
                RegistryKey key = Registry.LocalMachine;
                RegistryKey software = key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\firefox.exe", false);
                return software.GetValue("").ToString();
                //}
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "FireFox未安装");
                return null;
            }
        }
        #endregion

        #region 浏览器tab操作
        public override BrowserTabPage CreateBrowserTabPage(ParamCreateBrowserTabPageModel paramModel)
        {
            return new FireFoxBrowserTabPage(this, paramModel);
        }
        #endregion
    }
}

