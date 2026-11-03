using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebExtensionInstall
{
    public static class NativeHostManifestEditor
    {
        public static bool EnsureAllowedOrigins(string manifestPath, IEnumerable<string> extensionIds)
        {
            try
            {
                JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
                JArray origins = manifest["allowed_origins"] as JArray;
                if (origins == null)
                {
                    origins = new JArray();
                    manifest["allowed_origins"] = origins;
                }

                foreach (string id in (extensionIds ?? Enumerable.Empty<string>())
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    string origin = $"chrome-extension://{id}/";
                    if (!origins.Values<string>().Contains(origin, StringComparer.OrdinalIgnoreCase))
                    {
                        origins.Add(origin);
                    }
                }

                File.WriteAllText(manifestPath, manifest.ToString());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
