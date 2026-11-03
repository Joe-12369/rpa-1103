using System;
using System.Collections.Concurrent;
using System.Threading;

namespace II.RPA.MobileProvider.Provider
{
    internal static class MobileSystemPortAllocator
    {
        private const int FirstSystemPort = 8200;
        private const int LastSystemPort = 8299;
        private static int _nextPort = FirstSystemPort - 1;
        private static readonly ConcurrentDictionary<string, int> Ports =
            new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public static int GetOrAllocate(string udid)
        {
            return Ports.GetOrAdd(udid ?? string.Empty, _ =>
            {
                int candidate = Interlocked.Increment(ref _nextPort);
                if (candidate > LastSystemPort)
                {
                    throw new InvalidOperationException("没有可用的UiAutomator2 systemPort");
                }
                return candidate;
            });
        }

        public static bool TryGet(string udid, out int systemPort)
        {
            return Ports.TryGetValue(udid ?? string.Empty, out systemPort);
        }

        public static void Release(string udid)
        {
            Ports.TryRemove(udid ?? string.Empty, out _);
        }
    }
}
