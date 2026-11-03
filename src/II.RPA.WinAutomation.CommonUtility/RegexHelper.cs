
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace II.RPA.WinAutomation.CommonUtility
{
    /// <summary>
    /// 操作正则表达式的公共类
    /// </summary>    
    public static class RegexHelper
    {
        static string IP_Regex = @"(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])";
        #region 验证输入字符串是否与模式字符串匹配
        /// <summary>
        /// 验证输入字符串是否与模式字符串匹配，匹配返回true
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="pattern">模式字符串</param>        
        public static bool IsMatch(string input, string pattern)
        {
            return IsMatch(input, pattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 验证输入字符串是否与模式字符串匹配，匹配返回true
        /// </summary>
        /// <param name="input">输入的字符串</param>
        /// <param name="pattern">模式字符串</param>
        /// <param name="options">筛选条件</param>
        public static bool IsMatch(string input, string pattern, RegexOptions options)
        {
            return Regex.IsMatch(input, pattern, options);
        }
        /// <summary>
        /// 验证输入的是否为合法的IP地址
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsMatchIPV4(string input)
        {
            return IsMatch(input, IP_Regex);
        }
        public static bool IsMathNotEmptyInput(string input)
        {
            return !(string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input));
        }

        /// <summary>
        /// 过滤文字集合 使用正则修改输出
        /// </summary>
        /// <param name="_Valuelist"></param>
        /// <param name="_RegexValue"></param>
        /// <returns></returns>
        public static List<string> GenerateValuesByRegex(List<string> _OriginValuelist,string _RegexValue)
        {
            if (string.IsNullOrWhiteSpace(_RegexValue))
            {
                return _OriginValuelist;
            }

            List<string> ValueList = new List<string>();
            Regex _RegexObject = new Regex(_RegexValue);

            foreach (var _item in _OriginValuelist)
            {
                string RegexItem = _RegexObject.Match(_item).Value;
                ValueList.Add(RegexItem);
            }
            return ValueList;
        }
        #endregion
    }
}
