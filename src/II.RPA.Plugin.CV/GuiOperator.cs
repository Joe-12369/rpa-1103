using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.Core.Commons;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Security.Policy;

namespace II.RPA.Plugin.CV
{
    public class GuiOperator : IGuiOperator_6_9_0, IGuiCVOperator650
    {
        private static ILogger logger = Logger.OfContext();
        internal static ConcurrentDictionary<IntPtr, List<CVItem>> CVResultsDic = new ConcurrentDictionary<IntPtr, List<CVItem>>();
        internal static List<CVItem> _CapturingWindowCVResults = null;
        private static IntPtr CapturingWindowHandle = IntPtr.Zero;
        public eGuiType GuiType => eGuiType.CV;

        public int Sort { get; set; }

        protected SelectorAdapter SelectorAdapter { get; set; } = new SelectorAdapter();

        public Rectangle CaptureByRegion(Rectangle location, Bitmap bitmap, string savepath)
        {
            CV.CVScreenWnd wnd = new CVScreenWnd(location, bitmap, savepath);
            if (wnd.ShowDialog() == DialogResult.OK)
            {
                Rectangle r = new Rectangle();
                Point start = wnd.startPoint;
                Point end = wnd.endPoint;

                r.X = start.X;
                r.Y = start.Y;

                r.Width = end.X - start.X;
                r.Height = end.Y - start.Y;

                return r;
            }
            else
            {
                return Rectangle.Empty;
            }
        }

        /// <summary>
        /// GetElementFromSelector 从原CVNode中分离出
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public IGuiNode GetElementFromSelector(GetElementFromSelectorParams args)
        {
            ISelectorContext context = this.SelectorAdapter.GetSelectorJson(args);
            string selector = context.Selector;

            logger.LogInformation($"CV 进入GetElementFromSelector");

            try
            {
                Thread.Sleep(20);
                CursorHelper.SetCursorBusy(true);
                CVNode node = new CVNode(context);
                node.TragetWindow = args.WindowInfo;
                node.BuliderAutomationElement += CreateGuiWindowsOperator().BuilderAutomationElement;

                if (!args.isFromOCRTable)
                {
                    //正常CV元素走这里
                    node.InitNode(false);
                }
                else
                {
                    //OCR表格解析元素识别走这里
                    node.InitNode(true);
                    return node;
                }
                var componentType = args.GetExtendParamValue<eComponentType>("ComponentType", eComponentType.None);
                var isCheckboxCompontent = (componentType == eComponentType.GetElementCheck || componentType == eComponentType.SetElementCheck);
                //if (isCheckboxCompontent)
                //{
                //    node.Context.IsSmartControl = "true";
                //}

                if (node.automationElement == null && node.Count > 1)//非屏幕截图类型
                {
                    CVException cVException = new CVException(eErrorMessageType.Error_CVAppNotFoundError);
                    throw cVException;//向上层抛出异常
                }
                //node.SetForeground();//构造中已经设置了前置窗体,这里不需要了
                if (!string.IsNullOrEmpty(args.CVExecutionServiceURL) && Uri.TryCreate(args.CVExecutionServiceURL, UriKind.Absolute, out _))
                {
                    logger.LogInformation($"update zbrain base url to:{args.CVExecutionServiceURL}");
                    GlobleConfigurations.CVExecutionServiceURL = args.CVExecutionServiceURL;
                }
                else
                {
                    GlobleConfigurations.CVExecutionServiceURL = $"{GlobleConfigurations.ZBrainServicePath}{CVRouteConstFields.CV_EXECUTION_SERVICE}";
                    logger.LogInformation($"zbrain url is empty.use default instead:{GlobleConfigurations.CVExecutionServiceURL}");
                } 
                int idx = node.Count - 1;
                if (node.Count == 1 && !node[idx].Properties.Any(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_ELEMENT_OBJ)))//屏幕截图类型
                {
                    int locationX = int.Parse(node[idx].Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.PROP_X)).First().Value.ToString());
                    int locationY = int.Parse(node[idx].Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.PROP_Y)).First().Value.ToString());
                    int width = int.Parse(node[idx].Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.PROP_WIDTH)).First().Value.ToString());
                    int height = int.Parse(node[idx].Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.PROP_HEIGHT)).First().Value.ToString());
                    node.CacheRectangle = new Rectangle(locationX, locationY, width, height);
                }
                else if (node.Count >= 2)  //如果是拾取的元素
                {
                    if (!node[idx].Properties.Any(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_ELEMENT_OBJ)))
                    {
                        int locationX = int.Parse(node[idx].Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.PROP_LOCATION_X)).First().Value.ToString());
                        int locationY = int.Parse(node[idx].Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.PROP_LOCATION_Y)).First().Value.ToString());
                        int width = int.Parse(node[idx].Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.PROP_LOCATION_WIDTH)).First().Value.ToString());
                        int height = int.Parse(node[idx].Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.PROP_LOCATION_HEIGHT)).First().Value.ToString());
                        node.CacheRectangle = new Rectangle(locationX, locationY, width, height);
                    }
                    else
                    {
                        //获取CVNode中操作元素的对象中的 识别结构体数据 和  锚点元素识别结构体数据
                        string elementObjStr = node[idx].Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_ELEMENT_OBJ)).First().Value.ToString();
                        string anchor_obj = node[idx].Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_ANCHOR_OBJ)).First().Value.ToString(); ;
                        //CV元素操作前在服务端重新解析图片拾取操作员最新位置,如果拾取失败返回空集合
                        List<int[]> locations = node.OperationMatchingByCVService(elementObjStr, anchor_obj, isCheckboxCompontent);
                        if (locations.Count() == 4)
                        {

                            if (!Configurations.Info.NeedHandleCorrection)
                            {

                                //获取 元素操作前在服务端重新解析 之后  得到的 返回信息
                                //OperationMatchingByCVService_Result matchingReulst = JsonHelper.JsonDesByNewtonJson<OperationMatchingByCVService_Result>(result);
                                int locationX = locations[0][0];
                                int locationY = locations[0][1];
                                int width = locations[1][0] - locations[0][0];
                                int height = locations[2][1] - locations[1][1];
                                //最终以BoundingRectangle属性体现
                                node.CacheRectangle = new Rectangle(locationX, locationY, width, height);
                                float ScaleX = UiCommon.ResolutionScalingX();
                                node.CacheRectangle = new Rectangle
                                {
                                    X = (int)(locationX * ScaleX),
                                    Y = (int)(locationY * ScaleX),
                                    Width = (int)(width * ScaleX),
                                    Height = (int)(height * ScaleX)
                                };
                            }
                        }
                        else
                        {
                            CVException cVException = new CVException(eErrorMessageType.Error_CVNotMatchingElementError);
                            throw cVException;//向上层抛出异常
                        }
                    }
                }
                return node;
            }
            catch (CVException cvEx)
            {
                throw cvEx;
            }
            catch (Exception ex)
            {
                CVException cVException = new CVException(eErrorMessageType.Error_CVElementMatchingError, eComponentType.None, ex);
                throw cVException;//向上层抛出异常
            }
            finally
            {
                CursorHelper.SetCursorBusy(false);
            }
        }



        public string GetElementFromSelectorAll(GetElementFromSelectorParams args)
        {
            return null;
        }


        /// <summary>
        /// 返回包含某个点的矩形框，从原CVNode中分离出
        /// </summary>
        public Rectangle GetPointRectangle(Point pointS, IGuiCVNode rootNode, Rectangle areaRect, out CVItem elementObj, out int areaCount)
        {
            elementObj = null;
            areaCount = 0;
            try
            {
                logger.LogDebug($"CV根窗口句柄：{rootNode.WindowHandle}");
                CapturingWindowHandle = rootNode.WindowHandle;
                if (!CVResultsDic.ContainsKey(rootNode.WindowHandle))
                {
                    CVResultsDic.TryAdd(rootNode.WindowHandle, new List<CVItem>());
                }
                _CapturingWindowCVResults = CVResultsDic[rootNode.WindowHandle];
                //NLogger.Default.Trace("3-1-1-2");
                if (CVResultsDic.Count == 0 || _CapturingWindowCVResults == null || _CapturingWindowCVResults.Count == 0)
                {
                    return new Rectangle();
                }
                //float ScaleX = NativeMethods.ScaleX;
                float ScaleX = UiCommon.ResolutionScalingX();

                Rectangle offsetRec = new Rectangle
                {
                    X = (int)(rootNode.BoundingRectangle.Location.X / ScaleX),
                    Y = (int)(rootNode.BoundingRectangle.Location.Y / ScaleX),
                    Width = (int)(rootNode.BoundingRectangle.Width / ScaleX),
                    Height = (int)(rootNode.BoundingRectangle.Height / ScaleX)
                };

                Point point = new Point
                {
                    X = (int)(pointS.X / ScaleX),
                    Y = (int)(pointS.Y / ScaleX),
                };
                //NLogger.Default.Trace("3-1-1-3");
                //这里涉及到一个坐标的换算，相对于被CV拾取的对象的坐标；
                var allMatches = _CapturingWindowCVResults.Where(x => (x.left_top[0] <= point.X - offsetRec.X
               && x.left_top[1] <= point.Y - offsetRec.Y
               && x.right_bottom[0] >= point.X - offsetRec.X
               && x.right_bottom[1] >= point.Y - offsetRec.Y));

                //获取指定区域内的总数量
                if (areaRect != Rectangle.Empty)
                {
                    logger.LogDebug($"areaRect:{areaRect}");
                    areaRect = new Rectangle
                    {
                        X = (int)(areaRect.X / ScaleX),
                        Y = (int)(areaRect.Y / ScaleX),
                        Width = (int)(areaRect.Width / ScaleX),
                        Height = (int)(areaRect.Height / ScaleX)
                    };
                    logger.LogDebug($"areaRect/ScaleX:{areaRect}");
                    areaCount = _CapturingWindowCVResults.Count(x =>
                    areaRect.Contains((int)x.left_top[0] + offsetRec.X, (int)x.left_top[1] + offsetRec.Y)
                    && areaRect.Contains((int)x.right_bottom[0] + offsetRec.X, (int)x.right_bottom[1] + offsetRec.Y));
                }

                //NLogger.Default.Trace("3-1-1-4");
                if (allMatches.Count() == 0)
                {
                    return new Rectangle();
                }
                //找出面积最小的
                //var minMathes = allMatches.OrderBy(x => ((Math.Max(x.right_top[0], x.right_bottom[0]) - Math.Min(x.left_top[0], x.left_bottom[0]) * (Math.Max(x.right_bottom[1], x.left_bottom[1]) - Math.Min(x.left_top[1], x.right_top[1])))));

                var minMathes = allMatches.OrderBy(x => (Math.Max(x.right_top[0], x.right_bottom[0]) - Math.Min(x.left_top[0], x.left_bottom[0])) * (Math.Max(x.right_bottom[1], x.left_bottom[1]) - Math.Min(x.left_top[1], x.right_top[1])));
                //NLogger.Default.Trace("3-1-1-5");
                var minItem = minMathes.First();
                float minx = Math.Min(minItem.left_top[0], minItem.left_bottom[0]);
                float maxx = Math.Max(minItem.right_top[0], minItem.right_bottom[0]);
                float miny = Math.Min(minItem.left_top[1], minItem.right_top[1]);
                float maxy = Math.Max(minItem.left_bottom[1], minItem.right_bottom[1]);
                //记录用户选择元素的 构体信息 便于后生成selector时使用
                elementObj = minItem;
                //NLogger.Default.Trace("3-1-1-6");
                var eleRect = new Rectangle((int)Math.Ceiling(minx), (int)Math.Ceiling(miny), (int)Math.Ceiling(maxx - minx), (int)Math.Ceiling(maxy - miny));
                ScaleX = UiCommon.ResolutionScalingX();
                return new Rectangle
                {
                    X = (int)(eleRect.Location.X * ScaleX),
                    Y = (int)(eleRect.Location.Y * ScaleX),
                    Width = (int)(eleRect.Width * ScaleX),
                    Height = (int)(eleRect.Height * ScaleX)
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return new Rectangle();
        }
        public Rectangle GetPointRectangle_HandleCorrection(Point pointS, IGuiCVNode rootNode, Rectangle areaRect, out CVItem elementObj, out int areaCount)
        {
            elementObj = null;
            areaCount = 0;
            try
            {
                logger.LogDebug($"CV根窗口句柄：{rootNode.WindowHandle}");
                CapturingWindowHandle = rootNode.WindowHandle;
                if (!CVResultsDic.ContainsKey(rootNode.WindowHandle))
                {
                    CVResultsDic.TryAdd(rootNode.WindowHandle, new List<CVItem>());
                }
                _CapturingWindowCVResults = CVResultsDic[rootNode.WindowHandle];
                if (CVResultsDic.Count == 0 || _CapturingWindowCVResults == null || _CapturingWindowCVResults.Count == 0)
                {
                    return new Rectangle();
                }
                Rectangle offsetRec = rootNode.BoundingRectangle;
                Point point = new Point
                {
                    X = pointS.X - offsetRec.X,
                    Y = pointS.Y - offsetRec.Y
                };
                //NLogger.Default.Trace("3-1-1-3");
                //这里涉及到一个坐标的换算，相对于被CV拾取的对象的坐标；
                var allMatches = _CapturingWindowCVResults.Where(x => x.Area.Contains(point));

                //获取指定区域内的总数量
                if (areaRect != Rectangle.Empty)
                {
                    logger.LogDebug($"areaRect:{areaRect}");
                    areaRect = new Rectangle
                    {
                        X = areaRect.X - offsetRec.X,
                        Y = areaRect.Y - offsetRec.Y,
                        Width = areaRect.Width,
                        Height = areaRect.Height
                    };
                    logger.LogDebug($"offset areaRect:{areaRect}");
                    areaCount = _CapturingWindowCVResults.Count(x =>
                    areaRect.Contains(x.Area));
                }
                if (allMatches.Count() == 0)
                {
                    return new Rectangle();
                }
                //找出面积最小的

                var minMathes = allMatches.OrderBy(x => x.Area.Width * x.Area.Height);

                var minItem = minMathes.First();

                elementObj = minItem;
                return minItem.Area;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return new Rectangle();
        }
        public bool InspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            throw new NotImplementedException();
        }
        internal static IGuiWindowsOperator CreateGuiWindowsOperator()
        {
            return GuiRuntime.WindowsOperator;
        }

        public IGuiNode InspectNode(AutomationElement element)
        {
            CVNode node = new CVNode(element);
            node.BuliderSelector += CreateGuiWindowsOperator().BuilderSelector;
            return node;
        }

        public bool InspectNode(Point screenPoint, IGuiCVNode rootNode, string selector, out IGuiNode inspectNode)
        {
            return InspectNode(screenPoint, rootNode, selector, null, out inspectNode);
        }
        /// <summary>
        /// 拾取已经解析之后的元素 从原CVNode中分离出
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="rootNode"></param>
        /// <param name="inspectNode"></param>
        /// <returns></returns>
        public bool InspectNode(Point screenPoint, IGuiCVNode rootNode, string selector, object extendParam, out IGuiNode inspectNode)
        {
            inspectNode = null;
            CVItem elementObj;
            int areaCount = 0;
            Rectangle areaRect = Rectangle.Empty;
            if (extendParam != null)
            {
                areaRect = (Rectangle)((Dictionary<string, object>)extendParam)["areaRect"];
            }
            Rectangle rec;
            if (Configurations.Info.NeedHandleCorrection)
            {
                rec = GetPointRectangle_HandleCorrection(screenPoint, rootNode, areaRect, out elementObj, out areaCount);

            }
            else
            {
                rec = GetPointRectangle(screenPoint, rootNode, areaRect, out elementObj, out areaCount);
            }
            if (rec == Rectangle.Empty)
            {
                return false;
            }
            ((Dictionary<string, object>)extendParam)["areaCount"] = areaCount;
            CVNode parentNode = (CVNode)rootNode;
            parentNode.BuliderSelector += CreateGuiWindowsOperator().BuilderSelector;
            inspectNode = new CVNode((CVNode)rootNode);
            ((CVNode)inspectNode).CacheRectangle = rec;
            ((CVNode)inspectNode).BuliderSelector += CreateGuiWindowsOperator().BuilderSelector;
            ((CVNode)inspectNode).ElementObj = elementObj;//记录元素的结构体信息，便于生成selector 时使用
            return true;
        }

        public bool IsCanUse(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {
            //CV走独立的拾取分支
            return false;
        }

        public void ReadyToCapture()
        {
            foreach (var item in CVResultsDic)
            {
                item.Value.Clear();
            }
            CapturingWindowHandle = IntPtr.Zero;
            CVResultsDic.Clear();
            _CapturingWindowCVResults?.Clear();
        }

        public void Release()
        {
            try
            {
                foreach (var item in CVResultsDic)
                {
                    item.Value.Clear();
                }
                CapturingWindowHandle = IntPtr.Zero;
                CVResultsDic.Clear();
                _CapturingWindowCVResults?.Clear();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"清除CV拾取缓存数据异常，不影响后续操作");
            }
        }

        public string GetElementsAll(GetElementsAllParams @params)
        {
            List<JObject> retData = new List<JObject>();
            string cacheKey = $"{@params.WindowHandle.ToString()}";
            string[] clsNameArr = string.IsNullOrEmpty(@params.ClsNames) ? null : @params.ClsNames?.Split(',');


            var rootElement = UiCommon.UIAAutomation.FromHandle(@params.WindowHandle);
            var cvNode = new CVNode(rootElement);
            var allCvElement = cvNode.GetElementsAll(@params);
            var cvRootEleRect = cvNode.BoundingRectangle;
            for (int i = 0; i < allCvElement.Count; i++)
            {
                if (clsNameArr != null && !clsNameArr.Contains(allCvElement[i].result_value.semantic_info.cls))
                {
                    continue;
                }
                var area = allCvElement[i].ZbrainArea.ToRpaRect();
                var area_ZH = allCvElement[i].Area.ToRpaRect();
                var area_ZHPY = allCvElement[i].Area.ToRpaRect();
                area_ZHPY.x += cvRootEleRect.X;
                area_ZHPY.y += cvRootEleRect.Y;
                var cacheID = string.IsNullOrEmpty(allCvElement[i].uniqueId) ? $"{cacheKey}/{i}" : allCvElement[i].uniqueId;
                var dataObj = new JObject()
                    {
                        { "uniqueId",cacheID},
                        { "text",allCvElement[i].result_value.text_contents},
                        { "area",JToken.FromObject(area)},
                        { "area2",JToken.FromObject(area_ZH)},
                        { "area3",JToken.FromObject(area_ZHPY)},
                        { "cls",allCvElement[i].result_value.semantic_info.cls},
                        { "semantic", JToken.FromObject(allCvElement[i].result_value.semantic_info.semantic??new JObject()) },
                        { "anchor", JToken.FromObject(allCvElement[i].result_value.semantic_info.anchor??new JObject()) },
                        { "step", allCvElement[i].step },
                        { "from_mml", allCvElement[i].from_mml }
                    };
                retData.Add(dataObj);
            }

            //{'img_id':'xxx', 'elements':处理后的elemets}
            return new JObject() {
                { "elements",JArray.FromObject( retData)},
                { "img_id",cvNode.ImgId},
                { "current_status",cvNode.CurrentStatus}
            }.ToJsonNoneFormating();
        }

        public string GetTargetElementInfo(GetTargetElementInfoModelParams @params)
        {
            var rootElement = UiCommon.UIAAutomation.FromHandle(@params.WindowHandle);
            var cvNode = new CVNode(rootElement);
            var allCvElement = cvNode.GetTargetElementInfo(@params);
            return allCvElement.ToJsonNoneFormating();
        }

        public string BuildSelectors(BulidSelectorsParams @params)
        {
            var eleIds = JsonConvert.DeserializeObject<string[]>(@params.ElementIds);
            List<string> selectorList = new List<string>();
            IntPtr hwnd = IntPtr.Zero;
            AutomationElement rootElement = null;
            CVNode parentNode = null;
            string[] idItems;
            for (int i = 0; i < eleIds.Length; i++)
            {
                idItems = eleIds[i].Split('/');
                hwnd = (IntPtr)Convert.ToInt32(idItems[0]);
                if (GuiOperator.CVResultsDic.ContainsKey(hwnd) && GuiOperator.CVResultsDic[hwnd].Count > int.Parse(idItems[1]))
                {
                    _CapturingWindowCVResults = CVResultsDic[hwnd];
                    if (rootElement == null)
                    {
                        rootElement = UiCommon.UIAAutomation.FromHandle(hwnd);
                        parentNode = new CVNode(rootElement);
                        parentNode.BuliderSelector += CreateGuiWindowsOperator().BuilderSelector;
                    }
                    var inspectNode = new CVNode(parentNode);
                    ((CVNode)inspectNode).CacheRectangle = GuiOperator.CVResultsDic[hwnd][int.Parse(idItems[1])].Area;
                    ((CVNode)inspectNode).BuliderSelector += CreateGuiWindowsOperator().BuilderSelector;
                    ((CVNode)inspectNode).ElementObj = GuiOperator.CVResultsDic[hwnd][int.Parse(idItems[1])];//记录元素的结构体信息，便于生成selector 时使用
                    selectorList.Add(inspectNode.NodeSelector);
                    _CapturingWindowCVResults.Clear();
                }
            }
            return selectorList.ToJsonNoneFormating();
        }

        public string BuildSelectors(ParamBuildSelectorsModel paramModel)
        {
            throw new NotImplementedException();
        }

        public List<SelectorProps> GetSameParentsProps(ParamCollectDataBySelectorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public List<List<string>> CollectDataBySelector(ParamCollectDataBySelectorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public void ScrollIntoView(string srcoll)
        {
            throw new NotImplementedException();
        }

        public string GetBoundings(string jsonParams)
        {
            throw new NotImplementedException();
        }

        public string BuildSelectors(string jsonParams)
        {
            throw new NotImplementedException();
        }

        public IGuiNode GetElementById(GetElementByIdParams args)
        {
            var idItems = args.ElementId.Split('/');
            var hwnd = (IntPtr)Convert.ToInt32(idItems[0]);
            if (GuiOperator.CVResultsDic.ContainsKey(hwnd) && GuiOperator.CVResultsDic[hwnd].Count > int.Parse(idItems[1]))
            {
                _CapturingWindowCVResults = CVResultsDic[hwnd];
                var rootElement = UiCommon.UIAAutomation.FromHandle(hwnd);
                var parentNode = new CVNode(rootElement);
                parentNode.BuliderSelector += CreateGuiWindowsOperator().BuilderSelector;

                var cvNode = new CVNode(parentNode);
                cvNode.CacheRectangle = GuiOperator.CVResultsDic[hwnd][int.Parse(idItems[1])].Area;
                cvNode.BuliderSelector += CreateGuiWindowsOperator().BuilderSelector;
                cvNode.ElementObj = GuiOperator.CVResultsDic[hwnd][int.Parse(idItems[1])];//记录元素的结构体信息，便于生成selector 时使用
                return cvNode;
            }
            return null;
        }

        public List<List<string>> CollectData_v2(ParamDataCollect_V2Model paramModel)
        {
            throw new NotImplementedException();
        }

        public List<SelectorProps> GetSameParentsProps_v2(ParamDataCollect_V2Model paramModel)
        {
            throw new NotImplementedException();
        }
        public List<JObject> GetSimilarOrDataCollectElements(GetElementsAllParams @params)
        {
            List<JObject> retData = new List<JObject>();
            string cacheKey = $"{@params.WindowHandle.ToString()}";
            string[] clsNameArr = string.IsNullOrEmpty(@params.ClsNames) ? null : @params.ClsNames?.Split(',');


            var rootElement = UiCommon.UIAAutomation.FromHandle(@params.WindowHandle);
            var cvNode = new CVNode(rootElement);
            var cvRootEleRect = cvNode.BoundingRectangle;
            var allCvElement = cvNode.GetSimilarOrDataCollectElements(@params);
            for (int i = 0; i < allCvElement.Count; i++)
            {
                if (clsNameArr != null && !clsNameArr.Contains(allCvElement[i].result_value.semantic_info.cls))
                {
                    continue;
                }
                var area = allCvElement[i].ZbrainArea.ToRpaRect();
                var area_ZH = allCvElement[i].Area.ToRpaRect();
                var area_ZHPY = allCvElement[i].Area.ToRpaRect();
                area_ZHPY.x += cvRootEleRect.X;
                area_ZHPY.y += cvRootEleRect.Y;
                var cacheID = string.IsNullOrEmpty(allCvElement[i].uniqueId) ? $"{cacheKey}/{i}" : allCvElement[i].uniqueId;
                var dataObj = new JObject()
                    {
                        { "uniqueId",cacheID},
                        { "text",allCvElement[i].result_value.text_contents},
                        { "area",JToken.FromObject(area)},
                        { "area2",JToken.FromObject(area_ZH)},
                        { "area3",JToken.FromObject(area_ZHPY)},
                        { "cls",allCvElement[i].result_value.semantic_info.cls},
                        { "semantic", JToken.FromObject(allCvElement[i].result_value.semantic_info.semantic??new JObject()) },
                        { "anchor", JToken.FromObject(allCvElement[i].result_value.semantic_info.anchor??new JObject()) },
                        { "step", allCvElement[i].step },
                        { "from_mml", allCvElement[i].from_mml }
                    };

                retData.Add(dataObj);
            }

            return retData;
        }

        public IGuiNode[] QuerySelectorAll(GetElementFromSelectorParams args)
        {
            throw new NotImplementedException();
        }

        public bool VerifyWindowOfNode(GetElementFromSelectorParams args)
        {
            return true;
        }
    }
}
