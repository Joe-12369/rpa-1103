using System;
using System.Windows.Data;

namespace II.RPA.Plugin.GUI.ProcessCreator.Controls
{
    class ProgressThicknessConverter : IValueConverter
    {
        /// <summary>
        /// Checks if the <see cref="Common.SymbolRegular"/> is valid and not empty.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: It's too hardcoded, we should define better formula.

            if (value is double height)
                return height / 8;

            return 12.0d;
        }

        /// <summary>
        /// Not Implemented.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
