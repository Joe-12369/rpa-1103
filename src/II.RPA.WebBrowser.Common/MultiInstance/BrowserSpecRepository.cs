using II.RPA.Logging;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.WebBrowser.Common.MultiInstance
{
    public class BrowserSpecRepository
    {
        private static readonly ILogger logger = Logger.OfContext();
        private static readonly ConcurrentDictionary<string, BrowserSpec> _map;

        static BrowserSpecRepository()
        {
            _map = new ConcurrentDictionary<string, BrowserSpec>(StringComparer.OrdinalIgnoreCase);
        }
        public static BrowserSpec GetSpec(string key) => _map.TryGetValue(key, out var spec) ? spec : null;
        public static BrowserSpec GetFirstOrDefaultSpec(WindowType windowType) => _map.Values.FirstOrDefault(spec => spec.WindowType == windowType);
        public static bool TryAddSpec(string key, BrowserSpec spec) => _map.TryAdd(key, spec);
        public static IReadOnlyDictionary<string, BrowserSpec> Snapshot => _map.ToDictionary(kv => kv.Key, kv => kv.Value);
        public static int LoadSpecs(IEnumerable<KeyValuePair<string, BrowserSpec>> specs)
        {
            int skipped = 0;
            foreach (var kv in specs)
            {
                if (!_map.TryAdd(kv.Key, kv.Value))
                {
                    skipped++;
                }
            }
            return skipped;
        }
        internal static BrowserSpec GetBrowserSepc(BrowserWindow browserWindow)
        {
            if (browserWindow.WindowType.HasFlag(WindowType.IE))
            {
                return BrowserSpecRepository.GetFirstOrDefaultSpec(WindowType.IE);
            }
            if (browserWindow.HitMatchHandler != null)
            {
                return BrowserSpecRepository.GetSpec(browserWindow.HitMatchHandler.MatchConfig.ConfigKey);
            }
            return BrowserSpecRepository.GetFirstOrDefaultSpec(browserWindow.WindowType);
        }
    }
}
