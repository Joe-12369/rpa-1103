using AutoIt;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA2;
using FlaUI.UIA3;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Commons.WindowMatch.WindowCache;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace II.RPA.Plugin.GUI.Commons
{
    public static class UiCommon
    {
        #region UIAutomation 相关
        public static UIAModeType UIAMode { get; set; }
        public static bool IsAutoSeekDepth { get; set; }

        private static ITreeWalker _treeWalker_uia2;
        private static ITreeWalker _treeWalker_uia3;

        private static AutomationBase uia3Automation = null;
        private static AutomationBase uia2Automation = null;
        private static ILogger logger = Logger.OfContext();
        public static IDictionary<string, string> AiBuriedDataCacheDic { get; set; } = new Dictionary<string, string>();
        //internal AutomationElement automationElement;
        public static AutomationBase UIAAutomation
        {
            get
            {
                //if (uia2Automation == null)
                //{
                //    uia2Automation = new UIA2Automation();
                //}
                //return uia2Automation;  //Win7 Windows下拉框问题 UIA2能正常执行 UIA3失败 必现（GetClickablePoint()）
                //此处根据需求来确定用UIA2还是UIA3
                if (UIAMode == UIAModeType.UIA2)
                {
                    if (uia2Automation == null)
                    {
                        try
                        {
                            uia2Automation = new UIA2Automation();
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"uia2Automation 初始化异常，此电脑不支持，使用UIA3Automation");
                            uia2Automation = CreateUIA3Automation();
                        }
                    }
                    return uia2Automation;
                }
                else
                {
                    if (uia3Automation == null)
                    {
                        uia3Automation = CreateUIA3Automation();
                    }
                    return uia3Automation;
                }
            }
        }
        private static UIA3Automation CreateUIA3Automation()
        {
            var automation = new UIA3Automation();
            try
            {
                // Configure once before publishing the shared instance; never change per request.
                automation.ConnectionTimeout = TimeSpan.FromSeconds(2);
                automation.TransactionTimeout = TimeSpan.FromSeconds(5);
                logger.LogInformation($"UIA3 provider timeouts: connection={automation.ConnectionTimeout.TotalMilliseconds}ms, transaction={automation.TransactionTimeout.TotalMilliseconds}ms");
            }
            catch (Exception ex) when (ex is InvalidCastException || ex is COMException || ex is NotSupportedException)
            {
                // Older systems may not expose IUIAutomation2. Keep their existing behavior.
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "UIA3 provider timeout configuration unavailable; retaining supported native settings");
            }
            return automation;
        }

        public static ITreeWalker TreeWalker
        {
            get
            {
                if (UIAMode == UIAModeType.UIA2)
                {
                    if (_treeWalker_uia2 == null)
                    {
                        _treeWalker_uia2 = UIAAutomation.TreeWalkerFactory.GetControlViewWalker();
                        //_treeWalker.GetFirstChild(uia3Automation.GetDesktop());
                    }
                    return _treeWalker_uia2;
                }
                else
                {
                    if (_treeWalker_uia3 == null)
                    {
                        _treeWalker_uia3 = UIAAutomation.TreeWalkerFactory.GetControlViewWalker();
                        //_treeWalker.GetFirstChild(uia3Automation.GetDesktop());
                    }
                    return _treeWalker_uia3;
                }
            }
        }
        public static ITreeWalker RawViewWalker
        {
            get
            {
                if (UIAMode == UIAModeType.UIA2)
                {
                    if (_treeWalker_uia2 == null)
                    {
                        _treeWalker_uia2 = UIAAutomation.TreeWalkerFactory.GetRawViewWalker();
                    }
                    return _treeWalker_uia2;
                }
                else
                {
                    if (_treeWalker_uia3 == null)
                    {
                        _treeWalker_uia3 = UIAAutomation.TreeWalkerFactory.GetRawViewWalker();
                    }
                    return _treeWalker_uia3;
                }
            }
        }
        public volatile static AutomationElement _DesktopElement = null;
        public static AutomationElement DesktopElement
        {
            get
            {
                //if (_DesktopElement == null)
                {
                    _DesktopElement = UIAAutomation.GetDesktop();
                }
                return _DesktopElement;
            }
            set
            {
                _DesktopElement = value;
            }
        }
        #endregion

        #region 公共方法
        ///// <summary>
        ///// 设置UIAMode
        ///// </summary>
        ///// <param name="mode2Apps">mode2Apps</param>
        ///// <param name="processName">进程名</param>
        ///// <returns>默认UIA3</returns>
        public static bool SetUIAMode(List<string> mode2Apps, string processName)
        {
            try
            {
                if (mode2Apps.Contains(processName))
                {
                    UiCommon.UIAMode = UIAModeType.UIA2;
                }
                else
                {
                    UiCommon.UIAMode = UIAModeType.UIA3;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        ///// <summary>
        ///// 设置自动使用深度拾取的程序
        ///// </summary>
        ///// <param name="mode2Apps">mode2Apps</param>
        ///// <param name="processName">进程名</param>
        ///// <returns>默认UIA3</returns>
        public static bool SetAutoSeekDepth(List<string> autoSeekDepthApps, string processName)
        {
            try
            {
                if (autoSeekDepthApps.Any(n => processName.Equals(n, StringComparison.OrdinalIgnoreCase)))
                {
                    IsAutoSeekDepth = true;
                }
                else
                {
                    IsAutoSeekDepth = false;

                }
                //logger.LogDebug($"进程：{processName}，深度匹配：{IsAutoSeekDepth}");
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static ProcessInfo GetProcessInfo(this AutomationElement element)
        {
            if (!element.Properties.ProcessId.IsSupported) return null;
            ProcessInfo result = new ProcessInfo();
            int processId = -1;
            IntPtr handle = IntPtr.Zero;
            try
            {
                processId = element.Properties.ProcessId.Value;
                var p = System.Diagnostics.Process.GetProcessById(processId);
                handle = p.Handle;
                result.ProcessId = p.Id;
                result.ProcessName = p.ProcessName;
                result.Filename = p.MainModule.FileName.ReplaceEnvironmentVariable();
                p.Dispose();
            }
            catch (Exception)
            {
            }

            bool _isImmersiveProcess = false;
            try
            {
                if (handle != IntPtr.Zero) _isImmersiveProcess = NativeMethods.IsImmersiveProcess(handle);
            }
            catch (Exception)
            {
            }
            string ApplicationUserModelId = null;

            if (_isImmersiveProcess)
            {
                var automation = UiCommon.UIAAutomation;
                var pc = new PropertyCondition(automation.PropertyLibrary.Element.ClassName, "Windows.UI.Core.CoreWindow");
                AutomationElement _el = null;
                if (CacheRequest.IsCachingActive)
                {
                    _el = element.CachedChildren?.FirstOrDefault();
                }
                else
                {
                    _el = element.FindFirstChild(pc);
                }
                if (_el != null)
                {
                    processId = _el.Properties.ProcessId.Value;

                    IntPtr ptrProcess = OpenProcess(QueryLimitedInformation, false, processId);
                    if (IntPtr.Zero != ptrProcess)
                    {
                        uint cchLen = 130; // Currently APPLICATION_USER_MODEL_ID_MAX_LENGTH = 130
                        StringBuilder sbName = new StringBuilder((int)cchLen);
                        Int32 lResult = GetApplicationUserModelId(ptrProcess, ref cchLen, sbName);
                        if (APPMODEL_ERROR_NO_APPLICATION == lResult)
                        {
                            _isImmersiveProcess = false;
                        }
                        else if (ERROR_SUCCESS == lResult)
                        {
                            ApplicationUserModelId = sbName.ToString();
                        }
                        else if (ERROR_INSUFFICIENT_BUFFER == lResult)
                        {
                            sbName = new StringBuilder((int)cchLen);
                            if (ERROR_SUCCESS == GetApplicationUserModelId(ptrProcess, ref cchLen, sbName))
                            {
                                ApplicationUserModelId = sbName.ToString();
                            }
                        }
                        CloseHandle(ptrProcess);
                    }
                }
                else { _isImmersiveProcess = false; }


            }

            try
            {
                var arguments = GetCommandLine(processId);
                var arr = ParseCommandLine(arguments);

                if (arr.Length == 0)
                {

                }
                else if (arguments.Contains("\"" + arr[0] + "\""))
                {
                    result.Arguments = arguments.Replace("\"" + arr[0] + "\"", "");
                }
                else
                {
                    result.Arguments = arguments.Replace(arr[0], "");
                }
                if (result.Arguments != null) { result.Arguments = result.Arguments.ReplaceEnvironmentVariable(); }

            }
            catch (Exception)
            {
            }
            result.ApplicationUserModelId = ApplicationUserModelId;
            result.IsImmersiveProcess = _isImmersiveProcess;
            return result;
        }

        public static string GetCommandLine(int processId)
        {
            string result = null;
            try
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + processId))
                        using (ManagementObjectCollection objects = searcher.Get())
                        {
                            result = objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex);
                    }
                });
                thread.Start();
                thread.Join();

            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return null;
            }
            return result;
        }

        public const int QueryLimitedInformation = 0x1000;
        public const int ERROR_INSUFFICIENT_BUFFER = 0x7a;
        public const int ERROR_SUCCESS = 0x0;
        public const int APPMODEL_ERROR_NO_APPLICATION = 15703;
        public static string[] ParseCommandLine(string commandLine)
        {
            List<string> arguments = new List<string>();
            bool stringIsQuoted = false;
            string argString = "";
            if (commandLine != null)
            {
                for (int c = 0; c < commandLine.Length; c++)  //process string one character at a tie
                {
                    if (commandLine.Substring(c, 1) == "\"")
                    {
                        if (stringIsQuoted)  //end quote so populate next element of list with constructed argument
                        {
                            arguments.Add(argString);
                            argString = "";
                        }
                        else
                        {
                            stringIsQuoted = true; //beginning quote so flag and scip
                        }
                    }
                    else if (commandLine.Substring(c, 1) == "".PadRight(1))
                    {
                        if (stringIsQuoted)
                        {
                            argString += commandLine.Substring(c, 1); //blank is embedded in quotes, so preserve it
                        }
                        else if (argString.Length > 0)
                        {
                            arguments.Add(argString);  //non-quoted blank so add to list if the first consecutive blank
                        }
                    }
                    else
                    {
                        argString += commandLine.Substring(c, 1);  //non-blan character:  add it to the element being constructed
                    }
                }
            }
            return arguments.ToArray();
        }
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hHandle);
        [DllImport("kernel32.dll")]
        public static extern int GetApplicationUserModelId(
            IntPtr hProcess,
            ref uint AppModelIDLength,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sbAppUserModelID);
        public const int KEYEVENTF_KEYUP = 2;
        [DllImport("user32.dll", EntryPoint = "keybd_event", SetLastError = true)]
        public static extern void keybd_event(Keys bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        #endregion
        public const string CONTROL_TYPE = "ControlType";
        public const string NAME = "Name";
        public const string CLASS_NAME = "ClassName";
        public const string AUTOMATION_ID = "AutomationId";
        public const string FRAMEWORK_ID = "FrameworkId";
        public const string INDEX_IN_PARENT = "IndexInParent";
        public const string INDEX = "Index";

        public const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;

        public static class ShowWindowTypes
        {
            public const int SW_HIDE = 0;
            public const int SW_SHOWNORMAL = 1;
            public const int SW_NORMAL = 1;
            public const int SW_SHOWMINIMIZED = 2;
            public const int SW_SHOWMAXIMIZED = 3;
            public const int SW_MAXIMIZE = 3;
            public const int SW_SHOWNOACTIVATE = 4;
            public const int SW_SHOW = 5;
            public const int SW_MINIMIZE = 6;
            public const int SW_SHOWMINNOACTIVE = 7;
            public const int SW_SHOWNA = 8;
            public const int SW_RESTORE = 9;
            public const int SW_SHOWDEFAULT = 10;
            public const int SW_FORCEMINIMIZE = 11;
            public const int SW_MAX = 11;
        }



        public static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(
            IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public Point ptMinPosition;
            public Point ptMaxPosition;
            public Rectangle rcNormalPosition;
        }

        public enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }
        [DllImport("user32")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern IntPtr GetFocus();

        [ComImport]
        [Guid("00000114-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleWindow
        {
            void GetWindow(out IntPtr phwnd);
            void ContextSensitiveHelp([In, MarshalAs(UnmanagedType.Bool)] bool fEnterMode);
        }
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);


        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("psapi.dll")]
        static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In][MarshalAs(UnmanagedType.U4)] int nSize);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.SysInt)]
        public static extern IntPtr WindowFromPoint(System.Drawing.Point Point);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex,
            int dwNewLong);


        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);


        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("User32.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern IntPtr LoadCursorFromFile(String str);
        //判断窗口是否存在
        [DllImport("user32", EntryPoint = "IsWindow")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowEnabled(IntPtr hWnd);

        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size > 4)
            {
                return GetWindowLongPtr64(hWnd, nIndex);
            }
            return new IntPtr(GetWindowLongPtr32(hWnd, nIndex));
        }
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndlnsertAfter, int X, int Y, int cx, int cy, uint Flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern uint GetWindowLongPtr32(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "GetWindowTextLength", SetLastError = true)]
        internal static extern int GetWindowTextLength(IntPtr hwnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);
        [DllImport("User32", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        /// <summary>
        /// 从内嵌的资源中加载光标（通过释放临时文件进行加载）
        /// </summary>
        /// <param name="cursorResource"></param>
        /// <returns></returns>
        public static Cursor GetCursor(byte[] cursorResource)
        {
            try
            {
                var tmpPath = System.IO.Path.GetTempPath();
                var guid = Guid.NewGuid().ToString("N");

                if (tmpPath.Substring(tmpPath.Length - 1, 1) != @"\")
                {
                    tmpPath = tmpPath + @"\";
                }

                var tempFile = tmpPath + guid + @".cur";

                File.WriteAllBytes(tempFile, cursorResource);

                var cursor = new Cursor(LoadCursorFromFile(tempFile));
                File.Delete(tempFile);
                return cursor;
            }
            catch (Exception)
            {

            }

            return Cursors.Default;

        }

        private static bool IsNeedRestoreWindow(IntPtr hWnd)
        {
            var state = GetPlacement(hWnd).showCmd;
            if (state == ShowWindowCommands.Hide || state == ShowWindowCommands.Minimized)
            {
                return true;
            }

            return false;
        }
        private static string GetWindowTitle(IntPtr handle)
        {
            int length = GetWindowTextLength(handle);
            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(handle, sb, sb.Capacity);
            logger.LogDebug("handle: " + handle.ToString() + "Title: " + sb.ToString());
            return sb.ToString();
        }
        /// <summary>
        /// 与ForceShow 功能类似，区别在于 激活动作使用AutoItX
        /// </summary>
        /// <param name="hWnd"></param>
        public static void ActiveWindow(IntPtr hWnd)
        {


            IntPtr curForegroundHWnd = GetForegroundWindow();
            if (curForegroundHWnd == hWnd)
            {
                return;
            }
            uint processId1, processId2;
            GetWindowThreadProcessId(curForegroundHWnd, out processId1);
            GetWindowThreadProcessId(hWnd, out processId2);
            if (processId1 == processId2)
            {
                return;
            }
            var hParentWnd = curForegroundHWnd;
            while (hParentWnd != IntPtr.Zero)
            {
                hParentWnd = GetParent(hParentWnd);
                if (hParentWnd == hWnd)
                {
                    return;
                }
            }
            RPAUtils.Retry(delegate
            {
                if (AutoItX.WinActive(hWnd) != 0)
                {
                    return true;
                }
                AutoItX.WinActivate(hWnd);
                return false;
            }, 2000);

            //uint dwMyID = GetCurrentThreadId();
            //uint dwProcessId = default(uint);
            //uint dwCurID = GetWindowThreadProcessId(curForegroundHWnd, out dwProcessId);
            //AttachThreadInput(dwCurID, dwMyID, true);
            //SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0, 3u);
            //SetWindowPos(hWnd, new IntPtr(-2), 0, 0, 0, 0, 3u);
            //SetForegroundWindow(hWnd);
            //AttachThreadInput(dwCurID, dwMyID, false);
            //NativeMethods.SetFocus(hWnd);
            //SetActiveWindow(hWnd);

        }
        public static void ForceShow(IntPtr hWnd)
        {
            ForceShow(hWnd, true);
        }
        public static void ForceShow(IntPtr hWnd, bool useSetWindowPos)
        {
            //GetWindowTextLength(hWnd);
            IntPtr CurForegroundHWnd = GetForegroundWindow();
            //GetWindowTitle(CurForegroundHWnd);
            if (CurForegroundHWnd == hWnd)
            {
                return;
            }
            //https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getparent
            var handle = CurForegroundHWnd;
            List<IntPtr> handles = new List<IntPtr>();
            handles.Add(handle);
            while (handle != IntPtr.Zero)
            {
                handle = GetParent(handle);
                //GetWindowTitle(handle);
                handles.Add(handle);
            }

            if (handles.Contains(hWnd))
            {
                return;
            }
            uint processId1, processId2;
            GetWindowThreadProcessId(CurForegroundHWnd, out processId1);
            GetWindowThreadProcessId(hWnd, out processId2);
            if (processId1 == processId2)
            {
                //bool currentWndEnable = IsWindowEnabled(CurForegroundHWnd);
                //bool hWndEnable = IsWindowEnabled(hWnd);
                //NLogger.Default.Info("CurrentWndEnable:" + currentWndEnable + " hWndEnable:" + hWndEnable);
                //if (currentWndEnable == true && hWndEnable == false)
                {
                    return;
                }
            }

            if (IsNeedRestoreWindow(hWnd))
            {
                //已经是最大化时调用SW_RESTORE会导致窗口还原，所以此处需要判断下
                ShowWindow(hWnd, ShowWindowTypes.SW_RESTORE);
                try
                {
                    //if (JavaNode.accessBridge.Functions.IsJavaWindow(hWnd))
                    //{
                    //    InvalidateRect(hWnd, IntPtr.Zero, true);//java窗口需要重刷下，不然黑屏
                    //}
                }
                catch { }
            }
            SetForegroundWindow(hWnd);
            if (useSetWindowPos)
                SecondForceShow(hWnd);//这里为了解决前置无效的情况，同时为了保持原有的程序不被干扰，在最后加上一个兼容
            RPAUtils.Retry(() =>
            {
                CurForegroundHWnd = GetForegroundWindow();
                logger.LogDebug($"waiting window foreground .{CurForegroundHWnd},{hWnd}");
                return CurForegroundHWnd == hWnd;
            }, 500, 10);
        }

        public static void SecondForceShow(IntPtr hWnd)
        {
            try
            {

                IntPtr CurForegroundHWnd = GetForegroundWindow();
                if (CurForegroundHWnd != hWnd)
                {
                    IntPtr hWndlnsertAfter = new IntPtr(-1);
                    IntPtr hWndlnsertAfter2 = new IntPtr(-2);
                    long num = 8L;
                    long num2 = (long)GetWindowLongPtr(hWnd, -20);
                    SetWindowPos(hWnd, hWndlnsertAfter, 0, 0, 0, 0, 3u);
                    if ((num2 & num) != num)
                    {
                        SetWindowPos(hWnd, hWndlnsertAfter2, 0, 0, 0, 0, 3u);
                    }
                }
            }
            catch
            {
            }
        }
        #region 进程信息 相关
        /// <summary>
        /// 获取缓存中的进程名信息，若无 做补全
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns>不包含.exe</returns>
        public static string GetProcessNameByCache(IntPtr hWnd)
        {
            return DesktopWindowCache.Shared.GetOrAddProcessName(hWnd);
        }
        /// <summary>
        /// 根据窗口句柄获取程序路径（优化版本，带缓存）
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>程序路径，如果获取失败返回null或空字符串</returns>
        public static string GetProgramAddressByCache(IntPtr windowHandle)
        {
            return DesktopWindowCache.Shared.GetOrAddProgramPath(windowHandle);
        }

        public static string GetProductNameByCache(IntPtr hWnd)
        {
            return DesktopWindowCache.Shared.GetOrAddProductName(hWnd);
        }
        public static string GetProcessName(IntPtr wndH)
        {

            UiCommon.GetWindowThreadProcessId(wndH, out var processId);
            string wmiQueryString = "SELECT ProcessId, Name FROM Win32_Process WHERE ProcessId = " + processId;
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            {
                using (var results = searcher.Get())
                {
                    ManagementObject mo = results.Cast<ManagementObject>().FirstOrDefault();
                    if (mo != null)
                    {
                        var pName = (string)mo["Name"];
                        return pName;
                    }
                }
            }
            return string.Empty;
        }
        public static string GetProcessNameById(int processId)
        {
            IntPtr intPtr = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.QueryLimitedInformation, bInheritHandle: false, processId);
            if (intPtr != IntPtr.Zero)
            {
                StringBuilder stringBuilder = new StringBuilder(1024);
                uint nameLen = (uint)stringBuilder.Capacity;
                if (NativeMethods.QueryFullProcessImageName(intPtr, 0u, stringBuilder, ref nameLen))
                {
                    NativeMethods.CloseHandle(intPtr);
                    return Path.GetFileNameWithoutExtension(stringBuilder.ToString());
                }
                NativeMethods.CloseHandle(intPtr);
            }
            try
            {
                foreach (ManagementObject item in new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID = " + processId).Get())
                {
                    if (item["Name"] == null)
                    {
                        continue;
                    }
                    try
                    {
                        string text = item["Name"].ToString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            return text.Substring(0, text.LastIndexOf('.'));
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception)
            {
            }
            return Process.GetProcessById(processId).ProcessName;
        }

        public static Dictionary<int, string> GetAllProcessIDAndName()
        {
            Dictionary<int, string> procDic = new Dictionary<int, string>();
            //UiCommon.GetWindowThreadProcessId(wndH, out var processId);
            string wmiQueryString = "SELECT ProcessId, Name FROM Win32_Process ";
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            {
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject mo in results)
                    {
                        procDic.Add(mo["ProcessId"].ToString().Trim().As<int>(), (string)mo["Name"]);
                    }
                    //ManagementObject mo = results.Cast<ManagementObject>().FirstOrDefault();
                    //if (mo != null)
                    //{
                    //    var pName = (string)mo["Name"];
                    //    return pName;
                    //}
                }
            }
            return procDic;
        }
        public static List<int> GetProcessIdListFromWin32Process(string processName, out List<int> allProcessIdList)
        {
            List<int> pidLis = new List<int>();
            allProcessIdList = new List<int>();
            try
            {
                string wmiQueryString = "SELECT ProcessId, Name FROM Win32_Process ";
                using (var searcher = new ManagementObjectSearcher(wmiQueryString))
                {
                    using (var results = searcher.Get())
                    {
                        foreach (ManagementObject mo in results)
                        {
                            if (mo["Name"] == null || mo["ProcessId"] == null)
                            {
                                continue;
                            }
                            var a = mo.Properties;
                            string pName = (string)mo["Name"];
                            if (string.Equals(processName, pName, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(processName, pName.Replace(".exe", ""), StringComparison.OrdinalIgnoreCase))
                            {
                                pidLis.Add(mo["ProcessId"].ToString().Trim().As<int>());
                            }
                            allProcessIdList.Add(mo["ProcessId"].ToString().Trim().As<int>());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"获取所有进程信息异常");
            }
            return pidLis;
        }
        public static string GetProcessName(int pid)
        {
            return Path.GetFileName(GetProcessFullPath(pid));
            //var p=    System.Diagnostics.Process.GetProcessById(pid);

            // return p.ProcessName;
        }

        public static List<int> GetProcessIdList(string processName, out List<int> allProcessIdList)
        {
            List<int> pidLis = new List<int>();
            allProcessIdList = new List<int>();
            try
            {
                Process[] processes = Process.GetProcesses();
                for (int i = 0; i < processes.Length; i++)
                {
                    if (string.Equals(processName, processes[i].ProcessName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(processName.Replace(".exe", ""), processes[i].ProcessName.Replace(".exe", ""), StringComparison.OrdinalIgnoreCase))
                    {
                        pidLis.Add(processes[i].Id);
                    }
                    allProcessIdList.Add(processes[i].Id);
                    processes[i].Dispose();
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"获取所有进程信息异常");
            }
            return pidLis;
        }
        public static string GetProcessFullPath(int pid)
        {

            var p = Process.GetProcessById(pid);
            string fileName = p.MainModule.FileName;
            p.Dispose();
            return fileName;
        }



        #endregion

        public static void EnablePassThrough(Form form)
        {
            int style = GetWindowLong(
                form.Handle, GWL_EXSTYLE);
            SetWindowLong(
             form.Handle, GWL_EXSTYLE,
                style | WS_EX_TRANSPARENT);
        }

        public static void DisablePassThrough(Form form)
        {
            int style = GetWindowLong(
                form.Handle, GWL_EXSTYLE);
            SetWindowLong(
            form.Handle, GWL_EXSTYLE,
               style & ~WS_EX_TRANSPARENT);
        }
        /// <summary>
        /// 获取当前鼠标点对应的根窗口
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static IntPtr GetRootWindow(Point point)
        {
            var list = GetRootWindowList(point);
            return list.FirstOrDefault();
        }
        /// <summary>
        /// 获取当前鼠标点对应的根窗口链 根窗口在前(默认为目标窗体) 最小的元素窗口在尾
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static List<IntPtr> GetRootWindowList(Point point)
        {
            List<IntPtr> parentLinked = new List<IntPtr>();
            IntPtr hWnd = WindowFromPoint(point);
            if (hWnd == IntPtr.Zero)
            {
                return parentLinked;
            }
            parentLinked.Add(hWnd);
            IntPtr hParentWnd = GetParent(hWnd);
            while (hParentWnd != IntPtr.Zero)
            {
                hWnd = hParentWnd;
                parentLinked.Add(hWnd);
                hParentWnd = GetParent(hParentWnd);
            }
            parentLinked.Reverse();//根窗口在前 最小的元素窗口在尾
            return parentLinked;
        }
        public static List<IntPtr> GetRootWindowListNew(Point point)
        {
            List<IntPtr> parentLinked = new List<IntPtr>();
            IntPtr hWnd = WindowFromPointNew(point);
            if (hWnd == IntPtr.Zero)
            {
                return parentLinked;
            }
            parentLinked.Add(hWnd);
            IntPtr hParentWnd = GetParent(hWnd);
            while (hParentWnd != IntPtr.Zero)
            {
                hWnd = hParentWnd;
                parentLinked.Add(hWnd);
                hParentWnd = GetParent(hParentWnd);
            }
            parentLinked.Reverse();//根窗口在前 最小的元素窗口在尾
            return parentLinked;
        }
        public static IntPtr WindowFromPointNew(Point point)
        {
            var hWnd = NativeMethods.WindowFromPoint(point);
            bool breakb = false;
            while (hWnd != IntPtr.Zero)
            {
                if ((NativeMethods.IsWindow(hWnd))
                    && (NativeMethods.IsWindowVisible(hWnd))
                    && ((NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE) & NativeMethods.WS_EX_TOOLWINDOW) == 0)
                )
                {
                    NativeMethods.GetWindowRect(hWnd, out var windowRect);
                    if (point.X >= windowRect.Left &&
                       point.X <= windowRect.Right &&
                       point.Y >= windowRect.Top &&
                       point.Y <= windowRect.Bottom)
                    {
                        UiCommon.GetWindowThreadProcessId(hWnd, out uint id);
                        if (id != GlobleConfigurations.CurrentProcessId)
                        {
                            breakb = true;
                            break;
                        }
                    }
                }
                hWnd = NativeMethods.GetWindow(NativeMethods.GetAncestor(hWnd, GetAncestorFlags.GetRoot), (uint)GET_WINDOW_CMD.GW_HWNDNEXT);
            }
            if (breakb)
            {
                WindowObject windowObject = new WindowObject(hWnd);
                var wndObjs = windowObject.FindAllChildWindowEx(wnd => wnd.WindowRect.Contains(point), 99);
                var wndobj = wndObjs.OrderBy(wnd => wnd.WindowRect.Height * wnd.WindowRect.Width).FirstOrDefault();
                if (wndobj != null)
                {
                    hWnd = wndobj.Handle;
                }
            }
            return hWnd;
        }
        public static List<IntPtr> GetAncestorWindowList(IntPtr hWnd)
        {
            List<IntPtr> parentLinked = new List<IntPtr>();
            IntPtr hParentWnd = hWnd;
            for (var h = hWnd; h != IntPtr.Zero; h = NativeMethods.GetAncestor(h, GetAncestorFlags.GetParent))
            {
                if (!IsDesktopWindow(h)) parentLinked.Add(h);
            }
            parentLinked.Reverse();//根窗口在前 最小的元素窗口在尾
            return parentLinked;
        }
        static bool IsDesktopWindow(IntPtr hwnd)
        {
            return GetClassName(hwnd) == "#32769";
        }
        //public static void RunInUI(Action func)
        //{
        //    DispatcherHelper.CheckBeginInvokeOnUI(() =>
        //    {
        //        func();
        //    });
        //}

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        public delegate bool EnumWindowsProc(IntPtr windowHandle, IntPtr lParam);
        [DllImport("user32")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr hWndStart, EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

        public static string GetWindowText(IntPtr ptr)
        {
            var textLen = NativeMethods.GetWindowTextLength(ptr) * 2 + 1;
            StringBuilder textSB = new StringBuilder(textLen);
            NativeMethods.GetWindowText(ptr, textSB, textLen);
            return textSB.ToString();
        }
        public static string GetClassName(IntPtr handle, int MaxCount = 256)
        {
            StringBuilder stringBuilder = new StringBuilder(MaxCount);
            NativeMethods.GetClassName(handle, stringBuilder, stringBuilder.Capacity);
            return stringBuilder.ToString();
        }
        public static string GetClassName(IntPtr hWnd)
        {
            StringBuilder sb2 = new StringBuilder(2 * 1024);
            NativeMethods.GetClassName(hWnd, sb2, sb2.Capacity);
            return sb2.ToString();
        }
        public enum BinaryType : uint
        {
            SCS_32BIT_BINARY = 0, // A 32-bit Windows-based application
            SCS_64BIT_BINARY = 6, // A 64-bit Windows-based application.
            SCS_DOS_BINARY = 1,   // An MS-DOS – based application
            SCS_OS216_BINARY = 5, // A 16-bit OS/2-based application
            SCS_PIF_BINARY = 3,   // A PIF file that executes an MS-DOS – based application
            SCS_POSIX_BINARY = 4, // A POSIX – based application
            SCS_WOW_BINARY = 2    // A 16-bit Windows-based application 
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetBinaryType(
            string lpApplicationName,
            out BinaryType dwBinType
            );


        /// <summary>
        /// 判断EXE是否为64位程序
        /// </summary>
        /// <param name="exePath"></param>
        /// <returns></returns>
        public static bool IsExe64Bit(string exePath)
        {
            BinaryType exeType = BinaryType.SCS_32BIT_BINARY;
            if (GetBinaryType(exePath, out exeType))
            {
                if (exeType == BinaryType.SCS_64BIT_BINARY)
                {
                    return true;
                }
            }

            return false;
        }

        #region 处理进程 相关
        public static Process RunProcess(string fileName, string arguments, bool isHidden = false)
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            if (isHidden)
            {
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.CreateNoWindow = true;
            }

            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.FileName = fileName;
            psi.Arguments = arguments;

            var p = Process.Start(psi);

            return p;
        }

        public static void KillSameProcess(string processName, bool isKillselfe = false)
        {
            Process[] processesByName = GetProcessesByName(processName, onlyCurrentUser: true);
            int curPid = Process.GetCurrentProcess().Id;
            Process[] array = processesByName;
            foreach (Process process in array)
            {
                if (process.Id != curPid || (process.Id == curPid && isKillselfe))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }
                }
            }
        }
        public static string GetProcessOwner(int processId)
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
        public static Process[] GetProcessesByName(string processName, bool onlyCurrentUser)
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
        public static Process[] GetProcessesBySessionId(string processName)
        {
            try
            {
                int sid = Process.GetCurrentProcess().SessionId;
                return (from m in Process.GetProcessesByName(processName)
                        where m.SessionId == sid
                        select m).ToArray();
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
            return null;
        }
        #endregion
        public static void CopyFileToDir(string srcPath, string dstDir, bool isOverwrite = false)
        {
            try
            {
                string dstPath = "";
                if (dstDir.Substring(dstDir.Length - 1, 1) == @"\")
                {
                    dstPath = dstDir + Path.GetFileName(srcPath);
                }
                else
                {
                    dstPath = dstDir + @"\" + Path.GetFileName(srcPath);
                }

                System.IO.File.Copy(srcPath, dstDir, isOverwrite);
            }
            catch (Exception)
            {

            }
        }
        #region 根据selectorItem 前置窗口
        /// <summary>
        /// 获取不带通配符的匹配条件
        /// </summary>
        /// <returns></returns>
        internal static AndCondition GetConditionsWithoutStar(SelectorItem si)
        {
            var automation = UiCommon.UIAAutomation;
            var cond = new List<ConditionBase>();
            foreach (var p in si.Properties.Where(x => x.Enabled == true && (x.Value != null && !x.Value.Contains("*"))))
            {
                if (p.Name == CONTROL_TYPE)
                {
                    ControlType ct = (ControlType)Enum.Parse(typeof(ControlType), p.Value);
                    cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ControlType, ct));
                }
                if (p.Name == NAME)
                {
                    cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.Name, p.Value));
                }
                if (p.Name == CLASS_NAME)
                {
                    cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ClassName, p.Value));
                }
                if (p.Name == AUTOMATION_ID)
                {
                    cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.AutomationId, p.Value));
                }
            }
            return new AndCondition(cond);
        }
        public static AutomationElement SetForeground(SelectorItem si)
        {
            AutomationElement topElement = null;
            var conditions = GetConditionsWithoutStar(si);    //因为自带的通过Condition查找的方式不支持通配符，所以需要自己后面进行特殊处理
            AutomationElement root = UiCommon.UIAAutomation.GetDesktop();
            topElement = root.FindFirst(TreeScope.Children, conditions);
            if (topElement != null)
            {
                if (topElement.Properties.NativeWindowHandle.IsSupported)
                {
                    UiCommon.ForceShow(topElement.Properties.NativeWindowHandle);
                }
            }
            return topElement;
        }

        /// <summary>
        /// 根据父元素及子节点属性信息查找子节点元素 add by mty
        /// </summary>
        /// <param name="parentroot"></param>
        /// <param name="si"></param>
        /// <returns></returns>
        public static AutomationElement SetForegroundByParent(AutomationElement parentroot, SelectorItem si)
        {
            return SetForegroundByParent(parentroot, si, true);
        }
        public static AutomationElement SetForegroundByParent(AutomationElement parentroot, SelectorItem si, bool isNeedSetForeground)
        {
            AutomationElement topElement = null;
            var conditions = GetConditionsWithoutStar(si);    //因为自带的通过Condition查找的方式不支持通配符，所以需要自己后面进行特殊处理
            AutomationElement root = parentroot;
            topElement = root.FindFirst(TreeScope.Children, conditions);
            if (topElement != null && isNeedSetForeground)
            {
                if (topElement.Properties.NativeWindowHandle.IsSupported)
                {
                    UiCommon.ForceShow(topElement.Properties.NativeWindowHandle);
                }
            }
            return topElement;
        }
        #endregion

        #region 获取桌面区域截图

        /// <summary>
        /// 拾取桌面区域截图
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Bitmap CaptureDesktopByRectangle(Rectangle r)
        {
            Bitmap bitmap = null;
            try
            {
                using (var img = UiCommon.UIAAutomation.GetDesktop().Capture())
                {
                    bitmap = new Bitmap(r.Width, r.Height);
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.DrawImage(img, new Rectangle(0, 0, bitmap.Width, bitmap.Height), r, GraphicsUnit.Pixel);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return bitmap;
        }
        public static Bitmap GetDesktop()
        {
            Bitmap bitmap = null;
            try
            {
                //float ScaleX = NativeMethods.ScaleX;
                //Size sz = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
                //sz.Width = (int)(sz.Width * ScaleX);
                //sz.Height = (int)(sz.Height * ScaleX);
                Size sz = NativeMethods.DESKTOP;
                IntPtr hDesk = NativeMethods.GetDesktopWindow();
                IntPtr hSrce = NativeMethods.GetWindowDC(hDesk);
                IntPtr hDest = NativeMethods.CreateCompatibleDC(hSrce);
                IntPtr hBmp = NativeMethods.CreateCompatibleBitmap(hSrce, sz.Width, sz.Height);
                IntPtr hOldBmp = NativeMethods.SelectObject(hDest, hBmp);
                bool b = NativeMethods.BitBlt(hDest, 0, 0, sz.Width, sz.Height, hSrce, 0, 0, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

                bitmap = Bitmap.FromHbitmap(hBmp);

                NativeMethods.SelectObject(hDest, hOldBmp);
                NativeMethods.DeleteObject(hBmp);
                NativeMethods.DeleteDC(hDest);
                NativeMethods.ReleaseDC(hDesk, hSrce);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return bitmap;
        }


        public static Bitmap GetDesktopImage(int X, int Y, int width, int height)
        {
            Bitmap printscreen = null;
            IntPtr hWnd = IntPtr.Zero;
            IntPtr hDC = IntPtr.Zero;
            IntPtr hMemDC = IntPtr.Zero;
            IntPtr hOld = IntPtr.Zero;
            IntPtr m_HBitmap = IntPtr.Zero;
            try
            {
                hWnd = NativeMethods.GetDesktopWindow();
                hDC = NativeMethods.GetWindowDC(hWnd);
                hMemDC = NativeMethods.CreateCompatibleDC(hDC);
                m_HBitmap = NativeMethods.CreateCompatibleBitmap(hDC, width, height);
                hOld = NativeMethods.SelectObject(hMemDC, m_HBitmap);
                NativeMethods.BitBlt(hMemDC, 0, 0, width, height, hDC, X, Y, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
                printscreen = Image.FromHbitmap((IntPtr)m_HBitmap);
            }
            catch (Exception ex)
            {
                NLogger.Default.Error(ex);
            }
            finally
            {
                NativeMethods.SelectObject(hMemDC, hOld);
                NativeMethods.DeleteObject(m_HBitmap);
                NativeMethods.DeleteDC(hMemDC);
                NativeMethods.ReleaseDC(hWnd, hDC);
            }
            return printscreen;

            //var sourceImg = new Bitmap(width, height);
            //using (var graphics = Graphics.FromImage(sourceImg))
            //{
            //    graphics.CopyFromScreen(X, Y, 0, 0, sourceImg.Size, CopyPixelOperation.SourceCopy);
            //}
            //return sourceImg;
            //string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempimg");
            //if (!Directory.Exists(path))
            //{
            //    Directory.CreateDirectory(path);
            //}
            //string name = DateTime.Now.ToString("yyyyMMddHHmmss");
            //printscreen.Save(Path.Combine(path, $@"{name}_printscreen.png"));
            //sourceImg.Save(Path.Combine(path, $@"{name}_CopyFromScreen.png"));
            //return printscreen;
        }

        public static Bitmap GetRectBitmapDesktop(Rectangle rect)
        {
            Bitmap bitmap = null;
            try
            {
                NLogger.Default.Trace($"GetRectBitmapDesktop rect is {rect.ToString()}");
                //float sx = PrimaryScreen.DpiX / 96F;
                //float sy = PrimaryScreen.DpiY / 96F;
                float ScaleX = NativeMethods.ScaleX;

                Size sz = NativeMethods.DESKTOP;
                NLogger.Default.Trace($"GetRectBitmapDesktop rect is {sz.ToString()}");

                using (var screenshot = GetDesktopImage(0, 0, sz.Width, sz.Height))
                {
                    if (screenshot != null)
                    {
                        var rect2 = new Rectangle
                        {
                            X = (int)(rect.X * ScaleX),
                            Y = (int)(rect.Y * ScaleX),
                            Width = (int)(rect.Width * ScaleX),
                            Height = (int)(rect.Height * ScaleX)
                        };
                        bitmap = ImageHelper.CaptureImage(screenshot, rect2);
                    }
                    else
                    {
                        NLogger.Default.Trace($"GetRectBitmapDesktop GetDesktopImage is null");
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"获取截图报错，区域：{rect}");
            }
            return bitmap;
        }

        public static Bitmap GetRectBitmapDesktop_Backup(Rectangle rect)
        {

            Bitmap bitmap = null;
            //try
            //{
            //    float sx = PrimaryScreen.DpiX / 96F;
            //    float sy = PrimaryScreen.DpiY / 96F;
            //    //rect = new Rectangle
            //    //{
            //    //    X = (int)(rect.X * sx),
            //    //    Y = (int)(rect.Y * sy),
            //    //    Width = (int)(rect.Width * sx),
            //    //    Height = (int)(rect.Height * sy)
            //    //};
            //    var sourceImg = new Bitmap(rect.Width, rect.Height);
            //    {
            //        using (var graphics = Graphics.FromImage(sourceImg))
            //        {
            //            graphics.CopyFromScreen(rect.X, rect.Y, 0, 0, sourceImg.Size);
            //        }
            //        bitmap = sourceImg;
            //        //bitmap = ImageHelper.Resize(sourceImg, (int)(rect.Width / sx), (int)(rect.Height / sy));
            //    }
            //}
            //catch (Exception ex)
            //{
            //    NLogger.Default.Error(ex);
            //}
            //return bitmap;

            IntPtr hDesk = IntPtr.Zero;
            IntPtr hSrce = IntPtr.Zero;
            IntPtr hDest = IntPtr.Zero;
            IntPtr hBmp = IntPtr.Zero;
            IntPtr hOldBmp = IntPtr.Zero;
            try
            {
                float sx = PrimaryScreen.DpiX / 96F;
                float sy = PrimaryScreen.DpiY / 96F;
                float ScaleX = NativeMethods.ScaleX;
                //Size sz = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
                //sz.Width = (int)(sz.Width * ScaleX);
                //sz.Height = (int)(sz.Height * ScaleX);
                Size sz = NativeMethods.DESKTOP;
                hDesk = NativeMethods.GetDesktopWindow();
                hSrce = NativeMethods.GetWindowDC(hDesk);
                hDest = NativeMethods.CreateCompatibleDC(hSrce);
                hBmp = NativeMethods.CreateCompatibleBitmap(hSrce, sz.Width, sz.Height);
                hOldBmp = NativeMethods.SelectObject(hDest, hBmp);
                bool b = NativeMethods.BitBlt(hDest, 0, 0, sz.Width, sz.Height, hSrce, 0, 0, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

                using (var deskTopImg = Bitmap.FromHbitmap(hBmp))
                {
                    //sz = new Size((int)(sz.Width / sx), (int)(sz.Height / sy));
                    ////MessageBox.Show(rect.ToString());
                    //var img = ImageHelper.Resize(deskTopImg, sz.Width, sz.Height);

                    //MessageBox.Show(rect.ToString());
                    var rect2 = new Rectangle
                    {
                        X = (int)(rect.X * ScaleX),
                        Y = (int)(rect.Y * ScaleX),
                        Width = (int)(rect.Width * ScaleX),
                        Height = (int)(rect.Height * ScaleX)
                    };
                    bitmap = ImageHelper.CaptureImage(deskTopImg, rect2);
                    //using (var temp = ImageHelper.CaptureImage(deskTopImg, rect2))
                    //{
                    //    var rect3 = new Rectangle
                    //    {
                    //        X = (int)(rect.X / sx),
                    //        Y = (int)(rect.Y / sy),
                    //        Width = (int)(rect.Width / sx),
                    //        Height = (int)(rect.Height / sy)
                    //    };
                    //    temp?.Save(Path.Combine(path, DateTime.Now.Ticks.ToString() + $"_桌面切图.png"));
                    //    bitmap = ImageHelper.Resize(temp, rect3.Width, rect3.Height);
                    //    bitmap?.Save(Path.Combine(path, DateTime.Now.Ticks.ToString() + $"_桌面切图缩放.png"));
                    //}
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            finally
            {
                NativeMethods.SelectObject(hDest, hOldBmp);
                NativeMethods.DeleteObject(hBmp);
                NativeMethods.DeleteDC(hDest);
                NativeMethods.ReleaseDC(hDesk, hSrce);
            }
            return bitmap;
        }
        #endregion

        #region  获取分辨率缩放比例
        public static float ResolutionScalingX()
        {
            float scalx;
            try
            {
                scalx = NativeMethods.DpiX / 96F;
            }
            catch
            {
                scalx = 1f;
            }
            return scalx;
        }
        #endregion

        #region 剪切输入相关

        /// <summary>
        /// 通过剪切板的方式实现文本输入
        /// </summary>
        /// <param name="text">输入的值</param>
        /// <param name="isReplace">是否覆盖</param>
        /// <param name="originalValue">原有值</param>
        public static void ClipboardPaste(string text, int isReplace, string originalValue = "")
        {
            try
            {
                logger.LogDebug("剪切板输入");
                var replace = isReplace == 0 ? true : false;

                //if (!string.IsNullOrEmpty(originalValue))
                //{
                //    //NLogger.Default.Trace("按键清空输入框");
                //    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                //    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                //    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);

                //    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
                //    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                //    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                //    Thread.Sleep(200);
                //}

                if (replace)
                {
                    logger.LogDebug("剪切板替换");
                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);

                    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                    //Keyboard.CombinedInput(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL, FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);

                    Thread.Sleep(500);

                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
                    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
                    //Keyboard.CombinedInput(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);

                    Thread.Sleep(500);
                }
                else
                {
                    logger.LogDebug("剪切板追加");
                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);

                    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                    Thread.Sleep(200);

                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.RIGHT);
                    Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.RIGHT);
                    Thread.Sleep(200);
                }

                //text = replace ? text : originalValue + text;
                //System.Windows.Clipboard.SetText(text);
                logger.LogDebug($"剪切输入内容:{text}");
                SetClipBoard(text);

                Keyboard.CombinedInput(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL, FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_V);
                //keybd_event(Keys.ControlKey, 0, 0, 0);
                //keybd_event(Keys.V, 0, 0, 0);
                //keybd_event(Keys.ControlKey, 0, KEYEVENTF_KEYUP, 0);


                //Thread.Sleep(2000);
                //Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                //Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_V);

                //Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_V);
                //Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                Thread.Sleep(200);
                //Clipboard.Clear();
                ClearClipBoard();
                logger.LogDebug("ClearClipBoard");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "剪切输入异常");
            }
        }

        // 导入 Windows API 函数
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);


        /// <summary>
        /// 设置剪切板
        /// </summary>
        /// <param name="text"></param>
        /// <param name="MethodType">对应不同的实现方式 目前暂定为0</param>
        public static bool SetClipBoard(string text, int MethodType = 0)
        {
            try
            {
                ClipBoardInvoker.SetText(text);
                return true;

            }
            catch (Exception ex)
            {
                logger.LogError($"设置剪切板数据异常:{ex.ToString()}");
            }
            return false;
        }


        /// <summary>
        /// 清空剪切板
        /// </summary>
        /// <param name="text"></param>
        /// <param name="MethodType">对应不同的实现方式 目前暂定为0</param>
        public static bool ClearClipBoard(int MethodType = 0)
        {
            try
            {
                ClipBoardInvoker.Clear();
                return true;

            }
            catch (Exception ex)
            {
                logger.LogError($"清空剪切板异常:{ex.ToString()}");
            }
            return false;
        }

        #endregion


        /// <summary>
        /// 将图片存入剪切板
        /// </summary>
        /// <param name="source">二进制文件</param>
        public static void ClipboardSetImage(Image image)
        {
            logger.LogDebug("复制图片至剪切板");
            Clipboard.SetImage(image);
        }

        #region 保存/读取静态selector数据

        private static string _projectsDirectory = string.Empty;
        public static string ProjectsDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_projectsDirectory))
                {
                    string dllPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    //项目目录/packages/XXX/cs/XX.dll
                    _projectsDirectory = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(dllPath)));
                }
                return _projectsDirectory;
            }
        }

        public static void SaveStaticSelectorData(string uid, string fileName, string selectorData)
        {

            string resPath = Path.Combine(GlobleConfigurations.PackageDirectory, "res", uid);
            if (!Directory.Exists(resPath))
            {
                Directory.CreateDirectory(resPath);
            }
            string fileFullName = Path.Combine(resPath, fileName);
            using (StreamWriter streamWriter = new StreamWriter(fileFullName, append: false, encoding: Encoding.UTF8))
            {
                streamWriter.Write(selectorData);
            }
        }

        public static string ReadStaticSelectorData(string resDirPath, string fileName)
        {
            logger.LogInformation($"ProjectsDirectory:{GlobleConfigurations.PackageDirectory} resDirPath:{resDirPath} fileName:{fileName}");
            string fileFullName = Path.Combine(GlobleConfigurations.PackageDirectory, resDirPath, fileName);
            if (!File.Exists(fileFullName))
            {
                return string.Empty;
            }
            string text = File.ReadAllText(fileFullName, Encoding.UTF8);
            return text;
        }
        #endregion

        #region 提取selector
        public static string ExtractSelectors(string selector)
        {
            Func<string, bool> keepUncheckNodeFunc = (guiTypeStr) =>
                guiTypeStr.EqualIgnoreCase("web") ||
                new[] { eGuiType.Chrome,
                    eGuiType.IE,
                    eGuiType.Sogou,
                    eGuiType.FireFox,
                    eGuiType.Chrome,
                    eGuiType.Edge,
                    eGuiType.Web360se,
                    eGuiType.Web360X,
                    eGuiType.ZExplorer,
                    eGuiType.Windows,
                    eGuiType.SuperBrowser,
                    eGuiType.Chromium
                }.Any(tp => tp.ToString().Equals(guiTypeStr, StringComparison.OrdinalIgnoreCase));
            return ExtractViewData.ExtractSelectors(selector, keepUncheckNodeFunc);
        }
        #endregion
    }
}

