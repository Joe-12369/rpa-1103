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

namespace RobotLiteControls.UCControl
{
    /// <summary>
    /// UCCheck.xaml 的交互逻辑
    /// </summary>
    public partial class UCCheck : UserControl, IRpaCustomControl
    {
        private InputModel _inputModel;
        public UCCheck()
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
                    if (input.Direction == 2)
                    {
                        this.SP.Orientation = Orientation.Vertical;
                    }
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
                            var but = new CheckBox
                            {
                                Content = list[i]
                            };
                            //默认值
                            if (defaultList.Contains(list[i]))
                            {
                                but.IsChecked = true;
                            }
                            this.SP.Children.Add(but);
                        }
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
                NLogger.Default.Error("Set Value Error", ex);
                throw ex;
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
                NLogger.Default.Error("Set Value Error", ex);
            }
        }
    }
}
