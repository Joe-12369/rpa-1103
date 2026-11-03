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
    /// UCSeparator.xaml 的交互逻辑
    /// </summary>
    public partial class UCSeparator : UserControl, IGetInputModelControl
    {
        private InputModel _inputModel;
        public UCSeparator()
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
                    //这个允许为空吧
                    //if (!string.IsNullOrEmpty(input.label))
                    {
                        this.LAB.Content = input.Label;
                    }
                    if (input.Height != 0)
                    {
                        this.LINE.StrokeThickness = input.Height;
                    }
                    if (!string.IsNullOrEmpty(input.Color))
                    {
                        BrushConverter brushConverter = new BrushConverter();
                        this.LINE.Stroke = (Brush)brushConverter.ConvertFromString(input.Color); 
                    }
                }
            }
            catch (Exception ex)
            {
                NLogger.Default.Error("Set Value Error", ex);
                throw ex;
            }
        }

        public InputModel GetInputModel()
        {
            return _inputModel;
        }
    }
}
