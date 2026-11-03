using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Deeps.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace II.RPA.Plugin.GUI.Deeps
{
    public class WndService : IDisposable
    {
        private readonly static ILogger _logger = Logger.OfContext();
        private readonly HashSet<string> _productNames;
        private readonly ConcurrentDictionary<IntPtr, WindowCacheInfo> _excludedWindowsCache;
        private readonly Timer _cacheCleanupTimer;
        private readonly TimeSpan _cacheValidDuration;
        private bool _disposed = false;
        private readonly ConcurrentDictionary<uint, string> _processNameCache = new ConcurrentDictionary<uint, string>();

        public WndService()
        {
            _productNames = new HashSet<string>
            {
                "实在设计器",
                "实在设计器.exe", 
                "II.RPA.Core.Server.exe",
                "II.RPA.Core.Server",
                "II.RPA.RuntimeHost.exe",
                "II.RPA.RuntimeHost"
            };
            _excludedWindowsCache = new ConcurrentDictionary<IntPtr, WindowCacheInfo>();
            _cacheValidDuration = TimeSpan.FromMinutes(5);

            _cacheCleanupTimer = new Timer(CleanupExpiredCache, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
        }
        public WndService(IEnumerable<string> productNames, TimeSpan? cacheValidDuration = null, TimeSpan? cleanupInterval = null)
        {
            _productNames = new HashSet<string>(productNames ?? throw new ArgumentNullException(nameof(productNames)));
            _excludedWindowsCache = new ConcurrentDictionary<IntPtr, WindowCacheInfo>();
            _cacheValidDuration = cacheValidDuration ?? TimeSpan.FromMinutes(5);

            var interval = cleanupInterval ?? TimeSpan.FromMinutes(2);
            _cacheCleanupTimer = new Timer(CleanupExpiredCache, null, interval, interval);
        }
        private void CleanupExpiredCache(object state)
        {
            if (_disposed) return;

            var expiredKeys = new List<IntPtr>();
            var now = DateTime.Now;

            foreach (var kvp in _excludedWindowsCache)
            {
                var cacheInfo = kvp.Value;

                if (now - cacheInfo.CacheTime > _cacheValidDuration)
                {
                    expiredKeys.Add(kvp.Key);
                    continue;
                }

                if (!NativeMethods.IsWindow(kvp.Key))
                {
                    expiredKeys.Add(kvp.Key);
                    continue;
                }

                _ = UiCommon.GetWindowThreadProcessId(kvp.Key, out uint currentProcessId);
                if (currentProcessId != cacheInfo.ProcessId)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _excludedWindowsCache.TryRemove(key, out _);
            }

            _logger.LogInformation($"清理了 {expiredKeys.Count} 个过期的窗口缓存项");
        }

        private bool IsWindowExcluded(IntPtr hWnd, uint processId)
        {
            if (_excludedWindowsCache.TryGetValue(hWnd, out var cacheInfo))
            {
                if (DateTime.Now - cacheInfo.CacheTime < _cacheValidDuration &&
                    cacheInfo.ProcessId == processId)
                {
                    return cacheInfo.IsExcluded;
                }

                _excludedWindowsCache.TryRemove(hWnd, out _);
            }

            bool isExcluded = false;
            string processName = string.Empty;

            try
            {
                processName = GetProcessNameById(processId);
                isExcluded = _productNames.Contains(processName);
            }
            catch
            {
                isExcluded = false;
            }

            var newCacheInfo = new WindowCacheInfo
            {
                ProcessId = (int)processId,
                ProcessName = processName,
                CacheTime = DateTime.Now,
                IsExcluded = isExcluded
            };

            _excludedWindowsCache[hWnd] = newCacheInfo;
            if (isExcluded)
            {
                _logger.LogInformation($"excluded wnd-{hWnd.ToString("x")}:{newCacheInfo}");
            }
            return isExcluded;
        }

        public List<ElementModel> GetRootWindows(bool onlyValid = true)
        {
            ThrowIfDisposed();

            var results = new List<ElementModel>();
            var currentLevel = Utils.GetCurrentProcessIntegrityInfo();

            UiCommon.EnumWindows((hWnd, lParam) =>
            {
                try
                {
                    var tid = UiCommon.GetWindowThreadProcessId(hWnd, out uint processId);

                    if (tid == 0 || IsWindowExcluded(hWnd, processId))
                    {
                        return true;
                    }

                    var targetInfo = Utils.GetProcessIntegrityInfo((int)processId);
                    bool isAdmin = targetInfo != null && targetInfo.Level >= Utils.SECURITY_MANDATORY_HIGH_RID;
                    bool isHigher = targetInfo != null && targetInfo.Level > currentLevel.Level;
                    NativeMethods.RECT rect = new NativeMethods.RECT();
                    bool visible = NativeMethods.IsWindowVisible(hWnd);
                    bool hasRect = NativeMethods.GetWindowRect(hWnd, out rect);
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;

                    if (onlyValid)
                    {
                        if (!visible || !hasRect || width <= 0 || height <= 0 || tid == 0 || isHigher)
                            return true;
                    }

                    var name = UiCommon.GetWindowText(hWnd) ?? string.Empty;
                    var automationElement = UiCommon.UIAAutomation.FromHandle(hWnd);

                    if (string.IsNullOrEmpty(name))
                    {
                        name = automationElement.Properties.Name.ValueOrDefault;
                    }
                    if (string.IsNullOrEmpty(name))
                    {
                        name = $"0x{hWnd.ToString("X")}";
                        return true;
                    }

                    results.Add(new ElementModel(automationElement)
                    {
                        Name = name,
                        Tag = "Window",
                        Handle = hWnd,
                        ProcessId = (int)processId,
                        IsAdmin = isAdmin,
                        IsHigher = isHigher,
                        IntegrityLevel = targetInfo?.Level ?? 0,
                        IntegrityName = targetInfo?.Name ?? string.Empty,
                        Region = new System.Drawing.Rectangle(rect.Left, rect.Top, width, height)
                    });
                }
                catch
                {
                }
                return true;
            }, IntPtr.Zero);

            return results;
        }

        public void InvalidateWindowCache(IntPtr hWnd)
        {
            ThrowIfDisposed();
            _excludedWindowsCache.TryRemove(hWnd, out _);
        }

        public bool IsWindowExcluded(IntPtr hWnd)
        {
            ThrowIfDisposed();
            if (_excludedWindowsCache.TryGetValue(hWnd, out var info))
            {
                return info.IsExcluded;
            }
            else
            {
                var tid = UiCommon.GetWindowThreadProcessId(hWnd, out uint processId);

                if (tid == 0 || IsWindowExcluded(hWnd, processId))
                {
                    return true;
                }
            }
            return false;
        }

        public void AddToExcludedProcessNames(string name)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(name) || _productNames.Contains(name))
            {
                return;
            }
            _productNames.Add(name);
        }

        public void ClearAllCache()
        {
            ThrowIfDisposed();
            _excludedWindowsCache.Clear();
            _processNameCache.Clear();
        }

        public (int TotalCached, int ExcludedWindows, int IncludedWindows) GetCacheStats()
        {
            ThrowIfDisposed();

            int totalCached = _excludedWindowsCache.Count;
            int excludedWindows = 0;
            int includedWindows = 0;

            foreach (var cacheInfo in _excludedWindowsCache.Values)
            {
                if (cacheInfo.IsExcluded)
                    excludedWindows++;
                else
                    includedWindows++;
            }

            return (totalCached, excludedWindows, includedWindows);
        }

        public void WarmupCache()
        {
            ThrowIfDisposed();
            _logger.LogDebug($"ignore processes:{string.Join(" ", _productNames)}");
            InitProcesses();
            UiCommon.EnumWindows((hWnd, lParam) =>
            {
                try
                {
                    var tid = UiCommon.GetWindowThreadProcessId(hWnd, out uint processId);
                    if (tid != 0 && processId != 0)
                    {
                        IsWindowExcluded(hWnd, processId);
                    }
                }
                catch
                {
                }
                return true;
            }, IntPtr.Zero);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WndService));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _cacheCleanupTimer?.Dispose();
                _excludedWindowsCache.Clear();
                _disposed = true;
            }
        }
        private string GetProcessNameById(uint processId)
        {
            if (_processNameCache.TryGetValue(processId, out var cachedName))
                return cachedName;

            try
            {
                using (var process = Process.GetProcessById((int)processId))
                {
                    var name = process.ProcessName;
                    _processNameCache[processId] = name;
                    return name;
                }
            }
            catch
            {
                return string.Empty;
            }
        }
        private void InitProcesses()
        {
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    _processNameCache[(uint)proc.Id] = proc.ProcessName;
                }
                catch
                {
                    
                }
                finally
                {
                    proc.Dispose();
                }
            }

        }

    }
}