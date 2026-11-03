using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace II.Framework.UI
{
    public class BoolToVisibiliyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool flag) 
            {
                if (parameter is string paramFlag)
                {
                    if (paramFlag == bool.TrueString)
                    {
                        return flag ? Visibility.Visible : Visibility.Collapsed;
                    }
                    else
                    {
                        return flag ? Visibility.Collapsed : Visibility.Visible;
                    }
                }
                else
                {
                    return flag ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolsToVisibiliyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Any())
            {
                var bools = values.Cast<bool>();
                if (parameter is string paramFlag)
                {
                    if (paramFlag == bool.TrueString)
                    {
                        return bools.All(x => x) ? Visibility.Visible : Visibility.Collapsed;
                    }
                    else
                    {
                        return bools.All(x => !x) ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
                else
                {
                    return bools.All(x => !x) ? Visibility.Visible : Visibility.Collapsed;
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
