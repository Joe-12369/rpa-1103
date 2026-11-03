using II.RPA.Logging;
using II.RPA.Plugin.GUI.Commons;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.Utils
{
    public class DataCollectRegexHelper
    {
        private static ILogger logger = Logger.OfContext();
        /// <summary>
        /// 正则提取字符串中内容
        /// </summary>
        /// <param name="_strOriginValue"></param>
        /// <param name="_RegexExpress"></param>
        /// <returns></returns>
        public static List<string> GetMatchValuesWithRegex(string _strOriginValue, string _RegexExpress)
        {
            if (string.IsNullOrEmpty(_RegexExpress))
            {
                return new List<string> { _strOriginValue };
            }

            List<string> _ValueList = new List<string>();
            var reg = new Regex(_RegexExpress, RegexOptions.Multiline);
            var mc = reg.Matches(_strOriginValue);
            foreach (Match m in mc)
            {
                var val = m.Groups[0].Value;
                _ValueList.Add(val);
            }
            return _ValueList;
        }

        /// <summary>
        /// 拼接正则提取后的字符串集合
        /// </summary>
        /// <param name="_strOriginValue"></param>
        /// <param name="_RegexExpress"></param>
        /// <param name="_Separator"></param>
        /// <returns></returns>
        public static string GenerateStringWithRegex(string _strOriginValue, string _RegexExpress, string _Separator = " ")
        {
            if (string.IsNullOrEmpty(_RegexExpress) || string.IsNullOrEmpty(_strOriginValue))
            {
                return _strOriginValue;
            }

            string strResult = string.Empty;
            List<string> _ValueList = GetMatchValuesWithRegex(_strOriginValue, _RegexExpress);
            strResult = string.Join(_Separator, _ValueList);
            return strResult;
        }
        #region 数据类型判断 相关

        public static eFeildType GetFeildType(List<string> listStr)
        {
            try
            {
                List<eFeildType> feildTypeList = new List<eFeildType>();
                foreach (var str in listStr)
                {
                    if (string.IsNullOrEmpty(str))
                    {
                        continue;
                    }
                    feildTypeList.Add(GetFeildType(str));
                }
                feildTypeList = feildTypeList.Distinct().ToList();
                if (feildTypeList.Count == 0)
                {
                    return eFeildType.String;
                }
                else if (feildTypeList.Count == 1)
                {
                    return feildTypeList.First();
                }
                else if (feildTypeList.Count == 2 && new[] { eFeildType.Integer, eFeildType.Decimal }.All(t => feildTypeList.IndexOf(t) >= 0))
                {
                    return eFeildType.Decimal;
                }
                else
                {
                    return eFeildType.String;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"根据列数据 获取数据类型发生了异常");
                return eFeildType.String;
            }
        }
        public static eFeildType GetFeildType(string str)
        {
            if (str.EqualIgnoreCase("true") || str.EqualIgnoreCase("false"))
            {
                return eFeildType.Boolean;
            }
            else if (IsInt(str))
            {
                return eFeildType.Integer;
            }
            else if (IsDouble(str))
            {
                return eFeildType.Decimal;
            }
            else if (IsDate(str) || IsDateTime(str) || IsTime(str))
            {
                return eFeildType.Date;
            }
            else
            {
                return eFeildType.String;
            }
        }
        private static string ReInt = @"^[+-]?(?:\d+)";
        private static string ReUInt = @"^[+]?(?:\d+)";
        private static string ReDouble = @"^[+-]?(?:\d+(\.\d*)?|\d*\.\d+?)";
        private static string ReDate = @"^(?:(?!0000)[0-9]{4}([-\.]?)(?:(?:0?[1-9]|1[0-2])\1(?:0?[1-9]|1[0-9]|2[0-8])|(?:0?[13-9]|1[0-2])\1(?:29|30)|(?:0?[13578]|1[02])\1(?:31))|(?:[0-9]{2}(?:0[48]|[2468][048]|[13579][26])|(?:0[48]|[2468][048]|[13579][26])00)([-\.]?)0?2\2(?:29))";
        private static string ReTime = @"^([0-9]|[0-1][0-9]|[2][0-3]):([0-9]|[0-5][0-9])(:([0-9]|[0-5][0-9]))?";
        private static string EndFlag = @"\s*$";

        public static bool RegexMatch(string input, string pattern)
        {
            if (input == null || input.Trim().Length == 0)
                return false;
            return System.Text.RegularExpressions.Regex.IsMatch(input, pattern);
        }

        public static bool IsDouble(string str)
        {
            return RegexMatch(str, ReDouble + EndFlag);
        }

        public static bool IsInt(string str)
        {
            return RegexMatch(str, ReInt + EndFlag);
        }

        public static bool IsUInt(string str)
        {
            return RegexMatch(str, ReUInt + EndFlag);
        }

        /// <summary>
        /// 格式为yyyy-MM-dd HH:mm:ss 或yyyy-M-d H:m:s
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsDateTime(string str)
        {
            return RegexMatch(str, ReDate + @"\s+" + ReTime.Replace("^", "") + EndFlag);
        }

        /// <summary>
        /// 格式yyyy-MM-dd或yyyy-M-d，连字符可以没有或是“-”、“/”、“.”之一
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsDate(string str)
        {
            return RegexMatch(str, ReDate + EndFlag);
        }

        /// <summary>
        /// 时间格式为HH:mm:ss 或H:m:s或HH:mm或H:m
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsTime(string str)
        {
            return RegexMatch(str, ReTime + EndFlag);
        }

        #endregion
    }

}

