using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace II.RPA.Plugin.GUI.DataCollection.DataTypeConverter
{
    /// <summary>
    /// 文字显示不下通过ToolTip提示显示
    /// </summary>
    public class TrimmedTextBlockVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null)
                {
                    return Visibility.Visible;
                }

                TextBlock textBlock = (TextBlock)value;
                if (string.IsNullOrWhiteSpace(textBlock.Text))
                {
                    return Visibility.Collapsed;
                }

                bool isTrim = IsTextTrimmed(textBlock);

                if (isTrim)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
            catch
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 判断当前显示的内容是否显示不全被截断
        /// </summary>
        /// <param name="textBlock"></param>
        /// <returns></returns>
        private bool IsTextTrimmed(TextBlock textBlock)
        {
            Typeface typeface = new Typeface(
                textBlock.FontFamily,
                textBlock.FontStyle,
                textBlock.FontWeight,
                textBlock.FontStretch);

            FormattedText formattedText = new FormattedText(
                textBlock.Text,
                System.Threading.Thread.CurrentThread.CurrentCulture,
                textBlock.FlowDirection,
                typeface,
                textBlock.FontSize,
                textBlock.Foreground);
            bool isTrimmed = formattedText.Width > textBlock.ActualWidth;
            return isTrimmed;
        }
    }
}
