using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.Plugin.Dialog.Helpers
{
    public static class MenuItemHelper
    {
        public static string GetLastName(DependencyObject obj)
        {
            return (string)obj.GetValue(LastNameProperty);
        }

        public static void SetLastName(DependencyObject obj, string value)
        {
            obj.SetValue(LastNameProperty, value);
        }

        // Using a DependencyProperty as the backing store for LastName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LastNameProperty =
            DependencyProperty.RegisterAttached("LastName", typeof(string), typeof(MenuItemHelper), new PropertyMetadata(""));


    }
}
