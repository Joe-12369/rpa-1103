using System;

namespace II.RPA.Core.Models
{

    public readonly struct SettingValue
    {
        public enum ValueType
        {
            None,
            String,
            Int,
            Bool,
            Double,
            Bytes
        }

        public ValueType Type { get; }
        private readonly object _value;

        private SettingValue(ValueType type, object value)
        {
            Type = type;
            _value = value;
        }

        public static SettingValue From<T>(T value) => value switch
        {
            string s => new SettingValue(ValueType.String, s),
            int i => new SettingValue(ValueType.Int, i),
            bool b => new SettingValue(ValueType.Bool, b),
            double d => new SettingValue(ValueType.Double, d),
            float f => new SettingValue(ValueType.Double, Convert.ToDouble(f)),
            byte[] bytes => new SettingValue(ValueType.Bytes, bytes),
            _ => throw new ArgumentException($"Unsupported type: {typeof(T).Name}")
        };

        public bool TryGet<T>(out T value)
        {
            if (_value is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        public object RawValue => _value;

        public override string ToString() => _value?.ToString() ?? "null";

        public static implicit operator SettingValue(string v) => From(v);
        public static implicit operator SettingValue(int v) => From(v);
        public static implicit operator SettingValue(bool v) => From(v);
        public static implicit operator SettingValue(double v) => From(v);
        public static implicit operator SettingValue(byte[] v) => From(v);
    }
}

