using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace II.RPA.Plugin.Dialog.Converts
{
    public class ValueArrrayJsonConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanConvert(Type objectType)
        {
            return typeof(string[]) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                var jArray = serializer.Deserialize<JArray>(reader);
                if (jArray != null && jArray.Any())
                {
                    var item = jArray[0];
                    if (item.Type == JTokenType.Array)
                    {
                        return item.ToObject<List<string>>()
                            .Where(i => !string.IsNullOrEmpty(i))
                            .ToArray();
                    }
                    else
                    {
                        return jArray.Select(j =>
                        {
                            if (j.Type == JTokenType.String
                               || j.Type == JTokenType.Integer
                               || j.Type == JTokenType.Float)
                            {
                                return j.ToString();
                            }
                            if (j.Type == JTokenType.Boolean)
                            {
                                return j.ToString().ToLower();
                            }
                            return string.Empty;
                        }).ToObject<List<string>>()
                            .Where(i => !string.IsNullOrEmpty(i))
                            .ToArray();
                    }

                }
            }
            catch (Exception)
            {
                //ignore
            }
            return Enumerable.Empty<string>().ToArray();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
