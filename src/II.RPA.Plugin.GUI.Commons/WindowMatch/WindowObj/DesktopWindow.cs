using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch
{
    public class DesktopWindow : WindowObject
    {
        private static ILogger logger = Logger.OfContext();
        public DesktopWindow() : base(NativeMethods.GetDesktopWindow())
        {
            this.WindowType = WindowType.Desktop;
        }
        public IEnumerable<T> MatchDesktopWindowInZorder<T>(Func<IntPtr, T> predicate) where T : class
        {
            return MatchDesktopWindowInZorder<T>(predicate, FirstWndSourceType.DeskTopFirstWnd, true, true, true);
        }
        public IEnumerable<T> MatchDesktopWindowInZorder<T>(Func<IntPtr, T> predicate, FirstWndSourceType firstWndSourceType) where T : class
        {
            return MatchDesktopWindowInZorder<T>(predicate, firstWndSourceType, true, true, true);
        }
        public IEnumerable<T> MatchDesktopWindowInZorder<T>(Func<IntPtr, T> predicate, FirstWndSourceType firstWndSourceType, bool mustWindow, bool mustWindowVisible, bool mustNonToolWindow) where T : class
        {
            IntPtr hWnd = this.GetFirstWndHandle(firstWndSourceType);
            while (hWnd != IntPtr.Zero)
            {
                if ((!mustWindow || NativeMethods.IsWindow(hWnd))
                    && (!mustWindowVisible || NativeMethods.IsWindowVisible(hWnd))
                    && (!mustNonToolWindow || (NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE) & NativeMethods.WS_EX_TOOLWINDOW) == 0)
                )
                {
                    var ret = predicate.Invoke(hWnd);
                    if (ret != null)
                        yield return ret;
                }
                hWnd = NativeMethods.GetWindow(hWnd, GET_WINDOW_CMD.GW_HWNDNEXT);
            }
            logger.LogDebug($"EachAllWindowInZorder end");
        }
        public void WaitClientWndMin(int timeout)
        {
            RPAUtils.Retry(() =>
            {
                var childWnd = this.GetAllChildWindows((wnd) => wnd.IsWindowVisible && wnd.IsWindow && !wnd.IsToolWindow).FirstOrDefault();
                var processName = childWnd.ProcessName;
                if (processName != null && GlobleConfigurations.ClientProcessNames.Any(pn => processName.Contains(pn)))
                {
                    return false;
                }
                return true;
            }, timeout, 20);
        }
        private IntPtr GetFirstWndHandle(FirstWndSourceType firstWndSourceType)
        {
            IntPtr hWnd = default;
            switch (firstWndSourceType)
            {
                case FirstWndSourceType.DeskTopFirstWnd:
                default:
                    hWnd = NativeMethods.GetWindow(this.Handle, (uint)GET_WINDOW_CMD.GW_CHILD);
                    break;
                case FirstWndSourceType.ForegroundWnd:
                    hWnd = NativeMethods.GetForegroundWindow();
                    break;
            }
            return hWnd;
        }
        public IEnumerable<WindowObject> FindAllChildWindow(string className, string windowText, Func<WindowObject, bool> predicate = null)
        {
            var hWndChild = NativeMethods.FindWindowEx(this.Handle, IntPtr.Zero, className, windowText);
            while (hWndChild != IntPtr.Zero)
            {
                var wndInfo = new WindowObject(hWndChild);
                if (predicate == null || predicate(wndInfo))
                {
                    yield return wndInfo;
                }
                hWndChild = NativeMethods.FindWindowEx(this.Handle, hWndChild, className, windowText);
            }
        }
        public IEnumerable<WindowObject> FindAllSavaDialogWindow(Func<WindowObject, bool> predicate)
        {
            var className = "#32770";
            return FindAllChildWindow(className, null, predicate);
        }

    }
}
