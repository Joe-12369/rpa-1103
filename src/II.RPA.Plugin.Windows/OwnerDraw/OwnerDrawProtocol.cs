using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    internal static class OwnerDrawProtocol
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        internal static Dictionary<string, string> ParseMap(string response)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(response)) return map;
            string[] parts = response.Split('\t');
            for (int index = 1; index < parts.Length; index++)
            {
                int equals = parts[index].IndexOf('=');
                if (equals <= 0) continue;
                map[parts[index].Substring(0, equals)]
                    = parts[index].Substring(equals + 1);
            }
            return map;
        }

        internal static string ResponseKind(string response)
        {
            if (string.IsNullOrEmpty(response)) return string.Empty;
            int tab = response.IndexOf('\t');
            return tab < 0 ? response : response.Substring(0, tab);
        }

        internal static bool TryGetInt(IDictionary<string, string> map,
            string key, out int value)
        {
            value = 0;
            return map.TryGetValue(key, out string text)
                && int.TryParse(text, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out value);
        }

        internal static int GetInt(IDictionary<string, string> map,
            string key, int fallback)
        {
            return TryGetInt(map, key, out int value) ? value : fallback;
        }

        internal static string Get(IDictionary<string, string> map,
            string key, string fallback = "")
        {
            return map.TryGetValue(key, out string value) ? value : fallback;
        }

        /// <summary>Missing/empty means a real blank value; malformed data fails.</summary>
        internal static bool TryDecodeOptional(IDictionary<string, string> map,
            string key, out string value, out string status)
        {
            value = string.Empty;
            if (!map.TryGetValue(key, out string encoded)
                || string.IsNullOrEmpty(encoded))
            {
                status = key + " absent/empty (blank)";
                return true;
            }
            try
            {
                value = StrictUtf8.GetString(Convert.FromBase64String(encoded));
                status = key + " decoded length=" + value.Length;
                return true;
            }
            catch (Exception ex) when (ex is FormatException
                || ex is DecoderFallbackException)
            {
                status = key + " decode failed: " + ex.GetType().Name + ": "
                    + Short(ex.Message);
                return false;
            }
        }

        internal static string Encode(string value)
        {
            return Convert.ToBase64String(StrictUtf8.GetBytes(value ?? string.Empty));
        }

        internal static string Preview(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string normalized = value.Replace("\r", "\\r")
                .Replace("\n", "\\n").Replace("\t", "\\t");
            return normalized.Length <= 160
                ? normalized : normalized.Substring(0, 157) + "...";
        }

        internal static string Short(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string normalized = value.Replace("\r", " ").Replace("\n", " ");
            return normalized.Length <= 200
                ? normalized : normalized.Substring(0, 197) + "...";
        }
    }
}
