using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace II.RPA.WebBrowser.Common.Utils
{
    public static class UtilsHelper
    {
        public static List<T> ConvertList<T>(List<T> elementAll, int startIndex, int endIndex, bool isAntitone)
        {
            if (isAntitone)
                elementAll.Reverse();
            if (!(startIndex == 0 || startIndex == 1) || endIndex != -1)
            {
                return elementAll.Slice(startIndex, endIndex);
            }
            return elementAll;
        }
        public static List<T> Slice<T>(this List<T> source, int start, int end)
        {
            int len = source.Count;
            start = start == 1 ? 0 : start;
            if (start > 1)
            {
                start--;
            }
            if (end > 0)
            {
                end--;
            }

            // 处理负索引
            if (start < 0)
            {
                start += len;
            }
            if (end < 0)
            {
                end += len;
            }

            if (end >= len)
            {
                end = len - 1;
            }

            // 检查起始和结束索引的有效性
            if (start < 0 || start > end)
            {
                return new List<T>();
            }

            // 计算切片的长度
            int length = end - start + 1;

            // 创建一个新列表来存储切片结果
            List<T> result = new List<T>(length);

            // 复制元素到新列表
            for (int i = start; i <= end; i++)
            {
                result.Add(source[i]);
            }

            return result;
        }
        public static string CompleteInformation(string url)
        {
            if (!string.IsNullOrEmpty(url))
                return ValidURL(url);
            return url;
        }

        internal static string ValidURL(string url)
        {
            string actualUrl = string.Empty;
            try
            {
                new Uri(url);
                actualUrl = url;
            }
            catch (UriFormatException)
            {
                if (new Regex("^[a-z,A-Z]:[\\/\\\\].*").IsMatch(url))
                {
                    actualUrl = url;
                }
                else
                {
                    actualUrl = "http://" + url;
                }
            }
            return actualUrl;
        }
    }
}
