using RobotLiteControls.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace RobotLiteControls
{
    /// <summary>
    /// WatermarkTextBox.xaml 的交互逻辑
    /// </summary>
    public partial class WatermarkTextBox : UserControl
    {
       public WatermarkViewModel wtData { get; set; }
        public WatermarkTextBox()
        {
            InitializeComponent();
            wtData = new WatermarkViewModel();
            this.DataContext = wtData;
        }
        public string Watermarker
        {
            get { return wtData.WatermarkText; }
            set { wtData.WatermarkText = value; }
        }
    }
}
