using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Browsers.Web360se.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace II.RPA.Plugin.Browsers.Web360se
{
    internal class Web360seBrowserTabPage : BrowserTabPage
    {
        public Web360seBrowserTabPage(Browser browser, ParamCreateBrowserTabPageModel paramModel) : base(paramModel)
        {
            this.Browser = browser;
        }
        public override ElementDownloader Downloader => new Web360seDownloader(this);
        /// <summary>
        /// 创建元素对象
        /// </summary>
        /// <param name="userDefinedID">元素的自定义id</param>
        /// <returns>ElementGuiNode</returns>
        public override ElementGuiNode CreateElementGuiNode(string userDefinedID)
        {
            Web360sePageElement pageElement = new Web360sePageElement(userDefinedID, this);
            return new Web360seGuiNode(pageElement);
        }

        //public override IntPtr GetClientWndHandle(string title)
        //{
        //    if (string.IsNullOrEmpty(title))
        //        logger.LogInformation("from title is null");
        //    var deskTopHWnd = NativeMethods.GetDesktopWindow();
        //    var windowIntPtr = NativeMethods.FindWindowByClassNameAndTitle(deskTopHWnd, "360se6_Frame", title, " - 360安全浏览器");
            
        //    if (windowIntPtr == IntPtr.Zero)
        //    {
        //        logger.LogWarning($"警示：未匹配到指定tab窗口，可能会发生偏移问题");
        //    }
        //    IntPtr docIntPtr = NativeMethods.FindWindowEx(windowIntPtr, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", null);
        //    #region [查找句柄补偿机制]
        //    if (!NativeMethods.IsWindowVisible(docIntPtr))
        //    {
        //        var windowBrowser = UiCommon.UIAAutomation.FromHandle(windowIntPtr);
        //        var children = UiCommon.TreeWalker.GetFirstChild(windowBrowser);
        //        while (children != null)
        //        {
        //            if (children.Properties.ClassName.IsSupported
        //                && children.ClassName == "Chrome_RenderWidgetHostHWND"
        //                && children.Properties.BoundingRectangle.IsSupported
        //                && children.BoundingRectangle.Y > 0)
        //            {
        //                docIntPtr = children.Properties.NativeWindowHandle.ValueOrDefault;
        //                break;
        //            }
        //            children = UiCommon.TreeWalker.GetNextSibling(children);
        //        }
        //    }
        //    #endregion
        //    if (docIntPtr != IntPtr.Zero)
        //    {
        //        return docIntPtr;
        //    }
        //    else
        //    {
        //        logger.LogInformation("360 安全浏览器窗口不存在");
        //        return IntPtr.Zero;
        //    }
        //}

    }
}
