using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.MobileDeviceManager
{
    public class ContentDialogExtension
    {
        public static bool? GetDialogResult(DependencyObject obj)
        {
            return (bool?)obj.GetValue(DialogResultProperty);
        }

        public static void SetDialogResult(DependencyObject obj, bool? value)
        {
            obj.SetValue(DialogResultProperty, value);
        }

        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached("DialogResult", typeof(bool?), typeof(ContentDialogExtension), new PropertyMetadata(null, (d, e) =>
            {
                if (d is Window self)
                {
                    self.DialogResult = e.NewValue as bool?;
                }
            }));
    }
}
