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

namespace RobotLiteControls.UCControl
{
    /// <summary>
    /// SubUpload.xaml 的交互逻辑
    /// </summary>
    public partial class SubUpload : UserControl
    {
        public SubUpload()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //定义传递参数
            RoutedEventArgs args2 = new RoutedEventArgs(ControlLoadOverEvent, this);
            //引用自定义路由事件
            this.RaiseEvent(args2);
        }
        /// <summary>
        /// 声明路由事件
        /// </summary>
        public static readonly RoutedEvent ControlLoadOverEvent = EventManager.RegisterRoutedEvent("ControlLoadOverEvent", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventArgs<Object>), typeof(SubUpload));
        /// <summary>
        /// 处理各种路由事件的方法 
        /// </summary>
        public event RoutedEventHandler FileDelete
        {
            //将路由事件添加路由事件处理程序
            add { AddHandler(ControlLoadOverEvent, value); }
            //从路由事件处理程序中移除路由事件
            remove { RemoveHandler(ControlLoadOverEvent, value); }
        }

        public string GetFileName()
        {
            return FileName.Text;
        }

        public void SetFileName(string fileName)
        { 
            this.FileName.Text = fileName ;
        }
    }
}
