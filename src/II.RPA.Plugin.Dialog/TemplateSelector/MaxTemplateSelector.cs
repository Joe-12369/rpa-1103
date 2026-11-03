using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace II.RPA.Plugin.Dialog.TemplateSelector
{
    /// <summary>
    /// 最大化与还原按钮状态模板选择器
    /// </summary>
    public class MaxTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MaxTemplate { get; set; }
        public DataTemplate RestoreTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            //Window win = Application.Current.MainWindow;
            //WindowState state = win.WindowState;
            //必须判断是否为空，否则设计器无法操作
            if (item == null)
                return null;
            WindowState state = (WindowState)item;
            switch (state)
            {
                case WindowState.Normal:
                    return MaxTemplate;
                case WindowState.Maximized:
                    return RestoreTemplate;
            }
            return null;
        }
    }
}
