using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Input;
using II.RPA.Logging;
using II.RPA.Logging.Formatting;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.MouseMotions;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.SAP.Controls;
using II.RPA.Plugin.SAP.Internal;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sapfewse;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static II.RPA.Plugin.SAP.ControlFactory;

namespace II.RPA.Plugin.SAP
{
    class SapNode : Selector, IGuiNode_6_6_0
        , IGuiSAPNode
    {
        private static ILogger logger = Logger.OfContext();

        /// <summary>
        /// SAP 连接管理器（单例），统一管理 SAP GUI COM 连接生命周期。
        /// </summary>
        private static readonly SapConnectionManager _connectionManager = new SapConnectionManager();

        /// <summary>
        /// 供 Controls 命名空间内部使用的连接管理器访问器。
        /// </summary>
        internal static SapConnectionManager ConnectionManager => _connectionManager;

        /// <summary>
        /// [兼容旧代码] 全局 Session 桥接属性，委托给 SapConnectionManager。
        /// 新代码应直接使用 ConnectionManager.CurrentSession。
        /// </summary>
        internal static GuiSession GlobalSession
        {
            get { return _connectionManager.CurrentSession; }
            set { /* 由 _connectionManager 管理，忽略外部赋值 */ }
        }

        internal AutomationElement RootElement;
        GuiComponent RawElement;
        internal WindowObject TragetWindow { get; set; }
        #region ProcessID 元素所在进程的ID
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
        #endregion

        bool isTree = false;  //是否是树控件，因为处理方式不一样，所以需要特别区分

        public string TreeNodeKey { get; set; }  //树节点的key，只有在isTree为True时，才有用

        public SapNode(GuiComponent rawElement, AutomationElement rootElement)
        {
            this.RawElement = rawElement;
            this.RootElement = rootElement;

            if (rawElement.TypeAsNumber == (int)ComponentType.GuiShell)
            {
                GuiShell shell = (GuiShell)rawElement;
                if (shell is GuiTree guiTree)
                {
                    isTree = true;
                }
            }
        }
        public SapNode(string json) : base(json)
        {
            this.cacheSelector = json;
        }
        public void MouseConterClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Middle));
        }

        public void MouseConterDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            throw new NotImplementedException();
        }
        //public string GetSubElements()
        //{

        //}

        private List<string> ReBuildSelector(List<string> subKeyList)
        {
            List<string> selectorList = new List<string>();
            var common = JsonConvert.DeserializeObject<RPACommon>(this.cacheSelector_o);
            List<SelectorProps> nodeList = common.Selector.ToObject<List<SelectorProps>>();
            var sapNode = nodeList.Last();
            if (sapNode.Props == null)
            {
                sapNode.Props = new List<Props>();
            }

            if (subKeyList.Count > 0)
            {
                foreach (var key in subKeyList)
                {
                    sapNode.Props.RemoveAll(x =>
                        x.Name.EqualIgnoreCase("ext")
                        || x.Name.EqualIgnoreCase("columnName")
                        || x.Name.EqualIgnoreCase("rowIndex"));
                    sapNode.Props.Add(new Props
                    {
                        Name = "ext",
                        Value = $"{key}",
                        Pattern = "equal",
                        Accurate = "true"
                    });
                    common.Selector = JArray.FromObject(nodeList);
                    selectorList.Add(common.ToJson());

                }
            }
            return selectorList;
        }
        static void AttachSap()
        {
            logger.LogInformation("进入 AttachSap");
            _connectionManager.Connect();
            logger.LogInformation("AttachSap 完成");
        }

        /// <summary>
        /// 确保 RawElement 已初始化。复用已有引用，避免重复 FindById 调用。
        /// </summary>
        private GuiComponent EnsureRawElement()
        {
            if (!_connectionManager.IsConnected)
                AttachSap();
            if (RawElement == null && _connectionManager.CurrentSession != null)
                RawElement = _connectionManager.CurrentSession.FindById(UserDefinedID);
            return RawElement;
        }

        /// <summary>
        /// 获取SAP控件，如获取结果为空则说明并非SAP组件
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static GuiCollection TryGetControl(Point point)
        {
            GuiCollection guiCollection = null;
            try
            {
                AttachSap();
                guiCollection = GlobalSession.FindByPosition(point.X, point.Y, false);
                return guiCollection;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return guiCollection;
            }
        }

        #region 属性
        //add by wenyi ,sap node extension
        public string ExtType
        {
            get
            {
                return string.IsNullOrEmpty(Ext) ? null : Ext.Contains("Cell") ? "单元格".I18n("SapNode_B8D22A17") : isTree == true ? "树".I18n("SapNode_334D15A") : null;
            }
            private set { }
        }
        /// <summary>
        /// 在树节点时，用以定位树列表的辅助信息，如1、C 3； 该值在拾取的时候通过element.At(1)获得；
        /// </summary>
        public string SubId
        {
            get; set;
        }
        /// <summary>
        /// 在树节点时的节点辅助信息，如节点1文本/节点2文本/节点3文本；由文本链形成辅助文本；
        /// </summary>
        public string Ext
        {
            get; set;
        }

        /// <summary>
        /// 即sap id ，如usr/wnd/shell[0]/shell
        /// </summary>
        public string UserDefinedID { get; set; }

        public string Idx { get; set; }

        public string AutomationId { get { return ""; } }

        public string ClassName { get { return ""; } }

        public string ControlType { get { return ExtType ?? RawElement.Type; } }

        public string Name { get { return RawElement.Name; } }

        public string Role => "";

        public string Description => "";
        private string cachedProcessName;
        private string cachedProcessFullPath;
        public string ProcessName
        {
            get
            {
                if (cachedProcessName == null)
                {
                    try
                    {
                        var processId = RootElement.Properties.ProcessId.Value;
                        var name = UiCommon.GetProcessName(processId);

                        cachedProcessName = name;
                    }
                    catch (Exception)
                    {
                        cachedProcessName = "";
                    }
                }

                return cachedProcessName;
            }
        }
        public string ProcessFullPath
        {
            get
            {
                if (cachedProcessFullPath == null)
                {
                    try
                    {
                        var processId = RootElement.Properties.ProcessId.Value;
                        var path = UiCommon.GetProcessFullPath(processId);

                        cachedProcessFullPath = path;
                    }
                    catch (Exception)
                    {
                        cachedProcessFullPath = "";
                    }
                }

                return cachedProcessFullPath;
            }
        }

        public IntPtr WindowHandle
        {
            get
            {
                try
                {
                    if (RootElement.Properties.NativeWindowHandle.IsSupported)
                    {
                        var windowHandle = RootElement.Properties.NativeWindowHandle.ValueOrDefault;
                        return windowHandle;
                    }
                    else
                    {
                        return IntPtr.Zero;
                    }
                }
                catch (Exception)
                {
                    return IntPtr.Zero;
                }
            }
        }

        public IGuiNode Parent
        {
            get
            {
                return new SapNode(this.RawElement.Parent);
            }
        }

        public IGuiNode AutomationElementParent => throw new NotImplementedException();
        private Rectangle boundingRectangle;
        public Rectangle BoundingRectangle
        {
            get
            {
                if (boundingRectangle == Rectangle.Empty)
                {
                    boundingRectangle = this.GetRegion(3);
                }
                return boundingRectangle;
            }
            set
            {
                boundingRectangle = value;
            }
        }

        public List<IGuiNode> Children => throw new NotImplementedException();

        public bool IsTopLevelWindow
        {
            get
            {
                try
                {
                    return RootElement.Parent != null && RootElement.Parent.Parent == null;

                }
                catch (Exception)
                {
                    return false;
                }

            }
        }

        private string cacheSelector;
        internal string cacheSelector_o;
        public string NodeSelector
        {
            get
            {
                return GetSelector(false);
            }
        }
        public string GetSelector(bool innerTextFirst)
        {
            if (string.IsNullOrEmpty(cacheSelector))
            {
                cacheSelector = GetSelector();
                var processId = RootElement.Properties.ProcessId.Value;
                var path = ExtractViewData.GetMainModuleFilepath(processId, out var processName);
                cacheSelector = ExtractViewData.AddCommonProps(cacheSelector, eGuiType.SAP, path, processName: processName);
            }
            return cacheSelector;
        }
        #endregion

        private string GetSelector()
        {
            string selector = string.Empty;
            try
            {
                SAPSelectorItem root = new SAPSelectorItem(this.RootElement);
                root.Enabled = true;
                this.Add(root);

                if (isTree == true)// 如果是树节点，ext需要组装，和父节点一起进行组装。反向递归获取节点信息
                {
                    Ext = SAPExtend.GetNodeTextPathByKey(this.RawElement as GuiTree, this.Ext);
                }
                SAPSelectorItem id = new SAPSelectorItem(this.UserDefinedID, Ext);
                id.Enabled = true;
                this.Add(id);

                selector = this.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return selector;
        }
        public AutomationElement FindRootElement()
        {
            AutomationElement element = null;
            try
            {
                UiCommon.UIAMode = UIAModeType.UIA2;
                var automation = UiCommon.UIAAutomation;
                var titleName = this.First().Properties.Where(x => x.Name.ToLower() == "name" || x.Name.ToLower() == "title")?.First().Value;
                var className = this.First().Properties.Where(x => x.Name.ToLower() == "class" || x.Name.ToLower() == "classname")?.First().Value;

                Func<IntPtr, AutomationElement> func = (hWnd) =>
                {
                    WindowObject wndObj = new WindowObject(hWnd);
                    if (wndObj.ClassName.Contains(className) && wndObj.Title.Contains(titleName))
                    {
                        return automation.FromHandle(hWnd);
                    }
                    return null;
                };
                element = new DesktopWindow().MatchDesktopWindowInZorder(func).FirstOrDefault();
            }
            catch (Exception)
            {
                throw;
            }
            return element;
        }
        public List<IGuiNode> FindAll(IGuiNode fromElement = null, int maxResult = 1, int milliseconds = 3000)
        {
            List<IGuiNode> nodes = new List<IGuiNode>();
            TimeSpan timeout = TimeSpan.FromMilliseconds(milliseconds);
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            do
            {
                try
                {
                    AttachSap();
                    AutomationElement element = FindRootElement();
                    //var selectors = this.Where(x => x.Enabled == true && x.Selector == null).ToList();
                    var selectors = this.Where(x => x.Properties.Where(y => y.Name.ToLower() == "sapid").Any()).ToList();
                    string sapID = selectors.FirstOrDefault()?.Properties.Find(x => x.Name.ToLower() == "sapid")?.Value;
                    string sapExt = selectors.FirstOrDefault()?.Properties.Find(x => x.Name.ToLower() == "ext")?.Value;
                    string subId = selectors.FirstOrDefault()?.Properties.Find(x => x.Name.ToLower() == "extid")?.Value;
                    string columnName = selectors.FirstOrDefault()?.Properties.Find(x => x.Name.ToLower() == "columnname")?.Value;
                    string rowIndexText = selectors.FirstOrDefault()?.Properties.Find(x => x.Name.ToLower() == "rowindex")?.Value;
                    var component = GlobalSession.FindById(sapID); //这里获取到的如果是GuiTree。就需要特殊处理
                    this.RawElement = component;

                    if (!string.IsNullOrEmpty(columnName) && component is GuiGridView gridView)
                    {
                        if (maxResult == 1)
                        {
                            SapNode columnNode = new SapNode(component, element);
                            columnNode.Ext = $"Column({columnName})";
                            columnNode.UserDefinedID = sapID;
                            columnNode.SubId = subId;
                            columnNode.cacheSelector_o = this.cacheSelector_o;
                            nodes.Add(columnNode);
                            return nodes;
                        }

                        int totalRows = gridView.RowCount;
                        int count = 0;
                        for (int row = 0; row < totalRows; row++)
                        {
                            SapNode gridNode = new SapNode(component, element);
                            gridNode.Ext = $"Cell({row},{columnName})";
                            gridNode.UserDefinedID = sapID;
                            gridNode.SubId = subId;
                            gridNode.cacheSelector_o = this.cacheSelector_o;
                            nodes.Add(gridNode);
                            count++;
                            if (count >= maxResult) break;
                        }
                        return nodes;
                    }
                    if (!string.IsNullOrEmpty(columnName)
                        && component is GuiTableControl tableControl
                        && int.TryParse(columnName, out int tableColumnIndex))
                    {
                        if (maxResult == 1)
                        {
                            SapNode columnNode = new SapNode(component, element);
                            columnNode.Ext = $"Column({tableColumnIndex})";
                            columnNode.UserDefinedID = sapID;
                            columnNode.SubId = subId;
                            columnNode.cacheSelector_o = this.cacheSelector_o;
                            nodes.Add(columnNode);
                            return nodes;
                        }

                        int count = 0;
                        for (int row = 0; row < tableControl.RowCount; row++)
                        {
                            SapNode tableNode = new SapNode(component, element);
                            tableNode.Ext = $"Cell({row},{tableColumnIndex})";
                            tableNode.UserDefinedID = sapID;
                            tableNode.SubId = subId;
                            tableNode.cacheSelector_o = this.cacheSelector_o;
                            nodes.Add(tableNode);
                            count++;
                            if (count >= maxResult) break;
                        }
                        return nodes;
                    }
                    if (!string.IsNullOrEmpty(sapExt)
                        && component is GuiGridView extGridView
                        && GuiGridCellAction.IsGridColumn(sapExt, out string extColumnName)
                        && maxResult != 1)
                    {
                        int totalRows = extGridView.RowCount;
                        int count = 0;
                        for (int row = 0; row < totalRows; row++)
                        {
                            SapNode gridNode = new SapNode(component, element);
                            gridNode.Ext = $"Cell({row},{extColumnName})";
                            gridNode.UserDefinedID = sapID;
                            gridNode.SubId = subId;
                            gridNode.cacheSelector_o = this.cacheSelector_o;
                            nodes.Add(gridNode);
                            count++;
                            if (count >= maxResult) break;
                        }
                        return nodes;
                    }
                    if (!string.IsNullOrEmpty(sapExt)
                        && component is GuiTableControl extTableControl
                        && GuiTableCellAction.IsTableColumn(sapExt, out int extTableColumnIndex)
                        && maxResult != 1)
                    {
                        int count = 0;
                        for (int row = 0; row < extTableControl.RowCount; row++)
                        {
                            SapNode tableNode = new SapNode(component, element);
                            tableNode.Ext = $"Cell({row},{extTableColumnIndex})";
                            tableNode.UserDefinedID = sapID;
                            tableNode.SubId = subId;
                            tableNode.cacheSelector_o = this.cacheSelector_o;
                            nodes.Add(tableNode);
                            count++;
                            if (count >= maxResult) break;
                        }
                        return nodes;
                    }
                    if (!string.IsNullOrEmpty(rowIndexText)
                        && int.TryParse(rowIndexText, out int rowIndex)
                        && component is GuiGridView rowGridView)
                    {
                        if (maxResult == 1)
                        {
                            SapNode rowNode = new SapNode(component, element);
                            rowNode.Ext = $"Row({rowIndex})";
                            rowNode.UserDefinedID = sapID;
                            rowNode.SubId = subId;
                            rowNode.cacheSelector_o = this.cacheSelector_o;
                            nodes.Add(rowNode);
                            return nodes;
                        }

                        int count = 0;
                        foreach (string column in GuiGridCellAction.GetGridColumnNames(rowGridView))
                        {
                            SapNode gridNode = new SapNode(component, element);
                            gridNode.Ext = $"Cell({rowIndex},{column})";
                            gridNode.UserDefinedID = sapID;
                            gridNode.SubId = subId;
                            gridNode.cacheSelector_o = this.cacheSelector_o;
                            nodes.Add(gridNode);
                            count++;
                            if (count >= maxResult) break;
                        }
                        return nodes;
                    }
                    if (!string.IsNullOrEmpty(rowIndexText)
                        && int.TryParse(rowIndexText, out int tableRowIndex)
                        && component is GuiTableControl rowTableControl)
                    {
                        if (maxResult == 1)
                        {
                            SapNode rowNode = new SapNode(component, element);
                            rowNode.Ext = $"Row({tableRowIndex})";
                            rowNode.UserDefinedID = sapID;
                            rowNode.SubId = subId;
                            rowNode.cacheSelector_o = this.cacheSelector_o;
                            nodes.Add(rowNode);
                            return nodes;
                        }

                        int count = 0;
                        foreach (string column in GuiTableCellAction.GetTableColumnNames(rowTableControl))
                        {
                            SapNode tableNode = new SapNode(component, element);
                            tableNode.Ext = $"Cell({tableRowIndex},{column})";
                            tableNode.UserDefinedID = sapID;
                            tableNode.SubId = subId;
                            tableNode.cacheSelector_o = this.cacheSelector_o;
                            nodes.Add(tableNode);
                            count++;
                            if (count >= maxResult) break;
                        }
                        return nodes;
                    }
                    if (!string.IsNullOrEmpty(sapExt)
                        && component is GuiGridView extRowGridView
                        && GuiGridCellAction.IsGridRow(sapExt, out int extRowIndex)
                        && maxResult != 1)
                    {
                        int count = 0;
                        foreach (string column in GuiGridCellAction.GetGridColumnNames(extRowGridView))
                        {
                            SapNode gridNode = new SapNode(component, element);
                            gridNode.Ext = $"Cell({extRowIndex},{column})";
                            gridNode.UserDefinedID = sapID;
                            gridNode.SubId = subId;
                            gridNode.cacheSelector_o = this.cacheSelector_o;
                            nodes.Add(gridNode);
                            count++;
                            if (count >= maxResult) break;
                        }
                        return nodes;
                    }
                    if (!string.IsNullOrEmpty(sapExt)
                        && component is GuiTableControl extRowTableControl
                        && GuiGridCellAction.IsGridRow(sapExt, out int extTableRowIndex)
                        && maxResult != 1)
                    {
                        int count = 0;
                        foreach (string column in GuiTableCellAction.GetTableColumnNames(extRowTableControl))
                        {
                            SapNode tableNode = new SapNode(component, element);
                            tableNode.Ext = $"Cell({extTableRowIndex},{column})";
                            tableNode.UserDefinedID = sapID;
                            tableNode.SubId = subId;
                            tableNode.cacheSelector_o = this.cacheSelector_o;
                            nodes.Add(tableNode);
                            count++;
                            if (count >= maxResult) break;
                        }
                        return nodes;
                    }

                    SapNode node = new SapNode(component, element);
                    //foreach(var selector in this)
                    //{
                    //    node.Add(selector);
                    //}
                    node.Ext = sapExt;
                    node.UserDefinedID = sapID;
                    node.SubId = subId;
                    nodes.Add(node);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            while (nodes.Count == 0 && sw.Elapsed < timeout);
            return nodes;
        }
        public IGuiNode FindRelativeNode(int position, int offsetX, int offsetY)
        {
            throw new NotImplementedException();
        }
        public void Focus()
        {

        }

        public IGuiNode GetChildByIdx(int idx)
        {
            throw new NotImplementedException();
        }

        public Point GetClickablePoint()
        {
            EnsureGridElementVisible();
            if (boundingRectangle == Rectangle.Empty)
            {
                boundingRectangle = GetRegion(3);
            }
            var point = new Point(BoundingRectangle.Left + BoundingRectangle.Width / 2, BoundingRectangle.Top + BoundingRectangle.Height / 2);
            return point;
        }
        #region 对外元素常用操作
        public string GetElementCheck()
        {
            logger.LogDebug($"GetElementCheck:");
            try
            {
                EnsureRawElement();
                ISapControl control = ControlFactory.GetControl(this.RawElement, Ext);
                GuiCheckBoxAction chk = control as GuiCheckBoxAction;
                ApplicationContext cxt = new ApplicationContext() { session = GlobalSession, path = UserDefinedID };
                if (chk != null)
                {
                    return chk.GetSelected(cxt) == true ? "true" : "false";
                }
                else
                {
                    GuiRadioButtonAction chk2 = control as GuiRadioButtonAction;
                    if (chk2 != null)
                    {
                        return chk2.GetSelected(cxt) == true ? "true" : "false";
                    }
                    else
                    {
                        throw new SAPException(eErrorMessageType.Error_ElementNotOperation);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SAPException(eErrorMessageType.Error_GetElementCheckError);
            }
        }

        public string GetElementSelect(int selectType = 1)
        {
            logger.LogDebug($"GetElementSelect:selectType{selectType}");
            try
            {
                EnsureRawElement();
                if (RawElement != null)
                {
                    if (!(RawElement is GuiComboBox))
                    {
                        throw new SAPException(eErrorMessageType.Error_ElementNotOperation);
                    }
                    GuiComboBox cb = RawElement as GuiComboBox;
                    return cb.Text;
                }
                else
                {
                    throw new SAPException(eErrorMessageType.Error_NotFoundElement);
                }
            }
            catch (Exception ex)
            {
                throw new SAPException(eErrorMessageType.Error_GetSelectError, ex);
            }
        }

        public string GetElementSelectEx(string strJson)
        {
            logger.LogDebug($"Sap GetElementSelectEx param:{strJson}");
            //chromeTab.IsActive();
            int selectMode = 0, selectType = 1;

            GetElementSelectExParamModel _GetElementSelectExParam = null;
            try
            {
                _GetElementSelectExParam = JsonConvert.DeserializeObject<GetElementSelectExParamModel>(strJson);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"Sap GetElementSelectEx param Err. Json:{strJson}");
                throw new SAPException(eErrorMessageType.Error_ParamsError);
            }
            finally
            {
                if (_GetElementSelectExParam == null || _GetElementSelectExParam.selectMode == -1 || _GetElementSelectExParam.selectType == -1)
                {
                    logger.LogError($"Sap GetElementSelectEx param Err. Json:{strJson}");
                    throw new SAPException(eErrorMessageType.Error_ParamsError);
                }
                selectMode = _GetElementSelectExParam.selectMode;
                selectType = _GetElementSelectExParam.selectType;
            }

            string strContent = string.Empty;

            logger.LogDebug($"GetElementSelectEx:selectMode--{selectMode} selectType{selectType}");
            try
            {
                EnsureRawElement();
                if (RawElement != null)
                {
                    if (!(RawElement is GuiComboBox))
                    {
                        throw new SAPException(eErrorMessageType.Error_ElementNotOperation);
                    }
                    GuiComboBox cbb = RawElement as GuiComboBox;

                    if (selectMode == 0)//获取当前下拉选项
                    {
                        switch (selectType)
                        {
                            case 0:
                                int _index = -1;
                                if (cbb.CurListBoxEntry != null)
                                {
                                    _index = cbb.CurListBoxEntry.Pos - 1;
                                }
                                var IndexItem = new JsonArrayClass(_index);
                                strContent = IndexItem.ToJson();
                                break;
                            case 1:
                                string strCurrentText = cbb.Text;
                                var TextItem = new JsonArrayClass(strCurrentText);
                                strContent = TextItem.ToJson();
                                break;
                            case 2:
                                string strCurrentValue = cbb.Value;
                                var ValueItem = new JsonArrayClass(strCurrentValue);
                                strContent = ValueItem.ToJson();
                                break;
                        }
                    }
                    else //获取全部下拉选项
                    {
                        switch (selectType)
                        {
                            case 0:
                                int _length = cbb.Entries.Count;
                                var IndexItem = new JsonArrayClass(Enumerable.Range(0, _length).ToList());
                                strContent = IndexItem.ToJson();
                                break;
                            case 1:
                                List<string> TextList = new List<string>();
                                for (int i = 0; i < cbb.Entries.Count; i++)
                                {
                                    GuiComboBoxEntry element = cbb.Entries.ElementAt(i) as GuiComboBoxEntry;
                                    TextList.Add(element.Value);
                                }

                                var TextItem = new JsonArrayClass(TextList);
                                strContent = TextItem.ToJson();
                                break;
                            case 2:

                                List<string> ValueList = new List<string>();
                                for (int i = 0; i < cbb.Entries.Count; i++)
                                {
                                    GuiComboBoxEntry element = cbb.Entries.ElementAt(i) as GuiComboBoxEntry;
                                    ValueList.Add(element.Value);
                                }

                                var ValueItem = new JsonArrayClass(ValueList);
                                strContent = ValueItem.ToJson();
                                break;
                        }
                    }
                }
                else
                {
                    throw new SAPException(eErrorMessageType.Error_NotFoundElement);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"SAP GetElementSelectEx Exception");
                throw new SAPException(eErrorMessageType.Error_GetSelectError, ex);
            }

            return strContent;
        }

        public Rectangle GetRegion(int positionType)
        {
            //logger.LogDebug($"GetRegion:positionType{positionType}");
            Rectangle rc = Rectangle.Empty;
            try
            {
                EnsureRawElement();
                ApplicationContext context = new ApplicationContext() { session = GlobalSession, component = RawElement, path = UserDefinedID, ext = this.Ext, subId = this.SubId };
                rc = ControlFactory.GetLocation(context);

            }
            catch (Exception ex)
            {
                throw new SAPException(eErrorMessageType.Error_GetRegionError, ex);
            }
            return rc;
        }
        public string GetText()
        {
            logger.LogDebug($"GetText:");
            string result = string.Empty;
            try
            {
                EnsureRawElement();
                ISapControl control = ControlFactory.GetControl(RawElement, Ext);
                if (control != null)
                {
                    ApplicationContext context = new ApplicationContext() { session = GlobalSession, component = RawElement, path = UserDefinedID, ext = this.Ext, subId = this.SubId };
                    result = control.GetText(context);
                }
            }
            catch (Exception ex)
            {
                throw new SAPException(eErrorMessageType.Error_GetTextError, ex);
            }
            return result;
        }
        public void MouseClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            logger.LogDebug($"MouseClick: offsetX:{offsetX} offsetY:{offsetY}");
            if (clickParams == null)
            {
                clickParams = new UiElementClickParams();
            }
            try
            {
                if (clickParams.operateType == 0)   //后台操作
                {
                    EnsureRawElement();
                    SapViewportPreparer.TryEnsureVisible(RawElement);
                    ISapControl control = ControlFactory.GetControl(RawElement, Ext);
                    GuiButtonAction btn = control as GuiButtonAction;
                    ApplicationContext context = new ApplicationContext() { session = GlobalSession, component = RawElement, path = UserDefinedID, ext = this.Ext, subId = this.SubId };
                    if (btn != null)
                    {
                        GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => btn.Execute(context));
                    }
                    else if (control is GuiGridCellAction || control is GuiGridColumnAction || control is GuiGridRowAction
                        || control is GuiTableCellAction || control is GuiTableColumnAction || control is GuiTableRowAction)
                    {
                        GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => control.Execute(context));
                    }
                    else if (RawElement is ISapLabelTarget sapLabel)
                    {
                        sapLabel.SetFocus();
                    }
                    else if (RawElement is ISapCTextField sapCtext)
                    {
                        sapCtext.SetFocus();
                    }
                    else if (RawElement is ISapTextFieldTarget sapText)
                    {
                        sapText.SetFocus();
                    }
                    else if (RawElement is GuiTree guiTree)
                    {
                        IntoView();
                        // ClickLink 需要目标节点已被 SelectNode 选中才能触发动作，
                        // 但 IntoView 只做展开+滚动不选中，这里补上选中步骤
                        string nodeKey = guiTree.GetNodeKeyByNodeTextPath(this.Ext, this.SubId);
                        if (!string.IsNullOrEmpty(nodeKey))
                            guiTree.SelectNode(nodeKey);
                        GuiTreeAction guiTreeAction = new GuiTreeAction();
                        guiTreeAction.Click(context, clickParams.RPAMousePresses);
                    }
                    else
                    {
                        throw new SAPException(eErrorMessageType.Error_NotSupport);
                    }
                }
                else
                {
                    EnsureRawElement();
                    IntoView();
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    Thread.Sleep(200);
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Left));
                }

            }
            catch (Exception ex)
            {
                throw new SAPException(eErrorMessageType.Error_ClickError, ex);
            }
        }
        public void MouseMove(int cursorPos, int offsetX = 0, int offsetY = 0, bool isUsePerson = false)
        {
            EnsureGridElementVisible();
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
            II.RPA.Plugin.GUI.Commons.MouseMotions.MouseMotionHelper.PersonMoveMouse(isUsePerson, point);
        }

        private void EnsureGridElementVisible()
        {
            if (string.IsNullOrEmpty(Ext))
            {
                return;
            }

            EnsureRawElement();
            ISapControl control = ControlFactory.GetControl(RawElement, Ext);
            if (control is GuiGridCellAction gridCell)
            {
                gridCell.EnsureVisible();
                boundingRectangle = Rectangle.Empty;
            }
            else if (control is GuiGridRowAction gridRow)
            {
                gridRow.EnsureVisible();
                boundingRectangle = Rectangle.Empty;
            }
            else if (control is GuiGridColumnAction gridColumn)
            {
                gridColumn.EnsureVisible();
                boundingRectangle = Rectangle.Empty;
            }
            else if (control is GuiTableCellAction tableCell)
            {
                tableCell.EnsureVisible();
                boundingRectangle = Rectangle.Empty;
            }
            else if (control is GuiTableRowAction tableRow)
            {
                tableRow.EnsureVisible();
                boundingRectangle = Rectangle.Empty;
            }
            else if (control is GuiTableColumnAction tableColumn)
            {
                tableColumn.EnsureVisible();
                boundingRectangle = Rectangle.Empty;
            }
        }
        public void MouseDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            logger.LogDebug($"MouseDoubleClick:offsetX{offsetX} offsetY:{offsetY}");
            if (clickParams == null)
            {
                clickParams = new UiElementClickParams();
            }
            try
            {
                if (clickParams.operateType == 0)   //后台操作
                {
                    EnsureRawElement();
                    SapViewportPreparer.TryEnsureVisible(RawElement);
                    IntoView();
                    ISapControl control = ControlFactory.GetControl(RawElement, Ext);
                    GuiButtonAction btn = control as GuiButtonAction;
                    if (btn != null)
                    {
                        ApplicationContext context = new ApplicationContext() { session = GlobalSession, path = UserDefinedID, component = RawElement, ext = this.Ext, subId = this.SubId };

                        GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
                        {
                            btn.Execute(context);
                            btn.Execute(context);  //双击执行两次
                        });
                    }
                    else if (control is GuiGridCellAction gridCell)
                    {
                        GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => gridCell.DoubleClick());
                    }
                    else if (control is GuiTableCellAction tableCell)
                    {
                        ApplicationContext context = new ApplicationContext() { session = GlobalSession, path = UserDefinedID, component = RawElement, ext = this.Ext, subId = this.SubId };
                        GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => tableCell.DoubleClick(context));
                    }
                    else if (RawElement is GuiTree guiTree)
                    {
                        IntoView();
                        // ClickLink 需要节点已被选中才能触发，DoubleClick 底層也是走 ClickLink
                        string nodeKey = guiTree.GetNodeKeyByNodeTextPath(this.Ext, this.SubId);
                        if (!string.IsNullOrEmpty(nodeKey))
                            guiTree.SelectNode(nodeKey);
                        ApplicationContext context = new ApplicationContext() { session = GlobalSession, path = UserDefinedID, component = RawElement, ext = this.Ext, subId = this.SubId };
                        GuiTreeAction guiTreeAction = new GuiTreeAction();
                        guiTreeAction.Click(context, RPAMousePresses.DoubleClick);
                    }
                    else if (RawElement is GuiGridView gridView)
                    {
                        if (!string.IsNullOrEmpty(this.Ext) && GuiGridCellAction.IsGridCell(this.Ext, out int row, out string col))
                        {
                            gridView.DoubleClick(row, col);
                        }
                        else
                        {
                            throw new SAPException(eErrorMessageType.Error_NotSupport);
                        }
                    }
                    else if (RawElement is ISapLabelTarget || RawElement is ISapCTextField || RawElement is ISapTextFieldTarget)
                    {
                        // 文本框/Label 不支持双击，做两次单击
                        ((GuiVComponent)RawElement).SetFocus();
                        Thread.Sleep(100);
                        ((GuiVComponent)RawElement).SetFocus();
                    }
                    else
                    {
                        throw new SAPException(eErrorMessageType.Error_NotSupport);
                    }
                }
                else
                {
                    EnsureRawElement();
                    IntoView();
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.LeftDoubleClick());
                }
            }
            catch (Exception ex)
            {
                throw new SAPException(eErrorMessageType.Error_DoubleClickError, ex);
            }
        }
        public void MouseHover(UiElementHoverParams hoverParams = null)
        {
            logger.LogDebug($"MouseHover:");
            if (hoverParams == null)
            {
                hoverParams = new UiElementHoverParams();
            }
            var clickablePoint = GetClickablePoint();
            if (hoverParams.moveMouse)
            {
                Mouse.Position = clickablePoint;
            }
            else
            {
                Mouse.Position = clickablePoint;
            }
        }
        public void MouseRightClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            logger.LogDebug($"MouseRightClick:");
            if (clickParams == null)
            {
                clickParams = new UiElementClickParams();
            }
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Right));
        }
        public void MouseRightDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            logger.LogDebug($"MouseRightDoubleClick:");
            if (clickParams == null)
            {
                clickParams = new UiElementClickParams();
            }
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.RightDoubleClick());
        }
        public void SetElementCheck(int check)
        {
            logger.LogDebug($"SetElementCheck:check{check}");
            try
            {
                EnsureRawElement();
                ISapControl control = ControlFactory.GetControl(RawElement, Ext);
                GuiCheckBoxAction chk = control as GuiCheckBoxAction;

                ApplicationContext cxt = new ApplicationContext() { session = GlobalSession, path = UserDefinedID };

                if (chk == null)
                {
                    GuiRadioButtonAction chk2 = control as GuiRadioButtonAction;
                    if (chk2 != null)
                    {
                        chk2.SetSelected(cxt, check > 0);
                    }
                    else
                    {
                        throw new SAPException(eErrorMessageType.Error_ElementNotOperation);
                    }
                }
                else
                {
                    chk.SetSelected(cxt, check > 0);
                }
            }
            catch (Exception ex)
            {
                throw new SAPException(eErrorMessageType.Error_SetElementCheckError, ex);
            }
        }
        public void SetElementSelect(int selectType, string setValue)
        {
            SetElementSelect(selectType, setValue, 0);
        }
        public void SetElementSelect(int selectType, string setValue, int matchMode = 0)
        {
            logger.LogDebug($"SetElementSelect:selectType{selectType} setValue:{setValue}");
            try
            {
                AttachSap();
                GuiComboBox chk = RawElement as GuiComboBox;
                if (chk == null)
                {
                    throw new SAPException(eErrorMessageType.Error_ElementNotOperation);
                }
                if (selectType == 0)
                {
                    int idx;
                    bool p = int.TryParse(setValue, out idx);
                    if (p == false)
                    {
                        throw new SAPException(eErrorMessageType.Error_SelectValueIsNotValid);
                    }
                    if (idx >= chk.Entries.Count)
                    {
                        throw new SAPException(eErrorMessageType.Error_IndexOverflow);
                    }
                    string matchValue = null;
                    for (int i = 0; i < chk.Entries.Count; i++)
                    {
                        if (i == idx)
                        {
                            GuiComboBoxEntry gbe = chk.Entries.ElementAt(i) as GuiComboBoxEntry;
                            matchValue = gbe.Value;
                            break;
                        }
                    }
                    chk.Value = matchValue;
                }
                else
                {
                    bool match = false;
                    string itemName = string.Empty;

                    for (int j = 0; j < chk.Entries.Count; j++)
                    {
                        GuiComboBoxEntry gbe = chk.Entries.ElementAt(j) as GuiComboBoxEntry;
                        if (matchMode == 0)
                        {
                            if (gbe.Value == setValue)
                            {
                                match = true;
                                break;
                            }
                        }
                        else if (matchMode == 1)
                        {
                            if (gbe.Value.Contains(setValue))
                            {
                                match = true;
                                break;
                            }
                        }
                        else if (matchMode == 2)
                        {
                            if (Regex.IsMatch(gbe.Value, setValue))
                            {
                                match = true;
                                break;
                            }
                        }
                    }
                    if (!match)
                    {
                        throw new SAPException(eErrorMessageType.Error_SetSelectError);
                    }
                    else
                    {
                        chk.Value = setValue;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SAPException(eErrorMessageType.Error_SetSelectError, ex);
            }
        }
        public void SetForeground()
        {
            logger.LogDebug($"SetForeground:");
            if (this.TragetWindow != null)
            {
                UiCommon.ForceShow(TragetWindow.Handle);
                return;
            }
            AutomationElement topElement = FindRootElement();
            if (topElement != null)
            {
                if (topElement.Properties.NativeWindowHandle.IsSupported)
                {
                    UiCommon.ForceShow(topElement.Properties.NativeWindowHandle);
                }
            }
        }
        public void SetText(string text, int isReplace, bool isSimulate)
        {
            logger.LogDebug($"SetText:text:{text} isReplace:{isReplace} isSimulate:{isSimulate}");
            try
            {
                EnsureRawElement();
                ISapControl control = ControlFactory.GetControl(RawElement, Ext);
                if (control != null)
                {
                    ApplicationContext context = new ApplicationContext() { session = GlobalSession, path = UserDefinedID, component = RawElement, ext = this.Ext, subId = this.SubId };
                    control.SetText(context, text);
                }
            }
            catch (Exception ex)
            {
                throw new SAPException(eErrorMessageType.Error_SetTextError, ex);
            }
        }

        public string GetEnableClick()
        {
            throw new NotImplementedException();
        }
        #endregion

        public void SetElementAttribute(string name, string value)
        {

        }
        public string GetElementAttribute(string name, string apiVersion)
        {
            string attrName = (name ?? string.Empty).ToLower();
            switch (attrName)
            {
                case "text":
                case "innertext":
                case "value":
                case "gettext":
                    return GetText();
                case "name":
                    return Name ?? string.Empty;
                case "type":
                case "controltype":
                    return ControlType ?? string.Empty;
                case "sapid":
                case "id":
                case "userdefinedid":
                    return UserDefinedID ?? string.Empty;
                case "ext":
                    return Ext ?? string.Empty;
                case "selector":
                    return NodeSelector ?? string.Empty;
                case "processname":
                    return ProcessName ?? string.Empty;
                case "processfullpath":
                    return ProcessFullPath ?? string.Empty;
                case "bounding":
                case "region":
                    var rect = GetRegion(3);
                    return JsonHelper.JsonByNewtonJson(new
                    {
                        x = rect.X.ToString(),
                        y = rect.Y.ToString(),
                        width = rect.Width.ToString(),
                        height = rect.Height.ToString()
                    });
                default:
                    return string.Empty;
            }
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

        public Bitmap ElementToBitmap()
        {
            throw new NotImplementedException();
        }
        public string GetTableData(string input)
        {
            logger.LogDebug($"GetTableData:input{input}");
            ParamTableModel tm = null;
            string result = string.Empty;
            try
            {
                tm = JsonHelper.JsonDesByNewtonJson<ParamTableModel>(input);
            }
            catch (Exception ex)
            {
                logger.LogError("入参格式错误:" + input);
                throw new SAPException(eErrorMessageType.Error_ParamsError, ex);
            }
            EnsureRawElement();
            //ISapControl control = ControlFactory.GetControl(component);
            ITable table = ControlFactory.GetTable(RawElement);
            if (table != null)
            {
                return JsonHelper.JsonByNewtonJson(table.GetTableData(UserDefinedID, tm));
            }
            else
            {
                throw new Exception("拾取的不是SAP表格元素不支持获取表格操作".I18n("controlType_SapNode_AC71AC15"));
            }
        }

        public void CheckInputForClickTable(SapClickModel input, int rowCount, int columnCount)
        {
            bool result = false;
            int rowIndex = 0;
            result = int.TryParse(input.RowNo, out rowIndex);
            if (result == false)
            {
                throw new Exception("行号入参错误，必须为整数".I18n("controlType_SapNode_7FC159D5"));
            }
            else
            {
                if (rowIndex <= 0)
                {
                    throw new Exception("行号必须为大于零的整数".I18n("controlType_SapNode_CA8F3EA1"));
                }
                if (rowIndex > rowCount)
                {
                    throw new Exception("行号越界".I18n("controlType_SapNode_A9084800"));
                }
            }
            int columnIndex = 0;
            result = int.TryParse(input.ColumnNo, out columnIndex);
            if (result == false)
            {
                throw new Exception("列号入参错误，必须为整数".I18n("controlType_SapNode_ADE0184C"));
            }
            else
            {
                if (columnIndex <= 0)
                {
                    throw new Exception("列号必须为大于零的整数".I18n("controlType_SapNode_607635E"));
                }
                if (columnIndex > columnCount)
                {
                    throw new Exception("列号越界".I18n("controlType_SapNode_2C69D075"));
                }
            }
        }

        public string ClickTableCell(string input)
        {
            logger.LogDebug($"ClickTableCell:");
            SapClickModel tm = null;
            string result = string.Empty;
            try
            {
                tm = JsonHelper.JsonDesByNewtonJson<SapClickModel>(input);
            }
            catch (Exception ex)
            {
                logger.LogError("入参格式错误:" + input);
                throw new SAPException(eErrorMessageType.Error_ParamsError, ex);
            }
            try
            {
                EnsureRawElement();
                GuiGridView gridView = RawElement as GuiGridView;
                if (gridView != null)
                {
                    CheckInputForClickTable(tm, gridView.RowCount, gridView.ColumnCount);
                    int.TryParse(tm.RowNo, out int rowIndex);
                    int.TryParse(tm.ColumnNo, out int columnIndex);
                    if (rowIndex > gridView.RowCount)
                    {
                        logger.LogError("row index is out of table range");
                        throw new SAPException(eErrorMessageType.Error_ParamsError);

                    }
                    if (columnIndex > gridView.ColumnCount)
                    {
                        logger.LogError("column index is out of table range");
                        throw new SAPException(eErrorMessageType.Error_ParamsError);

                    }
                    if (tm.ClickType == 0)
                    {
                        gridView.Click(int.Parse(tm.RowNo) - 1, gridView.ColumnOrder[columnIndex - 1]);
                    }
                    if (tm.ClickType == 1)
                    {
                        gridView.DoubleClick(int.Parse(tm.RowNo) - 1, gridView.ColumnOrder[columnIndex - 1]);
                    }
                }
                else
                {
                    throw new Exception("拾取的不是SAP表格元素不支持获取表格操作".I18n("controlType_SapNode_AC71AC15"));
                }
            }
            catch (Exception ex)
            {
                SAPException exception = new SAPException(eErrorMessageType.Error_GetTableDataError, ex);
                exception.CustomMessage = ex.Message;
                throw exception;
            }
            return result;
        }

        public void IntoView()
        {
            try
            {
                AttachSap();
                if (RawElement == null)
                    RawElement = GlobalSession.FindById(UserDefinedID);
                if (RawElement == null) return;

                // GuiTree：展开祖先节点、滚动到目标（但不选中，选中由后续点击操作负责）
                if (RawElement is GuiTree guiTree)
                {
                    string nodeKey = guiTree.GetNodeKeyByNodeTextPath(this.Ext, this.SubId);
                    if (!string.IsNullOrEmpty(nodeKey))
                    {
                        guiTree.BringNodeIntoViewByNodeKey($"{nodeKey}?{this.SubId}", false);
                        // 额外延迟让 SAP GUI 完成重绘
                        Thread.Sleep(200);
                    }
                    return;
                }

                // GuiGridView：滚动到指定单元格
                if (RawElement is GuiGridView gridView)
                {
                    if (!string.IsNullOrEmpty(this.Ext) && GuiGridCellAction.IsGridCell(this.Ext, out int row, out string col))
                    {
                        SapViewportPreparer.TryEnsureGridCellVisible(gridView, row, col);
                    }
                    return;
                }

                // GuiTableControl: scroll to the target row for cell/row/column pseudo nodes.
                if (RawElement is GuiTableControl)
                {
                    ISapControl control = ControlFactory.GetControl(RawElement, Ext);
                    if (control is GuiTableCellAction tableCell)
                    {
                        tableCell.EnsureVisible();
                        boundingRectangle = Rectangle.Empty;
                    }
                    else if (control is GuiTableRowAction tableRow)
                    {
                        tableRow.EnsureVisible();
                        boundingRectangle = Rectangle.Empty;
                    }
                    else if (control is GuiTableColumnAction tableColumn)
                    {
                        tableColumn.EnsureVisible();
                        boundingRectangle = Rectangle.Empty;
                    }
                    return;
                }

                // 通用 GuiVComponent：SetFocus 触发 SAP 自动滚动
                if (RawElement is GuiVComponent vComp)
                {
                    vComp.SetFocus();
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, "SapNode IntoView failed");
            }
        }

        public void MouseThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            logger.LogDebug($"MouseThreeClick: offsetX:{offsetX} offsetY:{offsetY}");
            if (clickParams == null)
            {
                clickParams = new UiElementClickParams();
            }
            try
            {
                if (clickParams.operateType == 0)   //后台操作
                {
                    EnsureRawElement();
                    ISapControl control = ControlFactory.GetControl(this.RawElement, Ext);
                    GuiButtonAction btn = control as GuiButtonAction;
                    if (btn != null)
                    {
                        ApplicationContext context = new ApplicationContext() { session = GlobalSession, component = RawElement, path = UserDefinedID, ext = this.Ext, subId = this.SubId };
                        GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
                        {
                            btn.Execute(context);
                            btn.Execute(context);
                            btn.Execute(context);
                        });
                    }
                    else
                    {
                        throw new SAPException(eErrorMessageType.Error_NotFoundElement);
                    }
                }
                else
                {
                    EnsureRawElement();
                    IntoView();
                    MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                    GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
                    {
                        Mouse.LeftDoubleClick();
                        Mouse.LeftClick();
                    });
                }

            }
            catch (Exception ex)
            {
                throw new SAPException(eErrorMessageType.Error_ClickError, ex);
            }
        }

        public void MouseRightThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            logger.LogDebug($"MouseRightThreeClick:");
            if (clickParams == null)
            {
                clickParams = new UiElementClickParams();
            }
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
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
            EnsureRawElement();
            ISapControl control = ControlFactory.GetControl(this.RawElement, Ext);
            ApplicationContext context = null;
            if (control != null)
            {
                context = new ApplicationContext() { session = GlobalSession, path = UserDefinedID, component = this.RawElement, ext = this.Ext, subId = this.SubId };
            }
            else
            {
                throw new SAPException(eErrorMessageType.Error_NotFoundElement);
            }

            if (!((control is GuiGridCellAction || control is GuiTableCellAction) && param.inputType == 0))
            {
                UiElementClickParams uiElementClickParams = new UiElementClickParams();
                uiElementClickParams.operateType = 1;
                uiElementClickParams.cursorPos = param.ElementPosition;
                MouseClick(param.OffsetX, param.OffsetY, uiElementClickParams);
                Thread.Sleep(param.FocusTimeout);
            }
            var replace = param.isReplace == 0 ? true : false;

            if (param.inputType == 0)
            {
                control.SetText(context, param.text);
            }
            else
            {
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
                    UiCommon.ClipboardPaste(param.text, param.isReplace, "");
                }
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
        public void SetText(string text, int isReplace, int inputType, int interval = 0)
        {
            logger.LogDebug($"SetText:text:{text} isReplace:{isReplace} isSimulate:{inputType}");
            try
            {

                EnsureRawElement();
                ISapControl control = ControlFactory.GetControl(RawElement, Ext);

                if (control != null)
                {
                    ApplicationContext context = new ApplicationContext() { session = GlobalSession, component = RawElement, path = UserDefinedID, ext = this.Ext, subId = this.SubId };
                    //剪切板文本输入
                    if (inputType == 2)
                    {
                        string value = string.Empty;//control.GetText(context);
                        MouseMove(0, 0, 0);
                        Mouse.LeftClick();
                        UiCommon.ClipboardPaste(text, isReplace, value);
                    }
                    else if (inputType == 1)
                    {
                        var replace = isReplace == 0 ? true : false;

                        var rectangle = ControlFactory.GetLocation(context);

                        Mouse.LeftClick(new Point(rectangle.Location.X + rectangle.Width / 2, rectangle.Location.Y + rectangle.Height / 2));
                        SetTextEx(replace, interval, text);
                    }
                    else
                    {
                        control.SetText(context, text);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SAPException(eErrorMessageType.Error_SetTextError, ex);
            }
        }


        #region 新方式处理模拟输入
        public void SetTextEx(bool replace, int interval, string text)
        {
            if (replace)
            {
                AutoItHelper.ClearWords();
            }
            else
            {
                AutoItHelper.MoveCursorToEnd();
            }
            Thread.Sleep(200);
            string result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "GetState"), "{}");
            var ddd = JObject.Parse(result)["info"];
            var state = JObject.Parse(ddd.ToString());
            if (state.Value<string>("state").Equals("中文"))
            {
                JObject j = new JObject();
                j.Add("state", "en_us");
                result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "SetState"), j.ToString());
            }
            if (interval > 0)
            {
                AutoItHelper.Send(text, mode: 1, interval);
            }
            else
            {
                AutoItHelper.Send(text, mode: 1, 50);
            }
        }
        #endregion

        #region 数据采集
        private static bool _isTable = false;
        public bool IsTable
        {
            get
            {
                EnsureRawElement();
                //ISapControl control = ControlFactory.GetControl(component);
                ITable table = ControlFactory.GetTable(RawElement);
                if (table != null)
                {
                    return true;
                }
                throw new RPAException(eErrorMessageType.Error_DataCollection_GUINoSupport);
            }
        }
        private string tableNodeSelector;
        public string TableNodeSelector
        {
            get
            {
                if (string.IsNullOrEmpty(cacheSelector))
                {
                    cacheSelector = GetSelector();
                    var processId = RootElement.Properties.ProcessId.Value;
                    var path = ExtractViewData.GetMainModuleFilepath(processId, out var processName);
                    cacheSelector = ExtractViewData.AddCommonProps(cacheSelector, eGuiType.SAP, path, processName: processName);
                }
                return cacheSelector;
            }
        }

        public eGuiType GuiType => eGuiType.SAP;

        public bool IsShow
        {
            get
            {
                return true;
            }
        }

        public List<SelectorProps> MatchNextButtonSelector(ParamDoElementEditorModel paramModel)
        {
            return new List<SelectorProps>();
        }

        public List<SelectorProps> AutoBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
        {
            List<SelectorProps> originalSelectorPropsList = paramModel.OriginalSelector;
            if (originalSelectorPropsList == null || originalSelectorPropsList.Count == 0)
                return new List<SelectorProps>();

            var origSapNode = originalSelectorPropsList.LastOrDefault(x => x.Name == "Sap");
            if (origSapNode == null)
                return originalSelectorPropsList;

            var origSapId = origSapNode.Props?.Find(x => x.Name.ToLower() == "sapid");
            if (origSapId == null)
                return originalSelectorPropsList;

            var origExt = origSapNode.Props?.Find(x => x.Name.ToLower() == "ext");
            if (origExt == null)
                return originalSelectorPropsList;

            string columnName = null;
            if (GuiGridCellAction.IsGridCell(origExt.Value, out int row, out string cellColumnName))
            {
                columnName = cellColumnName;
            }
            else if (GuiGridCellAction.IsGridColumn(origExt.Value, out string gridColumnName))
            {
                columnName = gridColumnName;
            }

            if (string.IsNullOrEmpty(columnName))
                return originalSelectorPropsList;

            var result = new List<SelectorProps>();
            var origWindowNode = originalSelectorPropsList.FirstOrDefault(x => x.Name != "Sap");
            if (origWindowNode != null)
            {
                result.Add(new SelectorProps
                {
                    Name = origWindowNode.Name,
                    Type = origWindowNode.Type,
                    Accurate = origWindowNode.Accurate,
                    Props = origWindowNode.Props?.Select(p => new Props
                    {
                        Name = p.Name,
                        Value = p.Value,
                        Pattern = p.Pattern,
                        Accurate = p.Accurate
                    }).ToList()
                });
            }
            result.Add(new SelectorProps
            {
                Name = "Sap",
                Accurate = "true",
                Props = new List<Props>
                {
                    new Props
                    {
                        Name = "sapId",
                        Value = origSapId.Value,
                        Pattern = "equal",
                        Accurate = "true"
                    },
                    new Props
                    {
                        Name = "columnName",
                        Value = columnName,
                        Pattern = "equal",
                        Accurate = "true"
                    }
                }
            });
            return result;
        }

        public List<SelectorProps> CompareBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
        {
            List<SelectorProps> originalSelectorPropsList = paramModel.OriginalSelector;
            List<SelectorProps> similarSelectorPropsList = paramModel.CompareOriginalSelector;

            if (originalSelectorPropsList == null || similarSelectorPropsList == null)
                return new List<SelectorProps>();
            if (originalSelectorPropsList.Count == 0 || similarSelectorPropsList.Count == 0)
                return new List<SelectorProps>();

            if (originalSelectorPropsList.Count != similarSelectorPropsList.Count)
                return new List<SelectorProps>();

            var result = CloneSelectorProps(originalSelectorPropsList);

            for (int i = 0; i < originalSelectorPropsList.Count; i++)
            {
                var originalNode = originalSelectorPropsList[i];
                var similarNode = similarSelectorPropsList[i];
                if (!IsSameSelectorNode(originalNode, similarNode))
                    return new List<SelectorProps>();

                var resultNode = result[i];
                resultNode.Props = originalNode.Props?
                    .Where(originalProp =>
                    {
                        var similarProp = FindProp(similarNode.Props, originalProp.Name);
                        return similarProp != null && originalProp.Value == similarProp.Value;
                    })
                    .Select(CloneProp)
                    .ToList() ?? new List<Props>();
            }

            var origSapNode = originalSelectorPropsList.LastOrDefault(x => x.Name == "Sap");
            var simSapNode = similarSelectorPropsList.LastOrDefault(x => x.Name == "Sap");
            var resultSapNode = result.LastOrDefault(x => x.Name == "Sap");

            if (origSapNode == null || simSapNode == null || resultSapNode == null)
                return new List<SelectorProps>();

            var origSapId = FindProp(origSapNode.Props, "sapId");
            var simSapId = FindProp(simSapNode.Props, "sapId");

            if (origSapId == null || simSapId == null)
                return new List<SelectorProps>();
            if (origSapId.Value != simSapId.Value)
                return new List<SelectorProps>();

            var origExt = FindProp(origSapNode.Props, "ext");
            var simExt = FindProp(simSapNode.Props, "ext");

            if (origExt != null && simExt != null && origExt.Value != simExt.Value)
            {
                if (!GuiGridCellAction.IsGridCell(origExt.Value, out int row1, out string col1)
                    || !GuiGridCellAction.IsGridCell(simExt.Value, out int row2, out string col2))
                {
                    return new List<SelectorProps>();
                }

                bool isSameColumn = col1 == col2 && row1 != row2;
                bool isSameRow = row1 == row2 && col1 != col2;
                if (!isSameColumn && !isSameRow)
                {
                    return new List<SelectorProps>();
                }

                string wildcardPropName = isSameColumn ? "columnName" : "rowIndex";
                string wildcardPropValue = isSameColumn ? col1 : row1.ToString();
                resultSapNode.Props.RemoveAll(p =>
                    p.Name.EqualIgnoreCase("ext")
                    || p.Name.EqualIgnoreCase("columnName")
                    || p.Name.EqualIgnoreCase("rowIndex"));
                resultSapNode.Props.Add(new Props
                {
                    Name = wildcardPropName,
                    Value = wildcardPropValue,
                    Pattern = "equal",
                    Accurate = "true"
                });
            }

            if (resultSapNode.Props == null || !resultSapNode.Props.Any(p => p.Name.EqualIgnoreCase("sapId")))
                return new List<SelectorProps>();

            return result;
        }

        private static bool IsSameSelectorNode(SelectorProps originalNode, SelectorProps similarNode)
        {
            if (originalNode == null || similarNode == null)
                return false;
            return string.Equals(originalNode.Name, similarNode.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(originalNode.Type ?? string.Empty, similarNode.Type ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static Props FindProp(List<Props> props, string propName)
        {
            return props?.FirstOrDefault(p => p.Name.EqualIgnoreCase(propName));
        }

        private static List<SelectorProps> CloneSelectorProps(List<SelectorProps> selectorProps)
        {
            return selectorProps.Select(p => new SelectorProps
            {
                Name = p.Name,
                Type = p.Type,
                Accurate = p.Accurate,
                Props = p.Props?.Select(CloneProp).ToList() ?? new List<Props>()
            }).ToList();
        }

        private static Props CloneProp(Props prop)
        {
            return new Props
            {
                Name = prop.Name,
                Value = prop.Value,
                Pattern = prop.Pattern,
                Accurate = prop.Accurate,
                TextValue = prop.TextValue,
                ExpressionValue = prop.ExpressionValue
            };
        }
        private List<SelectorProps> CompareSelectorAndRemoveDiffProps(List<SelectorProps> originalSelectorPropsList, List<SelectorProps> similarSelectorPropsList)
        {
            for (int i = 0; i < originalSelectorPropsList.Count; i++)
            {
                //去异留同
                originalSelectorPropsList[i].Props = originalSelectorPropsList[i].Props.Where(osp => similarSelectorPropsList[i].Props.Any(ssp => ssp.Name == osp.Name && ssp.Value == osp.Value)).ToList();
            }
            return originalSelectorPropsList;
        }

        private List<SelectorProps> BuilidCollectDataSelector(List<SelectorProps> originalSelector, List<string> structureSimilarSelectors)
        {
            return new List<SelectorProps>();
        }
        public string GetNextElementBuildSelector(List<string> cssSelectorList)
        {
            try
            {
                return null;
            }
            catch (Exception e)
            {
                throw new SAPException(eErrorMessageType.Error_NotFoundElement);
            }
        }
        public static void ScrollIntoView(string srcoll)
        {
        }

        public string GetHtml()
        {
            throw new NotImplementedException();
        }
        #endregion
        public string GetSubElements(ParamGetSubElements param)
        {
            //未单独实现的类型，按照老的逻辑返回子元素
            List<string> selectors = new List<string>();
            List<string> nodeTexts = new List<string>();
            try
            {
                if (this.RawElement is GuiTree guiTree)
                {
                    if (!string.IsNullOrEmpty(this.Ext))
                    {
                        List<string> nodeKeys = new List<string>();
                        string currentNodeKey = guiTree.GetNodeKeyByNodeTextPath(this.Ext, this.SubId);

                        GuiCollection objs = guiTree.GetSubNodesCol(currentNodeKey) as GuiCollection;
                        foreach (var item in objs)
                        {
                            nodeTexts.Add($"{this.Ext}/{guiTree.GetTextByItemOrNode(item.ToString())}");
                        }
                    }
                    selectors = ReBuildSelector(nodeTexts);
                }
                else if (this.RawElement is GuiGridView gridView)
                {
                    if (!string.IsNullOrEmpty(this.Ext) && GuiGridCellAction.IsGridCell(this.Ext, out int cellRow, out string cellCol))
                    {
                        for (int row = 0; row < gridView.RowCount; row++)
                        {
                            nodeTexts.Add($"Cell({row},{cellCol})");
                        }
                    }
                    else if (!string.IsNullOrEmpty(this.Ext) && GuiGridCellAction.IsGridColumn(this.Ext, out string colName))
                    {
                        for (int row = 0; row < gridView.RowCount; row++)
                        {
                            nodeTexts.Add($"Cell({row},{colName})");
                        }
                    }
                    else if (!string.IsNullOrEmpty(this.Ext) && GuiGridCellAction.IsGridRow(this.Ext, out int rowIndex))
                    {
                        foreach (string cName in GuiGridCellAction.GetGridColumnNames(gridView))
                        {
                            nodeTexts.Add($"Cell({rowIndex},{cName})");
                        }
                    }
                    else
                    {
                        foreach (string cName in GuiGridCellAction.GetGridColumnNames(gridView))
                        {
                            nodeTexts.Add($"Column({cName})");
                        }
                    }
                    selectors = ReBuildSelector(nodeTexts);
                }
                else if (this.RawElement is GuiTableControl tableControl)
                {
                    if (!string.IsNullOrEmpty(this.Ext) && GuiTableCellAction.IsTableCell(this.Ext, out int tableCellRow, out int tableCellCol))
                    {
                        for (int row = 0; row < tableControl.RowCount; row++)
                        {
                            nodeTexts.Add($"Cell({row},{tableCellCol})");
                        }
                    }
                    else if (!string.IsNullOrEmpty(this.Ext) && GuiTableCellAction.IsTableColumn(this.Ext, out int tableCol))
                    {
                        for (int row = 0; row < tableControl.RowCount; row++)
                        {
                            nodeTexts.Add($"Cell({row},{tableCol})");
                        }
                    }
                    else if (!string.IsNullOrEmpty(this.Ext) && GuiGridCellAction.IsGridRow(this.Ext, out int tableRowIndex))
                    {
                        foreach (string cName in GuiTableCellAction.GetTableColumnNames(tableControl))
                        {
                            nodeTexts.Add($"Cell({tableRowIndex},{cName})");
                        }
                    }
                    else
                    {
                        foreach (string cName in GuiTableCellAction.GetTableColumnNames(tableControl))
                        {
                            nodeTexts.Add($"Column({cName})");
                        }
                    }
                    selectors = ReBuildSelector(nodeTexts);
                }

            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
            return selectors.ToJson();
        }

        public string GetParent(ParamGetParent param)
        {
            List<string> nodeTexts = new List<string>();
            try
            {
                if (this.RawElement is GuiTree guiTree)
                {
                    if (!string.IsNullOrEmpty(this.Ext))
                    {
                        List<string> nodeKeys = new List<string>();
                        string currentNodeKey = guiTree.GetNodeKeyByNodeTextPath(this.Ext, this.SubId);

                        string parentNode = guiTree.GetParent(currentNodeKey);

                        string parentPath = guiTree.GetNodeTextPathByKey($"{parentNode}?{this.SubId}");
                        if (parentPath.Contains("extId"))
                        {
                            int index = parentPath.IndexOf("extId");
                            parentPath = parentPath.Substring(0, index - 1);
                        }
                        nodeTexts = ReBuildSelector(new List<string>() { parentPath }); //替换节点
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
            return nodeTexts.ToJson();
        }

        public string GetSiblings(ParamGetSiblings param)
        {
            List<string> nodeTexts = new List<string>();
            try
            {
                if (this.RawElement is GuiTree guiTree)
                {
                    if (!string.IsNullOrEmpty(this.Ext))
                    {
                        List<string> nodeKeys = new List<string>();
                        string currentNodeKey = guiTree.GetNodeKeyByNodeTextPath(this.Ext, this.SubId);

                        string parentNode = guiTree.GetParent(currentNodeKey);  //获取父元素Key

                        string parentPath = guiTree.GetNodeTextPathByKey($"{parentNode}?{this.SubId}");  //获取父节点完整路径

                        if (parentPath.Contains("extId"))
                        {
                            int index = parentPath.IndexOf("extId");
                            parentPath = parentPath.Substring(0, index - 1);
                        }

                        GuiCollection objs = guiTree.GetSubNodesCol(parentNode) as GuiCollection;   //遍历父元素的子元素

                        foreach (var item in objs)
                        {
                            if (item.ToString() == currentNodeKey)  //如果是本元素，就跳过
                            {
                                continue;
                            }

                            nodeTexts.Add($"{parentPath}/{guiTree.GetTextByItemOrNode(item.ToString())}");
                        }
                        nodeTexts = ReBuildSelector(nodeTexts); //替换节点
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
            return nodeTexts.ToJson();
        }

        public IGuiNode[] QueryTargetSelectorAll(ParamQueryTargetSelectorAllModel param)
        {
            SapNode node = new SapNode(param.selector);
            node.cacheSelector_o = param.selector;
            var nodes = node.FindAll(null, maxResult: int.MaxValue, milliseconds: 10000);
            return nodes.ToArray();
        }

        public string GetElementAttribute_V1(string name, bool isIntact, string apiVersion)
        {
            return GetElementAttribute(name, apiVersion);
        }

        public List<string> GetSelectorAll(List<IGuiNode> guiNodes, string sourceSelector)
        {
            return guiNodes.Select(node => node.NodeSelector).ToList();
        }

        public List<Rectangle> GetRegionAll(List<IGuiNode> guiNodes)
        {
            return guiNodes.Select(node => node.GetRegion(3)).ToList();
        }

        public List<string> GetTextAll(List<IGuiNode> guiNodes)
        {
            return guiNodes.Select(node => node.GetText()).ToList();
        }

        public List<string> GetHtmlAll(List<IGuiNode> guiNodes)
        {
            return guiNodes.Select(node => node.NodeSelector ?? string.Empty).ToList();
        }
        public List<string> GetAttributeAll(List<IGuiNode> guiNodes, string attributeName, bool isIntact, string apiVersion)
        {
            return guiNodes.Select(node => node.GetElementAttribute(attributeName, apiVersion)).ToList();
        }

        public void SetElementSelectWithMatchModel(int selectType, int matchModel, string setValue)
        {
            setValue = setValue.ToObject<List<string>>((ex) =>
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_SelectValueIsNotValid, ex);
            }).First();
            SetElementSelect(selectType, setValue, matchModel);
        }

        public string GetCleanHtml()
        {
            throw new NotImplementedException();
        }
    }
}

