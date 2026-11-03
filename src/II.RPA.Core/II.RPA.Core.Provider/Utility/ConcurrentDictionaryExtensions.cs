using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json.Linq;

namespace II.RPA.Core.Provider.Utility
{
    public static class ConcurrentDictionaryExtensions
    {
        public static T WaitUntil<T>(Func<Tuple<bool, T>> func, CancellationToken token = default)
        {
            Lazy<SpinWait> spin = new Lazy<SpinWait>();
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var result = func.Invoke();
                if (result.Item1)
                {
                    return result.Item2;
                }
                spin.Value.SpinOnce();
            }
        }

        public static TValue GetValue<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key)
        {
            dict.TryGetValue(key, out var value);
            return value;
        }

        public static TValue Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, CancellationToken token = default)
        {
            return WaitUntil(() =>
            {
                var done = dict.TryRemove(key, out var value);
                return new Tuple<bool, TValue>(done, value);
            }, token);
        }

        public static TValue Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, int timeout)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                try
                {
                    return dict.Remove(key, cts.Token);
                }
                catch (OperationCanceledException ex)
                {
                    throw new TimeoutException($"ConcurrentDictionary.Remove timeout in {timeout} milliseconds", ex);
                }
            }
        }
    }
}
