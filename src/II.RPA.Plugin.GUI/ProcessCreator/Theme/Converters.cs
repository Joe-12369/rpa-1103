using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace II.RPA.Plugin.GUI.ProcessCreator.Theme
{
    public static class Converters
    {
        public static ItemTextConverter ItemTextConverter => new ItemTextConverter();
        public static PassWordConverter PassWordConverter => new PassWordConverter();
        public static CommandParamConverter CommandParamConverter => new CommandParamConverter();
    }
    public class ItemTextConverter : IMultiValueConverter
    {
        private static readonly ILogger logger = Logger.OfContext();
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 4)
            {
                logger.LogDebug($"Convert values:{values[0]};{values[1]};{values[2]};{values[3]};{values[4]}");
            }
            //if (values[0] != null && !string.IsNullOrWhiteSpace(values[0].ToString()))
            //{
            //    value = values[0].ToString();
            //}
            //else if (values[1] != null && !string.IsNullOrWhiteSpace(values[1].ToString()))
            //{
            //    value = values[1].ToString(); ;
            //}
            return new object[] { values[0], values[1], values[2], values[3], values[4] };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class PassWordConverter : IValueConverter
    {
        private string realWord = "";
        private char replaceChar = '*';

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null)
            {
                string temp = parameter.ToString();
                if (!string.IsNullOrEmpty(temp))
                {
                    replaceChar = temp.First();
                }
            }

            if (value != null)
            {
                realWord = value.ToString();
            }


            string replaceWord = "";
            for (int index = 0; index < realWord.Length; ++index)
            {
                replaceWord += replaceChar;
            }

            return replaceWord;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string backValue = "";
            if (value != null)
            {
                string strValue = value.ToString();
                for (int index = 0; index < strValue.Length; ++index)
                {
                    if (strValue.ElementAt(index) == replaceChar)
                    {
                        backValue += realWord.ElementAt(index);
                    }
                    else
                    {
                        backValue += strValue.ElementAt(index);
                    }
                }
            }
            return backValue;
        }

    }
    public class CommandParamConverter : IMultiValueConverter
    {
        private static readonly ILogger logger = Logger.OfContext();
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            object[] temp = new object[values.Length];
            if (values != null && values.Any())
            {
                for (int i=0;i< values.Length;i++)
                {
                    logger.LogDebug($"CommandParamConverter:{values[i]}");
                    temp[i] = values[i];
                }
                return temp;
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
