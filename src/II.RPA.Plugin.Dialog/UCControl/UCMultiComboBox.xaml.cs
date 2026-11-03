using II.RPA.Plugin.Dialog.Depends;
using II.RPA.WinAutomation.CommonUtility;
using Panuon.UI.Silver;
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
using System.ArrayExtensions;
using II.RPA.Plugin.Dialog.Helpers;

namespace II.RPA.Plugin.Dialog.UCControl
{
    /// <summary>
    /// UCMultiComboBox.xaml 的交互逻辑
    /// </summary>
    public partial class UCMultiComboBox : UserControl, IRpaCustomControl, IRequired
    {
        private InputModel _inputModel;
        public DependRequired dependRequired = new DependRequired();
        private static ILogger logger = Logger.OfContext();

        public UCMultiComboBox()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            this.DataContext = dependRequired;
            this.Loaded += UCMultiComboBox_Loaded;
        }

        private void UCMultiComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            //VerifySelectedItemEmpty();
        }

        public List<string> ItemValueSelectedList { get; set; } = new List<string>();


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

                    if (input.Options != null && input.ValueArray != null)
                    {
                        List<string> ValueList = input.Options.ToList();
                        this.MCMB.ItemsSource = ValueList;
                        //this.MCMB.SelectionList = input.ValueArray;
                        //input.ValueArray.Select(v => this.MCMB.SelectedItems.Add(v)).ToArray();
                        ItemValueSelectedList.AddRange(input.ValueArray);
                        //this.MCMB.SelectedItems.Add(ValueList[0]);
                    }
                }
                //初始化 给默认颜色
                dependRequired.WarningColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D5E0"));
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Set Value Error");
                throw;
            }
        }

        public string GetResult()
        {
            List<string> select = new List<string>();
            foreach (var item in MCMB.SelectedItems)
            {
                select.Add(item.ToString());
            }
            return string.Join(",", select);
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Set Value Error");
            }
        }

        private void SetSelect(string[] lastList)
        {
            foreach (MultiComboBoxItem item in MCMB.Items)
            {
                if (lastList.Contains(item.Content))
                {
                    item.IsSelected = true;
                }
                else
                {
                    item.IsSelected = false;
                }
            }
        }

        public bool IsEmpty(bool isFouce = true)
        {
            if (!string.IsNullOrEmpty(_inputModel.IsRequired) && _inputModel.IsRequired == "1" && MCMB.SelectedItems.Count == 0)
            {
                if (isFouce)
                {
                    MCMB.Focus();
                }
                return true;
            }
            return false;
        }

        private void MCMB_MouseEnter(object sender, MouseEventArgs e)
        {
            dependRequired.WarningColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8910"));
        }

        private void MCMB_MouseLeave(object sender, MouseEventArgs e)
        {
            VerifySelectedItemEmpty();
        }

        public bool HasInit { get; set; } = false;

        private void VerifySelectedItemEmpty(bool isInit = false)
        {
            if (!HasInit)
            {
                if (!isInit)
                {
                    return;
                }
                HasInit = true;
            }

            if (!string.IsNullOrEmpty(_inputModel.IsRequired) && _inputModel.IsRequired == "1" && MCMB.SelectedItems.Count == 0)
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

        public void Refresh()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.MCMB.SelectedItems.Clear();
                if (ItemValueSelectedList.Count > 0)
                {
                    foreach (var item in ItemValueSelectedList)
                    {
                        var target = MCMB.ItemsSource.OfType<string>().FirstOrDefault(i => i.Equals(item, StringComparison.OrdinalIgnoreCase));
                        if (target != null)
                        {
                            MCMB.SelectedItems.Add(target);
                        }
                    }
                }

                this.MCMB.UpdateTags();

                VerifySelectedItemEmpty(true);
            }));
        }
    }
}

