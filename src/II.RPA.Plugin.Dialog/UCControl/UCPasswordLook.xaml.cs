using II.RPA.Plugin.Dialog.Depends;
using II.RPA.WinAutomation.CommonUtility;
using Rubyer;
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
    /// UCPassword.xaml 的交互逻辑
    /// </summary>
    public partial class UCPasswordLook : UserControl, IRpaCustomControl, IRequired, IInputFocusControl
    {
        private InputModel _inputModel;
        public DependRequired dependRequired = new DependRequired();
        private static ILogger logger = Logger.OfContext();

        public UCPasswordLook()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            DateTimePicker dateTimePicker = new DateTimePicker();
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
                        this.BOX.Password = input.Value;
                    }
                    this.Resources["Tips"] = input.Tips;
                    BOX_MouseLeave(null, null);
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
            return BOX.Password;
        }

        public bool FocusInput()
        {
            BOX.Focus();
            Keyboard.Focus(BOX);
            return BOX.IsKeyboardFocusWithin;
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

        private void BOX_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            };
            BOX.RaiseEvent(eventArg);
        }

        public bool IsEmpty(bool isFouce = true)
        {
            if (!string.IsNullOrEmpty(_inputModel.IsRequired) && _inputModel.IsRequired == "1" && string.IsNullOrEmpty(BOX.Password))
            {
                if (isFouce)
                {
                    BOX.Focus();
                }
                return true;
            }
            return false;
        }

        private void BOX_MouseEnter(object sender, MouseEventArgs e)
        {
            dependRequired.WarningColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8910"));
        }

        private void BOX_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!string.IsNullOrEmpty(_inputModel.IsRequired) && _inputModel.IsRequired == "1" && string.IsNullOrEmpty(BOX.Password))
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

