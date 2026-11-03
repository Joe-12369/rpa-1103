using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace II.RPA.Core.Models
{

    public class SettingsModel
    {
        private readonly ConcurrentDictionary<string, SettingValue> _settings = new ConcurrentDictionary<string, SettingValue>();

        public SettingsModel()
        {

        }

        public SettingsModel(IEnumerable<KeyValuePair<string, SettingValue>> items)
        {
            AddOrUpdateRange(items);
        }

        public void Set<T>(string key, T value) =>
            _settings.AddOrUpdate(key, SettingValue.From(value), (_, old) => SettingValue.From(value));

        public bool TryGet<T>(string key, out T value)
        {
            value = default!;
            return _settings.TryGetValue(key, out var settingValue) &&
                   settingValue.TryGet(out T val) &&
                   (value = val!) != null;
        }

        public IEnumerable<string> Keys => _settings.Keys;

        public IReadOnlyDictionary<string, SettingValue> Snapshot() =>
            _settings;

        public void UpdateFromDictionary(IEnumerable<KeyValuePair<string, SettingValue>> items) =>
            AddOrUpdateRange(items);

        public void AddOrUpdate(SettingsModel other) =>
            AddOrUpdateRange(other.Snapshot());

        private void AddOrUpdateRange(IEnumerable<KeyValuePair<string, SettingValue>> items)
        {
            foreach (var item in items)
            {
                _settings.AddOrUpdate(item.Key, item.Value, (_, old) => item.Value);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var setting in _settings)
            {
                sb.AppendLine($"{setting.Key}: {setting.Value}");
            }
            return sb.ToString();
        }
    }
}
