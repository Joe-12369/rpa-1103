using System;
using System.Windows;
using System.Windows.Data;

namespace II.RPA.MobileDeviceManager.ValueTypeConvert
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    class Bool2VisibilityConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((Visibility)value == Visibility.Visible)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((bool)value == true)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }
    }
}
