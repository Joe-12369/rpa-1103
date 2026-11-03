using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace II.RPA.Plugin.GUI.ProcessCreator.ActionProvider
{
    public class TextToVisiblityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values!=null && values.Length==2)
            {
                if (values[0] is string text && values[1] is Visibility visibility)
                {
                    if (string.IsNullOrEmpty(text) && visibility == Visibility.Visible)
                    {
                        return Visibility.Visible;
                    }
                }
            }
            return Visibility.Collapsed;
          
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
