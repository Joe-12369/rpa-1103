using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace II.RPA.Core.Common
{
    public class PowerSavingMonitor : IDisposable
    {
        #region Windows API 与常量（Vista/Win7+）

        private const int WM_POWERBROADCAST = 0x218;
        private const int PBT_POWERSETTINGCHANGE = 0x8013;
        private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;

        // 必须与 WinNT.h 一致，错误 GUID 会导致收不到电源方案变更通知（Win7 等先启动后改计划不触发事件）
        private static readonly Guid GUID_POWERSCHEME_PERSONALITY = new Guid("245D8541-3943-4422-B025-13A784F679B7");
        private static readonly Guid GUID_POWERSAVER_PLAN = new Guid("a1841308-3541-4fab-bc81-f71556f20b4a");

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, int Flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterPowerSettingNotification(IntPtr handle);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);

        [DllImport("powrprof.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint PowerReadFriendlyName(
            IntPtr RootPowerKey, ref Guid SchemeGuid, IntPtr SubGroupOfPowerSettingGuid, IntPtr PowerSettingGuid,
            byte[] Buffer, ref uint BufferSize);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct POWERBROADCAST_SETTING
        {
            public Guid PowerSettingId;
            public uint DataLength;
            public byte Data;
        }

        #endregion

        #region 有效电源模式 / 节电模式（Win10 1809+）

        private const int EffectivePowerModeBatterySaver = 0;
        private const int EffectivePowerModeEnergySaverHighSavings = 1;
        private const uint EFFECTIVE_POWER_MODE_V1 = 1;

        private delegate void EffectivePowerModeCallback(int Mode, IntPtr Context);

        [DllImport("powrprof.dll", SetLastError = true, ExactSpelling = true)]
        private static extern int PowerRegisterForEffectivePowerModeNotifications(
            uint Version, EffectivePowerModeCallback Callback, IntPtr Context, out IntPtr RegistrationHandle);

        [DllImport("powrprof.dll", SetLastError = true, ExactSpelling = true)]
        private static extern int PowerUnregisterFromEffectivePowerModeNotifications(IntPtr RegistrationHandle);

        #endregion

        private readonly NotificationWindow _notificationWindow;
        private readonly Form _hostForm;
        private readonly EffectivePowerModeCallback _effectivePowerModeCallback;
        private GCHandle _effectivePowerModeContextHandle;
        private IntPtr _effectivePowerModeRegistrationHandle;
        private volatile bool _batterySaverOn;
        private bool _lastWasPowerSaver;
        private readonly object _stateLock = new object();
        private bool _disposed;
        /// <summary>防抖：关闭节电时 Windows 会连续触发两次回调(如先 2 再 1)，采纳第一次收到的值作为最终状态，避免被第二次覆盖错。</summary>
        private readonly object _effectivePowerModeDebounceLock = new object();
        private int _pendingEffectivePowerMode = -1;
        private bool _pendingEffectivePowerModeSet;
        private System.Threading.Timer _effectivePowerModeDebounceTimer;
        private const int EffectivePowerModeDebounceMs = 80;

        /// <summary>必须在带消息循环的线程上创建（如 UI 线程），否则电源方案变更不会收到 WM_POWERBROADCAST，PowerSavingStateChanged 不会触发。无窗体应用请用 CreateWithBackgroundMessageLoop()。</summary>
        public PowerSavingMonitor() : this(null) { }

        private PowerSavingMonitor(Form hostForm)
        {
            _hostForm = hostForm;
            _notificationWindow = new NotificationWindow(this);
            _effectivePowerModeCallback = OnEffectivePowerModeChanged;
            try
            {
                _effectivePowerModeContextHandle = GCHandle.Alloc(this, GCHandleType.Weak);
                int hr = PowerRegisterForEffectivePowerModeNotifications(
                    EFFECTIVE_POWER_MODE_V1, _effectivePowerModeCallback,
                    GCHandle.ToIntPtr(_effectivePowerModeContextHandle), out _effectivePowerModeRegistrationHandle);
                if (hr != 0 || _effectivePowerModeRegistrationHandle == IntPtr.Zero)
                {
                    _effectivePowerModeRegistrationHandle = IntPtr.Zero;
                    if (_effectivePowerModeContextHandle.IsAllocated) _effectivePowerModeContextHandle.Free();
                }
            }
            catch
            {
                _effectivePowerModeRegistrationHandle = IntPtr.Zero;
                if (_effectivePowerModeContextHandle.IsAllocated) _effectivePowerModeContextHandle.Free();
            }
            _lastWasPowerSaver = GetIsPowerSaverActive();
        }

        /// <summary>适用于无窗体应用：在后台启动带消息循环的线程并创建监听器。程序退出时请 Dispose()。</summary>
        public static PowerSavingMonitor CreateWithBackgroundMessageLoop()
        {
            PowerSavingMonitor monitor = null;
            var ready = new ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                var form = new PowerMonitorHostForm();
                form.Shown += (s, e) =>
                {
                    try { monitor = new PowerSavingMonitor(form); } catch { }
                    ready.Set();
                };
                Application.Run(form);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Name = "PowerSavingMonitor";
            thread.Start();
            ready.Wait();
            if (monitor == null) throw new InvalidOperationException("无法在后台线程创建 PowerSavingMonitor。");
            return monitor;
        }

        /// <summary>当前是否为节能状态：电源计划为「节能」或 设置-电池 中「节电模式」已开启</summary>
        public bool IsPowerSaverActive => GetIsPowerSaverActive();

        /// <summary>节能状态变化时触发（进入或退出节能），EventArgs.IsPowerSaverActive 为当前是否处于节能</summary>
        public event EventHandler<PowerSavingStateChangedEventArgs> PowerSavingStateChanged;

        private bool GetIsPowerSaverActive() => GetIsPowerPlanSaver() || _batterySaverOn;

        private bool GetIsPowerPlanSaver()
        {
            IntPtr pGuid = IntPtr.Zero;
            try
            {
                if (PowerGetActiveScheme(IntPtr.Zero, out pGuid) != 0 || pGuid == IntPtr.Zero) return false;
                var guid = (Guid)Marshal.PtrToStructure(pGuid, typeof(Guid));
                if (guid == GUID_POWERSAVER_PLAN) return true;
                string name = GetSchemeFriendlyName(guid);
                return !string.IsNullOrEmpty(name) && IsPowerSaverFriendlyName(name);
            }
            catch { return false; }
            finally { if (pGuid != IntPtr.Zero) LocalFree(pGuid); }
        }

        private static void OnEffectivePowerModeChanged(int mode, IntPtr context)
        {
            var monitor = GCHandle.FromIntPtr(context).Target as PowerSavingMonitor;
            if (monitor == null || monitor._disposed) return;
            lock (monitor._effectivePowerModeDebounceLock)
            {
                // 防抖窗口内只保留第一次收到的值：关闭节电时系统常先发正确状态(如 2)再发 1，若取最后一次会导致 _batterySaverOn 错误为 true
                if (!monitor._pendingEffectivePowerModeSet)
                {
                    monitor._pendingEffectivePowerMode = mode;
                    monitor._pendingEffectivePowerModeSet = true;
                }
                if (monitor._effectivePowerModeDebounceTimer == null)
                    monitor._effectivePowerModeDebounceTimer = new System.Threading.Timer(OnEffectivePowerModeDebounceTick, monitor, EffectivePowerModeDebounceMs, Timeout.Infinite);
                else
                    monitor._effectivePowerModeDebounceTimer.Change(EffectivePowerModeDebounceMs, Timeout.Infinite);
            }
        }

        private static void OnEffectivePowerModeDebounceTick(object state)
        {
            var monitor = (PowerSavingMonitor)state;
            if (monitor._disposed) return;
            int mode;
            lock (monitor._effectivePowerModeDebounceLock)
            {
                mode = monitor._pendingEffectivePowerMode;
                monitor._pendingEffectivePowerMode = -1;
                monitor._pendingEffectivePowerModeSet = false;
                monitor._effectivePowerModeDebounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
            if (mode < 0) return;
            monitor._batterySaverOn = (mode == EffectivePowerModeBatterySaver || mode == EffectivePowerModeEnergySaverHighSavings);
            RaiseIfChanged(monitor);
        }

        private static string GetSchemeFriendlyName(Guid schemeGuid)
        {
            byte[] buffer = new byte[512];
            uint bufferSize = (uint)buffer.Length;
            if (PowerReadFriendlyName(IntPtr.Zero, ref schemeGuid, IntPtr.Zero, IntPtr.Zero, buffer, ref bufferSize) != 0) return null;
            int len = 0;
            while (len + 1 < buffer.Length && (buffer[len] != 0 || buffer[len + 1] != 0)) len += 2;
            return Encoding.Unicode.GetString(buffer, 0, len);
        }

        private static bool IsPowerSaverFriendlyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string n = name.Trim();
            // 电源计划名称：节能/省电/最长续航 等均视为节能
            // "最佳能效"/"Best power efficiency" 是用户手动选择的性能偏好，非低电量紧急模式，不纳入节能判定
            if (n.Contains("节能") || n.Contains("省电") || n.Contains("最长续航")) return true;
            if (n.IndexOf("Power saver", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.IndexOf("Power saving", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.Equals("Saver", StringComparison.OrdinalIgnoreCase)) return true;
            if (n.IndexOf("Saver", StringComparison.OrdinalIgnoreCase) >= 0 && n.IndexOf("Power", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        internal void OnPowerBroadcast(Message m)
        {
            if (m.Msg != WM_POWERBROADCAST || m.LParam == IntPtr.Zero) return;
            if (m.WParam.ToInt32() != PBT_POWERSETTINGCHANGE) return;
            try
            {
                var setting = (POWERBROADCAST_SETTING)Marshal.PtrToStructure(m.LParam, typeof(POWERBROADCAST_SETTING));
                if (setting.PowerSettingId != GUID_POWERSCHEME_PERSONALITY) return;
                // 电源方案变更时不再依赖消息体内的 GUID 解析（易受结构布局/时序影响），直接重新查询当前状态并触发事件
                RaiseIfChanged(this);
            }
            catch { }
        }

        private static void RaiseIfChanged(PowerSavingMonitor monitor)
        {
            bool current = monitor.GetIsPowerSaverActive();
            bool changed = false;
            lock (monitor._stateLock)
            {
                if (current != monitor._lastWasPowerSaver) { changed = true; monitor._lastWasPowerSaver = current; }
            }
            if (changed) RaiseStateChanged(monitor, current);
        }

        private static void RaiseStateChanged(PowerSavingMonitor monitor, bool isPowerSaverActive)
        {
            try { monitor.PowerSavingStateChanged?.Invoke(monitor, new PowerSavingStateChangedEventArgs(isPowerSaverActive)); }
            catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _notificationWindow?.Dispose();
            if (_effectivePowerModeRegistrationHandle != IntPtr.Zero)
            {
                try { PowerUnregisterFromEffectivePowerModeNotifications(_effectivePowerModeRegistrationHandle); } catch { }
                _effectivePowerModeRegistrationHandle = IntPtr.Zero;
            }
            lock (_effectivePowerModeDebounceLock)
            {
                _effectivePowerModeDebounceTimer?.Dispose();
                _effectivePowerModeDebounceTimer = null;
            }
            if (_effectivePowerModeContextHandle.IsAllocated) _effectivePowerModeContextHandle.Free();
            if (_hostForm != null && _hostForm.IsHandleCreated && !_hostForm.IsDisposed)
            {
                try { _hostForm.Invoke(new Action(() => _hostForm.Close())); } catch { }
            }
            GC.SuppressFinalize(this);
        }

        private sealed class NotificationWindow : NativeWindow, IDisposable
        {
            private IntPtr _notificationHandle;
            private readonly PowerSavingMonitor _owner;

            public NotificationWindow(PowerSavingMonitor owner)
            {
                _owner = owner;
                CreateHandle(new CreateParams { Caption = "PowerSavingMonitor", Parent = IntPtr.Zero });
                Guid g = GUID_POWERSCHEME_PERSONALITY;
                _notificationHandle = RegisterPowerSettingNotification(Handle, ref g, DEVICE_NOTIFY_WINDOW_HANDLE);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_POWERBROADCAST) _owner.OnPowerBroadcast(m);
                base.WndProc(ref m);
            }

            public void Dispose()
            {
                if (_notificationHandle != IntPtr.Zero)
                {
                    UnregisterPowerSettingNotification(_notificationHandle);
                    _notificationHandle = IntPtr.Zero;
                }
                if (Handle != IntPtr.Zero) DestroyHandle();
            }
        }
    }

    internal sealed class PowerMonitorHostForm : Form
    {
        public PowerMonitorHostForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            Size = new System.Drawing.Size(0, 0);
            StartPosition = FormStartPosition.Manual;
            Location = new System.Drawing.Point(-32000, -32000);
            Opacity = 0;
            Show();
        }
    }

    /// <summary>节能状态变化事件参数</summary>
    public class PowerSavingStateChangedEventArgs : EventArgs
    {
        public bool IsPowerSaverActive { get; }
        public PowerSavingStateChangedEventArgs(bool isPowerSaverActive) { IsPowerSaverActive = isPowerSaverActive; }
    }
}
