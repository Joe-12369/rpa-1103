using II.RPA.Logging;
using II.RPA.Plugin.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch.WindowCache
{
    public sealed class DesktopWindowMonitor : IDisposable
    {
        private const uint EVENT_OBJECT_CREATE = 0x8000;
        private const uint EVENT_OBJECT_DESTROY = 0x8001;
        private const uint EVENT_OBJECT_SHOW = 0x8002;
        private const uint EVENT_OBJECT_HIDE = 0x8003;
        private const uint EVENT_OBJECT_NAMECHANGE = 0x800C;

        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        private const int OBJID_WINDOW = 0;
        private const int CHILDID_SELF = 0;
        private const uint WM_QUIT = 0x0012;

        public static DesktopWindowMonitor Shared { get; } = new DesktopWindowMonitor(DesktopWindowCache.Shared);

        private static ILogger logger = Logger.OfContext();

        private readonly object _syncRoot = new object();
        private readonly DesktopWindowCache _cache;
        private readonly NativeMethods.WinEventDelegate _winEventCallback;
        private readonly BlockingCollection<DesktopWindowEvent> _events = new BlockingCollection<DesktopWindowEvent>(2048);
        private readonly List<IntPtr> _hooks = new List<IntPtr>();

        private DesktopWindowCacheOptions _options = DesktopWindowCacheOptions.Default;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _workerTask;
        private Thread _hookThread;
        private ManualResetEventSlim _hookReady;
        private int _hookThreadId;
        private bool _started;
        private bool _disposed;

        public DesktopWindowMonitor(DesktopWindowCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _winEventCallback = OnWinEvent;
        }

        public DesktopWindowCache Cache => _cache;

        public Task StartAsync()
        {
            return StartAsync(DesktopWindowCacheOptions.Default, CancellationToken.None);
        }

        public Task StartAsync(DesktopWindowCacheOptions options, CancellationToken cancellationToken)
        {
            lock (_syncRoot)
            {
                ThrowIfDisposed();
                if (_started)
                {
                    return Task.FromResult(true);
                }

                _started = true;
                _options = options ?? DesktopWindowCacheOptions.Default;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                return StartCoreAsync(_cancellationTokenSource.Token);
            }
        }

        public Task StopAsync()
        {
            CancellationTokenSource cancellationTokenSource;
            Task workerTask;
            Thread hookThread;

            lock (_syncRoot)
            {
                if (!_started)
                {
                    return Task.FromResult(true);
                }

                _started = false;
                cancellationTokenSource = _cancellationTokenSource;
                workerTask = _workerTask;
                hookThread = _hookThread;
                _cancellationTokenSource = null;
                _workerTask = null;
                _hookThread = null;
            }

            cancellationTokenSource?.Cancel();
            if (_hookThreadId != 0)
            {
                NativeMethods.PostThreadMessage(_hookThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            }

            return Task.Run(() =>
            {
                try
                {
                    workerTask?.Wait(1000);
                    hookThread?.Join(1000);
                }
                catch
                {
                }
                finally
                {
                    cancellationTokenSource?.Dispose();
                }
            });
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                StopAsync().Wait(1000);
            }
            catch
            {
            }

            _events.Dispose();
            _hookReady?.Dispose();
        }

        private async Task StartCoreAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() => Scan(DesktopWindowUpdateSource.InitialScan, cancellationToken), cancellationToken).ConfigureAwait(false);

            _workerTask = Task.Run(() => ProcessEvents(cancellationToken), cancellationToken);
            StartHookThread();
        }

        private void StartHookThread()
        {
            _hookReady = new ManualResetEventSlim(false);
            _hookThread = new Thread(HookThreadProc)
            {
                IsBackground = true,
                Name = "RPA Desktop Window Monitor"
            };
            _hookThread.SetApartmentState(ApartmentState.STA);
            _hookThread.Start();
            _hookReady.Wait(3000);
        }

        private void HookThreadProc()
        {
            _hookThreadId = unchecked((int)NativeMethods.GetCurrentThreadId());
            try
            {
                RegisterWinEventHooks();
                _hookReady.Set();

                while (NativeMethods.GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
                {
                    NativeMethods.TranslateMessage(ref message);
                    NativeMethods.DispatchMessage(ref message);
                }
            }
            finally
            {
                UnregisterWinEventHooks();
                _hookThreadId = 0;
                _hookReady.Set();
            }
        }

        private void RegisterWinEventHooks()
        {
            RegisterWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_HIDE);
            RegisterWinEventHook(EVENT_OBJECT_NAMECHANGE, EVENT_OBJECT_NAMECHANGE);
        }

        private void RegisterWinEventHook(uint eventMin, uint eventMax)
        {
            var hook = NativeMethods.SetWinEventHook(
                eventMin,
                eventMax,
                IntPtr.Zero,
                _winEventCallback,
                0,
                0,
                WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);

            if (hook != IntPtr.Zero)
            {
                _hooks.Add(hook);
            }
        }

        private void UnregisterWinEventHooks()
        {
            foreach (var hook in _hooks.ToArray())
            {
                if (hook != IntPtr.Zero)
                {
                    NativeMethods.UnhookWinEvent(hook);
                }
            }

            _hooks.Clear();
        }

        private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hWnd == IntPtr.Zero || idObject != OBJID_WINDOW || idChild != CHILDID_SELF)
            {
                return;
            }

            if (eventType != EVENT_OBJECT_CREATE &&
                eventType != EVENT_OBJECT_SHOW &&
                eventType != EVENT_OBJECT_DESTROY &&
                eventType != EVENT_OBJECT_HIDE &&
                eventType != EVENT_OBJECT_NAMECHANGE)
            {
                return;
            }

            try
            {
                _events.TryAdd(new DesktopWindowEvent(hWnd, eventType, TryCreateLiveKey(hWnd)));
            }
            catch
            {
            }
        }

        private void ProcessEvents(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                DesktopWindowEvent item;
                try
                {
                    item = _events.Take(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (InvalidOperationException)
                {
                    return;
                }

                if (item.EventType == EVENT_OBJECT_DESTROY)
                {
                    if (item.Key.HasValue)
                    {
                        _cache.MarkClosed(item.Key.Value);
                    }
                    else
                    {
                        _cache.MarkClosed(item.Handle);
                    }
                    continue;
                }

                if (item.EventType == EVENT_OBJECT_HIDE)
                {
                    if (item.Key.HasValue)
                    {
                        _cache.MarkHidden(item.Key.Value);
                    }
                    else
                    {
                        _cache.MarkHidden(item.Handle);
                    }
                    continue;
                }

                try
                {
                    Task.Delay(Math.Max(0, _options.SnapshotDelayMilliseconds), cancellationToken).Wait(cancellationToken);
                }
                catch
                {
                    return;
                }

                if (DesktopWindowSnapshotProvider.TryCreate(item.Handle, _options, DesktopWindowUpdateSource.WinEvent, out var record))
                {
                    _cache.Upsert(record);
                }
                else if (!NativeMethods.IsWindow(item.Handle))
                {
                    _cache.MarkClosed(item.Handle);
                }
            }
        }

        private void Scan(DesktopWindowUpdateSource source, CancellationToken cancellationToken)
        {
            var deskWnds = new DesktopWindow().MatchDesktopWindowInZorder(handle =>
             {
                 return new WindowObject(handle);
             }).ToList().Select(wnd => wnd.Handle).ToList().Distinct().ToList();

            logger.LogDebug($"Scanning desktop windows, found {deskWnds.Count} windows.");

            deskWnds.ForEach(handle =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                if (DesktopWindowSnapshotProvider.TryCreate(handle, _options, source, out var record))
                {
                    _cache.Upsert(record);
                }
            });
        }

        private DesktopWindowCacheKey? TryCreateLiveKey(IntPtr handle)
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

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DesktopWindowMonitor));
            }
        }

        private sealed class DesktopWindowEvent
        {
            public DesktopWindowEvent(IntPtr handle, uint eventType, DesktopWindowCacheKey? key)
            {
                Handle = handle;
                EventType = eventType;
                Key = key;
            }

            public IntPtr Handle { get; }

            public uint EventType { get; }

            public DesktopWindowCacheKey? Key { get; }
        }
    }
}



