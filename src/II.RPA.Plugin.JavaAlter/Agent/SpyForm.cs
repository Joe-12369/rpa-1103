using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    public partial class SpyForm : Form
    {

        public static bool Is64Bit(Process process)
        {
            return false;
        }

        public SpyForm()
        {
            InitializeComponent();
            Instance = this;
            InitService();
        }
        public Process[] JVM;
        public static SpyForm Instance;
        static readonly Regex RegNode = new Regex(@"^(?<tag>\w+?)\[(?:index=(?<index>\d+))?.*?(?:text=(?<text>.+))?\]$", RegexOptions.Multiline | RegexOptions.Singleline);
        static readonly Regex RegRoot = new Regex(@"^(?<tag>\w+?)\[title=(?<title>.*)\]$", RegexOptions.Multiline | RegexOptions.Singleline);
        Func<Point, TreeNode, TreeNode> FindNodeFromPoint;
        Func<string, Element, Element> ParseElementPath = new Func<string, Element, Element>((path, element) =>
        {
            try
            {
                var nodeInfo = RegNode.Match(path?.Trim());
                if (!nodeInfo.Success)
                    return null;
                var Tag = nodeInfo.Groups["tag"].Value?.Trim();
                if (string.IsNullOrEmpty(Tag))
                {
                    return null;
                }
                var Text = nodeInfo.Groups["text"].Value?.Trim();
                var verifyIndex = int.TryParse(nodeInfo.Groups["index"].Value?.Trim(), out int Index);
                if (!verifyIndex && string.IsNullOrEmpty(Text))
                    return null;
                if (Tag.Equals("Row"))
                {
                    if (element.MappedClazz == "javax.swing.JTable")
                    {
                        var rows = element.Jow.InvokeMethod("getRowCount", Array.Empty<object>());

                        if (int.TryParse(rows[0].ToString(), out int rowCount))
                        {
                            if (rowCount < Index) return null;
                            var rowElement = new JavaRowFlavorElement(element.FlavorElement as AwtFlavorElement, Index - 1);
                            rowElement.ElementPath = $"{element.FlavorElement.ElementPath}/Row[index={Index}]";
                            return new Element(rowElement, element);
                        }
                    }
                    return null;
                }
                else if (Tag.Equals("Column"))
                {
                    if (element.MappedClazz == "javax.swing.JTable")
                    {
                        var columns = element.Jow.InvokeMethod("getColumnCount", Array.Empty<object>());
                        if (int.TryParse(columns[0].ToString(), out int colCount))
                        {
                            if (colCount < Index) return null;
                            var colElement = new JavaColumnFlavorElement(element.FlavorElement as AwtFlavorElement, Index - 1);
                            colElement.ElementPath = $"{element.FlavorElement.ElementPath}/Column[index={Index}]";
                            return new Element(colElement, element);
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
                        if (colCount < Index) return null;

                        var cellElement = new JavaCellFlavorElement(javaRowFlavorElement.Container, javaRowFlavorElement.RowIndex, Index - 1);
                        cellElement.ElementPath = $"{element.FlavorElement.ElementPath}/Cell[index={Index}]";
                        return new Element(cellElement, element);

                    }
                    JavaColumnFlavorElement javaColumnFlavorElement = element.FlavorElement as JavaColumnFlavorElement;
                    if (javaColumnFlavorElement != null)
                    {
                        int rowCount = (int)javaColumnFlavorElement.Container.JavaObject.InvokeMethod("getRowCount", Array.Empty<object>())[0];
                        if (rowCount < Index) return null;

                        var cellElement = new JavaCellFlavorElement(javaColumnFlavorElement.Container, Index, javaColumnFlavorElement.ColumnIndex);
                        cellElement.ElementPath = $"{element.FlavorElement.ElementPath}/Cell[index={Index}]";
                        return new Element(cellElement, element);
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
                            headerElement = new JavaHeaderCellFlavorElement(element.FlavorElement as AwtFlavorElement, table, Index - 1);
                            headerElement.ElementPath = $"{element.FlavorElement.ElementPath}/HeaderCell[index={Index}]";
                            return new Element(headerElement, element);
                        }

                    }
                    return null;
                }
                else if (Tag.Equals("TreeItem"))
                {
                    var text = nodeInfo.Groups["text"];
                    var treeItemElement = (element.FlavorElement as JavaTreeItemFlavorElement) ?? new JavaTreeItemFlavorElement(element.FlavorElement as AwtFlavorElement, new List<int>(), 0);
                    object[] array = element.Jow.InvokeGetChildItems(treeItemElement.ItemPath);
                    //有Index
                    if (verifyIndex)
                    {
                        if (array.Length < Index) return null;

                        List<int> treePath = new List<int>(treeItemElement.ItemPath)
                        {
                          Convert.ToInt32(array[Index-1])
                        };
                        var itemElement = new JavaTreeItemFlavorElement(treeItemElement.Container, treePath, Index - 1);
                        itemElement.ElementPath = $"{element.FlavorElement.ElementPath}/TreeItem[index={Index}]";
                        var nodeText = itemElement.GetAttributeValue(null, "text").ToString();
                        if (!string.IsNullOrEmpty(Text))
                        {

                            //有Text 属性
                            if (Text.EndsWith("*") && Text.EndsWith("*"))
                            {
                                if (nodeText.Contains(Text.Trim(new char[] { '*' })))
                                {
                                    return new Element(itemElement, element);
                                }
                            }
                            if (Text.StartsWith("*"))
                            {
                                if (nodeText.EndsWith(Text.TrimStart(new char[] { '*' })))
                                {
                                    return new Element(itemElement, element);
                                }
                            }
                            if (Text.EndsWith("*"))
                            {
                                if (nodeText.StartsWith(Text.TrimEnd(new char[] { '*' })))
                                {
                                    return new Element(itemElement, element);
                                }
                            }
                            if (string.Equals(Text, nodeText?.Trim()))
                            {
                                return new Element(itemElement, element);
                            }
                            return null;
                        }
                        return new Element(itemElement, element);
                    }
                    else
                    {
                        //没Index, 也没Text
                        if (string.IsNullOrEmpty(Text))
                        {
                            return null;
                        }
                        int itemIndex = 0;
                        foreach (int item in array)
                        {
                            List<int> treePath = new List<int>(treeItemElement.ItemPath)
                            {
                             item
                            };
                            var itemElement = new JavaTreeItemFlavorElement(treeItemElement.Container, treePath, itemIndex++);
                            itemElement.ElementPath = $"{element.FlavorElement.ElementPath}/TreeItem[index={itemIndex}]";
                            var nodeText = itemElement.GetAttributeValue(null, "text").ToString();
                            //有Text 属性
                            if (Text.EndsWith("*") && Text.EndsWith("*"))
                            {
                                if (nodeText.Contains(Text.Trim(new char[] { '*' })))
                                {
                                    return new Element(itemElement, element);
                                }
                            }
                            if (Text.StartsWith("*"))
                            {
                                if (nodeText.EndsWith(Text.TrimStart(new char[] { '*' })))
                                {
                                    return new Element(itemElement, element);
                                }
                            }
                            if (Text.EndsWith("*"))
                            {
                                if (nodeText.StartsWith(Text.TrimEnd(new char[] { '*' })))
                                {
                                    return new Element(itemElement, element);
                                }
                            }
                            if (string.Equals(Text, nodeText?.Trim()))
                            {
                                return new Element(itemElement, element);
                            }
                        }
                    }
                    return null;
                }
                else
                {
                    var children = element.Jow.InvokeGetChildren();
                    int index = 0;
                    if (children.Count < Index) return null;

                    var awtElement = new AwtFlavorElement(children[Index - 1], element.FlavorElement, element.FlavorElement, index - 1);
                    var _element = new Element(awtElement, awtElement.MapClassToRole(children[Index - 1].Lineage, out string mappedClazz), mappedClazz, element, children[Index - 1]);
                    var _tag = mappedClazz.Substring(mappedClazz.LastIndexOf(".") + 1);
                    if (!_tag.Equals(Tag)) return null;

                    awtElement.ElementPath = $"{element.FlavorElement.ElementPath}/{mappedClazz.Substring(mappedClazz.LastIndexOf(".") + 1)}[index={Index}]";
                    return _element;
                }

            }
            catch (Exception ex)
            {
                return null;
            }
        });

        private void InitService()
        {

        }
        private void GetJavaProcess()
        {
            JVM = Process.GetProcessesByName("javaw");
        }
        private bool CrateJavaProxy(int jvmId, out JavaProxy javaProxy)
        {
            var proxy = JavaProxy.Create(jvmId);
            if (proxy != null)
            {
                statusMsg.Text = "已链接 Java 程序";
                statusMsg.ForeColor = Color.Green;
                javaProxy = proxy;
                return true;
            }
            javaProxy = null;
            return false;
        }
        public void InjectJavaAgent(Process process)
        {
            try
            {
                var jrePath = AppDomain.CurrentDomain.BaseDirectory;
                var platform = Is64Bit(process) ? "" : "32";
                var processInfo = new ProcessStartInfo($"{Path.GetDirectoryName(process.MainModule.FileName)}\\java.exe", $" -jar {jrePath}bootstrap.jar {process.Id}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process proc;
                if ((proc = Process.Start(processInfo)) == null)
                {
                    throw new InvalidOperationException("??");
                }
                proc.WaitForExit();
                int exitCode = proc.ExitCode;
                proc.Close();
            }
            catch (Exception ex)
            {
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "agent.log", $"{ex.Message},{ex.StackTrace}");
            }
        }
        private void RetriveJavaWindow(IntPtr hwnd, JavaProxy proxy)
        {
            elementTree.Nodes.Clear();
            var windows = proxy.InvokeGetWindows();
            if (windows != null)
            {
                foreach (var item in windows)
                {
                    var javaObj = proxy.InvokeGetAwtWindow(int.Parse(item.ToString()), out string[] awtWindows);
                    if (javaObj != null)
                    {
                        var wndElement = new AwtWindowFlavorElement(javaObj, awtWindows, new Win32FlavorElement(hwnd));
                        Element element = new Element(wndElement, wndElement.MapClassToRole(javaObj.Lineage, out string clazz), clazz, null, javaObj);
                        AddNode(element, null);
                    }
                }
            }

        }
        private void RefreshJavaApplication()
        {
            try
            {
                Invoke(new Action(() =>
                {
                    elementTree.Nodes.Clear();
                }));
                GetJavaProcess();
                if (JVM.Length > 0)
                {
                    foreach (var jvm in JVM)
                    {
                        jvm.EnableRaisingEvents = true;
                        jvm.Exited += Jvm_Exited;
                        var hasConnect = CrateJavaProxy(jvm.Id, out JavaProxy proxy);
                        if (!hasConnect)
                        {
                            InjectJavaAgent(jvm);
                            var connected = CrateJavaProxy(jvm.Id, out proxy);
                            if (!connected)
                            {
                                statusMsg.Text = "创建Java 程序代理失败";

                                statusMsg.ForeColor = Color.Red;
                                continue;
                            }
                        }
                        Task.Run(() =>
                        {
                            RetriveJavaWindow(jvm.MainWindowHandle, proxy);
                        }).ContinueWith(t =>
                        {
                            Invoke(new Action(() =>
                            {
                                if (elementTree.Nodes.Count > 0)
                                {
                                    elementTree.SelectedNode = elementTree.Nodes[0];
                                    elementTree.CollapseAll();
                                }
                            }));

                        });

                    }
                }
                else
                {
                    statusMsg.Text = "未发现java程序";
                    statusMsg.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                Invoke(new Action(() =>
                {
                    statusMsg.Text = ex.Message;
                    statusMsg.ForeColor = Color.Red;
                }));

            }

        }
        private void Jvm_Exited(object sender, EventArgs e)
        {
            RefreshJavaApplication();
        }
        private void CaptureScreen(Rectangle rectangle)
        {
            try
            {
                Bitmap _bitMap = new Bitmap(rectangle.Width, rectangle.Height);
                Graphics g = Graphics.FromImage(_bitMap);
                g.CopyFromScreen(rectangle.X, rectangle.Y, 0, 0, new Size(rectangle.Width, rectangle.Height));
                elementImg.Image = _bitMap;
                g.Dispose();
            }
            catch (Exception)
            {

                elementImg.Image = null;
            }


        }
        private void AddNode(Element element, TreeNode parent)
        {
            var title = element.FlavorElement.GetAttributeValue(element, "title");
            var text = element.FlavorElement.GetAttributeValue(element, "text");
            var tag = element.Role == Role.Unknown ? element.FlavorName : element.Role.ToString();
            if (parent == null)
            {
                Invoke(new Action(() =>
                {
                    var node = elementTree.Nodes.Add($"{tag} {title ?? text}");
                    element.Form = node;
                    node.Tag = element;
                    if (!element.FlavorElement.Visible)
                    {
                        //node.ForeColor = Color.DarkGray;
                        node.Remove();
                    }
                    element.FlavorElement.ElementPath = $"{element.MappedClazz.Substring(element.MappedClazz.LastIndexOf(".") + 1)}[title={title}]";
                }));
            }
            else
            {
                Invoke(new Action(() =>
                {
                    var node = parent.Nodes.Add($"{tag} {title ?? text}");
                    element.Form = (parent.Tag as Element).Form;
                    node.Tag = element;
                    if (!element.FlavorElement.Visible)
                    {
                        //node.ForeColor = Color.DarkGray;
                        node.Remove();
                    }
                }));
            }
        }
        private void SearchChildNode(TreeNode node)
        {
            try
            {
                elementAttr.Clear();

                var element = node.Tag as Element;
                elementAttr.AppendText($"Class Name: {element.MappedClazz}\n");
                elementAttr.AppendText($"Visible: {element.FlavorElement.Visible}\n");
                elementAttr.AppendText($"Text: {element.FlavorElement.GetAttributeValue(element, "title") ?? element.FlavorElement.GetAttributeValue(element, "text")}\n");
                elementAttr.AppendText($"X: {element.FlavorElement.ElementRectangle.X}\nY: {element.FlavorElement.ElementRectangle.Y}\nWidth: {element.FlavorElement.ElementRectangle.Width}\nHeight: {element.FlavorElement.ElementRectangle.Height}");

                //if (element.FlavorElement is JavaCellFlavorElement || element.FlavorElement is JavaHeaderCellFlavorElement)
                //    return;
                JavaRowFlavorElement javaRowFlavorElement = element.FlavorElement as JavaRowFlavorElement;
                if (javaRowFlavorElement != null)
                {
                    int colCount = (int)javaRowFlavorElement.Container.JavaObject.InvokeMethod("getColumnCount", Array.Empty<object>())[0];
                    for (int c = 0; c < colCount; c++)
                    {
                        var cellElement = new JavaCellFlavorElement(javaRowFlavorElement.Container, javaRowFlavorElement.RowIndex, c)
                        {
                            ElementPath = $"{element.FlavorElement.ElementPath}/Cell[index={c + 1}]"
                        };
                        AddNode(new Element(cellElement, element), node);
                    }

                    return;
                }
                JavaColumnFlavorElement javaColumnFlavorElement = element.FlavorElement as JavaColumnFlavorElement;
                if (javaColumnFlavorElement != null)
                {
                    int rowCount = (int)javaColumnFlavorElement.Container.JavaObject.InvokeMethod("getRowCount", Array.Empty<object>())[0];
                    for (int r = 0; r < rowCount; r++)
                    {
                        var cellElement = new JavaCellFlavorElement(javaColumnFlavorElement.Container, r, javaColumnFlavorElement.ColumnIndex);
                        cellElement.ElementPath = $"{element.FlavorElement.ElementPath}/Cell[index={r + 1}]";
                        AddNode(new Element(cellElement, element), node);
                    }

                    return;
                }

                if (element.FlavorElement is JavaTreeItemFlavorElement javaTreeItemFlavorElement)
                {
                    object[] array = element.Jow.InvokeGetChildItems(javaTreeItemFlavorElement.ItemPath);
                    int itemCount = 0;
                    foreach (int item in array)
                    {
                        List<int> treePath = new List<int>(javaTreeItemFlavorElement.ItemPath)
                        {
                          item
                        };
                        var itemElement = new JavaTreeItemFlavorElement(javaTreeItemFlavorElement.Container, treePath, itemCount++);
                        itemElement.ElementPath = $"{element.FlavorElement.ElementPath}/TreeItem[index={itemCount},text={itemElement.GetAttributeValue(null, "text")}]";
                        AddNode(new Element(itemElement, element), node);
                    }
                    return;
                }
                var children = element.Jow.InvokeGetChildren();
                int index = 0;
                foreach (var item in children)
                {
                    var awtElement = new AwtFlavorElement(item, element.FlavorElement, element.FlavorElement, index++);
                    var _element = new Element(awtElement, awtElement.MapClassToRole(item.Lineage, out string mappedClazz), mappedClazz, element, item);
                    awtElement.ElementPath = $"{element.FlavorElement.ElementPath}/{mappedClazz.Substring(mappedClazz.LastIndexOf(".") + 1)}[index={index}]";
                    AddNode(_element, node);
                }

                if (element.MappedClazz == "javax.swing.JTable")
                {
                    var rows = element.Jow.InvokeMethod("getRowCount", Array.Empty<object>());

                    if (int.TryParse(rows[0].ToString(), out int rowCount))
                    {
                        for (int r = 0; r < rowCount; r++)
                        {
                            var rowElement = new JavaRowFlavorElement(element.FlavorElement as AwtFlavorElement, r)
                            {
                                ElementPath = $"{element.FlavorElement.ElementPath}/Row[index={r + 1}]"
                            };
                            AddNode(new Element(rowElement, element), node);
                        }
                        var columns = element.Jow.InvokeMethod("getColumnCount", Array.Empty<object>());
                        if (int.TryParse(columns[0].ToString(), out int colCount))
                        {
                            for (int c = 0; c < colCount; c++)
                            {
                                var colElement = new JavaColumnFlavorElement(element.FlavorElement as AwtFlavorElement, c);
                                colElement.ElementPath = $"{element.FlavorElement.ElementPath}/Column[index={c + 1}]";
                                AddNode(new Element(colElement, element), node);
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
                            headerElement.ElementPath = $"{element.FlavorElement.ElementPath}/HeaderCell[index={c + 1}]";
                            AddNode(new Element(headerElement, element), node);
                        }
                    }
                }
                if (element.MappedClazz == "javax.swing.JTree")
                {
                    var treeItemElement = new JavaTreeItemFlavorElement(element.FlavorElement as AwtFlavorElement, new List<int>(), 0);
                    object[] array = element.Jow.InvokeGetChildItems(treeItemElement.ItemPath);
                    int itemCount = 0;
                    foreach (int item in array)
                    {
                        List<int> treePath = new List<int>(treeItemElement.ItemPath)
                        {
                          item
                        };
                        var itemElement = new JavaTreeItemFlavorElement(treeItemElement.Container, treePath, itemCount++);
                        itemElement.ElementPath = $"{element.FlavorElement.ElementPath}/TreeItem[index={itemCount},text={itemElement.GetAttributeValue(null, "text")}]";
                        AddNode(new Element(itemElement, element), node);
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.StackTrace);
                statusMsg.Text = ex.Message;
                statusMsg.ForeColor = Color.Red;
            }

        }
        private void FindElementFromPoint(Point point)
        {
            bool hasElement = false;

            //todo: 多个窗口重叠，需要识别鼠标在哪个窗体上的逻辑，暂时不做

            #region 
            //for (int i = 0; i < elementTree.Nodes.Count; i++)
            //{
            //    var element = elementTree.Nodes[i].Tag as Element;
            //    if (element.FlavorElement.ElementRectangle.Contains(point) && element.FlavorElement.Visible)
            //    {
            //        var node = FindNodeFromPoint(point, elementTree.Nodes[i]);
            //        hasElement = true;
            //        Invoke(new Action(() =>
            //        {
            //            elementTree.SelectedNode = node;
            //            node.ForeColor = Color.White;
            //            node.BackColor = Color.Green;
            //        }));
            //        break;
            //    }
            //}
            #endregion

            TreeNode elementNode = elementTree.Nodes.Count < 1 ? null : elementTree.Nodes.Count > 1 ? elementTree.SelectedNode == null ? elementTree.Nodes[0] : elementTree.SelectedNode : elementTree.Nodes[0];

            if (elementNode != null)
            {
                Element element = elementNode.Tag as Element;
                var formElement = element.Form.Tag as Element;
                if (formElement.FlavorElement.ElementRectangle.Contains(point) && formElement.FlavorElement.Visible)
                {   
                    var node = FindNodeFromPoint(point, element.Form);
                    hasElement = true;
                    Invoke(new Action(() =>
                    {
                        elementTree.SelectedNode = node;
                        node.ForeColor = Color.White;
                        node.BackColor = Color.Green;
                    }));
                    statusMsg.Text = "😀";
                    statusMsg.ForeColor = Color.Green;
                }
            }

            if (!hasElement)
            {
                statusMsg.Text = "未找到元素(鼠标点击上方其他【Form】节点然后重试)";
                statusMsg.ForeColor = Color.Red;
            }

        }
        private bool FindElementFromPath(string path, out Element result)
        {
            var nodes = path.Split('/');
            result = null;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (i == 0)
                {
                    var wndInfo = RegRoot.Match(nodes[i]?.Trim());
                    if (wndInfo.Success)
                    {
                        for (int n = 0; n < elementTree.Nodes.Count; n++)
                        {
                            var element = elementTree.Nodes[n].Tag as Element;
                            var title = element.FlavorElement.GetAttributeValue(element, "title");
                            if ((title ?? "").ToString().Contains(wndInfo.Groups["title"].Value) && element.MappedClazz.Contains(wndInfo.Groups["tag"].Value))
                            {
                                result = element;
                                break;
                            }
                        }
                    }
                    else
                        return false;
                }
                else
                {
                    result = ParseElementPath(nodes[i], result);
                    if (result == null)
                        return false;
                }
            }
            if (result == null) return false;
            return true;
        }

        #region Form Action

        private void FormLoad(object sender, EventArgs e)
        {
            var cur = AppDomain.CurrentDomain.BaseDirectory + "magnify.cur";
            if (File.Exists(cur))
            {
                traceButton.Cursor = new Cursor(cur);
            }
            elementImg.SizeMode = PictureBoxSizeMode.Zoom;
            RefreshJavaApplication();

            FindNodeFromPoint = new Func<Point, TreeNode, TreeNode>((point, node) =>
            {
                node.Nodes.Clear();
                SearchChildNode(node);
                for (int i = 0; i < node.Nodes.Count; i++)
                {
                    var element = node.Nodes[i].Tag as Element;
                    if (element.FlavorElement.ElementRectangle.Contains(point) && element.FlavorElement.Visible)
                    {
                        return FindNodeFromPoint(point, node.Nodes[i]);
                    }
                }
                return node;
            });
        }
        private void refreshButton_Click(object sender, EventArgs e)
        {
            elementTree.Nodes.Clear();
            RefreshJavaApplication();
        }
        private void elementTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var selectedElement = elementTree.SelectedNode.Tag as Element;
            CaptureScreen(selectedElement.FlavorElement.ElementRectangle);
            elementPath.Text = selectedElement.FlavorElement.ElementPath;
            e.Node.Expand();
        }
        private void elementTree_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            e.Node?.Nodes.Clear();
            SearchChildNode(e.Node);
        }
        private void traceButton_MouseHover(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(traceButton, " ");
        }
        private void traceButton_MouseUp(object sender, MouseEventArgs e)
        {
            FindElementFromPoint(Control.MousePosition);
        }
        private void btnVerify_Click(object sender, EventArgs e)
        {
            if (FindElementFromPath(elementPath.Text, out Element element))
            {
                toolTip2.ToolTipIcon = ToolTipIcon.Info;
                toolTip2.SetToolTip(btnVerify, "校验成功");
            }
            else
            {
                toolTip2.ToolTipIcon = ToolTipIcon.Error;
                toolTip2.SetToolTip(btnVerify, "校验失败");
            }

        }
        private void btnVerify_MouseHover(object sender, EventArgs e)
        {
            toolTip2.ToolTipIcon = ToolTipIcon.Info;
            toolTip2.SetToolTip(btnVerify, "点击校验元素路径");
        }
        private void 显示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            TopMost = true;
        }
        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("退出后流程将无法使用Java扩展组件", "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                Application.Exit();
            }
        }
        private void SpyForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.ApplicationExitCall)
            {
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
                Hide();
                e.Cancel = true;
            }
        }
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }
        private void elementPath_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(elementPath.Text);
            toolTip2.ToolTipIcon = ToolTipIcon.Info;
            toolTip2.SetToolTip(elementPath, "元素路径已复制到剪切板");
        }

        #endregion


        #region 元素编辑


        private void btn_edit_Click(object sender, EventArgs e)
        {

        }
        public void EditCallBack(string eventSource)
        {

        }

        #endregion
    }
}
