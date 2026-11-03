using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Dialog.Depends;
using II.RPA.Plugin.Dialog.Params;
using II.RPA.Plugin.Dialog.UCControl;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.WinAutomation.CommonUtility.XamlResource;
using System.Data;
using NPOI.OpenXmlFormats.Vml;
using HandyControl.Controls;
using System.Reflection;
using System.Windows.Interop;

namespace II.RPA.Plugin.Dialog
{
    /// <summary>
    /// ConfigSetWnd.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigSetWnd : System.Windows.Window
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr ptr);
        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);
        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public static int DpiY
        {
            get
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                int DpiX = GetDeviceCaps(hdc, 90);
                ReleaseDC(IntPtr.Zero, hdc);
                return DpiX;
            }
        }
        List<InputModel> InputList;
        public string outJson;
        private string _defaultTitle = eCommonMessageType.ConfigSetWnd_xaml_cs_ShuChuDuiHuaKuang.GetMessage();
        private readonly string _packagePath;
        private readonly ExeMode _exeMode;
        private readonly AutoOut _autoOutParam;
        private AutoOutSource autoOutSource;
        private static ILogger logger = Logger.OfContext();
        public ConfigSetWnd(List<InputModel> json, string saveLocation, string title = "", ExeMode exeMode = ExeMode.Exe, AutoOut autoOutParam = null)
        {
            InitializeComponent();
            LanguageResourceHelper.GetInstance().SetAppInstance(this);
            //RibbonGroupBox window = new RibbonGroupBox();
            _packagePath = saveLocation;
            _exeMode = exeMode;
            _autoOutParam = autoOutParam;
            //ParseJson(json);
            InputList = json;
            if (!string.IsNullOrEmpty(title))
            {
                _defaultTitle = title;
            }
            this.TITLE.Text = _defaultTitle;
            //this.Windows.MaxHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height * 96 / DpiY;
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 7;
            GenerateControl();
            autoOutSource = new AutoOutSource()
            {
                IsEnable = _autoOutParam?.IsEnable == EmOpenAutoOut.Open ? Visibility.Visible : Visibility.Collapsed,
                OutTime = _autoOutParam?.Time ?? 0,
                Tip = GitTip(_autoOutParam),
            };
            if (_autoOutParam != null && _autoOutParam.IsEnable == EmOpenAutoOut.Open)
            {
                StartAutoOut(_autoOutParam);
                this.GotFocus += new System.Windows.RoutedEventHandler(this.Windows_GotFocus);
                AddHandler(MouseDownEvent, new MouseButtonEventHandler(MouseDownEventHandler), true);
            }
            this.DataContext = autoOutSource;

            this.Topmost = true;

            GetInterval();
        }

        private void MouseDownEventHandler(object sender, MouseButtonEventArgs e)
        {
            CloseAutoOut();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WndProc));
        }

        private string GitTip(AutoOut autoOut)
        {
            return autoOut?.OutState == EmAutoOutStste.Confirm && !HasEmpty() ? eCommonMessageType.ConfigSetWnd_xaml_cs_JiShiWeiCaoZuoJiangZiDongZhiXing.GetMessage() : eCommonMessageType.ConfigSetWnd_xaml_cs_JiShiWeiCaoZuoJiangZiDongZhiXing1.GetMessage();
        }

        public bool HasEmpty()
        {
            foreach (var children in ParameterPanl.Children)
            {
                if (children is IRequired required)
                {
                    if (required.IsEmpty(false))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void GenerateControl()
        {
            if (InputList != null)
            {
                var orderList = InputList.OrderBy(x => int.Parse(x.Number));

                foreach (var item in orderList)
                {
                    try
                    {
                        switch (item.Type)
                        {
                            case "DataTableWithSheet":
                                var uc_DataTableWithSheet = new UCDataTableWithSheet(_exeMode);
                                uc_DataTableWithSheet.Init(item);
                                ParameterPanl.Children.Add(uc_DataTableWithSheet);
                                break;
                            case "TextBox":
                                var uCTextBox = new UCTextBox();
                                uCTextBox.Init(item);
                                ParameterPanl.Children.Add(uCTextBox);
                                break;
                            case "Password":
                                var uCPassword = new UCPassword();
                                uCPassword.Init(item);
                                ParameterPanl.Children.Add(uCPassword);
                                break;
                            case "Radio":
                                var uCRedioH = new UCRedioH();
                                uCRedioH.Init(item);
                                ParameterPanl.Children.Add(uCRedioH);
                                break;
                            case "CheckBox":
                                var uCCheck = new UCCheck();
                                uCCheck.Init(item);
                                ParameterPanl.Children.Add(uCCheck);
                                break;
                            case "DropdownList":
                                var uCComboBox = new UCComboBox();
                                uCComboBox.Init(item);
                                ParameterPanl.Children.Add(uCComboBox);
                                break;
                            case "MultiSelect":
                                var uCMultiComboBox = new UCMultiComboBox();
                                uCMultiComboBox.Init(item);
                                ParameterPanl.Children.Add(uCMultiComboBox);
                                break;
                            case "Date":
                                var uCDate = new UCDate();
                                uCDate.Init(item);
                                ParameterPanl.Children.Add(uCDate);
                                break;
                            case "Label":
                                var uCLabel = new UCLabel();
                                uCLabel.Init(item);
                                ParameterPanl.Children.Add(uCLabel);
                                break;
                            case "TextArea":
                                var uCTextField = new UCTextField();
                                uCTextField.Init(item);
                                ParameterPanl.Children.Add(uCTextField);
                                break;
                            case "File":
                                var uCBrowserFile = new UCBrowserFile();
                                uCBrowserFile.Init(item);
                                ParameterPanl.Children.Add(uCBrowserFile);
                                break;
                            case "UploadFile":
                                var uCUpload = new UCUpload();
                                uCUpload.Init(item);
                                ParameterPanl.Children.Add(uCUpload);
                                break;
                            case "Line":
                                var uCSeparator = new UCSeparator();
                                uCSeparator.Init(item);
                                ParameterPanl.Children.Add(uCSeparator);
                                break;
                            case "Switch":
                                var uCSwitch = new UCSwitch();
                                uCSwitch.Init(item);
                                ParameterPanl.Children.Add(uCSwitch);
                                break;
                            case "Table":
                                var uCTable = new UCTable();
                                uCTable.Init(item);
                                ParameterPanl.Children.Add(uCTable);
                                break;
                            case "DataTable":
                                var uCDataTable = new UCDataTable(_packagePath, _exeMode);
                                uCDataTable.Init(item);
                                ParameterPanl.Children.Add(uCDataTable);
                                break;
                            case "Annex":
                                var uCAnnex = new UCAnnex(_packagePath);
                                uCAnnex.Init(item);
                                ParameterPanl.Children.Add(uCAnnex);
                                break;
                            case "AnnexUpgrade":
                                var uCAnnexUpgrade = new UCAnnex(_packagePath);
                                uCAnnexUpgrade.Init(item);
                                ParameterPanl.Children.Add(uCAnnexUpgrade);
                                break;
                            case "Static":
                                IsRemember.IsChecked = item.Value == "1";
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        string errMessage = string.Empty;
                        if (ex is RPAException RpaEx)
                        {
                            errMessage = RpaEx.CustomMessage ?? RpaEx.ErrorMessage;
                        }
                        else
                        {
                            errMessage = ex.Message;
                        }


                        string errMsgSummary = string.Format("自定义对话框的第{0}个控件运行异常，控件名称:{1}，异常信息:{2}".I18n("ConfigSetWnd.xaml_708B22D4"),
                            item.Number, item.Label ?? "UnKnow", errMessage ?? "参数配置错误");

                        string exceptionMsg = string.Empty;
                        if (ex is DuplicateNameException)
                        {
                            exceptionMsg = "，检测到参数重复".I18n("ConfigSetWnd.xaml_5EC51C5C");
                        }

                        errMsgSummary += exceptionMsg;

                        logger.LogError($"控件{item.Number}:{item.Label}生成出现问题:{ex.ToString()}");
                        throw new RPAException(eErrorMessageType.Error_ParamsError, eComponentType.ConfigDialog, errMsgSummary);
                    }
                }
            }
        }

        private void ParseJson(string json)
        {
            try
            {
                InputList = JsonConvert.DeserializeObject<List<InputModel>>(json);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ParseJson Error:");
                throw new Exception("配置的参数有误请检查".I18n("controlType_ConfigSetWnd.xaml_1F4EA7C0"));
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Ok();
        }

        private void Ok()
        {
            try
            {
                if (_exeMode == ExeMode.Exe)
                {
                    foreach (var children in ParameterPanl.Children)
                    {
                        if (children is IRequired required)
                        {
                            if (required.IsEmpty())
                            {
                                return;
                            }
                        }
                    }
                }
                var templist = new List<InputModel>();
                foreach (var children in ParameterPanl.Children)
                {
                    if (children is IGetInputModelControl)
                    {
                        //OutputModel sub = ((IRpaCustomControl)children).GetOutputModel();
                        InputModel sub = ((IGetInputModelControl)children).GetInputModel();
                        templist.Add(sub);
                    }
                }
                InputModel inputModel = new InputModel
                {
                    Name = "isRemember",
                    Type = "Static",
                    Value = IsRemember.IsChecked.Value ? "1" : "0",
                    Number = "0"
                };
                templist.Add(inputModel);
                var jsonSetting = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() };
                outJson = JsonConvert.SerializeObject(templist, Formatting.Indented, jsonSetting);
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "确认按下时异常");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 预览删除临时文件
        /// </summary>
        private void Windows_Closed(object sender, EventArgs e)
        {
            if (_exeMode == ExeMode.Pre)
            {
                foreach (var children in ParameterPanl.Children)
                {
                    if (children is UCDataTable uCDataTable)
                    {
                        uCDataTable.DeleteTempFile();
                    }
                }
            }
        }

        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (WindowState == WindowState.Maximized)
                    SystemCommands.RestoreWindow(this);
                else
                    SystemCommands.MaximizeWindow(this);
            }
            this.DragMove();
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            //SystemCommands.MinimizeWindow(this);
        }

        private void RestoreWindow_Exec(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        private void MaximizeWindow_Exec(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        private void Windows_GotFocus(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.ScrollViewer)
            {
                e.Handled = true;
                return;
            }
            logger.LogInformation("Windows_GotFocus");
            this.GotFocus -= new System.Windows.RoutedEventHandler(this.Windows_GotFocus);
            if (e.Source.ToString().Contains("UCMultiComboBox"))
            {
                return;
            }
            if (e.OriginalSource is System.Windows.Controls.RadioButton rb)
            {

                rb.IsChecked = true;
            }
            if (e.OriginalSource is System.Windows.Controls.CheckBox cb)
            {
                cb.IsChecked = !cb.IsChecked;
            }
        }

        DispatcherTimer timerQuit = new DispatcherTimer();
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (autoOutSource.OutTime > 0)
            {
                autoOutSource.OutTime--;
            }
            else
            {
                AutoOut(_autoOutParam);
            }
        }

        /// <summary>
        /// 启动自动退出
        /// </summary>
        private void StartAutoOut(AutoOut autoOut)
        {
            autoOutSource.IsEnable = Visibility.Visible;
            autoOutSource.OutTime = autoOut.Time;
            timerQuit.Interval = TimeSpan.FromMilliseconds(1000);
            timerQuit.Tick += new EventHandler(dispatcherTimer_Tick);
            timerQuit.Start();
        }
        /// <summary>
        /// 关闭自动退出
        /// </summary>
        private void CloseAutoOut()
        {
            timerQuit.Tick -= new EventHandler(dispatcherTimer_Tick);
            timerQuit.Stop();
            autoOutSource.IsEnable = Visibility.Collapsed;
        }

        /// <summary>
        /// 自动退出
        /// </summary>
        private void AutoOut(AutoOut autoOut)
        {
            if (autoOut.OutState == EmAutoOutStste.Confirm && !HasEmpty())
            {
                Ok();
            }
            else
            {
                this.Close();
            }
        }

        public int ComboboxControlRefreshInterval { get; set; } = 250;

        private void Windows_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(async () =>
            {
                await this.Dispatcher.InvokeAsync(async () =>
                {
                    foreach (var item in ParameterPanl.Children)
                    {
                        if (item is UCMultiComboBox multiComboBox)
                        {
                            await Task.Delay(ComboboxControlRefreshInterval);
                            multiComboBox.Refresh();
                        }

                        if (item is UCComboBox ComboBox)
                        {
                            await Task.Delay(ComboboxControlRefreshInterval);
                            ComboBox.Refresh();
                        }
                    }
                });
            });
        }

        private static string ReadConfig()
        {
            string fileName = "dialog.txt";
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string directoryPath = System.IO.Path.GetDirectoryName(assemblyPath);
            string ConfigPath = System.IO.Path.Combine(directoryPath, fileName);
            logger.LogDebug($"Read Config:{ConfigPath}");
            if (File.Exists(ConfigPath))
            {
                string jsonContent = File.ReadAllText(ConfigPath);
                return jsonContent;
            }
            else
            {
                return "";
            }
        }

        public void GetInterval()
        {
            string content = ReadConfig();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            if (int.TryParse(content, out var interval))
            {
                if (interval >= 0)
                {
                    ComboboxControlRefreshInterval = interval;
                    logger.LogInformation($"Set RefreshInterval:{ComboboxControlRefreshInterval}");
                }
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MAXIMIZE = 0xF030;

            if (msg == WM_SYSCOMMAND && wParam.ToInt32() == SC_MAXIMIZE)
            {
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                    this.Left = RestoreBounds.Left;
                    this.Top = RestoreBounds.Top;
                    this.Width = RestoreBounds.Width;
                    this.Height = RestoreBounds.Height;
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void Windows_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseAutoOut();
            RemoveHandler(MouseDownEvent, new MouseButtonEventHandler(MouseDownEventHandler));
        }
    }
}

