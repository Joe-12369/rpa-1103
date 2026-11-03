using Google.Protobuf;
using II.RPA.Core.Models;
using Rpa.Core.Worker.Settings;
using System.Collections.Generic;

namespace II.RPA.Core.GRPCService.Extension
{

    public static class SettingsExtensions
    {
        public static Dictionary<string, SettingValue> ToDictionary(this Settings settings)
        {
            var result = new Dictionary<string, SettingValue>();

            foreach (var item in settings.Items)
            {
                switch (item.ValueCase)
                {
                    case SettingItem.ValueOneofCase.StringValue:
                        result[item.Key] = SettingValue.From(item.StringValue);
                        break;
                    case SettingItem.ValueOneofCase.IntValue:
                        result[item.Key] = SettingValue.From(item.IntValue);
                        break;
                    case SettingItem.ValueOneofCase.BoolValue:
                        result[item.Key] = SettingValue.From(item.BoolValue);
                        break;
                    case SettingItem.ValueOneofCase.DoubleValue:
                        result[item.Key] = SettingValue.From(item.DoubleValue);
                        break;
                    case SettingItem.ValueOneofCase.BytesValue:
                        result[item.Key] = SettingValue.From(item.BytesValue.ToByteArray());
                        break;
                }
            }

            return result;
        }

        public static Settings ToSettings(this IEnumerable<KeyValuePair<string, SettingValue>> dict)
        {
            var settings = new Settings();

            foreach (var pair in dict)
            {
                if (string.IsNullOrWhiteSpace(pair.Key)) continue;

                var item = new SettingItem { Key = pair.Key };
                var settingValue = pair.Value;
                switch (settingValue.Type)
                {
                    case SettingValue.ValueType.String:
                        item.StringValue = (string)settingValue.RawValue!;
                        break;

                    case SettingValue.ValueType.Int:
                        item.IntValue = (int)settingValue.RawValue!;
                        break;

                    case SettingValue.ValueType.Bool:
                        item.BoolValue = (bool)settingValue.RawValue!;
                        break;

                    case SettingValue.ValueType.Double:
                        item.DoubleValue = (double)settingValue.RawValue!;
                        break;

                    case SettingValue.ValueType.Bytes:
                        if (settingValue.RawValue is byte[] ba)
                            item.BytesValue = ByteString.CopyFrom(ba);
                        else if (settingValue.RawValue is ByteString bs)
                            item.BytesValue = bs;
                        break;

                    default:
                        continue;
                }

                settings.Items.Add(item);
            }

            return settings;
        }

    }
}
