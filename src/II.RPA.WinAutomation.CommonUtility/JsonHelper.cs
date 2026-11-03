using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility
{
    public static class JsonHelper
    {
        /// <summary>
        /// json反序列化
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static T JsonDesByNewtonJson<T>(string input)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(input);
        }
        public static T JsonDesByNewtonJson<T>(object obj)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(JsonByNewtonJson(obj));
        }
        public static string JsonByNewtonJson(object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }
        public static string JsonByNewtonJson(object obj, Newtonsoft.Json.Formatting formating)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj, formating);
        }
        public static string JsonByNewtonJson<T>(object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }
        public static object JsonDesByNewtonJson(string input)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject(input);
        }
        public static T ToObject<T>(this string str)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(str);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <param name="errorAction">异常后的处理方法，若异常 返回default(T)</param>
        /// <returns></returns>
        public static T ToObject<T>(this string str, Action<Exception> errorAction)
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(str);

            }
            catch (Exception ex)
            {
                errorAction(ex);
                return default(T);
            }
        }
        public static T ToObject<T>(this object obj)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(JsonByNewtonJson(obj));
        }
        public static string ToJson(this object obj)
        {
            return JsonByNewtonJson(obj);
        }
        public static string ToJsonNoneFormating(this object obj)
        {
            return JsonByNewtonJson(obj, Newtonsoft.Json.Formatting.None);
        }
        public static string ToJson(this object obj, Newtonsoft.Json.Formatting formating)
        {
            return JsonByNewtonJson(obj, formating);
        }
        public static string JsonByNewtonJson(object obj, Newtonsoft.Json.JsonSerializerSettings jsonSetting)
        {
            //var jsonSetting = new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented, jsonSetting);
        }

        /// <summary>
        /// 针对对象的属性/属性链进行脱敏，可以指定脱敏方法，如果不指定（传null）则使用默认的脱敏方法，可以指定多个属性（链）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="protecter"></param>
        /// <param name="selectors"></param>
        /// <returns></returns>
        public static JObject ProtectText<T>(
            this T source,
            Func<string, string[], string> protecter,
            params Expression<Func<T, string>>[] selectors)
        {
            if (source == null)
            {
                return null;
            }

            var jObject = JObject.FromObject(source);

            if (protecter == null)
            {
                protecter = (val, pathes) =>
                {
                    return "######PROTECTED CONTENT######";
                };
            }

            foreach (var selector in selectors)
            {
                var properyChain = GetPropertyChain(selector);
                var path = string.Join(".", properyChain);
                var property = jObject.SelectToken(path)?.Parent as JProperty;

                if (property == null)
                {
                    continue;
                }

                var value = property.Value.Value<string>();
                property.Value = new JValue(protecter(value, properyChain));
            }

            return jObject;
        }

        private static string[] GetPropertyChain<T>(Expression<Func<T, string>> exp)
        {
            var result = new List<string>();
            var body = exp.Body;

            while (body != null)
            {
                if (body.NodeType == ExpressionType.MemberAccess &&
                    body is MemberExpression memberExpression)
                {
                    result.Add(memberExpression.Member.Name);
                    body = memberExpression.Expression;

                    if (body.NodeType == ExpressionType.Parameter)
                    {
                        break;
                    }
                }
                else
                {
                    throw new InvalidOperationException("仅支持成员访问表达式".I18n("controlType_JsonHelper_F3C9F7FA"));
                }
            }

            result.Reverse();
            return result.ToArray();
        }
    }
}
