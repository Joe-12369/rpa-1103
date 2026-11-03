using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace II.RPA.Core.Talker.Processes;

/// <summary>
/// Owns one Windows Job Object so child processes cannot outlive the Talker host.
/// </summary>
internal sealed class ChildProcessJob : IDisposable
{
    private static readonly Lazy<ChildProcessJob> ProcessJob =
        new(() => new ChildProcessJob(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly SafeJobHandle _job;
    private bool _disposed;

    private ChildProcessJob()
    {
        _job = NativeMethods.CreateJobObject(IntPtr.Zero, null);
        if (_job.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var info = new NativeMethods.JobObjectExtendedLimitInformation
        {
            BasicLimitInformation =
            {
                LimitFlags = NativeMethods.JobObjectLimitKillOnJobClose
            }
        };
        var length = Marshal.SizeOf<NativeMethods.JobObjectExtendedLimitInformation>();
        var pointer = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.StructureToPtr(info, pointer, false);
            if (!NativeMethods.SetInformationJobObject(
                    _job,
                    NativeMethods.JobObjectExtendedLimitInformationClass,
                    pointer,
                    (uint)length))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    public static void AddChildProcess(Process process) => ProcessJob.Value.AddProcess(process);

    private void AddProcess(Process process)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(process);
        if (process.HasExited)
        {
            throw new InvalidOperationException("进程已经退出，无法加入 Job Object。");
        }

        if (NativeMethods.AssignProcessToJobObject(_job, process.SafeHandle))
        {
            return;
        }

        var error = Marshal.GetLastWin32Error();
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Keep the original AssignProcessToJobObject error.
        }
        throw new Win32Exception(error, "无法将子进程加入 Job Object。");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _job.Dispose();
    }

    private sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeJobHandle() : base(ownsHandle: true) { }

        protected override bool ReleaseHandle() => NativeMethods.CloseHandle(handle);
    }

    private static class NativeMethods
    {
        internal const uint JobObjectLimitKillOnJobClose = 0x00002000;
        internal const int JobObjectExtendedLimitInformationClass = 9;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeJobHandle CreateJobObject(IntPtr securityAttributes, string? name);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetInformationJobObject(SafeJobHandle job, int informationClass, IntPtr information, uint informationLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AssignProcessToJobObject(SafeJobHandle job, SafeProcessHandle process);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr handle);

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobObjectBasicLimitInformation
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobObjectExtendedLimitInformation
        {
            public JobObjectBasicLimitInformation BasicLimitInformation;
            public IoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
    }
}
