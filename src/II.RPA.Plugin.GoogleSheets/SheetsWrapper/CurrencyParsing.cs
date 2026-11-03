using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets.SheetsWrapper
{
    public class CurrencyParsing
    {
        public static double ParseCurrencyString(string amount)
        {
            return double.Parse(
                amount,
                NumberStyles.Currency | NumberStyles.AllowDecimalPoint, CultureInfo.CreateSpecificCulture("en-us"));
        }

        public static string ConvertToCurrencyString(double amount)
        {
            return $"{amount:C2}";
        }
    }
}
