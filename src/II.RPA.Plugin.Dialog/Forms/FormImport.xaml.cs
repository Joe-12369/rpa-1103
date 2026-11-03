using II.RPA.WinAutomation.CommonUtility.XamlResource;
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
using System.Windows.Shapes;

namespace II.RPA.Plugin.Dialog.Forms
{
    /// <summary>
    /// FormImport.xaml 的交互逻辑
    /// </summary>
    public partial class FormImport : Window
    {
        public FormImport()
        {
            InitializeComponent();
            LanguageResourceHelper.GetInstance().SetAppInstance(this);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        public bool GetSheetName(out string sheetName, IEnumerable<string> sheetNames)
        {
            ComB.ItemsSource = sheetNames;
            ComB.SelectedIndex = 0;
            bool? ret = this.ShowDialog();
            if (ret == true)
            {
                sheetName = ComB.SelectedItem.ToString();
                return true;
            }
            else
            {
                sheetName = "";
                return false;
            }
        }
    }
}
