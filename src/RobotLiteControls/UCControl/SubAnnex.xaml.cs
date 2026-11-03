using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// SubAnnex.xaml 的交互逻辑
    /// </summary>
    public partial class SubAnnex : UserControl
    {
        private string _fileName;
        private string _path;
        public SubAnnex(string path,string fileName)
        {
            NLogger.Default.Trace($"path：{path},fileName：{fileName}");

            _path = System.IO.Path.Combine(path, fileName);
            if (!File.Exists(_path))
            {
                throw new Exception("文件不存在");
            }
            _fileName = fileName;
            InitializeComponent();
            FileName.Content = _fileName;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //SaveFileDialog
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "另存为";
            dlg.FileName = _fileName;
            if (dlg.ShowDialog() == true)
            {
                byte[] buffer;
                if (File.Exists(_path)) //文件的全路径字符串
                {
                    FileStream fs = new FileStream(_path, FileMode.Open);
                    buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, int.Parse(fs.Length.ToString()));
                    fs.Close();
                    fs.Dispose();
                    fs = new FileStream(dlg.FileName, FileMode.Create);//fileName是要保存到的文件路径
                    fs.Write(buffer, 0, buffer.Length);
                    fs.Dispose();
                }
            }
           
        }
    }
}
