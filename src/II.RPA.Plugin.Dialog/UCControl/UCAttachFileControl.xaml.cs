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

namespace II.RPA.Plugin.Dialog.UCControl
{
    /// <summary>
    /// Interaction logic for UCAttachFileControl.xaml
    /// </summary>
    public partial class UCAttachFileControl : UserControl
    {
        public UCAttachFileControl(string aFileName, 
            FileType aFileType)
        {
            InitializeComponent();
            PART_FileName.Text = aFileName;
            switch (aFileType) 
            {
                case FileType.Local:
                    PART_TypeBorder.Background = new SolidColorBrush(Color.FromArgb(30,0,133,255));
                    PART_TypeTextblock.Foreground = new SolidColorBrush(Color.FromRgb(0, 133, 255));
                    PART_Button_Replace.Content = FindResource("Icon.Folder");
                    PART_TypeTextblock.Text = "本地";
                    break;
                case FileType.Cloud:
                    PART_TypeBorder.Background = new SolidColorBrush(Color.FromArgb(30, 255, 137, 16));
                    PART_TypeTextblock.Foreground = new SolidColorBrush(Color.FromRgb(255, 137, 16));
                    PART_Button_Replace.Content = FindResource("Icon.Cloud");
                    PART_TypeTextblock.Text = "云端";
                    break;
                default:
                    PART_TypeBorder.Background = new SolidColorBrush(Color.FromArgb(30, 0, 133, 255));
                    PART_TypeTextblock.Foreground = new SolidColorBrush(Color.FromRgb(0, 133, 255));
                    break;
            }
        }
    }
}
