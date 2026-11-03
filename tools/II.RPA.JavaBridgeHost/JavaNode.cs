using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using II.RPA.JavaBridgeHost.Mode;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WindowsAccessBridgeInterop;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Logging.Formatting;
using II.RPA.Plugin.GUI.Commons;

namespace II.RPA.JavaBridgeHost
{
    public class JavaNode : Selector
    {
        private static ILogger logger = Logger.OfContext();
        public static AccessBridge accessBridge = new AccessBridge();
        public Rectangle BoundingRectangleForCell;
        public AccessibleContextInfo info = null;
        internal static AutomationBase uia3Automation = new UIA3Automation();
        internal AccessibleNode accessibleNode;
        internal int windowX = 0;
        internal int windowY = 0;

        private const PropertyOptions DefaultPropertyOptions = PropertyOptions.AccessibleContextInfo |
             PropertyOptions.AccessibleIcons |
             PropertyOptions.AccessibleKeyBindings |
             PropertyOptions.AccessibleRelationSet |
             PropertyOptions.ParentContext |
             PropertyOptions.Children |
             PropertyOptions.ObjectDepth |
             PropertyOptions.TopLevelWindowInfo |
             PropertyOptions.ActiveDescendent |
             PropertyOptions.AccessibleText |
             PropertyOptions.AccessibleHyperText |
             PropertyOptions.AccessibleValue |
             PropertyOptions.AccessibleSelection |
             PropertyOptions.AccessibleTable |
             PropertyOptions.AccessibleTableCells |
             PropertyOptions.AccessibleTableCellsSelect |
             PropertyOptions.AccessibleActions;

        private static List<AccessibleJvm> cachedJvms;
        private static bool isBridgeInitlized = false;
        private AccessibleContextNode accessibleContextNode;
        private JavaNode cachedParent;
        private List<JavaControlElement> cacheList = new List<JavaControlElement>();
        private string cacheSelector;
        private Dictionary<string, string> propDict = new Dictionary<string, string>();
        private Rectangle rect = new Rectangle();
        private AutomationElement uiAutomation;

        static JavaNode()
        {
            InitBridge();
        }

        public JavaNode(string json, string version) : base(json, version)
        {
            RefreshJvms();
            cacheSelector = json;
        }

        public JavaNode(AccessibleNode accessibleNode, AutomationElement element)
        {
            this.accessibleNode = accessibleNode;
            this.uiAutomation = element;
            Reload();
        }

        //对于表格内部的cell类型，
        public Rectangle BoundingRectangle
        {
            get
            {
                Rectangle rect = accessibleNode.GetScreenRectangle() ?? new Rectangle();
                if (rect == Rectangle.Empty || ((rect.X == -1) && (rect.Y == -1)))
                {
                    return BoundingRectangleForCell;
                }
                return rect;
            }
        }

        public List<JavaNode> Children
        {
            get
            {
                var list = new List<JavaNode>();

                var children = accessibleNode.GetChildren();
                foreach (var child in children)
                {
                    if (child == null)
                    {
                        continue;
                    }
                    list.Add(new JavaNode(child, uiAutomation));
                }

                return list;
            }
        }

        public string Idx
        {
            get;
            set;
        }

        public string Name
        {
            get
            {
                if (propDict.ContainsKey(ConstFields.SELECTOR_NAME))
                {
                    return propDict[ConstFields.SELECTOR_NAME];
                }

                return string.Empty;
            }
        }

        public string NodeSelector
        {
            get
            {
                if (string.IsNullOrEmpty(cacheSelector))
                {
                    cacheSelector = GetSelector();
                }
                return cacheSelector;
            }
            set
            {
                value = cacheSelector;
            }
        }

        [JsonIgnore]
        public JavaNode Parent
        {
            get
            {
                if (cachedParent == null)
                {
                    var parent = accessibleNode.GetParent();
                    if (parent != null)
                    {
                        JavaNode parentUiNode = new JavaNode(parent, this.uiAutomation);
                        cachedParent = parentUiNode;
                    }
                }

                return cachedParent;
            }
        }

        public string Role
        {
            get
            {
                if (propDict.ContainsKey(ConstFields.SELECTOR_ROLE))
                {
                    return propDict[ConstFields.SELECTOR_ROLE];
                }

                return string.Empty;
            }
        }

        //TODO
        public string WindowTitle
        {
            get
            {
                try
                {
                    JArray ja = JArray.Parse(this.NodeSelector);
                    foreach (JObject obj in ja)
                    {
                        if (obj.ContainsKey("title"))
                        {
                            return obj.Value<string>("title").ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
                return null;
            }
        }

        public static JavaNode GetElementFromSelector(string selector, bool findOnce = false, string version = null)
        {
            JavaNode node = new JavaNode(selector, version);
            node.SetForeground();
            var nodes = node.FindAll(null, 1, 3000, findOnce);
            logger.LogDebug("GetElementFromSelector End");
            var javaNode = nodes.FirstOrDefault() as JavaNode;
            if (javaNode == null)
            {
                throw new JavaException(eErrorMessageType.Error_NotFoundElement);
            }
            return javaNode;
        }

        public static void RefreshJvms()
        {
            InitBridge();
            if (cachedJvms != null)
            {
                cachedJvms.Clear();
            }
            cachedJvms = EnumJvms();
        }

        public bool ButtonClickEnable()
        {
            try
            {
                AccessibleContextNode cn = accessibleNode as AccessibleContextNode;
                var info = cn.GetInfo();
                if (info.states_en_US.ToLower().Contains("enabled"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (JavaException ex)
            {
                throw new JavaException(eErrorMessageType.Error_ElementIsEnable, eComponentType.ElementEnable, ex);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_SetTextError, eComponentType.ElementEnable, e);
            }
        }

        public string CaptureInformativeScreenshotToFile(string filePath)
        {
            string result = string.Empty;

            try
            {
                var deskTop = uia3Automation.GetDesktop().Capture();
                string dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                //之前生成框选元素图时出错，需要重新生成
                var currentInformativeScreenshot = DrawInformativeOnUiNode(deskTop);

                currentInformativeScreenshot.Save(filePath);

                filePath = result;
            }
            catch (Exception ex)
            {
                LoggingExtensions.LogError(logger, ex, "CaptureInformativeScreenshotToFile error");
            }
            return result;
        }

        public List<JavaNode> FindAll(JavaNode fromElement = null, int maxResult = 1, int milliseconds = 3000, bool findOnce = false)
        {
            List<JavaNode> result = new List<JavaNode>();
            var timeout = TimeSpan.FromMilliseconds(milliseconds);
            var sw = new Stopwatch();
            sw.Start();
            int index = 0;
            do
            {
                logger.LogDebug($"第{index}次查找：{result.Count}, Elapsed{sw.Elapsed}, timeout{timeout}");

                if (index > 0)
                {
                    JavaNode.RefreshJvms();
                }
                foreach (var jvm in cachedJvms)
                {
                    result = GetElementsWithuiSelector(jvm, fromElement, maxResult);
                    if (result != null && result.Count() > 0)
                    {
                        return result.ToList();
                    }
                }
                index++;
                if (result.Count == 0)
                {
                    Thread.Sleep(600);
                }
                if (findOnce == true)
                {
                    if (result.Count == 0)
                    {
                        return new List<JavaNode>();
                    }
                }
            }
            while (result.Count == 0 && sw.Elapsed < timeout);
            logger.LogDebug("FindAll End:");
            return new List<JavaNode>();
        }

        //TODO 实际是有问题的
        public string GetChild(string selelctor)
        {
            if (this.Children.Count <= 0)
            {
                throw new Exception(string.Format("不存在子元素"));
            }

            try
            {
                JArray arrayAllSelector = new JArray();
                JArray selectorProps = JArray.Parse(selelctor);
                SelectorAdapter adapter = new SelectorAdapter();
                for (int i = 0; i < this.Children.Count; i++)
                {
                    var item = this.Children[i];

                    SelectorProps selectorItem = new SelectorProps()
                    {
                        Name = item.Role,
                        Props = new List<Props>()
                    };

                    if (item.Name != null & item.Name != string.Empty)
                    {
                        Props prop = new Props()
                        {
                            Name = ConstFields.SELECTOR_NAME,
                            Value = item.Name
                        };
                        selectorItem.Props.Add(prop);
                    }
                    else
                    {
                        Props prop = new Props()
                        {
                            Name = ConstFields.SELECTOR_INDEX_IN_PARENT,
                            Value = item.Idx,
                            Accurate = ConstFields.PROP_ACCURATE_TURE,
                            Pattern = ConstFields.PATTERN_EQUAL
                        };
                        selectorItem.Props.Add(prop);
                    }
                    var subObj = JObject.FromObject(selectorItem);
                    selectorProps.Add(subObj);
                    string subSelector = selectorProps.ToString();
                    selectorProps.Remove(subObj);
                    subSelector = adapter.Convert2EditIndexFormat(subSelector);
                    arrayAllSelector.Add(JObject.Parse(subSelector));
                }
                return JsonConvert.SerializeObject(arrayAllSelector);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Point GetClickablePoint()
        {
            var point = new Point(BoundingRectangle.Left + BoundingRectangle.Width / 2, BoundingRectangle.Top + BoundingRectangle.Height / 2);
            return point;
        }

        public string GetElementCheck()
        {
            string result = string.Empty;
            try
            {
                if (Role.Contains(ConstFields.ROLE_VALUE_CHECK) || Role.Contains(ConstFields.ROLE_VALUE_RADIO))
                {
                    var sb = new StringBuilder(accessBridge.TextBufferLengthLimit);

                    var rst = accessBridge.Functions.GetCurrentAccessibleValueFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, sb, (short)sb.Capacity);
                    if (rst == true)
                    {
                        int tmp = -1;
                        if (int.TryParse(sb.ToString(), out tmp))
                        {
                            result = tmp > 0 ? "true" : "false";
                            return result;
                        }
                    }
                }
                else
                {
                    throw new JavaException(eErrorMessageType.Error_ElementNotOperation);
                }
            }
            catch (JavaException e)
            {
                throw new JavaException(e.ErrorMessageType, eComponentType.GetElementCheck, e);
            }
            catch (Exception ex)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_GetElementCheckError, eComponentType.GetElementCheck, ex);
            }
            return result;
        }

        public string GetElementSelect()
        {
            try
            {
                if (Role.Contains(ConstFields.ROLE_VALUE_COMBO))
                {
                    var tmp = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                    AccessibleContextNode acn = new AccessibleContextNode(accessBridge, tmp);
                    var info = acn.GetInfo();
                    return info.name;
                }
                else
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

        public string GetElementSelectEx(string strJson)
        {
            logger.LogDebug($"JavaNode GetElementSelectEx param:{strJson}");
            //chromeTab.IsActive();
            int selectMode = 0, selectType = 1;

            GetElementSelectExParamModel _GetElementSelectExParam = null;
            try
            {
                _GetElementSelectExParam = JsonConvert.DeserializeObject<GetElementSelectExParamModel>(strJson);
            }
            catch (Exception ex)
            {
                LoggingExtensions.LogError(logger, ex, $"JavaNode GetElementSelectEx param Err. Json:{strJson}");
                throw new JavaException(eErrorMessageType.Error_ParamsError);
            }
            finally
            {
                if (_GetElementSelectExParam == null || _GetElementSelectExParam.selectMode == -1 || _GetElementSelectExParam.selectType == -1)
                {
                    logger.LogError($"JavaNode GetElementSelectEx param Err. Json:{strJson}");
                    throw new JavaException(eErrorMessageType.Error_ParamsError);
                }
                selectMode = _GetElementSelectExParam.selectMode;
                selectType = _GetElementSelectExParam.selectType;
            }


            try
            {
                if (Role.Contains(ConstFields.ROLE_VALUE_COMBO))
                {
                    if (selectMode == 0)//获取当前选项
                    {
                        var tmp = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                        AccessibleContextNode acn = new AccessibleContextNode(accessBridge, tmp);
                        var info = acn.GetInfo();
                        if (selectType == 0)//索引
                        {
                            var retJson = new JObject
                            {{"ret", info.indexInParent}}.ToString();
                            return retJson;
                        }
                        else //1 文本
                        {
                            var retJson = new JObject
                            {{"ret", info.name}}.ToString();
                            return retJson;
                        }
                    }
                    else //获取全部
                    {
                        if (selectType == 0)//索引
                        {
                            var selectedContext = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                            var contextNode = new AccessibleContextNode(accessBridge, selectedContext);
                            var list = contextNode.GetParent().GetChildren().ToList();
                            var retJson = new JsonArrayClass();
                            retJson.ret = Enumerable.Range(0, list.Count).ToList();
                            return retJson.ToJson();
                        }
                        else //1 文本
                        {
                            var selectedContext = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                            var contextNode = new AccessibleContextNode(accessBridge, selectedContext);
                            var list = contextNode.GetParent().GetChildren().ToList();
                            var nameList = new List<string>();

                            for (int i = 0; i < list.Count; i++)
                            {
                                var cNode = list[i] as AccessibleContextNode;
                                if (cNode != null)
                                {
                                    nameList.Add(cNode.GetInfo().name);
                                }
                            }
                            var retJson = new JsonArrayClass();
                            retJson.ret = nameList;
                            return retJson.ToJson();
                        }
                    }
                }
                else
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
            return string.Empty;
        }

        public JavaNode GetNodeByPoint(int x, int y)
        {
            JavaNode newNode = null;
            AccessibleContextNode accessibleNode = this.accessibleNode as AccessibleContextNode;
            AccessibleContextNode contextNode = accessibleNode.GetNodeAt(accessibleNode.JvmId, accessibleNode.AccessibleContextHandle, x, y);
            if (contextNode != null)
            {
                newNode = new JavaNode(contextNode, this.uiAutomation);
            }
            return newNode;
        }

        public Rectangle GetRegion(int positionType)
        {
            if (positionType == 1)
            {
                return new Rectangle();
            }
            else if (positionType == 2)
            {
                RelativePosition(accessibleNode);
                return new Rectangle(this.BoundingRectangle.X - rect.X, this.BoundingRectangle.Y - rect.Y, this.BoundingRectangle.Width, this.BoundingRectangle.Height);
            }
            else if (positionType == 3)
            {
                return this.BoundingRectangle;
            }
            else
            {
                return this.BoundingRectangle;
            }
        }

        public string GetTableData()
        {
            string reuslt = string.Empty;
            try
            {
                List<List<string>> sub = new List<List<string>>();
                AccessibleTableInfo tableInfo;
                bool brt = JavaNode.accessBridge.Functions.GetAccessibleTableInfo(this.accessibleNode.JvmId, this.accessibleContextNode.AccessibleContextHandle, out tableInfo);
                if (brt == true)
                {
                    int column = tableInfo.columnCount;
                    int row = tableInfo.rowCount;

                    for (int i = 0; i < row; i++)
                    {
                        List<string> items = new List<string>();
                        for (int j = 0; j < column; j++)
                        {
                            AccessibleTableCellInfo cellInfo;
                            JavaNode.accessBridge.Functions.GetAccessibleTableCellInfo(this.accessibleNode.JvmId, this.accessibleContextNode.AccessibleContextHandle, i, j, out cellInfo);
                            AccessibleContextNode cn = new AccessibleContextNode(JavaNode.accessBridge, cellInfo.accessibleContext);

                            var info = cn.GetInfo();
                            if (info.accessibleText != 0)
                            {
                                AccessibleTextItemsInfo textItems;
                                if (accessBridge.Functions.GetAccessibleTextItems(cn.JvmId, cn.AccessibleContextHandle, out textItems, 0))
                                {
                                    var result = textItems.sentence;
                                    if (result.EndsWith("\n")) result = result.Substring(0, result.Length - 1);
                                    if (string.IsNullOrEmpty(result))
                                    {
                                        result = info.description;
                                    }
                                    items.Add(result);
                                    continue;
                                }
                            }
                            items.Add(info.name);
                        }
                        sub.Add(items);
                    }

                    var rowCount = sub.FirstOrDefault().Count;
                    List<string> rowFirst = new List<string>();
                    for (int i = 0; i < rowCount; i++)
                    {
                        rowFirst.Add("");
                    }
                    sub.Insert(0, rowFirst);
                    reuslt = Newtonsoft.Json.JsonConvert.SerializeObject(sub);
                }
            }
            catch (RPAException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_GetTableDataError);
            }
            return reuslt;
        }

        /// <summary>
        /// 因为node对象在查找过程被重置 ，缓存selector为空 现在查找表格需要用到selector 特此透传 请勿效仿
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        /// <exception cref="JavaException"></exception>
        /// <exception cref="RPAException"></exception>
        public string GetText(string selector = null)

        {
            try
            {
                if (info.accessibleText != 0)
                {
                    AccessibleTextItemsInfo textItems;
                    if (accessBridge.Functions.GetAccessibleTextItems(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out textItems, 0))
                    {
                        var result = textItems.sentence;
                        if (result.EndsWith("\n")) result = result.Substring(0, result.Length - 1);
                        if (string.IsNullOrEmpty(result))
                        {
                            result = accessibleContextNode.GetInfo().description;
                        }
                        return result;
                    }
                }
                if (info.accessibleSelection != 0)
                {
                    if (Role.Contains("combo"))
                    {
                        var tmp = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                        AccessibleContextNode acn = new AccessibleContextNode(accessBridge, tmp);
                        var info = acn.GetInfo();
                        return info.name;
                    }
                    else
                    {
                        AccessibleTextSelectionInfo textItems;
                        if (accessBridge.Functions.GetAccessibleTextSelectionInfo(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out textItems))
                        {
                            var result = textItems.selectedText;
                            if (result.EndsWith("\n")) result = result.Substring(0, result.Length - 1);
                            return result;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(selector))
                {
                    try
                    {
                        var selectorList = JsonConvert.DeserializeObject<List<SelectorProps>>(selector);

                        int rowIndex = int.Parse(selectorList[selectorList.Count - 1].Props.Where(x => x.Name.EqualIgnoreCase(ConstFields.ROLE_VALUE_TABLE_ROW)).First().Value);
                        int columnIndex = int.Parse(selectorList[selectorList.Count - 1].Props.Where(x => x.Name.EqualIgnoreCase(ConstFields.ROLE_VALUE_TABLE_COLUMN)).First().Value);
                        for (int i = selectorList.Count - 1; i > 0; i--)
                        {
                            if (!selectorList[i].Name.Equals("table", StringComparison.OrdinalIgnoreCase))
                                selectorList.Remove(selectorList[i]);
                            else
                                break;
                        }
                        if (selectorList.Count > 0)
                        {
                            JavaNode node = new JavaNode(JsonConvert.SerializeObject(selectorList), "1.0");
                            var nodes = node.FindAll(null, 1, 3000, false);
                            var tableData = nodes.FirstOrDefault().GetTableData();
                            var tableDataList = JsonConvert.DeserializeObject<List<List<string>>>(tableData);
                            info.name = tableDataList[rowIndex + 1][columnIndex];
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                return info.name;
            }
            catch (JavaException e)
            {
                throw new JavaException(eErrorMessageType.Error_GetTextError, eComponentType.GetText, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_GetTextError, eComponentType.GetText, e);
            }
        }

        public void MouseClick(int offsetX, int offsetY, bool issimulate, int cursorPos = 0)
        {
            try
            {
                if (issimulate)
                {
                    MouseMove(cursorPos, offsetX, offsetY);
                    Mouse.LeftClick();
                }
                else
                {
                    AccessibleActions _actions;
                    var actions = new Dictionary<string, object>();
                    if (accessBridge.Functions.GetAccessibleActions(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out _actions))
                    {
                        if (_actions.actionsCount > 0)
                        {
                            foreach (var a in _actions.actionInfo)
                            {
                                if (actions.Count >= _actions.actionsCount) continue;
                                if (!string.IsNullOrEmpty(a.name))
                                {
                                    if (!actions.ContainsKey(a.name))
                                    {
                                        actions.Add(a.name, a);
                                    }
                                }
                            }
                        }
                    }
                    if (actions.ContainsKey("单击") || actions.ContainsKey("click"))
                    {
                        int failure = 0;
                        AccessibleActionInfo info = new AccessibleActionInfo();
                        if (actions.ContainsKey("单击"))
                        {
                            info = (AccessibleActionInfo)actions["单击"];
                        }
                        else
                        {
                            info = (AccessibleActionInfo)actions["click"];
                        }
                        AccessibleActionsToDo actiontodo = new AccessibleActionsToDo();
                        actiontodo.actions = new AccessibleActionInfo[32];
                        actiontodo.actions[0] = info;
                        actiontodo.actionsCount = 1;
                        if (accessBridge.Functions.DoAccessibleActions(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, ref actiontodo, out failure))
                        {
                            logger.LogDebug("Success, click!");
                        }
                        else
                        {
                            throw new JavaException(eErrorMessageType.Error_ClickError, eComponentType.ElementClick);
                        }
                    }
                    else
                    {
                        // throw new RPAExecuteException("元素不支持点击操作");
                        MouseMove(cursorPos, offsetX, offsetY);
                        Mouse.LeftClick();
                    }
                }
            }
            catch (JavaException e)
            {
                throw new JavaException(eErrorMessageType.Error_ClickError, eComponentType.ElementClick, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_ClickError, eComponentType.ElementClick, e);
            }
        }

        public void MouseConterClick(int offsetX, int offsetY, int cursorPos)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.Click(FlaUI.Core.Input.MouseButton.Middle);
        }

        public void MouseConterDoubleClick(int offsetX, int offsetY, int cursorPos)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.DoubleClick(FlaUI.Core.Input.MouseButton.Middle);
        }

        public void MouseDoubleClick(int offsetX, int offsetY, int cursorPos = 0)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.LeftDoubleClick();
        }

        public void MouseHover(int offsetX, int offsetY, int cursorPos = 0)
        {
            MouseMove(cursorPos, offsetX, offsetY);
        }

        public void MouseMove(int cursorPos, int offsetX = 0, int offsetY = 0)
        {
            var rect = GetRegion(3);
            if (cursorPos == MouseParamDef.CUR_POS_CENTER)
            {
                var point = GetClickablePoint();
                Mouse.Position = new Point(point.X + offsetX, point.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_LEFT_TOP)//左上
            {
                Mouse.Position = new Point(rect.X + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_TOP)//右上
            {
                Mouse.Position = new Point(rect.X + rect.Width + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_LEFT_BOTTOM)//左下
            {
                Mouse.Position = new Point(rect.X + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_BOTTOM)//右下
            {
                Mouse.Position = new Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RANDOM)//随机
            {
                Mouse.Position = GetRandomPoint(rect);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_TOP)//上中
            {
                Mouse.Position = new Point(rect.X + rect.Width / 2 + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_LEFT)//左中
            {
                Mouse.Position = new Point(rect.X + offsetX, rect.Y + rect.Height / 2 + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_RIGHT)//右中
            {
                Mouse.Position = new Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height / 2 + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_BOTTOM)//下中
            {
                Mouse.Position = new Point(rect.X + rect.Width / 2 + offsetX, rect.Y + rect.Height + offsetY);
            }
        }
        private Point GetRandomPoint(Rectangle rect)
        {
            try
            {
                System.Random rand = new System.Random(Guid.NewGuid().GetHashCode());
                // 生成随机X坐标
                int x = rand.Next(rect.X, rect.X + rect.Width);
                // 生成随机Y坐标
                int y = rand.Next(rect.Y, rect.Y + rect.Height);
                // 随机坐标
                return new System.Drawing.Point(x, y);
            }
            catch (Exception ex)
            {
                LoggingExtensions.LogDebug(logger, ex, "获取随机坐标异常，使用(x,y)");
                return new System.Drawing.Point(rect.X, rect.Y);
            }
        }
        public void MouseRightClick(int offsetX, int offsetY, int cursorPos = 0)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.RightClick();
        }

        public void MouseRightDoubleClick(int offsetX, int offsetY, int cursorPos = 0)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.RightDoubleClick();
        }

        public void Reload()
        {
            try
            {
                propDict = new Dictionary<string, string>();
                var props = accessibleNode.GetProperties(DefaultPropertyOptions);
                EnumProperties(props, null, 0, true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }

            try
            {
                this.Idx = accessibleNode.GetIndexInParent().ToString();
                accessibleNode.Refresh();
                accessibleContextNode = accessibleNode as AccessibleContextNode;
                if (accessibleContextNode == null)
                {
                    return;
                }
                info = accessibleContextNode.GetInfo();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }

        public void SetElementCheck(int check)
        {
            try
            {
                if (Role.Contains(ConstFields.ROLE_VALUE_CHECK) || Role.Contains(ConstFields.ROLE_VALUE_RADIO))
                {
                    var sb = new StringBuilder(accessBridge.TextBufferLengthLimit);

                    var rst = accessBridge.Functions.GetCurrentAccessibleValueFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, sb, (short)sb.Capacity);
                    if (rst == true)
                    {
                        int tmp = -1;
                        if (int.TryParse(sb.ToString(), out tmp))
                        {
                            if (tmp == 0 && check > 0)
                            {
                                MouseClick(0, 0, true);
                            }
                            if (tmp == 1 && check < 1)
                            {
                                MouseClick(0, 0, true);
                            }
                        }
                    }
                }
                else
                {
                    throw new JavaException(eErrorMessageType.Error_ElementNotOperation);
                }
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

        public void SetElementSelect(int selectType, string setValue)
        {
            try
            {
                if (this.Role.Contains(ConstFields.ROLE_VALUE_COMBO))
                {
                    cacheList.Clear();

                    RecursionChildren(accessibleContextNode, 0);
                    if (cacheList.Count > 1)
                    {
                        var temp = cacheList.OrderBy(x => x.Depth).LastOrDefault();
                        if (temp != null)
                        {
                            int depth = temp.Depth;
                            var selections = cacheList.Where(x => x.Depth == depth).ToList();
                            if (selectType == 0)   //按顺序设置
                            {
                                int idx;
                                bool p = int.TryParse(setValue, out idx);
                                if (p == false)
                                {
                                    throw new JavaException(eErrorMessageType.Error_SelectValueIsNotValid);
                                }
                                if (idx >= selections.Count)
                                {
                                    throw new JavaException(eErrorMessageType.Error_IndexOverflow);
                                }
                                accessBridge.Functions.AddAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, idx);
                            }
                            else if (selectType == 1)
                            {
                                int matchIndex = -1;
                                for (int i = 0; i < selections.Count; i++)
                                {
                                    var info = selections[i].AccInfo;
                                    if (setValue == info.name)
                                    {
                                        matchIndex = i;
                                        break;
                                    }
                                }
                                if (matchIndex == -1)
                                {
                                    throw new JavaException(eErrorMessageType.Error_SetSelectError);
                                }
                                else
                                {
                                    accessBridge.Functions.AddAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, matchIndex);
                                }
                            }
                        }
                    }
                    else
                    {
                        var selectedContext = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                        var contextNode = new AccessibleContextNode(accessBridge, selectedContext);
                        var list = contextNode.GetParent().GetChildren().ToList();
                        Dictionary<int, string> pairs = new Dictionary<int, string>();
                        for (int i = 0; i < list.Count; i++)
                        {
                            var cNode = list[i] as AccessibleContextNode;
                            if (cNode != null)
                            {
                                pairs.Add(i, cNode.GetInfo().name);
                            }
                        }
                        if (selectType == 0)   //按顺序设置
                        {
                            int idx;
                            bool p = int.TryParse(setValue, out idx);
                            if (p == false)
                            {
                                throw new JavaException(eErrorMessageType.Error_SelectValueIsNotValid);
                            }
                            if (idx >= pairs.Count)
                            {
                                throw new JavaException(eErrorMessageType.Error_IndexOverflow);
                            }
                            accessBridge.Functions.AddAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, idx);
                        }
                        else if (selectType == 1)
                        {
                            int matchIndex = -1;
                            var matchNode = pairs.Where(x => x.Value == setValue);
                            if (matchNode.Count() > 0)
                            {
                                matchIndex = matchNode.First().Key;
                            }

                            if (matchIndex == -1)
                            {
                                throw new JavaException(eErrorMessageType.Error_SetSelectError);
                            }
                            else
                            {
                                accessBridge.Functions.AddAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, matchIndex);
                            }
                        }
                    }
                }
                else
                {
                    throw new JavaException(eErrorMessageType.Error_ElementNotOperation);
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
            var selectorItem = this.OfType<JavaSelectorItem>().FirstOrDefault(x => x.Enabled); //找出不是顶层元素
            foreach (var jvm in cachedJvms)
            {
                JavaNode node = new JavaNode(jvm, uia3Automation.GetDesktop());
                JavaNode matchNode = selectorItem.Matches(node).FirstOrDefault();
                if (matchNode != null)
                {
                    try
                    {
                        windowX = matchNode.BoundingRectangle.X;
                        windowY = matchNode.BoundingRectangle.Y;
                        IntPtr ptr = matchNode.accessibleNode.AccessBridge.Functions.GetHWNDFromAccessibleContext(matchNode.accessibleNode.JvmId, matchNode.accessibleContextNode.AccessibleContextHandle);
                        UiCommon.ForceShow(ptr);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex);
                    }
                }
            }
        }
        private AccessibleContextInfo GetInfo()
        {
            //this.ThrowIfDisposed();
            AccessibleContextInfo info;
            if (!accessBridge.Functions.GetAccessibleContextInfo(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out info))
            {
                return null;
            }
            return info;
        }
        private string GetAccessibleText()
        {
            int len = 32767;
            AccessibleTextInfo textInfo;
            if (accessBridge.Functions.GetAccessibleTextInfo(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out textInfo, 0, 0))
            {
                len = Math.Min(textInfo.charCount, len);
            }
            StringBuilder sb = new StringBuilder(len);
            string value = "";
            AccessibleContextInfo info = this.GetInfo();
            if ((info.accessibleInterfaces & AccessibleInterfaces.cAccessibleValueInterface) != (AccessibleInterfaces)0 && accessBridge.Functions.GetCurrentAccessibleValueFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, sb, (short)len))
            {
                value = sb.ToString();
            }
            string text = "";
            if ((info.accessibleInterfaces & AccessibleInterfaces.cAccessibleTextInterface) != (AccessibleInterfaces)0)
            {
                for (int i = 0; i < len; i++)
                {
                    AccessibleTextItemsInfo itemsInfo;
                    if (accessBridge.Functions.GetAccessibleTextItems(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out itemsInfo, i))
                    {
                        sb.Append(itemsInfo.letter);
                    }
                }
                text = sb.ToString();
            }
            if (!string.IsNullOrEmpty(value))
            {
                return "";
            }
            return text;
        }
        public void SetText(string text, int isReplace, bool isSimulate)
        {
            try
            {
                if (info.accessibleText == 0)
                {
                    logger.LogError(("元素不支持文本操作,进行模拟输入"));
                    if (Role.Contains("combo"))
                    {
                        int retry = 0;

                        do
                        {
                            var tmp = accessBridge.Functions.GetAccessibleSelectionFromContext(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, 0);
                            AccessibleContextNode acn = new AccessibleContextNode(accessBridge, tmp);

                            var info = acn.GetInfo();
                            var selectName = info.name;  //获取当前选中值

                            var block = LogBlock.Rows(
                                default,
                                retry.ToString().Tag("次数"),
                                selectName.Tag("selectName"),
                                text.Tag("SetText"));

                            logger.LogInformation(block);

                            if (selectName.Trim() != text.Trim())
                            {
                                cacheList.Clear();
                                RecursionChildren(accessibleContextNode, 0);
                                int nowIndex = 0, targetIndex = 0;
                                var temp = cacheList.OrderBy(x => x.Depth).LastOrDefault();
                                if (temp != null)
                                {
                                    int depth = temp.Depth;
                                    var selections = cacheList.Where(x => x.Depth == depth).ToList();
                                    for (int i = 0; i < selections.Count; i++)
                                    {
                                        if (selections[i].Name == selectName)
                                        {
                                            nowIndex = i;
                                        }
                                        if (selections[i].Name == text)
                                        {
                                            targetIndex = i;
                                        }
                                    }
                                    if (nowIndex > targetIndex)
                                    {
                                        //向上按键
                                        Point p = GetClickablePoint();
                                        Mouse.Position = p;
                                        Mouse.Click();
                                        logger.LogInformation("NowIndex:" + nowIndex + "targetIndex:" + targetIndex);
                                        for (int i = targetIndex; i < nowIndex; i++)
                                        {
                                            Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.UP);
                                            Thread.Sleep(20);
                                        }
                                        Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
                                    }
                                    else
                                    {
                                        Point p = GetClickablePoint();
                                        Mouse.Position = p;
                                        Mouse.Click();
                                        logger.LogInformation("NowIndex:" + nowIndex + "targetIndex:" + targetIndex);
                                        for (int i = nowIndex; i < targetIndex; i++)
                                        {
                                            Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DOWN);
                                            Thread.Sleep(20);
                                        }
                                        Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
                                    }
                                }
                            }
                            else
                            {
                                return;
                            }
                            retry++;
                        }
                        while (retry < 3);
                    }
                    else
                    {
                        Point p = GetClickablePoint();
                        Mouse.Position = p;
                        Mouse.Click();
                        Thread.Sleep(200);
                        if (isReplace == 0)
                        {
                            Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                            Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                            Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);

                            Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
                            Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                            Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                        }
                        Keyboard.Type(text);
                        Mouse.Click();
                    }
                }
                else
                {
                    AccessibleTextItemsInfo textItems;
                    if (accessBridge.Functions.GetAccessibleTextItems(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out textItems, 0))
                    {
                        if (isSimulate)
                        {
                            var value = textItems.word;
                            if (!string.IsNullOrEmpty(textItems.word))
                            {
                                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);

                                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
                                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                                Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                                Thread.Sleep(200);
                            }
                            if (isReplace == 0)
                            {
                                Keyboard.SendInput(text);
                            }
                            else
                            {
                                Keyboard.SendInput(value + text);
                            }
                        }
                        else
                        {
                            string value = string.Empty;
                            if (isReplace == 0)
                                value = text;
                            else
                                value = GetAccessibleText() + text;
                            if (!accessBridge.Functions.SetTextContents(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, value))
                            {
                                accessBridge.Functions.GetAccessibleTextItems(accessibleContextNode.JvmId, accessibleContextNode.AccessibleContextHandle, out textItems, 0);
                                if (textItems.word != value)
                                    throw new JavaException(eErrorMessageType.Error_SetTextError);
                            }
                        }
                    }
                }
            }
            catch (JavaException e)
            {
                throw new JavaException(eErrorMessageType.Error_SetTextError, eComponentType.SetText, e);
            }
            catch (Exception e)
            {
                throw new RPAException(eGuiType.Java, eErrorMessageType.Error_SetTextError, eComponentType.SetText, e);
            }
        }

        internal static List<AccessibleJvm> EnumJvms(bool bRefresh = false)
        {
            if (bRefresh)
            {
                cachedJvms = null;
            }
            if (cachedJvms == null || cachedJvms.Count == 0)
            {
                cachedJvms = accessBridge.EnumJvms(hwnd => accessBridge.CreateAccessibleWindow(hwnd));
            }
            return cachedJvms;
        }

        protected override SelectorItem GenerateSelectorItem(JObject json, string version)
        {
            return JavaSelectorItem.FromJObject(json, version);
        }

        private static void AccessBridge_Initilized(object sender, EventArgs e)
        {
            isBridgeInitlized = true;
            logger.LogInformation("AccessBridge_Initilized");
        }

        private static void InitBridge()
        {
            try
            {
                if (!isBridgeInitlized)
                {
                    accessBridge.Initilized -= AccessBridge_Initilized;
                    accessBridge.Initilized += AccessBridge_Initilized;
                    accessBridge.Initialize();
                    Thread.Sleep(500);
                }
            }
            catch (Exception e)
            {
                LoggingExtensions.LogError(logger, e, "AccessBridge Initialize Error");
            }
        }

        private Bitmap DrawInformativeOnUiNode(Bitmap currentDesktopScreenshot)
        {
            using (Graphics g = Graphics.FromImage(currentDesktopScreenshot))
            {
                //最终截图的红色标记边框
                Pen pen = new Pen(Color.Red, 2);
                g.DrawRectangle(pen, this.BoundingRectangle);
            }

            Rectangle cropRect = this.BoundingRectangle;
            cropRect.Inflate(5, 5);
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(currentDesktopScreenshot, new Rectangle(0, 0, target.Width, target.Height),
                                 cropRect,
                                 GraphicsUnit.Pixel);
            }

            return target;
        }

        private void EnumProperties(PropertyList list, string prefix, int ident, bool ignoreGroups)
        {
            foreach (var prop in list)
            {
                var name = prop.Name.ToLower();

                if (name == "parent")
                {
                    continue;
                }

                if (name == "children" && !ignoreGroups)
                {
                    var g = prop as PropertyGroup;
                    if (g == null)
                    {
                        continue;
                    }
                    g.LoadChildren();
                    foreach (var c in g.Children)
                    {
                        var v = c.Value;
                    }
                    continue;
                }
                if (name == "top level window")
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(prefix))
                {
                    name = prefix + "." + name;
                }

                if (prop is PropertyGroup)
                {
                    if (ignoreGroups) continue;
                    try
                    {
                        var g = prop as PropertyGroup;
                        if (g == null)
                        {
                            continue;
                        }
                        g.LoadChildren();
                        if (ident <= 2)
                        {
                            EnumProperties(g.Children, name, (ident + 1), false);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingExtensions.LogError(logger, ex, $"EnumProperties Error for {name}");
                    }
                }
                else
                {
                    if (prop.Value == null)
                    {
                        continue;
                    }
                    var value = prop.Value.ToString();
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    if (!propDict.ContainsKey(name))
                    {
                        propDict.Add(name, prop.Value.ToString());
                    }
                }
            }
        }

        private List<JavaNode> GetElementsWithuiSelector(WindowsAccessBridgeInterop.AccessibleJvm jvm, JavaNode fromElement, int maxresults)
        {
            var currents = new List<JavaNode>();
            var selectors = this.OfType<JavaSelectorItem>().Where(x => x.Enabled).ToList(); //找出不是顶层元素
            JavaNode startfrom = null;
            if (fromElement != null)
            {
                startfrom = fromElement;
            }
            if (startfrom == null)
            {
                startfrom = new JavaNode(jvm, null);
            }
            currents.Add(startfrom);

            JavaNode[] result = null;
            for (int i = 0; i < selectors.Count; i++)
            {
                JavaSelectorItem selectorItem = selectors[i];
                List<JavaNode> elements = new List<JavaNode>(currents);
                currents.Clear();
                foreach (var eleItem in elements)
                {
                    result = selectorItem.Matches(eleItem, windowX, windowY);
                    if (result.Count() > 0)
                    {
                        currents.AddRange(result);
                    }
                    else
                    {
                        //如果没有在子元素中获取到，再去后代里面找元素
                        if (selectorItem.Properties.Where(x => x.Name.EqualIgnoreCase(ConstFields.SELECTOR_POSITION_X)).Count() > 0) //需要根据坐标来获取
                        {
                            int x = int.Parse(selectorItem.Properties.Where(m => m.Name.EqualIgnoreCase(ConstFields.SELECTOR_POSITION_X)).FirstOrDefault().Value) + windowX;
                            int y = int.Parse(selectorItem.Properties.Where(n => n.Name.EqualIgnoreCase(ConstFields.SELECTOR_POSITION_Y)).FirstOrDefault().Value) + windowY;
                            var node = this.accessibleContextNode.GetNodeAt(this.accessibleContextNode.JvmId, this.accessibleContextNode.AccessibleContextHandle, x, y);
                            if (node != null)
                            {
                                JavaNode javaNode = new JavaNode(node, this.uiAutomation);
                                currents.Add(javaNode);
                            }
                        }
                        else
                        {
                            result = GetFromSubList(selectorItem, eleItem.Children);
                            currents.AddRange(result);
                        }
                    }
                }

                if (i == (selectors.Count - 1))
                {
                    result = currents.ToArray();
                }
            }
            if (result == null)
            {
                return new List<JavaNode>();
            }
            return new List<JavaNode>(result);
        }

        private JavaNode[] GetFromSubList(JavaSelectorItem selectItem, List<JavaNode> parent)
        {
            JavaNode[] list = new JavaNode[0];
            foreach (var ele in parent)
            {
                list = selectItem.Matches(ele);
                if (list.Count() == 0)
                {
                    list = GetFromSubList(selectItem, ele.Children);
                }
            }
            return list;
        }

        private string GetSelector()
        {
            throw new NotImplementedException("Java BridgeHost cannot support selector generate feature!");
        }

        private void RecursionChildren(AccessibleContextNode node, int depth)
        {
            JavaControlElement jce = new JavaControlElement(node, depth);
            cacheList.Add(jce);
            depth++;
            var nodes = node.GetChildren();
            foreach (var item in nodes)
            {
                AccessibleContextNode acn = item as AccessibleContextNode;
                RecursionChildren(acn, depth);
            }
        }

        private void RelativePosition(AccessibleNode acc)
        {
            if (WindowTitle == acc.GetTitle())
            {
                rect = acc.GetScreenRectangle().Value;
            }
            else
            {
                RelativePosition(acc.GetParent());
            }
        }
        public void MouseThreeClick(int offsetX, int offsetY, int cursorPos)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.LeftDoubleClick();
            Mouse.LeftClick();
        }

        public void MouseRightThreeClick(int offsetX, int offsetY, int cursorPos)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.RightDoubleClick();
            Mouse.RightClick();
        }

        public void MouseConterThreeClick(int offsetX, int offsetY, int cursorPos)
        {
            MouseMove(cursorPos, offsetX, offsetY);
            Mouse.DoubleClick(FlaUI.Core.Input.MouseButton.Middle);
            Mouse.Click(FlaUI.Core.Input.MouseButton.Middle);
        }
    }
}