using Fluent;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RobotLiteControls.UCControl;
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

namespace RobotLiteControls
{
    /// <summary>
    /// ConfigSetWnd.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigSetWnd : RibbonWindow
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
        private string _defaultTitle = "输出对话框";
        private Version _currentVersion;
        private readonly Version _version585 = new Version("5.8.5");
        private readonly string _packagePath;
        public ConfigSetWnd(string json, string saveLocation, string title = "", string var ="1.0")
        {
            InitializeComponent();
            _currentVersion = new Version(var);
            _packagePath = saveLocation;
            ParseJson(json);
            if (!string.IsNullOrEmpty(title))
            {
                _defaultTitle = title;
            }
            this.Windows.Title = "   " + _defaultTitle;
            this.Windows.MaxHeight = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height * 9 / 10 * 96 / DpiY;
            GenerateControl();
            this.Topmost = true;
        }


        private void GenerateControl()
        {
            if (InputList != null)
            {
                var orderList = InputList.OrderBy(x => int.Parse(x.Number));
                if (_currentVersion >= _version585)
                {
                    IsRemember.Visibility = Visibility.Visible;
                }
                foreach (var item in orderList)
                {
                    switch (item.Type)
                    {
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
                        case "Annex":
                            var uCAnnex = new UCAnnex(_packagePath);
                            uCAnnex.Init(item);
                            ParameterPanl.Children.Add(uCAnnex);
                            break;
                        case "Static":
                            IsRemember.IsChecked = item.Value == "1";
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void ParseJson(string json)
        {
            try
            {
                InputList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<InputModel>>(json);
            }
            catch (Exception ex)
            {
                NLogger.Default.Error("ParseJson Error:", ex);
                throw new Exception("配置的参数有误请检查");
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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
                InputModel inputModel = new InputModel();
                inputModel.Name = "isRemember";
                inputModel.Type = "Static";
                inputModel.Value = IsRemember.IsChecked.Value ? "1":"0";
                inputModel.Number = "0";
                templist.Add(inputModel);
                var jsonSetting = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() };
                outJson = JsonConvert.SerializeObject(templist, Formatting.Indented, jsonSetting);
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                NLogger.Default.Error("Set Value Error", ex);
            }
        }

        private void BtnClose_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
