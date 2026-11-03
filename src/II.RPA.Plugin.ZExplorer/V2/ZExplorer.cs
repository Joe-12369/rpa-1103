using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Plugin.ZExplorer.Cef;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using II.RPA.Plugin.ZExplorer.Config;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Text;
using Newtonsoft.Json;
using System.Security.AccessControl;
using System.Xml.Linq;

namespace II.RPA.Plugin.ZExplorer.V2
{
    public class ZExplorer : Browser
    {
        private new ILogger logger = Logger.OfContext();

        public string BrowserPath
        {
            get;
            private set;
        }
        private bool WebVeiw2Installed { get; set; }
        private bool _isLatestVersion; //是否是最新版本的浏览器
        public bool IsLatestVersion
        {
            get
            {
                if (!_isLatestVersion)
                {
                    var existInstance = Process.GetProcessesByName("zexplore");
                    if (existInstance?.FirstOrDefault() != null)
                    {
                        var info_file = BrowserPath.Replace("zexplore.exe", $"{existInstance.First().Id}.txt");
                        _isLatestVersion = File.Exists(info_file);
                    }
                    else
                    {
                        _isLatestVersion = true; //如果没有运行的浏览器实例，则认为是最新版本
                    }
                }
                return _isLatestVersion;
            }
            private set { }
        }

        public override List<string> IgnoreUrl => new List<string>();

        #region 单例 全局点
        //private readonly static object objBrowserLock = new object();
        //protected static ZExplorer instance = null;
        //public static ZExplorer Instance
        //{
        //    get
        //    {
        //        if (instance == null)
        //        {
        //            lock (objBrowserLock)
        //            {
        //                if (instance == null)
        //                {
        //                    instance = new ZExplorer();
        //                }
        //            }
        //        }
        //        return instance;
        //    }
        //}

        public override WebDataProvider WebDataProvider
        {
            get
            {
                if (ZExplorerWebDataProvider.Instance.Connection == null)
                {
                    ZExplorerWebDataProvider.Instance.Connection = ZExplorerConnection.Instance;
                }

                return ZExplorerWebDataProvider.Instance;
            }
            set { }
        }
        public ZExplorer()
        {
            InitBrowser();
        }

        #endregion

        public override eGuiType GuiType => eGuiType.ZExplorer;

        public override string BrowserProcessName => "zexplore";
        public override string[] BrowserProcessNames => new string[] { "zexplore" };

        public override string CreateProcessArgs
        {
            get; set;
        }

        public override BrowserTabPage CreateBrowserTabPage(ParamCreateBrowserTabPageModel paramModel)
        {
            return new ZExplorerTabPage(this, paramModel);
        }

        public override bool CheckBrowserExtensions()
        {
            return true;
        }
        public override int DetectionMessageProcess(bool isServiceHub, int detectionTime)
        {
            return 1;
        }

        public override string GetBrowserAppPath()
        {
            if (File.Exists(BrowserPath))
            {
                if (WebVeiw2Installed)
                {
                    return BrowserPath;
                }
                //检测是否安装webview2
                var envCode = GetAppEnvCode(BrowserPath, "check");
                logger.LogInformation($"check webview2 install :{envCode}");
                if (envCode == 1)
                {
                    WebVeiw2Installed = true;
                    return BrowserPath;
                }
                else if (envCode == -1)
                {
                    throw new ZExplorerException(eErrorMessageType.Error_WebView2UnInstalled);
                }
                else
                {
                    throw new ZExplorerException(eErrorMessageType.Error_WebView2VersionLow);
                }
            }
            throw new ZExplorerException(eErrorMessageType.Error_ZEXPLORERNOTFOUND);
        }

        private void InitBrowser()
        {
            BrowserPath = Path.Combine(GlobleConfigurations.SupportDirectory, @"zexplorer\zexplore.exe");
            var userData = GlobleConfigurations.ClientType == ClientType.DeskTop ? "z-desktop" : "default";
            logger.LogInformation($"zexplorer path: {BrowserPath},user dir:{userData}");

            CreateProcessArgs = $"--user-data-dir={userData}";

            //BrowserPath = "C:\\Workspace\\Projects\\II.RPA.Browser\\build\\zexplore.exe";

        }
        private int GetAppEnvCode(string exePath, string args)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                Process process = Process.Start(startInfo);
                process.WaitForExit();
                return process.ExitCode;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex);
                return -1;
            }

        }




        public override void CloseAll(bool killProcess, int timeout = 10000, bool isCloseAlert = false)
        {
            Process.GetProcessesByName("zexplore").FirstOrDefault()?.Kill();
        }

    }
}
