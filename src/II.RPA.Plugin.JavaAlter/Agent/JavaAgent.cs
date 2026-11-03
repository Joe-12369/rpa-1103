using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Core.Log;
using II.RPA.Logging;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Parse;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Props = II.RPA.Plugin.Core.Commons.Props;
using SelectorProps = II.RPA.Plugin.Core.Commons.SelectorProps;
using II.RPA.Plugin.Java.Config;

namespace II.RPA.Plugin.JavaAlter.Agent
{

    internal class JavaAgent
    {
        private Microsoft.Extensions.Logging.ILogger logger = Logger.OfContext();

        private static readonly Lazy<JavaAgent> _instance = new Lazy<JavaAgent>(() => new JavaAgent());
        public ConcurrentDictionary<int, JavaProxy> JavaProxyes = new ConcurrentDictionary<int, JavaProxy>();


        public static JavaAgent Instance { get { return _instance.Value; } }
        private JavaProcessLocator javaProcessLocator;
        private JavaAgent()
        {
            javaProcessLocator = new JavaProcessLocator(JavaConfigurations.Info.Whitelist);
        }


        /// <summary>
        /// 注入Java 代理
        /// </summary>
        /// <param name="javaPath"></param>
        /// <param name="processId"></param>
        /// <returns></returns>
        public bool InjectAgent(int targetId)
        {
            try
            {

                string localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string javaLibPath = Path.Combine(localPath, "IIRPA", "javaAgent");
                var javaDllPath = Path.Combine(javaLibPath, "indeed.java.instrumentation.dll");
                var javaInjectorPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IndeedInjector.exe");

                if (!Directory.Exists(javaLibPath))
                {
                    logger.LogError($"拾取依赖文件未找到: {javaLibPath}");
                }
                if (!Directory.Exists(javaDllPath))
                {
                    logger.LogError($"拾取依赖文件未找到: {javaDllPath}");
                }
                if (!Directory.Exists(javaInjectorPath))
                {
                    logger.LogError($"拾取依赖文件未找到: {javaInjectorPath}");
                }

                logger.LogInformation($"Begin injection: PID={targetId} DLL={javaDllPath}");

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = javaInjectorPath;
                psi.Arguments = $"{targetId} \"{javaDllPath}\"";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                using (Process p = Process.Start(psi))
                {
                    // 读取输出
                    string output = p.StandardOutput.ReadToEnd();
                    string error = p.StandardError.ReadToEnd();
                    p.WaitForExit();

                    logger.LogDebug($"IndeedInjector Output: {output}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        logger.LogError($"IndeedInjector Error: {error}");
                    }
                    if (p.ExitCode == 0)
                    {
                        logger.LogInformation($"Injection successful for PID={targetId}");
                        return true;
                    }
                    else
                    {
                        string msg = Localize($"注入失败 (ExitCode={p.ExitCode}): {output} {error}", "JavaAgent_D13336EF");
                        logger.LogError(msg);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 创建Java Proxy
        /// </summary>
        /// <param name="jvmId"></param>      
        /// <returns></returns>
        public bool CrateJavaProxy(int jvmId)
        {
            if (JavaProxyes.ContainsKey(jvmId))
            {
                logger.LogInformation($"ProcessId:{jvmId},Proxy Exists");
                return true;
            }
            var exist_proxy = JavaProxy.Create(jvmId);
            if (exist_proxy != null)
            {
                JavaProxyes[jvmId] = exist_proxy;
                logger.LogInformation($"JavaProxyes created (existing). PID={jvmId}, Connected={exist_proxy.Connected}");
                return true;
            }
            else
            {
                var proxy = InjectAgent(jvmId);
                if (proxy)
                {
                    JavaProxyes[jvmId] = JavaProxy.Create(jvmId, 6);
                    logger.LogInformation($"JavaProxyes created after injection. PID={jvmId}, Connected={JavaProxyes[jvmId]?.Connected}");
                    return true;
                }
                logger.LogInformation($"JavaProxyes creation failed for PID={jvmId}");
                return false;
            }
        }

        private List<Element> RetrieveChilds(Element element)
        {
            var childs = new List<Element>();

            if (element.FlavorElement is AwtFlavorElement)
            {
                if (element.MappedClazz == "javax.swing.JTable")
                {
                    var rows = element.Jow.InvokeMethod("getRowCount", Array.Empty<object>());

                    if (int.TryParse(rows[0]?.ToString(), out int rowCount))
                    {
                        for (int r = 0; r < rowCount; r++)
                        {
                            var rowElement = new JavaRowFlavorElement(element.FlavorElement as AwtFlavorElement, r);
                            childs.Add(new Element(rowElement, element, r, rowElement.FlavorName));
                        }
                        var columns = element.Jow.InvokeMethod("getColumnCount", Array.Empty<object>());
                        if (int.TryParse(columns[0]?.ToString(), out int colCount))
                        {
                            for (int c = 0; c < colCount; c++)
                            {
                                var colElement = new JavaColumnFlavorElement(element.FlavorElement as AwtFlavorElement, c);
                                childs.Add(new Element(colElement, element, c, colElement.FlavorName));
                            }
                        }

                    }
                }
                if (element.MappedClazz == "javax.swing.table.JTableHeader")
                {
                    JavaObjectWrapper table = (JavaObjectWrapper)element.Jow.InvokeGetProperty("getTable");
                    int colCount = (int)table.InvokeGetProperty("getColumnCount");
                    for (int c = 0; c < colCount; c++)
                    {
                        if (element.FlavorElement is AwtFlavorElement)
                        {
                            var headerElement = new JavaHeaderCellFlavorElement(element.FlavorElement as AwtFlavorElement, table, c);
                            childs.Add(new Element(headerElement, element, c, headerElement.FlavorName));
                        }
                    }
                }
                if (element.MappedClazz == "javax.swing.JTree")
                {
                    var treeItemElement = new JavaTreeItemFlavorElement(element.FlavorElement as AwtFlavorElement, new List<int>(), 0);
                    //先获取根节点
                    var rootItem = element.Jow.InvokeGetSpecialProperty("treerootitem", Array.Empty<object>());
                    for (int i = 0; i < rootItem.Length; i++)
                    {
                        var itemElement = new JavaTreeItemFlavorElement(treeItemElement.Container, new List<int>(), i);
                        childs.Add(new Element(itemElement, element, i, "TreeRootItem"));
                    }
                }
                if (element.MappedClazz == "javax.swing.JTabbedPane")
                {
                    int tabCount = (int)element.Jow.InvokeGetProperty("getTabCount");
                    for (int j = 0; j < tabCount; j++)
                    {
                        var tabElement = new JavaTabFlavorElement(element.FlavorElement as AwtFlavorElement, j);
                        childs.Add(new Element(tabElement, element, j, tabElement.FlavorName));
                    }
                }
                if (element.MappedClazz == "javax.swing.JList")
                {
                    int itemCount = (int)element.Jow.InvokeGetSpecialProperty("listitemcount", Array.Empty<object>())[0];
                    for (int m = 0; m < itemCount; m++)
                    {
                        var listElement = new JavaListItemFlavorElement(element.FlavorElement as AwtFlavorElement, m);
                        childs.Add(new Element(listElement, element, m, listElement.FlavorName));
                    }
                }
                if (element.MappedClazz == "java.awt.List")
                {
                    int itemCount = (int)element.Jow.InvokeMethod("getItemCount", Array.Empty<object>())[0];
                    for (int n = 0; n < itemCount; n++)
                    {
                        var listElement = new JavaListItemFlavorElement(element.FlavorElement as AwtFlavorElement, n);
                        childs.Add(new Element(listElement, element, n, listElement.FlavorName));
                    }
                }
                var children = element.Jow.InvokeGetChildren();
                int index = 0;
                foreach (var item in children)
                {
                    var toolTips = item.Lineage?.Contains("javax.swing.JToolTip");
                    if (toolTips.HasValue && toolTips.Value)
                    {
                        continue;
                    }
                    if ((bool)item.InvokeGetSpecialProperty("visible")[0])
                    {
                        var awtElement = new AwtFlavorElement(item, element.FlavorElement, element.FlavorElement, index);
                        var _element = new Element(awtElement, awtElement.MapClassToRole(item.Lineage, out string mappedClazz), mappedClazz, element, item, index, mappedClazz.Substring(mappedClazz.LastIndexOf(".") + 1));
                        childs.Add(_element);
                    }
                    index++;
                }

            }

            #region 表格行元素
            JavaRowFlavorElement javaRowFlavorElement = element.FlavorElement as JavaRowFlavorElement;
            if (javaRowFlavorElement != null)
            {
                int colCount = (int)javaRowFlavorElement.Container.JavaObject.InvokeMethod("getColumnCount", Array.Empty<object>())[0];
                for (int c = 0; c < colCount; c++)
                {
                    var cellElement = new JavaCellFlavorElement(javaRowFlavorElement.Container, javaRowFlavorElement.RowIndex, c)
                    ;
                    childs.Add(new Element(cellElement, element, c, cellElement.FlavorName));
                }
            }
            #endregion

            #region 表格列元素

            JavaColumnFlavorElement javaColumnFlavorElement = element.FlavorElement as JavaColumnFlavorElement;
            if (javaColumnFlavorElement != null)
            {
                int rowCount = (int)javaColumnFlavorElement.Container.JavaObject.InvokeMethod("getRowCount", Array.Empty<object>())[0];
                for (int r = 0; r < rowCount; r++)
                {
                    var cellElement = new JavaCellFlavorElement(javaColumnFlavorElement.Container, r, javaColumnFlavorElement.ColumnIndex);
                    childs.Add(new Element(cellElement, element, r, cellElement.FlavorName));
                }
            }
            #endregion

            #region 树节点元素
            if (element.FlavorElement is JavaTreeItemFlavorElement javaTreeItemFlavorElement)
            {
                object[] array = element.Jow.InvokeGetChildItems(javaTreeItemFlavorElement.ItemPath);
                foreach (int item in array)
                {
                    List<int> treePath = new List<int>(javaTreeItemFlavorElement.ItemPath)
                        {
                          item
                        };
                    var itemElement = new JavaTreeItemFlavorElement(javaTreeItemFlavorElement.Container, treePath, item);
                    childs.Add(new Element(itemElement, element, item, itemElement.FlavorName));
                }
            }
            #endregion

            return childs;
        }
        private Element FindTreeChilds(Element element, Point point)
        {
            var childs = RetrieveChilds(element);
            for (int i = 0; i < childs.Count; i++)
            {
                int j = i == childs.Count - 1 ? i : i + 1;
                if (childs[i].FlavorElement.ElementRectangle.Contains(point))
                {
                    //坐标在节点内直接返回
                    return childs[i];
                }
                //计算当前节点的宽高（含子节点）
                var nextChild = childs[j];
                int height = j == i ? childs[i].FlavorElement.ElementRectangle.Height : nextChild.FlavorElement.ElementRectangle.Y - childs[i].FlavorElement.ElementRectangle.Y;
                var elementRect = new Rectangle(childs[i].FlavorElement.ElementRectangle.X, childs[i].FlavorElement.ElementRectangle.Y, childs[i].FlavorElement.ElementRectangle.Width, height);
                if ((elementRect.Contains(point) && childs[i].FlavorElement.Visible) || i == j)
                {
                    return FindTreeChilds(childs[i], point);
                }
            }
            return element;
        }
        private Element FindElement(Element element, Point point)
        {
            var childs = RetrieveChilds(element);
            foreach (var child in childs)
            {
                if (child.FlavorElement.ElementRectangle.Contains(point) && child.FlavorElement.Visible)
                {
                    if (child.FlavorElement is JavaTreeItemFlavorElement)
                    {
                        //树的根节点
                        return child;
                    }
                    return FindElement(child, point);
                }
                else
                {
                    //树的子节点
                    if (child.FlavorElement is JavaTreeItemFlavorElement)
                    {
                        var _childElement = FindTreeChilds(child, point);
                        return _childElement;
                    }
                }
            }
            return element;
        }

        public JavaElement FindElementByPointV2(int processId, IntPtr hwnd, Point point)
        {
            if (JavaProxyes.TryGetValue(processId, out JavaProxy javaProxy))
            {
                var hitResult = javaProxy.CallFunction(JavaTech.DontCare, FunctionId.FuncGetElementByPoint, new object[] { point.X, point.Y }, out CallStatus status);
                if (status == CallStatus.Ok)
                {
                    if (hitResult != null && hitResult.Length > 3)
                    {
                        var elementObj = hitResult[0] as JavaObjectWrapper;
                        var rect = hitResult[1] is Rectangle r ? r : Rectangle.Empty;
                        var mode = (JavaTech)Convert.ToInt32(hitResult[hitResult.Length - 1]);
                        var _tech = (JavaTech)Convert.ToInt32(hitResult[hitResult.Length - 2]);
                        elementObj.Tech = _tech;
                        var javaElement = new JavaElement(elementObj, rect);
                        javaElement.ClassName = hitResult[2]?.ToString();
                        return javaElement;
                    }

                }
                else
                {
                    logger.LogError($"element not found,message:{status},length:{hitResult.Length}");
                }

            }
            else
            {
                //proxy not exist
                logger.LogWarning("java proxy not exist in {processId}");
            }
            return null;
        }
        public Element FindElementByPoint(int processId, IntPtr hwnd, Point point)
        {
            try
            {
                var windows = JavaProxyes[processId].InvokeGetWindows();
                var findWnd = windows.FirstOrDefault(w => Convert.ToInt32(w) == (int)hwnd);
                if (findWnd != null)
                {
                    windows = new object[1] { findWnd };
                }
                foreach (var item in windows)
                {
                    if (int.TryParse(item?.ToString(), out int targetWnd))
                    {
                        var javaObj = JavaProxyes[processId].InvokeGetAwtWindow(targetWnd, out string[] awtWindows);
                        logger.LogDebug($"Element Wndindow:{javaObj}");
                        if (javaObj != null && !javaObj.Lineage[0].Contains("swing.Popup") && (bool)javaObj.InvokeGetSpecialProperty("visible", Array.Empty<object>())[0])
                        {
                            var wndElement = new AwtWindowFlavorElement(javaObj, awtWindows, new Win32FlavorElement(hwnd));
                            var rootElement = new Element(wndElement, wndElement.MapClassToRole(javaObj.Lineage, out string clazz), clazz, null, javaObj, 0, wndElement.FlavorName);
                            var findElement = FindElement(rootElement, point);
                            //findElement.JVM = Jvms[processId];
                            return findElement;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return null;
        }
        public Element FindElementChild(JToken selectorProps, Element element)
        {
            // 1. 获取 Tag (原 selectorProps.Name)
            var Tag = selectorProps["name"]?.ToString();

            if (string.IsNullOrEmpty(Tag) || element == null)
            {
                return null;
            }

            // 提取 Props 数组引用，方便后续 Find 操作
            var propsArray = selectorProps["props"] as JArray;
            if (propsArray == null) return null;

            // 2. 提取 Text (保持原变量名和逻辑)
            var Text = propsArray.FirstOrDefault(p => p["name"]?.ToString() == "text")?["value"]?.ToString();
            var TextPattern = propsArray.FirstOrDefault(p => p["name"]?.ToString() == "text")?["pattern"]?.ToString();

            // 3. 提取 Title (保持原变量名和逻辑)
            var Title = propsArray.FirstOrDefault(p => p["name"]?.ToString() == "title")?["value"]?.ToString();
            var TitlePattern = propsArray.FirstOrDefault(p => p["name"]?.ToString() == "title")?["pattern"]?.ToString();

            // 4. 提取并解析 Index (保持原变量名和逻辑)
            // 注意：XPath 里的 index 有时叫 "@index" 有时叫 "index"，这里做个兼容
            var indexToken = propsArray.FirstOrDefault(p => p["name"]?.ToString() == "@index")
                             ?? propsArray.FirstOrDefault(p => p["name"]?.ToString() == "index");

            var verifyIndex = int.TryParse(indexToken?["value"]?.ToString()?.Trim('\'', '\"', ' '), out int Index);
            //if (!verifyIndex && string.IsNullOrEmpty(Text))
            //return null;
            if (Tag.Equals("Row"))
            {
                if (element.MappedClazz == "javax.swing.JTable")
                {
                    var rows = element.Jow.InvokeMethod("getRowCount", Array.Empty<object>());

                    if (int.TryParse(rows[0]?.ToString(), out int rowCount))
                    {
                        if (rowCount < Index + 1) return null;
                        var rowElement = new JavaRowFlavorElement(element.FlavorElement as AwtFlavorElement, Index);
                        return new Element(rowElement, element, Index, "Row");
                    }
                }
                return null;
            }
            else if (Tag.Equals("Column"))
            {
                if (element.MappedClazz == "javax.swing.JTable")
                {
                    var columns = element.Jow.InvokeMethod("getColumnCount", Array.Empty<object>());
                    if (int.TryParse(columns[0]?.ToString(), out int colCount))
                    {
                        if (colCount < Index + 1) return null;
                        var colElement = new JavaColumnFlavorElement(element.FlavorElement as AwtFlavorElement, Index);
                        return new Element(colElement, element, Index, "Column");
                    }
                }
                return null;
            }
            else if (Tag.Equals("Cell"))
            {
                JavaRowFlavorElement javaRowFlavorElement = element.FlavorElement as JavaRowFlavorElement;
                if (javaRowFlavorElement != null)
                {
                    int colCount = (int)javaRowFlavorElement.Container.JavaObject.InvokeMethod("getColumnCount", Array.Empty<object>())[0];
                    if (colCount < Index + 1) return null;

                    var cellElement = new JavaCellFlavorElement(javaRowFlavorElement.Container, javaRowFlavorElement.RowIndex, Index);
                    return new Element(cellElement, element, Index, "Cell");

                }
                JavaColumnFlavorElement javaColumnFlavorElement = element.FlavorElement as JavaColumnFlavorElement;
                if (javaColumnFlavorElement != null)
                {
                    int rowCount = (int)javaColumnFlavorElement.Container.JavaObject.InvokeMethod("getRowCount", Array.Empty<object>())[0];
                    if (rowCount < Index + 1) return null;

                    var cellElement = new JavaCellFlavorElement(javaColumnFlavorElement.Container, Index, javaColumnFlavorElement.ColumnIndex);
                    return new Element(cellElement, element, Index, "Cell");
                }
                return null;
            }
            else if (Tag.Equals("HeaderCell"))
            {
                if (element.MappedClazz == "javax.swing.table.JTableHeader")
                {
                    JavaObjectWrapper table = (JavaObjectWrapper)element.Jow.InvokeGetProperty("getTable");
                    int colCount = (int)table.InvokeGetProperty("getColumnCount");
                    if (colCount < Index) return null;
                    JavaHeaderCellFlavorElement headerElement = null;
                    if (element.FlavorElement is AwtFlavorElement)
                    {
                        headerElement = new JavaHeaderCellFlavorElement(element.FlavorElement as AwtFlavorElement, table, Index);
                        return new Element(headerElement, element, Index, "HeaderCell");
                    }

                }
                return null;
            }
            else if (Tag.Equals("TreeItem") || Tag.Equals("TreeRootItem"))
            {
                bool isRoot = Tag.Equals("TreeRootItem");
                var treeItemElement = (element.FlavorElement as JavaTreeItemFlavorElement) ?? new JavaTreeItemFlavorElement(element.FlavorElement as AwtFlavorElement, new List<int>(), 0);
                object[] treeItems = null;
                object[] rootItems = null;
                if (isRoot)
                {
                    rootItems = element.Jow.InvokeGetSpecialProperty("treerootitem", Array.Empty<object>());
                }
                else
                {
                    treeItems = element.Jow.InvokeGetChildItems(treeItemElement.ItemPath);

                }
                int count = isRoot ? rootItems.Length : treeItems.Length;
                //有Index
                if (verifyIndex)
                {
                    if (count < Index + 1) return null;
                    JavaTreeItemFlavorElement itemElement = null;

                    if (isRoot)
                    {
                        itemElement = new JavaTreeItemFlavorElement(treeItemElement.Container, new List<int>(), Index);
                    }
                    else
                    {
                        List<int> treePath = new List<int>(treeItemElement.ItemPath)
                        {
                          Convert.ToInt32(treeItems[Index])
                        };
                        itemElement = new JavaTreeItemFlavorElement(treeItemElement.Container, treePath, Index);
                    }


                    var nodeText = itemElement.GetAttributeValue(null, "text")?.ToString();
                    if (!string.IsNullOrEmpty(Text))
                    {

                        //有Text 属性
                        if ((TextPattern == Pattern.CONTAIN.ToString().ToLower() && nodeText.Contains(Text)) ||
                            (TextPattern == Pattern.BEGIN.ToString().ToLower() && nodeText.StartsWith(Text)) ||
                            (TextPattern == Pattern.END.ToString().ToLower() && nodeText.EndsWith(Text)) ||
                            (TextPattern == Pattern.EQUAL.ToString().ToLower() && nodeText.Equals(Text)) ||
                            (TextPattern == Pattern.REGULAR.ToString().ToLower() && Regex.IsMatch(nodeText, Text))
                        )
                        {
                            return new Element(itemElement, element, Index, isRoot ? "TreeRootItem" : "TreeItem");
                        }
                        return null;
                    }
                    return new Element(itemElement, element, Index, isRoot ? "TreeRootItem" : "TreeItem");
                }
                else
                {
                    //没Index, 也没Text
                    if (string.IsNullOrEmpty(Text))
                    {
                        return null;
                    }

                    JavaTreeItemFlavorElement itemElement = null;
                    if (isRoot)
                    {
                        itemElement = new JavaTreeItemFlavorElement(treeItemElement.Container, new List<int>(), Index);
                    }
                    else
                    {
                        List<int> treePath = new List<int>(treeItemElement.ItemPath)
                        {
                          Convert.ToInt32(treeItems[Index])
                        };
                        itemElement = new JavaTreeItemFlavorElement(treeItemElement.Container, treePath, Index);
                    }

                    var nodeText = itemElement.GetAttributeValue(null, "text")?.ToString();

                    //有Text 属性
                    if ((TextPattern == Pattern.CONTAIN.ToString().ToLower() && nodeText.Contains(Text)) ||
                        (TextPattern == Pattern.BEGIN.ToString().ToLower() && nodeText.StartsWith(Text)) ||
                        (TextPattern == Pattern.END.ToString().ToLower() && nodeText.EndsWith(Text)) ||
                        (TextPattern == Pattern.EQUAL.ToString().ToLower() && nodeText.Equals(Text)) ||
                        (TextPattern == Pattern.REGULAR.ToString().ToLower() && Regex.IsMatch(nodeText, Text))
                    )
                    {
                        return new Element(itemElement, element, Index, isRoot ? "TreeRootItem" : "TreeItem");
                    }

                }
                return null;
            }
            else if (Tag.Equals("TabPage"))
            {
                var treeItemElement = element.FlavorElement as JavaTabFlavorElement;
                int itemCount = (int)element.Jow.InvokeGetProperty("getTabCount");
                //有Index
                if (verifyIndex)
                {
                    if (itemCount < Index + 1) return null;

                    var tabElement = new JavaTabFlavorElement(element.FlavorElement as AwtFlavorElement, Index);

                    var nodeText = tabElement.GetAttributeValue(null, "text")?.ToString();
                    var nodeTitle = tabElement.GetAttributeValue(null, "title")?.ToString();
                    if (!string.IsNullOrEmpty(Text))
                    {

                        //有Text 属性
                        if ((TextPattern == Pattern.CONTAIN.ToString().ToLower() && nodeText.Contains(Text)) ||
                            (TextPattern == Pattern.BEGIN.ToString().ToLower() && nodeText.StartsWith(Text)) ||
                            (TextPattern == Pattern.END.ToString().ToLower() && nodeText.EndsWith(Text)) ||
                            (TextPattern == Pattern.EQUAL.ToString().ToLower() && nodeText.Equals(Text)) ||
                            (TextPattern == Pattern.REGULAR.ToString().ToLower() && Regex.IsMatch(nodeText, Text))
                        )
                        {
                            return new Element(tabElement, element, Index, "TabPage");
                        }
                        //有Title 属性
                        if ((TextPattern == Pattern.CONTAIN.ToString().ToLower() && nodeTitle.Contains(Text)) ||
                            (TextPattern == Pattern.BEGIN.ToString().ToLower() && nodeTitle.StartsWith(Text)) ||
                            (TextPattern == Pattern.END.ToString().ToLower() && nodeTitle.EndsWith(Text)) ||
                            (TextPattern == Pattern.EQUAL.ToString().ToLower() && nodeTitle.Equals(Text)) ||
                            (TextPattern == Pattern.REGULAR.ToString().ToLower() && Regex.IsMatch(nodeTitle, Text))
                        )
                        {
                            return new Element(tabElement, element, Index, "TabPage");
                        }
                        return null;
                    }

                    return new Element(tabElement, element, Index, "TabPage");
                }
                else
                {
                    //没Index, 也没Text
                    if (string.IsNullOrEmpty(Text))
                    {
                        return null;
                    }
                    for (int i = 0; i < itemCount; i++)
                    {

                        var tabElement = new JavaTabFlavorElement(element.FlavorElement as AwtFlavorElement, i);
                        var nodeText = tabElement.GetAttributeValue(null, "text")?.ToString();
                        var nodeTitle = tabElement.GetAttributeValue(null, "title")?.ToString();

                        //有Text 属性
                        if ((TextPattern == Pattern.CONTAIN.ToString().ToLower() && nodeText.Contains(Text)) ||
                            (TextPattern == Pattern.BEGIN.ToString().ToLower() && nodeText.StartsWith(Text)) ||
                            (TextPattern == Pattern.END.ToString().ToLower() && nodeText.EndsWith(Text)) ||
                            (TextPattern == Pattern.EQUAL.ToString().ToLower() && nodeText.Equals(Text)) ||
                            (TextPattern == Pattern.REGULAR.ToString().ToLower() && Regex.IsMatch(nodeText, Text))
                        )
                        {
                            return new Element(tabElement, element, Index, "TabPage");
                        }
                        //有Title 属性
                        if ((TextPattern == Pattern.CONTAIN.ToString().ToLower() && nodeTitle.Contains(Text)) ||
                            (TextPattern == Pattern.BEGIN.ToString().ToLower() && nodeTitle.StartsWith(Text)) ||
                            (TextPattern == Pattern.END.ToString().ToLower() && nodeTitle.EndsWith(Text)) ||
                            (TextPattern == Pattern.EQUAL.ToString().ToLower() && nodeTitle.Equals(Text)) ||
                            (TextPattern == Pattern.REGULAR.ToString().ToLower() && Regex.IsMatch(nodeTitle, Text))
                        )
                        {
                            return new Element(tabElement, element, Index, "TabPage");
                        }

                    }
                }
                return null;
            }
            else if (Tag.Equals("ListItem"))
            {
                var listItemElement = element.FlavorElement as JavaListItemFlavorElement;
                int itemCount = element.MappedClazz == "javax.swing.JList" ? (int)element.Jow.InvokeGetSpecialProperty("listitemcount", Array.Empty<object>())[0] : (int)element.Jow.InvokeMethod("getItemCount", Array.Empty<object>())[0];
                //有Index
                if (verifyIndex)
                {
                    if (itemCount < Index + 1) return null;

                    var listElement = new JavaListItemFlavorElement(element.FlavorElement as AwtFlavorElement, Index);

                    var nodeText = listElement.GetAttributeValue(null, "text")?.ToString();
                    if (!string.IsNullOrEmpty(Text))
                    {

                        //有Text 属性
                        if ((TextPattern == Pattern.CONTAIN.ToString().ToLower() && nodeText.Contains(Text)) ||
                            (TextPattern == Pattern.BEGIN.ToString().ToLower() && nodeText.StartsWith(Text)) ||
                            (TextPattern == Pattern.END.ToString().ToLower() && nodeText.EndsWith(Text)) ||
                            (TextPattern == Pattern.EQUAL.ToString().ToLower() && nodeText.Equals(Text)) ||
                            (TextPattern == Pattern.REGULAR.ToString().ToLower() && Regex.IsMatch(nodeText, Text))
                        )
                        {
                            return new Element(listElement, element, Index, "ListItem");
                        }
                        return null;
                    }
                    return new Element(listElement, element, Index, "ListItem");
                }
                else
                {
                    //没Index, 也没Text
                    if (string.IsNullOrEmpty(Text))
                    {
                        return null;
                    }
                    for (int i = 0; i < itemCount; i++)
                    {

                        var listElement = new JavaListItemFlavorElement(element.FlavorElement as AwtFlavorElement, Index);
                        var nodeText = listElement.GetAttributeValue(null, "text")?.ToString();

                        //有Text 属性
                        if ((TextPattern == Pattern.CONTAIN.ToString().ToLower() && nodeText.Contains(Text)) ||
                            (TextPattern == Pattern.BEGIN.ToString().ToLower() && nodeText.StartsWith(Text)) ||
                            (TextPattern == Pattern.END.ToString().ToLower() && nodeText.EndsWith(Text)) ||
                            (TextPattern == Pattern.EQUAL.ToString().ToLower() && nodeText.Equals(Text)) ||
                            (TextPattern == Pattern.REGULAR.ToString().ToLower() && Regex.IsMatch(nodeText, Text))
                        )
                        {
                            return new Element(listElement, element, Index, "ListItem");
                        }
                    }
                }
                return null;
            }
            else
            {
                var children = element.Jow.InvokeGetChildren();
                int index = 0;
                if (verifyIndex)
                {
                    if (children.Count < Index + 1) return null;
                    var awtElement = new AwtFlavorElement(children[Index], element.FlavorElement, element.FlavorElement, index);
                    var _element = new Element(awtElement, awtElement.MapClassToRole(children[Index].Lineage, out string mappedClazz), mappedClazz, element, children[Index], Index, awtElement.FlavorName);
                    var _tag = mappedClazz.Substring(mappedClazz.LastIndexOf(".") + 1);
                    if (!_tag.Equals(Tag)) return null;
                    return _element;
                }
                else
                {
                    for (int i = 0; i < children.Count; i++)
                    {
                        var awtElement = new AwtFlavorElement(children[i], element.FlavorElement, element.FlavorElement, index);
                        var _element = new Element(awtElement, awtElement.MapClassToRole(children[i].Lineage, out string mappedClazz), mappedClazz, element, children[i], Index, awtElement.FlavorName);
                        var _tag = mappedClazz.Substring(mappedClazz.LastIndexOf(".") + 1);
                        if (_tag.Equals(Tag))
                            return _element;

                    }
                    return null;
                }

            }
        }
        public Element FindElementByPath(JArray selectorArray, JavaProxy javaProxy)
        {
            try
            {
                Element element = null;
                var windows = javaProxy.InvokeGetWindows();
                var windowElement = new List<Element>();
                if (windows != null)
                {
                    foreach (var item in windows)
                    {
                        var javaObj = javaProxy.InvokeGetAwtWindow(int.Parse(item?.ToString()), out string[] awtWindows);
                        if (javaObj != null)
                        {
                            var wndElement = new AwtWindowFlavorElement(javaObj, awtWindows, new Win32FlavorElement(IntPtr.Zero));
                            if (!wndElement.Visible)
                            {
                                continue;
                            }
                            Element _element = new Element(wndElement, wndElement.MapClassToRole(javaObj.Lineage, out string clazz), clazz, null, javaObj, 0, wndElement.FlavorName);
                            windowElement.Add(_element);
                        }
                    }
                    for (int i = 0; i < selectorArray.Count; i++)
                    {
                        var currentSegment = selectorArray[i];
                        string className = currentSegment["name"]?.ToString();
                        var props = currentSegment["props"] as JArray;
                        if (i == 0)
                        {
                            var titleProp = props?.FirstOrDefault(p => p["name"]?.ToString() == "text").Value<string>();
                            for (int n = 0; n < windowElement.Count; n++)
                            {
                                var title = windowElement[n].FlavorElement.GetAttributeValue(element, "title");
                                if ((title ?? "").ToString().Contains(titleProp) && windowElement[n].MappedClazz.Contains(className))
                                {
                                    element = windowElement[n];
                                    break;
                                }
                            }
                        }
                        else
                        {
                            element = FindElementChild(currentSegment, element);
                            if (element == null)
                            {
                                logger.LogError($"child index {i},class:{className} Not Match");
                                return null;
                            }
                        }
                    }
                }
                return element;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return null;
            }

        }

        public JArray GetElementSelector(JavaNode node)
        {
            var xpath = node.JavaElement.GetElementPath();
            if (string.IsNullOrEmpty(xpath))
            {
                return null;
            }
            var result = new List<Core.Commons.SelectorProps>();

            // 1. 拆分路径段，例如 /JFrame[...] -> JFrame[...]
            // 使用正则的负向先行断言（Negative Lookahead），仅当 "/" 没有处于 [] 内部时才进行分割
            string[] segments = Regex.Split(xpath, @"/(?![^\[]*\])")
                                     .Where(s => !string.IsNullOrEmpty(s))
                                     .ToArray();

            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                var propNode = new Core.Commons.SelectorProps();
                propNode.Props = new List<Core.Commons.Props>();

                // 2. 正则解析类名和方括号内的属性
                // 匹配格式: ClassName[attr1=val1,attr2=val2]
                var match = Regex.Match(seg, @"^(?<className>[^\[]+)(?:\[(?<attrs>.*)\])?$");

                if (match.Success)
                {
                    propNode.Name = match.Groups["className"].Value;
                    string attrsContent = match.Groups["attrs"].Value;

                    if (string.IsNullOrEmpty(attrsContent))
                    {
                        // 规则：没有属性时，添加 @index, accurate 为 false
                        propNode.Props.Add(new Props { Name = "@index", Value = "0", Accurate = "false", Pattern = "equal" });
                    }
                    else
                    {
                        // 3. 解析逗号分隔的属性对（尊重转义逗号，避免单元格文本含逗号时被截断）
                        string[] attrPairs = SplitXPathAttributes(attrsContent);
                        foreach (var pair in attrPairs)
                        {
                            string[] kv = pair.Split('=');
                            if (kv.Length != 2) continue;

                            string key = kv[0].Trim();
                            string val = kv[1].Trim();
                            var _key = key.Equals("index") ? "@index" : key;

                            string pattern = "equal";
                            if (val.Contains("/"))
                            {
                                val = val.Substring(0, val.IndexOf('/'));
                                pattern = "contain";
                            }

                            var prop = new Props { Name = _key, Value = val, Pattern = pattern, Accurate = "true" };

                            propNode.Props.Add(prop);
                        }

                        var textProp = propNode.Props.FirstOrDefault(p => p.Name == "text");
                        if (textProp != null && !string.IsNullOrEmpty(textProp.Value))
                        {
                            var indexProp = propNode.Props.FirstOrDefault(p => p.Name == "@index");
                            if (indexProp != null)
                            {
                                indexProp.Accurate = "false";
                            }
                            //第一层节点，也就是窗口节点，如果 有 text ,且值在 JavaConfigrations.Info.WindowMap 中，将text 映射为newTitle, 且 pattern 改为 contains
                            if (i == 0)
                            {
                                propNode.Props.Add(new Props { Name = "tech", Value = ((int)node.JavaElement.Tech).ToString(), Accurate = "true", Pattern = "equal" });
                                if (JavaConfigurations.Info.WindowMaps != null)
                                {
                                    var windowMap = JavaConfigurations.Info.WindowMaps.FirstOrDefault(m => m.Title == textProp.Value);
                                    if (windowMap != null)
                                    {
                                        textProp.Value = windowMap.NewTitle;
                                        textProp.Pattern = "contain";
                                    }
                                }
                                var nameProp = propNode.Props.FirstOrDefault(p => p.Name == "name");
                                if (nameProp != null)
                                {
                                    nameProp.Accurate = "false";
                                }
                            }
                        }
                    }
                }
                result.Add(propNode);
            }

            return JArray.FromObject(result);

        }

        /// <summary>
        /// 拆分 XPath 方括号内的属性对，尊重用反斜杠转义的逗号/右括号。
        /// Java 侧 buildXPath 会把 text/name 值中的 "," 转义为 "\,"、" ]" 转义为 "\]"，
        /// 这里按“未被转义的逗号”切分，并把 "\,"、"\ ]" 还原为原字符，避免单元格文本含逗号时被截断。
        /// </summary>
        private static string[] SplitXPathAttributes(string attrsContent)
        {
            var parts = new List<string>();
            var sb = new StringBuilder();
            for (int i = 0; i < attrsContent.Length; i++)
            {
                char c = attrsContent[i];
                if (c == '\\' && i + 1 < attrsContent.Length)
                {
                    char next = attrsContent[i + 1];
                    if (next == ',' || next == ']')
                    {
                        sb.Append(next);
                        i++;
                        continue;
                    }
                }
                if (c == ',')
                {
                    parts.Add(sb.ToString());
                    sb.Length = 0;
                    continue;
                }
                sb.Append(c);
            }
            parts.Add(sb.ToString());
            return parts.ToArray();
        }

        public string ConvertSelectorToXPath(JArray nodes, out int tech)
        {
            tech = 0;
            if (nodes == null || nodes.Count == 0) return "";

            StringBuilder xpathBuilder = new StringBuilder();

            //定位属性不参与匹配，去掉
            bool isFirstNode = true;

            foreach (JObject node in nodes)
            {
                if (node["accurate"]?.ToString().Equals("false") == true) continue;

                xpathBuilder.Append("/");

                xpathBuilder.Append(node["name"]?.ToString());

                var props = node["props"] as JArray;

                if (props != null && props.Count > 0)
                {
                    xpathBuilder.Append("[");
                    List<string> propStrings = new List<string>();

                    foreach (JObject prop in props)
                    {
                        if (prop["accurate"]?.ToString().Equals("false") == true) continue;
                        if (isFirstNode)
                        {
                            string name = prop["name"]?.ToString();
                            if (name.Equals("identity") == true)
                            {
                                continue;
                            }
                            if (name.Equals("tech") == true)
                            {
                                int.TryParse(prop["value"]?.ToString(), out tech);
                                continue;
                            }
                        }

                        propStrings.Add(ConvertPropToXPathPredicate(prop));
                    }

                    xpathBuilder.Append(string.Join(",", propStrings));
                    xpathBuilder.Append("]");
                }
                if (isFirstNode) isFirstNode = false;
            }

            return xpathBuilder.ToString();
        }

        //根据匹配符号转成xpath 的属性谓词
        private string ConvertPropToXPathPredicate(JObject prop)
        {
            string propName = prop["name"]?.ToString().Replace("@", "") ?? "";

            string propValue = prop["value"]?.ToString() ?? "";

            propValue = propValue.Trim('\'');

            string pattern = prop["pattern"]?.ToString().ToLower() ?? "equal";

            switch (pattern)
            {
                case "equal":
                    return $"{propName}='{propValue}'";
                case "contain":
                    return $"{propName}*='{propValue}'";
                case "begin":
                    return $"{propName}^='{propValue}'";
                case "end":
                    return $"{propName}$='{propValue}'";
                case "regular":
                    return $"{propName}=~'{propValue}'";
                default:
                    return $"{propName}='{propValue}'"; // 默认等于
            }
        }

        //新版无插件
        internal JavaElement GetElementFromSelectorV2(JArray selectorArray, JavaProxy javaProxy)
        {
            logger.LogInformation("GetElementFromSelectorV2");
            try
            {
                var xpath = ConvertSelectorToXPath(selectorArray, out int tech);
                logger.LogDebug($"Converted XPath: {xpath}");
                if (string.IsNullOrEmpty(xpath)) return null;
                var hitResult = javaProxy.CallFunction(JavaTech.DontCare, FunctionId.FuncQueryElementFromXpath, new object[] { xpath, tech }, out CallStatus status);
                if (status == CallStatus.Ok)
                {
                    if (hitResult[0] != null && hitResult.Length > 3)
                    {
                        var elementObj = hitResult[0] as JavaObjectWrapper;
                        var rect = hitResult[1] is Rectangle r ? r : Rectangle.Empty;
                        var mode = Convert.ToInt32(hitResult[hitResult.Length - 1]);
                        var _tech = (JavaTech)Convert.ToInt32(hitResult[hitResult.Length - 2]);
                        elementObj.Tech = _tech;
                        var javaElement = new JavaElement(elementObj, rect)
                        {
                            MatchedCount = Convert.ToInt32((hitResult.Length - 2) / mode),
                            QueryPath = xpath // 存储用于滚动后RefreshBoundingRectangle重新查询
                        };
                        return javaElement;


                    }
                    else
                    {
                        logger.LogError($"element not found,message:{status},length:{hitResult.Length}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return null;

        }

        public JavaProxy GetJavaProxy(JArray selector)
        {
            var windowTitle = selector.SelectToken("$[0].props[?(@.name == 'text')].value")?.ToString();
            var identity = selector.SelectToken("$[0].props[?(@.name == 'identity')].value")?.ToString();

            var pid = javaProcessLocator.LocatePid(identity, windowTitle);
            logger.LogInformation($"GetJavaProxy pid:{pid} for window title:{windowTitle}");
            if (pid != 0)
            {
                if (JavaProxyes.TryGetValue(pid, out JavaProxy existingProxy))
                {
                    return existingProxy;
                }
                else
                {
                    if (CrateJavaProxy(pid))
                    {
                        return JavaProxyes[pid];
                    }
                    else
                    {
                        logger.LogWarning($"InitJavaService failed for pid:{pid}");
                        return null;
                    }
                }
            }
            return null;

        }
    }
}
