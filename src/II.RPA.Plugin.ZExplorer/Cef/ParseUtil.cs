using System;
using System.Drawing;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    internal static class ParseUtil
    {
        public static Rectangle? ParseRectangle(string str)
        {
            string[] array = str.Split(new char[]
            {
                ';'
            });
            if (array.Length != 4)
            {
                return null;
            }
            int? num = TryParseIntIgnoreDecimals(array[0]);
            int? num2 = TryParseIntIgnoreDecimals(array[1]);
            int? num3 = TryParseIntIgnoreDecimals(array[2]);
            int? num4 = TryParseIntIgnoreDecimals(array[3]);
            if (num == null || num2 == null || num3 == null || num4 == null)
            {
                return null;
            }
            return new Rectangle?(new Rectangle(num.Value, num2.Value, num3.Value, num4.Value));
        }

        public static Point? ParsePosition(string str)
        {
            string[] array = str.Split(new char[]
            {
                ';'
            });
            if (array.Length != 2)
            {
                return null;
            }
            int? num = TryParseIntIgnoreDecimals(array[0]);
            int? num2 = TryParseIntIgnoreDecimals(array[1]);
            if (num == null || num2 == null)
            {
                return null;
            }
            return new Point?(new Point(num.Value, num2.Value));
        }

        public static float? TryParseFloat(string str)
        {
            float value;
            if (!float.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            {
                return null;
            }
            return new float?(value);
        }

        public static int? TryParseIntIgnoreDecimals(string str)
        {
            float? num = ParseUtil.TryParseFloat(str);
            if (num == null)
            {
                return null;
            }
            if (num.Value >= -2.1474836E+09f && num.Value <= 2.1474836E+09f)
            {
                return new int?((int)num.Value);
            }
            return null;
        }

        internal static Rectangle? ParseRectangle(CefResult<JToken> result)
        {
            if (result.IsError || result.Success == null)
            {
                return null;
            }
            return ParseUtil.ParseRectangle(result.Success.Value<string>("value"));
        }
    }
}
