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

namespace RobotLiteControls
{
    /// <summary>
    /// BroswerControl.xaml 的交互逻辑
    /// </summary>
    public partial class BroswerControl : UserControl
    {
        string saveLocation;
        public BroswerControl(string localtion)
        {
            InitializeComponent();
            this.saveLocation = localtion;
            if (!string.IsNullOrEmpty(localtion))
            {
                TxtValue = localtion;
            }
        }
        public string TxtValue
        {
            get { return txtSelect.Text; }
            set { txtSelect.Text = value; }
        }

        private void btnBroswer_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (!string.IsNullOrEmpty(saveLocation))
            {
                folderBrowserDialog.SelectedPath = saveLocation;
            }
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtValue = folderBrowserDialog.SelectedPath;
            }
        }
    }
}
