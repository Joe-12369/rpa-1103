using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.CompareMode;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.WebBrowser.Common.Basic.DownloaderBasic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;

namespace II.RPA.WebBrowser.Common.Basic
{
    public abstract class BrowserTabPage
    {

        public BrowserTabPage(ParamCreateBrowserTabPageModel paramModel)
        {
            this.TabId = paramModel.TabId;
            this.ParamExtensionModel = new ParamExtensionModel
            {
                TabId = paramModel.TabId,
                RunParam = paramModel.RunParam
            };
            this.SetWebWindowInfo(paramModel.WebWindowInfo);
        }

        protected static ILogger logger = Logger.OfContext();
        public eGuiType GuiType => this.Browser.GuiType;

        public ParamExtensionModel ParamExtensionModel { get; set; }
        public int TabId { get; set; } = -1;
        public Browser Browser { get; set; } = null;
        public virtual ElementDownloader Downloader { get; }
        private Rectangle rectangle = Rectangle.Empty;
        public Rectangle IntoViewPortRectangle
        {
            get
            {
                if (rectangle == Rectangle.Empty)
                {
                    rectangle = this.GetIntoViewPortRect();
                }
                return rectangle;
            }
            set { rectangle = value; }
        }
        #region 页面窗口 句柄、区域信息等 相关
        /// <summary>
        /// 呈现页面的UIA对象在计算机中的原始区域
        /// </summary>
        public virtual Rectangle DocumentRectangle { get; set; } = Rectangle.Empty;
        protected BrowserWindow browserWindowInfo = null;
        private IntPtr clientWndHandle = IntPtr.Zero;
        public virtual BrowserWindow BrowserWindowInfo
        {
            get
            {
                if (this.browserWindowInfo == null)
                {
                    if (this.MainWindowHandle != 0)
                    {
                        this.browserWindowInfo = this.Browser.FindWindowInfo(MainWindowHandle) ?? this.Browser.FindWindowInfo();
                    }
                    else
                    {
                        this.browserWindowInfo = this.Browser.FindWindowInfo(this.Title) ?? this.Browser.FindWindowInfo();
                    }
                }
                return this.browserWindowInfo;
            }
            protected set
            {
                this.browserWindowInfo = value;
            }
        }

        public void SetWebWindowInfo(BrowserWindow webWindowInfo)
        {
            this.BrowserWindowInfo = webWindowInfo;
        }
        /// <summary>
        /// 客户端窗口句柄，可根据此句柄按照一定逻辑，得到DocumentRectangle的句柄，通常是Document(UIA)句柄，或 浏览器主窗口句柄
        /// </summary>
        public virtual IntPtr ClientWndHandle
        {
            get
            {
                if (this.clientWndHandle == IntPtr.Zero)
                {
                    this.clientWndHandle = GetClientWndHandle();
                }
                return clientWndHandle;
            }
            set
            {
                clientWndHandle = value;
            }
        }

        #region  获取浏览器窗口中渲染区窗口句柄
        /// <summary>
        /// 获取浏览器窗口中渲染区窗口句柄
        /// </summary>
        public virtual IntPtr GetClientWndHandle()
        {
            // 查找所有可见的渲染窗口
            var childRenderWnds = this.BrowserWindowInfo
                .FindAllChildWindowEx(
                    childWndInfo => childWndInfo.IsVisible && this.BrowserWindowInfo.IsRenderWindow(childWndInfo),
                    99)
                .ToList();

            if (childRenderWnds.Count == 0)
            {
                logger.LogDebug("未找到任何渲染窗口");
                NativeMethods.SetFocus(IntPtr.Zero);
                return IntPtr.Zero;
            }

            // 如果存在多个渲染窗口，尝试通过名称匹配找到正确的窗口
            var targetWindow = childRenderWnds.Count > 1
                ? FindMatchingWindow(childRenderWnds)
                : childRenderWnds.FirstOrDefault();

            var renderHandle = targetWindow?.Handle ?? IntPtr.Zero;

            // 如果未找到句柄，重置焦点
            if (renderHandle == IntPtr.Zero)
            {
                NativeMethods.SetFocus(IntPtr.Zero);
            }

            // 记录调试信息
            LogWindowState(renderHandle, targetWindow);

            return renderHandle;
        }

        /// <summary>
        /// 在多个渲染窗口中查找匹配的窗口
        /// </summary>
        private WindowObject FindMatchingWindow(List<WindowObject> childRenderWnds)
        {
            if (!this.BrowserWindowInfo.AutomationElement.Properties.Name.TryGetValue(out var browserUiaName))
            {
                logger.LogDebug("无法获取浏览器窗口名称，按面积最大选择渲染窗口");
                return childRenderWnds
                    .OrderByDescending(wnd => wnd.WindowRect.Width * wnd.WindowRect.Height)
                    .FirstOrDefault();
            }

            logger.LogDebug($"存在多个渲染窗口（{childRenderWnds.Count}个），尝试通过名称匹配:{browserUiaName}");

            // 首先尝试直接匹配窗口名称
            var matchedWindow = childRenderWnds
                .Where(wnd => TryMatchWindowName(wnd, browserUiaName))
                .OrderByDescending(wnd => wnd.WindowRect.Width * wnd.WindowRect.Height)
                .FirstOrDefault();

            if (matchedWindow != null)
            {
                logger.LogDebug("通过窗口名称匹配成功");
                return matchedWindow;
            }

            // 如果直接匹配失败，尝试匹配子元素名称
            matchedWindow = childRenderWnds
                .Where(wnd => TryMatchChildElementName(wnd, browserUiaName))
                .OrderByDescending(wnd => wnd.WindowRect.Width * wnd.WindowRect.Height)
                .FirstOrDefault();

            if (matchedWindow != null)
            {
                logger.LogDebug("通过子元素名称匹配成功");
                return matchedWindow;
            }

            matchedWindow = childRenderWnds.OrderByDescending(wnd => wnd.WindowRect.Width * wnd.WindowRect.Height).FirstOrDefault();
            if (matchedWindow != null)
            {
                logger.LogDebug("匹配子元素面积最大的窗口成功");
                return matchedWindow;
            }

            logger.LogDebug("名称匹配失败，使用第一个渲染窗口");
            return childRenderWnds.FirstOrDefault();
        }

        /// <summary>
        /// 尝试匹配窗口名称
        /// </summary>
        private bool TryMatchWindowName(WindowObject window, string browserUiaName)
        {
            var ret = window.AutomationElement.Properties.Name.TryGetValue(out var uiaName)
                   && !string.IsNullOrEmpty(uiaName)
                   && browserUiaName.ContainsIgnoreCase(uiaName);
            logger.LogDebug($"尝试匹配窗口名称: 窗口名称='{uiaName}', 浏览器名称='{browserUiaName}', 匹配结果={ret}");
            return ret;
        }
        public IEnumerable<AutomationElement> FindAllByRawView(AutomationElement parent, Func<AutomationElement, int, bool> func = null)
        {
            ITreeWalker treeWalker = UiCommon.UIAAutomation.TreeWalkerFactory.GetRawViewWalker();
            var ele = treeWalker.GetFirstChild(parent);
            int idx = -1;
            while (ele != null)
            {
                if (func?.Invoke(ele, ++idx) == true)
                {
                    yield return ele;
                }
                ele = treeWalker.GetNextSibling(ele);
            }
        }
        /// <summary>
        /// 尝试匹配子元素名称
        /// </summary>
        private bool TryMatchChildElementName(WindowObject window, string browserUiaName)
        {

            var doc = FindAllByRawView(window.AutomationElement, (ele, idx) =>
            {
                if (!ele.Properties.ControlType.TryGetValue(out var controlType) ||
                    controlType != FlaUI.Core.Definitions.ControlType.Document
                    )
                {
                    return false;
                }
                _ = ele.Properties.Name.TryGetValue(out var docUiaName);
                logger.LogDebug($"扫描到Doc: idx={idx}, 名称='{docUiaName}', 浏览器名称='{browserUiaName}'");

                if (string.Equals(docUiaName, "DevTools", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogDebug($"跳过DevTools文档节点: idx={idx}, 名称='{docUiaName}'");
                    return false;
                }

                var isMatch = !string.IsNullOrWhiteSpace(docUiaName)
                              && browserUiaName.ContainsIgnoreCase(docUiaName);
                return isMatch;
            }).FirstOrDefault();

            return doc != null;
        }

        /// <summary>
        /// 记录窗口状态调试信息
        /// </summary>
        private void LogWindowState(IntPtr renderHandle, WindowObject childWnd)
        {
            logger.LogDebugBlock(() =>
            {
                logger.LogDebug("===============获取渲染窗口句柄时窗口状态=======================");

                // 渲染窗口句柄信息
                logger.LogDebug($"渲染窗口句柄: {renderHandle} (0x{renderHandle.ToString("x4")})");

                if (renderHandle != IntPtr.Zero)
                {
                    if (NativeMethods.GetWindowRect(renderHandle, out var rect))
                    {
                        logger.LogDebug($"渲染窗口矩形: {rect.ToJson()}");
                    }
                }

                // 根窗口句柄信息
                if (childWnd?.RootWindowHandle != null)
                {
                    logger.LogDebug($"根窗口句柄: {childWnd.RootWindowHandle} (0x{childWnd.RootWindowHandle.ToString("x4")})");
                }

                // 前置窗口信息
                var foregroundWnd = NativeMethods.GetForegroundWindow();
                logger.LogDebug($"前置窗口句柄: {foregroundWnd} (0x{foregroundWnd.ToString("x4")})");
                logger.LogDebug($"前置窗口类名: {UiCommon.GetClassName(foregroundWnd)}");
                logger.LogDebug($"前置窗口标题: {UiCommon.GetWindowText(foregroundWnd)}");

                // 浏览器窗口信息
                var browserHandle = this.BrowserWindowInfo.Handle;
                logger.LogDebug($"浏览器窗口句柄: {browserHandle} (0x{browserHandle.ToString("x4")})");
                logger.LogDebug($"浏览器窗口类名: {UiCommon.GetClassName(browserHandle)}");
                logger.LogDebug($"浏览器窗口标题: {UiCommon.GetWindowText(browserHandle)}");

                logger.LogDebug("===============================================================");
            });
        }

        #endregion

        /// <summary>
        ///// 获取document 元素的句柄
        ///// </summary>
        ///// <param name="title"></param>
        ///// <returns></returns>
        //public virtual IntPtr GetClientWndHandle()
        //{
        //    var childRenderWnds = this.BrowserWindowInfo
        //                         .FindAllChildWindowEx((childWndInfo) => childWndInfo.IsVisible
        //                                                                 && this.BrowserWindowInfo.IsRenderWindow(childWndInfo), 99)
        //                         .ToList();
        //    WindowObject childWnd = null;
        //    if (childRenderWnds.Count > 1 && this.BrowserWindowInfo.AutomationElement.Properties.Name.TryGetValue(out var browserUiaName))
        //    {
        //        logger.LogDebug($"存在多个与渲染区相同window特征的窗口");
        //        childWnd = childRenderWnds.FirstOrDefault(wnd =>
        //        {
        //            if (wnd.AutomationElement.Properties.Name.TryGetValue(out var uiaName) && browserUiaName.ContainsIgnoreCase(uiaName))
        //            {
        //                return true;
        //            }
        //            var doc = wnd.AutomationElement.FindFirst(FlaUI.Core.Definitions.TreeScope.Children, TrueCondition.Default);
        //            return doc.Properties.Name.TryGetValue(out var docUiaName) && browserUiaName.ContainsIgnoreCase(docUiaName);
        //        });
        //    }
        //    childWnd = childWnd ?? childRenderWnds.FirstOrDefault();

        //    var renderhandle = childWnd?.Handle ?? IntPtr.Zero;
        //    if (renderhandle == IntPtr.Zero)
        //        NativeMethods.SetFocus(IntPtr.Zero);
        //    logger.LogDebugBlock(() =>
        //    {
        //        logger.LogDebug($"===============获取渲染窗口句柄时窗口状态=======================");
        //        logger.LogDebug($"render handle ：{renderhandle},{renderhandle.ToString("x4")}");
        //        NativeMethods.GetWindowRect(renderhandle, out var rect);
        //        logger.LogDebug($"Render Rect:{rect.ToJson()}");
        //        logger.LogDebug($"render root handle ：{childWnd?.RootWindowHandle},{childWnd?.RootWindowHandle.ToString("x4")}");
        //        IntPtr fhWnd = NativeMethods.GetForegroundWindow();
        //        logger.LogDebug($"前置窗口 handle：{fhWnd}({fhWnd.ToString("x4")}) className：{UiCommon.GetClassName(fhWnd)}");
        //        logger.LogDebug($"前置窗口 标题：{UiCommon.GetWindowText(fhWnd)}");
        //        logger.LogDebug($"使用浏览器窗口 标题：{UiCommon.GetWindowText(this.BrowserWindowInfo.Handle)}");
        //        logger.LogDebug($"使用浏览器窗口 handle：{this.BrowserWindowInfo.Handle}({this.BrowserWindowInfo.Handle.ToString("x4")}) className：{UiCommon.GetClassName(this.BrowserWindowInfo.Handle)}");
        //        logger.LogDebug($"===============================================================");
        //        logger.LogDebug($"===============================================================");

        //    });
        //    return renderhandle;
        //}
        /// <summary>
        /// 同步Document元素的区域信息，即整个页面的区域信息（以windows句柄方式获取的区域信息）
        /// </summary>
        public virtual void SyncDocumentRectangle()
        {
            var clientHandleIsZero = RPAUtils.Retry(() => this.ClientWndHandle != IntPtr.Zero, 3000, 50);
            if (!clientHandleIsZero)
            {
                logger.LogDebug($"获取浏览器dom句柄为0，这不符合使用场景");
                logger.LogDebug($"Title:{this.Title}");
            }
            NativeMethods.GetWindowRect(this.ClientWndHandle, out var rect);
            this.DocumentRectangle = new Rectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
            if (!clientHandleIsZero)
            {
                logger.LogDebug($"DocumentRectangle:{this.DocumentRectangle}");
            }
        }

        #endregion

        protected WebDataProvider WebDataProvider => this.Browser.WebDataProvider;
        public abstract ElementGuiNode CreateElementGuiNode(string userDefinedID);
        public virtual IEnumerable<ElementGuiNode> CreateElementGuiNodes(List<string> userDefinedIDs)
        {
            return userDefinedIDs.Select(id => CreateElementGuiNode(id));
        }

        #region 获取 Tab 的基础信息数据
        #region Tab 的基础属性 

        /// <summary>
        /// tab信息详情，含tab关联的 windowdetail 信息
        /// </summary>
        private JToken tabDetail = null;

        public string Title
        {
            get
            {
                var detail = GetTabDetail();
                return detail.Value<string>("title");
            }
        }
        public string Url
        {
            get
            {
                var detail = GetTabDetail();
                return detail.Value<string>("url");
            }
        }
        /// <summary>
        /// 页面是是否激活，（激活不代表前置）
        /// </summary>
        public bool IsActive
        {
            get
            {
                var detail = GetTabDetail();
                return detail.Value<bool>("active");
            }
        }
        public DocumentReadyState ReadyState
        {
            get
            {
                var detail = GetTabDetail(true);
                Enum.TryParse(detail.Value<string>("status"), true, out DocumentReadyState readState);
                return readState;
            }
        }
        /// <summary>
        /// 仅用于打开网页组件，判断是否网页正常打开
        /// </summary>
        public PageLoadStatus LoadStatus
        {
            get
            {
                var detail = GetTabDetail(true);
                if (detail.Type == JTokenType.Object)
                {
                    var detailJO = detail.ToObject<JObject>();
                    if (detailJO.TryGetValue<string>("load_status", out string load_status))
                    {
                        Enum.TryParse(load_status, true, out PageLoadStatus loadState);
                        logger.LogDebug($"load_status:{load_status};{loadState}");
                        return loadState;
                    }
                }
                Enum.TryParse(detail.Value<string>("status"), true, out DocumentReadyState readyState);
                return readyState == DocumentReadyState.Loading ? PageLoadStatus.Loading : PageLoadStatus.Success;
            }
        }
        public bool IsFocused
        {
            get
            {
                var detail = GetTabDetail();
                return detail["windowdetail"]?["focused"].ToString().ToLower() == "true";
            }
        }
        public bool IsPopup
        {
            get
            {
                var detail = GetTabDetail();
                return detail["windowdetail"]?["type"].ToString().ToLower() == "popup";
            }
        }
        public bool IsMinimized
        {
            get
            {
                var detail = GetTabDetail();
                return detail["windowdetail"]?["state"].ToString().ToLower() == "minimized";
            }
        }
        public int WindowId
        {
            get
            {
                var detail = GetTabDetail();
                return detail.Value<int>("windowId");
            }
        }
        public int MainWindowHandle
        {
            get
            {
                var detail = GetTabDetail();
                return detail.Value<int>("mainWindowHandle");
            }
        }
        #endregion
        private Rectangle ConvertRectangle(JToken token)
        {
            Rectangle boundingRectangle = Rectangle.Empty;
            try
            {
                /*
                 * 正常情况下不应该加this.DocumentRectangle.X和 this.DocumentRectangle.Y
                 * 这里加上的原因是 校验操作，获取元素范围的时候，输出的元素范围加上了浏览器窗口到document的距离
                 * 所以这里获取document范围的时候，也将document到外层窗口的偏移距离加上
                */
                boundingRectangle = new Rectangle
                {
                    X = token.Value<int>("left"),
                    Y = token.Value<int>("top"),
                    Width = token.Value<int>("width"),
                    Height = token.Value<int>("height"),
                };
            }
            catch (Exception ex)
            {
                boundingRectangle = new Rectangle();
                logger.LogError(ex);
            }
            return boundingRectangle;
        }
        public virtual Rectangle GetIntoViewPortRect()
        {
            var rect = this.WebDataProvider.GetIntoViewPortRect(new ParamModel()
            {
                TabId = this.TabId,
                RunParam = ParamExtensionModel.RunParam
            });
            return ConvertRectangle(rect);
        }
        #endregion

        #region TabDetail
        /// <summary>
        /// 获取tab详情 获取数据将缓存，realtimeData可控制更新数据
        /// </summary>
        /// <param name="realtimeData">实时数据</param>
        /// <returns></returns>
        public virtual JToken GetTabDetail(bool realtimeData = false)
        {
            if (realtimeData || this.tabDetail == null)
            {
                this.tabDetail = this.WebDataProvider.GetTabDetail(new ParamModel()
                {
                    TabId = this.TabId,
                    RunParam = ParamExtensionModel.RunParam
                });
                if (this.tabDetail == null || !this.tabDetail.Any())
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_PageNotFound);
                }
            }
            return this.tabDetail;
        }

        public virtual void ClearTabDetail()
        {
            this.tabDetail = null;
        }
        public virtual JToken GetCompleteTabDetail(bool realtimeData = false)
        {
            var detail = GetTabDetail(realtimeData);
            if (detail["mainWindowHandle"] == null)
            {
                detail["mainWindowHandle"] = (long)this.BrowserWindowInfo.Handle;
            }
            return detail;
        }

        #endregion
        public virtual Image CaptureByTab()
        {
            var base64 = this.WebDataProvider.TabCapture(new ParamModel()
            {
                TabId = this.TabId,
                RunParam = ParamExtensionModel.RunParam
            }).ToString();
            return base64.DataURIToImage();
        }
        public virtual Image CaptureByDebugger(int height, int perHeight = 1000)
        {
            bool metricsOverridden = false;
            try
            {
                var totalHeight = this.ScrollToBottom(height);

                var mayScrollable = this.HasScrollEvent();
                if (mayScrollable)
                {
                    this.ScrollToBottomRecurve(height, preHeight: totalHeight);
                }

                // 一些带高度检测逻辑的页面截图可能出现问题, 手动返回页眉
                this.ScrollTopUnitStop(0);

                // Scroll-triggered lazy loading can change the page height.
                var layout = this.WebDataProvider.SendCommand(this.TabId, "Page.getLayoutMetrics", new JObject { });
                var zoom = layout["visualViewport"].Value<double>("zoom");
                this.WebDataProvider.SendCommand(this.TabId, "Overlay.setShowViewportSizeOnResize", new JObject { { "show", false } });

                // contentSize为过时的写法, 此处兼容360
                var layoutRoot = layout["cssContentSize"] ?? layout["contentSize"];
                var captureHeight = layoutRoot.Value<int>("height");
                var captureWidth = layoutRoot.Value<int>("width");

                var ratio = this.WebDataProvider.ExecuteScriptOnFrame(new ParamExecuteScriptOnFrameModel()
                {
                    TabId = this.TabId,
                    code = "window.devicePixelRatio"
                }).Value<decimal>();

                this.WebDataProvider.SendCommand(this.TabId, "Emulation.resetPageScaleFactor", new JObject { }, keepAttached: true);
                this.WebDataProvider.SendCommand(this.TabId, "Emulation.setDeviceMetricsOverride", new JObject {
                    { "width", (int)(captureWidth*zoom) },//此部分调整主要适配浏览器在缩放比和dpi非100%时的兼容（其他改动下同），需要测试介入再去验证
                    { "height", (int)(captureHeight*zoom) },
                    { "deviceScaleFactor", ratio },
                    { "mobile", false },
                    { "scale", 1 / zoom }
                }, keepAttached: true);
                metricsOverridden = true;

                // 等待浏览器绘制
                Thread.Sleep(1000);

                var pages = Enumerable.Range(0, (int)Math.Ceiling((double)captureHeight / (double)perHeight)).ToList();

                var images = pages.Select(index =>
                {
                    var currentY = index * perHeight;
                    var clipHeight = Math.Min(perHeight, captureHeight - currentY);
                    return this.WebDataProvider.SendCommand(this.TabId, "Page.captureScreenshot", new JObject {
                        { "format", "png" },
                        { "quality", 100 },
                        { "fromSurface", true },
                        { "clip", new JObject {
                            { "x", 0 },
                            { "y", (int)(currentY * zoom) },  // Y坐标乘以缩放比（映射到实际视口）
                            { "width", (int)(captureWidth * zoom) },  // 宽度乘以缩放比
                            { "height", (int)(clipHeight * zoom) },    // 高度乘以缩放比
                            { "scale", 1 / zoom }  // 与setDeviceMetricsOverride的scale保持一致
                        }}
                    }, 30000, keepAttached: true).Value<string>("data").DataURIToImage();
                }).ToList();

                captureHeight = Math.Min(height, captureHeight);
                return ImageProcesser.MergeImages(images, captureWidth, captureHeight, perHeight, ratio);
            }
            finally
            {
                try
                {
                    if (metricsOverridden)
                        this.WebDataProvider.SendCommand(this.TabId, "Emulation.clearDeviceMetricsOverride", new JObject { }, keepAttached: true);
                }
                finally
                {
                    this.WebDataProvider.EndInspectNetwork(new ParamModel() { TabId = this.TabId });
                }
            }
        }


        #region 网络监听
        public virtual void EndInspectNetwork() => this.WebDataProvider.EndInspectNetwork(new ParamModel() { TabId = this.TabId });
        public virtual void StartInspectNetwork()
        {
            this.WebDataProvider.StartInspectNetwork(new ParamModel()
            {
                TabId = this.TabId,
                RunParam = ParamExtensionModel.RunParam
            });
        }
        public virtual string GetInspectNetwork(string type, string url, bool isRegex, string filePath)
        {
            var records = this.WebDataProvider.GetInspectNetwork(new ParamInspectNetworkModel()
            {
                TabId = this.TabId,
                type = type,
                url = url,
                isRegex = isRegex
            });
            var dir = new FileInfo(filePath).FullName;
            var tmpFile = Path.Combine(dir, $"datafile_{(int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds}.json");
            Directory.CreateDirectory(dir);
            File.WriteAllText(tmpFile, records.ToJson(), Encoding.UTF8);
            return tmpFile;
        }

        #endregion

        #region 元素拾取

        public virtual bool InspectNode(IntPtr rootWnd, Point point, string selector, out IGuiNode node)
        {
            node = null;
            if (this.ClientWndHandle == IntPtr.Zero)
            {
                this.ClientWndHandle = NativeMethods.WindowFromPoint(point);
            }
            //if (TabId == 0)
            //{
            //    this.TabId = this.Browser.AcquireCurrentActiveTab();
            //}
            logger.LogDebug($"ElementGuiNodeFormPoint");
            ElementGuiNode guiNode = this.ElementGuiNodeFormPoint(point);
            logger.LogDebug($"ElementGuiNodeFormPoint end");

            if (guiNode != null)
            {
                logger.LogDebug($"BuildElementBounding end");

                guiNode.SimilarSelector = selector;//记录相似元素selector,便于生成selector时使用
                node = guiNode;
                return true;
            }
            return false;
        }


        public virtual ElementGuiNode ElementGuiNodeFormPoint(Point screenPoint)
        {
            var clientPoint = ScreenToClient(screenPoint);
            if (clientPoint == null) return null;

            try
            {
                var retObj = this.WebDataProvider.ElementFormPoint(this.TabId, clientPoint.Value) as JObject;
                if (retObj == null)
                {
                    logger.LogInformation($"ElementGuiNodeFormPoint retObj is null, TabId={TabId}, screenPoint={screenPoint}");
                    return null;
                }
                var boundingObj = retObj["bounding"] as JObject;
                var uniqueId = retObj.Value<string>("uniqueId");

                if (boundingObj == null || string.IsNullOrWhiteSpace(uniqueId))
                {
                    logger.LogInformation($"ElementGuiNodeFormPoint boundingObj is null or uniqueId is empty, TabId={TabId}, screenPoint={screenPoint}");
                    return null;
                }

                var eleNode = this.CreateElementGuiNode(uniqueId);

                var boundingRectangle = new Rectangle
                {
                    X = boundingObj.Value<int>("left"),
                    Y = boundingObj.Value<int>("top"),
                    Width = boundingObj.Value<int>("width"),
                    Height = boundingObj.Value<int>("height")
                };

                eleNode.SetCaupureBounding(boundingRectangle);
                eleNode.Attributes = retObj["attributes"]?.ToString();

                try
                {
                    var dropdownObj = retObj["dropdown"] as JObject;
                    if (dropdownObj?.Value<bool?>("success") == true)
                    {
                        eleNode.Dropdown = dropdownObj["result"]?.ToString();
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "解析 dropdown 相关数据异常，不影响正常元素拾取");
                }

                return eleNode;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex,
                    "ElementGuiNodeFormPoint 解析元素失败, TabId={TabId}, screenPoint={ScreenPoint}",
                    this.TabId, screenPoint);
                return null;
            }
        }
        /// <summary>
        /// 屏幕坐标转页面坐标
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <returns></returns>
        public virtual Point? ScreenToClient(Point screenPoint)
        {
            if (NativeMethods.ScreenToClient(this.ClientWndHandle, ref screenPoint))
            {
                return screenPoint;
            }
            else
                return null;
        }
        #endregion

        #region 检测是否是IE模式，并抛出异常
        public virtual void ThrowIfEmbeddedIE()
        {
            if (this.BrowserWindowInfo == null)
            {
                return;
            }
            if (this.BrowserWindowInfo.IsEmbeddedIE())
            {
                this.BrowserWindowInfo.WindowType |= WindowType.IE;
                this.BrowserWindowInfo.SpecifiedWindow = true;
                this.BrowserWindowInfo.SpecifiedTabId = -1;
                throw new RPAException(this.GuiType, eErrorMessageType.Error_EmbeddedIEMode);
            }
        }
        #endregion

        #region 匹配(查找)元素 相关
        /// <summary>
        /// 根据selelctor 中的信息 获取TabId
        /// 对象内部使用，其它使用 Browser.MatchsTabId
        /// </summary>
        /// <param name="selectorObj"></param>
        /// <returns></returns>
        protected virtual int GetTabIdOrDefault(List<SelectorProps> selectorObj)
        {
            List<IntPtr> wndHandles = null;
            if (this.BrowserWindowInfo.SpecifiedWindow)
            {
                if (this.BrowserWindowInfo.WindowType.HasFlag(WindowType.IE) && this.BrowserWindowInfo.WindowType > WindowType.IE)
                    wndHandles = new List<IntPtr>() { this.BrowserWindowInfo.Handle };
                else
                {
                    this.BrowserWindowInfo.SpecifiedWindowId = this.Browser.GetWindowIdFromWindowHandle(this.BrowserWindowInfo.Handle);
                    if (this.BrowserWindowInfo.SpecifiedWindowId <= 0)
                    {
                        throw new RPAException("指定的窗口对象，未匹配到合适的网页".I18n("controlType_BrowserTabPage_CA60FE82"));//TODO:需要执行错误类型，最后补充
                    }
                }
            }
            return this.GetTabIdOrDefault(selectorObj, wndHandles);
        }
        /// <summary>
        /// 根据selelctor 中的信息 获取tabId
        /// 对象内部使用，其它使用 Browser.MatchsTabId 
        /// </summary>
        /// <param name="selectorObj"></param>
        /// <param name="wndHandles">IE 浏览器使用，如果未指定匹配窗口，则使用所有IE浏览器窗口</param>
        /// <returns></returns>
        /// <exception cref="RPAException"></exception>
        protected int GetTabIdOrDefault(List<SelectorProps> selectorObj, List<IntPtr> wndHandles)
        {
            var tabId = -1;
            PredicateFilter<BrowserTabPage> matchFilter = new PredicateFilter<BrowserTabPage>();
            if (this.BrowserWindowInfo.SpecifiedWindowId > 0)
            {
                matchFilter.AddLast(tabPage => tabPage.WindowId == this.BrowserWindowInfo.SpecifiedWindowId);
            }
            //提取 首节点中的标题和url信息（tab节点）
            if (selectorObj?.Count > 0 && selectorObj[0].Props != null && selectorObj[0].Props.Count > 0)
            {
                var titleProp = selectorObj[0]?.Props.Find(p => p.Name == ConstFields.DATACOLECTING_PAGE_TITLE && p.Accurate.As<bool>());
                var urlProp = selectorObj[0]?.Props.Find(p => p.Name == ConstFields.DATACOLECTING_PAGE_URL && p.Accurate.As<bool>());

                var cormpareTitle = CormpareFactory.GenerateStringMode(titleProp?.Pattern ?? "");
                var cormpareUrl = CormpareFactory.GenerateStringMode(urlProp?.Pattern ?? "");

                matchFilter.AddLast(tabPage => tabPage.IsMatch(titleProp?.Value, cormpareTitle, urlProp?.Value, cormpareUrl, true));

                var tabpage = this.Browser.MatchTabPageByPriority(matchFilter, wndHandles);
                if (tabpage != null)
                {
                    tabId = tabpage.TabId;
                }
                else
                {
                    matchFilter.RemoveLast();
                }
            }
            logger.LogDebug($"匹配TabId:{tabId}");
            //未匹配到页面时，默认使用当前页
            if (tabId == -1)
            {
                logger.LogDebug($"尝试获取 CurrentActiveTabId");
                tabId = this.Browser.AcquireCurrentActiveTab(wndHandles, matchFilter).TabId;
                logger.LogDebug($"CurrentActiveTabId：{tabId}");
            }
            if (tabId == -1)
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_PageNotFound);
            }
            return tabId;
        }
        public virtual string GetElementsAll(string tagNames, string attrKeywords)
        {
            var idsStr = this.WebDataProvider.QueryElementAll(this.TabId, tagNames, attrKeywords);
            return string.IsNullOrEmpty(idsStr) ? "[]" : idsStr;
            //return string.IsNullOrEmpty(idsStr) ? new string[0] : idsStr.ToObject<string[]>();
        }
        #region 获取可见区域内的元素信息 x y w h tagname uid

        public virtual ViewElementsDataModel GetViewElementsData()
        {
            this.SyncDocumentRectangle();
            var elementsDataJson = this.WebDataProvider.GetViewElementsData(this.TabId);
            var ret = string.IsNullOrEmpty(elementsDataJson) ? "{}" : elementsDataJson;
            var elementsData = ret.ToObject<ViewElementsDataModel>();
            SetViewElementsDataOffset(elementsData);
            return elementsData;
        }
        public virtual string GetWebCode()
        {
            //this.WebDataProvider.SetWebUnqueId(this.TabId);
            string webCode = this.WebDataProvider.GetWebCode(this.TabId);
            return webCode;
        }
        private void SetViewElementsDataOffset(ViewElementsDataModel elementsData)
        {
            if (elementsData == null)
            {
                return;
            }
            if (elementsData.bounding != null)
            {
                if (elementsData.bounding.width > 0 || elementsData.bounding.height > 0)
                {
                    elementsData.bounding.x += this.DocumentRectangle.X;
                    elementsData.bounding.y += this.DocumentRectangle.Y;
                }
                if (elementsData.children == null)
                {
                    return;
                }
                for (int i = 0; i < elementsData.children.Count; i++)
                {
                    SetViewElementsDataOffset(elementsData.children[i]);
                }
            }
        }
        #endregion
        public virtual string GetNextElementBuildSelector(string cssSelector, bool innerTextFirst)
        {
            var retJToken = this.WebDataProvider.GetNextElementBuildSelector(new ParamcssSelectorModel()
            {
                TabId = this.TabId,
                cssSelector = cssSelector,
                innerTextFirst = innerTextFirst,
                RunParam = ParamExtensionModel.RunParam
            });
            return retJToken.ToString();
        }
        //public virtual ElementGuiNode GetElementFromSelector(string selector, string cssSelector, bool isActive, string version, string queryMode, int timeout = 0)
        //{
        //    string userDefinedID = this.GetElementIdFromSelector(selector, cssSelector, isActive, version, queryMode, timeout);
        //    return this.CreateElementGuiNode(userDefinedID);
        //}
        public virtual ElementGuiNode GetElementFromSelector(string selector, string cssSelector, bool isActive, string version, string queryMode, string defaultUserDefinedID = "", bool discernHidden = false)
        {
            string userDefinedID = !string.IsNullOrEmpty(defaultUserDefinedID) ? defaultUserDefinedID : this.GetElementIdFromSelector(selector, cssSelector, isActive, version, queryMode, discernHidden);
            return this.CreateElementGuiNode(userDefinedID);
        }

        public virtual ElementGuiNode GetElementFromSelector(string selector, string cssSelector, bool isActive, string version, bool discernHidden)
        {
            return GetElementFromSelector(selector, cssSelector, isActive, version, "default", "", discernHidden);
        }
        public virtual ElementGuiNode GetElementFromSelectorOld(string selector)
        {
            string userDefinedID = this.GetElementIdFromSelectorOld(selector);
            return this.CreateElementGuiNode(userDefinedID);
        }
        public virtual string GetElementIdFromSelector(string selector, string cssSelector, bool isActive, string version, string queryMode, bool discernHidden)
        {
            try
            {
                string userDefinedID;
                var jsonObj = JsonConvert.DeserializeObject<List<SelectorProps>>(selector);
                //提取 首节点中的标题和url信息（tab节点）
                if (this.TabId < 0)
                {
                    //tab对象ID后补，不可删除
                    this.TabId = this.GetTabIdOrDefault(jsonObj);
                }
                if (isActive)
                    this.ActiveTab();
                ThrowIfEmbeddedIE();

                var pinpath = jsonObj.FirstOrDefault(p => p.Name == ConstFields.PINSELECTOR_TAG_NAME && p.Accurate.As<bool>());
                var UDIDProp = pinpath?.Props.Find(p => p.Name == ConstFields.PINSELECTOR_PROP_UDID && p.Accurate.As<bool>());
                if (UDIDProp != null)
                {
                    return UDIDProp.Value;
                }

                ParamQuerySelectorModel paramQuerySelectorModel = null;
                if (!string.IsNullOrEmpty(cssSelector))
                {
                    paramQuerySelectorModel = new ParamQuerySelectorModel()
                    {
                        TabId = this.TabId,
                        selector = selector,
                        cssSelector = cssSelector,
                        version = version,
                        mode = queryMode,
                        RunParam = ParamExtensionModel.RunParam,
                        discernHidden = discernHidden
                    };
                    if (queryMode == "fullXpath")
                    {
                        userDefinedID = this.WebDataProvider.QuerySelectorByXpath(paramQuerySelectorModel);
                    }
                    else
                    {
                        userDefinedID = this.WebDataProvider.QuerySelector(paramQuerySelectorModel);
                    }
                }
                else
                {
                    paramQuerySelectorModel = new ParamQuerySelectorModel()
                    {
                        TabId = this.TabId,
                        selector = selector,
                        version = version,
                        RunParam = ParamExtensionModel.RunParam,
                        discernHidden = discernHidden
                    };
                    userDefinedID = this.WebDataProvider.QuerySelector(paramQuerySelectorModel);
                }
                logger.LogDebug($"userDefinedID:{userDefinedID}");
                if (string.IsNullOrEmpty(userDefinedID))
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                return userDefinedID;
            }
            catch (Exception ex)
            {
                throw ex is RPAException ? ex : new RPAException(eErrorMessageType.Error_NotFoundElement, ex);
            }
        }

        public virtual bool VerifyTab(string selector)
        {
            var selectorObj = JsonConvert.DeserializeObject<List<SelectorProps>>(selector);
            if (this.TabId < 0)
            {
                //tab对象ID后补，不可删除
                this.TabId = this.GetTabIdOrDefault(selectorObj);
            }
            if (this.TabId > -1)
            {
                if (selectorObj?.Count > 0 && selectorObj[0].Props != null && selectorObj[0].Props.Count > 0)
                {
                    //提取 首节点中的url信息（tab节点）
                    var url = selectorObj[0]?.Props.Find(p => p.Name == ConstFields.DATACOLECTING_PAGE_URL);
                    var result = this.GetTabDetail();
                    if (result != null)
                    {
                        var u = result.Value<string>("url");
                        if (new Uri(url?.Value).GetLeftPart(UriPartial.Path).EqualIgnoreCase(new Uri(u).GetLeftPart(UriPartial.Path)))
                        {
                            return true;
                        }
                    }
                }
            }
            throw new RPAException(this.GuiType, eErrorMessageType.Error_PageNotFound);
        }
        public virtual string GetElementIdFromSelectorOld(string selector)
        {
            try
            {
                string userDefinedID;
                string cssSelector; ;
                var jsonObj = (JToken)JsonHelper.JsonDesByNewtonJson(selector);
                var firsrSelectorNode = jsonObj.FirstOrDefault();
                cssSelector = firsrSelectorNode.Value<string>("cssSelector");
                string title = firsrSelectorNode.Value<string>("title");
                string url = firsrSelectorNode.Value<string>("url");
                var tabpage = this.Browser.MatchTabPage(title, url);
                if (tabpage != null)
                {
                    this.TabId = tabpage.TabId;
                }

                if (this.TabId <= 0)
                {
                    if (string.IsNullOrEmpty(cssSelector))
                    {
                        userDefinedID = this.QuerySelectorOld(selector);
                        if (string.IsNullOrEmpty(userDefinedID))
                            userDefinedID = this.QueryCssSelector(selector);
                    }
                    else
                    {
                        userDefinedID = this.QueryCssSelector(selector);
                        if (string.IsNullOrEmpty(userDefinedID))
                            userDefinedID = this.QuerySelectorOld(selector);
                    }
                    if (string.IsNullOrEmpty(userDefinedID))
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                    return userDefinedID;
                }
                else
                {
                    Thread.Sleep(100);
                    logger.LogInformation("未获取到ChromeTab信息");
                }

                return null;
            }
            catch (Exception ex)
            {
                if (ex is RPAException)
                    throw ex;
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Chrome Exception");
                return null;
            }
        }

        public string GetElementFromSelectorAll(string selector,
           string version,
           string cssSelector = null,
           string resultAttrName = null,
           ElementRelativeType relativeType = ElementRelativeType.Similar,
           string positionNum = null,
           string filter = null,
           bool isActive = true,
           bool discernHidden = false)
        {
            return GetElementFromSelectorAll(selector, version, "default", cssSelector, resultAttrName, relativeType, positionNum, filter, isActive, discernHidden);
        }
        public string GetElementFromSelectorAll(string selector,
           string version,
           string queryMode,
           string cssSelector = null,
           string resultAttrName = null,
           ElementRelativeType relativeType = ElementRelativeType.Similar,
           string positionNum = null,
           string filter = null,
           bool isActive = true,
           bool discernHidden = false)
        {
            try
            {
                var jsonObj = JsonConvert.DeserializeObject<List<SelectorProps>>(selector);
                //提取 首节点中的标题和url信息（tab节点）
                if (this.TabId < 0)
                {
                    //tab对象ID后补，不可删除
                    this.TabId = this.GetTabIdOrDefault(jsonObj);
                }
                if (isActive)
                    this.ActiveTab();
                ThrowIfEmbeddedIE();

                if (resultAttrName == "bounding")
                {
                    this.SyncDocumentRectangle();//获取一下窗口句柄 获得元素的相对坐标 
                }
                string uniqueIds = string.Empty;
                if (!string.IsNullOrEmpty(cssSelector))
                {
                    uniqueIds = this.QuerySelectorAll(new ParamQuerySelectorModel
                    {
                        TabId = this.TabId,
                        selector = selector,
                        cssSelector = cssSelector,
                        version = version,
                        mode = queryMode,
                        RelateElementId = null,
                        resultAttrName = resultAttrName,
                        index = -1,
                        RunParam = ParamExtensionModel.RunParam,
                        discernHidden = discernHidden
                    });
                }
                else
                {
                    uniqueIds = this.QuerySelectorAll(new ParamQuerySelectorModel
                    {
                        TabId = this.TabId,
                        selector = selector,
                        version = version,
                        index = -1,
                        RelateElementId = null,
                        resultAttrName = resultAttrName,
                        filter = filter,
                        relativeType = relativeType,
                        positionNum = positionNum,
                        RunParam = ParamExtensionModel.RunParam,
                        discernHidden = discernHidden
                    });
                }
                JArray elementIds = JArray.Parse(uniqueIds);
                if (elementIds.Count == 0)
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_NotFoundElement);
                return elementIds.ToString();
            }
            catch (Exception ex)
            {
                throw ex is RPAException ? ex : new RPAException(this.GuiType, eErrorMessageType.Error_NotFoundElement, ex);
            }
        }
        public IGuiNode[] QueryTargetSelectorAll(ParamQueryTargetSelectorAllModel paramModel)
        {
            var cssSelector = ExtractViewData.GetQueryModeSelector(paramModel.selector, out string queryMode);
            var selector = UiCommon.ExtractSelectors(paramModel.selector);
            //var result = this.QuerySelectorAll(this.TabId, selector, cssSelector, paramModel.selectorVersion, queryMode, index: -1, parentElementId: paramModel.parentElement, "uniqueId");
            var result = this.QuerySelectorAll(new ParamQuerySelectorModel
            {
                TabId = this.TabId,
                selector = selector,
                cssSelector = cssSelector,
                version = paramModel.selectorVersion,
                mode = queryMode,
                RelateElementId = paramModel.RelateElementId,
                ElementRelateMode = paramModel.ElementRelateMode,
                resultAttrName = "uniqueId",
                index = -1,
                RunParam = ParamExtensionModel.RunParam,
                discernHidden = paramModel.discernHidden
            });
            var array = result.ToObject<List<string>>();
            var nodes = this.CreateElementGuiNodes(array);
            return nodes.ToArray<IGuiNode>();
        }
        //private string QuerySelectorAll(int TabId, string selector, string cssSelector, string version, int index = -1, string parentElementId = null, string resultAttrName = null)
        //{
        //    return QuerySelectorAll(TabId, selector, cssSelector, version, "default", index, parentElementId, resultAttrName);
        //}
        //private string QuerySelectorAll(int TabId, string selector, string cssSelector, string version, string queryMode, int index = -1, string parentElementId = null, string resultAttrName = null)
        //{
        //    var model = new ParamQuerySelectorModel()
        //    {
        //        TabId = this.TabId,
        //        selector = selector,
        //        cssSelector = cssSelector,
        //        version = version,
        //        mode = queryMode,
        //        parentElementId = parentElementId,
        //        resultAttrName = resultAttrName,
        //        index = index,
        //        RunParam = ParamExtensionModel.RunParam
        //    };
        //    var uniqueIds = this.WebDataProvider.QuerySelectorAll(model);
        //    return ConvertUniqueIds(uniqueIds, resultAttrName);
        //}
        private string ConvertUniqueIds(string uniqueIds, string resultAttrName)
        {
            var content = JArray.Parse(uniqueIds);
            if (resultAttrName == "bounding")
            {
                List<Rectangle> retRectList = new List<Rectangle>();
                foreach (var item in content)
                {
                    JObject itemJObj = JObject.Parse(item.ToString());
                    int X = itemJObj.Value<int>("left");
                    int Y = itemJObj.Value<int>("top");
                    int Width = itemJObj.Value<int>("width");
                    int Height = itemJObj.Value<int>("height");
                    Rectangle boundingRect = new Rectangle
                    {
                        X = (int)(X / NativeMethods.ScaleX) + this.DocumentRectangle.X,
                        Y = (int)(Y / NativeMethods.ScaleY) + this.DocumentRectangle.Y,
                        Width = (int)(Width / NativeMethods.ScaleX),
                        Height = (int)(Height / NativeMethods.ScaleY),
                    };
                    retRectList.Add(boundingRect);
                }
                return retRectList.ToJson();
            }
            else
            {
                return content.ToString();
            }
        }
        /// <summary>
        /// 获取QuerySelectorAll 并对返回的数据 进行数据逻辑加工
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="version"></param>
        /// <param name="index"></param>
        /// <param name="parentElementId"></param>
        /// <param name="resultAttrName"></param>
        /// <param name="filter"></param>
        /// <param name="relativeType"></param>
        /// <param name="positionNum"></param>
        /// <returns></returns>
        //private string QuerySelectorAll(string selector, string version, int index = -1, string parentElementId = null, string resultAttrName = null, string filter = null, ElementRelativeType relativeType = ElementRelativeType.Similar, string positionNum = null)
        //{
        //    var model = new ParamQuerySelectorModel()
        //    {
        //        TabId = this.TabId,
        //        selector = selector,
        //        version = version,
        //        index = index,
        //        parentElementId = null,
        //        resultAttrName = resultAttrName,
        //        filter = filter,
        //        relativeType = relativeType,
        //        positionNum = positionNum,
        //        RunParam = ParamExtensionModel.RunParam
        //    };
        //    var uniqueIds = this.WebDataProvider.QuerySelectorAll(model);
        //    return ConvertUniqueIds(uniqueIds, resultAttrName);
        //}
        private string QuerySelectorAll(ParamQuerySelectorModel queryModel)
        {
            var uniqueIds = "";
            if (queryModel.mode == "fullXpath")
            {
                uniqueIds = this.WebDataProvider.QuerySelectorByXpathAll(queryModel);
            }
            else
            {
                uniqueIds = this.WebDataProvider.QuerySelectorAll(queryModel);
            }
            return ConvertUniqueIds(uniqueIds, queryModel.resultAttrName);
        }
        private string QuerySelectorOld(string selector)
        {
            return this.WebDataProvider.QuerySelectorOld(this.TabId, this.GuiType.ToString(), selector);
        }
        private string QueryCssSelector(string selector)
        {
            return this.WebDataProvider.QueryCssSelector(this.TabId, this.GuiType.ToString(), selector, 0, null);
        }
        private List<List<string>> QueryCollectionData(string parentSelector, string selectorDataList)
        {
            var model = new ParamQueryCollectionDataModel()
            {
                TabId = this.TabId,
                parentSelector = parentSelector,
                selectorDataList = selectorDataList
            };
            return this.WebDataProvider.QueryCollectionData(model).ToObject<List<List<string>>>();
        }
        private List<List<string>> QueryCollectionData(ParamDataCollect_V2Model paramModel)
        {
            var ret = this.WebDataProvider.QueryCollectionData(
                        this.TabId,
                        paramModel.DataCollectConfig.CollectionConfig,
                        paramModel.ColorGuidanceConfig);
            if (ret == null || !ret.Any())
            {
                return new List<List<string>>();
            }
            return ret.ToObject<List<List<string>>>();
        }
        private (List<List<string>> collectionData, ElementGuiNode markedElement) ScrollAndCollectionData(ParamDataCollect_V2Model paramModel)
        {
            var ret = this.WebDataProvider.ScrollAndCollectionData(
                        this.TabId,
                        paramModel.DataCollectConfig.CollectionConfig,
                        paramModel.ColorGuidanceConfig,
                        ((ElementGuiNode)paramModel.MarkedElement)?.PageElement.UserDefinedID);
            if (ret == null ||
                ret.Type != JTokenType.Object ||
                ret["data"] == null ||
                ret["markedElementId"] == null)
            {
                return (new List<List<string>>(), null);
            }
            return (JsonConvert.DeserializeObject<List<List<string>>>(ret["data"].ToString()), CreateElementGuiNode(ret["markedElementId"].ToString()));
        }
        public string GetElementFromSelectorAllOld(string selector)
        {
            //IsExecution();
            try
            {
                //chrome = new Chrome();
                var jsonObj = (JToken)JsonHelper.JsonDesByNewtonJson(selector);
                string cssSelector = string.Empty;
                //for (int i = 0; i < 10; i++)//找10 次
                //{
                var TabId = -1;
                BrowserTabPage tabPage = null;
                foreach (var item in jsonObj)
                {
                    cssSelector = item.Value<string>("cssSelector");

                    if (item.Value<string>("title") != null && item.Value<string>("url") != null)
                    {
                        if (!string.IsNullOrEmpty(item.Value<string>("title").ToString()) && !string.IsNullOrEmpty(item.Value<string>("url").ToString()))
                            tabPage = this.Browser.MatchTabPage(item.Value<string>("title").ToString(), item.Value<string>("url").ToString());
                        //chromeTab = chrome.Acquire(item.Value<string>("title").ToString(), item.Value<string>("url").ToString());

                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(item.Value<string>("title").ToString()))
                            tabPage = this.Browser.MatchTabPage(item.Value<string>("title").ToString());
                        else if (!string.IsNullOrEmpty(item.Value<string>("url").ToString()))
                            tabPage = this.Browser.MatchTabPage(url: item.Value<string>("url").ToString());
                        else
                            tabPage = this.Browser.MatchTabPage();
                    }
                    break;
                }
                if (tabPage != null)
                {
                    //tab对象ID后补，不可删除
                    this.TabId = TabId;
                }
                this.IsLoaded(1000);

                List<string> elementIds = new List<string>();
                if (!string.IsNullOrEmpty(cssSelector))
                {
                    var userDefinedID = this.QuerySelectorOld(selector);
                    if (string.IsNullOrEmpty(userDefinedID))
                        userDefinedID = this.QueryCssSelector(selector);
                    elementIds.Add(userDefinedID);
                }
                else
                {
                    var uniqueIds = this.WebDataProvider.QuerySelectorAllOld(this.TabId, selector);
                    elementIds = uniqueIds.ToObject<List<string>>();
                }
                if (elementIds.Count == 0)
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_NotFoundElement);

                var parentNodes = this.CreateElementGuiNodes(elementIds);
                return parentNodes.Select(node =>
                    node.PageElement.BuildSelector(false)
                ).ToJson();

                //foreach (var elementId in elementIds)
                //{
                //    elements.Add(this.BuildSelector(elementId.ToString()));
                //}
                //return elements.ToString();


                //if (chromeTab != null)
                //{
                //    JArray elements = new JArray();
                //    chrome.IsLoaded(chromeTab.TabId);
                //    getTitle = Title(chromeTab.TabId);
                //    JArray elementIds = new JArray();
                //    if (!string.IsNullOrEmpty(cssSelector))
                //    {
                //        userDefinedID = chromeTab.QuerySelectorOld(selector);
                //        if (string.IsNullOrEmpty(userDefinedID))
                //            userDefinedID = chromeTab.QueryCssSelector(selector, 0, null);
                //        elementIds.Add(userDefinedID);
                //    }
                //    else
                //    {
                //        var uniqueIds = chromeTab.QuerySelectorAllOld(selector);
                //        elementIds = JArray.Parse(uniqueIds);
                //    }
                //    if (elementIds.Count == 0)
                //        throw new ChromeException(eErrorMessageType.Error_NotFoundElement);
                //    foreach (var elementId in elementIds)
                //    {
                //        elements.Add(chromeTab.BuildSelector(elementId.ToString()));
                //    }
                //    return elements.ToString();
                //}
                //else
                //{
                //    Thread.Sleep(100);
                //    logger.LogInformation("未获取到ChromeTab信息");
                //}
                //}
                //return null;
            }
            catch (Exception ex)
            {
                if (ex is RPAException)
                    throw ex;
                throw new RPAException(this.GuiType, eErrorMessageType.Error_NotFoundElement);
                //return null;
            }
        }


        #endregion

        #region 数据采集
        /// <summary>
        /// 690版本及之后，如非必要，不要修改
        /// </summary>
        /// <param name="sameParentsProps"></param>
        /// <param name="selectorDataList"></param>
        /// <returns></returns>
        /// <exception cref="RPAException"></exception>
        public List<List<string>> GetCollectDataFromSelector(List<SelectorProps> sameParentsProps, List<CollectData_SelectorData> selectorDataList)
        {
            //IsExecution();
            try
            {
                List<List<string>> collectDataList = new List<List<string>>();
                int timeout = 10000;//数据采集最大等待时间10秒
                this.IsLoaded(timeout);
                //getTitle = Title(chromeTab.TabId);
                string uniqueIds = string.Empty;
                collectDataList = this.QueryCollectionData(sameParentsProps.ToJsonNoneFormating(), selectorDataList.ToJsonNoneFormating());
                return collectDataList;
            }
            catch (Exception e)
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_NotFoundElement, e);
            }
        }
        /// <summary>
        /// 新版数据采集 Version>=6.9.0
        /// </summary>
        /// <param name="sameParentsProps"></param>
        /// <param name="selectorDataList"></param>
        /// <returns></returns>
        /// <exception cref="RPAException"></exception>
        public virtual List<List<string>> GetCollectDataFromConfig(ParamDataCollect_V2Model paramModel)
        {
            if (this.TabId < 0)
            {
                List<SelectorProps> selectorObj = new List<SelectorProps>();
                if (paramModel.IsColorGuidance && paramModel.IsWeb)
                {
                    selectorObj.Add(BuildTabSelectorNodeByPageInfo(paramModel.DataCollectConfig.PageInfo));
                }
                this.TabId = this.GetTabIdOrDefault(selectorObj);
            }
            this.ActiveTab();
            this.ThrowIfEmbeddedIE();
            try
            {
                List<List<string>> collectDataList = new List<List<string>>();
                if (!paramModel.IsColorGuidance)
                {
                    int timeout = 10000;//数据采集最大等待时间10秒
                    this.IsLoaded(timeout);
                }
                collectDataList = this.QueryCollectionData(paramModel);
                return collectDataList;
            }
            catch (Exception e)
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_NotFoundElement, e);
            }
        }
        public virtual (List<List<string>> collectionData, ElementGuiNode markedElement) GetCollectWaterfallDataFromConfig(ParamDataCollect_V2Model paramModel)
        {

            if (this.TabId < 0)
            {
                if (paramModel.MarkedElement != null)
                {
                    this.TabId = ((ElementGuiNode)paramModel.MarkedElement).PageElement.TabPage.TabId;
                }
                List<SelectorProps> selectorObj = new List<SelectorProps>();
                if (paramModel.IsColorGuidance && paramModel.IsWeb)
                {
                    selectorObj.Add(BuildTabSelectorNodeByPageInfo(paramModel.DataCollectConfig.PageInfo));
                }
                this.TabId = this.GetTabIdOrDefault(selectorObj);
            }
            this.ActiveTab();
            this.ThrowIfEmbeddedIE();
            try
            {
                List<List<string>> collectDataList = new List<List<string>>();
                if (!paramModel.IsColorGuidance)
                {
                    int timeout = 10000;//数据采集最大等待时间10秒
                    this.IsLoaded(timeout);
                }
                var ret = this.ScrollAndCollectionData(paramModel);
                return ret;
            }
            catch (Exception e)
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_NotFoundElement, e);
            }
        }
        private SelectorProps BuildTabSelectorNodeByPageInfo(PageInfo PageInfo)
        {
            var titleProp = SelectorPropHelper.BuildSelectorProp(
                        ConstFields.DATACOLECTING_PAGE_TITLE,
                        PageInfo.WindowTitle,
                        ConstFields.PATTERN_EQUAL,
                        ConstFields.SELECTOR_SELECTORPROPS_ACCURATE_TURE);

            var urlProp = SelectorPropHelper.BuildSelectorProp(
                ConstFields.DATACOLECTING_PAGE_URL,
                PageInfo.PageUrl,
                ConstFields.PATTERN_EQUAL,
                ConstFields.SELECTOR_SELECTORPROPS_ACCURATE_TURE);

            return SelectorPropHelper.BuildSelectorNode(
                ConstFields.DATACOLECTING_SELECTORPROPS_TAB,
                ConstFields.SELECTOR_SELECTORPROPS_ACCURATE_TURE,
                ConstFields.SELECTOR_SELECTORPROPS_TYPE_WEB,
                titleProp, urlProp);
        }
        #endregion

        #region 页面是否加载完成
        public bool IsLoaded(DocumentReadyState readyState)
        {
            try
            {
                var curReadState = this.ReadyState;
                logger.LogDebug($"IsLoaded网页当前状态{curReadState.ToString()}");
                if ((curReadState & readyState) > 0)
                {
                    logger.LogInformation($"IsLoaded网页当前状态{curReadState.ToString()}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"IsLoaded 执行异常，此处按照未加载完成（false）处理");
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"本次错错误日志只作为分析使用，可忽略");
            }
            return false;
        }
        /// <summary>
        /// 是否加载完成，根据TabDetail信息获取加载状态
        /// </summary>
        /// <param name="timeout">0判断一次load状态后返回</param>
        /// <param name="readyState"></param>
        /// <returns></returns>
        public bool IsLoaded(int timeout = 1000, DocumentReadyState readyState = DocumentReadyState.Complete)
        {
            if (timeout <= 0)
            {
                return false;
            }
            using (var cts = new CancellationTokenSource(timeout))
            {
                do
                {
                    if (IsLoaded(readyState))
                    {
                        return true;
                    }
                    Thread.Sleep(200);
                } while (!cts.IsCancellationRequested);

            }
            return false;
        }

        /// <summary>
        /// 是否加载完成，通过在页面中执行JS的方式，获取页面加载状态
        /// </summary>
        /// <param name="TabId"></param>
        /// <param name="timeout"></param>
        /// <param name="readyState"></param>
        /// <returns></returns>
        public bool IsLoadedByExecJs(int timeout = 500, DocumentReadyState readyState = DocumentReadyState.Complete)
        {
            try
            {
                const string code = "document.readyState";
                var status = this.WebDataProvider.Executejs(new ParamExecuteJavaScriptModel()
                {
                    TabId = this.TabId,
                    ElementId = "",
                    scriptCode = code,
                    RunParam = new Plugin.Core.Entities.RunParamModel() { StartTimeStamp = ParamExtensionModel.RunParam.StartTimeStamp, TotalTime = timeout }
                });
                if (string.IsNullOrEmpty(status))//超时后，返回null
                {
                    return false;
                }
                logger.LogDebug($"IsLoadedByExecJs网页当前状态{status}");
                Enum.TryParse(status, true, out DocumentReadyState curReadState);
                if ((curReadState & readyState) > 0)
                {
                    logger.LogInformation($"IsLoadedByExecJs网页当前状态{status}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"执行IsLoadedByExecJs异常，此处按照未加载完成（false）处理");
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"本次错错误日志只作为分析使用，可忽略");
            }
            return false;
        }
        public virtual bool WaitToReadyState(DocumentReadyState readyState, int timeout)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            do
            {
                var _t = timeout - stopWatch.ElapsedMilliseconds;
                if (_t <= 0) break;
                //两种共方式目前的架构只能串行,执行JS匹配的方式 ，可能会在页面未加载完成时，阻塞，所以超时时间设置为1s
                if (IsLoaded(readyState))
                {
                    stopWatch.Stop();
                    return true;
                }
                if (stopWatch.ElapsedMilliseconds >= 3000 && this.IsLoadedByExecJs(1000, readyState))
                {
                    stopWatch.Stop();
                    return true;
                }
                Thread.Sleep(200);
            } while (stopWatch.ElapsedMilliseconds < timeout);
            stopWatch.Stop();
            return false;
        }

        #endregion

        #region 页面滚动（body滚动）
        /// <summary>
        /// 检查window对象是否绑定滚动事件
        /// </summary>
        /// <returns></returns>
        public bool HasScrollEvent()
        {
            try
            {
                var objectId = this.WebDataProvider.SendCommand(this.TabId, "Runtime.evaluate", new JObject { { "expression", "window" } })["result"].Value<string>("objectId");
                var listeners = this.WebDataProvider.SendCommand(this.TabId, "DOMDebugger.getEventListeners", new JObject { { "objectId", objectId } })["listeners"];
                return listeners.Any(n => n.Value<string>("type") == "scroll");
            }
            catch
            {
                return true;
            }
        }

        public JToken GetBodyScroll(eScrollDirection direction)
        {
            return this.WebDataProvider.GetBodyScroll(new ParamScrollModel()
            {
                TabId = this.TabId,
                direction = direction
            });
        }
        public JToken GetBodyScroll(eScrollDirection direction, string selector)
        {
            string elementId = "";
            string validitySelector = "";
            if (!string.IsNullOrEmpty(selector))
            {
                string version;
                bool discernHidden;
                var common = ExtractViewData.ExtractCommonProps(selector);
                try
                {
                    var props = JsonConvert.DeserializeObject<CommonProps>(common);
                    version = props.Version.ToString();
                    discernHidden = props.DiscernHidden.As<bool>();
                    validitySelector = UiCommon.ExtractSelectors(selector);
                }
                catch (Exception)
                {
                    version = GlobleConfigurations.SelectorLatestVersion;
                    discernHidden = true;
                }
                elementId = this.GetElementIdFromSelector(validitySelector, "", true, version, "default", discernHidden);
            }
            return this.WebDataProvider.GetBodyScroll(new ParamScrollModel()
            {
                TabId = this.TabId,
                direction = direction,
                elementId = elementId
            });
        }
        public void ScrollTop(int top, int waitTime = 200, int timeout = 5000)
        {
            this.Scroll(eScrollDirection.Top, top, waitTime, timeout);
        }
        public void ScrollLeft(int left, int waitTime = 200, int timeout = 5000)
        {
            this.Scroll(eScrollDirection.Left, left, waitTime, timeout);
        }
        public void Scroll(int directionType, string selector, string targetSelector, string scrollType, int offSetX, int offSetY)
        {
            string elementId = "";
            string targetElementId = "";
            if (!string.IsNullOrEmpty(selector))
            {
                string version;
                string validitySelector = "";
                bool discernHidden = true;
                var common = ExtractViewData.ExtractCommonProps(selector);

                try
                {
                    var props = JsonConvert.DeserializeObject<CommonProps>(common);
                    version = props.Version.ToString();
                    discernHidden = props.DiscernHidden.As<bool>();
                    validitySelector = UiCommon.ExtractSelectors(selector);
                }
                catch (Exception)
                {
                    version = GlobleConfigurations.SelectorLatestVersion;
                }
                elementId = this.GetElementIdFromSelector(validitySelector, "", true, version, "default", discernHidden);
            }
            if (!string.IsNullOrEmpty(targetSelector))
            {
                string version;
                string validitySelector = "";
                bool discernHidden = true;
                var common = ExtractViewData.ExtractCommonProps(targetSelector);
                try
                {
                    var props = JsonConvert.DeserializeObject<CommonProps>(common);
                    version = props.Version.ToString();
                    discernHidden = props.DiscernHidden.As<bool>();
                    validitySelector = UiCommon.ExtractSelectors(targetSelector);
                }
                catch (Exception)
                {
                    version = GlobleConfigurations.SelectorLatestVersion;
                }
                targetElementId = this.GetElementIdFromSelector(validitySelector, "", true, version, "default", discernHidden);
                //滚动到指定元素位置,保持和现有滚动到元素效果一致
                if (!string.IsNullOrEmpty(targetElementId) && scrollType == "cuselement")
                {
                    this.WebDataProvider.ScrollIntoViewIfNeeded_V2(new ParamScrollModel
                    {
                        TabId = this.TabId,
                        ElementId = targetElementId
                    });
                    return;
                }
            }
            eScrollDirection eScrollDirection = directionType == 0 ? eScrollDirection.Left : eScrollDirection.Top;
            var model = new ParamScrollModel()
            {
                TabId = this.TabId,
                direction = eScrollDirection,
                elementId = elementId,
                targetElementId = targetElementId,
                scrollType = scrollType,
                offSetX = offSetX,
                offSetY = offSetY
            };
            this.WebDataProvider.Scroll_V2(model);
        }
        public void Scroll(eScrollDirection direction, int scrollVal, int waitTime = 200, int timeout = 5000)
        {
            int record = -1;
            using (var cs = new CancellationTokenSource(timeout))
            {
                do
                {
                    var model = new ParamScrollModel()
                    {
                        TabId = this.TabId,
                        direction = direction,
                        value = scrollVal,
                    };
                    this.WebDataProvider.Scroll(model);
                    Thread.Sleep(waitTime);
                    var scrolltopStr = this.GetBodyScroll(direction).ToString();
                    float.TryParse(scrolltopStr, out var scrolltopF);
                    int scrolltop = (int)scrolltopF;
                    if (record != scrolltop)
                        record = scrolltop;
                    else
                        return;
                    if (scrolltop >= scrollVal)
                    {
                        return;
                    }
                }
                while (!cs.IsCancellationRequested);
            }
        }
        /// <summary>
        /// 执行一次滚动, 目标为最大页面高度或限制最大值(取最小), 当滚动停止时返回, 当页面总高度不再大于前一次时停止.
        /// </summary>
        /// <param name="limit">高度最大值</param>
        /// <param name="timeout">滚动超时时间</param>
        /// <param name="preHeight">前一次目标滚动的位置</param>
        /// <returns>尝试滚动的位置(不保证滚动到该位置)</returns>
        public int ScrollToBottom(int limit, int timeout = 5000, int? preHeight = null)
        {
            if (preHeight != null && preHeight.Value >= limit)
            {
                return preHeight.Value;
            }
            var totalHeight = this.WebDataProvider.GetTotalHeight(new ParamModel() { TabId = this.TabId });
            // 目标位置小于等于上一次探索, 结束
            if (preHeight != null && totalHeight <= preHeight)
            {
                return preHeight.Value;
            }
            var scrollTo = Math.Min(totalHeight, limit);
            this.ScrollTopUnitStop(scrollTo, timeout: timeout);
            return scrollTo;
        }
        public void ScrollTopUnitStop(int scrollVal, int waitTime = 200, int timeout = 5000)
        {
            this.ScrollUnitStop(eScrollDirection.Top, scrollVal, waitTime, timeout);
        }
        public void ScrollUnitStop(eScrollDirection direction, int scrollVal, int waitTime = 200, int timeout = 5000)
        {
            int top = 0, left = 0;
            switch (direction)
            {
                case eScrollDirection.Left:
                    left = scrollVal;
                    break;
                case eScrollDirection.Top:
                default:
                    top = scrollVal;
                    break;
            }

            this.WebDataProvider.ScrollTo(new ParamScrollToModel() { TabId = this.TabId, top = top, left = left });
            int previousTop = this.GetBodyScroll(direction).As<int>();
            using (var cs = new CancellationTokenSource(timeout))
            {
                var ct = cs.Token;
                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        throw new RPAException(eErrorMessageType.Error_WaitTimeout);
                    }
                    Thread.Sleep(waitTime);
                    int currentTop = this.GetBodyScroll(direction).As<int>();
                    logger.LogTrace($"Scrolling to top: {top} current: {currentTop} previous: {previousTop}");
                    if (previousTop == currentTop)
                    {
                        break;
                    }
                    previousTop = currentTop;
                }
            }
        }
        /// <summary>
        /// 递归滚动到页面底部(页面滚动到页底激发页面高度变化的情况).
        /// </summary>
        /// <param name="limit">高度最大值</param>
        /// <param name="scrollTimeout">单次滚动超时时间</param>
        /// <param name="totalTimeout">总超时时间</param>
        /// <param name="preHeight">前一次目标滚动的位置</param>
        public void ScrollToBottomRecurve(int limit, int waitTime = 1500, int scrollTimeout = 5000, int totalTimeout = 30000, int? preHeight = null)
        {
            if (preHeight == null)
            {
                preHeight = this.ScrollToBottom(limit, timeout: scrollTimeout);
            }
            using (var cs = new CancellationTokenSource(totalTimeout))
            {
                var ct = cs.Token;
                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        throw new RPAException(eErrorMessageType.Error_WaitTimeout);
                    }
                    Thread.Sleep(waitTime);
                    var currentHeight = this.ScrollToBottom(limit, timeout: scrollTimeout, preHeight: preHeight);
                    if (currentHeight == preHeight)
                    {
                        break;
                    }
                    preHeight = currentHeight;
                }
            }

        }
        #endregion

        #region 下载
        public void AddCreatedEvent(ParamModel paramModel)
        {
            this.WebDataProvider.AddCreatedEvent(paramModel);
        }
        public void RemoveCreatedEvent(ParamModel paramModel)
        {
            this.WebDataProvider.RemoveCreatedEvent(paramModel);
        }
        /// <summary>
        /// 状态查询 不计算超时时间
        /// </summary>
        /// <returns></returns>
        public List<DownloadDataInfo> DownloadDataInfo()
        {
            var info = this.WebDataProvider.DownloadingV1(new ParamModel());
            logger.LogDebug($"DownloadDataInfo:{info}");
            var dataInfos = JsonConvert.DeserializeObject<List<DownloadDataInfo>>(info.ToString());
            return dataInfos;
        }
        /// <summary>
        /// 状态查询 不计算超时时间
        /// </summary>
        /// <param name="paramModel"></param>
        /// <returns></returns>
        public List<DownloadDataInfo> DownLoadSearchInfos(ParamDownloadModel paramModel)
        {
            paramModel.TabId = this.TabId;
            var info = this.WebDataProvider.DownLoadSearchV1(paramModel);
            if (info == null)
            {
                return null;
            }
            var dataInfos = JsonConvert.DeserializeObject<List<DownloadDataInfo>>(info.ToString());
            return dataInfos;
        }

        public JArray Downloading()
        {
            return JArray.Parse(this.WebDataProvider.Downloading(new ParamModel() { RunParam = ParamExtensionModel.RunParam }).ToString());
        }
        public JToken DownLoadSearch(ParamDownloadModel paramModel)
        {
            paramModel.RunParam = ParamExtensionModel.RunParam;
            paramModel.TabId = this.TabId;
            var ret = this.WebDataProvider.DownLoadSearch(paramModel);
            if (ret == null)
            {
                return JToken.FromObject(new JObject[0]);
            }
            return ret;
        }
        public string DownLoadUrl(ParamDownloadModel paramModel)
        {
            paramModel.RunParam = ParamExtensionModel.RunParam;
            return this.WebDataProvider.DownLoadUrl(paramModel).ToString();
        }

        #endregion

        #region tab激活逻辑
        public virtual void ActiveTab()
        {
            logger.LogDebug($"ActiveTab");
            if (this.IsActive && this.IsFocused)
            {
                logger.LogDebug($"this tab is active and focused");
                return;
            }
            if (this.IsActive)
            {
                //可能存在多窗口 性能高于SetForegroundIfInactive()
                logger.LogDebug($"this tab is active but no focus");
                if (this.Browser.ActiveTabCount > 1)
                {
                    List<BrowserWindow> browserWnds = this.Browser.FindAllSameTitleWindow(this.Title) ?? this.Browser.FindAllSameTitleWindow();
                    if (this.BrowserWindowInfo.SpecifiedWindow)
                    {
                        browserWnds = browserWnds.Where(wnd => wnd.Handle == this.BrowserWindowInfo.Handle).ToList();
                    }
                    foreach (var browserWnd in browserWnds)
                    {
                        browserWnd.SetForeground();
                        this.GetTabDetail(true);
                        logger.LogDebug($"激活浏览器窗口：{browserWnd.Handle},{browserWnd.ClassName},{browserWnd.ClassName},{browserWnd.Title}");
                        logger.LogDebug($"ActiveTab tab {this.TabId} focused:{this.IsFocused} ");
                        if (this.IsFocused)
                        {
                            break;
                        }
                    }
                    //指定窗口无需重置
                    if (!this.BrowserWindowInfo.SpecifiedWindow)
                        this.SetWebWindowInfo(null);//这里考虑应该可以放在上边的break内，需要考虑 focused的时效性
                    this.ActiveWindowAndTab();//避免 激活tab是同标题窗口，SetForeground不能准确命中
                }
                else
                {
                    this.SetForeground();
                }
                logger.LogDebug($"激活tab所在窗口 结束");
            }
            else
            {
                logger.LogDebug($"this tab is inactive");
                SetForegroundIfInactive();
                this.ActiveWindowAndTab();
                this.ClientWndHandle = IntPtr.Zero;
                logger.LogDebug($"ActiveTab ClientWndHandle重置");
            }
        }
        public virtual void ActiveWindowAndTab()
        {
            this.WebDataProvider.ActiveTab(new ParamModel()
            {
                TabId = this.TabId,
                RunParam = ParamExtensionModel.RunParam
            });
        }
        public virtual void SwitchTab()
        {
            this.WebDataProvider.SwitchTab(new ParamModel()
            {
                TabId = this.TabId,
                RunParam = ParamExtensionModel.RunParam
            });
        }
        public virtual void SetForegroundIfInactive()
        {
            logger.LogDebug($"Browser.ActiveTabCount:{this.Browser.ActiveTabCount}");

            var activetabIds = this.Browser.GetActiveTabIds();
            logger.LogDebug($"activetabIds：{activetabIds}");
            //仅有一个浏览器窗口
            if (activetabIds.Count == 1 && this.browserWindowInfo != null)
            {
                this.SetForeground();
                logger.LogDebug($"仅有一个浏览器窗口 激活tab所在窗口结束");
                return;
            }
            foreach (var tabId in activetabIds)
            {
                var tabPage = this.Browser.CreateBrowserTabPage(new ParamCreateBrowserTabPageModel
                {
                    TabId = tabId,
                    RunParam = ParamExtensionModel.RunParam
                });
                logger.LogDebugBlock(() =>
                {
                    logger.LogDebug($"浏览器窗口：{tabPage.Title}，{tabPage.Url}");
                });
                if (tabPage.WindowId == this.WindowId)
                {
                    logger.LogDebug($"激活tab所在窗口");
                    tabPage.ActiveTab();
                    //if (this.BrowserWindowInfo.SpecifiedWindow && tabPage.BrowserWindowInfo.Handle == this.BrowserWindowInfo.Handle)
                    //{

                    //}
                    this.BrowserWindowInfo = tabPage.BrowserWindowInfo;
                    this.BrowserWindowInfo.Title = string.Empty;
                    logger.LogDebug($"激活tab所在窗口 结束");
                    return;
                }
            }
        }
        #endregion

        #region 基于tab的基础操作

        public string GetCookies(ParamCookieModel paramModel)
        {
            paramModel.TabId = this.TabId;
            paramModel.RunParam = ParamExtensionModel.RunParam;
            return this.WebDataProvider.GetCookies(paramModel).ToString();
        }
        public string GetDocument()
        {
            return this.WebDataProvider.GetDocument(new ParamModel()
            {
                TabId = this.TabId,
                RunParam = ParamExtensionModel.RunParam
            }).ToString();
        }
        public virtual void GoBack()
        {
            this.WebDataProvider.GoBack(new ParamModel()
            {
                TabId = this.TabId,
                RunParam = ParamExtensionModel.RunParam
            });
        }
        public virtual void GoForward()
        {
            this.WebDataProvider.GoForward(new ParamModel()
            {
                TabId = this.TabId,
                RunParam = ParamExtensionModel.RunParam
            });
        }
        public void Navigate(ParamNavigateModel paramModel)
        {
            paramModel.TabId = this.TabId;
            paramModel.RunParam = ParamExtensionModel.RunParam;
            this.WebDataProvider.Navigate(paramModel);
        }
        public void Reload()
        {
            this.WebDataProvider.Reload(new ParamModel()
            {
                TabId = this.TabId,
                RunParam = ParamExtensionModel.RunParam
            });
        }
        public void RemoveCookie(ParamCookieModel paramModel)
        {
            paramModel.TabId = this.TabId;
            paramModel.RunParam = ParamExtensionModel.RunParam;
            this.WebDataProvider.RemoveCookie(paramModel);
        }
        public void SetCookie(ParamCookieModel paramModel)
        {
            paramModel.TabId = this.TabId;
            paramModel.RunParam = ParamExtensionModel.RunParam;
            this.WebDataProvider.SetCookie(paramModel);
        }
        public JToken StopLoad()
        {
            return this.WebDataProvider.StopLoad(new ParamModel()
            {
                TabId = this.TabId,
                RunParam = ParamExtensionModel.RunParam
            });
        }
        public virtual void SetForeground()
        {
            if (this.BrowserWindowInfo == null)
            {
                return;
            }
            this.BrowserWindowInfo.SetForeground();
        }
        public virtual void CloseRight(long tabId, bool isCloseAlert = false)
        {
            this.WebDataProvider.CloseRight(new ParamMultipleCloseModel()
            {
                TabId = this.TabId,
                IsCloseAlert = isCloseAlert,
                RunParam = ParamExtensionModel.RunParam
            });
        }
        public virtual void CloseOther(long tabId, bool isCloseAlert = false)
        {
            this.WebDataProvider.CloseOther(new ParamMultipleCloseModel()
            {
                TabId = this.TabId,
                IsCloseAlert = isCloseAlert,
                RunParam = ParamExtensionModel.RunParam
            });
        }
        public virtual void CloseList(List<int> tabIds, bool isCloseAlert = false)
        {
            this.WebDataProvider.CloseList(new ParamMultipleCloseModel()
            {
                TabIds = tabIds,
                IsCloseAlert = isCloseAlert,
                RunParam = ParamExtensionModel.RunParam
            });
        }
        public virtual void Close(bool isCloseAlert = false)
        {
            this.WebDataProvider.Close(new ParamModel()
            {
                TabId = this.TabId,
                IsCloseAlert = isCloseAlert,
                RunParam = ParamExtensionModel.RunParam
            });
        }
        #endregion

        #region tab title\url匹配
        /// <summary>
        /// 
        /// </summary>
        /// <param name="title">null 忽略特征，默认title匹配</param>
        /// <param name="titlePattern"></param>
        /// <param name="url">null 忽略特征，默认url匹配</param>
        /// <param name="urlPattern"></param>
        /// <param name="supportRegular"></param>
        public virtual bool IsMatch(string title, ICompareMode<string> cormpareTitle, string url, ICompareMode<string> cormpareUrl, bool supportRegular = false)
        {
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(url))
            {
                return true;
            }
            bool isMatch = true;
            if (cormpareTitle != null)
            {
                var t = this.Title;
                isMatch = cormpareTitle.Compare(t, title);
                if (!isMatch && supportRegular)
                {
                    isMatch = PatternMatcher.RegexMatch(t, title);
                }
            }
            if (isMatch && cormpareUrl != null)
            {
                var u = this.Url;
                isMatch = cormpareUrl.Compare(u, url);
                if (!isMatch)
                {
                    var extensionUrlStr = this.Browser.ExtensionsWhitelist.FirstOrDefault(urlStr => u.Contains(urlStr));
                    if (!string.IsNullOrEmpty(extensionUrlStr))
                    {
                        isMatch = cormpareUrl.Compare(u.Replace(extensionUrlStr, ""), url);
                    }
                }
                if (!isMatch && supportRegular)
                {
                    isMatch = PatternMatcher.RegexMatch(u, url);
                }
            }
            return isMatch;
        }
        #endregion
    }

    public enum eScrollDirection
    {
        Top,
        Left
    }
}



