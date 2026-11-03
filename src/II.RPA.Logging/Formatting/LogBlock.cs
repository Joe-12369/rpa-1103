using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Logging.Formatting
{
    public class LogBlock
    {
        private IEnumerable<KeyValuePair<string, IEnumerable<KeyValuePair<string, string>>>> strcutedBlock;

        public static Func<string, IEnumerable<KeyValuePair<string, string>>> EmptyValueFormatter
             = text => new[] { text.Tag(string.Empty) };

        //支持数据行二级展开
        //public static Func<string, IEnumerable<KeyValuePair<string, string>>> SimpleValueFormatter
        //     = text =>
        //     {
        //         var subLines = text.Split(new[] { ";", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        //         return subLines.Select(x =>
        //         {
        //             var index = x.IndexOf(':');

        //             if (index < 0)
        //             {
        //                 return x.Tag(string.Empty);
        //             }

        //             return x.Substring(index).Trim(':').Tag(x.Substring(0, index));
        //         });
        //     };

        public static Func<string, IEnumerable<KeyValuePair<string, string>>> DefaultValueFormatter
             = text => new[] { text.Replace("\r", "").Replace("\n", "").Tag("") };

        public LogBlock(
            Func<string, IEnumerable<KeyValuePair<string, string>>> valueProcesser = null,
            params KeyValuePair<string, string>[] rows)
        {
            if (valueProcesser == null)
            {
                valueProcesser = EmptyValueFormatter;
            }

            strcutedBlock = rows
                .Select(x => valueProcesser(x.Value).Tag(x.Key));
        }

        public LogBlock(
            Func<string, IEnumerable<KeyValuePair<string, string>>> valueProcesser = null,
            params string[] lines)
        {
            if (valueProcesser == null)
            {
                valueProcesser = EmptyValueFormatter;
            }

            strcutedBlock = lines
                .Select(x => x.Tag(string.Empty))
                .Select(x => valueProcesser(x.Value).Tag(x.Key));
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            foreach (var item in strcutedBlock)
            {
                builder.AppendFormat("{1}\t{0,-16}", item.Key, Environment.NewLine);

                var first = item.Value.FirstOrDefault();

                if (item.Value.Count() == 1 && first.Key == string.Empty)
                {
                    builder.AppendFormat("\t{0}", first.Value.Limit());
                }
                else
                {
                    foreach (var subItem in item.Value)
                    {
                        if (subItem.Key == string.Empty &&
                            subItem.Value == string.Empty)
                        {
                            continue;
                        }

                        builder.AppendFormat("{1}\t\t{0,-20}", subItem.Key, Environment.NewLine);
                        builder.AppendFormat("\t{0}", subItem.Value.Limit());
                    }
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// 在日志中输出行号和调用方法
        /// </summary>
        /// <param name="callerLineNumber"></param>
        /// <param name="callerMemberName"></param>
        /// <returns></returns>
        public static string Location(
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerMemberName = null)
        {
            return $"{callerMemberName}@{callerLineNumber}输出：";
        }

        /// <summary>
        /// 输出若干行，可以在每一行中指定标题
        /// </summary>
        /// <param name="valueProcesser"></param>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static string Rows(Func<string, IEnumerable<KeyValuePair<string, string>>> valueProcesser = null,
            params KeyValuePair<string, string>[] rows)
        {
            return new LogBlock(valueProcesser, rows).ToString();
        }

        /// <summary>
        /// 输出若干行
        /// </summary>
        /// <param name="valueProcesser"></param>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static string Lines(Func<string, IEnumerable<KeyValuePair<string, string>>> valueProcesser = null,
            params string[] lines)
        {
            return new LogBlock(valueProcesser, lines).ToString();
        }

        /// <summary>
        /// 检查对象是否为空
        /// </summary>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <param name="callerLineNumber"></param>
        /// <param name="callerMemberName"></param>
        /// <returns></returns>
        public static string NullCheck(
            object target,
            string name,
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerMemberName = null)
        {
            var tag = $"{callerMemberName}@{callerLineNumber}检测对象是否为空{name}";
            var block = new LogBlock(default, (target == null).ToString().ToUpper().Tag(tag));
            return block.ToString();
        }

        /// <summary>
        /// 打印对象信息
        /// </summary>
        /// <param name="target"></param>
        /// <param name="valueProcesser"></param>
        /// <param name="callerLineNumber"></param>
        /// <param name="callerMemberName"></param>
        /// <returns></returns>
        public static string Object(object target,
            Func<string, IEnumerable<KeyValuePair<string, string>>> valueProcesser = default,
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerMemberName = null)
        {
            if (target == null)
            {
                return $"{callerMemberName}@{callerLineNumber}打印对象信息：null";
            }

            IEnumerable<KeyValuePair<string, string>> items;

            var settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.None,
            };

            var serializer = JsonSerializer.Create(settings);
            var jToken = JToken.FromObject(target, serializer);

            if (jToken.Type != JTokenType.Object)
            {
                items = new[] { jToken.ToString(Newtonsoft.Json.Formatting.None).Tag(string.Empty) };
            }
            else
            {
                var jObject = jToken as JObject;
                var properties = jObject.Properties().ToList();
                items = properties
                    .Where(x => x.Value != null)
                    .Select(x => x.Value.ToString(Newtonsoft.Json.Formatting.None).Tag(x.Name));
            }

            var content = new LogBlock(valueProcesser, items.ToArray());

            return $"{callerMemberName}@{callerLineNumber}打印对象信息：{content}";
        }

        /// <summary>
        /// 打印一个json字符串
        /// </summary>
        /// <param name="json"></param>
        /// <param name="valueProcesser"></param>
        /// <param name="callerLineNumber"></param>
        /// <param name="callerMemberName"></param>
        /// <returns></returns>
        public static string Json(string json,
            Func<string, IEnumerable<KeyValuePair<string, string>>> valueProcesser = default,
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerMemberName = null)
        {
            if (json == null)
            {
                return $"{callerMemberName}@{callerLineNumber}打印对象信息：null";
            }

            IEnumerable<KeyValuePair<string, string>> items;

            var settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.None,
            };

            try
            {
                var jToken = JToken.Parse(json);

                if (jToken.Type != JTokenType.Object)
                {
                    items = new[] { jToken.ToString(Newtonsoft.Json.Formatting.None).Tag(string.Empty) };
                }
                else
                {
                    var jObject = jToken as JObject;
                    var properties = jObject.Properties().ToList();
                    items = properties
                        .Where(x => x.Value != null)
                        .Select(x => x.Value.ToString(Newtonsoft.Json.Formatting.None).Tag(x.Name));
                }

                var content = new LogBlock(valueProcesser, items.ToArray());
                return $"{callerMemberName}@{callerLineNumber}打印对象信息：{content}";
            }
            catch
            {
                return $"{callerMemberName}@{callerLineNumber}打印对象信息：{json.Limit()}";
            }
        }
    }
}