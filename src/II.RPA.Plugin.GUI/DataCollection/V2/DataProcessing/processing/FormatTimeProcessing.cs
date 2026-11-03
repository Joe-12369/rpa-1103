using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataProcessing
{
    internal class FormatTimeProcessing : IProcessing
    {
        private readonly static Lazy<FormatTimeProcessing> _instance = new Lazy<FormatTimeProcessing>(() => new FormatTimeProcessing());
        public static IProcessing Instance => _instance.Value;

        public int Order => 50;

        private FormatTimeProcessing()
        {

        }
        public string Processing(string content, Commons.Params.DataCollect.ProcessingConfig config)
        {
            try
            {


                //  {
                //      label: "1970-01-01 12:00:00",
                //  "value": "\"%Y-%m-%d %H:%M:%S\""
                //},
                //{
                //      label: "1970/01/01 12:00:00",
                //  "value": "\"%Y/%m/%d %H:%M:%S\""
                //},
                //{
                //      label: "1970.01.01 12:00:00",
                //  "value": "\"%Y.%m.%d %H:%M:%S\""
                //},
                //{
                //      label: "1970年01月01日 12:00:00",
                //  "value": "\"%Y年%m月%d日 %H:%M:%S\""
                //},
                //{
                //      label: "1970-01-01T12:00:00",
                //  "value": "\"%Y-%m-%dT%H:%M:%S\""
                //},
                //{
                //      label: "1970年01月01日 12时00分00秒",
                //  "value": "\"%Y年%m月%d日 %H时%M分%S秒\""
                //},
                //{
                //      label: "1970-01-01 12:00",
                //  "value": "\"%Y-%m-%d %H:%M\""
                //},
                //{
                //      label: "1970/01/01 12:00",
                //  "value": "\"%Y/%m/%d %H:%M\""
                //},
                //{
                //      label: "1970.01.01 12:00",
                //  "value": "\"%Y.%m.%d %H:%M\""
                //},
                //{
                //      label: "1970年01月01日 12:00",
                //  "value": "\"%Y年%m月%d日 %H:%M\""
                //},
                //{
                //      label: "1970年01月01日 12时00分",
                //  "value": "\"%Y年%m月%d日 %H时%M分\""
                //},
                //{
                //      label: "01-01 12:00:00",
                //  "value": "\"%m-%d %H:%M:%S\""
                //},
                //{
                //      label: "01/01 12:00:00",
                //  "value": "\"%m/%d %H:%M:%S\""
                //},
                //{
                //      label: "01.01 12:00:00",
                //  "value": "\"%m.%d %H:%M:%S\""
                //},
                //{
                //      label: "01月01日 12:00:00",
                //  "value": "\"%m月%d日 %H:%M:%S\""
                //},
                //{
                //      label: "01月01日 12时00分00秒",
                //  "value": "\"%m月%d日 %H时%M分%S秒\""
                //},
                //{
                //      label: "1970-01-01",
                //  "value": "\"%Y-%m-%d\""
                //},
                //{
                //      label: "1970-1-1",
                //  "value": "\"%Y-%#m-%#d\""
                //},
                //{
                //      label: "1970/01/01",
                //  "value": "\"%Y/%m/%d\""
                //},
                //{
                //      label: "1970.01.01",
                //  "value": "\"%Y.%m.%d\""
                //},
                //{
                //      label: "1970年01月01日",
                //  "value": "\"%Y年%m月%d日\""
                //},
                //{
                //      label: "1970-01",
                //  "value": "\"%Y-%m\""
                //},
                //{
                //      label: "1970/01",
                //  "value": "\"%Y/%m\""
                //},
                //{
                //      label: "1970.01",
                //  "value": "\"%Y.%m\""
                //},
                //{
                //      label: "1970年01月",
                //  "value": "\"%Y年%m月\""
                //},
                //{
                //      label: "01-01",
                //  "value": "\"%m-%d\""
                //},
                //{
                //      label: "01/01",
                //  "value": "\"%m/%d\""
                //},
                //{
                //      label: "01.01",
                //  "value": "\"%m.%d\""
                //},
                //{
                //      label: "01月01日",
                //  "value": "\"%m月%d日\""
                //},
                //{
                //      label: "12:00:00",
                //  "value": "\"%H:%M:%S\""
                //},
                //{
                //      label: "12时00分00秒",
                //  "value": "\"%H时%M分%S秒\""
                //},
                //{
                //      label: "12:00",
                //  "value": "\"%H:%M\""
                //},
                //{
                //      label: "12时00分",
                //  "value": "\"%H时%M分\""
                //},
                //{
                //      label: "0-6 数字分别表示周日-周六",
                //  "value": "\"%w\""
                //},
                //{
                //      label: "数字表示一年当中的第几天",
                //  "value": "\"%j\""
                //},
                //{
                //      label: "数字表示一年当中的第几周",
                //  "value": "\"%W\""
                //}
                if (config.Parameters?.Length == 0)
                {
                    return content ?? string.Empty;
                }
                var param1 = JObject.FromObject(config.Parameters[0]);
                string dateFormat = param1.Value<string>("val");
                if (!config.IsConversion)
                {
                    //%Y-%m-%d %H:%M:%S    %Y-%#m-%#d
                    param1["val"] = dateFormat = dateFormat.ToString()
                       .Replace("%Y", "yyyy")
                       .Replace("%m", "MM")//为了避免替换错误现在月份替换成%A
                       .Replace("%d", "dd")
                       .Replace("%H", "HH")
                       .Replace("%M", "mm")
                       .Replace("%S", "ss")
                       .Replace("%#m", "M")
                       .Replace("%#d", "d");
                    config.Parameters[0] = param1;
                    config.IsConversion = true;
                }
                string format = dateFormat.ToString();
                if (DateTime.TryParse(content, out var datetime))
                {
                    switch (format)
                    {
                        case "%w"://0-6 数字分别表示周日-周六
                            return ((int)datetime.DayOfWeek).ToString();
                        case "%j"://数字表示一年当中的第几天
                            return datetime.DayOfYear.ToString();
                        case "%W"://数字表示一年当中的第几周
                            return GetTheWeekNum(datetime).ToString();
                        default:
                            return datetime.ToString(format);
                    }
                }
            }
            catch
            {
            }
            return string.Empty;
        }

        public static string GetTheWeekNum(DateTime dateTime)
        {
            //创建公历日历对象
            GregorianCalendar gregorianCalendar = new GregorianCalendar();
            //获取指定日期是周数 CalendarWeekRule指定 第一周开始于该年的第一天，DayOfWeek指定每周第一天是星期几
            int weekOfYear = gregorianCalendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            return weekOfYear.ToString();
        }
    }
}
