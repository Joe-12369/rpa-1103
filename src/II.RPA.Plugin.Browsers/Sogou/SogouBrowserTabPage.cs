using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.Browsers.Sogou.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace II.RPA.Plugin.Browsers.Sogou
{
    internal class SogouBrowserTabPage : BrowserTabPage
    {
        public SogouBrowserTabPage(Browser browser, ParamCreateBrowserTabPageModel paramModel) : base(paramModel)
        {
            this.Browser = browser;
        }
        public override ElementDownloader Downloader => new SogouDownloader(this);
        /// <summary>
        /// 创建元素对象
        /// </summary>
        /// <param name="userDefinedID">元素的自定义id</param>
        /// <returns>ElementGuiNode</returns>
        public override ElementGuiNode CreateElementGuiNode(string userDefinedID)
        {
            SogouPageElement pageElement = new SogouPageElement(userDefinedID, this);
            return new SogouGuiNode(pageElement);
        }
        #region 检测是否是IE模式，并抛出异常
        public override void ThrowIfEmbeddedIE()
        {
            if (this.BrowserWindowInfo == null)
            {
                return;
            }

            if (this.BrowserWindowInfo.ClassName.EqualIgnoreCase((SogouBrowser.MAINWINDOWCLASSNAME_V12)))
            {
                base.ThrowIfEmbeddedIE();
                return;
            }
            WindowObject windowObject = new DesktopWindow()
                                    .FindAllChildWindowEx(wndObj => wndObj.ClassName == SogouBrowser.MAINWINDOWCLASSNAME_EMBEDDEDIE_V11
                                                                    && wndObj.IsVisible, 99u)
                                    .FirstOrDefault();
            if (windowObject == null)
            {
                return;
            }
            BrowserWindow browserWindow = new BrowserWindow(windowObject.Handle)
            {
                WindowType = this.BrowserWindowInfo.WindowType,
                HitMatchHandler = this.BrowserWindowInfo.HitMatchHandler,
            };
            if (browserWindow.IsEmbeddedIE())
            {
                this.BrowserWindowInfo = browserWindow;
                this.BrowserWindowInfo.WindowType |= WindowType.IE;
                this.BrowserWindowInfo.SpecifiedWindow = true;
                throw new RPAException(this.GuiType, eErrorMessageType.Error_EmbeddedIEMode);
            }
        }
        #endregion
        public override void GoBack()
        {
            var model = new ParamExecuteScriptOnFrameModel()
            {
                TabId = this.TabId,
                code = "history.back();"
            };
            this.WebDataProvider.ExecuteScriptOnFrame(model);
        }
        public override void GoForward()
        {
            var model = new ParamExecuteScriptOnFrameModel()
            {
                TabId = this.TabId,
                code = "history.forward();"
            };
            this.WebDataProvider.ExecuteScriptOnFrame(model);
        }
        public override Image CaptureByDebugger(int height, int perHeight = 1000)
        {
            throw new SogouException(eErrorMessageType.Error_NotSupport);
        }

    }
}
