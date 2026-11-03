using System;
using System.Globalization;
using System.Linq;
using IPA.Ubases.Extensions;

namespace II.RPA.Observable
{
    public static class StringConvertExtension
    {
        public static object Number(this string str, Type numberType)
        {
            if (numberType.EqualsWhenNotNullable<sbyte>())
            {
                sbyte value;

                if (sbyte.TryParse(str, out value))
                {
                    return value;
                }

                return null;
            }

            if (numberType.EqualsWhenNotNullable<short>())
            {
                short value;

                if (short.TryParse(str, out value))
                {
                    return value;
                }

                return null;
            }

            if (numberType.EqualsWhenNotNullable<int>())
            {
                int value;

                if (int.TryParse(str, out value))
                {
                    return value;
                }

                return null;
            }

            if (numberType.EqualsWhenNotNullable<long>())
            {
                long value;

                if (long.TryParse(str, out value))
                {
                    return value;
                }

                return null;
            }

            if (numberType.EqualsWhenNotNullable<byte>())
            {
                byte value;

                if (byte.TryParse(str, out value))
                {
                    return value;
                }

                return null;
            }

            if (numberType.EqualsWhenNotNullable<ushort>())
            {
                ushort value;

                if (ushort.TryParse(str, out value))
                {
                    return value;
                }

                return null;
            }

            if (numberType.EqualsWhenNotNullable<uint>())
            {
                uint value;

                if (uint.TryParse(str, out value))
                {
                    return value;
                }

                return null;
            }

            if (numberType.EqualsWhenNotNullable<ulong>())
            {
                ulong value;

                if (ulong.TryParse(str, out value))
                {
                    return value;
                }

                return null;
            }

            if (numberType.EqualsWhenNotNullable<float>())
            {
                float value;

                if (float.TryParse(str, out value))
                {
                    return value;
                }

                return null;
            }

            if (numberType.EqualsWhenNotNullable<double>())
            {
                double value;

                if (double.TryParse(str, out value))
                {
                    return value;
                }

                return null;
            }

            if (numberType.EqualsWhenNotNullable<decimal>())
            {
                decimal value;

                if (decimal.TryParse(str, out value))
                {
                    return value;
                }

                return null;
            }

            return null;
        }

        public static T? Number<T>(this string str)
            where T : struct
        {
            return str.Number(typeof(T)) as T?;
        }

        public static object Enum(this string str, Type enumType)
        {
            try
            {
                return System.Enum.Parse(enumType, str);
            }
            catch
            {
                return null;
            }
        }

        public static T? Enum<T>(this string str)
            where T : struct
        {
            T result;

            if (System.Enum.TryParse(str, true, out result))
            {
                var values = System.Enum.GetValues(typeof(T));

                if (!values.Cast<object>().Contains(result))
                {
                    return null;
                }

                return result;
            }

            return null;
        }

        public static bool? Bool(this string str)
        {
            var lowerStr = str.ToLower();

            if (lowerStr.Number<int>() == 1)
            {
                return true;
            }

            if (lowerStr.Number<int>() == 0)
            {
                return false;
            }

            if (lowerStr?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            if (lowerStr?.Equals("false", StringComparison.OrdinalIgnoreCase) == true)
            {
                return false;
            }

            return null;
        }

        public static char? Char(this string str)
        {
            if (str?.Length == 1)
            {
                return str[0];
            }

            return null;
        }

        public static Guid? Guid(this string str)
        {
            Guid guid;
            return System.Guid.TryParse(str, out guid)
                ? guid
                : default(Guid?);
        }

        public static DateTime? DateTime(this string str, string format = null)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return default;
            }

            DateTime date;

            if (format.IsNullOrEmpty())
            {
                return System.DateTime.TryParse(str, out date) ? date : default(DateTime?);
            }

            return System.DateTime.TryParseExact(
                str,
                format,
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out date)
                ? date
                : default(DateTime?);
        }

        public static object Convert(this string str, Type targetType)
        {
            if (targetType.Equals<object>())
            {
                return str;
            }

            if (targetType.Equals<string>())
            {
                return str;
            }

            if (targetType.EqualsWhenNotNullable<char>())
            {
                return str.Char();
            }

            if (targetType.EqualsWhenNotNullable<bool>())
            {
                return str.Bool();
            }

            if (targetType.IsEnum ||
                (targetType.IsNullable() && targetType.WhenNotNullable().IsEnum))
            {
                return str.Enum(targetType);
            }

            if (targetType.EqualsWhenNotNullable<DateTime>())
            {
                return str.DateTime();
            }

            if (targetType.EqualsWhenNotNullable<Guid>())
            {
                return str.Guid();
            }

            return str.Number(targetType);
        }
    }
}
