using II.RPA.Plugin.Browsers.Config;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.WebBrowser.Common.Basic;
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
using System.Threading;

namespace II.RPA.Plugin.Browsers.Chromium
{
    public class ChromiumBrowser : Browser
    {
        public ChromiumBrowser(MatchConfig matchConfig) : base()
        {
            BrowserMatchConfig = matchConfig;
            BrowserProcessName = BrowserMatchConfig.ProcessFeatures.FirstOrDefault();
        }
        #region 浏览器基础配置参数
        public override bool IgorneCreateSetting => true;
        public override string NativeMessageHostProcessName => "II.RPA.NativeMessagingHost";
        public override string BrowserProcessName { get; }
        protected override string ServiceKey => "U2FsdGVkX19Xagmra6VZ0BB2FitqhBx8";
        protected override string ExtensionsRegistryKey => "Software\\Google\\Chrome\\Extensions";
        protected override string[] ExtensionIDs => new[]
        {
            "nfiffhdmjkohbbolbamdboplicimjnpm",//chrome v2
            "pcopnibgkbdnlaeagepigbboebdfejmb",//chrome v3
        };



        public override eGuiType GuiType => eGuiType.Chromium;
        protected override eErrorMessageType ExtensionUnInstalledErrorMessageType => eErrorMessageType.Error_ChromeExtensionUnInstalled;
        protected override eErrorMessageType ExtensionUnopenErrorMessageType => eErrorMessageType.Error_ChromeExtensionUnopen;

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
            return true;
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
                return this.BrowserMatchConfig.AppExePath;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "Chrome未安装");
                return null;
            }
        }
        #endregion



        #region 浏览器tab操作
        public override BrowserTabPage CreateBrowserTabPage(ParamCreateBrowserTabPageModel paramModel)
        {
            return new ChromiumBrowserTabPage(this, paramModel);
        }
        #endregion
        //public override int DetectionMessageProcess(bool isServiceHub, int detectionTime)
        //{
        //    if (this.NativeMessageHostProcessID > 0)
        //    {
        //        if (BrowserContextProvider.Instance.CheckProfileNMHProcId(this.NativeMessageHostProcessID, out var newNMHProcId))
        //        {
        //            return this.NativeMessageHostProcessID;
        //        }
        //        else if (newNMHProcId > 0)
        //        {
        //            this.NativeMessageHostProcessID = newNMHProcId;
        //            return this.NativeMessageHostProcessID;
        //        }
        //    }
        //    var stopWatch = new Stopwatch();
        //    stopWatch.Start();
        //    do
        //    {
        //        Process[] processArr;
        //        if (isServiceHub)
        //            processArr = Process.GetProcessesByName(this.ServiceHubProcessName);
        //        else
        //            processArr = UiCommon.GetProcessesBySessionId(this.NativeMessageHostProcessName);
        //        if (processArr.Length > 0)
        //        {
        //            foreach (var p in processArr)
        //            {
        //                if (isServiceHub || IsCurrentParentBrowser(p.Id))
        //                {
        //                    if (this.NativeMessageHostProcessID != p.Id)
        //                    {
        //                        this.StopConnecttion();
        //                    }
        //                    this.NativeMessageHostProcessID = p.Id;
        //                    stopWatch.Stop();
        //                    return this.NativeMessageHostProcessID;
        //                }
        //            }
        //        }
        //        if (stopWatch.Elapsed.TotalMilliseconds < detectionTime - 50)
        //        {
        //            Thread.Sleep(50);
        //        }
        //    } while (stopWatch.Elapsed.TotalMilliseconds < detectionTime);
        //    this.NativeMessageHostProcessID = 0;
        //    stopWatch.Stop();
        //    return this.NativeMessageHostProcessID;
        //}

        public bool IsCurrentParentBrowser(int aPid)
        {
            try
            {
                string exePath = null;
                Process parentProcess = null;
                Process process = ParentProcessUtilities.GetParentProcess(aPid);
                if (process.ProcessName == "cmd")
                {
                    parentProcess = ParentProcessUtilities.GetParentProcess(process);
                }
                exePath = (parentProcess ?? process).GetExeFilePath();
                process.Dispose();
                parentProcess?.Dispose();
                if (this.BrowserMatchConfig.AppExePath.EqualIgnoreCase(exePath))
                {
                    return true;
                }
            }
            catch (Win32Exception win32Ex)
            {
                throw win32Ex;
            }
            catch (Exception ex)
            {
                logger.LogDebug($"IsCurrentParentBrowser Error,PID:{aPid},{ex.Message}");
            }
            return false;
        }
        #region 按照标题匹配浏览器窗口
        public override BrowserWindow GetFirstBrowserByZOrder(string title = "")
        {
            return BrowserWindowProvider.GetFirstBrowserByZOrder((wndInfo) => wndInfo.HitMatchHandler.MatchConfig.ConfigKey == this.BrowserMatchConfig.ConfigKey && wndInfo.ToAutomationElement().Properties.Name.ValueOrDefault?.ContainsIgnoreCase(title) == true, this.GuiType);
        }
        public override List<BrowserWindow> GetAllBrowserByZOrder(string title = "")
        {
            return BrowserWindowProvider.GetAllBrowserByZOrder((wndInfo) => wndInfo.HitMatchHandler.MatchConfig.ConfigKey == this.BrowserMatchConfig.ConfigKey && wndInfo.ToAutomationElement().Properties.Name.ValueOrDefault?.ContainsIgnoreCase(title) == true, this.GuiType);
        }
        public override BrowserWindow FindWindowInfo(string title = "")
        {
            logger.LogDebug($"FindWindowInfo:{title}");
            int timeout = string.IsNullOrEmpty(title) ? 0 : 1000;
            BrowserWindow webWindowInfo = RPAUtils.RetryWait(() => this.GetFirstBrowserByZOrder(title), (uint)timeout, 5);
            return webWindowInfo;
        }
        public override List<BrowserWindow> FindAllSameTitleWindow(string title = "")
        {
            logger.LogDebug($"FindWindowInfo:{title}");
            List<BrowserWindow> browserWnds = null;
            int timeout = string.IsNullOrEmpty(title) ? 0 : 500;
            RPAUtils.Retry(() =>
            {
                browserWnds = this.GetAllBrowserByZOrder(title);
                return browserWnds.Count > 0;
            }, timeout, 5);
            return browserWnds.Count > 0 ? browserWnds : null;
        }
        #endregion

    }
}

