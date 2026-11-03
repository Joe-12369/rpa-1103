using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace II.RPA.Plugin.GUI.UIProviders.Controls
{
    public class IconControl:ContentControl
    {

        public IconType IconType
        {
            get { return (IconType)GetValue(IconTypeProperty); }
            set { SetValue(IconTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconTypeProperty =
            DependencyProperty.Register("IconType", typeof(IconType), typeof(IconControl), new PropertyMetadata(IconType.DEFAULT));


    }
    public enum IconType
    {
        DEFAULT,
        PROCEED,
        DONE,
        ERROR
    }
}
