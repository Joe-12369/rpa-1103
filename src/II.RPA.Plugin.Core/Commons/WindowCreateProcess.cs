using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace II.RPA.Plugin.Core.Commons
{
    public class WindowCreateProcess
    {
        private static ILogger logger = Logger.OfContext();
        /// <summary>
        /// chrome,edge
        /// </summary>
        public const int ExitCode_ProfileOccupation = 21;

        public const int ExitCode_Success = 0;
        public const int ExitCode_Failed = -1;
        public const int ExitCode_Unknown = -2;
        public const int ExitCode_AccessDeny = 5;
        public const int ExitCode_Timeout = 259;
        [System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
        private class SECURITY_ATTRIBUTES
        {
            public int nLength;
            public string lpSecurityDescriptor;
            public bool bInheritHandle;
        }
        [Flags]
        public enum PROCESS_CREATION_FLAGS : uint
        {
            DEBUG_PROCESS = 0x1u,
            DEBUG_ONLY_THIS_PROCESS = 0x2u,
            CREATE_SUSPENDED = 0x4u,
            DETACHED_PROCESS = 0x8u,
            CREATE_NEW_CONSOLE = 0x10u,
            NORMAL_PRIORITY_CLASS = 0x20u,
            IDLE_PRIORITY_CLASS = 0x40u,
            HIGH_PRIORITY_CLASS = 0x80u,
            REALTIME_PRIORITY_CLASS = 0x100u,
            CREATE_NEW_PROCESS_GROUP = 0x200u,
            CREATE_UNICODE_ENVIRONMENT = 0x400u,
            CREATE_SEPARATE_WOW_VDM = 0x800u,
            CREATE_SHARED_WOW_VDM = 0x1000u,
            CREATE_FORCEDOS = 0x2000u,
            BELOW_NORMAL_PRIORITY_CLASS = 0x4000u,
            ABOVE_NORMAL_PRIORITY_CLASS = 0x8000u,
            INHERIT_PARENT_AFFINITY = 0x10000u,
            INHERIT_CALLER_PRIORITY = 0x20000u,
            CREATE_PROTECTED_PROCESS = 0x40000u,
            EXTENDED_STARTUPINFO_PRESENT = 0x80000u,
            PROCESS_MODE_BACKGROUND_BEGIN = 0x100000u,
            PROCESS_MODE_BACKGROUND_END = 0x200000u,
            CREATE_SECURE_PROCESS = 0x400000u,
            CREATE_BREAKAWAY_FROM_JOB = 0x1000000u,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x2000000u,
            CREATE_DEFAULT_ERROR_MODE = 0x4000000u,
            CREATE_NO_WINDOW = 0x8000000u,
            PROFILE_USER = 0x10000000u,
            PROFILE_KERNEL = 0x20000000u,
            PROFILE_SERVER = 0x40000000u,
            CREATE_IGNORE_SYSTEM_DEFAULT = 0x80000000u
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public int lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public int wShowWindow;
            public int cbReserved2;
            public byte lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateProcess(
          StringBuilder lpApplicationName, StringBuilder lpCommandLine,
          SECURITY_ATTRIBUTES lpProcessAttributes,
          SECURITY_ATTRIBUTES lpThreadAttributes,
          bool bInheritHandles,
          int dwCreationFlags,
          StringBuilder lpEnvironment,
          StringBuilder lpCurrentDirectory,
          ref STARTUPINFO lpStartupInfo,
          ref PROCESS_INFORMATION lpProcessInformation
          );
        #region Win32 Api : WaitForSingleObject
        //检测一个系统核心对象(线程，事件，信号)的信号状态，当对象执行时间超过dwMilliseconds就返回，否则就一直等待对象返回信号
        [DllImport("Kernel32.dll")]
        private static extern uint WaitForSingleObject(System.IntPtr hHandle, uint dwMilliseconds);

        #endregion

        #region Win32 Api : CloseHandle
        //关闭一个内核对象,释放对象占有的系统资源。其中包括文件、文件映射、进程、线程、安全和同步对象等
        [DllImport("Kernel32.dll")]
        private static extern bool CloseHandle(System.IntPtr hObject);

        #endregion       

        #region Win32 Api : GetExitCodeProcess
        //获取一个已中断进程的退出代码,非零表示成功，零表示失败。
        //参数hProcess，想获取退出代码的一个进程的句柄，参数lpExitCode，用于装载进程退出代码的一个长整数变量。
        [DllImport("Kernel32.dll")]
        private static extern bool GetExitCodeProcess(System.IntPtr hProcess, ref uint lpExitCode);

        #endregion
        public static void CreateProcess(string argm, bool retry)
        {
            try
            {
                CreateProcess(argm);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"失败，准备再次执行CreateProcess ");
                if (retry)
                {
                    Thread.Sleep(2000);
                    CreateProcess(argm);
                }
                else
                {
                    throw ex;
                }
            }
        }
        [Obsolete]
        public static void CreateBrowser(string browserPath, string browserProcessName, string url, string processArgs, bool IgorneCreateSetting = false)
        {
            var createParam = string.Empty;
            if (IgorneCreateSetting)
            {
                if (!string.IsNullOrEmpty(url))
                {
                    createParam = $"{browserPath} \"{url}\"{processArgs}";
                }
                else
                {
                    createParam = $"{browserPath} {processArgs}";
                }
                WindowCreateProcess.CreateProcess(createParam);
            }
            else
            {
                Process[] ps = RPAUtils.GetProcessesBySessionId(browserProcessName);
                if (ps == null || ps?.Length == 0)
                {
                    WindowCreateProcess.CreateProcess(browserPath + processArgs);
                }
                if (!RPAUtils.Retry(() => RPAUtils.GetProcessesBySessionId(browserProcessName).Length > 0, 3000))
                {
                    logger.LogDebug($"浏览器进程未打开");
                }
                if (!string.IsNullOrEmpty(url))
                {
                    createParam = $"{browserPath} \"{url}\"{processArgs}";
                }
                else
                {
                    createParam = $"{browserPath} {processArgs}";
                }
                WindowCreateProcess.CreateProcess(createParam);
            }
        }

        public static bool CreateBrowser(string browserPath,
            string browserProcessName,
            string url,
            string processArgs,
            bool ignoreCreateSetting,
            out int code,
            bool careProfileOccupation)
        {
            string createParam;
            //ignoreCreateSetting true 和 false的逻辑没有区别
            if (!string.IsNullOrEmpty(url))
            {
                createParam = $"{browserPath} \"{url}\"{processArgs}";
            }
            else
            {
                createParam = $"{browserPath} {processArgs}";
            }
            return CreateProcess(createParam, out code, careProfileOccupation);
        }

        [Obsolete]
        public static void CreateProcess(string args)
        {
            STARTUPINFO sInfo = new STARTUPINFO();
            PROCESS_INFORMATION pInfo = default;
            logger.LogInformation($"CreateProcess {args}");
            var lpCommandLine = new StringBuilder(args);
            try
            {
                if (!CreateProcess(null, lpCommandLine, null, null, false, (int)(PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE | PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW), null, null, ref sInfo, ref pInfo))
                {
                    if (Marshal.GetLastWin32Error() != 5)
                    {
                        throw new InvalidOperationException("创建进程失败，可能是拒绝访问".I18n("WindowCreateProcess_4AE9FC2D"));
                    }
                    logger.LogDebug($"创建进程失败，即将再次尝试");
                    if (!CreateProcess(null, lpCommandLine, null, null, false, (int)(PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE | PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW), null, null, ref sInfo, ref pInfo))
                    {
                        throw new InvalidOperationException("创建进程失败".I18n("WindowCreateProcess_B5E6309E"));
                    }
                }
                uint i = 0;
                WaitForSingleObject(pInfo.hProcess, 0);
                GetExitCodeProcess(pInfo.hProcess, ref i);
                Thread.Sleep(300);

            }
            finally
            {
                if (pInfo.hProcess != IntPtr.Zero)
                {
                    CloseHandle(pInfo.hProcess);
                    CloseHandle(pInfo.hThread);
                }
            }
        }


        /// <summary>
        /// CreateProcess
        /// </summary>
        /// <param name="args"></param>
        /// <param name="code">
        /// 0: 启动成功
        /// 21：chrome/edge profile文件创建失败
        /// -1：启动失败
        /// -2: 没有获取到 exit code
        /// </param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static bool CreateProcess(string args, out int code, bool careProfileOccupation)
        {
            code = ExitCode_Failed;
            STARTUPINFO sInfo = new STARTUPINFO();
            PROCESS_INFORMATION pInfo = default;
            logger.LogInformation($"CreateProcess {args}");
            var lpCommandLine = new StringBuilder(args);
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                if (!CreateProcess(null, lpCommandLine, null, null, false, (int)(PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE | PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW), null, null, ref sInfo, ref pInfo))
                {
                    if (Marshal.GetLastWin32Error() != 5)
                    {
                        logger.LogError("创建进程失败，可能是拒绝访问");
                        code = ExitCode_AccessDeny;
                        return false;
                    }
                    logger.LogDebug($"创建进程失败，即将再次尝试");
                    if (!CreateProcess(null, lpCommandLine, null, null, false, (int)(PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE | PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW), null, null, ref sInfo, ref pInfo))
                    {
                        throw new InvalidOperationException("创建进程失败".I18n("WindowCreateProcess_B5E6309E"));
                    }
                }
                logger.LogDebug($"Create process cost:{sw.ElapsedMilliseconds}ms");
                sw.Restart();
                if (careProfileOccupation)
                {
                    logger.LogDebug($"Start watching profile occupation");
                    uint exit = ExitCode_Success;
                    ///经验证, 如果发生了 profile occupation 该方法基本在200ms以内返回
                    ///如果正常启动, 可能直到手动关闭浏览器才会触发 exit
                    WaitForSingleObject(pInfo.hProcess, 500);
                    if (GetExitCodeProcess(pInfo.hProcess, ref exit))
                    {
                        code = (int)exit;
                        logger.LogInformation($"{pInfo.dwProcessId} exit code:{code}");
                    }
                    else
                    {
                        logger.LogInformation($"Failed to get exit code");
                        code = ExitCode_Unknown;
                    }
                    logger.LogDebug($"Get exit code cost:{sw.ElapsedMilliseconds}ms");
                }
                else
                {
                    Thread.Sleep(300);
                }
                sw.Stop();
            }
            finally
            {
                if (pInfo.hProcess != IntPtr.Zero)
                {
                    CloseHandle(pInfo.hProcess);
                    CloseHandle(pInfo.hThread);
                }
            }
            return code == ExitCode_Success || code == ExitCode_Timeout;
        }
    }
}

