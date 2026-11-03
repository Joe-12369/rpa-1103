using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WF = System.Windows.Forms;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;


namespace II.RPA.Plugin.Dialog.UCControl
{
    /// <summary>
    /// UCUpload.xaml 的交互逻辑
    /// </summary>
    public partial class UCUpload : UserControl, IRpaCustomControl
    {
        private InputModel _inputModel;
        private static ILogger logger = Logger.OfContext();

        public UCUpload()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
        }

        private void GroupBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                int count = ((System.Array)e.Data.GetData(System.Windows.DataFormats.FileDrop)).Length;
                for (int i = 0; i < count; i++)
                {
                    string dropFile = ((System.Array)e.Data.GetData(System.Windows.DataFormats.FileDrop)).GetValue(i).ToString();
                    NewFileText(System.IO.Path.GetFileName(dropFile));
                }
            }
        }

        private void NewFileText(string fileName)
        {
            try
            {
                var t = new SubUpload();
                t.SetFileName(fileName);
                t.FileDelete += Event_FileDelete;
                this.SP.Children.Add(t);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Set Value Error");
                throw;
            }
        }

        private void Event_FileDelete(object sender, RoutedEventArgs e)
        {
            this.SP.Children.Remove(sender as FrameworkElement);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WF.OpenFileDialog openFileDialog = new WF.OpenFileDialog();
            if (openFileDialog.ShowDialog() == WF.DialogResult.OK)
            {
                NewFileText(System.IO.Path.GetFileName(openFileDialog.FileName));
            }
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
                        foreach (var item in input.Value.Split(','))
                        {
                            NewFileText(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Set Value Error");
                throw;
            }
        }

        public string GetResult()
        {
            string ret = "";
            try
            {
                List<string> retList = new List<string>();
                foreach (SubUpload item in SP.Children)
                {
                    retList.Add(item.GetFileName());
                }
                ret = string.Join(",", retList);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Set Value Error");
                throw;
            }
            return ret;
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Set Value Error");
            }
        }

    }
}

