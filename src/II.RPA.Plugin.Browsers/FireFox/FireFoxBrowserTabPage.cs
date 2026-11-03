using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Browsers.FireFox.DownloaderBasic;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Documents;

namespace II.RPA.Plugin.Browsers.FireFox
{
    internal class FireFoxBrowserTabPage : BrowserTabPage
    {
        public FireFoxBrowserTabPage(Browser browser, ParamCreateBrowserTabPageModel paramModel) : base(paramModel)
        {
            this.Browser = browser;
        }
        public override ElementDownloader Downloader => new FireFoxDownloader(this);
        /// <summary>
        /// 创建元素对象
        /// </summary>
        /// <param name="userDefinedID">元素的自定义id</param>
        /// <returns>ElementGuiNode</returns>
        public override ElementGuiNode CreateElementGuiNode(string userDefinedID)
        {
            FireFoxPageElement pageElement = new FireFoxPageElement(userDefinedID, this);
            return new FireFoxGuiNode(pageElement);
        }

        #region 因Firefox 与chrome类其他浏览器差异 需要重写的行为
        public override bool WaitToReadyState(DocumentReadyState readState, int timeout)
        {
            return this.IsLoaded((int)timeout, readState);
        }
        /// <summary>
        /// 屏幕坐标转页面坐标
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <returns></returns>
        public override Point? ScreenToClient(Point screenPoint)
        {
            if (this.DocumentRectangle == Rectangle.Empty)
            {
                this.SyncDocumentRectangle();
            }
            if (this.DocumentRectangle != Rectangle.Empty)
            {
                return new Point(screenPoint.X - DocumentRectangle.X, screenPoint.Y - this.DocumentRectangle.Y);
            }
            else
                return null;
        }
        public override IntPtr GetClientWndHandle()
        {
            return this.BrowserWindowInfo.Handle;
        }
        public IntPtr GetClientWndHandleByClassName(string className)
        {
            IntPtr windowIntPtr = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, className, null);
            return windowIntPtr;
        }
        public override void SyncDocumentRectangle()
        {
            IntPtr windowIntPtr = this.ClientWndHandle;
            if (windowIntPtr != IntPtr.Zero)
            {
                var element = UiCommon.UIAAutomation.FromHandle(windowIntPtr);

                List<ConditionBase> conditions = new List<ConditionBase>
                    {
                        new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ControlType, ControlType.Document)
                    };
                AndCondition ac = new AndCondition(conditions);
                var child = element.FindFirstDescendant(ac);
                if (child?.Properties.BoundingRectangle.TryGetValue(out var rect) == true)
                {
                    this.DocumentRectangle = child.BoundingRectangle;
                    return;
                }
            }
            else
            {
                logger.LogInformation("FireFox 窗口不存在");
            }
        }

        #endregion

        public override Image CaptureByDebugger(int height, int perHeight = 1000)
        {
            this.ScrollToBottomRecurve(height);

            this.ScrollTopUnitStop(0);

            var captureHeight = this.WebDataProvider.GetTotalHeight(new ParamModel() { TabId = this.TabId });
            var captureWidth = this.WebDataProvider.GetTotalWidth(new ParamModel() { TabId = this.TabId });

            var pages = Enumerable.Range(0, (int)Math.Ceiling((double)captureHeight / (double)perHeight)).ToList();

            var images = pages.Select(index =>
            {
                return this.WebDataProvider.TabCaptureFull(this.TabId, rect: new JObject {
                    { "x", 0 },
                    { "y", index * perHeight },
                    { "width", captureWidth },
                    { "height", Math.Min(perHeight, captureHeight - index * perHeight) }
                }, timeout: 30000).ToString().DataURIToImage();
            }).ToList();

            return ImageProcesser.MergeImages(images, captureWidth, captureHeight, perHeight, 1);
        }


    }
}
