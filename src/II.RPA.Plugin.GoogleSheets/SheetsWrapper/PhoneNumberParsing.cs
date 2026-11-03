using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GoogleSheets.SheetsWrapper
{
    public class PhoneNumberParsing
    {
        public static string RemoveUSInterationalPhoneCode(string number)
        {
            return RemoveExtraCharactersFromPhoneNumber(number.Replace("+1", ""));
        }

        public static string RemoveExtraCharactersFromPhoneNumber(string number)
        {
            return Regex.Replace(number, @"[^\d]", "");
        }

        public static long ConvertToLong(string number)
        {
            var numberAsString = RemoveExtraCharactersFromPhoneNumber(number.Replace("+1", ""));

            return long.Parse(numberAsString);
        }
    }
}
