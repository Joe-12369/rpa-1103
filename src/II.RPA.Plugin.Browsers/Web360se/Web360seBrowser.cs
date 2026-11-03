using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace II.RPA.Plugin.Browsers.Web360se
{
    public class Web360seBrowser : Browser
    {
        #region 浏览器基础配置参数
        public override string CreateProcessArgs { get; set; } = " --window --silent-debugger-extension-api";
        public override bool IgorneCreateSetting => true;
        /// <summary>
        /// 本息消息主机进程名
        /// </summary>
        public override string NativeMessageHostProcessName => "II.RPA.NativeMessagingHost360";
        /// <summary>
        /// 本息消息主机进程ID
        /// </summary>
        public override string BrowserProcessName => "360se";
        /// <summary>
        /// 通信服务的key,作为各种通信管道地址的基础数据
        /// </summary>
        protected override string ServiceKey => "a1773d62a609dd09e98ea1aebeddbd949b78bf83";
        /// <summary>
        /// 浏览器扩展程序的注册表信息地址,例（chrome）:Software\\Google\\Chrome\\Extensions
        /// </summary>
        protected override string ExtensionsRegistryKey => @"Software\\360\\360se6\\Chrome\\Extensions";
        /// <summary>
        /// 通信服务的key,作为各种通信管道地址的基础数据
        /// </summary>
        protected override string[] ExtensionIDs => new[] { "dkoieojamolcmboailagjcpjeibdiahd", "decdnjjmiebjighpimpjhcppghkgbgbk" };

        public override eGuiType GuiType => eGuiType.Web360se;
        protected override eErrorMessageType ExtensionUnInstalledErrorMessageType => eErrorMessageType.Error_360ExtensionUnInstalled;
        protected override eErrorMessageType ExtensionUnopenErrorMessageType => eErrorMessageType.Error_360ExtensionUnopen;

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
        //                    instance = new Web360seBrowser();
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
                throw new RPAException(eErrorMessageType.Error_360ExtensionUnInstalled);
            var node = nodeKey.GetSubKeyNames();
            if (ExtensionIDs.Any(id => node.Contains(id)))
            {
                return true;
            }
            else
            {
                throw new RPAException(eErrorMessageType.Error_360ExtensionUnInstalled);
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
                var se360Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"360se\Application\360se.exe");
                if (File.Exists(se360Path))
                {
                    return se360Path;
                }
                var se360LocalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"360se\Application\360se.exe");
                if (File.Exists(se360LocalPath))
                {
                    return se360LocalPath;
                }
                else
                {
                    string strStartUpPath = string.Empty;

                    //计算机\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Edge.exe
                    RegistryKey key = Registry.LocalMachine;
                    RegistryKey software = key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\360se6.exe", false);
                    if (software != null)
                    {
                        strStartUpPath = software.GetValue("").ToString();
                    }
                    else
                    {
                        RegistryKey keyUser = Registry.CurrentUser;
                        RegistryKey softwareUser = keyUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\360se6.exe", false);
                        if (softwareUser != null)
                        {
                            strStartUpPath = softwareUser.GetValue("").ToString();
                        }
                    }

                    if (string.IsNullOrEmpty(strStartUpPath))
                    {
                        if (AppInstallLocationHelper.GetAppStartUpPath("360安全浏览器", "360se.exe", ref strStartUpPath))
                        {
                            logger.LogInformation("定位到360安全浏览器");
                            return strStartUpPath;
                        }
                        else
                        {
                            logger.LogInformation("360安全浏览器定位失败");
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
                logger.LogWarning($"Chrome360查找失败 {ex.ToString()}");
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Chrome360未安装");
                return null;
            }

            logger.LogWarning("未获取360安全浏览器启动目录");
            return null;
        }
        #endregion

        #region 浏览器tab操作
        public override BrowserTabPage CreateBrowserTabPage(ParamCreateBrowserTabPageModel paramModel)
        {
            return new Web360seBrowserTabPage(this, paramModel);
        }
        #endregion
    }
}

