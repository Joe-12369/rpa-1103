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
    /// UCLabel.xaml 的交互逻辑
    /// </summary>
    public partial class UCLabel : UserControl, IGetInputModelControl
    {
        private InputModel _inputModel;
        private static ILogger logger = Logger.OfContext();

        public UCLabel()
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
                    if (!string.IsNullOrEmpty(input.Value))
                    {
                        this.LAB.Text = input.Value;
                    }
                    this.FontWeight = GetFontWeight(input.FontWeight);
                    this.FontFamily = new FontFamily(input.FontFamily);
                    this.LAB.FontSize = input.FontSize;
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

        private FontWeight GetFontWeight(string fontWeight)
        {
            if (fontWeight == "lighter")
            {
                return FontWeights.Light;
            }
            else if (fontWeight == "bold")
            {
                return FontWeights.Bold;
            }
            else if (fontWeight == "Black")
            {
                return FontWeights.Black;
            }
            else
            {
                return FontWeights.Normal;
            }
        }
    }
}

