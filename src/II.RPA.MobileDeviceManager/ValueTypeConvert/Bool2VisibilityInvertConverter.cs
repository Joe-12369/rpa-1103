using System;
using System.Windows;
using System.Windows.Data;

namespace II.RPA.MobileDeviceManager.ValueTypeConvert
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    class Bool2VisibilityInvertConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((Visibility)value == Visibility.Visible)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return true;
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((bool)value == true)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
            catch (Exception)
            {
                return Visibility.Visible;
            }
        }
    }
}
