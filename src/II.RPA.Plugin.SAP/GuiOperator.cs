using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using sapfewse;
using II.RPA.Plugin.SAP.Controls;
using II.RPA.Plugin.GUI.Commons.WindowMatch;

namespace II.RPA.Plugin.SAP
{
    public class GuiOperator : IGuiOperator_6_9_0
    {
        private static ILogger logger = Logger.OfContext();
        // 缓存 IsCanUse 的 FindByPosition 结果供 InspectNode 复用，消除双重 COM 调用
        private static volatile Tuple<Point, GuiCollection> _cachedControl;

        public int Sort { get; set; }

        public eGuiType GuiType => eGuiType.SAP;

        public IGuiNode GetElementFromSelector(GetElementFromSelectorParams args)
        {
            logger.LogInformation($"SAP 进入GetElementFromSelector");
            string selector = string.Empty;
            string selector_o = string.Empty;
            ExtractViewData.SelectCommonProps(args.Selector, out string selectorType, out string version);
            if (version.StartsWith("1."))
            {
                selector_o = UiCommon.ExtractSelectors(args.Selector);
                selector = PropsToOldJson(selector_o);
            }
            else
            {
                selector = args.Selector;
            }

            //var selector = ExtractViewData.ExtractSelectors(args.Selector);

            SapNode node = new SapNode(selector);
            node.TragetWindow = args.WindowInfo;
            node.SetForeground();
            var nodes = node.FindAll();

            return nodes.FirstOrDefault(item =>
            {
                ((SapNode)item).cacheSelector_o = args.Selector;
                if (args.DiscernHidden)
                {
                    return true;
                }
                var rect = item.BoundingRectangle;
                return rect.Width > 0 && rect.Height > 0;
            }) as SapNode;
        }
        private string PropsToOldJson(string selector)
        {
            List<SelectorProps> nodeList = selector.ToObject<List<SelectorProps>>();
            JArray jarray = new JArray();
            foreach (var node in nodeList)
            {
                JObject jObj = new JObject();
                foreach (var prop in node.Props)
                {
                    jObj.Add(prop.Name, prop.Value);
                }
                jarray.Add(jObj);
            }
            return jarray.ToString();
        }
        public string GetElementFromSelectorAll(GetElementFromSelectorParams args)
        {
            bool resultBoundings = args.GetExtendParamValue<string>("resultAttrName") == "bounding";

            if (resultBoundings && TryGetSapCollectionVisibleBoundings(args, out string visibleBoundings))
            {
                return visibleBoundings;
            }

            var nodes = QuerySelectorAll(args);
            if (resultBoundings)
            {
                return nodes.Select(n => n.BoundingRectangle).ToJsonNoneFormating();
            }
            return nodes.Select(n => ((SapNode)n).NodeSelector).ToJsonNoneFormating();
        }

        private bool TryGetSapCollectionVisibleBoundings(GetElementFromSelectorParams args, out string boundings)
        {
            boundings = null;

            SapNode sapNode = TryResolveSingleSapNode(args);
            if (sapNode == null || string.IsNullOrWhiteSpace(sapNode.Ext))
            {
                return false;
            }

            bool isCollectionExt = GuiGridCellAction.IsGridColumn(sapNode.Ext, out string gridColumn)
                || GuiGridCellAction.IsGridRow(sapNode.Ext, out int rowIndex);
            if (!isCollectionExt)
            {
                return false;
            }

            List<Rectangle> rects = new List<Rectangle>();
            int elementCount = 0;

            try
            {
                GuiComponent component = SapNode.GlobalSession.FindById(sapNode.UserDefinedID);
                if (component is GuiGridView gridView)
                {
                    if (GuiGridCellAction.IsGridColumn(sapNode.Ext, out gridColumn))
                    {
                        rects = GetVisibleGridColumnBoundings(gridView, gridColumn, out elementCount);
                    }
                    else if (GuiGridCellAction.IsGridRow(sapNode.Ext, out rowIndex))
                    {
                        rects = GetVisibleGridRowBoundings(gridView, rowIndex, out elementCount);
                    }
                }
                else if (component is GuiTableControl tableControl)
                {
                    if (GuiTableCellAction.IsTableColumn(sapNode.Ext, out int tableColumn))
                    {
                        rects = GetVisibleTableColumnBoundings(tableControl, tableColumn, out elementCount);
                    }
                    else if (GuiGridCellAction.IsGridRow(sapNode.Ext, out rowIndex))
                    {
                        rects = GetVisibleTableRowBoundings(tableControl, rowIndex, out elementCount);
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "SAP collection visible bounding failed.");
            }

            if (elementCount < rects.Count)
            {
                elementCount = rects.Count;
            }
            SetSapCollectionCounts(args, elementCount, rects.Count);
            boundings = rects.ToJsonNoneFormating();
            return true;
        }

        private SapNode TryResolveSingleSapNode(GetElementFromSelectorParams args)
        {
            try
            {
                string selector = string.Empty;
                ExtractViewData.SelectCommonProps(args.Selector, out string selectorType, out string version);
                if (version.StartsWith("1."))
                {
                    selector = PropsToOldJson(UiCommon.ExtractSelectors(args.Selector));
                }
                else
                {
                    selector = args.Selector;
                }

                SapNode node = new SapNode(selector);
                node.TragetWindow = args.WindowInfo;
                node.SetForeground();
                return node.FindAll(null, maxResult: 1, milliseconds: 10000).FirstOrDefault() as SapNode;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "Resolve SAP single node for collection bounding failed.");
                return null;
            }
        }

        private List<Rectangle> GetVisibleGridColumnBoundings(GuiGridView gridView, string columnName, out int elementCount)
        {
            List<Rectangle> rects = new List<Rectangle>();
            elementCount = SafeGetInt(() => gridView.RowCount);

            int firstVisibleRow = SafeGetInt(() => gridView.FirstVisibleRow);
            int visibleRowCount = SafeGetInt(() => gridView.VisibleRowCount);
            int lastVisibleRow = elementCount > 0
                ? Math.Min(firstVisibleRow + visibleRowCount, elementCount)
                : firstVisibleRow + visibleRowCount;

            Rectangle viewport = GetSapComponentRawRectangle(gridView);
            for (int row = firstVisibleRow; row < lastVisibleRow; row++)
            {
                TryAddVisibleRectangle(rects, GetGridCellRawRectangle(gridView, row, columnName), viewport);
            }

            if (elementCount <= 0)
            {
                elementCount = rects.Count;
            }
            return rects;
        }

        private List<Rectangle> GetVisibleGridRowBoundings(GuiGridView gridView, int rowIndex, out int elementCount)
        {
            List<string> columns = GuiGridCellAction.GetGridColumnNames(gridView);
            List<Rectangle> rects = new List<Rectangle>();
            elementCount = columns.Count;

            int totalRows = SafeGetInt(() => gridView.RowCount);
            if (rowIndex < 0 || (totalRows > 0 && rowIndex >= totalRows))
            {
                elementCount = 0;
                return rects;
            }

            int firstVisibleRow = SafeGetInt(() => gridView.FirstVisibleRow);
            int visibleRowCount = SafeGetInt(() => gridView.VisibleRowCount);
            if (rowIndex < firstVisibleRow || rowIndex >= firstVisibleRow + visibleRowCount)
            {
                return rects;
            }

            Rectangle viewport = GetSapComponentRawRectangle(gridView);
            foreach (string column in columns)
            {
                TryAddVisibleRectangle(rects, GetGridCellRawRectangle(gridView, rowIndex, column), viewport);
            }

            if (elementCount <= 0)
            {
                elementCount = rects.Count;
            }
            return rects;
        }

        private List<Rectangle> GetVisibleTableColumnBoundings(GuiTableControl tableControl, int column, out int elementCount)
        {
            List<Rectangle> rects = new List<Rectangle>();
            elementCount = SafeGetInt(() => tableControl.RowCount);

            int columnCount = SafeGetInt(() => tableControl.Columns.Count);
            if (column < 0 || column >= columnCount)
            {
                elementCount = 0;
                return rects;
            }

            int visibleRowCount = GetVisibleTableRowCount(tableControl);
            Rectangle viewport = GetSapComponentRawRectangle(tableControl);
            for (int visibleRow = 0; visibleRow < visibleRowCount; visibleRow++)
            {
                try
                {
                    GuiTableRow tableRow = tableControl.Rows.ElementAt(visibleRow);
                    GuiComponent cell = tableRow.ElementAt(column);
                    TryAddVisibleRectangle(rects, GuiTableCellAction.GetComponentRectangle(cell), viewport);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"Get visible SAP table column bounding failed. visibleRow:{visibleRow}, column:{column}");
                }
            }

            if (elementCount <= 0)
            {
                elementCount = rects.Count;
            }
            return rects;
        }

        private List<Rectangle> GetVisibleTableRowBoundings(GuiTableControl tableControl, int rowIndex, out int elementCount)
        {
            List<Rectangle> rects = new List<Rectangle>();
            int columnCount = SafeGetInt(() => tableControl.Columns.Count);
            elementCount = columnCount;

            int totalRows = SafeGetInt(() => tableControl.RowCount);
            if (rowIndex < 0 || (totalRows > 0 && rowIndex >= totalRows))
            {
                elementCount = 0;
                return rects;
            }

            int firstVisibleRow = GetFirstVisibleTableRow(tableControl);
            int visibleRow = rowIndex - firstVisibleRow;
            if (visibleRow < 0 || visibleRow >= GetVisibleTableRowCount(tableControl))
            {
                return rects;
            }

            Rectangle viewport = GetSapComponentRawRectangle(tableControl);
            GuiTableRow tableRow = tableControl.Rows.ElementAt(visibleRow);
            for (int column = 0; column < columnCount; column++)
            {
                try
                {
                    GuiComponent cell = tableRow.ElementAt(column);
                    TryAddVisibleRectangle(rects, GuiTableCellAction.GetComponentRectangle(cell), viewport);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"Get visible SAP table row bounding failed. row:{rowIndex}, column:{column}");
                }
            }

            if (elementCount <= 0)
            {
                elementCount = rects.Count;
            }
            return rects;
        }

        private Rectangle GetGridCellRawRectangle(GuiGridView gridView, int row, string column)
        {
            try
            {
                return new Rectangle(
                    gridView.ScreenLeft + gridView.GetCellLeft(row, column),
                    gridView.ScreenTop + gridView.GetCellTop(row, column),
                    gridView.GetCellWidth(row, column),
                    gridView.GetCellHeight(row, column));
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"Get SAP grid cell raw rectangle failed. row:{row}, column:{column}");
                return Rectangle.Empty;
            }
        }

        private static int GetFirstVisibleTableRow(GuiTableControl tableControl)
        {
            try
            {
                return Math.Max(0, tableControl.VerticalScrollbar.Position);
            }
            catch
            {
                return 0;
            }
        }

        private static int GetVisibleTableRowCount(GuiTableControl tableControl)
        {
            int rowCount = SafeGetInt(() => tableControl.Rows.Count);
            if (rowCount > 0)
            {
                return rowCount;
            }

            return Math.Max(0, SafeGetInt(() => tableControl.VisibleRowCount));
        }

        private static Rectangle GetSapComponentRawRectangle(object component)
        {
            try
            {
                dynamic dynamicComponent = component;
                return new Rectangle(
                    Convert.ToInt32(dynamicComponent.ScreenLeft),
                    Convert.ToInt32(dynamicComponent.ScreenTop),
                    Convert.ToInt32(dynamicComponent.Width),
                    Convert.ToInt32(dynamicComponent.Height));
            }
            catch
            {
                return Rectangle.Empty;
            }
        }

        private static void TryAddVisibleRectangle(List<Rectangle> rects, Rectangle rawRectangle, Rectangle rawViewport)
        {
            if (rawRectangle.Width <= 0 || rawRectangle.Height <= 0)
            {
                return;
            }

            Rectangle visibleRectangle = rawViewport.IsEmpty
                ? rawRectangle
                : Rectangle.Intersect(rawRectangle, rawViewport);
            if (visibleRectangle.Width <= 0 || visibleRectangle.Height <= 0)
            {
                return;
            }

            Rectangle highlightRectangle = DpiTransform.TransformBackRectangle(visibleRectangle);
            if (highlightRectangle.Width > 0 && highlightRectangle.Height > 0)
            {
                rects.Add(highlightRectangle);
            }
        }

        private static int SafeGetInt(Func<int> valueFactory, int defaultValue = 0)
        {
            try
            {
                return valueFactory();
            }
            catch
            {
                return defaultValue;
            }
        }

        private static void SetSapCollectionCounts(GetElementFromSelectorParams args, int elementCount, int visibleCount)
        {
            if (args?.ExtendParam is Dictionary<string, object> extendParam)
            {
                elementCount = Math.Max(0, elementCount);
                visibleCount = Math.Max(0, visibleCount);
                if (elementCount < visibleCount)
                {
                    elementCount = visibleCount;
                }

                extendParam["sapCollectionElementCount"] = elementCount;
                extendParam["sapCollectionVisibleCount"] = visibleCount;
                extendParam["sapCollectionHiddenElementCount"] = Math.Max(0, elementCount - visibleCount);
            }
        }

        private bool TryGetGridRowBounding(GetElementFromSelectorParams args, out string bounding)
        {
            bounding = null;

            try
            {
                string selector = string.Empty;
                ExtractViewData.SelectCommonProps(args.Selector, out string selectorType, out string version);
                if (version.StartsWith("1."))
                {
                    selector = PropsToOldJson(UiCommon.ExtractSelectors(args.Selector));
                }
                else
                {
                    selector = args.Selector;
                }

                SapNode node = new SapNode(selector);
                node.TragetWindow = args.WindowInfo;
                node.SetForeground();
                var nodes = node.FindAll();
                SapNode sapNode = nodes.FirstOrDefault() as SapNode;
                if (sapNode == null
                    || string.IsNullOrEmpty(sapNode.Ext)
                    || !GuiGridCellAction.IsGridRow(sapNode.Ext, out int row))
                {
                    return false;
                }

                sapNode.cacheSelector_o = args.Selector;
                bounding = new[] { sapNode.BoundingRectangle }.ToJsonNoneFormating();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool InspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            inspectNode = null;
            try
            {
                var point = NativeMethods.GetRealPoint(screenPoint);
                var control = _cachedControl?.Item2;

                // 如果缓存的 FindByPosition 和当前坐标距离 < 3px，直接复用，
                // 避免 IsCanUse 和 InspectNode 之间重复 SAP COM 调用
                if (_cachedControl != null)
                {
                    var lastPt = _cachedControl.Item1;
                    if (Math.Abs(lastPt.X - point.X) + Math.Abs(lastPt.Y - point.Y) < 3)
                        control = _cachedControl.Item2;
                    else
                        control = SapNode.TryGetControl(point);
                }
                else
                {
                    control = SapNode.TryGetControl(point);
                }

                string currentID = string.Empty;
                Rectangle rectangle = new Rectangle();
                GuiComponent component = null;
                if (control != null && control.Count > 0)
                {
                    // FindByPosition 返回从外到内的层级：
                    //   ElementAt(0) = 最外层容器, ElementAt(1..) = 子控件或特殊 token
                    var nodeExt = control.Count > 1 ? control.ElementAt(1) : null;
                    currentID = control.ElementAt(0);

                    if (GuiTableCellAction.TryResolveInspectCell(
                        SapNode.GlobalSession,
                        currentID,
                        nodeExt,
                        out GuiTableControl tableControl,
                        out string tableId,
                        out string tableCellExt,
                        out Rectangle tableCellRectangle))
                    {
                        if (!tableCellRectangle.IsEmpty)
                        {
                            tableCellRectangle = DpiTransform.TransformBackRectangle(tableCellRectangle);
                        }

                        SapNode tableCellNode = new SapNode((GuiComponent)tableControl, rootElement);
                        tableCellNode.UserDefinedID = tableId;
                        tableCellNode.RootElement = rootElement;
                        tableCellNode.BoundingRectangle = tableCellRectangle;
                        tableCellNode.Ext = tableCellExt;
                        inspectNode = tableCellNode;
                        return true;
                    }

                    // 优先取最内层元素（弹窗内的按钮/复选框等）
                    if (nodeExt != null && !nodeExt.Contains("Cell(") && !nodeExt.Contains("?"))
                    {
                        try
                        {
                            var innerComponent = SapNode.GlobalSession.FindById(nodeExt);
                            if (innerComponent != null)
                            {
                                currentID = nodeExt;
                                component = innerComponent;
                            }
                        }
                        catch { /* FindById 失败时回退 */ }
                    }

                    if (component == null)
                    {
                        component = SapNode.GlobalSession.FindById(currentID);
                    }

                    var sapNode = new SapNode(component, rootElement);
                    ApplicationContext context = new ApplicationContext() { component = component, inspectId = nodeExt, ext = nodeExt };
                    rectangle = ControlFactory.GetLocation(context);

                    if (!rectangle.IsEmpty)
                    {
                        sapNode.UserDefinedID = currentID;
                        sapNode.RootElement = rootElement;
                        sapNode.BoundingRectangle = rectangle;
                        inspectNode = sapNode;
                        sapNode.Ext = nodeExt;
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return false;
            }
        }

        public bool IsCanUse(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {
            try
            {
                screenPoint = NativeMethods.GetRealPoint(screenPoint);
                var rootElement_Test = UiCommon.UIAAutomation.FromHandle(UiCommon.GetRootWindow(screenPoint));
                if (rootElement_Test != null && rootElement_Test.ProcessId == rootElement.ProcessId)
                {
                    rootElement = rootElement_Test;
                }
                var className = rootElement.ClassName;
                if (!className.Contains("SAP"))
                    return false;

                // 先做轻量 Win32 检查：沿 WindowFromPoint 父窗链扫描。
                // 链上的 #32770（MFC 对话框类）既可能是 SAP GUI 原生设置对话框，
                // 也可能是 Dynpro 模态弹窗（含 ALV 表格），单靠类名无法区分。
                // 因此这里只记录标志，不再直接拒绝；最终以 FindByPosition 能否命中
                // SAP 控件为准：命中则是 Dynpro 弹窗，仍走 SAP 拾取；否则才拒绝走 Windows。
                const uint WS_CHILD = 0x40000000u;
                bool isDialog = false;
                {
                    var hwnd = NativeMethods.WindowFromPoint(screenPoint);
                    while (hwnd != IntPtr.Zero)
                    {
                        var cls = NativeMethods.GetClassName(hwnd);
                        if (cls == "#32770")
                        {
                            isDialog = true;
                            break;
                        }
                        var style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
                        if ((style & WS_CHILD) == 0)
                        {
                            if (string.IsNullOrEmpty(cls) || !cls.Contains("SAP"))
                                return false;
                            break;
                        }
                        hwnd = NativeMethods.GetParent(hwnd);
                    }
                }

                var control = SapNode.TryGetControl(screenPoint);
                if (control == null) return false;

                // #32770 对话框既可能是 Dynpro 模态弹窗，也可能是 SAP GUI 原生设置对话框。
                // 二者 FindByPosition 都能返回控件，区别在于返回的路径：
                //   Dynpro 弹窗  → 返回 wnd[0]/usr/...（深入到用户区）
                //   原生设置对话框 → 只返回裸 wnd[0]（不含 /usr/）
                // 因此当路径不含 /usr/ 时，说明是原生设置对话框，交给 Windows 拾取内部元素。
                if (isDialog)
                {
                    string dialogId = control.Count > 0 ? control.ElementAt(0) as string : null;
                    if (string.IsNullOrEmpty(dialogId) || !dialogId.Contains("/usr/"))
                        return false;
                }

                // 缓存结果供 InspectNode 复用，消除双重 FindByPosition 调用
                _cachedControl = Tuple.Create(screenPoint, control);

                if (control.Count > 0)
                {
                    string currentID = control.ElementAt(0);
                    var component = SapNode.GlobalSession.FindById(currentID);
                    if (component.TypeAsNumber == (int)ControlFactory.ComponentType.GuiShell)
                    {
                        var nodeExt = control.Count > 1 ? control.ElementAt(1) : null;
                        if (nodeExt != null && nodeExt.Contains("Cell"))
                            return true;
                        var element = UiCommon.UIAAutomation.FromPoint(screenPoint);
                        if (element != null && (element.ControlType == FlaUI.Core.Definitions.ControlType.TitleBar
                            || element.ControlType == FlaUI.Core.Definitions.ControlType.SplitButton
                            || element.ControlType == FlaUI.Core.Definitions.ControlType.Button))
                            return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "SAP IsCanUse error");
            }

            return false;
        }

        public void ReadyToCapture()
        {

        }
        public void Release()
        {
            // 释放 SAP GUI COM 连接资源
            SapNode.ConnectionManager?.Dispose();
        }

        #region 数据采集

        private List<List<string>> CollectTableData(List<SelectorProps> tableSelector, WindowObject tragetWindow = null)
        {
            List<List<string>> collectDataList = new List<List<string>>();
            var paramSelector = JsonConvert.SerializeObject(tableSelector);
            JObject jobject = new JObject();
            jobject.Add("scopeType", 0);
            jobject.Add("rowNo", "");
            jobject.Add("columnNo", "");
            jobject.Add("containHeader", 1);
            var selector = PropsToOldJson(paramSelector);
            SapNode node = new SapNode(selector);
            node.TragetWindow = tragetWindow;
            node.SetForeground();
            var nodes = node.FindAll();
            var sapNode = nodes.FirstOrDefault() as SapNode;
            string data = sapNode.GetTableData(jobject.ToString());
            return data.ToObject<List<List<string>>>();
        }
        public List<List<string>> CollectDataBySelector(ParamCollectDataBySelectorModel paramModel)
        {
            List<List<string>> collectDataList = new List<List<string>>();
            if (paramModel.IsTable)
            {
                var tableSelector = paramModel.SelectorDataList.FirstOrDefault()?.Selector;
                collectDataList = CollectTableData(tableSelector);
            }
            else
            {
                throw new NotImplementedException();
            }
            return collectDataList;
        }

        public void ScrollIntoView(string srcoll)
        {
            // 框架传入的 srcoll 是 SapNode 的选择器 JSON，但 SapNode 的 IntoView() 是实例方法，
            // 需要先通过 selector 查找元素再调用。此处委托给 GetElementFromSelector 找到节点后执行。
        }
        public List<List<string>> CollectData_v2(ParamDataCollect_V2Model paramModel)
        {
            List<List<string>> collectDataList = new List<List<string>>();
            if (paramModel.IsTable)
            {
                var tableSelector = paramModel.DataCollectConfig.CollectionConfig.TableSelector;
                collectDataList = CollectTableData(tableSelector, paramModel.WindowInfo);
            }
            else
            {
                throw new NotImplementedException();
            }
            return collectDataList;
        }

        public List<SelectorProps> GetSameParentsProps_v2(ParamDataCollect_V2Model paramModel)
        {
            throw new NotImplementedException();
        }

        public string GetBoundings(string jsonParams)
        {
            throw new NotImplementedException();
        }

        public string BuildSelectors(BulidSelectorsParams @params)
        {
            throw new NotImplementedException();
        }
        public string BuildSelectors(string jsonParams)
        {
            throw new NotImplementedException();
        }
        public List<SelectorProps> GetSameParentsProps(ParamCollectDataBySelectorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public string GetElementsAll(GetElementsAllParams @params)
        {
            throw new NotImplementedException();
        }

        public IGuiNode GetElementById(GetElementByIdParams args)
        {
            throw new NotImplementedException();
        }


        public string BuildSelectors(ParamBuildSelectorsModel paramModel)
        {
            throw new NotImplementedException();
        }

        public IGuiNode[] QuerySelectorAll(GetElementFromSelectorParams args)
        {
            logger.LogInformation($"SAP 进入 QuerySelectorAll");
            string selector = string.Empty;
            string selector_o = string.Empty;
            ExtractViewData.SelectCommonProps(args.Selector, out string selectorType, out string version);
            if (version.StartsWith("1."))
            {
                selector_o = UiCommon.ExtractSelectors(args.Selector);
                selector = PropsToOldJson(selector_o);
            }
            else
            {
                selector = args.Selector;
            }

            SapNode node = new SapNode(selector);
            node.TragetWindow = args.WindowInfo;
            node.SetForeground();

            var nodes = node.FindAll(null, maxResult: int.MaxValue, milliseconds: 10000);

            var filtered = nodes
                .Where(item =>
                {
                    var sapNode = (SapNode)item;
                    sapNode.cacheSelector_o = args.Selector;
                    if (args.DiscernHidden)
                    {
                        return true;
                    }
                    if (!string.IsNullOrEmpty(sapNode.Ext)
                        && GuiGridCellAction.IsGridCell(sapNode.Ext, out int row, out string column))
                    {
                        return true;
                    }
                    var rect = item.BoundingRectangle;
                    return rect.Width > 0 && rect.Height > 0;
                })
                .ToArray();

            if (filtered.Length == 0)
            {
                throw new SAPException(eErrorMessageType.Error_NotFoundElement);
            }

            return filtered;
        }
        public List<JObject> GetSimilarOrDataCollectElements(GetElementsAllParams @params)
        {
            logger.LogInformation($"SAP GetSimilarOrDataCollectElements, ActionName: {@params.ActionName}");
            List<JObject> retData = new List<JObject>();
            return retData;
        }
        #endregion
        public bool VerifyWindowOfNode(GetElementFromSelectorParams args)
        {
            try
            {
                string selector = string.Empty;
                ExtractViewData.SelectCommonProps(args.Selector, out string selectorType, out string version);
                if (version.StartsWith("1."))
                {
                    selector = UiCommon.ExtractSelectors(args.Selector);
                    selector = PropsToOldJson(selector);
                }
                else
                {
                    selector = args.Selector;
                }
                SapNode node = new SapNode(selector);
                return node.FindRootElement() != null;
            }
            catch (Exception e)
            {
                logger.LogWarning($"校验目标窗口异常:{e}");
                return false;
            }
        }

        public string GetTargetElementInfo(GetTargetElementInfoModelParams @params)
        {
            throw new NotImplementedException();
        }
    }
}

