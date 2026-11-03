using II.RPA.WinAutomation.CommonUtility;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using II.RPA.Plugin.Core;
using System.IO;
using II.RPA.Plugin.GUI.Commons;
using System.Text;
using System.Collections.Concurrent;
using System.Drawing.Imaging;
using FlaUI.Core;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using II.RPA.Plugin.GUI.Commons.Params;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;
using Newtonsoft.Json;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.MouseMotions;
using System.Threading.Tasks;
using II.RPA.Plugin.GUI.Commons.WindowMatch;

namespace II.RPA.Plugin.CV
{
    public class CVNode : Selector, IGuiCVNode650, IGuiNode_6_6_0
    {
        public delegate string BuliderSelectorHander(AutomationElement element);
        public event BuliderSelectorHander BuliderSelector;
        private static ILogger logger = Logger.OfContext();
        public delegate AutomationElement BuliderAutomationElementHander(string selector, WindowObject tragetWindow);
        public event BuliderAutomationElementHander BuliderAutomationElement;


        private static int processID;
        /// <summary>
        /// 元素所在进程的ID
        /// </summary>
        public int ProcessID
        {
            get
            {
                return processID;
            }

            set
            {
                processID = value;
            }
        }
        /// <summary>
        /// 算法端使用的图片id DLL端透传是适用
        /// </summary>
        public string ImgId { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        internal AutomationElement automationElement { get; set; }
        internal WindowObject TragetWindow { get; set; }
        internal ISelectorContext Context { get; set; }
        public CVNode(ISelectorContext context) : base(context.Selector)
        {
            Context = context;
            //LoadNode();
        }
        public bool CheckState { get; set; } = false;
        public void InitNode(bool isFromOcrTable)
        {
            if (!isFromOcrTable)
            {
                LoadNode();
            }
            SetForeground();
        }

        public CVNode(ISelectorContext context, bool isFromOcrTable) : base(context.Selector)
        {
            Context = context;
            //SetForeground();
        }

        protected override SelectorItem GenerateSelectorItem(JObject json, string version)
        {
            return CVSelectorItem.FromJObject(json);
        }

        public CVNode(AutomationElement element)
        {
            this.automationElement = element;
        }
        private CVNode CVParent;
        public CVNode(CVNode parent)
        {
            CVParent = parent;
            this.automationElement = parent.automationElement;
            this.Clear();
            parent.GetSelector();

            foreach (var item in parent)
            {
                this.Add(item);
            }
        }
        internal static List<CVItem> CVResults = new List<CVItem>();

        public Bitmap CacheImage { get; set; }
        private string ElementText { get; set; }
        /// <summary>
        /// 用户选择的元素的识别结构体 信息
        /// </summary>
        public CVItem ElementObj { get; set; }

        static WaitingWnd waitWnd;
        private ITreeWalker TreeWalker
        {
            get
            {
                return UiCommon.TreeWalker;
            }
        }
        public Bitmap ElementToBitmap(bool autoScale)
        {
            if (autoScale)
            {
                return ElementToBitmap();
            }
            else
            {
                return automationElement.Capture();
            }
        }
        private bool isUseDeaktopShot = false;
        public Bitmap ElementToBitmap()
        {
            try
            {
                //if (_CacheImg != null)
                //    return _CacheImg;
                //float ScaleX = NativeMethods.ScaleX;

                #region test
                //string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempimg");
                //if (!Directory.Exists(path))
                //{
                //    Directory.CreateDirectory(path);
                //}
                //var testImg = UiCommon.GetRectBitmapDesktop(this.BoundingRectangle);

                //testImg?.Save(Path.Combine(path, DateTime.Now.Ticks.ToString() + $"{this.WindowHandle}_aaa.png"));
                //testImg = automationElement.Capture();
                //testImg?.Save(Path.Combine(path, DateTime.Now.Ticks.ToString() + $"{this.WindowHandle}_bbb.png"));
                //using (CaptureProvider cstest = new CaptureProvider())
                //{
                //    if (!cstest.Capture(this.WindowHandle, out Bitmap btest))
                //    {
                //        return null;
                //    }
                //    testImg = btest;
                //}
                //testImg?.Save(Path.Combine(path, DateTime.Now.Ticks.ToString() + $"{this.WindowHandle}_ccc.png"));
                #endregion

                //IntPtr CurForegroundHWnd = UiCommon.GetForegroundWindow();
                //isUseDeaktopShot = false;
                ////当前窗口已经是置顶窗口，或 置顶窗口和当前操作元素窗口是同一个进程 侧 使用桌面截图
                //if (CurForegroundHWnd == this.WindowHandle)
                //{
                //    isUseDeaktopShot = true;
                //}
                //else
                //{
                //    UiCommon.GetWindowThreadProcessId(CurForegroundHWnd, out var CurForegroundPID);
                //    UiCommon.GetWindowThreadProcessId(this.WindowHandle, out var ElementAppPID);
                //    if (CurForegroundPID == ElementAppPID)
                //    {
                //        isUseDeaktopShot = true;
                //    }
                //}

                isUseDeaktopShot = true;
                if (isUseDeaktopShot)
                {
                    _CacheImg = UiCommon.GetRectBitmapDesktop(this.BoundingRectangle);
                    //_CacheImg = automationElement.Capture();
                }
                else
                {
                    using (CaptureProvider cs = new CaptureProvider())
                    {
                        if (!cs.Capture(this.WindowHandle, out Bitmap bitmap))
                        {
                            return null;
                        }
                        _CacheImg = bitmap;
                    }
                }
                //string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempimg");
                //if (!Directory.Exists(path))
                //{
                //    Directory.CreateDirectory(path);
                //}
                //_CacheImg?.Save(Path.Combine(path, DateTime.Now.Ticks.ToString() + $"{this.WindowHandle}_111.png"));
                return _CacheImg;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "截取CV元素所在的应用的图片异常");
                throw ex;
            }
        }
        public bool CVParse(Bitmap appImg)
        {
            return ShowWaitingWnd(() =>
            {
                CVParse(appImg, IntPtr.Zero, false);
            });
        }
        public bool CVParseAndWait(Bitmap appImg, IntPtr appHandle)
        {
            return ShowWaitingWnd(() =>
            {
                CVParse(appImg, appHandle, false);
            });
        }
        public bool CVParse(Bitmap appImg, IntPtr appHandle)
        {
            return CVParse(appImg, appHandle, false);
        }
        public bool CVParse(Bitmap appImg, IntPtr appHandle, bool isRefrush)
        {
            string result = InspectByCV(appImg, appHandle, isRefrush);
            ParseCVInfo(result, appHandle);
            return true;
        }
        /// <summary>
        /// 弹出等待窗口 ，并开启一个线程执行  输入的方法 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private bool ShowWaitingWnd(Action action)
        {
            waitWnd = new WaitingWnd();
            bool bResult = false;
            Exception exception = null;
            Thread thread = new Thread(() =>
            {
                try
                {
                    action?.Invoke();
                    bResult = true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                    bResult = false;
                    exception = ex;
                }
                finally
                {
                    waitWnd.Dispatcher.Invoke(() =>
                    {
                        waitWnd.Close();
                    });
                }
            });
            thread.IsBackground = true;
            thread.Start();
            waitWnd.ShowDialog();
            if (exception != null)
            {
                throw exception;
            }
            return bResult;
        }
        private string InspectByCV(Bitmap bitmap, IntPtr appHandle, bool isRefrush)
        {
            try
            {
                CVResults.Clear();
                string imgStr = ImageHelper.Bitmap2Base64(bitmap);
                var processInfo = this.automationElement.GetProcessInfo();
                HttpItem item = new HttpItem();
                item.URL = GlobleConfigurations.CVServer;
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["image"] = imgStr;
                dic["wndHandle"] = appHandle;
                dic["app"] = processInfo.ProcessName ?? "";
                dic["refrush"] = isRefrush ? 1 : 0;
                JObject rootWndAttr = GetRootWndAttr(this.automationElement);
                rootWndAttr.Add(ConstFields.SELECTOR_ROOT_PROCESSNAME, processInfo.ProcessName ?? string.Empty);
                dic["attr"] = rootWndAttr.ToString(Formatting.None);

                logger.LogDebug($"GetRootWndAttr:{rootWndAttr}");
                if (isUseDeaktopShot)
                {
                    float sx = NativeMethods.DpiX / 96F;
                    float sy = NativeMethods.DpiY / 96F;
                    float ScaleX = NativeMethods.ScaleX;
                    dic["sx"] = (NativeMethods.DynamicScale * NativeMethods.ScaleX).ToString();
                }

                var result = GetHtml(item, dic);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.Html:OK");
                    logger.LogDebug($"result.Html:{(result.Html.Length <= 100 ? result.Html : result.Html.Substring(0, 100))}");
                    return result.Html;
                }
                else
                {
                    logger.LogError($"CV解析服务返回异常，请联系相关人员.{result?.Html}");
                    CVException cVException = new CVException(eErrorMessageType.Error_CVElementServiceError, eComponentType.Capture);
                    throw cVException;//向上层抛出异常
                }
            }
            catch (CVException cvEx)
            {
                throw cvEx;
            }
            catch (Exception ex)
            {
                CVException cVException = new CVException(eErrorMessageType.Error_CVElementServiceError, eComponentType.Capture, ex);
                throw cVException;//向上层抛出异常
            }
        }
        private JObject GetRootWndAttr(CVSelectorItem si)
        {
            var props = si.Properties.Where(x => x.Enabled == true && x.Value != null);
            string _name = "";
            string _className = "";
            string _processName = string.Empty;
            JObject retObj = new JObject();
            foreach (var p in props)
            {
                if (p.Name.EqualIgnoreCase(ConstFields.SELECTOR_NAME))
                {
                    _name = p.Value;
                    retObj.Add(ConstFields.SELECTOR_NAME, _name);
                }
                else if (p.Name.EqualIgnoreCase(ConstFields.SELECTOR_CLASS_NAME))
                {
                    _className = p.Value;
                    retObj.Add(ConstFields.SELECTOR_CLASS_NAME, _className);
                }
                else if (p.Name.EqualIgnoreCase(ConstFields.SELECTOR_ROOT_PROCESSNAME))
                {
                    _processName = p.Value;
                    retObj.Add(ConstFields.SELECTOR_ROOT_PROCESSNAME, _processName);
                }
            }
            logger.LogDebug($"GetRootWndAttr:{retObj}");
            return retObj;
        }

        private JObject GetRootWndAttr(AutomationElement automation)
        {
            JObject retObj = new JObject();
            try
            {

                var pathToRoot = new List<AutomationElement>();
                var element = automation;
                AutomationElement elementParent = element;
                while (element != null)
                {
                    try
                    {
                        //遍历父元素获得整个控件链
                        elementParent = GetCorrectParent(element);
                        if (elementParent != null)
                        {
                            pathToRoot.Add(element);
                        }
                    }
                    catch (Exception)
                    {
                        elementParent = null;//如果这段代码真的有异常抛出，不置为null则无法退出循环，如果期望抛出异常此处也未throw
                        logger.LogInformation("GetCorrectParent异常");
                    }
                    element = elementParent;
                }
                var root = pathToRoot.LastOrDefault();
                if (root == null)
                {
                    logger.LogDebug($"GetRootWndAttr:{retObj}");
                    return retObj;
                }
                //if (root.Properties.ControlType.TryGetValue(out var controlType))
                //{
                //    retObj.Add(ConstFields.SELECTOR_CONTROL_TYPE, controlType.ToString());
                //}
                if (root.Properties.Name.TryGetValue(out var name))
                {
                    retObj.Add(ConstFields.SELECTOR_NAME, name);
                }
                if (root.Properties.ClassName.TryGetValue(out var className))
                {
                    retObj.Add(ConstFields.SELECTOR_CLASS_NAME, className);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "获取根窗口属性异常，但并不影响使用");
            }
            logger.LogDebug($"GetRootWndAttr:{retObj}");
            return retObj;
        }
        private void ParseCVInfo(string result, IntPtr appHandle)
        {
            try
            {
                CVMessage cvm = JsonHelper.JsonDesByNewtonJson<CVMessage>(result);
                JArray items = null;
                if (cvm.status == "success")
                {
                    Configurations.Info.NeedHandleCorrection = false;
                    Rectangle remoteRefRect = Rectangle.Empty;
                    if (cvm.WndInfo != null)
                    {
                        Configurations.Info.NeedHandleCorrection = true;
                        remoteRefRect = RectHelper.FromAraeArr(cvm.WndInfo.Area.ToArray());
                    }

                    if (cvm.result is JArray)
                    {
                        items = cvm.result as JArray;
                    }
                    else //if (cvm.result is JObject)
                    {
                        items = (cvm.result as JObject)["elements"] as JArray;
                        this.ImgId = (cvm.result as JObject)["img_id"].ToString();
                        this.CurrentStatus = (cvm.result as JObject)["current_status"]?.ToString();
                    }

                    if (items != null)
                    {
                        foreach (var item in items.Children())
                        {
                            try
                            {

                                CVItem cv = JsonHelper.JsonDesByNewtonJson<CVItem>(item.ToString());
                                if (Configurations.Info.NeedHandleCorrection)
                                {
                                    cv.ZbrainArea = RectHelper.PointsToRectangle(cv.result_value.locations);
                                    cv.Area = RectHelper.RemoteRectCorrection(this.BoundingRectangle, remoteRefRect, cv.ZbrainArea);
                                }
                                CVResults.Add(cv);

                            }
                            catch (Exception)
                            {

                                throw;
                            }
                        }
                    }

                }
                if (items == null || items.Count < 1)
                {
                    //满足此条件客户端使用cv服务端返回的自定义异常消息
                    if (!string.IsNullOrEmpty(cvm.message))
                    {
                        logger.LogInformation($"CV识别失败：{cvm.message}");
                        var cvException = new CVException(eErrorMessageType.Error_CVElementServiceError, eComponentType.Capture);
                        cvException.CustomMessage = $"{cvm.message}";
                        throw cvException;
                    }
                }
                //if (appHandle == IntPtr.Zero)
                //{
                //    return;
                //}
                if (!GuiOperator.CVResultsDic.ContainsKey(appHandle))
                {
                    GuiOperator.CVResultsDic.TryAdd(appHandle, new List<CVItem>());
                }
                GuiOperator.CVResultsDic[appHandle].Clear();
                GuiOperator.CVResultsDic[appHandle].AddRange(CVResults);
                CVResults.Clear();
            }
            catch (CVException cvex)
            {
                throw cvex;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"CV元素解析服务返回的数据：{result}");
                CVException cVException = new CVException(eErrorMessageType.Error_CVElementServiceDataUseError, eComponentType.Capture, ex);
                throw cVException;//向上层抛出异常
            }
        }

        public Rectangle CacheRectangle;
        private Bitmap _CacheImg = null;

        public Bitmap CVSubElementToBitmap(string filePath, Bitmap bitmap)
        {
            try
            {
                Bitmap subBitmap;
                try
                {
                    string dirName = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(dirName))
                    {
                        Directory.CreateDirectory(dirName);
                    }
                    subBitmap = ImageHelper.Base642Bitmap(this.ElementObj.result_value.template_image.Split(',')[1]);
                    subBitmap.Save(filePath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                    return null;
                }
                return subBitmap;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "元素截图发生异常");
                return null;
            }
        }
        public void MouseConterClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            throw new NotImplementedException();
        }

        public void MouseConterDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            throw new NotImplementedException();
        }
        public string GetSubElements()
        {
            throw new NotImplementedException();
        }
        public string UserDefinedID => "";

        public string Idx => throw new NotImplementedException();

        public string AutomationId => throw new NotImplementedException();

        public string ClassName => throw new NotImplementedException();

        private string controlType = string.Empty;
        public string ControlType
        {
            get
            {
                if (!string.IsNullOrEmpty(controlType))
                {
                    return controlType;
                }
                if (ElementObj != null)
                    return ElementObj.algorithm;
                else
                    return "";
            }
        }

        public string Name => throw new NotImplementedException();

        public string Role => throw new NotImplementedException();

        public string Description => throw new NotImplementedException();

        public string ProcessName => throw new NotImplementedException();

        public string ProcessFullPath => throw new NotImplementedException();

        string cacheSelector;

        public string NodeSelector
        {
            get
            {
                return GetSelector(false);
            }
        }

        public string GetSelector(bool innerTextFirst)
        {
            logger.LogDebug("NodeSelector start");
            if (string.IsNullOrEmpty(cacheSelector))
            {
                if (Parent == null)
                {
                    cacheSelector = BuliderSelector?.Invoke(automationElement);
                    logger.LogDebug($"NodeSelector诊断: Parent==null, BuliderSelector返回长度={(cacheSelector == null ? -1 : cacheSelector.Length)}");
                }
                else
                {
                    cacheSelector = GetCVElementSelector();
                    logger.LogDebug($"NodeSelector诊断: GetCVElementSelector返回长度={(cacheSelector == null ? -1 : cacheSelector.Length)}, CVParent={(CVParent == null ? "null" : "not null")}");
                    var rawSelector = string.Empty;
                    if (CVParent != null && CVParent.automationElement.Properties.ProcessId.IsSupported)
                    {
                        var processId = CVParent.automationElement.Properties.ProcessId.Value;
                        var path = ExtractViewData.GetMainModuleFilepath(processId, out var processName);
                        rawSelector = BuliderSelector?.Invoke(automationElement);
                        logger.LogDebug($"NodeSelector诊断: BuliderSelector窗口原始selector长度={(rawSelector == null ? -1 : rawSelector.Length)}, processName={processName}, path={path}");
                        //UIA 构建窗口选择器失败时，尝试在 CVParent 上重新构建一次
                        if (string.IsNullOrEmpty(rawSelector) || rawSelector == "[]")
                        {
                            logger.LogWarning("BuliderSelector 返回空，尝试使用 CVParent 构建窗口选择器");
                            rawSelector = CVParent.BuliderSelector?.Invoke(CVParent.automationElement);
                        }
                        //UIA 获取窗口选择器均失败，从 CVParent 提取窗口属性构建 Window Selector 兜底
                        if (string.IsNullOrEmpty(rawSelector) || rawSelector == "[]")
                        {
                            logger.LogWarning("UIA 获取窗口选择器均失败，从 CVParent 提取窗口属性构建 Window Selector 兜底");
                            rawSelector = BuildWindowSelectorFromCVParent();
                        }
                        cacheSelector = ExtractViewData.AddCommonProps(rawSelector, eGuiType.CV, path, processName: processName);
                        logger.LogDebug($"NodeSelector诊断: AddCommonProps后长度={(cacheSelector == null ? -1 : cacheSelector.Length)}");
                    }
                }
            }
            logger.LogDebug("NodeSelector end");
            return cacheSelector;
        }
        public string GetCVElementSelector()
        {
            logger.LogDebug("GetCVElementSelector start");
            if (BoundingRectangle != Rectangle.Empty)
            {
                if (this.ElementObj.algorithm.Equals(ConstFields.ALGORITHM_OCR) && string.IsNullOrEmpty(this.ElementObj.result_value.text_contents))
                {
                    string eleImgBase64 = this.ElementObj.result_value.template_image;
                    string text = GetImgText(eleImgBase64);
                    if (!string.IsNullOrEmpty(text))
                    {
                        this.ElementObj.result_value.text_contents = text;
                        //this.ElementObj.result_value.template_image = string.Empty;
                    }
                }
                logger.LogDebug("ElementObj" + ElementObj.ToJson());
                //得到最终 操作前需要回传服务端解析做需要的 结构体信息 JSON
                string elementObjStr = JsonHelper.JsonByNewtonJson(this.ElementObj);
                logger.LogDebug("ElementObj");

                //6.9.7版本，去掉 anchor 信息单独存储机制
                //获取 用户选择的元素对应锚点(也是一个元素)的返回结构体信息
                //CVItem anchorObj = GuiOperator._CapturingWindowCVResults.Where(item => item.result_id == this.ElementObj.result_value.anchor).FirstOrDefault();
                //string anchorObjStr = anchorObj == null ? string.Empty : JsonHelper.JsonByNewtonJson(anchorObj);
                string anchorObjStr = string.Empty;

                elementObjStr = GetAnchorDistributeData(elementObjStr, GuiOperator._CapturingWindowCVResults.ToJson());

                CVSelectorItem itemWnd = new CVSelectorItem(automationElement, BoundingRectangle.X, BoundingRectangle.Y, BoundingRectangle.Width, BoundingRectangle.Height, elementObjStr: elementObjStr, anchorObjStr: anchorObjStr);
                itemWnd.Enabled = true;
                //this.Add(itemWnd);
                CVResultJson = itemWnd.json;

                SaveCVData(itemWnd);

                logger.LogDebug("GetCVElementSelector end");
                return this.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// UIA 在 CVParent 上也无法构建窗口选择器时，通过窗口句柄链提取窗口属性构建 Window Selector 兜底
        /// </summary>
        private string BuildWindowSelectorFromCVParent()
        {
            try
            {
                var hwnd = CVParent.automationElement.Properties.NativeWindowHandle.IsSupported
                    ? CVParent.automationElement.Properties.NativeWindowHandle.Value
                    : IntPtr.Zero;
                if (hwnd == IntPtr.Zero || !UiCommon.IsWindow(hwnd))
                {
                    return string.Empty;
                }
                //根窗口在前 目标窗口在尾
                var hwndList = UiCommon.GetAncestorWindowList(hwnd);
                if (hwndList.Count == 0)
                {
                    hwndList.Add(hwnd);
                }
                var items = new List<SelectorItem>();
                for (int i = 0; i < hwndList.Count; i++)
                {
                    var element = UiCommon.UIAAutomation.FromHandle(hwndList[i]);
                    var item = new CVSelectorItem(element);
                    var props = new List<SelectorItemProperty>();
                    if (i == 0)
                    {
                        //首节点附加进程名等根节点属性
                        var processId = element.Properties.ProcessId;
                        if (processId.IsSupported)
                        {
                            ExtractViewData.GetMainModuleFilepath(processId.Value, out var processName);
                            props.Add(new CVSelectorItemProperty(ConstFields.SELECTOR_ROOT_PROCESSNAME, processName ?? string.Empty) { Accurate = true });
                        }
                    }
                    if (i > 0)
                    {
                        //窗口链中相邻窗口的父子索引
                        var parentEle = UiCommon.UIAAutomation.FromHandle(hwndList[i - 1]);
                        int indexInParent = -1;
                        int idx = 0;
                        foreach (var child in parentEle.FindAllChildren())
                        {
                            if (child.Equals(element))
                            {
                                indexInParent = idx;
                                break;
                            }
                            idx++;
                        }
                        if (indexInParent > -1)
                        {
                            props.Add(new CVSelectorItemProperty(ConstFields.SELECTOR_INDEX_IN_PARENT, indexInParent.ToString()));
                        }
                    }
                    var wnd = new WindowObject(hwndList[i]);
                    if (!string.IsNullOrEmpty(wnd.Title))
                    {
                        props.Add(new CVSelectorItemProperty("title", wnd.Title) { Accurate = true });
                    }
                    if (!string.IsNullOrEmpty(wnd.ClassName))
                    {
                        props.Add(new CVSelectorItemProperty(ConstFields.SELECTOR_CLASS_NAME, wnd.ClassName) { Accurate = true });
                    }
                    var name = element.Properties.Name;
                    if (name.IsSupported && !string.IsNullOrEmpty(name.Value))
                    {
                        props.Add(new CVSelectorItemProperty(ConstFields.SELECTOR_NAME, name.Value) { Accurate = string.IsNullOrEmpty(wnd.Title) });
                    }
                    item.Properties = props;
                    foreach (var p in item.Properties)
                    {
                        p.Enabled = true;
                    }
                    items.Add(item);
                }
                var selector = new Selector();
                selector.AddRange(items);
                return selector.ToString();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "BuildWindowSelectorFromCVParent异常");
                return string.Empty;
            }
        }

        private void SaveCVData(CVSelectorItem itemWnd)        {
            JObject jObj = new JObject();
            jObj.Add(new JProperty("CVData", itemWnd.json));
            UiCommon.SaveStaticSelectorData(GlobleConfigurations.CaptureUniqueID, "intellectData.json", jObj.ToString());
        }
        private string GetAnchorDistributeData(string elementObjStr, string allElementsStr)
        {
            try
            {
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.ANCHOR_DISTRIBUTE;
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["pick_ele"] = elementObjStr;
                dic["elements"] = allElementsStr;
                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.Html:OK");
                    logger.LogDebug("result.Html:" + (result.Html.Length <= 100 ? result.Html : result.Html.Substring(0, 100)));
                    CVMessage cvmsg = result.Html.ToObject<CVMessage>();
                    return cvmsg.result.ToString();
                }
                else
                {
                    logger.LogError($"锚点分布服务返回异常，本次拾取的元素将没有锚点数据，可重新拾取，或联系相关人员的查看.{result?.Html}");
                    return elementObjStr;
                    //CVException cVException = new CVException(eErrorMessageType.Error_GetAnchorDistributeData_Error);
                    //throw cVException;//向上层抛出异常
                }
            }
            catch (Exception ex)
            {
                if (ex is CVException)
                    throw ex;
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"调用锚点分布服务返回异常");
                return "";
            }
        }
        public bool ReadCVData(string resDirPath)
        {
            string text = UiCommon.ReadStaticSelectorData(resDirPath, "intellectData.json");
            JObject jObj = JObject.Parse(text);
            jObj.TryGetValue("CVData", StringComparison.OrdinalIgnoreCase, out var val);
            if (val != null)
            {
                var item = this.GenerateSelectorItem((JObject)val, null);
                item.Enabled = true;
                Add(item);
                return true;
            }
            return false;

        }
        private string GetSelector()
        {
            AutomationElement root = automationElement.Parent;  //根元素
            this.parseParentSelector(automationElement);//递归父级元素
            return this.ToString();
        }
        #region 递归父级元素
        List<CVSelectorItem> cvlist = new List<CVSelectorItem>();
        public void parseParentSelector(AutomationElement automation)
        {
            var pathToRoot = new List<AutomationElement>();
            var element = automation;
            AutomationElement elementParent = element;
            while (element != null)
            {
                try
                {
                    //遍历父元素获得整个控件链
                    elementParent = GetCorrectParent(element);
                    if (elementParent != null)
                    {
                        pathToRoot.Add(element);
                    }
                }
                catch (Exception)
                {
                    elementParent = null;//如果这段代码真的有异常抛出，不置为null则无法退出循环，如果期望抛出异常此处也未throw
                    logger.LogInformation("GetCorrectParent异常");
                }
                element = elementParent;
            }
            if (pathToRoot.Count > 0)
            {
                pathToRoot.Reverse();
                pathToRoot.ForEach(item =>
                {
                    CVSelectorItem itemWnd = new CVSelectorItem(item);
                    itemWnd.Enabled = true;
                    this.Add(itemWnd);
                });
            }
            #region 此方法剔除顶层桌面元素不太好
            //if (automation != null)
            //{
            //    CVSelectorItem itemWnd = new CVSelectorItem(automation, false);
            //    itemWnd.Enabled = true;
            //    cvlist.Add(itemWnd);
            //    if (automation.Parent != null)
            //    {
            //        if (!automation.Parent.Name.Contains("桌面"))//此处桌面元素判断可能需要优化
            //            parseParentSelector(automation.Parent);
            //    }
            //}
            //if (cvlist.Count > 0)
            //{
            //    cvlist.Reverse();
            //    cvlist.ForEach(item =>
            //    {
            //        this.Add(item);
            //    });
            //    cvlist.Clear();
            //}
            #endregion
        }
        private AutomationElement GetCorrectParent(AutomationElement element)
        {
            return TreeWalker.GetParent(element);
        }
        private int GetElementIndexInParent(AutomationElement parent, AutomationElement element)
        {
            int indexInParent = -1;
            int skipCount = 0;
            try
            {
                var childs = parent.FindAllChildren();
                var count = childs.Count();
                for (int x = 0; x < count; x++)
                {
                    if (element.Equals(childs[x]))
                    {
                        indexInParent = x;
                        break;
                    }
                    else if (childs[x].Properties.ControlType.IsSupported && childs[x].ControlType == FlaUI.Core.Definitions.ControlType.ToolTip)
                    {
                        skipCount++;
                        continue;
                    }
                    else if (II.RPA.WinAutomation.CommonUtility.Configurations.Configs.FilterHiddenForIndex
                        && childs[x].Properties.IsOffscreen.TryGetValue(out var isOff) && isOff)
                    {
                        skipCount++;
                        continue;
                    }
                }
                indexInParent -= skipCount;
                //item.Properties.Add(new SelectorItemProperty(WindowsSelectorItem.INDEX, indexInParent.ToString()));
                return indexInParent;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        #endregion

        ///// <summary>
        ///// 根据selector 获取CVNode
        ///// </summary>
        ///// <param name="selector"></param>
        ///// <param name="cvExecutionServiceURL"></param>
        ///// <returns></returns>
        //public static CVNode GetElementFromSelector(string selector, string cvExecutionServiceURL)
        //{
        //    try
        //    {
        //        CVNode node = new CVNode(selector);
        //        //node.SetForeground();//构造中已经设置了前置窗体,这里不需要了
        //        GlobleConfigurations.CVExecutionServiceURL = cvExecutionServiceURL;
        //        if (node.Count >= 3)  //如果是拾取的元素
        //        {
        //            if (!node[2].Properties.Any(x => x.Name == "ElementObj"))
        //            {
        //                int locationX = int.Parse(node[2].Properties.Where(x => x.Name == "LocationX").First().Value.ToString());
        //                int locationY = int.Parse(node[2].Properties.Where(x => x.Name == "LocationY").First().Value.ToString());
        //                int width = int.Parse(node[2].Properties.Where(x => x.Name == "Width").First().Value.ToString());
        //                int height = int.Parse(node[2].Properties.Where(x => x.Name == "Height").First().Value.ToString());
        //                node.CacheRectangle = new Rectangle(locationX, locationY, width, height);
        //            }
        //            else
        //            {
        //                //获取CVNode中操作元素的对象中的 识别结构体数据 和  锚点元素识别结构体数据
        //                string elementObjStr = node[2].Properties.Where(x => x.Name == "ElementObj").First().Value.ToString();
        //                string anchor_obj = node[2].Properties.Where(x => x.Name == "AnchorObj").First().Value.ToString(); ;
        //                //CV元素操作前在服务端重新解析图片拾取操作员最新位置,如果拾取失败返回空集合
        //                List<int[]> locations = node.OperationMatchingByCVService(elementObjStr, anchor_obj);
        //                if (locations.Count() == 4)
        //                {
        //                    //获取 元素操作前在服务端重新解析 之后  得到的 返回信息
        //                    //OperationMatchingByCVService_Result matchingReulst = JsonHelper.JsonDesByNewtonJson<OperationMatchingByCVService_Result>(result);
        //                    int locationX = locations[0][0];
        //                    int locationY = locations[0][1];
        //                    int width = locations[1][0] - locations[0][0];
        //                    int height = locations[2][1] - locations[1][1];
        //                    //最终以BoundingRectangle属性体现
        //                    node.CacheRectangle = new Rectangle(locationX, locationY, width, height);
        //                }
        //                else
        //                {
        //                    CVException cVException = new CVException(eErrorMessageType.Error_CVNotMatchingElementError);
        //                    throw cVException;//向上层抛出异常
        //                }
        //            }
        //        }
        //        return node;
        //    }
        //    catch (CVException cvEx)
        //    {
        //        throw cvEx;
        //    }
        //    catch (Exception ex)
        //    {
        //        CVException cVException = new CVException(eErrorMessageType.Error_CVElementMatchingError, eComponentType.None, ex);
        //        throw cVException;//向上层抛出异常
        //    }
        //}

        /// <summary>
        /// 操作前的元素匹配 CV 服务器解析，返回匹配到元素的坐标四点信息
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns>[[x1, y1],[x2, y2],[x3, y4],[x4, y4]]，获取异常或 失败，长度为0</returns>

        internal List<int[]> OperationMatchingByCVService(string element_obj, string anchor_obj, bool isCheckboxCompontent)
        {
            try
            {
                List<int[]> locations = new List<int[]>();//要返回的坐标4点信息
                string result = string.Empty;//接收服务端返回值
                string imgStr = string.Empty; //base64 image
                                              //获取程序截图，并转为base64 供服务器解析时使用
                using (var bitmap = this.ElementToBitmap())
                {
                    imgStr = ImageHelper.Bitmap2Base64(bitmap);

                    //弹出等待窗口，并传入 去服务端调用 元素匹配拾取 服务
                    //ShowWaitingWnd(() =>
                    //{
                    //http 调用获取 元素匹配拾取服务提供 的匹配信息
                    if (isCheckboxCompontent)
                    {
                        result = OperationMatchingCheckboxByCVService_HttpRequest(imgStr, element_obj, anchor_obj);
                    }
                    else
                    {
                        result = OperationMatchingByCVService_HttpRequest(imgStr, element_obj, anchor_obj);
                    }
                    //});
                    try
                    {
                        CVMessage cvm = JsonHelper.JsonDesByNewtonJson<CVMessage>(result);
                        logger.LogInformation(cvm.status);
                        if (cvm.status == "success")
                        {
                            if (!isCheckboxCompontent)
                            {
                                locations = JsonHelper.JsonDesByNewtonJson<List<int[]>>(cvm.result);
                                ElementText = cvm.text;
                            }
                            else
                            {
                                JObject resultObj = JObject.FromObject(cvm.result);
                                resultObj.TryGetValue("is_checkbox", out var is_checkbox);
                                if (is_checkbox.ToString().EqualIgnoreCase("true"))
                                {
                                    this.controlType = "checkbox";
                                }
                                locations = JsonHelper.JsonDesByNewtonJson<List<int[]>>(resultObj["res"]);
                                CheckState = resultObj["gouxuan"].ToString().EqualIgnoreCase("true");
                            }

                            Configurations.Info.NeedHandleCorrection = cvm.WndInfo != null;
                            if (Configurations.Info.NeedHandleCorrection)
                            {
                                var remoteRefRect = RectHelper.FromAraeArr(cvm.WndInfo.Area.ToArray());
                                this.CacheRectangle = RectHelper.RemotePointsCorrection(this.BoundingRectangle, remoteRefRect, locations.ToObject<float[][]>());
                            }
                        }
                        else
                        {
#if Trace
                            string path = GlobleConfigurations.PackageDirectory + @"\.screenshots\";
                            if (!Directory.Exists(path))
                            {
                                Directory.CreateDirectory(path);
                            }
                            string errorImg = path + DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
                            bitmap.Save(errorImg);
                            logger.LogInformation(errorImg);
#endif
                            //cv查找元素失败时，基于服务端返回的异常message做消息返回
                            if (!string.IsNullOrEmpty(cvm.message))
                            {
                                var cvException = new CVException(eErrorMessageType.Error_CVNotMatchingElementCommonError);
                                cvException.CustomMessage = $"{cvException.ErrorCodeSuffixMessage},{cvm.message}";
                                throw cvException;
                            }
                            throw new CVException(eErrorMessageType.Error_CVNotMatchingElementError);
                        }
                    }
                    catch (CVException e)
                    {
                        throw e;
                    }
                    catch (Exception e)
                    {
                        throw new CVException(eErrorMessageType.Error_CVElementServiceDataUseError, e);
                    }
                }
                return locations;
            }
            catch (CVException cvEx)
            {
                throw cvEx;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw ex;
            }
        }

        /// <summary>
        /// 发起Http 请求，进行操作前的元素匹配拾取  CV 服务器解析，返回服务器提供的数据
        /// </summary>
        /// <param name="imgStr"></param>
        /// <param name="element_obj"></param>
        /// <param name="anchor_obj"></param>
        /// <returns></returns>
        private string OperationMatchingByCVService_HttpRequest(string imgStr, string element_obj, string anchor_obj)
        {
            try
            {
                HttpItem item = new HttpItem();
                item.URL = GlobleConfigurations.CVExecutionServiceURL;
                item.Method = "POST";
                //OperationMatchingParams pl = new OperationMatchingParams();
                //pl.image = imgStr;
                //pl.element_obj = JsonHelper.JsonDesByNewtonJson<CVItem>(element_obj);//服务端需要的是对象，非字符创
                //pl.anchor_obj = JsonHelper.JsonDesByNewtonJson<CVItem>(anchor_obj);//服务端需要的是对象，非字符创
                //string js = JsonHelper.JsonByNewtonJson(pl);

                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["app"] = this.Context.ProcessName;
                JObject rootWndAttr = GetRootWndAttr(this.OfType<CVSelectorItem>().First());
                dic["attr"] = rootWndAttr.ToString(Formatting.None);
                dic["image"] = imgStr;
                if (this.automationElement.Properties.NativeWindowHandle.TryGetValue(out var wndHandle))
                {
                    dic["wndHandle"] = (int)wndHandle;
                }
                dic["is_dynamic"] = this.Context.IsSmartControl.EqualIgnoreCase("true");//勾选智能修复，即走动态锚点逻辑

                if (string.IsNullOrEmpty(element_obj))
                {
                    dic["element_obj"] = "";
                }
                else
                {
                    dic["element_obj"] = JsonHelper.JsonDesByNewtonJson<CVItem>(element_obj);

                }
                if (string.IsNullOrEmpty(anchor_obj))
                {
                    dic["anchor_obj"] = "";
                }
                else
                {
                    dic["anchor_obj"] = JsonHelper.JsonDesByNewtonJson<CVItem>(anchor_obj);

                }
                if (isUseDeaktopShot)
                {
                    float sx = NativeMethods.DpiX / 96F;
                    float sy = NativeMethods.DpiY / 96F;
                    float ScaleX = NativeMethods.ScaleX;
                    dic["sx"] = (NativeMethods.DynamicScale * NativeMethods.ScaleX).ToString();
                }
                //string js = JsonHelper.JsonByNewtonJson(dic);
                //item.Postdata = js;
#if Trace
                //logger.LogDebug("OperationMatchingByCVService_HttpRequest:\n\r" + js);
#endif
                var result = GetHtml(item, dic);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.StatusCode:OK");
                    logger.LogDebug("result.Html:" + (result.Html.Length <= 100 ? result.Html : result.Html.Substring(0, 100)));
                    return result.Html;
                }
                else
                {
                    logger.LogError($"CV解析服务返回异常，请联系相关人员.{result?.Html}");
                    CVException cVException = new CVException(eErrorMessageType.Error_CVElementMatchingServiceError);
                    throw cVException;//向上层抛出异常
                }
            }
            catch (CVException cvEx)
            {
                throw cvEx;
            }
            catch (Exception ex)
            {
                CVException cVException = new CVException(eErrorMessageType.Error_CVElementMatchingServiceError, ex);
                throw cVException;//向上层抛出异常
            }
        }
        /// <summary>
        /// 发起Http 请求，进行操作前的元素匹配拾取  CV 服务器解析，返回服务器提供的数据
        /// </summary>
        /// <param name="imgStr"></param>
        /// <param name="element_obj"></param>
        /// <param name="anchor_obj"></param>
        /// <returns></returns>
        private string OperationMatchingCheckboxByCVService_HttpRequest(string imgStr, string element_obj, string anchor_obj)
        {
            try
            {
                HttpItem item = new HttpItem();
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.CV_CHECKBOX_EXEC;
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["image"] = imgStr;
                if (this.automationElement.Properties.NativeWindowHandle.TryGetValue(out var wndHandle))
                {
                    dic["wndHandle"] = (int)wndHandle;
                }

                //dic["is_dynamic"] = this.Context.IsSmartControl;//勾选智能修复，即走动态锚点逻辑
                //dic["element_obj"] = imgStr;
                if (string.IsNullOrEmpty(element_obj))
                {
                    dic["element_obj"] = "";
                }
                else
                {
                    dic["element_obj"] = JsonHelper.JsonDesByNewtonJson<CVItem>(element_obj);

                }
                if (string.IsNullOrEmpty(anchor_obj))
                {
                    dic["anchor_obj"] = "";
                }
                else
                {
                    dic["anchor_obj"] = JsonHelper.JsonDesByNewtonJson<CVItem>(anchor_obj);

                }
                //if (isUseDeaktopShot)
                //{
                //    float sx = NativeMethods.DpiX / 96F;
                //    float sy = NativeMethods.DpiY / 96F;
                //    float ScaleX = NativeMethods.ScaleX;
                //    dic["sx"] = (NativeMethods.DynamicScale * NativeMethods.ScaleX).ToString();
                //}
                //string js = JsonHelper.JsonByNewtonJson(dic);
                //item.Postdata = js;
#if Trace
                //logger.LogDebug("OperationMatchingByCVService_HttpRequest:\n\r" + js);
#endif
                var result = GetHtml(item, dic);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.StatusCode:OK");
                    logger.LogDebug("result.Html:" + (result.Html.Length <= 100 ? result.Html : result.Html.Substring(0, 100)));
                    return result.Html;
                }
                else
                {
                    logger.LogError($"CV解析服务返回异常，请联系相关人员.{result?.Html}");
                    CVException cVException = new CVException(eErrorMessageType.Error_CVElementMatchingServiceError);
                    throw cVException;//向上层抛出异常
                }
            }
            catch (CVException cvEx)
            {
                throw cvEx;
            }
            catch (Exception ex)
            {
                CVException cVException = new CVException(eErrorMessageType.Error_CVElementMatchingServiceError, ex);
                throw cVException;//向上层抛出异常
            }
        }
        private IntPtr _WindowHandle = IntPtr.Zero;
        public IntPtr WindowHandle
        {
            get
            {
                try
                {

                    if (_WindowHandle != IntPtr.Zero)
                    {
                        return _WindowHandle;
                    }
                    if (this.automationElement.Properties.NativeWindowHandle.IsSupported)
                    {
                        _WindowHandle = this.automationElement.Properties.NativeWindowHandle;
                        return _WindowHandle;
                    }
                    else
                    {
                        return default(IntPtr);
                    }

                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "获取NativeWindowHandle发生了一次异常");
                    return default(IntPtr);
                }
            }
        }

        public IGuiNode Parent
        {
            get { return CVParent; }
        }

        public IGuiNode AutomationElementParent => throw new NotImplementedException();

        public Rectangle BoundingRectangle
        {
            get
            {
                if (CacheRectangle != Rectangle.Empty)
                {
                    return CacheRectangle;
                }
                else
                {
                    if (this.automationElement == null)
                    {
                        return Rectangle.Empty;
                    }
                    if (!this.automationElement.Properties.BoundingRectangle.IsSupported)
                    {
                        return Rectangle.Empty;
                    }
                    return this.automationElement.BoundingRectangle;
                }
            }
        }
        public List<IGuiNode> Children => throw new NotImplementedException();

        public bool IsTopLevelWindow => throw new NotImplementedException();

        public JObject CVResultJson
        {
            get;
            set;
        }
        public bool IsCVText
        {
            get;
            set;
        }

        public bool IsShow => throw new NotImplementedException();

        public eGuiType GuiType => eGuiType.CV;

        public bool IsTable => throw new NotImplementedException();

        public string TableNodeSelector => throw new NotImplementedException();

        public List<IGuiNode> FindAll(IGuiNode fromElement = null, int maxResult = 1, int milliseconds = 3000)
        {
            throw new NotImplementedException();
        }

        public IGuiNode FindRelativeNode(int position, int offsetX, int offsetY)
        {
            throw new NotImplementedException();
        }

        public void Focus()
        {
            throw new NotImplementedException();
        }

        public IGuiNode GetChildByIdx(int idx)
        {
            throw new NotImplementedException();
        }

        public Point GetClickablePoint()
        {
            var point = new Point();
            var rect = GetRegion(3);
            point.X = rect.X + rect.Width / 2;
            point.Y = rect.Y + rect.Height / 2;
            return point;
        }

        public string GetElementCheck()
        {
            if (!this.ControlType.EqualIgnoreCase("checkbox"))
            {
                throw new CVException(eErrorMessageType.Error_ElementNotOperation);
            }
            return CheckState.ToString().ToLower();
        }

        public string GetElementSelect(int selectType = 1)
        {
            throw new NotImplementedException();
        }

        public string GetElementSelectEx(string strJson)
        {
            throw new NotImplementedException();
        }
        public Rectangle GetRegion(int positionType)
        {
            if (automationElement == null)
            {
                return this.BoundingRectangle;
            }
            int startX = automationElement.BoundingRectangle.X;
            int startY = automationElement.BoundingRectangle.Y;

            int realX = startX + this.BoundingRectangle.X;
            int realY = startY + this.BoundingRectangle.Y;
            return new Rectangle(realX, realY, this.BoundingRectangle.Width, this.BoundingRectangle.Height);
        }
        public string GetText()
        {
            return this.ElementText;
            //try
            //{
            //    CVItem elementObj = this[2].Properties.Where(x => x.Name == "ElementObj").First().Value.ToObject<CVItem>();
            //    if (elementObj == null || elementObj.result_value == null || string.IsNullOrEmpty(elementObj.algorithm))
            //    {
            //        return string.Empty;
            //    }
            //    if (!elementObj.algorithm.Equals("ocr_recognition"))
            //    {
            //        return string.Empty;
            //    }
            //    return elementObj.result_value.text_contents;
            //}
            //catch (Exception ex)
            //{
            //    new CVException(eErrorMessageType.Error_GetTextError, ex);
            //    return string.Empty;
            //}
        }
        public string GetText(string cvServer)
        {
            try
            {
                string imgStr = string.Empty;
                using (Bitmap bitmap = automationElement.Capture())
                {
                    using (var img = ImageHelper.CaptureImage(bitmap, this.BoundingRectangle))
                    {
                        imgStr = ImageHelper.Bitmap2Base64(img);
                    }
                }
                ;
                if (string.IsNullOrEmpty(imgStr))
                {
                    return string.Empty;
                }
                HttpItem item = new HttpItem();
                item.URL = cvServer;
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                //PayloadGetText pl = new PayloadGetText();
                dic["image"] = imgStr;
                //pl.image = imgStr;
                List<List<int>> points = new List<List<int>>();
                List<int> leftTop = new List<int>();
                leftTop.Add(this.BoundingRectangle.X);
                leftTop.Add(this.BoundingRectangle.Y);

                List<int> rightTop = new List<int>();
                rightTop.Add(this.BoundingRectangle.X + this.BoundingRectangle.Width);
                rightTop.Add(this.BoundingRectangle.Y);

                List<int> rightBottom = new List<int>();
                rightBottom.Add(this.BoundingRectangle.X + this.BoundingRectangle.Width);
                rightBottom.Add(this.BoundingRectangle.Y + this.BoundingRectangle.Height);

                List<int> leftBottom = new List<int>();
                leftBottom.Add(this.BoundingRectangle.X);
                leftBottom.Add(this.BoundingRectangle.Y + this.BoundingRectangle.Height);

                points.Add(leftTop);
                points.Add(rightTop);
                points.Add(rightBottom);
                points.Add(leftBottom);
                dic["points"] = points;
                //string js = JsonHelper.JsonByNewtonJson(dic);
                //item.Postdata = js;
                var result = GetHtml(item, dic);
                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug($"result.StatusCode:{result.StatusCode.ToString()}");
                    logger.LogDebug("result.Html:" + (result.Html.Length <= 100 ? result.Html : result.Html.Substring(0, 100)));
                    throw new CVException(eErrorMessageType.Error_CVElementServiceError);
                }
                else
                {
                    logger.LogDebug("result.StatusCode:OK");
                    logger.LogDebug("result.Html:" + (result.Html.Length <= 100 ? result.Html : result.Html.Substring(0, 100)));
                    CVMessage cvm = JsonHelper.JsonDesByNewtonJson<CVMessage>(result.Html);
                    string rst = string.Empty;
                    if (cvm != null)
                    {
                        if (cvm.status == "success")
                        {
                            rst = cvm.result.ToString();
                        }
                    }
                    return rst;
                }
            }
            catch (Exception ex)
            {
                throw new CVException(eErrorMessageType.Error_GetTextError, ex);
                //return string.Empty;
            }
        }
        public void MouseMove(int cursorPos, int offsetX = 0, int offsetY = 0, bool isUsePerson = false)
        {
            //NLogger.Default.Trace($"鼠标移动到: offsetX:{offsetX} offsetY:{offsetY} cursorPos:{cursorPos}");
            var rect = GetRegion(3);
            //NLogger.Default.Trace($"当前Rect: offsetX:{offsetX} offsetY:{offsetY} cursorPos:{cursorPos}");
            System.Drawing.Point point = new System.Drawing.Point(0, 0);
            if (cursorPos == MouseParamDef.CUR_POS_CENTER)
            {
                point = GetClickablePoint();
                point = new Point(point.X + offsetX, point.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_LEFT_TOP)//左上
            {
                point = new System.Drawing.Point(rect.X + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_TOP)//右上
            {
                point = new System.Drawing.Point(rect.X + rect.Width + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_LEFT_BOTTOM)//左下
            {
                point = new System.Drawing.Point(rect.X + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_BOTTOM)//右下
            {
                point = new System.Drawing.Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RANDOM)//随机
            {
                point = rect.GetRandomPoint();
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_TOP)//上中
            {
                point = new Point(rect.X + rect.Width / 2 + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_LEFT)//左中
            {
                point = new Point(rect.X + offsetX, rect.Y + rect.Height / 2 + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_RIGHT)//右中
            {
                point = new Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height / 2 + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_BOTTOM)//下中
            {
                point = new Point(rect.X + rect.Width / 2 + offsetX, rect.Y + rect.Height + offsetY);
            }
            II.RPA.Plugin.GUI.Commons.MouseMotions.MouseMotionHelper.PersonMoveMouse(isUsePerson, point);

        }
        public void MouseClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            if (clickParams == null)
                clickParams = new UiElementClickParams();
            var point = GetClickablePoint();

            var finalX = point.X + offsetX;
            var finalY = point.Y + offsetY;

            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            Thread.Sleep(200);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Left));
        }
        public void MouseHover(UiElementHoverParams clickParams = null)
        {
            //int startX = automationElement.BoundingRectangle.X;
            //int startY = automationElement.BoundingRectangle.Y;
            //int offsetX = 0;
            //int offsetY = 0;
            //if (clickParams != null)
            //{
            //    offsetX = clickParams.offsetX;
            //    offsetY = clickParams.offsetY;
            //}
            //int realX = startX + this.BoundingRectangle.X + this.BoundingRectangle.Width / 2 + offsetX;
            //int realY = startY + this.BoundingRectangle.Y + this.BoundingRectangle.Height / 2 + offsetY;

            //Mouse.Position = new Point(realX, realY);
            if (clickParams == null)
                clickParams = new UiElementHoverParams();
            MouseMove(clickParams.cursorPos, clickParams.offsetX, clickParams.offsetY);
        }
        public void MouseDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            //int startX = automationElement.BoundingRectangle.X;
            //int startY = automationElement.BoundingRectangle.Y;

            //int realX = startX + this.BoundingRectangle.X + this.BoundingRectangle.Width / 2 + offsetX;
            //int realY = startY + this.BoundingRectangle.Y + this.BoundingRectangle.Height / 2 + offsetY;

            //Mouse.Position = new Point(realX, realY);
            if (clickParams == null)
                clickParams = new UiElementClickParams();
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.LeftDoubleClick());
        }

        public void MouseRightClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            //int startX = automationElement.BoundingRectangle.X;
            //int startY = automationElement.BoundingRectangle.Y;

            //int realX = startX + this.BoundingRectangle.X + this.BoundingRectangle.Width / 2 + offsetX;
            //int realY = startY + this.BoundingRectangle.Y + this.BoundingRectangle.Height / 2 + offsetY;

            //Mouse.Position = new Point(realX, realY);
            if (clickParams == null)
                clickParams = new UiElementClickParams();
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Right));
        }

        public void MouseRightDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            //int startX = automationElement.BoundingRectangle.X;
            //int startY = automationElement.BoundingRectangle.Y;

            //int realX = startX + this.BoundingRectangle.X + this.BoundingRectangle.Width / 2 + offsetX;
            //int realY = startY + this.BoundingRectangle.Y + this.BoundingRectangle.Height / 2 + offsetY;

            //Mouse.Position = new Point(realX, realY);
            if (clickParams == null)
                clickParams = new UiElementClickParams();
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.RightDoubleClick());
        }

        public void SetElementCheck(int check)
        {
            if (!this.ControlType.EqualIgnoreCase("checkbox"))
            {
                throw new CVException(eErrorMessageType.Error_ElementNotOperation);
            }
            var isChecked = check > 0 ? true : false;
            if (isChecked != this.CheckState)
            {
                MouseClick(0, 0);
            }
        }

        public void SetElementSelect(int selectType, string setValue)
        {
            throw new NotImplementedException();
        }


        private void LoadNode()
        {
            //var selectors = this.OfType<CVSelectorItem>().Where(x => x.Enabled == true && x.Selector == null).ToList(); //找出不是顶层元素
            if (!string.IsNullOrEmpty(Context.ResDirPath))
            {
                var isReadSuccess = ReadCVData(Context.ResDirPath);
                if (!isReadSuccess)
                {
                    CVException cVException = new CVException(eErrorMessageType.Error_CVNotMatchingElementError);
                    throw cVException;//向上层抛出异常
                }
            }
            //SetForeground();
        }
        List<CVSelectorItem> selectorList;
        public void SetForeground()
        {
            selectorList = new List<CVSelectorItem>();
            var selectors = this.OfType<CVSelectorItem>().Where(x => x.Enabled == true && x.Selector == null).ToList(); //找出不是顶层元素
            selectorList = selectors;
            //判断是否要置顶
            string flagVal = "";
            bool isNeedSetForeground = true;
            bool hasFlag = this[0].Properties.Any(x => x.Name.Equals("Flag", StringComparison.OrdinalIgnoreCase));
            if (hasFlag)
            {
                flagVal = this[0].Properties.First(x => x.Name.Equals("Flag", StringComparison.OrdinalIgnoreCase)).Value;
            }
            if (!string.IsNullOrEmpty(flagVal))
            {
                if (flagVal.Split(',').Contains("NoForeground"))
                {
                    isNeedSetForeground = false;
                }
                else if (flagVal.Split(',').Contains("Foreground"))
                {
                    isNeedSetForeground = true;
                }
            }
            if (isNeedSetForeground && selectorList.Count >= 2)
            {
                JArray array = JArray.Parse(Context.Selector);
                if (array.Count > 1)
                {
                    JObject last = (JObject)array[array.Count - 1];
                    if (last[ConstFields.SELECTOR_NAME].ToString() == ConstFields.SELECTOR_CV_ELEMENT)
                    {
                        array.RemoveAt(array.Count - 1);
                    }
                }
                automationElement = BuliderAutomationElement?.Invoke(array.ToString(), this.TragetWindow);
            }


            //int matchStartSelectorIndex = 0;
            //if (Configurations.Info.MatchRootNodeMode == MatchRootNodeModeType.WindowMatch)
            //{
            //    var lastWindowIndex = selectors.FindLastIndex(nodeSelector => nodeSelector.ElementName.EqualIgnoreCase(ConstFields.PROP_VALUE_CONTROL_TYPE_WINDOW));
            //    if (lastWindowIndex > 0)
            //    {
            //        var eles = this.FindSubChildrenFromDeskTop(selectors[lastWindowIndex], Context.ProcessName);
            //        if (eles?.Length == 1)//如果跳级找了唯一的，就使用，否则不使用
            //        {
            //            logger.LogDebug($"使用最后window 匹配到元素数：{eles?.Length}个");
            //            automationElement = eles?.Where(e => e.Properties.BoundingRectangle.IsSupported && e.Properties.BoundingRectangle.Value.Width > 0 &&
            //            e.IsOffscreen == false
            //            )?.FirstOrDefault();
            //        }
            //    }
            //    if (automationElement != null)
            //    {
            //        logger.LogDebug("使用最后window 匹配到元素");
            //        matchStartSelectorIndex = lastWindowIndex + 1;
            //        //logger.LogDebug($"automationElement:{automationElement.ToJson()}");
            //        //logger.LogDebug($"automationElement.Properties:{automationElement.Properties.ToJson()}");
            //    }
            //    else
            //    {
            //        logger.LogDebug("窗口匹配方式未直接匹配到最后一层窗口，将进行节点匹配");
            //    }
            //}
            /*
            for (int i = matchStartSelectorIndex; i < selectorList.Count - 1; i++)
            {
                AutomationElement topElement = null;
                if (i == 0)
                {
                    logger.LogDebug("开始根窗口匹配");
                    var rootElements = this.FindSubChildrenFromDeskTop(selectors[i], Context.ProcessName);
                    if (rootElements != null && rootElements.Length > 0)
                    {
                        topElement = rootElements[0];
                    }
                    if (topElement == null)
                    {
                        logger.LogDebug("新根窗口匹配未找到，将使用findfirst");
                        topElement = SetForegroundByParent(UiCommon.UIAAutomation.GetDesktop(), selectors[i], isNeedSetForeground);
                    }
                }
                else
                {
                    topElement = SetForegroundByParent(automationElement, selectors[i], isNeedSetForeground);
                }
                if (topElement != null)
                {
                    automationElement = topElement;
                }
                else
                {
                    break;
                }
            }
            if (automationElement != null && isNeedSetForeground)
            {
                if (automationElement.Properties.NativeWindowHandle.IsSupported)
                {
                    UiCommon.ForceShow(automationElement.Properties.NativeWindowHandle);
                }
            }

            */
            //if (WindowHandle != IntPtr.Zero)
            //{
            //    using (CaptureProvider cs = new CaptureProvider())
            //    {
            //        cs.SetToCenter(this.WindowHandle);
            //    }
            //}
        }

        /// <summary>
        /// 根据父元素及子节点属性信息查找子节点元素 add by mty
        /// </summary>
        /// <param name="parentroot"></param>
        /// <param name="si"></param>
        /// <returns></returns>
        internal AutomationElement SetForegroundByParent(AutomationElement parentroot, CVSelectorItem si, bool isNeedSetForeground)
        {
            AutomationElement topElement = null;
            bool isContainStar = false;
            bool isSAPWindow = false;
            var conditions = si.GenerateConditionsWithoutStar(out isContainStar, ref isSAPWindow);    //因为自带的通过Condition查找的方式不支持通配符，所以需要自己后面进行特殊处理
            AutomationElement root = parentroot;
            topElement = root.FindFirst(TreeScope.Children, conditions);

            if (isSAPWindow && topElement == null)
            {
                //SAP窗体在最小化时 窗体名称会改变（增加ER1(1)的后缀） 从而造成无法查找窗体并激活 故需使用现有SAP窗体获取方式
                topElement = FindRootElement();
            }

            if (topElement != null && isNeedSetForeground)
            {
                if (topElement.Properties.NativeWindowHandle.IsSupported)
                {
                    UiCommon.ForceShow(topElement.Properties.NativeWindowHandle);
                }
            }
            return topElement;
        }

        /// <summary>
        /// 获取SAP根元素
        /// </summary>
        /// <returns></returns>
        private AutomationElement FindRootElement()
        {
            AutomationElement element = null;
            try
            {
                UiCommon.UIAMode = UIAModeType.UIA2;
                var automation = UiCommon.UIAAutomation;
                var cond = new List<ConditionBase>();
                var titleName = this.First().Properties.Where(x => x.Name.ToLower() == "name" || x.Name.ToLower() == "title")?.First().Value;
                var className = this.First().Properties.Where(x => x.Name.ToLower() == "class" || x.Name.ToLower() == "classname")?.First().Value;
                cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ClassName, className));
                AndCondition ad = new AndCondition(cond);
                var ss = UiCommon.UIAAutomation.GetDesktop().FindAllChildren();
                element = ss.Where(s => s.Name.Contains(titleName) && s.ClassName.Contains(className)).FirstOrDefault();
            }
            catch (Exception)
            {
                throw;
            }
            return element;
        }

        public void SetText(string text, int isReplace, bool isSimulate)
        {
            int startX = automationElement.BoundingRectangle.X;
            int startY = automationElement.BoundingRectangle.Y;

            int realX = startX + this.BoundingRectangle.X + this.BoundingRectangle.Width / 2;
            int realY = startY + this.BoundingRectangle.Y + this.BoundingRectangle.Height / 2;

            Mouse.Position = new Point(realX, realY);
            Mouse.LeftClick();
            Thread.Sleep(10);
            var replace = isReplace == 0 ? true : false;
            if (replace)
            {
                AutoItHelper.ClearWords();
                Thread.Sleep(200);
            }
            else
            {
                AutoItHelper.MoveCursorToEnd();
            }

            string result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "GetState"), "{}");
            var info = JObject.Parse(result)["info"];
            var state = JObject.Parse(info.ToString());
            bool isChange = false;
            if (state.Value<string>("state").Equals("中文"))
            {
                JObject j = new JObject();
                j.Add("state", "en_us");
                result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "SetState"), j.ToString());
                isChange = true;
            }
            AutoItHelper.Send(text, 1, sendKeyDelay: 50);

            if (isChange)
            {
                JObject j = new JObject();
                j.Add("state", "zh_cn");
                result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "SetState"), j.ToString());
            }
        }

        public string GetEnableClick()
        {
            throw new NotImplementedException();
        }
        public void SetElementAttribute(string name, string value)
        {

        }
        public string GetElementAttribute(string name, string apiVersion)
        {
            return string.Empty;
        }
        public string FindElementWithPicPath(string strPicPath)
        {
            return "true";
        }
        public string GetHTML()
        {
            return "";
        }
        public string FindRelativeElementsInfo(string strRelativeSelector, ref JArray context)
        {
            return "";
        }
        public void SetElementMultipleSelect(int selectType, string setValue)
        {

        }

        private HttpResult GetHtml(HttpItem item, Dictionary<string, object> dicParams)
        {
            string saltStr = EncryptUtils.MD5Encrypt("indeedRpaCVOfflineSalt" + GlobleConfigurations.UserToken);
            DateTime DateNow = DateTime.Now;
            long t = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            dicParams.Add("t", t);
            if (!dicParams.ContainsKey("timeout"))
            {
                dicParams.Add("timeout", item.Timeout);
            }
            var dicParams_sort = (from k in dicParams
                                  orderby k.Key
                                  select k).ToDictionary(kv => kv.Key, kv => kv.Value ?? "");
            StringBuilder sb_salt = new StringBuilder();
            List<KeyValuePair<string, object>> sgin_params = new List<KeyValuePair<string, object>>();
            string signSourceStr = "{0}&{1}&{2}";
            Uri uri = new Uri(item.URL);
            //item.Postdata = dicParams_sort.ToJson();
            foreach (var p in dicParams_sort)
            {
                //try
                //{
                sb_salt.Append(saltStr);


                //if (p.Value != null)
                //{
                if (p.Value.GetType().IsArray)
                {
                    continue;
                }
                if (!(p.Value is string) && p.Value.GetType().IsClass)
                {
                    continue;
                }
                //}
                sgin_params.Add(p);
                logger.LogDebug($"参与签名参数：【{p.Key.ToUpper()}】{(p.Value.ToString().Length > 20 ? p.Value.ToString().Substring(0, 20) : p.Value.ToString())}");
                //}
                //finally
                //{
                //    var pVal = new KeyValuePair<string, object>(p.Key, p.Value ?? "");
                //    sgin_params.Add(pVal);
                //    logger.LogDebug($"参与签名参数：【{pVal.Key.ToUpper()}】{(pVal.Value.ToString().Length > 20 ? pVal.Value.ToString().Substring(0, 20) : pVal.Value.ToString())}");
                //}
            }

            logger.LogDebug(sb_salt.ToString());
            string paramsStr = string.Join("&", sgin_params.Select(o => $"{o.Key.ToUpper()}={(o.Value ?? "")}"));
            signSourceStr = string.Format(signSourceStr, uri.AbsolutePath, sb_salt.ToString(), paramsStr);
            string sign = EncryptUtils.MD5Encrypt(signSourceStr);
            dicParams_sort.Remove("t");

            item.Postdata = dicParams_sort.ToJson();
            item.URL = $"{item.URL}{(item.URL.IndexOf("?") > 0 ? "&" : "?")}sign={sign}&t={t}";
            logger.LogDebug($"CV URL{item.URL}");
            return GetHtml(item);
        }
        private HttpResult GetHtml(HttpItem item)
        {
            item.Header.Add("CLIENT-USER", GlobleConfigurations.ClientUser);
            item.Header.Add("CLIENT-MAC", GlobleConfigurations.ClientMac);
            item.Header.Add("engineCode", GlobleConfigurations.EngineCode);
            logger.LogDebug($"engineCode：{GlobleConfigurations.EngineCode}");
            return new HttpHelper().GetHtml(item);
        }

        public string GetTableData(string input)
        {
            throw new NotImplementedException();
        }

        public string ClickTableCell(string input)
        {
            throw new NotImplementedException();
        }

        public void IntoView()
        {

        }
        public void StopCVServerCall()
        {
            try
            {
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                HttpItem item = new HttpItem();
                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.STOP_CV_ELEMENT_SERVICE;
                item.Method = "POST";
                var a = GetHtml(item);
            }
            catch (Exception ex)
            {
                //Trace即可 
                logger.LogDebug($"调用CV本地拾取服务异常：{ex}");
            }
        }

        private string GetImgText(string imgBase64Str)
        {
            try
            {
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);


                HttpItem item = new HttpItem();
                item.URL = uri.GetLeftPart(UriPartial.Authority) + CVRouteConstFields.NO_DETECT_OCR;
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["image"] = imgBase64Str;
                //item.Postdata = dic.ToJson();
                var result = GetHtml(item, dic);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.Html:OK");
                    logger.LogDebug("result.Html:" + (result.Html.Length <= 100 ? result.Html : result.Html.Substring(0, 100)));
                    CVMessage cvmsg = result.Html.ToObject<CVMessage>();
                    var resultDic = cvmsg.result.ToObject<Dictionary<string, object>>();
                    return resultDic["text"]?.ToString();
                }
                else
                {
                    logger.LogError($"CV解析服务返回异常，请联系相关人员.{result?.Html}");
                    return "";
                    //CVException cVException = new CVException(eErrorMessageType.Error_CVElementMatchingServiceError);
                    //throw cVException;//向上层抛出异常
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"调用CV本地拾取服务异常");
                return "";
            }
        }

        public void MouseThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            int startX = automationElement.BoundingRectangle.X;
            int startY = automationElement.BoundingRectangle.Y;

            int realX = startX + this.BoundingRectangle.X + this.BoundingRectangle.Width / 2 + offsetX;
            int realY = startY + this.BoundingRectangle.Y + this.BoundingRectangle.Height / 2 + offsetY;

            Mouse.Position = new Point(realX, realY);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
            {
                Mouse.LeftDoubleClick();
                Mouse.LeftClick();
            });
        }

        public void MouseRightThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            int startX = automationElement.BoundingRectangle.X;
            int startY = automationElement.BoundingRectangle.Y;

            int realX = startX + this.BoundingRectangle.X + this.BoundingRectangle.Width / 2 + offsetX;
            int realY = startY + this.BoundingRectangle.Y + this.BoundingRectangle.Height / 2 + offsetY;

            Mouse.Position = new Point(realX, realY);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
            {
                Mouse.RightDoubleClick();
                Mouse.RightClick();
            });
        }

        public void MouseConterThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            throw new NotImplementedException();
        }
        public JObject GetInputMethod()
        {
            string result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "GetState"), "{}");
            var info = JObject.Parse(result)["info"];
            var state = JObject.Parse(info.ToString());
            return state;
        }
        public void SetInputMethod(string model = "en_us")
        {
            JObject j = new JObject();
            j.Add("state", model);
            var result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "SetState"), j.ToString());
        }
        public bool SetText(ParamSetTextModel param)
        {

            UiElementClickParams uiElementClickParams = new UiElementClickParams();
            uiElementClickParams.operateType = 1;
            uiElementClickParams.cursorPos = param.ElementPosition;
            MouseClick(param.OffsetX, param.OffsetY, uiElementClickParams);
            Thread.Sleep(param.FocusTimeout);
            var replace = param.isReplace == 0 ? true : false;
            if (param.inputType == 0)
            {
                //throw new CVException(eErrorMessageType.Error_ElementNotOperation);
            }
            else
            {
                Task.Delay(1000).Wait();
                if (replace)
                {
                    AutoItHelper.ClearWords(param.Interval);
                }
                else
                {
                    AutoItHelper.MoveCursorToEnd(param.Interval);
                }
                Task.Delay(200).Wait();
                if (param.inputType == 1)
                {
                    if (param.Trigger == 1)
                    {
                        JObject j = GetInputMethod();
                        if (j.Value<string>("state").Equals("中文"))
                        {
                            SetInputMethod();
                        }
                    }
                    AutoItHelper.Send(param.text, false, param.Interval);
                }
                else
                {
                    UiCommon.ClipboardPaste(param.text, param.isReplace, "");
                }
            }

            //if (replace)
            //{
            //    //throw new CVException(eErrorMessageType.Error_ElementNotOperation);
            //}
            return true;
        }
        public void SetText(string text, int isReplace, int inputType, int interval = 0)
        {
            int startX = automationElement.BoundingRectangle.X;
            int startY = automationElement.BoundingRectangle.Y;

            int realX = startX + this.BoundingRectangle.X + this.BoundingRectangle.Width / 2;
            int realY = startY + this.BoundingRectangle.Y + this.BoundingRectangle.Height / 2;

            Mouse.Position = new Point(realX, realY);
            Mouse.LeftClick();
            Thread.Sleep(10);

            //CV 仅支持模拟输入与剪切板输入

            if (isReplace == 0)//清空 
            {
                AutoItHelper.ClearWords();
            }
            else
            {
                AutoItHelper.MoveCursorToEnd();
            }
            Thread.Sleep(200);

            string result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "GetState"), "{}");
            var info = JObject.Parse(result)["info"];
            var state = JObject.Parse(info.ToString());
            bool isChange = false;
            if (state.Value<string>("state").Equals("中文"))
            {
                JObject j = new JObject();
                j.Add("state", "en_us");
                result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "SetState"), j.ToString());
                isChange = true;
            }
            if (interval > 0)
            {
                AutoItHelper.Send(text, 1, sendKeyDelay: interval);
            }
            else
            {
                AutoItHelper.Send(text, 1, sendKeyDelay: 50);
            }


            if (isChange)
            {
                JObject j = new JObject();
                j.Add("state", "zh_cn");
                result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "SetState"), j.ToString());
            }

        }

        /// <summary>
        /// 暂时返回图片给engine，由engine处理
        /// </summary>
        /// <param name="cvServer"></param>
        /// <returns></returns>
        public string GetOriginImage(string cvServer)
        {
            try
            {
                string imgStr = string.Empty;
                using (Bitmap bitmap = automationElement.Capture())
                {
                    using (var img = ImageHelper.CaptureImage(bitmap, this.BoundingRectangle))
                    {
                        imgStr = ImageHelper.Bitmap2Base64(img);
                    }
                }
                ;
                if (string.IsNullOrEmpty(imgStr))
                {
                    return string.Empty;
                }
                return imgStr;
            }
            catch (Exception ex)
            {
                throw new CVException(eErrorMessageType.Error_GetCVPriceAmount_Error, ex);
                //return string.Empty;
            }
        }
        public List<CVItem> GetElementsAll(GetElementsAllParams @params)
        {
            string prevCVData = "[]";
            if (!string.IsNullOrEmpty(@params.PrevCVSelector))
            {
                prevCVData = @params.PrevCVSelector;
            }

            string result = "";
            if (@params.SourceType.EqualIgnoreCase("webCombineCv"))
            {
                result = InspectByCV_webCombineCv(@params.WindowHandle, @params.CVUseLastData, prevCVData, @params.SpecialOperation, @params.ViewElementsData, @params.TaskInfo, @params.IsFirstRecognition);
            }
            else
            {
                result = InspectByCV(@params.WindowHandle, @params.CVUseLastData, prevCVData, @params.SpecialOperation, @params.TaskInfo, @params.IsFirstRecognition);
            }
            ParseCVInfo(result, @params.WindowHandle);
            return GuiOperator.CVResultsDic[@params.WindowHandle];
        }

        public AgentCVInfo GetTargetElementInfo(GetTargetElementInfoModelParams @params)
        {
            try
            {
                var rect = RectHelper.GetWindowRect(@params.WindowHandle);
                HttpItem item = new HttpItem();
                item.Timeout = 190000;//gui_recognition超时时间设置190s
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                item.URL = uri.GetLeftPart(UriPartial.Authority) + (string.IsNullOrEmpty(@params.WebCode) ? CVRouteConstFields.GUI_RECOGNITION : CVRouteConstFields.DATA_COLLECT_API);
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["wndHandle"] = @params.WindowHandle;
                dic["img_area"] = new[] { rect.X, rect.Y, rect.Width, rect.Height };
                dic["client_info"] = new ClientInfoModel
                {
                    TarsAgent_ServiceUrl = GlobleConfigurations.TarsAgentServiceUrl,
                    Client_TenantCode = GlobleConfigurations.ClientTenantCode,
                    TarsAgent_ServiceToken = GlobleConfigurations.TarsAgentServiceToken
                };
                dic["task_info"] = @params.Step_Info;
                if (!string.IsNullOrEmpty(@params.WebCode))
                {
                    dic["xml_rpa"] = @params.WebCode;
                }
                var result = GetHtml(item, dic);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.Html:OK");
                    logger.LogDebug($"result.Html:{(result.Html.Length <= 100 ? result.Html : result.Html.Substring(0, 100))}");
                    var cvRetContent = result.Html;
                    //cvRetContent = "{\"code\": \"010300200000000\"," +
                    //    " \"message\": \"算法服务模块异常,请稍后重试，若未解决请联系相关人员解决\"," +
                    //    " \"result\": {\"action\":{\"action_type\":\"click\",\"elements\": [[399, 116]],\"content\":\"\",\"ele_ids\":[]},\"wnd_info\":{\"area\":[0, 0, 1080, 1920],\"handle\":514586}}, " +
                    //    "\"status\": \"success\"}";

                    CVMessage cvm = JsonHelper.JsonDesByNewtonJson<CVMessage>(cvRetContent);
                    List<string> idItems = null;
                    if (cvm.status == "success")
                    {
                        logger.LogDebug($"cvresult string:{cvm.result.ToString()}");
                        if (cvm.result != null && !string.IsNullOrEmpty(cvm.result.ToString()))
                        {
                            var cvresult = JsonConvert.DeserializeObject<CVResult>(cvm.result.ToString());
                            Configurations.Info.NeedHandleCorrection = false;
                            Rectangle remoteRefRect = Rectangle.Empty;
                            if (cvresult.WndInfo != null)
                            {
                                Configurations.Info.NeedHandleCorrection = true;
                                remoteRefRect = RectHelper.FromAraeArr(cvresult.WndInfo.Area.ToArray());
                            }
                            var actionType = cvresult.Action.action_type;
                            var content = cvresult.Action.content;
                            var actionDescription = cvresult.Action.action_description;
                            idItems = cvresult.Action.ele_ids;
                            return new AgentCVInfo
                            {
                                action_type = actionType,
                                content = content,
                                elementsPoint = cvresult.Action.elementsPoint,
                                absElementsPoint = cvresult.Action.elementsPoint,
                                ele_ids = idItems,
                                message = cvm.message,
                                action_description = actionDescription,
                                model_output = cvresult.Action.model_output
                            };
                        }
                        else
                        {
                            logger.LogWarning($"CV解析未返回有效数据，{result?.Html}");
                            return null;
                        }
                    }
                    ////满足此条件客户端使用cv服务端返回的自定义异常消息
                    //if (!string.IsNullOrEmpty(cvm.message))
                    //{
                    //    logger.LogInformation($"CV识别失败：{cvm.message}");
                    //    var cvException = new CVException(eErrorMessageType.Error_CVElementServiceError, eComponentType.Capture);
                    //    cvException.CustomMessage = $"{cvm.message}";
                    //    throw cvException;
                    //}
                    return null;
                }
                else
                {
                    logger.LogError($"CV解析服务返回异常，请联系相关人员.{result?.Html}");
                    CVException cVException = new CVException(eErrorMessageType.Error_CVElementServiceError, eComponentType.Capture);
                    throw cVException;//向上层抛出异常
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"处理zbrain返回数据异常：{ex}");
                return null;
            }
            //ParseCVInfo(result, @params.WindowHandle);
            //return GuiOperator.CVResultsDic[@params.WindowHandle];
        }
        /// <summary>
        /// CV元素拾取，不同于普通CV元素拾取，chatrpa获取界面虽有元素使用，含场景数据
        /// </summary>
        /// <param name="appHandle"></param>
        /// <returns></returns>
        private string InspectByCV(IntPtr appHandle, bool useLast, string prevCVData, string specialOperation, object taskInfo, bool isFirstRecognition = false)
        {
            try
            {
                CVResults.Clear();
                var processInfo = this.automationElement.GetProcessInfo();
                HttpItem item = new HttpItem();
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                item.URL = uri.GetLeftPart(UriPartial.Authority) + (isFirstRecognition ? CVRouteConstFields.CV_ELEMENT_RECOGNITION_FIRST : CVRouteConstFields.CV_ELEMENT_RECOGNITION);
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["wndHandle"] = appHandle;
                dic["use_last"] = useLast;
                dic["prevCVData"] = prevCVData;
                dic["special_operation"] = specialOperation;
                dic["app_name"] = processInfo.ProcessName ?? "";
                dic["task_info"] = taskInfo;
                dic["client_tenantCode"] = GlobleConfigurations.ClientTenantCode;
                dic["tarsAgent_serviceUrl"] = GlobleConfigurations.TarsAgentServiceUrl;
                dic["tarsAgent_serviceToken"] = GlobleConfigurations.TarsAgentServiceToken;
                var result = GetHtml(item, dic);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.Html:OK");
                    logger.LogDebug($"result.Html:{(result.Html.Length <= 100 ? result.Html : result.Html.Substring(0, 100))}");
                    return result.Html;
                }
                else
                {
                    logger.LogError($"CV解析服务返回异常，请联系相关人员.{result?.Html}");
                    CVException cVException = new CVException(eErrorMessageType.Error_CVElementServiceError, eComponentType.Capture);
                    throw cVException;//向上层抛出异常
                }
            }
            catch (CVException cvEx)
            {
                throw cvEx;
            }
            catch (Exception ex)
            {
                CVException cVException = new CVException(eErrorMessageType.Error_CVElementServiceError, eComponentType.Capture, ex);
                throw cVException;//向上层抛出异常
            }
        }
        /// <summary>
        /// CV元素拾取，不同于普通CV元素拾取，chatrpa获取界面虽有元素使用，含场景数据
        /// </summary>
        /// <param name="appHandle"></param>
        /// <returns></returns>
        private string InspectByCV_webCombineCv(IntPtr appHandle, bool useLast, string prevCVData, string specialOperation, string viewElesData, object taskInfo, bool isFirstRecognition = false)
        {
            try
            {
                CVResults.Clear();
                var processInfo = this.automationElement.GetProcessInfo();

                var rect = RectHelper.GetWindowRect(appHandle);
                HttpItem item = new HttpItem();
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                item.URL = uri.GetLeftPart(UriPartial.Authority) + (isFirstRecognition ? CVRouteConstFields.WEB_RECOGNITION_FIRST : CVRouteConstFields.WEB_RECOGNITION);
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["wndHandle"] = appHandle;
                dic["img_area"] = new[] { rect.X, rect.Y, rect.Width, rect.Height };
                dic["elements"] = viewElesData;

                dic["use_last"] = useLast;
                dic["prevCVData"] = prevCVData;
                dic["special_operation"] = specialOperation;
                dic["app_name"] = processInfo.ProcessName ?? "";
                dic["task_info"] = taskInfo;
                dic["client_tenantCode"] = GlobleConfigurations.ClientTenantCode;
                dic["tarsAgent_serviceUrl"] = GlobleConfigurations.TarsAgentServiceUrl;
                dic["tarsAgent_serviceToken"] = GlobleConfigurations.TarsAgentServiceToken;
                var result = GetHtml(item, dic);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.Html:OK");
                    logger.LogDebug($"result.Html:{(result.Html.Length <= 100 ? result.Html : result.Html.Substring(0, 100))}");
                    return result.Html;
                }
                else
                {
                    logger.LogError($"CV解析服务返回异常，请联系相关人员.{result?.Html}");
                    CVException cVException = new CVException(eErrorMessageType.Error_CVElementServiceError, eComponentType.Capture);
                    throw cVException;//向上层抛出异常
                }
            }
            catch (CVException cvEx)
            {
                throw cvEx;
            }
            catch (Exception ex)
            {
                CVException cVException = new CVException(eErrorMessageType.Error_CVElementServiceError, eComponentType.Capture, ex);
                throw cVException;//向上层抛出异常
            }
        }

        //public string GetSimilarOrDataCollectElements(IntPtr hWnd, object taskInfo, string viewElementData, string actionName)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(GlobleConfigurations.ZBrainServicePath))
        //        {
        //            logger.LogWarning("算法服务地址未同步");
        //            return "";
        //        }
        //        Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
        //        HttpItem item = new HttpItem();
        //        item.URL = uri.GetLeftPart(UriPartial.Authority) + actionName == "data_collect" ? CVRouteConstFields.DATA_COLLECT : CVRouteConstFields.CYCLE_ELES;
        //        item.Method = "POST";
        //        item.Timeout = 60 * 1000;
        //        Dictionary<string, object> dic = new Dictionary<string, object>();
        //        dic["wndHandle"] = hWnd;
        //        dic["tsak_info"] = taskInfo;
        //        dic["web_elements"] = viewElementData;
        //        var result = GetHtml(item, dic);
        //        if (result.StatusCode == System.Net.HttpStatusCode.OK)
        //        {
        //            logger.LogDebug("GetSimilarOrDataCollectElements result.Html:OK");
        //            JObject retObj = JObject.Parse(result.Html);

        //            if (retObj.ContainsKey("status"))
        //            {
        //                if (retObj["status"].ToString().Equals("success"))
        //                {
        //                    return retObj["result"].ToString();
        //                }
        //                else
        //                {
        //                    logger.LogInformation($"DataCollectInfo: {result?.Html}");
        //                    return "";
        //                }
        //            }
        //        }
        //        else
        //        {
        //            logger.LogError($"DataCollectInfo 服务返回异常，请联系相关人员.{result?.Html}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //Trace即可 
        //        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "DataCollectInfo 执行异常，请联系相关人员");
        //    }
        //    return "";
        //}
        public List<CVItem> GetSimilarOrDataCollectElements(GetElementsAllParams @params)
        {
            string actionName = @params.ActionName;
            string result;
            try
            {
                CVResults.Clear();
                var processInfo = this.automationElement.GetProcessInfo();

                var rect = RectHelper.GetWindowRect(@params.WindowHandle);
                HttpItem item = new HttpItem();
                Uri uri = new Uri(GlobleConfigurations.ZBrainServicePath);
                item.URL = uri.GetLeftPart(UriPartial.Authority) + (actionName == "data_collect" ? CVRouteConstFields.DATA_COLLECT : CVRouteConstFields.CYCLE_ELES);
                //item.URL = uri.GetLeftPart(UriPartial.Authority) + "/data_collect";
                logger.LogInformation($"挑选数据采集列元素url:{item.URL}");
                item.Method = "POST";
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic["wndHandle"] = @params.WindowHandle;
                dic["img_area"] = "[]";
                dic["web_elements"] = @params.ViewElementsData;
                dic["app_name"] = processInfo.ProcessName ?? "";
                dic["task_info"] = @params.TaskInfo;
                dic["client_tenantCode"] = GlobleConfigurations.ClientTenantCode;
                dic["tarsAgent_serviceUrl"] = GlobleConfigurations.TarsAgentServiceUrl;
                dic["tarsAgent_serviceToken"] = GlobleConfigurations.TarsAgentServiceToken;
                if (@params.SourceType.EqualIgnoreCase("webCombineCv"))
                {
                    dic["img_area"] = new[] { rect.X, rect.Y, rect.Width, rect.Height };
                }
                var retResult = GetHtml(item, dic);

                if (retResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.LogDebug("result.Html:OK");
                    logger.LogDebug($"result.Html:{(retResult.Html.Length <= 100 ? retResult.Html : retResult.Html.Substring(0, 100))}");
                    result = retResult.Html;
                }
                else
                {
                    logger.LogError($"CV解析服务返回异常，请联系相关人员.{retResult?.Html}");
                    CVException cVException = new CVException(eErrorMessageType.Error_CVElementServiceError, eComponentType.Capture);
                    throw cVException;//向上层抛出异常
                }
            }
            catch (CVException cvEx)
            {
                throw cvEx;
            }
            catch (Exception ex)
            {
                CVException cVException = new CVException(eErrorMessageType.Error_CVElementServiceError, eComponentType.Capture, ex);
                throw cVException;//向上层抛出异常
            }
            ParseCVInfo(result, @params.WindowHandle);
            return GuiOperator.CVResultsDic[@params.WindowHandle];
        }
        #region 查找根窗口



        /// <summary>
        /// 找到桌面下符合条件的子节点，即应用根节点
        /// </summary>
        /// <param name="root">父节点</param>
        /// <param name="conditions">指定条件</param>
        /// <returns>找到的子节点</returns>
        private AutomationElement[] FindSubChildrenFromDeskTop(CVSelectorItem si, string processName)
        {
            var props = si.Properties.Where(x => x.Enabled == true && x.Value != null);
            string _name = "";
            string _className = "";
            string _processName = string.Empty;
            foreach (var p in props)
            {
                if (p.Name.EqualIgnoreCase(ConstFields.SELECTOR_NAME))
                {
                    _name = p.Value;
                }
                else if (p.Name.EqualIgnoreCase(ConstFields.SELECTOR_CLASS_NAME))
                {
                    _className = p.Value;
                }
                else if (p.Name.EqualIgnoreCase(ConstFields.SELECTOR_ROOT_PROCESSNAME))
                {
                    processName = p.Value;
                }
            }
            logger.LogDebug("start 查找所有可能匹配的窗口句柄");
            //获取桌面下 所有符合className ， name的窗口句柄，从而获取符合条件的窗口
            var deskTopHWnd = NativeMethods.GetDesktopWindow();
            var childHwndList = FindDeskTopChildren(deskTopHWnd, _className, _name, processName);
            List<AutomationElement> foundChildren = new List<AutomationElement>();
            string pName = string.Empty;
            foreach (var hwnd in childHwndList)
            {
                try
                {
                    //pName = UiCommon.GetProcessName(hwnd);
                    //if (pName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                    //{
                    var wndEle = UiCommon.UIAAutomation.FromHandle(hwnd);
                    foundChildren.Add(wndEle);
                    //}
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"FromHandle 异常，会忽略调这个句柄，当前异常信息：{ex}");
                }
            }
            logger.LogDebug("end 查找所有可能匹配的窗口句柄");
            logger.LogDebug("start run Match function get elements");
            //二次过滤 主要是除className  name之外的属性
            var matchList = new List<AutomationElement>();
            foreach (var item in foundChildren)
            {
                if (item.Properties.ControlType.IsSupported)
                {
                    ///部分情况ElementName可能为空，为空是允许的
                    if (!string.IsNullOrEmpty(si.ElementName))
                    {
                        if (item.Properties.ControlType.Value.ToString().EqualIgnoreCase(si.ElementName))
                        {
                            matchList.Add(item);
                        }
                    }
                }
            }
            logger.LogDebug("end run Match function get elements");
            return matchList.ToArray();
        }

        public List<IntPtr> FindDeskTopChildren(IntPtr hWnd, string className, string name, string processName)
        {
            List<IntPtr> list = new List<IntPtr>();
            //获取根窗口信息
            IntPtr hDesktopWnd = NativeMethods.GetWindow(hWnd, 6u);
            if (hDesktopWnd != IntPtr.Zero)
            {
                bool isMatched = this.IsMatchByProperty(hDesktopWnd, className, name);
                if (isMatched)
                {
                    list.Add(hDesktopWnd);
                }
            }
            List<IntPtr> msatch_hWnds = new List<IntPtr>();
            //className name都存在
            if (!string.IsNullOrEmpty(className) && !string.IsNullOrEmpty(name) && (className + name).IndexOf("*") < 0)
            {
                msatch_hWnds = MatchWindowWithClassnameAngTitle(hWnd, className, name);
            }
            //未匹配到数据，且className等值匹配，单独使用className
            if (msatch_hWnds?.Count() == 0 && !string.IsNullOrEmpty(className) && className.IndexOf("*") < 0)
            {
                msatch_hWnds = MatchWindowWithClassnameAngTitle(hWnd, className, null);
            }
            //未匹配到数据,className为空，name不为空
            if (msatch_hWnds?.Count() == 0 && string.IsNullOrEmpty(className) && !string.IsNullOrEmpty(name) && name.IndexOf("*") < 0)
            {
                msatch_hWnds = MatchWindowWithClassnameAngTitle(hWnd, null, name);
            }
            if (msatch_hWnds?.Count() > 0)
            {
                list.AddRange(msatch_hWnds);
            }
            else
            {
                IntPtr nextHWnd = NativeMethods.GetWindow(hWnd, 5u);
                while (nextHWnd != IntPtr.Zero)
                {
                    bool isMatched = this.IsMatchByProperty(nextHWnd, className, name);
                    if (isMatched)
                    {
                        list.Add(nextHWnd);
                    }
                    nextHWnd = NativeMethods.GetWindow(nextHWnd, 2u);
                }
            }
            return list;
        }
        private List<IntPtr> MatchWindowWithClassnameAngTitle(IntPtr hWnd, string className, string wndTitle)
        {
            List<IntPtr> list = new List<IntPtr>();
            IntPtr hwndChildAfter = IntPtr.Zero;
            while (true)
            {
                IntPtr hNextWnd = NativeMethods.FindWindowEx(hWnd, hwndChildAfter, className, wndTitle);
                if (hNextWnd == IntPtr.Zero)
                {
                    break;
                }
                bool isMatched = this.IsMatchByProperty(hNextWnd, className, wndTitle);
                if (isMatched)
                {
                    list.Add(hNextWnd);
                }
                hwndChildAfter = hNextWnd;
            }
            return list;
        }
        private bool IsMatchByProperty(IntPtr hWnd, string className, string name)
        {
            //logger.LogDebug("start IsMatchByProperty");
            try
            {
                StringBuilder classNameSB = new StringBuilder(256);
                NativeMethods.GetClassName(hWnd, classNameSB, 256);
                //logger.LogDebug("classNameSB" + classNameSB.ToString());
                if (string.IsNullOrEmpty(className) || PatternMatcher.FitsMask(classNameSB.ToString(), className))
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        return true;
                    }
                    var wndText = UiCommon.GetWindowText(hWnd);
                    if (wndText == null || string.IsNullOrEmpty(wndText))
                    {
                        return !string.IsNullOrEmpty(className);//读取title为空，但className通过，先通过，后边使用UIAELEMENT 对Name过滤
                    }
                    //logger.LogDebug("classNameSB" + textSB.ToString());
                    if (PatternMatcher.FitsMask(wndText, name))
                    {
                        if (classNameSB.ToString() == "Chrome_WidgetWin_1" && wndText == "RPA Studio")
                        {
                            return false;
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "IsMatchByProperty 出现一个异常，忽略继续");
            }
            //logger.LogDebug("end IsMatchByProperty");
            return false;
        }

        public string GetSubElements(ParamGetSubElements param)
        {
            throw new NotImplementedException();
        }

        public string GetParent(ParamGetParent param)
        {
            throw new NotImplementedException();
        }

        public string GetSiblings(ParamGetSiblings param)
        {
            throw new NotImplementedException();
        }

        public List<SelectorProps> MatchNextButtonSelector(ParamDoElementEditorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public List<SelectorProps> AutoBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public List<SelectorProps> CompareBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public string GetNextElementBuildSelector(List<string> cssSelectorList)
        {
            throw new NotImplementedException();
        }

        public string GetHtml()
        {
            throw new NotImplementedException();
        }

        public string GetSimilarSelector(string origionSelector, bool innerTextFirst)
        {
            throw new NotImplementedException();
        }

        public IGuiNode[] QueryTargetSelectorAll(ParamQueryTargetSelectorAllModel param)
        {
            throw new NotImplementedException();
        }

        public string GetElementAttribute_V1(string name, bool isIntact, string apiVersion)
        {
            throw new NotImplementedException();
        }

        public List<string> GetSelectorAll(List<IGuiNode> guiNodes, string sourceSelector)
        {
            throw new NotImplementedException();
        }

        public List<Rectangle> GetRegionAll(List<IGuiNode> guiNodes)
        {
            throw new NotImplementedException();
        }

        public List<string> GetTextAll(List<IGuiNode> guiNodes)
        {
            throw new NotImplementedException();
        }

        public List<string> GetHtmlAll(List<IGuiNode> guiNodes)
        {
            throw new NotImplementedException();
        }

        public List<string> GetAttributeAll(List<IGuiNode> guiNodes, string attributeName, bool isIntact, string apiVersion)
        {
            throw new NotImplementedException();
        }

        public void SetElementSelectWithMatchModel(int selectType, int matchModel, string setValue)
        {
            throw new NotImplementedException();
        }

        public string GetCleanHtml()
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}

