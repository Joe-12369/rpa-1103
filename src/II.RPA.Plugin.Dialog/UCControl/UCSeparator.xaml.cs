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
    /// UCSeparator.xaml 的交互逻辑
    /// </summary>
    public partial class UCSeparator : UserControl, IGetInputModelControl
    {
        private InputModel _inputModel;
        private static ILogger logger = Logger.OfContext();

        public UCSeparator()
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Set Value Error");
                throw;
            }
        }

        public InputModel GetInputModel()
        {
            return _inputModel;
        }
    }
}

