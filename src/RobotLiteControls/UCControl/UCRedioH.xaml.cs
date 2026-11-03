using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// UCRedioH.xaml 的交互逻辑
    /// </summary>
    public partial class UCRedioH : UserControl, IRpaCustomControl
    {
        private InputModel _inputModel;
        public UCRedioH()
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
                    if (input.Options != null)
                    {
                        var list = input.Options;
                        for (int i = 0; i < list.Length; i++)
                        {
                            var but = new RadioButton
                            {
                                Content = list[i]
                            };
                            //默认值
                            if (input.Value == list[i])
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
            string ret = "";
            try
            {
                foreach (RadioButton child in SP.Children)
                {
                    if (child.IsChecked.Value)
                    {
                        ret = child.Content.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                NLogger.Default.Error("Set Value Error", ex);
                throw ex;
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
                NLogger.Default.Error("Set Value Error", ex);
            }
        }

    }
}
