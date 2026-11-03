using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;

namespace II.RPA.Plugin.GUI.ProcessCreator
{
    public static class IconFontConverter
    {
        private static readonly ILogger logger = Logger.OfContext();
        public static JObject IconFontJObject = new JObject();
        private static string IconFontClass_Prefix;
        private static JArray ClassNames;
        static IconFontConverter()
        {
            using (var fileStream = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "iconfont_component.json"), FileMode.Open))
            {
                using (var sw = new StreamReader(fileStream))
                {
                    using (var jw = new JsonTextReader(sw))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        IconFontJObject = serializer.Deserialize<JObject>(jw);
                        IconFontClass_Prefix = IconFontJObject["css_prefix_text"].ToString();
                        ClassNames = IconFontJObject["glyphs"] as JArray;
                    }
                }
            }
        }
        public static string ConvertClassToUnicode(string aClassName)
        {
            if (string.IsNullOrEmpty(aClassName))
            {
                //logger.LogWarning($"ClassName is null");
                return "\ue631";
            }
            var iconfont = ClassNames.FirstOrDefault(cn => aClassName.Equals(IconFontClass_Prefix + cn["font_class"].ToString()));
            if (iconfont != null)
            {
                var code = iconfont["unicode"];
                return Regex.Unescape("\\u" + code.ToString());
                //return unicode;
            }
            else
            {
                //logger.LogWarning($"cannot find {aClassName}");
                return "\ue631";
            }
        }
    }
}
