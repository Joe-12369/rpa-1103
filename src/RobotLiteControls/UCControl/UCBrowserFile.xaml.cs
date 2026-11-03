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
using SWC = System.Windows.Controls;

namespace RobotLiteControls.UCControl
{
    /// <summary>
    /// UCBrowserFile.xaml 的交互逻辑
    /// </summary>
    public partial class UCBrowserFile : SWC.UserControl, IRpaCustomControl
    {
        private InputModel _inputModel;
        private string _lastLocation;
        /// <summary>
        /// 文件还是文件夹（1文件、2文件夹）
        /// </summary>
        private string _fileOrFolder;
        public UCBrowserFile()
        {
            InitializeComponent();
        }

        public void Init(InputModel input)
        {
            try
            {
                if(input!=null)
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
                }
            }
            catch (Exception ex)
            {
                NLogger.Default.Error("Set Value Error", ex);
                throw ex;
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
                NLogger.Default.Error("Set Value Error", ex);
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
                NLogger.Default.Error("Set Value Error", ex);
                throw ex;
            }
        }
    }
}
