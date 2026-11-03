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
using Wpf.Ui.Controls;

namespace II.RPA.MobileDeviceManager.Controls
{
    /// <summary>
    /// Interaction logic for ModifyDialog.xaml
    /// </summary>
    public partial class ModifyDialog : ContentDialog
    {
        public ModifyDialog(ContentPresenter contentPresenter)
        : base(contentPresenter)
        {
            InitializeComponent();
        }
    }
}
