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
    /// UCCheck.xaml 的交互逻辑
    /// </summary>
    public partial class UCCheck : UserControl, IRpaCustomControl, IRequired
    {
        private InputModel _inputModel;
        public DependRequired dependRequired = new DependRequired();
        private static ILogger logger = Logger.OfContext();

        public UCCheck()
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
                    if (input.Direction == 2)
                    {
                        this.SP.Orientation = Orientation.Vertical;
                    }
                    if (!string.IsNullOrEmpty(input.Name))
                    {
                        this.Name = input.Name;
                    }
                    if (!string.IsNullOrEmpty(input.IsRequired) && input.IsRequired == "1")
                    {
                        REQUIRED.Visibility = Visibility.Visible;
                        dependRequired.WarningColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6151"));
                        dependRequired.EmptyVisibity = Visibility.Visible;
                    }
                    else
                    {
                        dependRequired.WarningColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D5E0"));
                        dependRequired.EmptyVisibity = Visibility.Collapsed;
                    }
                    if (!string.IsNullOrEmpty(input.Label))
                    {
                        this.LAB.Content = input.Label;
                    }
                    if (input.Options != null && input.ValueArray != null)
                    {
                        var list = input.Options;
                        var defaultList = input.ValueArray;
                        for (int i = 0; i < list.Length; i++)
                        {
                            var but = new CheckBox
                            {
                                Content = list[i]
                            };
                            but.Click += CheckBox_Click;
                            but.Style = this.Resources["CheckBoxStyle_Main"] as Style;
                            //默认值
                            if (defaultList.Contains(list[i]))
                            {
                                but.IsChecked = true;
                            }
                            this.SP.Children.Add(but);
                        }
                        CheckBox_Click(null,null);
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
            try
            {
                List<string> ret = new List<string>();
                foreach (CheckBox child in SP.Children)
                {
                    if (child.IsChecked.Value)
                    {
                        ret.Add(child.Content.ToString());
                    }
                }
                return string.Join(",", ret);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex,"Set Value Error");
                throw;
            }
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
            _inputModel.ValueArray = GetResult().Split(',');
            return _inputModel;
        }

        public void SetLast(OutputModel model, InputModel input)
        {
            try
            {
                input.ValueArray = model.value.Split(',');
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex,"Set Value Error");
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_inputModel.IsRequired) && _inputModel.IsRequired == "1" && string.IsNullOrEmpty(GetResult()))
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

        public bool IsEmpty(bool isFouce = true)
        {
            if (!string.IsNullOrEmpty(_inputModel.IsRequired) && _inputModel.IsRequired == "1" && string.IsNullOrEmpty(GetResult()))
            {
                if (isFouce)
                { 
                    SP.Focus();
                }
                return true;
            }
            return false;
        }
    }
}

