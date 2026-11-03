using II.RPA.Logging;
using II.RPA.Plugin.Core;
using Microsoft.Extensions.Logging;
using System;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch.WindowCache
{
    internal static class DesktopWindowSnapshotProvider
    {
        private static ILogger logger = Logger.OfContext();

        public static bool TryCreate(IntPtr handle, DesktopWindowCacheOptions options, DesktopWindowUpdateSource source, out DesktopWindowRecord record)
        {
            record = null;
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                if (!NativeMethods.IsWindow(handle))
                {
                    return false;
                }

                NativeMethods.GetWindowThreadProcessId(handle, out var processId);
                if (processId == 0)
                {
                    return false;
                }

                if (!options.IncludeNonRootWindows && NativeMethods.GetAncestor(handle, GetAncestorFlags.GetRoot) != handle)
                {
                    return false;
                }

                var isVisible = NativeMethods.IsWindowVisible(handle);
                if (!options.IncludeInvisibleWindows && !isVisible)
                {
                    return false;
                }

                if (!options.IncludeToolWindows && ((int)NativeMethods.GetWindowLong(handle, NativeMethods.GWL_EXSTYLE) & NativeMethods.WS_EX_TOOLWINDOW) != 0)
                {
                    return false;
                }

                var key = new DesktopWindowCacheKey(handle, unchecked((int)processId));
                record = new DesktopWindowRecord(key)
                {
                    ProcessName = DesktopWindowCache.SafeGetProcessName(unchecked((int)processId)),
                    LastUpdateSource = source,
                    State = isVisible ? DesktopWindowState.Active : DesktopWindowState.Hidden
                };
                //logger.LogDebug($"Created DesktopWindowRecord for handle {handle} (process ID {processId}, process name '{record.ProcessName}')");
                return true;
            }
            catch
            {
                record = null;
                return false;
            }
        }

    }
}

