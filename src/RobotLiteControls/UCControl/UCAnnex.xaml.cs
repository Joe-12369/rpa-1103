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
    /// UCAnnex.xaml 的交互逻辑
    /// </summary>
    public partial class UCAnnex : UserControl, IGetInputModelControl
    {
        private InputModel _inputModel;
        private readonly string _annexPath;
        public UCAnnex(string packagePtah)
        {
            _annexPath = System.IO.Path.Combine(packagePtah, "annexFile");
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
                    if (input.ValueArray != null)
                    {
                        foreach (string option in input.ValueArray)
                        {
                            SubAnnex sub = new SubAnnex(_annexPath, option);
                            this.SP.Children.Add(sub);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLogger.Default.Error("Annex Set Value Error", ex);
                throw ex;
            }
        }

        public InputModel GetInputModel()
        {
            return _inputModel;
        }
    }
}
