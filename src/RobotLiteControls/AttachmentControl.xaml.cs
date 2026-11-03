using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RobotLiteControls
{
    /// <summary>
    /// AttachmentControl.xaml 的交互逻辑
    /// </summary>
    public partial class AttachmentControl : System.Windows.Controls.UserControl
    {
        private string attachment;
        public string Attachment {
            get { return attachment; }
            set
            {
                attachment = value;
                lblAttachName.Content = System.IO.Path.GetFileName(value);
            }
        }
        public AttachmentControl()
        {
            InitializeComponent();
            if(!string.IsNullOrEmpty(Attachment))
            {
                lblAttachName.Content = System.IO.Path.GetFileName(Attachment);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            lblAttachName.Content = null;
            Attachment = null;
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog browserDialog = new OpenFileDialog();
            browserDialog.Multiselect = false;
            if(Attachment!=null)
            {
                if (System.IO.File.Exists(Attachment))
                {
                    browserDialog.InitialDirectory = System.IO.Path.GetDirectoryName(Attachment);
                }
            }
            if (browserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Attachment = browserDialog.FileName;
            }
        }
    }
}
