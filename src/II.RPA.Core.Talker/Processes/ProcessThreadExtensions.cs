using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace II.RPA.Core.Talker.Processes;

internal static class ProcessThreadExtensions
{
    public static void Resume(this Process process) =>
        ForEachThread(process, NativeMethods.ResumeThread);

    public static void Suspend(this Process process) =>
        ForEachThread(process, NativeMethods.SuspendThread);

    private static void ForEachThread(Process process, Func<IntPtr, uint> operation)
    {
        ArgumentNullException.ThrowIfNull(process);
        var threads = process.Threads.ToArray<ProcessThread>();
        if (threads.Length == 0)
        {
            return;
        }

        void Apply(ProcessThread thread)
        {
            var threadHandle = NativeMethods.OpenThread(
                NativeMethods.ThreadSuspendResume,
                inheritHandle: false,
                (uint)thread.Id);
            if (threadHandle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                operation(threadHandle);
            }
            finally
            {
                NativeMethods.CloseHandle(threadHandle);
            }
        }

        if (threads.Length == 1)
        {
            Apply(threads[0]);
            return;
        }

        Parallel.ForEach(
            threads,
            new ParallelOptions { MaxDegreeOfParallelism = threads.Length },
            Apply);
    }

    private static T[] ToArray<T>(this ICollection collection)
    {
        var items = new T[collection.Count];
        collection.CopyTo(items, 0);
        return items;
    }

    private static class NativeMethods
    {
        internal const int ThreadSuspendResume = 0x0002;

        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenThread(int desiredAccess, bool inheritHandle, uint threadId);

        [DllImport("kernel32.dll")]
        internal static extern uint ResumeThread(IntPtr thread);

        [DllImport("kernel32.dll")]
        internal static extern uint SuspendThread(IntPtr thread);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr handle);
    }
}
