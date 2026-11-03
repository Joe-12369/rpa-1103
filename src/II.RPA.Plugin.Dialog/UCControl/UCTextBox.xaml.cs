using II.RPA.Plugin.Dialog.Depends;
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
    /// UCTextBox.xaml 的交互逻辑
    /// </summary>
    public partial class UCTextBox : UserControl, IRpaCustomControl, IRequired, IInputFocusControl
    {
        private InputModel _inputModel;
        public DependRequired dependRequired = new DependRequired();
        private static ILogger logger = Logger.OfContext();

        public UCTextBox()
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
                        this.TB.Text = input.Value;
                    }
                    TB_MouseLeave(null, null);
                    this.Resources["Tips"] = input.Tips;
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
            return this.TB.Text;
        }

        public bool FocusInput()
        {
            TB.Focus();
            Keyboard.Focus(TB);
            TB.CaretIndex = TB.Text.Length;
            return TB.IsKeyboardFocusWithin;
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

        public bool IsEmpty(bool isFouce = true)
        {
            if (!string.IsNullOrEmpty(_inputModel.IsRequired) && _inputModel.IsRequired == "1" && string.IsNullOrEmpty(TB.Text))
            {
                if (isFouce)
                {
                    TB.Focus();
                }
                return true;
            }
            return false;
        }

        private void TB_MouseEnter(object sender, MouseEventArgs e)
        {
            dependRequired.WarningColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8910"));
        }

        private void TB_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!string.IsNullOrEmpty(_inputModel.IsRequired) && _inputModel.IsRequired == "1" && string.IsNullOrEmpty(TB.Text))
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

