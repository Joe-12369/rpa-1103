using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch.WindowCache
{
    public sealed class DesktopWindowCache
    {
        private static ILogger logger = Logger.OfContext();

        public static DesktopWindowCache Shared { get; } = new DesktopWindowCache();

        private readonly ConcurrentDictionary<DesktopWindowCacheKey, DesktopWindowRecord> _activeWindows = new ConcurrentDictionary<DesktopWindowCacheKey, DesktopWindowRecord>();
        private readonly ConcurrentDictionary<DesktopWindowCacheKey, DesktopWindowRecord> _closedWindows = new ConcurrentDictionary<DesktopWindowCacheKey, DesktopWindowRecord>();
        private readonly ConcurrentDictionary<IntPtr, DesktopWindowCacheKey> _latestKeyByHandle = new ConcurrentDictionary<IntPtr, DesktopWindowCacheKey>();
        private readonly ConcurrentDictionary<string, string> _productNameByPath = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void Upsert(DesktopWindowRecord snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            _latestKeyByHandle[snapshot.Handle] = snapshot.Key;
            _closedWindows.TryRemove(snapshot.Key, out _);
            _activeWindows.AddOrUpdate(snapshot.Key, snapshot, (key, existing) =>
            {
                lock (existing)
                {
                    existing.ApplySnapshot(snapshot);
                    return existing;
                }
            });
        }

        public bool TryGet(IntPtr handle, out DesktopWindowRecord record)
        {
            record = null;
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            var key = ResolveLiveKey(handle);
            if (key.HasValue && _activeWindows.TryGetValue(key.Value, out var activeRecord))
            {
                record = activeRecord;
                return true;
            }

            return false;
        }

        public bool TryGet(DesktopWindowCacheKey key, out DesktopWindowRecord record)
        {
            return _activeWindows.TryGetValue(key, out record);
        }

        public IReadOnlyCollection<DesktopWindowRecord> GetActiveWindows()
        {
            return _activeWindows.Values.Select(item => item.Clone()).ToList();
        }

        public IReadOnlyCollection<DesktopWindowRecord> GetRecentlyClosedWindows()
        {
            return _closedWindows.Values.Select(item => item.Clone()).ToList();
        }

        public bool TryGetProcessName(IntPtr handle, out string processName)
        {
            processName = null;
            if (!TryGet(handle, out var record) || string.IsNullOrWhiteSpace(record.ProcessName))
            {
                return false;
            }

            processName = record.ProcessName;
            return true;
        }

        public string GetOrAddProcessName(IntPtr handle)
        {
            var key = ResolveLiveKey(handle);
            if (!key.HasValue)
            {
                return null;
            }

            if (_activeWindows.TryGetValue(key.Value, out var record) && !string.IsNullOrWhiteSpace(record.ProcessName))
            {
                return record.ProcessName;
            }

            var processName = SafeGetProcessName(key.Value.ProcessId);
            if (string.IsNullOrWhiteSpace(processName))
            {
                return null;
            }

            UpsertProcessName(key.Value, processName);
            return processName;
        }

        public bool TryGetProgramPath(IntPtr handle, out string programPath)
        {
            programPath = null;
            if (!TryGet(handle, out var record) || string.IsNullOrWhiteSpace(record.ProgramPath))
            {
                return false;
            }

            programPath = record.ProgramPath;
            return true;
        }

        public string GetOrAddProgramPath(IntPtr handle)
        {
            var key = ResolveLiveKey(handle);
            if (!key.HasValue)
            {
                return null;
            }

            if (_activeWindows.TryGetValue(key.Value, out var record) && !string.IsNullOrWhiteSpace(record.ProgramPath))
            {
                return record.ProgramPath;
            }

            var programPath = SafeGetProgramPath(key.Value.ProcessId);
            if (string.IsNullOrWhiteSpace(programPath))
            {
                return null;
            }

            UpsertProgramPath(key.Value, programPath);
            return programPath;
        }

        public string GetOrAddProductName(IntPtr handle)
        {
            var appExePath = GetOrAddProgramPath(handle);
            if (string.IsNullOrWhiteSpace(appExePath))
            {
                return null;
            }

            if (_productNameByPath.TryGetValue(appExePath, out var productName))
            {
                return productName;
            }

            productName = SafeGetProductName(appExePath);
            _productNameByPath[appExePath] = productName ?? string.Empty;
            return productName;
        }

        private void UpsertProcessName(DesktopWindowCacheKey key, string processName)
        {
            if (key.Handle == IntPtr.Zero || key.ProcessId == 0 || string.IsNullOrWhiteSpace(processName))
            {
                return;
            }

            var record = _activeWindows.GetOrAdd(key, item => new DesktopWindowRecord(item));
            lock (record)
            {
                record.ProcessName = processName;
                record.LastSeenAt = DateTimeOffset.Now;
                record.LastUpdateSource = DesktopWindowUpdateSource.Manual;
            }
            _latestKeyByHandle[key.Handle] = key;
        }

        private void UpsertProgramPath(DesktopWindowCacheKey key, string programPath)
        {
            if (key.Handle == IntPtr.Zero || key.ProcessId == 0 || string.IsNullOrWhiteSpace(programPath))
            {
                return;
            }

            var record = _activeWindows.GetOrAdd(key, item => new DesktopWindowRecord(item));
            lock (record)
            {
                record.ProgramPath = programPath;
                record.LastSeenAt = DateTimeOffset.Now;
                record.LastUpdateSource = DesktopWindowUpdateSource.Manual;
            }
            _latestKeyByHandle[key.Handle] = key;
        }

        public void MarkHidden(IntPtr handle)
        {
            var key = ResolveLiveKey(handle);
            if (key.HasValue)
            {
                MarkHidden(key.Value);
            }
        }

        public void MarkHidden(DesktopWindowCacheKey key)
        {
            if (!_activeWindows.TryGetValue(key, out var record))
            {
                return;
            }

            lock (record)
            {
                record.State = DesktopWindowState.Hidden;
                record.LastSeenAt = DateTimeOffset.Now;
                record.LastUpdateSource = DesktopWindowUpdateSource.WinEvent;
            }
        }

        public void MarkClosed(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                if (NativeMethods.IsWindow(handle))
                {
                    return;
                }
            }
            catch
            {
            }

            if (_latestKeyByHandle.TryGetValue(handle, out var key))
            {
                MarkClosed(key);
            }
        }

        public void MarkClosed(DesktopWindowCacheKey key)
        {
            if (!_activeWindows.TryRemove(key, out var record))
            {
                return;
            }

            lock (record)
            {
                record.State = DesktopWindowState.Closed;
                record.ClosedAt = DateTimeOffset.Now;
                record.LastSeenAt = record.ClosedAt.Value;
                record.LastUpdateSource = DesktopWindowUpdateSource.WinEvent;
            }

            if (_latestKeyByHandle.TryGetValue(record.Handle, out var latestKey) && latestKey.Equals(key))
            {
                _latestKeyByHandle.TryRemove(record.Handle, out _);
            }
            _closedWindows[key] = record;
        }

        private DesktopWindowCacheKey? ResolveLiveKey(IntPtr handle)
        {
            try
            {
                NativeMethods.GetWindowThreadProcessId(handle, out var processId);
                if (processId != 0)
                {
                    return new DesktopWindowCacheKey(handle, unchecked((int)processId));
                }
            }
            catch
            {
            }
            return null;
        }

        internal static string SafeGetProcessName(int processId)
        {
            if (processId == 0)
            {
                return string.Empty;
            }

            try
            {
                //logger.LogDebug($"Attempting to retrieve process name for process ID {processId}");
                return NormalizeProcessName(UiCommon.GetProcessNameById(processId));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SafeGetProgramPath(int processId)
        {
            try
            {
                return Process.GetProcessById(processId).GetExeFilePath();
            }
            catch
            {
                return null;
            }
        }

        private static string SafeGetProductName(string appExePath)
        {
            if (string.IsNullOrWhiteSpace(appExePath) || !File.Exists(appExePath))
            {
                return string.Empty;
            }

            try
            {
                return FileVersionInfo.GetVersionInfo(appExePath).ProductName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizeProcessName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return string.Empty;
            }

            return processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? processName.Substring(0, processName.Length - 4)
                : processName;
        }
    }
}




