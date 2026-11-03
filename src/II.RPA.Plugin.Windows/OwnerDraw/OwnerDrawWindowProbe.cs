using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    internal static class OwnerDrawWindowProbe
    {
        private const uint ProcessQueryInformation = 0x0400;
        private const uint ProcessQueryLimitedInformation = 0x1000;
        private const uint MonitorDefaultToNearest = 2;
        private const int LogPixelsX = 88;
        private const int LogPixelsY = 90;

        internal static bool TryLocateHost(Point screenPoint, IntPtr candidateHwnd,
            Predicate<string> classPredicate, out IntPtr hostHwnd,
            out string className, out int processId, out string status)
        {
            hostHwnd = IntPtr.Zero;
            className = string.Empty;
            processId = 0;
            if (classPredicate == null)
            {
                status = "Owner-draw host class predicate is missing.";
                return false;
            }

            var seeds = new List<IntPtr>();
            if (candidateHwnd != IntPtr.Zero) seeds.Add(candidateHwnd);
            IntPtr pointHwnd = WindowFromPoint(new NativePoint(screenPoint.X,
                screenPoint.Y));
            if (pointHwnd != IntPtr.Zero && !seeds.Contains(pointHwnd))
                seeds.Add(pointHwnd);

            foreach (IntPtr seed in seeds)
            {
                IntPtr current = seed;
                for (int depth = 0; current != IntPtr.Zero && depth < 32; depth++)
                {
                    string currentClass = GetClassName(current);
                    if (classPredicate(currentClass))
                    {
                        return TryValidateHost(current, classPredicate, out hostHwnd,
                            out className, out processId, out status);
                    }
                    current = GetParent(current);
                }
            }

            // Transparent and owner-drawn windows can cause WindowFromPoint to return
            // an outer host. Drill through the real child chain before giving up.
            foreach (IntPtr seed in seeds)
            {
                IntPtr current = seed;
                for (int depth = 0; current != IntPtr.Zero && depth < 32; depth++)
                {
                    string currentClass = GetClassName(current);
                    if (classPredicate(currentClass))
                    {
                        return TryValidateHost(current, classPredicate, out hostHwnd,
                            out className, out processId, out status);
                    }

                    var client = new NativePoint(screenPoint.X, screenPoint.Y);
                    if (!ScreenToClient(current, ref client)) break;
                    IntPtr child = RealChildWindowFromPoint(current, client);
                    if (child == IntPtr.Zero || child == current)
                        child = ChildWindowFromPoint(current, client);
                    if (child == IntPtr.Zero || child == current) break;
                    current = child;
                }
            }

            status = "No matching owner-draw host exists at point "
                + screenPoint.X + "," + screenPoint.Y + ".";
            return false;
        }

        internal static bool TryValidateHost(IntPtr candidate,
            Predicate<string> classPredicate, out IntPtr hostHwnd,
            out string className, out int processId, out string status)
        {
            hostHwnd = IntPtr.Zero;
            className = string.Empty;
            processId = 0;
            if (candidate == IntPtr.Zero || !IsWindow(candidate))
            {
                status = "Owner-draw host window is unavailable.";
                return false;
            }
            className = GetClassName(candidate);
            if (classPredicate == null || !classPredicate(className))
            {
                status = "Window class is not supported: \"" + className + "\".";
                return false;
            }
            GetWindowThreadProcessId(candidate, out uint pid);
            processId = unchecked((int)pid);
            if (processId <= 0)
            {
                status = "Cannot resolve the owner-draw host process.";
                return false;
            }
            hostHwnd = candidate;
            status = "Owner-draw host validated hwnd=0x"
                + candidate.ToInt64().ToString("X") + " class=\"" + className
                + "\" pid=" + processId + ".";
            return true;
        }

        internal static bool TryGetProcessId(IntPtr hwnd, out int processId)
        {
            processId = 0;
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
            GetWindowThreadProcessId(hwnd, out uint pid);
            processId = unchecked((int)pid);
            return processId > 0;
        }

        internal static string GetClassName(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return string.Empty;
            var buffer = new StringBuilder(256);
            return GetClassNameNative(hwnd, buffer, buffer.Capacity) > 0
                ? buffer.ToString() : string.Empty;
        }

        internal static bool TryScreenToClient(IntPtr hwnd, Point screen,
            out Point client)
        {
            var native = new NativePoint(screen.X, screen.Y);
            bool success = ScreenToClient(hwnd, ref native);
            client = success ? new Point(native.X, native.Y) : Point.Empty;
            return success;
        }

        internal static bool TryGetClientOrigin(IntPtr hwnd, out Point origin)
        {
            var native = new NativePoint(0, 0);
            bool success = ClientToScreen(hwnd, ref native);
            origin = success ? new Point(native.X, native.Y) : Point.Empty;
            return success;
        }

        internal static bool TryGetWindowBounds(IntPtr hwnd, out Rectangle bounds)
        {
            bounds = Rectangle.Empty;
            if (!GetWindowRect(hwnd, out NativeRect rect)) return false;
            if (rect.Right <= rect.Left || rect.Bottom <= rect.Top) return false;
            bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
            return true;
        }

        internal static bool TryGetArchitecture(int processId,
            out OwnerDrawArchitecture architecture, out string status)
        {
            architecture = OwnerDrawArchitecture.X86;
            if (processId <= 0)
            {
                status = "Target process id is invalid.";
                return false;
            }
            if (!Environment.Is64BitOperatingSystem)
            {
                architecture = OwnerDrawArchitecture.X86;
                status = "Target architecture=x86 (32-bit operating system).";
                return true;
            }

            IntPtr process = OpenProcess(ProcessQueryLimitedInformation, false,
                unchecked((uint)processId));
            if (process == IntPtr.Zero)
            {
                process = OpenProcess(ProcessQueryInformation, false,
                    unchecked((uint)processId));
            }
            if (process == IntPtr.Zero)
            {
                status = "Cannot open target process " + processId
                    + " to verify architecture; win32=" + Marshal.GetLastWin32Error() + ".";
                return false;
            }
            try
            {
                if (!IsWow64Process(process, out bool wow64))
                {
                    status = "IsWow64Process failed for pid=" + processId
                        + "; win32=" + Marshal.GetLastWin32Error() + ".";
                    return false;
                }
                architecture = wow64 ? OwnerDrawArchitecture.X86
                    : OwnerDrawArchitecture.X64;
                status = "Target architecture="
                    + (architecture == OwnerDrawArchitecture.X64 ? "x64" : "x86")
                    + " pid=" + processId + ".";
                return true;
            }
            finally
            {
                CloseHandle(process);
            }
        }

        /// <summary>
        /// Conversion used by the validated VCL ABI 8 physical-screen-v2 protocol.
        /// </summary>
        internal static Tuple<double, double> GetBridgeClientToPhysicalScale(IntPtr hwnd)
        {
            double physicalX = 1.0;
            double physicalY = 1.0;
            try
            {
                IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
                if (monitor != IntPtr.Zero
                    && GetScaleFactorForMonitor(monitor, out int percent) == 0
                    && percent >= 50 && percent <= 500)
                {
                    physicalX = percent / 100.0;
                    physicalY = physicalX;
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException
                || ex is EntryPointNotFoundException)
            {
            }

            if (Math.Abs(physicalX - 1.0) < 0.001)
            {
                IntPtr dc = IntPtr.Zero;
                try
                {
                    dc = GetDC(hwnd);
                    if (dc != IntPtr.Zero)
                    {
                        int x = GetDeviceCaps(dc, LogPixelsX);
                        int y = GetDeviceCaps(dc, LogPixelsY);
                        if (x >= 48 && x <= 768) physicalX = x / 96.0;
                        if (y >= 48 && y <= 768) physicalY = y / 96.0;
                    }
                }
                finally
                {
                    if (dc != IntPtr.Zero) ReleaseDC(hwnd, dc);
                }
            }

            double targetLogicalDpi = 96.0;
            try
            {
                uint dpi = GetDpiForWindow(hwnd);
                if (dpi >= 48 && dpi <= 768) targetLogicalDpi = dpi;
            }
            catch (EntryPointNotFoundException)
            {
            }
            double logicalScale = targetLogicalDpi / 96.0;
            double scaleX = physicalX / logicalScale;
            double scaleY = physicalY / logicalScale;
            if (scaleX < 0.125 || scaleX > 8.0) scaleX = 1.0;
            if (scaleY < 0.125 || scaleY > 8.0) scaleY = 1.0;
            return Tuple.Create(scaleX, scaleY);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            internal NativePoint(int x, int y)
            {
                X = x;
                Y = y;
            }

            internal int X;
            internal int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(NativePoint point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern IntPtr RealChildWindowFromPoint(IntPtr parent,
            NativePoint point);

        [DllImport("user32.dll")]
        private static extern IntPtr ChildWindowFromPoint(IntPtr parent,
            NativePoint point);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ScreenToClient(IntPtr hwnd, ref NativePoint point);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ClientToScreen(IntPtr hwnd, ref NativePoint point);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "GetClassNameW",
            CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int GetClassNameNative(IntPtr hwnd,
            StringBuilder className, int maxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd,
            out uint processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("shcore.dll")]
        private static extern int GetScaleFactorForMonitor(IntPtr monitor,
            out int scale);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr dc, int index);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint desiredAccess,
            bool inheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(IntPtr process,
            [MarshalAs(UnmanagedType.Bool)] out bool wow64Process);
    }
}
