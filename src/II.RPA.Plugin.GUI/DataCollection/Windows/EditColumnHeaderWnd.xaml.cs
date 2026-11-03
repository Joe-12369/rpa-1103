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
    /// <summary>
    /// EditColumnHeaderWnd.xaml 的交互逻辑
    /// </summary>
    public partial class EditColumnHeaderWnd : Window
    {
        public EditColumnHeaderWnd()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
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

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void btn_Confirm_Click(object sender, RoutedEventArgs e)
        {
            ColumnHeaderInfo _ColumnHeaderInfo = new ColumnHeaderInfo(true, ColumnID,CurrentColumnIndex, tb_HeaderName.Text);
            ReturnResult(_ColumnHeaderInfo);
            this.Close();
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            //ColumnHeaderInfo _ColumnHeaderInfo = new ColumnHeaderInfo(false, tb_HeaderName.Text);
            //ReturnResult(_ColumnHeaderInfo);
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

        private string _ColumnID = string.Empty;
        public string ColumnID
        {
            get { return _ColumnID; }
            set { _ColumnID = value; }
        }

        //private string _OriginColumnName = string.Empty;
        //public string OriginColumnName
        //{
        //    get { return _OriginColumnName; }
        //    set { _OriginColumnName = value; }
        //}

        private string _ColumnInfo = string.Empty;
        public string ColumnInfo
        {
            get { return _ColumnInfo; }
            set { _ColumnInfo = value;}
        }
        #endregion

        /// <summary>
        /// 设置列标题
        /// </summary>
        /// <param name="strColumnID">列名头部 A B C</param>
        public void SetColumnInfo(ColumnHeaderInfo _ColumnHeaderInfo, Action<ColumnHeaderInfo> _action)
        {
            ColumnID = _ColumnHeaderInfo.ColumnID;
            CurrentColumnIndex = _ColumnHeaderInfo.ColumnIndex;
            this.tb_HeaderID.Text = ColumnID;
            this.tb_HeaderName.Text = string.IsNullOrEmpty(_ColumnHeaderInfo.CurrentName) ? string.Empty: _ColumnHeaderInfo.CurrentName;
            ReturnResult = _action;
        }

        /// <summary>
        /// 返回处理结果
        /// </summary>
        private Action<ColumnHeaderInfo> ReturnResult = null;
    }

    public class ColumnHeaderInfo
    {
        public bool IsChanged = false;
        public string ColumnID = string.Empty;
        public string CurrentName = string.Empty;
        public int ColumnIndex = 0;
        
        public ColumnHeaderInfo() { }
        public ColumnHeaderInfo(bool HasChanged, string strID, int _index, string strCurrentName)
        {
            IsChanged = HasChanged;
            ColumnID  = strID;
            CurrentName = strCurrentName;
            ColumnIndex = _index;
        }
    }

}
