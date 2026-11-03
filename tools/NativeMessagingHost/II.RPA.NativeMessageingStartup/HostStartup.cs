using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Logging;
using II.RPA.NativeMessageingStartup.NamedPipe;
using Microsoft.Extensions.Logging;

namespace II.RPA.NativeMessageingStartup
{
    public class HostStartup
    {
        private const int TryCount = 30;
        private string processName;
        private string serviceKey;
        private static ILogger logger = Logger.OfContext();

        public static IntPtr ParentHwd { get; private set; }

        public HostStartup()
        {
            try
            {
                this.serviceKey = ConfigurationManager.AppSettings["servicekey"];
            }
            catch
            {
                logger.LogError("Cannot find the servicekey in config file.");
                logger.LogError("MESSAGE HOST EXIT(04)");
                Environment.Exit(0);
            }

            try
            {
                this.processName = ConfigurationManager.AppSettings["processname"];
            }
            catch
            {
                logger.LogError("Cannot find the processname in config file.");
                logger.LogError("MESSAGE HOST EXIT(05)");
                Environment.Exit(0);
            }

        }

        public void Startup(IEnumerable<string> args)
        {

            Task.Run(() => ProcessSnapshot.WriteCurrent());

            var msgTask = MessageService.Startup();
            var _ = Task.Run(async () => await msgTask.ConfigureAwait(false));

            NamedPipeHostService namedPipeService = null;
            try
            {
                namedPipeService = new NamedPipeHostService(this.serviceKey, new NamedPipeMessageHandler());
                namedPipeService.StartAsync().GetAwaiter().GetResult();
                namedPipeService.Ready.GetAwaiter().GetResult();
                logger.LogInformation($"Named Pipe service ready: {namedPipeService.PipeName}");

                var namedPipeServiceForMonitoring = namedPipeService;
                Task.Run(async () =>
                {
                    try
                    {
                        await namedPipeServiceForMonitoring.Completion.ConfigureAwait(false);
                        logger.LogError("Named Pipe service stopped unexpectedly.");
                    }
                    catch (Exception exception)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, exception, "Named Pipe service terminated unexpectedly.");
                    }

                    logger.LogError("MESSAGE HOST EXIT(07)");
                    Environment.Exit(1);
                });
            }
            catch (Exception exception)
            {
                namedPipeService?.Dispose();
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, exception, "Named Pipe service failed to start. IPC will not be started.");
                logger.LogError("MESSAGE HOST EXIT(06)");
                Environment.Exit(1);
            }

            int tryCount = 0;

            while (true)
            {
                if (tryCount >= TryCount)
                {
                    logger.LogError($"Error occur! up to max try restart times:{tryCount}");
                    break;
                }
                try
                {
                    using (PipeHostService pipeService = new PipeHostService(this.serviceKey))
                    {
                        HostService service = new HostService();
                        var task = Task.Factory.StartNew((contextObj) =>
                        {
                            var context = contextObj as Tuple<PipeHostService, HostService>;
                            context.Item1.StartHostService(context.Item2);
                        }, new Tuple<PipeHostService, HostService>(pipeService, service));
                        task.Wait();

                        try
                        {
                            msgTask.GetAwaiter().GetResult();
                            Thread.Sleep(100);
                            logger.LogError("MESSAGE HOST EXIT(01)");
                            Environment.Exit(0);
                        }
                        catch (Exception ex0)
                        {
                            logger.LogError(ex0);
                            logger.LogError("MESSAGE HOST EXIT(02)");
                            Environment.Exit(1);
                        }
                    }
                }
                catch (Exception ex1)
                {
                    logger.LogError(ex1);
                }
                Thread.Sleep(100);
                tryCount++;
                logger.LogError($"Error occur! try to restart times:{tryCount}");
            }
            logger.LogWarning("IPC service is unavailable. NMH will continue in NamedPipe-only mode.");

            try
            {
                msgTask.GetAwaiter().GetResult();
                logger.LogError("MESSAGE HOST EXIT(01)");
                Environment.Exit(0);
            }
            catch (Exception exception)
            {
                logger.LogError(exception);
                logger.LogError("MESSAGE HOST EXIT(02)");
                Debug.Assert(false, "MESSAGE HOST EXIT(02)");
                Environment.Exit(1);
            }
        }

        private Process[] GetProcessesByName(string processName, bool onlyCurrentUser)
        {
            try
            {
                if (!onlyCurrentUser)
                {
                    return Process.GetProcessesByName(processName);
                }
                return (from m in Process.GetProcessesByName(processName)
                        where GetProcessOwner(m.Id) == Environment.UserName
                        select m).ToArray();
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }

            return null;
        }

        private string GetProcessOwner(int processId)
        {
            try
            {
                foreach (ManagementObject obj in new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID = " + processId).Get())
                {
                    string[] argList = new string[2]
                    {
                    string.Empty,
                    string.Empty
                    };
                    object[] args = argList;
                    if (Convert.ToInt32(obj.InvokeMethod("GetOwner", args)) == 0)
                    {
                        return argList[0];
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return "NO OWNER";
            }

            return "NO OWNER";
        }

        private void KillPrevHost()
        {
            Process[] processesByName = GetProcessesByName(this.processName, onlyCurrentUser: true);
            int curPid = Process.GetCurrentProcess().Id;
            Process[] array = processesByName;
            foreach (Process process in array)
            {
                if (process.Id != curPid)
                {
                    try
                    {
                        logger.LogInformation($"Detect other process left.Kill it! {process.Id}");
                        process.Kill();
                    }
                    catch
                    {
                    }
                }
            }
        }

        static bool CheckedInThread = false;

        public static void HideWindow(IntPtr hWnd)
        {
            try
            {
                int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                exStyle &= ~WS_EX_APPWINDOW;
                exStyle |= WS_EX_TOOLWINDOW;
                SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);

                SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
                SetForegroundWindow(GetDesktopWindow());
                ShowWindow(hWnd, SW_HIDE);

                if (!IsWindowVisible(hWnd))
                {
                    logger.LogInformation($"Window {hWnd} hidden successfully.");
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex);
            }

            if (!CheckedInThread)
            {
                CheckedInThread = true;
                Thread t = new Thread(() =>
                {
                    while (IsWindow(hWnd))
                    {
                        Thread.Sleep(1000);
                        if (IsWindowVisible(hWnd) || IsShowInTaskBar(hWnd))
                        {
                            logger.LogInformation($"Re-hide window:{hWnd} again.");
                            HideWindow(hWnd);
                        }
                    }
                })
                {
                    IsBackground = true
                };
                t.Start();
            }
        }

        private const string WORKER_PAGE_TITLE = "实在agent";
        private const int SW_HIDE = 0;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_HIDEWINDOW = 0x0080;
        private const int SW_MINIMIZE = 6;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_FRAMECHANGED = 0x0020;
        public enum GET_WINDOW_CMD : uint
        {
            GW_HWNDFIRST = 0U,
            GW_HWNDLAST = 1U,
            GW_HWNDNEXT = 2U,
            GW_HWNDPREV = 3U,
            GW_OWNER = 4U,
            GW_CHILD = 5U,
            GW_ENABLEDPOPUP = 6U
        }
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, GET_WINDOW_CMD nCmd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static string GetWindowText(IntPtr ptr)
        {
            var textLen = GetWindowTextLength(ptr) * 2 + 1;
            StringBuilder textSB = new StringBuilder(textLen);
            GetWindowText(ptr, textSB, textLen);
            return textSB.ToString();
        }
        public List<IntPtr> GetWorkerPageWnd()
        {
            List<IntPtr> wndList = new List<IntPtr>();
            IntPtr hWnd = GetWindow(GetDesktopWindow(), GET_WINDOW_CMD.GW_CHILD);
            while (hWnd != IntPtr.Zero)
            {
                if (IsWindow(hWnd)
                    && IsWindowVisible(hWnd)
                    && ((GetWindowLong(hWnd, GWL_EXSTYLE) & WS_EX_TOOLWINDOW) == 0)
                )
                {
                    var title = GetWindowText(hWnd);
                    if (title.Contains(WORKER_PAGE_TITLE))
                    {
                        wndList.Add(hWnd);
                        logger.LogDebug($"{hWnd},{hWnd.ToString("x4")},{title}");
                    }
                }
                hWnd = GetWindow(hWnd, GET_WINDOW_CMD.GW_HWNDNEXT);
            }
            return wndList;
        }

        private static bool IsShowInTaskBar(IntPtr hWnd)
        {
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            bool showInTaskbar = (exStyle & WS_EX_APPWINDOW) != 0 ||
                                 (exStyle & WS_EX_TOOLWINDOW) == 0;
            return showInTaskbar;
        }

    }
}
