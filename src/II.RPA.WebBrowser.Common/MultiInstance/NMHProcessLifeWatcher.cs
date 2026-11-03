using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Chrome.BrowserProfile
{
    sealed class NMHProcessLifeWatcher : IDisposable
    {
        private readonly SafeWaitHandle _hProcess;

        public NMHProcessLifeWatcher(int pid)
        {
            var raw = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.Synchronize, false, pid);
            if (raw == IntPtr.Zero)
                throw new ArgumentException(Localize($"PID {pid} 不存在或无法打开", "controlType_NMHProcessLifeWatcher_E3711C1A"), nameof(pid));
            _hProcess = new SafeWaitHandle(raw, ownsHandle: true);
        }
        public bool HasExited => NativeMethods.WaitForSingleObject(_hProcess.DangerousGetHandle(), 0) == 0;

        public void Dispose() => _hProcess?.Dispose();
    }
}
