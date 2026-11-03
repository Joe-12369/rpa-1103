using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.Plugin.Test
{
    public partial class RPATest : Form
    {
        [MethodComponent("sheet数据表格")]
        public void Dialog_DataTableWithSheet()
        {
            var data = new
            {
                info = "",
                Title = "",
                ExeMode = "",
                autoOut = ""
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Dialog.ConfigDialog", "Excute", data.ToJson() }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        #region 窗口操作
        [MethodComponent("获取窗口列表")]
        public void Window_GetWindowList()
        {
            var data = new
            {

            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Window.GetWindowList", "Excute", data.ToJson() }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("获取窗口数据")]
        public void Window_GetWindowData()
        {
            var data = new
            {
                Title = textBox11.Text,
                SwitchType = "desktop",
                Selecter = rtb_selector.Text,
                WindowData = "{}"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Window.GetWindowData", "Excute", data.ToJson() }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("激活软件窗口")]
        public void Window_ActiveWindow()
        {
            var data = new
            {
                WindowData = textBox11.Text,
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Window.ActiveWindow", "Excute", data.ToJson() }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("设置窗口状态")]
        public void Window_SetWindowState()
        {
            var data = new
            {
                WindowData = textBox11.Text,
                WindowType = comboBox3.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Window.SetWindowState", "Excute", data.ToJson() }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("设置窗口是否显示")]
        public void Window_SetWindowShow()
        {
            var data = new
            {
                WindowData = textBox11.Text,
                WindowType = comboBox4.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Window.SetWindowShow", "Excute", data.ToJson() }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("获取窗口范围")]
        public void Window_GetWindowRect()
        {
            var data = new
            {
                WindowData = textBox11.Text,

            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Window.GetWindowRect", "Excute", data.ToJson() }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("移动窗口位置")]
        public void Window_MoveWindow()
        {
            var data = new
            {
                WindowData = textBox11.Text,
                X = Convert.ToInt32(textBox12.Text),
                Y = Convert.ToInt32(textBox13.Text)
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Window.MoveWindow", "Excute", data.ToJson() }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("调整窗口大小")]
        public void Window_SizeWindow()
        {
            var data = new
            {
                WindowData = textBox11.Text,
                Width = Convert.ToInt32(textBox15.Text),
                Height = Convert.ToInt32(textBox14.Text)

            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Window.SizeWindow", "Excute", data.ToJson() }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("关闭软件窗口")]
        public void Window_CloseWindow()
        {
            var data = new
            {
                WindowData = textBox11.Text,
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Window.CloseWindow", "Excute", data.ToJson() }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }


        #endregion

        #region 窗体事件、界面展示、程序初始化 等 相关

        public RPATest()
        {
            InitializeComponent();
        }
        public AppDomain _domain;
        Assembly _assembly = null;
        object pluginLoaderObject = null;
        Type pluginLoaderType = null;
        Type thisClassType = null;
        bool IsInitialized = false;
        bool _assemblyResolveHooked = false;
        MethodInfo methodRun = null;
        Excel.Config.ExcelConfigurations ExcelPluginInfo;

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckEnumTypes();
            this.thisClassType = this.GetType();
            cb_kettype.SelectedIndex = 0;
            cb_clickType.SelectedIndex = 0;
            cb_cursorPos.SelectedIndex = 0;
            cb_selectorType.SelectedIndex = 0;
            cb_selectMode.SelectedIndex = 0;
            cb_selectType_set.SelectedIndex = 0;
            cb_positionType.SelectedIndex = 1;
            rtb_selector.Text = ReadConfigInfo();
            LoadTreeNode();
            btn_loaderDLL_click(sender, e);
            this.comboBox1.Text = "ZExplorer";

            treeView1.CollapseAll();
            var lastSelectedNode = ReadConfigInfo("lastNode");
            if (!string.IsNullOrWhiteSpace(lastSelectedNode))
            {
                var lastNode = treeView1.Nodes.Find(lastSelectedNode, true)?.FirstOrDefault();
                if (lastNode != null)
                {
                    if (lastNode.Nodes.Count < 1)
                    {
                        lastNode.Parent.Expand();
                    }
                    else
                    {
                        lastNode.Expand();
                    }
                    lastNode.BackColor = Color.Red;
                }

            }
        }
        private void btn_loaderDLL_click(object sender, EventArgs e)
        {
            btn_loaderDLL.Enabled = false;
            treeView1.Enabled = false;
            //Task.Run(delegate
            //{
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                EnsureAssemblyResolveHooked();
                //\src\II.RPA.Plugin.Test\bin\II.RPA.Plugin\

        
                string packagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_package");
                string privatePath = Path.Combine(packagePath, "private");
                //if (Directory.Exists(privatePath))
                //{
                //    Directory.Delete(privatePath, true);
                //}
                string path = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.FullName,"bin", "II.RPA.PluginLoader");
                //_assembly = Assembly.Load(new AssemblyName { CodeBase = path + "\\II.RPA.PluginLoader.dll" });
                _assembly = Assembly.LoadFile(path + "\\II.RPA.PluginLoader.dll");
                sw.Stop();
                this.Invoke((Action)delegate
                {
                    btn_loaderDLL.Text = $"加载完成，耗时({sw.ElapsedMilliseconds})";
                });

                Thread.Sleep(1000);
                sw.Start();
                //_assembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path));
                pluginLoaderObject = _assembly.CreateInstance("II.RPA.PluginLoader.PluginLoader");
                pluginLoaderType = pluginLoaderObject.GetType();
                MethodInfo method = pluginLoaderType.GetMethod("InitAsync");

                //string pluginPath = @"C:\Users\mj\AppData\Roaming\Z-Runtime\6.9.1.0-feat-updatePluginConfig-691";
                //string packagesPath = @"C:\Users\mj\AppData\Roaming\Z-Factory\applications\c_53yGNoWLM6\";

                //string pluginPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)), "plugins");
                //string packagesPath = pluginPath;
                method.Invoke(pluginLoaderObject, new object[] { });
                sw.Stop();

                int count = 0;
                while (count <= 200)
                {
                    var isInitialized = pluginLoaderType.GetProperty("IsInitialized");
                    if (isInitialized == null) break;
                    if (isInitialized.GetValue(pluginLoaderObject).Equals(true))
                    {
                        break;
                    }
                    Thread.Sleep(50);
                }
                MethodInfo method_SetSharedVariable = pluginLoaderType.GetMethod("SetSharedVariable");
                JObject pJobj = new JObject() {
                     { "ZBrainServicePath", "http://127.0.0.1:6440" },
                     { "PackagePath", packagePath },
                     { "CLIENT-TENANTCODE","df2ebdfd3ded0b653191507f21a6dadd"},
                     { "TarsAgentServiceUrl","http://tars-ipa.qa.ii-ai.tech"},
                     {
                        "TarsAgentServiceToken",
                        "DBFA46ABD772672C591412F1A355D51CD9B09B26DE32FF5B5578A1982234ACD920A81CB3EFA1344DAB3CDA40497C86F9"
                     }
                    };
                method_SetSharedVariable.Invoke(pluginLoaderObject, new object[] { pJobj.ToString() });
                this.IsInitialized = true;
                this.Invoke((Action)delegate
                {
                    btn_loaderDLL.Text = $"初始化完成,耗时:{sw.ElapsedMilliseconds}(+<=50)";
                });
                methodRun = pluginLoaderType.GetMethod("RunAsync");
            }
            catch (Exception ex)
            {
                var detail = BuildLoadExceptionDetail(ex);
                this.Invoke((Action)delegate
                {
                    btn_loaderDLL.Text = "加载初始化异常，请重启";
                    rtbResult.Text = detail;
                });
                Debug.WriteLine(detail);
            }
            finally
            {
                this.Invoke(new Action(() =>
                {
                    treeView1.Enabled = true;
                }));
            }
            //});
        }
        private void EnsureAssemblyResolveHooked()
        {
            if (_assemblyResolveHooked)
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            _assemblyResolveHooked = true;
        }
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var asmName = new AssemblyName(args.Name).Name + ".dll";

                var requestingAssemblyPath = args.RequestingAssembly?.Location;
                if (!string.IsNullOrEmpty(requestingAssemblyPath))
                {
                    var requestingDirectory = Path.GetDirectoryName(requestingAssemblyPath);
                    if (!string.IsNullOrEmpty(requestingDirectory))
                    {
                        var candidate = Path.Combine(requestingDirectory, asmName);
                        if (File.Exists(candidate))
                        {
                            Debug.WriteLine($"[AssemblyResolve] Hit(RequestingAssembly): {candidate}");
                            return Assembly.LoadFile(candidate);
                        }
                    }
                }

                var baseCandidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, asmName);
                if (File.Exists(baseCandidate))
                {
                    Debug.WriteLine($"[AssemblyResolve] Hit(BaseDirectory): {baseCandidate}");
                    return Assembly.LoadFile(baseCandidate);
                }

                Debug.WriteLine($"[AssemblyResolve] Miss: {args.Name}");
                Debug.WriteLine($"[AssemblyResolve] RequestingAssembly: {args.RequestingAssembly?.Location}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AssemblyResolve] Exception: " + ex);
                return null;
            }
        }
        private static string BuildLoadExceptionDetail(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Load Exception ===");
            sb.AppendLine(ex.ToString());

            AppendFileNotFoundDetail(sb, ex);

            if (ex is ReflectionTypeLoadException reflectionTypeLoadException)
            {
                sb.AppendLine("--- ReflectionTypeLoadException.LoaderExceptions ---");
                if (reflectionTypeLoadException.LoaderExceptions != null)
                {
                    foreach (var loaderException in reflectionTypeLoadException.LoaderExceptions)
                    {
                        if (loaderException == null)
                        {
                            continue;
                        }

                        sb.AppendLine(loaderException.ToString());
                        AppendFileNotFoundDetail(sb, loaderException);
                    }
                }
            }

            var inner = ex.InnerException;
            while (inner != null)
            {
                sb.AppendLine("--- InnerException ---");
                sb.AppendLine(inner.ToString());
                AppendFileNotFoundDetail(sb, inner);
                inner = inner.InnerException;
            }

            return sb.ToString();
        }
        private static void AppendFileNotFoundDetail(StringBuilder sb, Exception ex)
        {
            if (ex is FileNotFoundException fileNotFoundException)
            {
                sb.AppendLine("--- FileNotFoundException ---");
                sb.AppendLine("FileName: " + fileNotFoundException.FileName);
                sb.AppendLine("FusionLog: " + fileNotFoundException.FusionLog);
            }
        }
        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Parent == null)
            {
                return;
            }
            if (_assembly == null)
            {
                if (MessageBox.Show("尚未加载DLL，是否加载？", "加载提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    btn_loaderDLL_click(sender, e);
                    return;
                }
                else
                {
                    return;
                }
            }

            try
            {

                string name = e.Node.Text.ToString();
                SaveConfigInfo(name, "lastNode");
                MethodInfo method = null;
                object[] args = null;
                if (e.Node.Parent.Text.StartsWith("Excel"))
                {
                    method = thisClassType.GetMethod("InvokeExcel");
                    args = new object[1] { name };
                    method.Invoke(this, args);
                }
                else
                {
                    method = thisClassType.GetMethods().FirstOrDefault(m => m.GetCustomAttribute(typeof(MethodComponentAttribute)) != null && m.GetCustomAttribute(typeof(MethodComponentAttribute)).As<MethodComponentAttribute>().Name == name);
                    this.WindowState = FormWindowState.Minimized;
                    if (method == null)
                    {
                        return;
                    }
                    Thread.Sleep(300);
                    int delayed = nud_clieck_delayed.Value.As<int>();
                    if (delayed > 0)
                    {
                        Thread.Sleep(delayed);
                    }
                    method.Invoke(this, args);
                    SaveConfigInfo(rtb_selector.Text);
                    if (delayed > 0)
                    {
                        Thread.Sleep(delayed);
                    }
                }

            }
            catch (Exception ex)
            {
                rtbResult.Text = ex.Message;
            }
        }
        private void RPATest_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        #region 检测 设置的【异常类型】 和 【组件类型】 有没有重复
        private void CheckEnumTypes()
        {
            var names = Enum.GetNames(typeof(eErrorMessageType));
            var values = names.Select(n => (int)Enum.Parse(typeof(eErrorMessageType), n)).ToList();

            if (values.Distinct().Count() != names.Length)
            {
                var repeatValues = values.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                MessageBox.Show($"兄弟！搞事情啊！eErrorMessageType:[{string.Join(",", repeatValues)}] 配置重复了！罢工！");
                this.panel1.Enabled = false;
                return;
            }
            names = Enum.GetNames(typeof(eComponentType));
            values = names.Select(n => (int)Enum.Parse(typeof(eComponentType), n)).ToList();
            if (values.Distinct().Count() != names.Length)
            {
                var repeatValues = values.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                MessageBox.Show($"兄弟！搞事情啊！eComponentType:[{string.Join(", ", repeatValues)}] 配置重复了！罢工！");
                this.panel1.Enabled = false;
                return;
            }
        }
        #endregion

        #region 加载树节点

        private void LoadTreeNode()
        {
            var treeNodesList = File.ReadAllLines("ComponentTree.ini").ToList();
            treeNodesList.RemoveAll(str => str.StartsWith("#") || string.IsNullOrEmpty(str.Trim()));

            #region 增加Excel组件节点
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            var configPath = currentDir + "\\Excel\\Config\\config.json";
            if (File.Exists(configPath))
            {
                var configInfo = File.ReadAllText(configPath);
                ExcelPluginInfo = JsonConvert.DeserializeObject<II.RPA.Plugin.Excel.Config.ExcelConfigurations>(configInfo);
                try
                {
                    treeNodesList.Add("Excel 组件");
                    treeNodesList.AddRange(ExcelPluginInfo.ActionSetting.Values.Select(a => $"@{a.Value<string>("name")}"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Excel配置读取失败，去看看咋回事呀！");
                }
            }
            #endregion

            List<TreeNode> rootNodes = new List<TreeNode>();
            int level = 0;
            TreeNode node = null;
            foreach (var item in treeNodesList)
            {
                string[] contents = item.Split('|');
                string text = contents[0].TrimStart('@');
                int cur_level = contents[0].Length - text.Length;
                if (cur_level > level)
                {
                    rootNodes.Add(node);
                    level++;
                }
                else if (cur_level < level)
                {
                    rootNodes.RemoveRange(cur_level, level - cur_level);
                    level = cur_level;
                }
                node = new TreeNode(text);
                node.Name = text;
                TreeNode pNode = rootNodes.LastOrDefault();
                if (pNode == null)
                {
                    treeView1.Nodes.Add(node);
                }
                else
                {
                    pNode.Nodes.Add(node);
                }
                if (contents.Length > 1)
                {
                    if (contents[1].Contains("O"))
                    {
                        var expandNode = node;
                        Task.Delay(500).ContinueWith((t) =>
                        {
                            this.Invoke(new Action(() => { expandNode.ExpandAll(); }));
                        });
                    }
                    if (contents[1].Contains("R"))
                    {
                        node.ForeColor = Color.Red;
                    }
                    if (contents[1].Contains("B"))
                    {
                        node.ForeColor = Color.Blue;
                    }
                }
            }
        }

        #endregion

        #region 配置记忆
        string ReadConfigInfo(string config = "selector")
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"cache/{config}.txt");
            if (!File.Exists(path))
            {
                return "";
            }
            return File.ReadAllText(path, Encoding.UTF8);
        }
        /// <summary>
        /// save config
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="config">配置名称</param>
        void SaveConfigInfo(string content, string config = "selector")
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"cache\{config}.txt");
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            File.WriteAllText(path, content);
        }
        #endregion

        #region JSON格式化方法
        private string ConvertJsonString(string str)
        {
            //格式化json字符串
            JsonSerializer serializer = new JsonSerializer();
            using (TextReader tr = new StringReader(str))
            {

                using (JsonTextReader jtr = new JsonTextReader(tr))
                {
                    object obj = serializer.Deserialize(jtr);
                    if (obj != null)
                    {
                        using (StringWriter textWriter = new StringWriter())
                        {

                            using (JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                            {
                                Formatting = Formatting.Indented,
                                Indentation = 4,
                                IndentChar = ' '
                            })
                            {
                                serializer.Serialize(jsonWriter, obj);
                                return textWriter.ToString();
                            }
                        }
                    }
                    else
                    {
                        return str;
                    }
                }
            }
        }
        #endregion
        #endregion

        #region [浏览器相关]
        [MethodComponent("获取网页元素")]
        public void Web_GetElement()
        {
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
                QueryMode = "cssSelector",
                Selector = "#su"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetElement", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();

            data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
                QueryMode = "xpath",
                Selector = "//*[@id=\"su\"]"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetElement", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }
        [MethodComponent("数据采集配置")]
        public void Web_DataConfig()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                jsPath = @"D:\Program Files (x86)\Z-Factory\resources\app.asar.unpacked\resources\support\xiaozhangyu\executor\index.js",
                config = textBox10.Text,
                url = "D:\\Program Files (x86)\\Z-Factory\\resources\\app.asar.unpacked\\resources\\rpa-engine\\rpa-engine-5.8.3.snapshot0806\\resource\\data_crawl\\configure\\configure.html",
                browserType = comboBox1.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.StartConfig", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                textBox10.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("数据采集运行")]
        public void Web_DataCapture()
        {
            // IsWebDetail();
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");

            var data = new
            {
                jsPath = @"D:\Program Files (x86)\Z-Factory\resources\app.asar.unpacked\resources\support\xiaozhangyu\executor\index.js",
                config = textBox10.Text,
                url = "D:\\Program Files (x86)\\Z-Factory\\resources\\app.asar.unpacked\\resources\\rpa-engine\\rpa-engine-5.8.3.snapshot0806\\resource\\data_crawl\\configure\\configure.html",
                browserType = comboBox1.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.StartCapture", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;

        }
        [MethodComponent("最小化浏览器")]
        public void Web_Minimized()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Minimized", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("最大化浏览器")]
        public void Web_Maximized()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Maximized", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("下载文件(点击元素)")]
        public void Web_Downloader()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                selector = rtb_selector.Text,
                saveFile = "D:\\",
                fileName = "demo",
                timeout = textBox3.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.Downloader", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("下载文件(点击按钮)")]
        public void Web_Download()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
                selector = rtb_selector.Text,
                saveFile = "D:\\",
                fileName = "download.png",
                timeout = textBox3.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Download", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("爬取数据")]
        public void Web_data()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                selector = rtb_selector.Text.ToString(),
                strSelector = ""
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.FindRelativeElementsInfo", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("执行JS")]
        public void Web_Executejs()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                title = textBox1.Text.ToString(),
                url = textBox2.Text.ToString(),
                scriptCode = textBox7.Text.ToString(),
                browserType = comboBox1.Text,
                @params = textBox8.Text.ToString()
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Executejs", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("跨域执行JS")]
        public void Web_ElementExecutejs()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                selector = rtb_selector.Text.ToString(),
                scriptCode = textBox7.Text.ToString(),
                @params = textBox8.Text.ToString(),
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.ElementExecutejs", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("浏览器截图")]
        public void Web_Screenshot()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                webData = rtb_WebDetail.Text.ToString(),
                browserType = comboBox1.Text,
                savePath = textBox6.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Screenshot", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("浏览器长截图")]
        public void Web_CaptureScreenshot()
        {
            var data = new
            {
                webData = rtb_WebDetail.Text.ToString(),
                browserType = comboBox1.Text,
                picPath = textBox6.Text,
                cutRange = 1,
                height = 2500
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.CaptureScreenshot", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("开始监听网络请求")]
        public void Web_StartInspectNetwork()
        {
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.StartInspectNetwork", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("获得网络请求")]
        public void Web_GetInspectNetwork()
        {
            var input = JsonConvert.DeserializeObject<JObject>(textBox8.Text);
            input.Add("browserType", comboBox1.Text);
            input.Add("webData", rtb_WebDetail.Text.ToString());
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.GetInspectNetwork", "Excute", JsonConvert.SerializeObject(input)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("结束监听网络请求")]
        public void Web_EndInspectNetwork()
        {
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.EndInspectNetwork", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("获取Title")]
        public void Web_GetTitle()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.GetTitle", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("获取Url")]
        public void Web_GetUrl()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.GetUrl", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("设置滚动条位置")]
        public void Web_Scroll()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                webData = rtb_WebDetail.Text.ToString(),
                browserType = comboBox1.Text,
                value = textBox5.Text.ToString()
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Scroll", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("获取滚动条位置")]
        public void Web_GetScroll()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text,
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.GetScroll", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("获取网页源码")]
        public void Web_GetDocument()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString()
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.GetDocument", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("设置Cookies")]
        public void Web_SetCookie()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
                value = textBox4.Text.ToString()

            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.SetCookie", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("获取Cookies(指定)")]
        public void Web_GetCookie()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
                name = textBox9.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.GetCookies", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("获取Cookies")]
        public void Web_GetCookies()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.GetCookies", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("停止加载")]
        public void Web_Stop()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString()
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Stop", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("等待加载")]
        public void Web_Wait()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
                timeout = textBox3.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Wait", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("刷新")]
        public void Web_Reload()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString()
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.ReLoad", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("前进")]
        public void Web_GoForward()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString()
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.GoForward", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("后退")]
        public void Web_GoBack()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString()
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.GoBack", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        //    [MethodComponent("下载文件(指定链接)")]
        //    public void Web_GetTab()
        //    {
        //        //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
        //        var data = new
        //        {
        //            TabId = JsonConvert.DeserializeObject(rtb_selector.Text)
        //        };
        //        rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Acquire", "Excute", JsonConvert.SerializeObject(data)
        //}).ToString();
        //        var msg = rtbResult.Text.ToObject<JMessage>();
        //        if (msg == null)
        //            return;
        //        if (msg.Code == 0)
        //            rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        //    }
        [MethodComponent("切换页面")]
        public void Web_GetTab()
        {
            //Web_Acquire();
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
                url = textBox2.Text,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Active", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            //if (msg.Code == 0)
            //    rtb_WebDetail.Text = rtbResult.Text.ToObject<JMessage>()?.Info;

        }
        [MethodComponent("跳转网页")]
        public void Web_Navigate()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
                url = textBox2.Text,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Navigate", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_WebDetail.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("关闭浏览器")]
        public void Web_CloseAll()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
                kill = 1,
                closeMode = "browser"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.MultipleClose", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }

        [MethodComponent("关闭多个网页")]
        public void Web_CloseList()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string[] datas = new string[] { rtb_WebDetail.Text.ToString() };
            var data = new
            {
                browserType = comboBox1.Text,
                //webData = rtb_WebDetail.Text.ToString(),
                webDatas = datas,
                closeMode = "select"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.MultipleClose", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("关闭网页")]
        public void Web_Close()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString()
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Close", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("获取Tab页")]
        public void Web_Acquire()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                value = comboBox2.Text == "title" ? textBox1.Text : textBox2.Text,
                mode = comboBox2.Text,
                index = 1,
                browserType = comboBox1.Text,
                isActive = true
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Acquire", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
            {
                rtb_WebDetail.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
            }
        }
        [MethodComponent("打开新浏览器")]
        public void Web_Create()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                url = textBox2.Text,
                browserType = comboBox1.Text,
                timeout = 20000,
                waitingload = 0,
                arguments = ""
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "WebBrowser.Create", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
            {
                rtb_WebDetail.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
            }
        }
        public void IsWebDetail()
        {
            if (string.IsNullOrWhiteSpace(rtb_WebDetail.Text)) MessageBox.Show("先获取Tab信息"); else return;
        }

        [MethodComponent("文件上传")]
        public void Web_FileUploader()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                browserType = comboBox1.Text,
                webData = rtb_WebDetail.Text.ToString(),
                selector = rtb_selector.Text,
                //filePath = "C:\\sqlite\\sqlite3.def;C:\\sqlite\\sqlite3.dll",
                filePath = "C:\\sqlite",
                uploadType = 1,
                Trigger = 1,
                interval = 150,
                inputMode = 1,
                waitDialogTime = 10000,
                timeout = textBox3.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.FileUploader", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        #endregion

        #region 鼠标操作 相关
        [MethodComponent("拾取鼠标坐标")]
        public void Gui_CapturePosition()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                type = 1
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.CapturePosition", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null || msg.Code != 0 || string.IsNullOrEmpty(msg.Info))
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("鼠标移动")]
        public void Mouse_MouseMove()
        {
            var data = new
            {
                positiontype = 1,
                x = 500,
                y = 500,
                speedValue = 50,
                isPersonLike = 1,
                Instant = 1,
                isUsePerson = 1
            };
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Mouse.MouseMove", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("鼠标移动(重复包名)")]
        public void Mouse_MouseMove2()
        {
            var data = new
            {
                positiontype = 1,
                x = 500,
                y = 500,
                speedValue = 50,
                isPersonLike = 1,
                Instant = 1,
                isUsePerson = 1
            };
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "DesktopInput.MouseMove", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("鼠标拖拽")]
        public void Mouse_MouseDrag()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                Positiontype = 1,
                Mousetype = 0,
                Keys = new string[0],
                Startx = 300,
                Starty = 300,
                Endx = 500,
                Endy = 500
            };
            if (!rtb_selector.Text.Contains("Positiontype") && !rtb_selector.Text.Contains("Mousetype"))
            {
                rtb_selector.Text = ConvertJsonString(JsonConvert.SerializeObject(data));

                this.WindowState = FormWindowState.Normal;
                MessageBox.Show("请在seletor文本框中完善要测试的参数，参数完善后可再次点击左边功能菜单");
                return;
            }
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Mouse.MouseDrag", "Excute", rtb_selector.Text }).ToString();
        }

        #endregion

        #region 界面元素相关
        [MethodComponent("相似元素判断")]
        public void Gui_IsSimilarElementExist()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                isHighlighting = cb_isHeight.Checked,
                cvExecutionServiceURL = txtCVExecutionServer.Text,
                timeout = 10000,
                BrowserType = "unknown"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.IsSimilarElementExist", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            this.WindowState = FormWindowState.Normal;
        }
        [MethodComponent("获取页面所有元素")]
        public void Gui_GetPageAllElements()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            Task.Delay(1000);
            string s = rtb_selector.Text;
            var data = new JObject()
            {
                { "sourceType" , "normal"},
                //{ "guiType" , "windows"},
                //{ "guiType" , "windows"},
                { "windowType","web"},
                //{ "tagNames","body"},
                //{ "attrKeywords","nui-scroll"}
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetAllElementsInWindow", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("获取元素selector")]
        public void Gui_GetElementSelector()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;

            var data = JObject.Parse(rtb_WebDetail.Text);
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetElementSelector", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }


        [MethodComponent("获取元素源码")]
        public void Gui_GetHtml()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetHtml", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("元素修复")]
        public void Gui_CaptureRenovate()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                filepath = Application.StartupPath + @"\.screenshots\" + Guid.NewGuid().ToString() + ".png",
                isCVCapture = false,
                cvServer = "",
                InspectStatus = 0,
                selector = rtb_selector.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.CaptureRenovate", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }
        [MethodComponent("拾取相似元素")]
        public void Gui_CaptureSimilar()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                filepath = Application.StartupPath + @"\.screenshots\" + Guid.NewGuid().ToString() + ".png",
                isCVCapture = false,
                cvServer = "",
                InspectStatus = 0,
                selector = rtb_selector.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.CaptureSimilar", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }
        [MethodComponent("获取相似元素")]
        public void Gui_GetElementSelectorAll()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                selector = rtb_selector.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetElementSelectorAll", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("获取相似元素列表")]
        public void Gui_GetSimilarElementList()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                selector = rtb_selector.Text,
                attributeName = "selector"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetSimilarElementList", "Excute", JsonConvert.SerializeObject(data)
    }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }

        [MethodComponent("元素拾取(普通)")]
        public void Gui_Capture()
        {
            //CellFormatParams cellFormatParams = new CellFormatParams();
            //logger.LogInformation("cellFormatParams:" + JsonHelper.ToJson(cellFormatParams));
            //string strJson = JsonHelper.ToJson(cellFormatParams);

            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                filepath = Application.StartupPath + @"\.screenshots\" + Guid.NewGuid().ToString() + ".png",
                isCVCapture = false,
                cvServer = txtCvServer.Text,
                InspectStatus = 0,
                CaptureMode = 1,
                IsCvSupport = 0
            };

            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.Capture", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }
        [MethodComponent("元素拾取(智能)")]
        public void Gui_Capture2()
        {

            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                filepath = Application.StartupPath + @"\.screenshots\" + Guid.NewGuid().ToString() + ".png",
                isCVCapture = false,
                cvServer = txtCvServer.Text,
                InspectStatus = 0,
                CaptureMode = 1,
                IsCvSupport = 1
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.Capture", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }
        [MethodComponent("元素拾取(Agent)")]
        public void Gui_Capture3()
        {

            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                filepath = Application.StartupPath + @"\.screenshots\" + Guid.NewGuid().ToString() + ".png",
                isCVCapture = false,
                cvServer = txtCvServer.Text,
                InspectStatus = 0,
                CaptureMode = 1,
                IsCvSupport = 0
            };

            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.CaptureForAgent", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }
        [MethodComponent("元素点击")]
        public void Gui_MouseClick()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            int offsetx = 0;
            int offsety = 0;
            if (!string.IsNullOrEmpty(txtOffX.Text))
            {
                offsetx = int.Parse(txtOffX.Text);
            }
            if (!string.IsNullOrEmpty(txtOffY.Text))
            {
                offsety = int.Parse(txtOffY.Text);
            }
            int simulate = ckbSimulate.Checked ? 1 : 0;

            var data = new
            {
                selector = s,
                clickType = cb_kettype.SelectedIndex,
                offsetX = offsetx,
                offsetY = offsety,
                issimulate = simulate,
                cursorPos = cb_cursorPos.SelectedIndex,
                keyType = cb_clickType.SelectedIndex,
                timeout = 10000,
                isUsePerson = 1,
                cvExecutionServiceURL = txtCVExecutionServer.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.MouseClick", "Excute", JsonConvert.SerializeObject(data) }).ToString();

        }
        [MethodComponent("精简元素结构")]
        public void Gui_SmartSelector()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000
            }; rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.SmartSelector", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();

            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }


        [MethodComponent("元素判断")]
        public void Gui_IsElementExist()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                isHighlighting = cb_isHeight.Checked,
                cvExecutionServiceURL = txtCVExecutionServer.Text,
                timeout = 10000,
                BrowserType = ""
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.IsElementExist", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }

        [MethodComponent("获取驱动输入列表")]
        public void Driver_DriverSearch()
        {
            //string s = rtb_selector.Text;
            //var data = new
            //{
            //    selector = s,
            //    isHighlighting = cb_isHeight.Checked,
            //    cvExecutionServiceURL = txtCVExecutionServer.Text,
            //    timeout = 10000,
            //    BrowserType = ""
            //};
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "DriverInput.DriverSearch", "Excute", "" }).ToString();
        }

        [MethodComponent("获取父元素")]
        public void Gui_GetParentElements()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000,
                UseAdvanced = 0,
                Level = "1"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetParent", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }

        [MethodComponent("获取子元素")]
        public void Gui_GetSubElements()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetSubElements", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }

        [MethodComponent("获取兄弟元素")]
        public void Gui_GetSiblingsElements()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetSiblingsElements", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }


        [MethodComponent("设置属性")]
        public void Gui_SetAttribute()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000,
                name = txtAttrName.Text,
                value = txtAttrValue.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.SetAttribute", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }

        [MethodComponent("获取属性")]
        public void Gui_GetAttribute()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                name = txtAttrName.Text,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetAttribute", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("获取选择值")]
        public void Gui_GetElementSelect()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                selectType = cb_selectorType.SelectedIndex + 1,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetElementSelect", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }

        [MethodComponent("获取选择值Ex")]
        public void Gui_GetElementSelectEx()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                selectType = cb_selectorType.SelectedIndex,
                selectMode = cb_selectMode.SelectedIndex,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetElementSelectEx", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }

        [MethodComponent("设置选择值")]
        public void Gui_SetElementSelect()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                selectType = cb_selectType_set.SelectedIndex,
                setValue = txt_selectSetValue.Text,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.SetElementSelect", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("获取勾选项")]
        public void Gui_GetElementCheck()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetElementCheck", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("设置勾选项")]
        public void Gui_SetElementCheck()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                check = cb_setCheck.Checked ? 1 : 0,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.SetElementCheck", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("滚动界面")]
        public void Gui_ScrollIntoViewIfNeeded()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.ScrollIntoViewIfNeeded", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("元素截图")]
        public void Gui_ElementScreenshot()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            string snapPath = Application.StartupPath + @"\.screenshots\" + Guid.NewGuid().ToString() + ".png";
            var data = new
            {
                selector = s,
                timeout = 10000,
                savePath = snapPath
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.ElementScreenshot", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("获取区域")]
        public void Gui_GetRegion()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                positionType = cb_positionType.SelectedIndex + 1,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetRegion", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("获取文本")]
        public void Gui_GetText()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000,
                cvExecutionServiceURL = txtCVExecutionServer.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetText", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("设置文本")]
        public void Gui_SetText()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000,
                cvExecutionServiceURL = txtCVExecutionServer.Text,
                txt = txt_setText.Text,
                isReplace = 0,
                isSimulate = 1,
                isVerify = 0,
                Retry = 0
                //isSimulate = ckbSimulate.Checked ? 1 : 0
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.SetText", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("元素是否可点击")]
        public void Gui_GetButtonEnable()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetButtonEnable", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("鼠标定位至元素上")]
        public void Gui_MouseHover()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000,
                offsetX = txtOffX.Text.As<int>(),
                offsetY = txtOffY.Text.As<int>(),
                cursorPos = cb_cursorPos.SelectedIndex,
                cvExecutionServiceURL = txtCVExecutionServer.Text,
                personification = 1,
                speed = 60,
                isUsePerson = 1,
                instant = 0,
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.MouseHover", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("鼠标定位至元素上-智能修复")]
        public void Gui_MouseHover_aiRepair()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000,
                offsetX = txtOffX.Text.As<int>(),
                offsetY = txtOffY.Text.As<int>(),
                cursorPos = cb_cursorPos.SelectedIndex,
                cvExecutionServiceURL = txtCVExecutionServer.Text,
                personification = 1,
                speed = 60,
                isUsePerson = 1,
                instant = 0,
                MatchType = "AiRepair"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.MouseHover", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("屏幕截图")]
        public void Gui_CaptureScreen()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string imgPath = Application.StartupPath + @"\.screenshots\" + Guid.NewGuid().ToString() + ".png";
            var data = new
            {
                imgPath = imgPath
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.CaptureScreen", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("配置数据采集")]
        public void Gui_DataCollectConfiguration()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                collectType = MessageBox.Show("是否要采集web数据？点【否】将采集应用数据", "选择采集类型", MessageBoxButtons.YesNo) == DialogResult.Yes ? "web" : "software"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.DataCollectConfiguration", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }

        #endregion

        #region AI CV相关

        [MethodComponent("CV元素拾取")]
        public void Gui_CVCapture()
        {

            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                filepath = Application.StartupPath + @"\.screenshots\" + Guid.NewGuid().ToString() + ".png",
                isCVCapture = true,
                cvServer = txtCvServer.Text,
                InspectStatus = 0
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.Capture", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtbResult.Text.ToObject<JMessage>()?.Info;
        }
        [MethodComponent("CV移动到元素上")]
        public void Gui_CVMouseHover()
        {

            Gui_MouseHover();
        }
        [MethodComponent("CV元素点击")]
        public void Gui_CVMouseClick()
        {

            Gui_MouseClick();
        }
        [MethodComponent("CV元素是否存在")]
        public void Gui_CVIsElementExist()
        {

            Gui_IsElementExist();
        }
        [MethodComponent("CV获取文本截图")]
        public void Gui_CaptureByRegion()
        {

            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                filepath = Application.StartupPath + @"\.screenshots\" + Guid.NewGuid().ToString() + ".png"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.CaptureByRegion", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }
        [MethodComponent("CV获取文本")]
        public void Gui_GetCVText()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                cvserver = "https://127.0.0.1:6440/get_text_content_service/",
                timeout = 10000,
                cvExecutionServiceURL = txtCVExecutionServer.Text
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetCVText", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        #endregion

        #region AI OCR-离线  相关

        [MethodComponent("拾取图像区域")]
        public void OcrOffline_CaptureCV()
        {

            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                filepath = Application.StartupPath + @"\.screenshots\" + Guid.NewGuid().ToString() + ".png"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "OcrOffline.CaptureCV", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }

        [MethodComponent("鼠标移动到OCR文本上")]
        public void OcrOffline_ExecuteCV()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000,
                textToFind = txt_setText.Text,
                server = "http://127.0.0.1:5000/ocr_service"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "OcrOffline.ExecuteCV", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null || msg.Code != 0 || string.IsNullOrEmpty(msg.Info))
                return;
            Point p = msg.Info.ToObject<Point>();
            var dataMouseMove = new
            {
                positiontype = 0,
                x = p.X,
                y = p.Y
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Mouse.MouseMove", "Excute", JsonConvert.SerializeObject(dataMouseMove) }).ToString();
        }

        [MethodComponent("鼠标点击OCR文本")]
        public void OcrOffline_OCRTextClick()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000,
                textToFind = txt_setText.Text,
                server = "http://127.0.0.1:5000/ocr_service"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "OcrOffline.ExecuteCV", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }

        [MethodComponent("图像OCR识别")]
        public void OcrOffline_GetTextByCV()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000,
                server = "http://127.0.0.1:5000/ocr_service"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "OcrOffline.GetTextByCV", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        [MethodComponent("图像OCR识别V2")]
        public void OcrOffline_GetTextByCV_V2()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            var data = new
            {
                selector = s,
                timeout = 10000,
                server = "http://127.0.0.1:6440/ocr_service"
                //server = "http://192.168.132.136:6440/ocr_service"   
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "OcrOffline.GetTextByCV_V2", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }

        [MethodComponent("表格OCR解析本地图片")]
        public void OcrOffline_GetOcrTableDataByImg()//解析本地图片获取表格数据
        {
            MethodInfo methodRun = pluginLoaderType.GetMethod("RunAsync");
            string s = rtb_selector.Text;
            var data = new
            {
                imagePath = s,
                timeout = 10000,
                //server = "http://192.168.132.136:6440/table/pickup"
                server = "http://10.1.0.249:15336/table/ocr_service"

            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "OcrOffline.GetOcrTableDataByImg", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }

        [MethodComponent("表格OCR截图拾取Selector")]
        public void OcrOffline_MakeOcrTableSelector()//截屏，获取表格数据并组装Selector
        {
            MethodInfo methodRun = pluginLoaderType.GetMethod("RunAsync");
            var data = new
            {
                timeout = 10000,
                //server = "http://127.0.0.1:6440/table/pickup",
                server = "http://10.1.0.249:15336/table/pickup",
                imgPath = @"123.jpg"

            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "OcrOffline.MakeOcrTableSelector", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);

        }

        [MethodComponent("表格OCR解析Selector获取表格数据")]
        public void OcrOffline_GetOcrTableDataBySelector()//根据selector查找表格并获取表格数据
        {
            MethodInfo methodRun = pluginLoaderType.GetMethod("RunAsync");
            string s = rtb_selector.Text;
            var data = new
            {
                isFromSelector = true,
                selector = s.ToObject<object>(),
                //timeout = 10000,
                //server = "http://127.0.0.1:6440/table/ocr_service"
                server = "http://10.1.0.249:15336/table/ocr_service"
            };

            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "OcrOffline.GetOcrTableDataBySelector", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        #endregion

        #region System相关


        [MethodComponent("获取应用地址")]
        public void GetApplicationPath_RDPLock()
        {
            if (string.IsNullOrEmpty(rtb_WebDetail.Text))
            {
                JObject obj = new JObject()
                {
                    { "AppProcessNames",JArray.FromObject(new[]{ ""})},
                    { "DisplayNames",JArray.FromObject(new[]{ ""})}
                };
                rtb_WebDetail.Text = ConvertJsonString(obj.ToString());
                return;
            }

            var data = JObject.Parse(rtb_WebDetail.Text);
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "RPASystem.GetApplicationPath", "Excute", data.ToJson() }).ToString();

        }
        [MethodComponent("屏幕锁屏")]
        public void RPASystem_RDPLock()
        {
            //MessageBox.Show("测试锁屏功能后，将于10秒之后尝试屏幕解锁");
            rtbResult.Text = "";
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var loopCount = 10;

            while (loopCount-- > 0)
            {
                var data = new
                {
                    username = "edy",
                    pwd = "Jing0123",
                };

                rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "RPASystem.RDPLock", "Excute", data.ToJson() }).ToString();
            }

            //Task.Delay(10000).ContinueWith(t =>
            //{
            //    RPASystem_RDPUnlock();
            //});
        }
        [MethodComponent("屏幕解锁")]
        public void RPASystem_RDPUnlock()
        {
            rtbResult.Text = "";
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                username = "admin",
                pwd = "123123"
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "RPASystem.RDPUnlock", "Excute", data.ToJson() }).ToString();
        }
        [MethodComponent("获取锁屏状态")]
        public void GetScreenLockState()
        {
            rtbResult.Text = "";
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "RPASystem.CheckScreenState", "Excute", "{}" }).ToString();
        }

        [MethodComponent("远程解锁")]
        public void RemoteUnLock()
        {
            rtbResult.Text = "";
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                WaitTime = 10000,
                Ip = "192.168.65.241",
                Port = "3389",
                User = "Indeed_wenyi",
                Pw = "1",
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "RPASystem.RemoteUnLock", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }

        [MethodComponent("取消远程")]
        public void RemoteCancel()
        {
            rtbResult.Text = "";
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                Ip = "192.168.65.241",
                User = "Indeed_wenyi",
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "RPASystem.RemoteCancel", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }

        [MethodComponent("HID驱动检测")]
        public void HIDDriveCheck()
        {
            rtbResult.Text = "";
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "RPASystem.HIDDriveCheck", "Excute", JsonConvert.SerializeObject("{}") }).ToString();
        }

        [MethodComponent("远程桌面连接")]
        public void LocalRemoteConnection()
        {
            rtbResult.Text = "";
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                WaitTime = 2000,
                Ip = "192.168.69.22",
                Port = "3389",
                User = "Indeed_wenyi",
                Pw = "1",
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "RPASystem.LocalRemoteConnection", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }

        [MethodComponent("远程桌面断开")]
        public void LocalRemoteOff()
        {
            rtbResult.Text = "";
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                Ip = "192.168.65.241",
                User = "Indeed_wenyi",
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "RPASystem.LocalRemoteOff", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }

        [MethodComponent("自定义对话框")]
        public void ConfigDialog()
        {
            //    string data = @"{info:[{
            //    'name': 'datePicker',
            // 'label': '日期选择',
            // 'dateTimeFormat': 'yyyy-MM-DD',
            // 'value': '',
            // 'type': 'Date',
            // 'number': 9
            //}],FolderPath:'',Title:''}";
            //string data = "{\"info\": [{\"name\": \"Dialog_Input\", \"label\": \"输入框\", \"tips\": \"请输入文本内容\", \"value\": \"1\", \"type\": \"TextBox\", \"number\": 1}, {\"name\": \"Dialog_Textarea\", \"label\": \"文本域\", \"tips\": \"请输入文本内容\", \"height\": 70, \"value\": \"3333\", \"type\": \"TextArea\", \"number\": 2},{\"name\":\"Dialog_MultiSelect\",\"label\":\"\",\"options\":[\"选项1\",\"选项2\",\"选项3\"],\"valueArray\":[\"选项3\",\"选项2\"],\"isRequired\":\"0\",\"type\":\"MultiSelect\",\"number\":3,\"id\":\"EAzez\"},{\"name\":\"Dialog_Select\",\"label\":\"下拉列表\",\"options\":[\"选项1\",\"选项2\",\"选项3\"],\"value\":\"选项2\",\"type\":\"DropdownList\",\"number\":4,\"id\":\"Ow4NI\"}], \"FolderPath\": \"C:\\\\Users\\\\56220\\\\Documents\\\\Z-Factory\\\\projects\\\\新建前台应用(148)\", \"Title\": \"\"}";

            string data = "{\"info\": [{\"name\": \"Dialog_Input\", \"label\": \"输入框\", \"tips\": \"请输入文本内容\", \"value\": \"1\", \"type\": \"TextBox\", \"number\": 1}, {\"name\": \"Dialog_Textarea\", \"label\": \"文本域\", \"tips\": \"请输入文本内容\", \"height\": 70, \"value\": \"3333\", \"type\": \"TextArea\", \"number\": 2},{\"name\":\"Dialog_MultiSelect\",\"label\":\"\",\"options\":[\"选项1\",\"选项2\",\"选项3\"],\"valueArray\":[\"选项3\",\"选项2\"],\"isRequired\":\"0\",\"type\":\"MultiSelect\",\"number\":3},{\"name\":\"Dialog_Select\",\"label\":\"下拉列表\",\"options\":[\"选项1\",\"选项2\",\"选项3\"],\"value\":\"选项2\",\"type\":\"DropdownList\",\"number\":4}\r\n,{\"name\":\"Dialog_MultiSelect\",\"label\":\"\",\"options\":[\"选项1\",\"选项2\",\"选项3\"],\"valueArray\":[\"选项1\",\"选项2\"],\"isRequired\":\"0\",\"type\":\"MultiSelect\",\"number\":5},{\"name\":\"Dialog_Select\",\"label\":\"下拉列表\",\"options\":[\"选项1\",\"选项2\",\"选项3\"],\"value\":\"选项1\",\"type\":\"DropdownList\",\"number\":6}\r\n,{\"name\":\"Dialog_MultiSelect\",\"label\":\"\",\"options\":[\"选项1\",\"选项2\",\"选项3\"],\"valueArray\":[\"选项3\"],\"isRequired\":\"0\",\"type\":\"MultiSelect\",\"number\":7},{\"name\":\"Dialog_Select\",\"label\":\"下拉列表\",\"options\":[\"选项1\",\"选项2\",\"选项3\"],\"value\":\"选项2\",\"type\":\"DropdownList\",\"number\":8}], \"FolderPath\": \"C:\\\\Users\\\\56220\\\\Documents\\\\Z-Factory\\\\projects\\\\新建前台应用(148)\", \"Title\": \"\"}";
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Dialog.ConfigDialog", "Excute", data }).ToString();
        }
        #endregion

        #region SAP 表格
        public enum TableDataScope
        {
            ALL,
            RowOnly,
            ColumnOnly,
            RowColumn
        }
        [MethodComponent("获取SAP表格数据")]
        public void SAP_GetTableData()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;
            int scopeType = cboScope.SelectedIndex;
            string row = string.Empty;
            string column = string.Empty;
            if (scopeType == (int)TableDataScope.ColumnOnly)
            {
                column = txtColumn.Text;
            }
            if (scopeType == (int)TableDataScope.RowOnly)
            {
                row = txtRow.Text;
            }
            if (scopeType == (int)TableDataScope.RowColumn)
            {
                column = txtColumn.Text;
                row = txtRow.Text;
            }
            var data = new
            {
                selector = s,
                scopeType = scopeType,
                rowNo = row,
                columnNo = column,
                containHeader = checkBox1.Checked ? 1 : 0
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetSapTableData", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }

        [MethodComponent("点击SAP单元格")]
        public void SAP_ClickTableCell()
        {
            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            string s = rtb_selector.Text;

            string row = string.Empty;
            string column = string.Empty;

            row = txtRow.Text;
            column = txtColumn.Text;
            var data = new
            {
                selector = s,
                rowNo = row,
                columnNo = column,
                clicktype = 1,
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.ClickSapTableCell", "Excute", JsonConvert.SerializeObject(data) }).ToString();
        }
        #endregion

        #region 独立的测试窗口
        #region @窗口-日志参数测试
        [MethodComponent("窗口-日志参数测试")]
        public void PluginAvtionParamTest_ShowTestForm()
        {
            PluginAvtionParamRun uhForm = new PluginAvtionParamRun(pluginLoaderObject, pluginLoaderType);
            //uhForm.Owner = this;
            uhForm.Show();
            uhForm.Activate();
        }
        #endregion
        #region @窗口-数据采集新版
        [MethodComponent("窗口-数据采集新版")]
        public void DataCollection_v2_ShowTestForm()
        {
            DataCollectionTestWnd uhForm = new DataCollectionTestWnd(pluginLoaderObject, pluginLoaderType);
            //uhForm.Owner = this;
            uhForm.Show();
            uhForm.Activate();
        }
        #endregion

        #region 外设-UsbHub
        [MethodComponent("窗口-UsbHub测试")]
        public void UsbHub_ShowTestForm()
        {
            UsbHubForm uhForm = new UsbHubForm(pluginLoaderObject, pluginLoaderType);
            uhForm.Owner = this;
            uhForm.Show();
            uhForm.Activate();
        }
        #endregion

        #region 数据库
        [MethodComponent("窗口-数据库测试")]
        public void Database_ShowTestForm()
        {
            DbForm uhForm = new DbForm(pluginLoaderObject, pluginLoaderType);
            uhForm.Owner = this;
            uhForm.Show();
            uhForm.Activate();
        }
        #endregion
        #region 数据库
        [MethodComponent("窗口-设置公共参数")]
        public void SetSharedVariableData_ShowTestForm()
        {
            SetSharedVariableData uhForm = new SetSharedVariableData(pluginLoaderObject, pluginLoaderType);
            uhForm.Owner = this;
            uhForm.Show();
            uhForm.Activate();
        }
        #endregion

        #region OutLook测试
        [MethodComponent("窗口-OutLook测试")]
        public void OutLook_ShowTestForm()
        {
            OutLookTest olForm = new OutLookTest(pluginLoaderObject, pluginLoaderType);
            olForm.Owner = this;
            olForm.Show();
            olForm.Activate();
        }
        #endregion
        #endregion
        private void button1_Click(object sender, EventArgs e)
        {
            //Mobile uhForm = new Mobile(pluginLoaderObject, pluginLoaderType);
            //uhForm.Owner = this;
            //uhForm.Show();
            //uhForm.Activate();
        }

        #region Excel      
        //"Excel执行入口"
        public void InvokeExcel(string name)
        {
            var invokeMethod = ExcelPluginInfo.ActionSetting.Where(c => c.Value.Value<string>("name") == name)?.OrderByDescending(c => c.Key)?.First().Key;
            if (invokeMethod != null)
            {
                var methodInfo = typeof(Excel.IExcel).GetMethod(invokeMethod);
                if (methodInfo != null)
                {
                    dynamic data = ReadConfigInfo(invokeMethod);
                    ExcelForm excelForm = new ExcelForm();
                    excelForm.Show();
                    excelForm.Component = name;
                    if (string.IsNullOrWhiteSpace(data))
                    {
                        if (excelForm.DefaultParamter.TryGetValue(invokeMethod, out dynamic value))
                        {
                            excelForm.Paramter = JObject.Parse(JsonConvert.SerializeObject(value)).ToString(Formatting.Indented);
                        }
                        else
                        {
                            var parameters = methodInfo.GetParameters();
                            // 封装参数信息为匿名对象
                            var parametersObj = new JObject();
                            foreach (ParameterInfo parameter in parameters)
                            {
                                parametersObj.Add(parameter.Name, "");
                            }
                            excelForm.Paramter = JObject.Parse(JsonConvert.SerializeObject(parametersObj)).ToString(Formatting.Indented);
                        }

                    }
                    else
                    {
                        excelForm.Paramter = JObject.Parse(data).ToString(Formatting.Indented);
                    }
                    excelForm.SaveConfig = new Action<string>(c =>
                    {
                        SaveConfigInfo(c, invokeMethod);
                    });
                    excelForm.Callback = new Action<string>((p) =>
                    {
                        var _param = JsonConvert.DeserializeObject<Dictionary<string, string>>(p);

                        data = new
                        {
                            Method = invokeMethod,
                            Paras = JsonConvert.SerializeObject(_param)
                        };
                        var result = methodRun.Invoke(pluginLoaderObject, new object[] { "Excel.Run", "Excute", JsonConvert.SerializeObject(data) });
                        var dicResult = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(result.ToString());
                        excelForm.Result = JObject.Parse(result.ToString()).ToString(Formatting.Indented);

                    });

                    excelForm.WindowState = FormWindowState.Normal;
                    this.WindowState = FormWindowState.Minimized;

                }
            }
        }

        #endregion

        [MethodComponent("拾取窗口")]
        public void Gui_CaptureWindow()
        {
            //CellFormatParams cellFormatParams = new CellFormatParams();
            //logger.LogInformation("cellFormatParams:" + JsonHelper.ToJson(cellFormatParams));
            //string strJson = JsonHelper.ToJson(cellFormatParams);

            //MethodInfo methodRun = pluginLoaderType.GetMethod("Run");
            var data = new
            {
                filepath = Application.StartupPath + @"\.screenshots\" + Guid.NewGuid().ToString() + ".png",
                isCVCapture = false,
                cvServer = txtCvServer.Text,
                InspectStatus = 0,
                CaptureMode = 1,
                IsCvSupport = 0
            };

            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.CaptureWindow", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }

        private void rtbResult_TextChanged(object sender, EventArgs e)
        {
            try
            {
                rtbResult.Text = ConvertJsonString(rtbResult.Text);
            }
            catch
            {
            }
        }
        [MethodComponent("元素拾取(测试)")]
        public void Gui_Test()
        {
            var data = new
            {

            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.Test", "Excute", "{}" }).ToString();
        }
        private void RPATest_DoubleClick(object sender, EventArgs e)
        {

        }
        [MethodComponent("根据位置获取元素")]
        public void GetSelectorByPoint()
        {
            var data = new
            {
                filepath = Application.StartupPath + @"\.screenshots\" + Guid.NewGuid().ToString() + ".png",
                left = Int32.Parse(txtOffX.Text),
                top = Int32.Parse(txtOffY.Text)
            };
            rtbResult.Text = methodRun.Invoke(pluginLoaderObject, new object[] { "Element.GetSelectorByPoint", "Excute", JsonConvert.SerializeObject(data) }).ToString();
            var msg = rtbResult.Text.ToObject<JMessage>();
            if (msg == null)
                return;
            if (msg.Code == 0)
                rtb_selector.Text = ConvertJsonString(rtbResult.Text.ToObject<JMessage>()?.Info);
        }
    }
}
