using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace II.RPA.Core.Common.Helpers
{
    public sealed class ChildProcessJob : IDisposable
    {
        private readonly SafeJobHandle _job;
        private bool _disposed;
        private static readonly Lazy<ChildProcessJob> _processJob;

        static ChildProcessJob()
        {
            _processJob = new Lazy<ChildProcessJob>(() => new ChildProcessJob(), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private ChildProcessJob()
        {
            _job = NativeMethods.CreateJobObject(IntPtr.Zero, null);

            if (_job.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var info = new NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION { BasicLimitInformation = { LimitFlags = NativeMethods.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE } };

            int length = Marshal.SizeOf<
                NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();

            IntPtr pointer = Marshal.AllocHGlobal(length);

            try
            {
                Marshal.StructureToPtr(info, pointer, false);

                if (!NativeMethods.SetInformationJobObject(_job, NativeMethods.JobObjectExtendedLimitInformation, pointer, (uint)length))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pointer);
            }
        }

        public static void AddChildProcess(Process process)
        {
            _processJob.Value.AddProcess(process);
        }

        public void AddProcess(Process process)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            ArgumentNullException.ThrowIfNull(process);

            if (process.HasExited)
            {
                throw new InvalidOperationException("进程已经退出，无法加入 Job Object。");
            }

            if (!NativeMethods.AssignProcessToJobObject(_job, process.SafeHandle))
            {
                int error = Marshal.GetLastWin32Error();

                // 如果分配失败，立即结束该进程，避免留下失控的孤儿进程。
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // 保留原始的 AssignProcessToJobObject 错误。
                }

                throw new Win32Exception(error, "无法将子进程加入 Job Object。");
            }
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
            private SafeJobHandle() : base(ownsHandle: true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return NativeMethods.CloseHandle(handle);
            }
        }

        private static class NativeMethods
        {
            internal const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;
            internal const int JobObjectExtendedLimitInformation = 9;

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern SafeJobHandle CreateJobObject(IntPtr lpJobAttributes, string? lpName);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetInformationJobObject(SafeJobHandle hJob, int jobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AssignProcessToJobObject(SafeJobHandle hJob, SafeProcessHandle hProcess);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseHandle(IntPtr hObject);

            [StructLayout(LayoutKind.Sequential)]
            internal struct JOBOBJECT_BASIC_LIMIT_INFORMATION
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
            internal struct IO_COUNTERS
            {
                public ulong ReadOperationCount;
                public ulong WriteOperationCount;
                public ulong OtherOperationCount;
                public ulong ReadTransferCount;
                public ulong WriteTransferCount;
                public ulong OtherTransferCount;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
                public IO_COUNTERS IoInfo;
                public UIntPtr ProcessMemoryLimit;
                public UIntPtr JobMemoryLimit;
                public UIntPtr PeakProcessMemoryUsed;
                public UIntPtr PeakJobMemoryUsed;
            }
        }
    }
}
