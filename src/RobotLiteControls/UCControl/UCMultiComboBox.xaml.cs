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

namespace RobotLiteControls.UCControl
{
    /// <summary>
    /// UCMultiComboBox.xaml 的交互逻辑
    /// </summary>
    public partial class UCMultiComboBox : UserControl, IRpaCustomControl
    {
        private InputModel _inputModel;
        public UCMultiComboBox()
        {
            InitializeComponent();
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
                    if (input.Options != null && input.ValueArray != null)
                    {
                        var list = input.Options;
                        var defaultList = input.ValueArray;
                        for (int i = 0; i < list.Length; i++)
                        {
                            var cmbi = new MultiComboBoxItem
                            {
                                Content = list[i]
                            };
                            this.MCMB.Items.Add(cmbi);
                        }
                        MCMB.Text = string.Join(",", input.ValueArray);
                    }
                    if (input.ValueArray != null)
                    {
                        SetSelect(input.ValueArray);
                    }
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
            List<string> select = new List<string>();
            foreach (MultiComboBoxItem item in MCMB.SelectedItems)
            {
                select.Add(item.Content.ToString());
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
                NLogger.Default.Error("Set Value Error", ex);
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
    }
}
