using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NativeMessageingStartup
{
    internal static class Extensions
    {
        internal static Rectangle GetRectangle(this object callbackResult)
        {
            var rectObj = (dynamic)callbackResult;
            int left = rectObj.left;
            int top = rectObj.top;
            int right = rectObj.right;
            int bottom = rectObj.bottom;
            return new Rectangle(left, top, right - left, bottom - top);
        }

        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };
        public static string ToJson(this object o) => JsonConvert.SerializeObject(o, _settings);
        public static T ToObject<T>(this string json) => JsonConvert.DeserializeObject<T>(json, _settings);

    }
}
