using II.RPA.WinAutomation.CommonUtility;
using Panuon.UI;
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
using II.RPA.Logging;
using Microsoft.Extensions.Logging;


namespace II.RPA.Plugin.Dialog.UCControl
{
    /// <summary>
    /// UCComboBox.xaml 的交互逻辑
    /// </summary>
    public partial class UCComboBox : UserControl, IRpaCustomControl
    {
        private InputModel _inputModel;
        private static ILogger logger = Logger.OfContext();

        public UCComboBox()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
        }

        public string InputValue { get; set; } = string.Empty;


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
                    if (input.Options != null)
                    {
                        var list = input.Options;

                        for (int i = 0; i < list.Length; i++)
                        {
                            if (input.Value == list[i])
                            {
                                InputValue = input.Value;
                                break;
                            }
                        }

                        this.CMB.ItemsSource = list;
                        
                    }
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
            return CMB?.SelectedValue?.ToString();
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

        private void CMB_MouseEnter(object sender, MouseEventArgs e)
        {
            CMB.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8910"));
        }

        private void CMB_MouseLeave(object sender, MouseEventArgs e)
        {
            CMB.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D5E0"));
        }

        public void Refresh(string inputTempValue = "")
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
            if (string.IsNullOrEmpty(inputTempValue))
            {
                this.CMB.SelectedItem = InputValue;
            }
            else
            {
                this.CMB.SelectedItem = inputTempValue;
            }

            this.CMB.UpdateTags();
            }));
        }

    }
}

