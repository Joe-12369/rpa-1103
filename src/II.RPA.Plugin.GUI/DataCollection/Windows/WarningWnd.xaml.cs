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
using System.Windows.Shapes;

namespace II.RPA.Plugin.GUI.DataCollection.Windows
{
    public class MessageResult
    {
        /// <summary>
        /// 结果，Yes为true，No为false
        /// </summary>
        public bool IsYes { get; set; }
    }
    public class MessageBoxEventArgs : EventArgs
    {
        /// <summary>
        /// 结果，Yes为true，No为false
        /// </summary>
        public MessageResult Result { get; set; }
    }


    /// <summary>
    /// WarningWnd.xaml 的交互逻辑
    /// </summary>
    public partial class WarningWnd : Window
    {

        public WarningWnd()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
        }

        private void btn_Close_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        public event EventHandler<MessageBoxEventArgs> Result;

        public string Context
        {
            get { return tb_WarningContent.Text; }
            set { tb_WarningContent.Text = value; }
        }

        public string ConfirmContent
        {
            get { return tb_ConfirmContent.Content.ToString(); }
            set { tb_ConfirmContent.Content = value; }
        }

        bool _isLegal = false;

        public static void ShowWarningMessage(string strContent, EventHandler<MessageBoxEventArgs> _Result, string strConfirmButtonContent = "")
        {
            if (string.IsNullOrEmpty(strConfirmButtonContent))
            {
                strConfirmButtonContent = eCommonMessageType.CVScreenWnd_Designer_cs_QueDing.GetMessage();
            }

            var WarningWnd = new WarningWnd();
            WarningWnd.Context = strContent;
            WarningWnd.Result += _Result;
            WarningWnd.ConfirmContent = strConfirmButtonContent;
            WarningWnd.Show();
        }

        public static MessageResult ShowWarningMessageDialog(string strContent, string strConfirmButtonContent = "")
        {
            if (string.IsNullOrEmpty(strConfirmButtonContent))
            {
                strConfirmButtonContent = eCommonMessageType.CVScreenWnd_Designer_cs_QueDing.GetMessage();
            }
            var WarningWnd = new WarningWnd();
            WarningWnd.Context = strContent;
            WarningWnd.ConfirmContent = strConfirmButtonContent;

            MessageResult _MessageResult = null;
            WarningWnd.Result += (s, e) =>
            {
                _MessageResult = e.Result;
            };
            WarningWnd.ShowDialog();
            return _MessageResult;
        }
        public void CloseWindow()
        {
            btn_Cancel_Click(null, null);
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //e.Cancel = !_isLegal;
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            _isLegal = true;
            Close();
            Result?.Invoke(this, new MessageBoxEventArgs() { Result = new MessageResult() { IsYes = false } });
        }

        private void btn_Confirm_Click(object sender, RoutedEventArgs e)
        {
            _isLegal = true;
            Close();
            Result?.Invoke(this, new MessageBoxEventArgs() { Result = new MessageResult() { IsYes = true } });
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
