using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace II.RPA.PatchUpdate.UserControl
{
    /// <summary>
    /// XMessageBox.xaml 的交互逻辑
    /// </summary>
    public partial class XMessageBox : BaseWindow
    {
        private bool Result = false;
        public XMessageBox(EnumNotifyType type, string msg)
        {
            this.AContent = msg;
            switch (type)
            {
                case EnumNotifyType.Error:
                    this.Title = "错误";
                    //this.AIcon = (Brush)FindResource("MsgError_Brush");
                    break;
                case EnumNotifyType.Warning:
                    this.Title = "警告";
                    //this.AIcon = (Brush)FindResource("MsgWarnning_Brush");
                    break;
                case EnumNotifyType.Info:
                    this.Title = "通知";
                    //this.AIcon = (Brush)FindResource("MsgInfo_Brush");
                    break;
                case EnumNotifyType.Question:
                    this.IsShowCancle = true;
                    this.Title = "提示";
                    //this.AIcon = (Brush)FindResource("MsgQuestion_Brush");
                    break;
                default:
                    break;
            }
            InitializeComponent();
            this.MouseLeftButtonDown += delegate {
                DragMove();
            };
            this.KeyDown += XMessageBox_KeyDown;
        }
        /// <summary>
        /// 确定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            this.Result = true;
            this.Close();
            e.Handled = true;
        }

        /// <summary>
        /// 取消
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Result = false;
            this.Close();
            e.Handled = true;
        }



        /// <summary>
        /// 显示提示消息框，
        /// owner指定所属父窗体，默认参数值为null，则指定主窗体为父窗体。
        /// </summary>
        private static bool Show(EnumNotifyType type, string mes, Window owner = null)
        {
            var res = true;
            Application.Current.Dispatcher.Invoke(new Action(() => {
                XMessageBox nb = new XMessageBox(type, mes);
                nb.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                nb.Owner = owner ?? UIHelper.GetTopWindow();
                nb.ShowInTaskbar = false;
                nb.ShowDialog();
                res = nb.Result;
            }));
            return res;
        }

        public static void Info(string msg, Window owner = null)
        {
            Show(EnumNotifyType.Info, msg, owner);
        }

        public static void Warnning(string msg, Window owner = null)
        {
            Show(EnumNotifyType.Warning, msg, owner);
        }

        public static void Error(string msg, Window owner = null)
        {
            Show(EnumNotifyType.Error, msg, owner);
        }

        public static bool Question(string msg, Window owner = null)
        {
            return Show(EnumNotifyType.Question, msg, owner);
        }


        /// <summary>
        /// 弹窗的退出快捷键和Esc确认快捷键Enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XMessageBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Result = false;
                this.Close();
            }
            if (e.Key == Key.Enter)
            {
                Result = true;
                this.Close();
            }
        }
    }
    public enum EnumNotifyType
    {
        [Description("错误")]
        Error,
        [Description("警告")]
        Warning,
        [Description("提示")]
        Info,
        [Description("确认")]
        Question,
    }

    public static class UIHelper
    {
        //GetForegroundWindow API
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// 获取当前顶级窗体，若获取失败则返回主窗体
        /// </summary>
        public static Window GetTopWindow()
        {
            var hwnd = GetForegroundWindow();
            Window win = GetWindowFromHwnd(hwnd);
            if (hwnd == IntPtr.Zero || win == null)
            {
                return Application.Current.MainWindow;
            }
            return win;
        }

        private static Window GetWindowFromHwnd(IntPtr hwnd)
        {
            HwndSource sd = HwndSource.FromHwnd(hwnd);
            if (sd != null)
            {
                return (Window)sd.RootVisual;
            }
            return null;
        }
    }
}
