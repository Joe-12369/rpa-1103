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

namespace DataCaptureJinyun.Windows
{
    /// <summary>
    /// EditRegularExpressWnd.xaml 的交互逻辑
    /// </summary>
    public partial class EditRegularExpressWnd : Window
    {
        public EditRegularExpressWnd()
        {
            InitializeComponent();
            II.RPA.WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void btn_Close_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            Border _Border = sender as Border;
            //_Border.BorderBrush = new SolidColorBrush(Colors.Red);
            _Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8900"));
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            Border _Border = sender as Border;
            _Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9DDE6"));
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_Confirm_Click(object sender, RoutedEventArgs e)
        {
            ColumnRegexInfo _ColumnHeaderInfo = new ColumnRegexInfo(true, CurrentColumnIndex, tb_RexContent.Text);
            ReturnResult(_ColumnHeaderInfo);
            this.Close();
        }

        #region 变量
        private int _ColumnIndex = 0;

        //当前操作的列序号
        public int CurrentColumnIndex
        {
            get { return _ColumnIndex; }
            set { _ColumnIndex = value; }
        }

        private string _ColumnRegex = string.Empty;
        public string ColumnRegex
        {
            get { return _ColumnRegex; }
            set { _ColumnRegex = value; }
        }
        #endregion

        /// <summary>
        /// 设置正则
        /// </summary>
        /// <param name="strColumnID">列名头部 A B C</param>
        public void SetColumnRegexConfig(ColumnRegexInfo _ColumnHeaderInfo, Action<ColumnRegexInfo> _action)
        {
            CurrentColumnIndex = _ColumnHeaderInfo.ColumnIndex;
            this.tb_RexContent.Text = string.IsNullOrEmpty(_ColumnHeaderInfo.CurrentRegex) ? string.Empty : _ColumnHeaderInfo.CurrentRegex;
            ReturnResult = _action;
        }

        /// <summary>
        /// 返回处理结果
        /// </summary>
        private Action<ColumnRegexInfo> ReturnResult = null;
    }

    public class ColumnRegexInfo
    {
        public bool IsChanged = false;
        public string CurrentRegex = string.Empty;
        public int ColumnIndex = 0;

        public ColumnRegexInfo() { }
        public ColumnRegexInfo(bool HasChanged, int index, string strCurrentRegex)
        {
            IsChanged = HasChanged;
            CurrentRegex = strCurrentRegex;
            ColumnIndex = index;
        }
    }
}
