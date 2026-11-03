using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace II.RPA.Plugin.GUI.DataCollection.DataTypeConverter
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    class Bool2StateColorConverterOrigin : IValueConverter
    {
        #region IValueConverter Members
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((Brush)value == (Brush)(new BrushConverter().ConvertFrom("#00B73E")))
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
                    return (Brush)(new BrushConverter().ConvertFrom("#00B73E"));
                }
                else
                {
                    return (Brush)(new BrushConverter().ConvertFrom("#B40F10"));
                }
            }
            catch (Exception)
            {
                return (Brush)(new BrushConverter().ConvertFrom("#B40F10"));
            }
        }
        #endregion
    }

    [ValueConversion(typeof(bool), typeof(Brush))]
    class Bool2StateColorConverter : IValueConverter
    {
        #region IValueConverter Members
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((Brush)value == (Brush)(new BrushConverter().ConvertFrom("#D9DDE6")))
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
                    return (Brush)(new BrushConverter().ConvertFrom("#D9DDE6"));
                }
                else
                {
                    return (Brush)(new BrushConverter().ConvertFrom("#505962"));
                }
            }
            catch (Exception)
            {
                return (Brush)(new BrushConverter().ConvertFrom("#505962"));
            }
        }
        #endregion
    }
}
