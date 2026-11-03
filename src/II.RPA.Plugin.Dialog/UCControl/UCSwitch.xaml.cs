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
using II.RPA.Logging;
using Microsoft.Extensions.Logging;


namespace II.RPA.Plugin.Dialog.UCControl
{
    /// <summary>
    /// UCSwitch.xaml 的交互逻辑
    /// </summary>
    public partial class UCSwitch : UserControl, IRpaCustomControl
    {
        private InputModel _inputModel;
        private static ILogger logger = Logger.OfContext();

        public UCSwitch()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
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
                    this.CBOX.IsChecked = input.ValueNumber == 1;
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
            return this.CBOX.IsChecked.Value ? "1" : "0";
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
            _inputModel.ValueNumber = Convert.ToInt32(GetResult());
            return _inputModel;
        }

        public void SetLast(OutputModel model, InputModel input)
        {
            try
            {
                input.ValueNumber = Convert.ToInt32(model.value);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Set Value Error");
            }
        }
    }
}

