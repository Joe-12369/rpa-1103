using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.Java.Config;
using II.RPA.Plugin.JavaAlter.Agent;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.JavaAlter
{
    public class JavaNode : Selector, IGuiNode_6_6_0
    {
        private static ILogger logger = Logger.OfContext();

        //旧版Java元素对象
        public Element Element { get; set; }

        //新版Java元素对象
        public JavaElement JavaElement { get; set; }

        string userDefinedID;
        public string UserDefinedID { get { return userDefinedID; } }

        public string Idx { get; }


        /// <summary>
        /// 元素所在进程的ID
        /// </summary>
        public int ProcessID
        {
            get
            {
                return JavaElement.ProcessId;
            }
            set
            {
                //do nothing
            }
        }

        public string AutomationId { get; }

        public string ClassName { get; set; }

        public string ControlType
        {
            get; set;
        }
        string name;
        public string Name
        {
            get { return name; }
        }

        public string Role { get; }

        public string Description { get; }

        public string ProcessName { get { return "java"; } }

        public string ProcessFullPath { get { return ""; } }

        public string NodeSelector
        {
            get
            {
                return GetSelector(false);
            }

        }
        public string GetSelector(bool innerTextFirst)
        {
            RPACommon common = new RPACommon();

            common.Selector = JavaAgent.Instance.GetElementSelector(this);

            CommonProps commonProps = new CommonProps();
            commonProps.Flag = "2.0";
            commonProps.GUIType = eGuiType.Swing.ToString();
            commonProps.ProcessName = ProcessName;

            ElementDetail elementDetail = new ElementDetail();
            elementDetail.CategoryIconFile = ProcessFullPath + "\\" + ProcessName;
            elementDetail.CategoryName = ProcessName;
            elementDetail.WindowName = elementDetail.CategoryName;
            elementDetail.ElementName = JavaElement.ControlType;
            common.ElementDetail = elementDetail;
            common.CommonProps = commonProps;

            return JsonConvert.SerializeObject(common);

        }
        IntPtr handler;
        public IntPtr WindowHandle { get { return handler; } }
        public IntPtr ElementPtr { get; set; }
        public string XPath { get; set; }
        public IGuiNode Parent { get; }

        public IGuiNode AutomationElementParent { get; }

        public Rectangle BoundingRectangle { get; set; }

        public List<IGuiNode> Children { get; }

        public bool IsTopLevelWindow { get; }

        public bool IsShow
        {
            get
            {
                return true;
            }
        }

        public eGuiType GuiType => eGuiType.Swing;

        public bool IsTable => false;

        public string TableNodeSelector => String.Empty;

        public System.Drawing.Point GetClickablePoint()
        {
            var point = new System.Drawing.Point();
            var rect = GetRegion(3);
            point.X = rect.X + rect.Width / 2;
            point.Y = rect.Y + rect.Height / 2;
            return point;
        }

        public string ClickTableCell(string input)
        {
            throw new NotImplementedException();
        }

        public Bitmap ElementToBitmap()
        {
            throw new NotImplementedException();
        }

        public List<IGuiNode> FindAll(IGuiNode fromElement, int maxResult, int milliseconds)
        {
            throw new NotImplementedException();
        }

        public string FindElementWithPicPath(string strPicPath)
        {
            throw new NotImplementedException();
        }

        public string FindRelativeElementsInfo(string strRelativeSelector, ref JArray context)
        {
            return null;
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

        public string GetElementAttribute(string name, string apiVersion)
        {
            return JavaElement.JavaObject.InvokeGetProperty(name)?.ToString();
        }

        public string GetElementCheck()
        {
            string result = string.Empty;
            try
            {
                return JavaElement.JavaObject.InvokeGetProperty("checked").ToString().ToLower();
            }
            catch (JavaException e)
            {
                throw new JavaException(e.ErrorMessageType, eComponentType.GetElementCheck, e);
            }
            catch (Exception ex)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_GetElementCheckError, eComponentType.GetElementCheck, ex);
            }
        }

        public string GetElementSelect(int selectType)
        {
            throw new NotImplementedException();
        }

        public string GetElementSelectEx(string strJson)
        {
            int selectMode = 0, selectType = 1;
            GetElementSelectExParamModel _GetElementSelectExParam = null;
            try
            {
                _GetElementSelectExParam = JsonConvert.DeserializeObject<GetElementSelectExParamModel>(strJson);
            }
            catch (Exception ex)
            {
                throw new JavaException(eErrorMessageType.Error_ParamsError);
            }
            finally
            {
                if (_GetElementSelectExParam == null || _GetElementSelectExParam.selectMode == -1 || _GetElementSelectExParam.selectType == -1)
                {
                    throw new JavaException(eErrorMessageType.Error_ParamsError);
                }
                selectMode = _GetElementSelectExParam.selectMode;
                selectType = _GetElementSelectExParam.selectType;
            }

            try
            {
                if (selectMode == 0)//获取当前选项
                {
                    if (selectType == 0)//索引
                    {
                        var _selectedIndex = JavaElement.JavaObject.InvokeGetProperty("selectedindex");
                        if(_selectedIndex==null)
                            _selectedIndex = JavaElement.JavaObject.InvokeGetProperty("selecteditemindex");
                        var retJson = new JObject
                            {{"ret", Convert.ToInt32(_selectedIndex)}}.ToString();
                        return retJson;
                    }
                    else //1 文本
                    {
                        string _selectedText = JavaElement.JavaObject.InvokeGetProperty("text")?.ToString();
                        if(string.IsNullOrEmpty(_selectedText))
                             _selectedText = JavaElement.JavaObject.InvokeGetProperty("selecteditemtext")?.ToString();
                        if(string.IsNullOrEmpty(_selectedText))
                            _selectedText = JavaElement.JavaObject.InvokeGetProperty("selection")?.ToString();
                        var retJson = new JObject
                            {{"ret", _selectedText?.ToString()}}.ToString();
                        return retJson;
                    }
                }
                else //获取全部
                {
                    throw new JavaException(eErrorMessageType.Error_ElementNotOperation);
                }
            }
            catch (JavaException e)
            {
                throw new JavaException(e.ErrorMessageType, eComponentType.GetElementSelect, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_GetSelectError, eComponentType.GetElementSelect, e);
            }
        }
        public string GetEnableClick()
        {
            var enable = JavaElement.JavaObject.InvokeGetSpecialProperty("enabled", Array.Empty<object>())[0];
            return enable.ToString().ToLower();
        }

        public Rectangle GetRegion(int positionType)
        {
            if (positionType == 1)
            {
                return Rectangle.Empty;
            }
            else if (positionType == 2)
            {
                try
                {
                    IntPtr handle = NativeMethods.GetForegroundWindow();
                    if (handle == IntPtr.Zero)
                    {
                        return BoundingRectangle;
                    }

                    var windowElement = GUI.Commons.UiCommon.UIAAutomation.FromHandle(handle);
                    if (windowElement == null || !windowElement.Properties.BoundingRectangle.IsSupported)
                    {
                        return BoundingRectangle;
                    }

                    Rectangle windowRectangle = windowElement.BoundingRectangle;
                    return new Rectangle(
                        BoundingRectangle.X - windowRectangle.X,
                        BoundingRectangle.Y - windowRectangle.Y,
                        BoundingRectangle.Width,
                        BoundingRectangle.Height);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "JavaAlter GetRegion failed to get window-relative coordinates, returning screen coordinates");
                    return BoundingRectangle;
                }
            }
            else if (positionType == 3)
            {
                return BoundingRectangle;
            }

            return BoundingRectangle;
        }

        public string GetSubElements()
        {
            throw new NotImplementedException();
        }

        public string GetTableData(string input)
        {
            throw new NotImplementedException();
        }

        public string GetText()
        {
            return JavaElement.JavaObject.InvokeGetProperty("text")?.ToString();
        }

        public void IntoView()
        {
            try
            {
                SetForeground();
                Thread.Sleep(100);

                var oldRect = BoundingRectangle;
                logger.LogInformation($"[IntoView] START — BoundingRectangle before scroll: {oldRect}");

                // 步骤1: 尝试Java注入的ensurevisible
                var ensureVisibleWorked = false;
                if (JavaElement?.JavaObject != null)
                {
                    try
                    {
                        JavaElement.JavaObject.InvokeSpecial("ensurevisible", Array.Empty<object>());
                        logger.LogInformation("[IntoView] ensurevisible called, waiting 200ms...");
                        Thread.Sleep(200);
                        RefreshBoundingRectangle();
                        var deltaY = BoundingRectangle.Y - oldRect.Y;
                        ensureVisibleWorked = deltaY != 0;
                        logger.LogInformation($"[IntoView] ensurevisible done — deltaY={deltaY}, worked={ensureVisibleWorked}, rect={BoundingRectangle}");
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "ensurevisible failed");
                    }
                }

                if (ensureVisibleWorked)
                {
                    logger.LogInformation("[IntoView] END — ensurevisible worked, element moved");
                    return;
                }

                // 步骤2: ensurevisible无效（如JTree节点），通过Java注入API直接操作JScrollPane scrollbar
                logger.LogInformation("[IntoView] ensurevisible was NO-OP, trying Java API scrollbar approach...");
                if (TryScrollViaJavaAPI(oldRect))
                {
                    // 滚动成功后等待UI渲染并刷新元素位置，否则MouseClick会使用滚动前的旧坐标
                    Thread.Sleep(200);
                    RefreshBoundingRectangle();
                    logger.LogInformation($"[IntoView] END (JavaScroll) — BoundingRectangle after refresh: {BoundingRectangle}");
                }
                else
                {
                    logger.LogInformation("[IntoView] END — Java API scroll had no effect");
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "JavaAlter IntoView failed, continuing with click");
            }
        }

        /// <summary>
        /// 通过Java注入API直接操作JScrollPane的vertical scrollbar，将元素滚动到可见区域。
        /// 不走Windows消息（PostMessage WM_MOUSEWHEEL对Java AWT无效）。
        /// </summary>
        private bool TryScrollViaJavaAPI(Rectangle oldRect)
        {
            try
            {
                var queryPath = JavaElement?.QueryPath;
                if (string.IsNullOrEmpty(queryPath))
                {
                    logger.LogInformation("[JavaScroll] SKIP — no QueryPath");
                    return false;
                }

                // 从元素XPath提取UIScrollPane和JViewport的XPath
                // QueryPath格式: /.../UIScrollPane[idx]/JViewport[idx]/JTree[...]
                // 需要: UIScrollPane XPath 和 JViewport XPath
                var segments = queryPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                int scrollPaneIdx = -1, viewportIdx = -1;
                for (int i = 0; i < segments.Length; i++)
                {
                    if (segments[i].StartsWith("UIScrollPane")) scrollPaneIdx = i;
                    if (segments[i].StartsWith("JViewport")) viewportIdx = i;
                }

                if (scrollPaneIdx < 0 || viewportIdx < 0)
                {
                    logger.LogInformation($"[JavaScroll] SKIP — no UIScrollPane/JViewport in path, scrollPaneIdx={scrollPaneIdx}, viewportIdx={viewportIdx}");
                    return false;
                }

                var scrollPanePath = "/" + string.Join("/", segments, 0, scrollPaneIdx + 1);
                var viewportPath = "/" + string.Join("/", segments, 0, viewportIdx + 1);
                logger.LogInformation($"[JavaScroll] scrollPanePath={scrollPanePath}");
                logger.LogInformation($"[JavaScroll] viewportPath={viewportPath}");

                var proxy = JavaElement.JavaObject.Proxy;
                var tech = JavaElement.JavaObject.Tech;

                // 1. 查询JViewport获取其屏幕矩形（用于计算元素相对viewport的位置）
                var vpResult = proxy.CallFunction(JavaTech.DontCare, FunctionId.FuncQueryElementFromXpath,
                    new object[] { viewportPath, (int)tech }, out CallStatus vpStatus);
                Rectangle viewportRect = Rectangle.Empty;
                if (vpStatus == CallStatus.Ok && vpResult != null && vpResult.Length > 3 && vpResult[1] is Rectangle vr)
                    viewportRect = vr;

                logger.LogInformation($"[JavaScroll] viewportRect={viewportRect}, elemRect={oldRect}");

                // 2. 查询UIScrollPane获取Java对象
                var spResult = proxy.CallFunction(JavaTech.DontCare, FunctionId.FuncQueryElementFromXpath,
                    new object[] { scrollPanePath, (int)tech }, out CallStatus spStatus);
                if (spStatus != CallStatus.Ok || spResult == null || spResult.Length == 0 || !(spResult[0] is JavaObjectWrapper scrollPaneObj))
                {
                    logger.LogInformation($"[JavaScroll] FAIL — query UIScrollPane failed, status={spStatus}");
                    return false;
                }

                // 3. 获取vertical scrollbar: scrollPane.getVerticalScrollBar()
                var vsbResult = scrollPaneObj.InvokeMethod("getVerticalScrollBar", Array.Empty<object>());
                if (vsbResult == null || vsbResult.Length == 0 || !(vsbResult[0] is JavaObjectWrapper vsbObj))
                {
                    logger.LogInformation("[JavaScroll] FAIL — getVerticalScrollBar returned null");
                    return false;
                }

                // 4. 读取当前scrollbar值: scrollBar.getValue()
                var curValResult = vsbObj.InvokeMethod("getValue", Array.Empty<object>());
                int curValue = (curValResult != null && curValResult.Length > 0) ? Convert.ToInt32(curValResult[0]) : 0;

                // 读取scrollbar范围
                var maxValResult = vsbObj.InvokeMethod("getMaximum", Array.Empty<object>());
                int maxValue = (maxValResult != null && maxValResult.Length > 0) ? Convert.ToInt32(maxValResult[0]) : 0;

                // 读取viewport extent (可见区域高度)
                var extentResult = vsbObj.InvokeMethod("getVisibleAmount", Array.Empty<object>());
                int visibleAmount = (extentResult != null && extentResult.Length > 0) ? Convert.ToInt32(extentResult[0]) : 200;

                logger.LogInformation($"[JavaScroll] scrollbar: curValue={curValue}, maxValue={maxValue}, visibleAmount={visibleAmount}");

                // 5. 计算目标scrollbar值
                // 元素在viewport内的相对Y = 元素屏幕Y - viewport屏幕Y
                // 元素在JTree模型中的Y = curValue + 相对Y (近似，因为可能有行高等因素)
                // 目标scroll值使元素位于viewport中央下部1/3处
                int elemScreenY = oldRect.Y + oldRect.Height / 2;
                int viewportScreenY = viewportRect.Y;
                int viewportVisibleHeight = viewportRect.Height > 0 ? viewportRect.Height : visibleAmount;

                // 元素在viewport可见区域内的偏移（可能为负，表示在viewport上方）
                int relativeY = elemScreenY - viewportScreenY;
                // 元素在scroll模型中的Y位置
                int modelY = curValue + relativeY;
                // 目标：让元素显示在viewport上部1/3处
                int targetValue = modelY - viewportVisibleHeight / 3;

                // 边界保护
                targetValue = Math.Max(0, targetValue);
                targetValue = Math.Min(targetValue, maxValue - visibleAmount);

                logger.LogInformation($"[JavaScroll] elemScreenY={elemScreenY}, viewportScreenY={viewportScreenY}, viewportH={viewportVisibleHeight}, relativeY={relativeY}, modelY={modelY}, targetValue={targetValue}");

                if (Math.Abs(targetValue - curValue) < 10)
                {
                    logger.LogInformation("[JavaScroll] SKIP — already at target position");
                    return false;
                }

                // 6. 设置scrollbar值: scrollBar.setValue(targetValue)
                vsbObj.InvokeVoid("setValue", new object[] { targetValue });
                logger.LogInformation($"[JavaScroll] setValue({targetValue}) called successfully");

                return true;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "[JavaScroll] failed with exception");
                return false;
            }
        }

        /// <summary>
        /// 重新查询元素屏幕位置，更新BoundingRectangle（滚动后元素位置会改变）
        /// 优先使用元素查找时的ConvertSelectorToXPath路径（与FuncQueryElementFromXpath兼容），
        /// 回退到FuncGetElementXpath（其返回的路径格式可能与查询不兼容导致返回2字段）。
        /// </summary>
        private void RefreshBoundingRectangle()
        {
            try
            {
                if (JavaElement?.JavaObject == null)
                {
                    logger.LogInformation("[RefreshBR] SKIP — JavaObject is null");
                    return;
                }

                var oldRect = this.BoundingRectangle;

                // 优先使用元素查找时的XPath(ConvertSelectorToXPath格式)，与FuncQueryElementFromXpath兼容
                var elementPath = JavaElement.QueryPath;
                if (!string.IsNullOrEmpty(elementPath))
                {
                    logger.LogInformation($"[RefreshBR] Using QueryPath: {elementPath}");
                }
                else
                {
                    // 回退到GetElementPath(通过FuncGetElementXpath,格式可能不兼容)
                    elementPath = JavaElement.GetElementPath();
                    logger.LogInformation($"[RefreshBR] Using GetElementPath: {elementPath}");
                }

                if (string.IsNullOrEmpty(elementPath))
                {
                    logger.LogInformation("[RefreshBR] SKIP — elementPath is null/empty");
                    return;
                }

                var proxy = JavaElement.JavaObject.Proxy;
                var tech = JavaElement.JavaObject.Tech;
                // 注意: CallFunction的第一个tech参数会被加到FunctionId上(!)，
                // 必须使用JavaTech.DontCare(0)避免改变FuncQueryElementFromXpath的ID。
                var hitResult = proxy.CallFunction(
                    JavaTech.DontCare,
                    FunctionId.FuncQueryElementFromXpath,
                    new object[] { elementPath, (int)tech },
                    out CallStatus status);

                if (status == CallStatus.Ok && hitResult != null && hitResult.Length > 3)
                {
                    var newRect = hitResult[1] is Rectangle r ? r : Rectangle.Empty;
                    if (newRect.Width > 0 && newRect.Height > 0)
                    {
                        this.BoundingRectangle = newRect;
                        logger.LogInformation($"[RefreshBR] UPDATED (XPath) — old: {oldRect}, new: {newRect}, delta: ({newRect.X - oldRect.X}, {newRect.Y - oldRect.Y})");
                    }
                    else
                    {
                        logger.LogInformation($"[RefreshBR] FAIL — XPath result rect is empty: {newRect}");
                    }
                }
                else
                {
                    logger.LogInformation($"[RefreshBR] FAIL — XPath query failed, status={status}, resultLen={hitResult?.Length ?? 0}");
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "JavaAlter RefreshBoundingRectangle failed");
            }
        }

        public void MouseClick(int offsetX, int offsetY, UiElementClickParams clickParams)
        {
            IntoView();
            logger.LogInformation($"[MouseClick] after IntoView — BoundingRectangle={BoundingRectangle}, offset=({offsetX},{offsetY}), cursorPos={clickParams?.cursorPos}, operateType={clickParams?.operateType}");
            if (clickParams.operateType == 0 && JavaElement.Tech == JavaTech.Awt) //后台单击
            {
                try
                {
                    var result = JavaElement.CallFunction("click", new object[] { offsetX, offsetY });
                    if (result != null)
                        return;
                }
                catch (Exception ex)
                {

                }
            }
            if (JavaConfigurations.Info.ClickBefore > 0)
            {
                Thread.Sleep(JavaConfigurations.Info.ClickBefore);
            }
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Left));
            if (JavaConfigurations.Info.ClickAfter > 0)
            {
                Thread.Sleep(JavaConfigurations.Info.ClickAfter);
            }

        }

        public void MouseConterClick(int offsetX, int offsetY, UiElementClickParams clickParams)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Middle));
        }
        public void MouseMove(int cursorPos, int offsetX = 0, int offsetY = 0, bool isUsePerson = false)
        {
            var rect = GetRegion(3);
            Point point = new Point(0, 0);
            if (cursorPos == MouseParamDef.CUR_POS_CENTER)
            {
                point = GetClickablePoint();
                point = new Point(point.X + offsetX, point.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_LEFT_TOP)//左上
            {
                point = new Point(rect.X + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_TOP)//右上
            {
                point = new Point(rect.X + rect.Width + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_LEFT_BOTTOM)//左下
            {
                point = new Point(rect.X + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_BOTTOM)//右下
            {
                point = new Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height + offsetY);
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
            logger.LogInformation($"[MouseMove] Moving to ({point.X},{point.Y}), rect=({rect.X},{rect.Y},{rect.Width},{rect.Height}), cursorPos={cursorPos}");
            II.RPA.Plugin.GUI.Commons.MouseMotions.MouseMotionHelper.PersonMoveMouse(isUsePerson, point);
        }

        public void MouseConterDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.DoubleClick(MouseButton.Middle));
        }

        public void MouseDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams)
        {
            IntoView();
            if (clickParams.operateType == 0)
            {
                if (JavaElement.Tech == JavaTech.Awt)
                {
                    try
                    {
                        var result = JavaElement.CallFunction("doubleclick", new object[] { offsetX, offsetY });
                        if (result != null)
                            return;
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.DoubleClick());
        }

        public void MouseHover(UiElementHoverParams hoverParams)
        {
            if (hoverParams == null)
                hoverParams = new UiElementHoverParams();
            MouseMove(hoverParams.cursorPos, hoverParams.offsetX, hoverParams.offsetY, hoverParams.isUsePerson);
        }

        public void MouseRightClick(int offsetX, int offsetY, UiElementClickParams clickParams)
        {
            IntoView();
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Right));
        }

        public void MouseRightDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams)
        {
            IntoView();
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.RightDoubleClick());
        }
        public string GetElementAttribute_V1(string name, bool isIntact, string apiVersion)
        {
            if (name.ToLower() == "matchedcount")
            {
                return JavaElement.MatchedCount.ToString();
            }
            return JavaElement.JavaObject.InvokeGetProperty(name).ToString();
        }
        public void SetElementAttribute(string name, string value)
        {
            JavaElement.JavaObject.InvokeSetProperty(name, value);
        }

        public void SetElementCheck(int check)
        {
            try
            {
                JavaElement.JavaObject.InvokeSetProperty("checked", check > 0 ? true : false);
            }
            catch (JavaException e)
            {
                throw new JavaException(e.ErrorMessageType, eComponentType.SetElementCheck, e);
            }
            catch (Exception ex)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_SetElementCheckError, eComponentType.SetElementCheck, ex);
            }
        }

        public void SetElementMultipleSelect(int selectType, string setValue)
        {
            throw new NotImplementedException();
        }

        public void SetElementSelect(int selectType, string setValue)
        {
            try
            {
                if (selectType == 0)   //按顺序设置
                {

                    bool intValue = int.TryParse(setValue, out int idx);
                    if (!intValue)
                    {
                        throw new JavaException(eErrorMessageType.Error_SelectValueIsNotValid);
                    }
                    JavaElement.JavaObject.InvokeSetProperty("selectedindex", intValue);
                }
                else
                {
                    JavaElement.JavaObject.InvokeSetProperty("SELECTION", setValue);
                }

            }
            catch (JavaException e)
            {
                throw new JavaException(e.ErrorMessageType, eComponentType.SetElementSelect, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_SetSelectError, eComponentType.SetElementSelect, e);
            }
        }

        public void SetForeground()
        {
            try
            {
                int pid = ProcessID;
                if (pid <= 0) return;

                var process = Process.GetProcessById(pid);
                IntPtr foundHwnd = process.MainWindowHandle;

                if (foundHwnd != IntPtr.Zero)
                {
                    // 缓存窗口句柄，供后续IntoView fallback等使用
                    handler = foundHwnd;
                    GUI.Commons.UiCommon.ForceShow(foundHwnd);
                    logger.LogInformation($"[SetForeground] ForceShow hwnd=0x{foundHwnd.ToInt64():X}, pid={pid}");
                }
                else
                {
                    logger.LogInformation($"[SetForeground] No MainWindowHandle for pid={pid}");
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "JavaAlter SetForeground failed, continuing");
            }
        }

        public void SetText(string text, int isReplace, bool isSimulate)
        {
            if (!isSimulate)
            {
                SetTextInBackground(text, isReplace);
                return;
            }
            var replace = isReplace == 0 ? true : false;
            UiElementClickParams clickParams = new UiElementClickParams();
            clickParams.operateType = 1;
            clickParams.cursorPos = 0;
            MouseClick(0, 0, clickParams);
            if (replace)
            {
                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                Thread.Sleep(500);
                KeyboardSimulate.KeyboardSimulateInput(text);
            }
            else
            {
                KeyboardSimulate.KeyboardSimulateInput(text);
            }
        }

        public void MouseThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
            {
                Mouse.DoubleClick();
                Mouse.Click();
            });
        }

        public void MouseRightThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
            {
                Mouse.RightDoubleClick();
                Mouse.RightClick();
            });
        }

        public void MouseConterThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
            {
                Mouse.DoubleClick(FlaUI.Core.Input.MouseButton.Middle);
                Mouse.Click(FlaUI.Core.Input.MouseButton.Middle);
            });
        }
        public void SetText(string text, int isReplace, int inputType, int interval = 0)
        {
            if (inputType == 0)
            {
                SetTextInBackground(text, isReplace);
                return;
            }
            var replace = isReplace == 0 ? true : false;
            UiElementClickParams clickParams = new UiElementClickParams();
            clickParams.operateType = 1;
            clickParams.cursorPos = 0;
            MouseClick(0, 0, clickParams);

            SetTextEx(replace, interval, text);
        }


        #region 新方式处理模拟输入
        public void SetTextEx(bool replace, int interval, string text)
        {
            if (replace)
            {
                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);

                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                Thread.Sleep(200);
            }
            else
            {
                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);

                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                Thread.Sleep(200);

                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.RIGHT);
                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.RIGHT);
                Thread.Sleep(200);
            }

            if (interval > 0)
            {
                List<string> _ButtinList = BreakString(text);
                foreach (var item in _ButtinList)
                {
                    KeyboardSimulate.KeyboardSimulateInput(item);
                    Thread.Sleep(interval);
                }
            }
            else
            {
                KeyboardSimulate.KeyboardSimulateInput(text);
            }
        }

        public List<string> BreakString(string _str)
        {
            //char[] strCharArr = _str.ToCharArray();
            List<string> RetList = new List<string>();
            foreach (var item in _str.ToCharArray())
            {
                RetList.Add(item.ToString());
            }
            return RetList;
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

        #endregion

        public string GetSubElements(ParamGetSubElements param)
        {
            //未单独实现的类型，按照老的逻辑返回子元素
            return this.GetSubElements();
        }

        public string GetParent(ParamGetParent param)
        {
            throw new NotImplementedException();
        }

        public string GetSiblings(ParamGetSiblings param)
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
        /// <summary>
        /// 输入组合按键，被组合的按键在输入的时候指定一个延迟，避免组合不生效
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="keys"></param>
        private void TypeCombine(int interval, params VirtualKeyShort[] keys)
        {
            foreach (var key in keys)
            {
                Keyboard.Press(key);
                Thread.Sleep(interval);
            }

            foreach (var key in keys.Reverse())
            {
                Keyboard.Release(key);
            }
        }
        /// <summary>
        /// 向剪贴板中添加文本
        /// </summary>
        /// <param name="text">文本</param>
        public static void ClipboardSetText(string text)
        {
            if (!NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                ClipboardSetText(text);
                return;
            }
            NativeMethods.EmptyClipboard();
            NativeMethods.SetClipboardData(13, Marshal.StringToHGlobalUni(text));
            NativeMethods.CloseClipboard();
        }
        public bool SetText(ParamSetTextModel param)
        {
            if (param.inputType == 0)
            {
                SetTextInBackground(param.text, param.isReplace);
                return true;
            }

            UiElementClickParams clickParams = new UiElementClickParams();
            clickParams.operateType = 1;
            clickParams.cursorPos = param.ElementPosition;
            MouseClick(param.OffsetX, param.OffsetY, clickParams);
            Thread.Sleep(param.FocusTimeout);
            var replace = param.isReplace == 0 ? true : false;
            if (replace)
            {
                AutoItHelper.ClearWords(param.Interval);
            }
            else
            {
                AutoItHelper.MoveCursorToEnd(param.Interval);
            }
            Thread.Sleep(200);
            if (param.inputType == 1)
            {
                if (param.Trigger == 1)
                {
                    SetInputMethod();
                }
                AutoItHelper.Send(param.text, false, param.Interval);
            }
            else
            {
                ClipboardSetText(param.text);
                //Ctrl + V
                TypeCombine(20, VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
            }
            var verify = true;
            if (param.isVerify == 1)
            {
                if (replace)
                    verify = GetText().Equals(param.text);
                else
                    verify = GetText().EndsWith(param.text);
            }
            return verify;
        }

        private void SetTextInBackground(string text, int isReplace)
        {
            try
            {
                BackgroundTextInput.SetText(JavaElement?.JavaObject, text, isReplace == 0);
            }
            catch (Exception ex)
            {
                throw new JavaException(eErrorMessageType.Error_SetTextError, eComponentType.SetText, ex);
            }
        }

        public IGuiNode[] QueryTargetSelectorAll(ParamQueryTargetSelectorAllModel param)
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
            setValue = setValue.ToObject<List<string>>((ex) =>
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_SelectValueIsNotValid, ex);
            }).First();
            SetElementSelect(selectType, setValue);
        }

        public string GetCleanHtml()
        {
            throw new NotImplementedException();
        }
    }
}

