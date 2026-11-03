using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace II.RPA.Plugin.Browsers.Sogou
{
    public class SogouBrowser : Browser
    {
        public const string MAINWINDOWCLASSNAME_V12 = "SogouExplorer_WidgetWin_1";
        public const string MAINWINDOWCLASSNAME_V11 = "SE_SogouExplorerFrame";
        public const string MAINWINDOWCLASSNAME_EMBEDDEDIE_V11 = "SE_AxControl";
        #region 浏览器基础配置参数
        public override bool IgorneCreateSetting => true;
        public override List<string> IgnoreUrl => new List<string>() { "https://123.sogou.com/" };
        /// <summary>
        /// 本息消息主机进程名
        /// </summary>
        public override string NativeMessageHostProcessName
        {
            get
            {
                return IsSogouNewVersion ? "II.RPA.NativeMessagingHostSogou" : "";
            }
        }
        /// <summary>
        /// 本息消息主机进程ID
        /// </summary>
        public override string BrowserProcessName => "SogouExplorer";
        
        /// <summary>
        /// 通信服务的key,作为各种通信管道地址的基础数据
        /// </summary>
        protected override string ServiceKey => "";
        /// <summary>
        /// 浏览器扩展程序的注册表信息地址,例（chrome）:Software\\Google\\Chrome\\Extensions
        /// </summary>
        protected override string ExtensionsRegistryKey => "Software\\Google\\Chrome\\Extensions";
        /// <summary>
        /// 通信服务的key,作为各种通信管道地址的基础数据
        /// </summary>
        protected override string[] ExtensionIDs => new[] { "ofdinijfhmgnfddmbmminecbaafgoocd", "dkjhpjbcmlpcffmplnoiajimipemjkgg" };

        public override eGuiType GuiType => eGuiType.Sogou;


        #region 连接类型，可通过类型重写，切换需要的connection
        public override Type ServiceConnectionType => null;
        public override Type NamePipeConnectionType => null;

        public override string SignalRUrl => "http://127.0.0.1:52050/rpa";
        protected override eErrorMessageType ExtensionUnInstalledErrorMessageType => eErrorMessageType.Error_SogouExtensionUnInstalled;
        protected override eErrorMessageType ExtensionUnopenErrorMessageType => eErrorMessageType.Error_SogouExtensionUnopen;
        #endregion
        //public SogouBrowser()
        //{
        //    ConnectionConfig connConfig = this.GetConnectionConfig();
        //    this.WebDataProvider = new SogouDataProvider(connConfig);
        //}

        public override ConnectionConfig GetConnectionConfig()
        {
            var _ServiceKey = string.Empty;
            Type _ServiceConnectionType = null;
            if (IsSogouNewVersion)
            {
                _ServiceKey = "1b2c46b3e673ce7f4aff9c4f5065d256459a36df";
                _ServiceConnectionType = typeof(IPCConnection);
            }
            else
            {
                _ServiceKey = this.ServiceKey;
                _ServiceConnectionType = this.ServiceConnectionType;
            }

            return new ConnectionConfig
            {
                GuiType = this.GuiType,
                NativeMessageHostProcessName = this.NativeMessageHostProcessName,
                NativeMessageHostProcessID = this.NativeMessageHostProcessID,
                BrowserProcessName = this.BrowserProcessName,
                ServiceKey = _ServiceKey,
                ServiceConnectionType = _ServiceConnectionType,
                NamePipeConnectionType = IsSogouNewVersion
                    ? base.NamePipeConnectionType
                    : this.NamePipeConnectionType,
                SignalRConnectionType = this.SignalRConnectionType,
                SignalRUrl = this.SignalRUrl,
                CheckBrowserExtensions = this.CheckBrowserExtensions,
                DetectionMessageProcess = this.DetectionMessageProcess,
                ExtensionUnInstalledErrorMessageType = this.ExtensionUnInstalledErrorMessageType,
                ExtensionUnopenErrorMessageType = this.ExtensionUnopenErrorMessageType
            };
        }

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
        //                    instance = new SogouBrowser();
        //                }
        //            }
        //        }
        //        return instance;
        //    }
        //}
        #endregion
        
        #region Connection 相关 
        public override void StopConnecttion()
        {
            this.WebDataProvider.Connection?.Stop();
            this.WebDataProvider.Connection = null;
        }
        #endregion

        #region 重写】判断本地消息主机是否已经启动
        /// <summary>
        /// 判断浏览器插件是否已经安装
        /// </summary>
        public override bool CheckBrowserExtensions()
        {
            if (IsSogouNewVersion)
            {
                //插件是否安装
                RegistryKey keyNode = Registry.CurrentUser;
                var nodeKey = keyNode.OpenSubKey(this.ExtensionsRegistryKey);
                if (nodeKey == null)
                    throw new RPAException(eErrorMessageType.Error_SogouExtensionUnInstalled);
                var node = nodeKey.GetSubKeyNames();
                if (ExtensionIDs.Any(id => node.Contains(id)))
                {
                    return true;
                }
                else
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_SogouExtensionUnInstalled);
                }
            }
            //插件是否安装
            var extensionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SogouExplorer", "Extension", ExtensionIDs[0]);
            var extensionPath_v12_upgrade = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sogou\\SogouExplorer\\User Data\\Default\\SogouExt", ExtensionIDs[0]);
            var extensionPath_v12 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sogou\\SogouExplorer\\User Data\\Default\\Extensions", ExtensionIDs[0]);
            if (Directory.Exists(extensionPath) && Directory.GetDirectories(extensionPath, "1.0.0").Length >= 1)
            {

                return true;
            }
            else if (Directory.Exists(extensionPath_v12) || Directory.Exists(extensionPath_v12_upgrade))
            {
                return true;
            }
            else
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_SogouExtensionUnInstalled);
            }
        }

        public override int DetectionMessageProcess(bool isServiceHub = true, int detectionTime = 10)
        {
            if (!this.IsRunBrowser)
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_SogouBrowserNotRun);
            }
            if (!IsSogouNewVersion)
            {
                if (Process.GetProcessesByName(this.ServiceHubProcessName).Length == 0)
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_ServerHubUnopen);
                }
                //StartServer();
                return base.DetectionMessageProcess(true, detectionTime);
            }
            return base.DetectionMessageProcess(isServiceHub, detectionTime);
        }

        public void StartServer()
        {
            try
            {

                if (Process.GetProcessesByName("II.RPA.BrowserAutomationServer").Length < 1)
                {
                    var executePath = AppDomain.CurrentDomain.BaseDirectory;
                    var installPath = Directory.GetParent(executePath).Parent.FullName;
                    var serverPath = Path.Combine(installPath, "II.RPA.BrowserAutomation", "II.RPA.BrowserAutomationServer.exe");
                    ProcessStartInfo startInfo = new ProcessStartInfo(serverPath);
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true;
                    Process.Start(startInfo);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("启动搜狗自动化服务失败");
            }


        }
        #endregion
        private string _sogouPath;
        public string sogouPath
        {
            get
            {
                if (string.IsNullOrEmpty(_sogouPath))
                    _sogouPath = GetBrowserAppPath();
                return _sogouPath;
            }
        }

        public bool IsSogouNewVersion
        {
            get
            {
                return FileHelper.IsHighVersionSogouExplorer(sogouPath);
            }
        }
        #region 获取浏览器执行程序地址 

        public override string GetBrowserAppPath()
        {
            try
            {
                var SogouPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"SogouExplorer\SogouExplorer.exe");
                if (File.Exists(SogouPath))
                {
                    return SogouPath;
                }
                else
                {
                    try
                    {
                        //计算机\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Sogou.exe
                        RegistryKey key = Registry.CurrentUser;
                        RegistryKey software = key.OpenSubKey("SOFTWARE\\SogouExplorer", false);
                        if (software == null)
                        {
                            software = key.OpenSubKey("SOFTWARE\\Sogou\\SogouExplorer", false);
                            return software?.GetValue("").ToString() + "\\SogouExplorer.exe";
                        }
                        return software.GetValue("").ToString() + "\\SogouExplorer.exe";
                    }
                    catch (Exception)
                    {
                        RegistryKey key = Registry.CurrentUser;
                        RegistryKey software = key.OpenSubKey("SOFTWARE\\Sogou\\SogouExplorer", false);
                        return software.GetValue("").ToString() + "\\SogouExplorer.exe";
                    }
                }
            }
            catch (Exception ex)
            { 
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Sogou未安装");
                return null;
            } 
        }
        #endregion

        #region 浏览器tab操作
        public override BrowserTabPage CreateBrowserTabPage(ParamCreateBrowserTabPageModel paramModel)
        {
            return new SogouBrowserTabPage(this, paramModel);
        }
        #endregion
    }
}

