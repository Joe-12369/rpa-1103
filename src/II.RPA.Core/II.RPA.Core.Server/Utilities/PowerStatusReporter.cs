using II.RPA.Core.Common;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Contracts.Talker.Engine;
using II.RPA.Core.Contracts.Talker.Engine.Message;
using II.RPA.Core.Log;
using II.RPA.Core.Talker;
using II.RPA.Core.Talker.Talkers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.Core.Server.Utilities
{
    // Keep Windows power monitoring outside the independent Talker assembly.
    internal sealed class PowerStatusReporter : IDisposable
    {
        private readonly TalkerManager _manager;
        private readonly ILogger _logger;
        private readonly object _gate = new object();
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
        private readonly System.Threading.Timer _timer;
        private readonly Dictionary<string, (DateTimeOffset? ConnectedAt, int PowerStatus, bool IsPowerSaverActive)> _reported =
            new Dictionary<string, (DateTimeOffset?, int, bool)>();
        private Task _pending = Task.CompletedTask;
        private long _refreshDueAt;
        private bool _disposed;

        public PowerStatusReporter(TalkerManager manager, ILogger logger)
        {
            _manager = manager;
            _logger = logger;
            _timer = new System.Threading.Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);
            SystemInfoUtility.OnPowerSavingStateChangedEvent += OnPowerSavingChanged;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            _manager.HostConnected += OnHostConnected;
            ScheduleRefresh();
        }

        private void OnPowerSavingChanged(object sender, PowerSavingStateChangedEventArgs args) => ScheduleRefresh();
        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs args) => ScheduleRefresh();
        private void OnHostConnected(HostTalker host) => ScheduleRefresh();

        private void ScheduleRefresh()
        {
            lock (_gate)
            {
                if (!_disposed)
                {
                    // One shot: subsequent events restart the two-second debounce window.
                    _refreshDueAt = Environment.TickCount64 + 2000;
                    _timer.Change(2000, Timeout.Infinite);
                }
            }
        }

        private void OnDebounceElapsed(object state)
        {
            lock (_gate)
            {
                if (!_disposed)
                {
                    // A callback already queued before Change must respect the latest event.
                    var remaining = _refreshDueAt - Environment.TickCount64;
                    if (remaining > 0)
                    {
                        _timer.Change((int)remaining, Timeout.Infinite);
                        return;
                    }
                    // Serialize deliveries so overlapping events cannot race the snapshot cache.
                    _pending = _pending.ContinueWith(_ => ReportAsync(), CancellationToken.None,
                        TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
                }
            }
        }

        private async Task ReportAsync()
        {
            try
            {
                _shutdown.Token.ThrowIfCancellationRequested();
                var hosts = _manager.GetTalkers<HostTalker>().ToArray();
                var hostIds = new HashSet<string>(hosts.Select(host => host.Id));
                foreach (var staleId in _reported.Keys.Where(id => !hostIds.Contains(id)).ToArray())
                {
                    _reported.Remove(staleId);
                }

                var powerStatus = (int)SystemInformation.PowerStatus.PowerLineStatus;
                var isPowerSaverActive = SystemInfoUtility.IsPowerSaverActive;
                foreach (var host in hosts)
                {
                    // A new connection receives a snapshot even when power status is unchanged.
                    var snapshot = (host.ConnectedAt, powerStatus, isPowerSaverActive);
                    if (_reported.TryGetValue(host.Id, out var previous) && previous == snapshot)
                    {
                        continue;
                    }

                    try
                    {
                        var message = new EngineMessage<object>(EngineMessageActions.PowerStatusChanged, new
                        {
                            PowerStatus = powerStatus,
                            IsPowerSaverActive = isPowerSaverActive
                        });
                        await host.Notify(new FrameMessage<ActionMessage>
                        {
                            Type = FrameType.EVENT,
                            Payload = new ActionMessage
                            {
                                Action = EngineMessageActions.PowerStatusChanged,
                                Payload = EngineMessage.Serialize(message)
                            }
                        }).WaitAsync(_shutdown.Token).ConfigureAwait(false);
                        _reported[host.Id] = snapshot;
                    }
                    catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("上报电源状态失败", ex);
                    }
                }
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError("读取电源状态失败", ex);
            }
        }

        public void Dispose()
        {
            Task pending;
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                _timer.Dispose();
                pending = _pending;
            }
            SystemInfoUtility.OnPowerSavingStateChangedEvent -= OnPowerSavingChanged;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            _manager.HostConnected -= OnHostConnected;
            _shutdown.Cancel();
            pending.GetAwaiter().GetResult();
            _shutdown.Dispose();
        }
    }
}
