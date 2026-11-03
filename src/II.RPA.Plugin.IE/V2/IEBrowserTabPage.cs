using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.Plugin.IE.V2.Automation.Provider;
using II.RPA.Plugin.IE.V2.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace II.RPA.Plugin.IE.V2
{
    internal class IEBrowserTabPage : BrowserTabPage
    {
        public IEBrowserTabPage(Browser browser, ParamCreateBrowserTabPageModel paramModel) : base(paramModel)
        {
            this.Browser = browser;
        }
        public override ElementDownloader Downloader => new IEDownloader(this);
        public override bool WaitToReadyState(DocumentReadyState readState, int timeout)
        {
            return this.IsLoaded((int)timeout, readState);
        }
        public override BrowserWindow BrowserWindowInfo
        {
            get
            {
                if (base.browserWindowInfo == null && this.TabId > 0)
                {
                    logger.LogDebug($"无 BrowserWindowInfo");
                    WindowObject wnd = new WindowObject((IntPtr)this.TabId);
                    if (wnd.IsWindow && wnd.RootWindowHandle != IntPtr.Zero)
                    {
                        base.browserWindowInfo = new BrowserWindow(wnd.RootWindowHandle) { WindowType = WindowType.IE };
                        logger.LogDebugBlock(() =>
                        {
                            logger.LogDebug($"使用DOM跟窗口最为 BrowserWindowInfo");
                            logger.LogDebug($"BrowserWindowInfo:{BrowserWindowInfo.Handle},{BrowserWindowInfo.ClassName},{BrowserWindowInfo.Title}");
                        });
                    }
                }
                return base.BrowserWindowInfo;
            }
            protected set
            {
                base.BrowserWindowInfo = value;
            }
        }
        #region [元素拾取]
        public override bool InspectNode(IntPtr rootWnd, Point point, string selector, out IGuiNode node)
        {
            node = null;
            //this.ClientWndHandle = NativeMethods.WindowFromPoint(point);
            this.ClientWndHandle = UiCommon.WindowFromPointNew(point);
            if (TabId <= 0)
            {
                this.TabId = (int)this.ClientWndHandle;
            }
            ElementGuiNode guiNode = this.ElementGuiNodeFormPoint(point);
            if (guiNode != null)
            {
                //guiNode.BuildElementBounding();//构造一次区域信息，便于高亮动作使用
                guiNode.SimilarSelector = selector;//记录相似元素selector,便于生成selector时使用
                node = guiNode;
                return true;
            }
            return false;
        }
        #endregion
        public override void ActiveTab()
        {
            if (this.BrowserWindowInfo.WindowType > WindowType.IE
                && BrowserWindowInfo.WindowType != (WindowType.IE | WindowType.Windows))
            {
                return;
            }
            var rootHandle = NativeMethods.GetAncestor((IntPtr)this.TabId, GetAncestorFlags.GetRoot);
            if (BrowserWindowInfo.Handle != rootHandle)
            {
                this.SetWebWindowInfo(null);
            }
            this.BrowserWindowInfo.SetForeground();
            this.WebDataProvider.ActiveTab(new ParamModel()
            {
                TabId = this.TabId,
                RunParam = ParamExtensionModel.RunParam
            });
            this.ClientWndHandle = IntPtr.Zero;
        }

        public override ElementGuiNode GetElementFromSelector(string selector, string cssSelector, bool isActive, string version, string queryMode, string defaultUserDefinedID = "", bool discernHidden = false)
        {
            string userDefinedID = defaultUserDefinedID == "0/2/DIV" ? "" : this.GetElementIdFromSelector(selector, cssSelector, isActive, version, queryMode, discernHidden);
            return this.CreateElementGuiNode(userDefinedID);
        }

        /// <summary>
        /// 创建元素对象
        /// </summary>
        /// <param name="userDefinedID">元素的自定义id</param>
        /// <returns>ElementGuiNode</returns>
        public override ElementGuiNode CreateElementGuiNode(string userDefinedID)
        {
            IEPageElement pageElement = new IEPageElement(userDefinedID, this);
            return new IEGuiNode(pageElement);
        }

        public override IntPtr GetClientWndHandle()
        {
            return (IntPtr)this.TabId;
        }
        /// <summary>
        /// 根据selelctor 中的信息 获取tabId
        /// 对象内部使用，其它使用 Browser.MatchsTabId
        /// </summary>
        /// <param name="selectorObj"></param>
        /// <returns></returns>
        //protected override int GetTabIdOrDefault(List<SelectorProps> selectorObj)
        //{
        //    List<IntPtr> wndHandles = null;
        //    if (this.BrowserWindowInfo.WindowType.HasFlag(WindowType.IE) && this.BrowserWindowInfo.WindowType > WindowType.IE)
        //    {
        //        wndHandles = new List<IntPtr>() { this.BrowserWindowInfo.Handle };
        //    }
        //    return base.GetTabIdOrDefault(selectorObj, wndHandles);
        //}
    }
}
