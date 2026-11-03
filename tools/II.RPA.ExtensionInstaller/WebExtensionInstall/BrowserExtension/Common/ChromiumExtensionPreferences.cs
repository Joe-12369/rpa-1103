using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebExtensionInstall
{
    public static class ChromiumExtensionPreferences
    {
        public static bool TryCleanProfile(string profilePath, IEnumerable<string> extensionIds)
        {
            return TryCleanFile(Path.Combine(profilePath, "Preferences"), extensionIds);
        }

        public static bool RemoveExtensionData(JObject preferences, IEnumerable<string> extensionIds)
        {
            HashSet<string> ids = CreateExtensionIdSet(extensionIds);
            bool changed = false;
            changed |= RemoveIds(preferences["extensions"]?["external_uninstalls"] as JArray, ids);
            changed |= RemoveIds(preferences["extensions"]?["toolbar"] as JArray, ids);

            JObject apps = preferences["updateclientdata"]?["apps"] as JObject;
            if (apps != null)
            {
                foreach (string extensionId in ids)
                {
                    changed |= apps.Remove(extensionId);
                }
            }

            return changed;
        }

        public static bool TryCleanFile(string preferencesPath, IEnumerable<string> extensionIds)
        {
            if (!File.Exists(preferencesPath))
            {
                return true;
            }

            string temporaryPath = preferencesPath + ".iirpa.tmp";
            string backupPath = preferencesPath + ".iirpa.bak";
            try
            {
                JObject preferences = JObject.Parse(File.ReadAllText(preferencesPath));
                if (!RemoveExtensionData(preferences, extensionIds))
                {
                    return true;
                }

                File.WriteAllText(temporaryPath, preferences.ToString(Formatting.None));
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                File.Replace(temporaryPath, preferencesPath, backupPath, true);
                File.Delete(backupPath);
                return true;
            }
            catch
            {
                TryDelete(temporaryPath);
                if (!File.Exists(preferencesPath) && File.Exists(backupPath))
                {
                    File.Move(backupPath, preferencesPath);
                }
                return false;
            }
        }

        private static HashSet<string> CreateExtensionIdSet(IEnumerable<string> extensionIds)
        {
            return new HashSet<string>(
                (extensionIds ?? Enumerable.Empty<string>()).Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
        }

        private static bool RemoveIds(JArray values, HashSet<string> extensionIds)
        {
            if (values == null)
            {
                return false;
            }

            List<JToken> matches = values
                .Where(value => value.Type == JTokenType.String && extensionIds.Contains(value.Value<string>()))
                .ToList();
            foreach (JToken match in matches)
            {
                match.Remove();
            }

            return matches.Count > 0;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }
}
