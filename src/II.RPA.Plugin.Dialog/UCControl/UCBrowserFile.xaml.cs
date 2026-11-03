using II.RPA.Plugin.Dialog.Depends;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using SWC = System.Windows.Controls;

namespace II.RPA.Plugin.Dialog.UCControl
{
    /// <summary>
    /// UCBrowserFile.xaml 的交互逻辑
    /// </summary>
    public partial class UCBrowserFile : SWC.UserControl, IRpaCustomControl, IRequired
    {
        /// <summary>
        /// 文件还是文件夹（1文件、2文件夹）
        /// </summary>
        private string _fileOrFolder;
        private InputModel _inputModel;
        private string _lastLocation;
        private static ILogger logger = Logger.OfContext();
        public DependRequired dependRequired = new DependRequired();
        public UCBrowserFile()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            this.DataContext = dependRequired;
        }

        public void Init(InputModel input)
        {
            try
            {
                if (input != null)
                {
                    _inputModel = input;
                    if (!string.IsNullOrEmpty(input.Name))
                    {
                        this.Name = input.Name;
                    }
                    if (!string.IsNullOrEmpty(input.Label))
                    {
                        this.LAB.Content = input.Label;
                    }
                    if (!string.IsNullOrEmpty(input.Value))
                    {
                        _lastLocation = input.Value;
                        txtSelect.Text = input.Value;
                    }
                    this.Resources["Tips"] = input.Tips;
                    _fileOrFolder = input.FileOrFolder;
                    txtSelect_MouseLeave(null, null);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex,"Set Value Error");
                throw;
            }
        }

        public string GetResult()
        {
            return txtSelect.Text;
        }

        public OutputModel GetOutputModel()
        {
            var outDto = new OutputModel
            {
                name = this.Name,
                value = GetResult(),
            };
            return outDto;
        }

        public InputModel GetInputModel()
        {
            _inputModel.Value = GetResult();
            return _inputModel;
        }

        public void SetLast(OutputModel model, InputModel input)
        {
            try
            {
                input.Value = model.value;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex,"Set Value Error");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_fileOrFolder == "openFile")
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    if (!string.IsNullOrEmpty(_lastLocation))
                    {
                        openFileDialog.FileName = _lastLocation;
                    }
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        txtSelect.Text = openFileDialog.FileName;
                    }
                }
                else
                {
                    FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                    if (!string.IsNullOrEmpty(_lastLocation))
                    {
                        folderBrowserDialog.SelectedPath = _lastLocation;
                    }
                    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    {
                        txtSelect.Text = folderBrowserDialog.SelectedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex,"Set Value Error");
                throw;
            }
        }

        public bool IsEmpty(bool isFouce= true)
        {
            if (!string.IsNullOrEmpty(_inputModel.IsRequired) && _inputModel.IsRequired == "1" && string.IsNullOrEmpty(txtSelect.Text))
            {
                if (isFouce)
                { 
                    txtSelect.Focus();
                }
                return true;
            }
            return false;
        }

        private void txtSelect_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            dependRequired.WarningColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8910"));
        }

        private void txtSelect_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!string.IsNullOrEmpty(_inputModel.IsRequired) && _inputModel.IsRequired == "1" && string.IsNullOrEmpty(txtSelect.Text))
            {
                dependRequired.WarningColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6151"));
                dependRequired.EmptyVisibity = Visibility.Visible;
            }
            else
            {
                dependRequired.WarningColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D5E0"));
                dependRequired.EmptyVisibity = Visibility.Collapsed;
            }
        }
    }
}

