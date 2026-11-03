using System.Windows;

namespace II.Framework.UI
{
    public class BaseFluentWindow : Window
    {
        public BaseFluentWindow()
        {
            ///无外观的控件需要在静态构造函数中设置DefaultStyleKeyProperty,这样它会去在themes/generic.xaml的文件获取默认的样式。
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BaseFluentWindow), new FrameworkPropertyMetadata(typeof(BaseFluentWindow)));
        }
    }
}
