using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace II.RPA.Plugin.Browsers.Edge
{
    public class EdgeBrowser : Browser
    {
        #region 浏览器基础配置参数
        public override bool IgorneCreateSetting => true;
        /// <summary>
        /// 本息消息主机进程名
        /// </summary>
        public override string NativeMessageHostProcessName => "II.RPA.NativeMessagingHostEdge";
        /// <summary>
        /// 本息消息主机进程ID
        /// </summary>
        public override string BrowserProcessName => "msedge";
        /// <summary>
        /// 通信服务的key,作为各种通信管道地址的基础数据
        /// </summary>
        protected override string ServiceKey => "8440b9eb7d03917b2509b5baeab2a299ce257eb0";
        /// <summary>
        /// 浏览器扩展程序的注册表信息地址,例（chrome）:Software\\Google\\Chrome\\Extensions
        /// </summary>
        protected override string ExtensionsRegistryKey => @"SOFTWARE\Microsoft\Edge\Extensions";

        private static readonly string[] s_extensionIds = { "badpelibibaolgkipoeccipldjmmbocd", "kghkafgpmdhgjkeihhhbeikbgdllgdoo" };
        /// <summary>
        /// 通信服务的key,作为各种通信管道地址的基础数据
        /// </summary>
        protected override string[] ExtensionIDs => s_extensionIds;


        public override List<string> IgnoreUrl => new List<string>();
        public override eGuiType GuiType => eGuiType.Edge;
        protected override eErrorMessageType ExtensionUnInstalledErrorMessageType => eErrorMessageType.Error_EdgeExtensionUnInstalled;
        protected override eErrorMessageType ExtensionUnopenErrorMessageType => eErrorMessageType.Error_EdgeExtensionUnopen;
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
        //                    instance = new EdgeBrowser();
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
            {
                logger.LogDebug("Edge浏览器插件未安装，未读取到注册表信息");
                throw new RPAException(eErrorMessageType.Error_EdgeExtensionUnInstalled);
            }
            var node = nodeKey.GetSubKeyNames();
            if (ExtensionIDs.Any(id => node.Contains(id)))
            {
                return true;
            }
            else
            {
                logger.LogDebug($"未找到Edge安装记录，Edge浏览器插件安装情况:{string.Join(",", node)}");
                throw new RPAException(eErrorMessageType.Error_EdgeExtensionUnInstalled);
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
                var edgePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Edge\Application\Edge.exe");
                if (File.Exists(edgePath))
                {
                    return edgePath;
                }
                else
                {
                    //计算机\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe
                    RegistryKey key = Registry.LocalMachine;
                    RegistryKey software = key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\msedge.exe", false);
                    return software.GetValue("").ToString();
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Edge未安装");
                return null;
            }
        }
        #endregion

        #region 浏览器tab操作
        public override BrowserTabPage CreateBrowserTabPage(ParamCreateBrowserTabPageModel paramModel)
        {
            return new EdgeBrowserTabPage(this, paramModel);
        }
        #endregion
    }
}

