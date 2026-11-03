using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    public class ProcessInjector
    {
        private static readonly ILogger logger = Logger.OfContext();
        #region Win32 API 声明

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flAllocationType,
            uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            uint nSize,
            out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(
            IntPtr hHandle,
            uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeThread(
            IntPtr hThread,
            out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint dwFreeType);

        #endregion

        #region 常量定义

        // 进程访问权限
        private const uint PROCESS_CREATE_THREAD = 0x0002;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_VM_WRITE = 0x0020;
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

        // 内存分配
        private const uint MEM_COMMIT = 0x00001000;
        private const uint MEM_RESERVE = 0x00002000;
        private const uint MEM_RELEASE = 0x8000;

        // 内存保护
        private const uint PAGE_READWRITE = 0x04;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        // 线程等待
        private const uint INFINITE = 0xFFFFFFFF;
        private const uint WAIT_OBJECT_0 = 0x00000000;

        #endregion

        #region 公共方法

        /// <summary>
        /// 注入 DLL 到指定进程
        /// </summary>
        /// <param name="processId">目标进程ID</param>
        /// <param name="dllPath">DLL完整路径</param>
        /// <returns>是否成功</returns>
        public static InjectionResult InjectDll(int processId, string dllPath)
        {
            IntPtr hProcess = IntPtr.Zero;
            IntPtr pAllocatedMemory = IntPtr.Zero;
            IntPtr pLoadLibrary = IntPtr.Zero;
            IntPtr hThread = IntPtr.Zero;
            IntPtr threadId = IntPtr.Zero;
            IntPtr bytesWritten = IntPtr.Zero;

            try
            {
                logger.LogInformation($"[Injector] 开始注入进程 {processId}");
                logger.LogDebug($"[Injector] DLL路径: {dllPath}");

                // 1. 验证 DLL 文件存在
                if (!File.Exists(dllPath))
                {
                    string error = Localize($"DLL文件不存在: {dllPath}", "ProcessInjector_5A6930A2");
                    logger.LogDebug($"[Injector] {error}");
                    return new InjectionResult(false, error);
                }

                // 2. 打开目标进程
                uint dwDesiredAccess = PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION |
                                      PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ;

                hProcess = OpenProcess(dwDesiredAccess, false, (int)processId);

                if (hProcess == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    string error = Localize($"无法打开进程 (Error: {errorCode})", "ProcessInjector_663CEAA1");
                    logger.LogDebug($"[Injector] {error}");
                    return new InjectionResult(false, error);
                }

                logger.LogDebug($"[Injector] 成功打开进程句柄: 0x{hProcess.ToInt64():X}");

                // 3. 获取 LoadLibraryA 函数地址
                IntPtr hKernel32 = GetModuleHandle("kernel32.dll");
                if (hKernel32 == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    string error = Localize($"无法获取 kernel32.dll 句柄 (Error: {errorCode})", "ProcessInjector_DA76210A");
                    logger.LogDebug($"[Injector] {error}");
                    return new InjectionResult(false, error);
                }

                logger.LogDebug($"[Injector] kernel32.dll 句柄: 0x{hKernel32.ToInt64():X}");

                pLoadLibrary = GetProcAddress(hKernel32, "LoadLibraryA");
                if (pLoadLibrary == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    string error = Localize($"无法获取 LoadLibraryA 地址 (Error: {errorCode})", "ProcessInjector_742E59F3");
                    logger.LogDebug($"[Injector] {error}");
                    return new InjectionResult(false, error);
                }

                logger.LogDebug($"[Injector] LoadLibraryA 地址: 0x{pLoadLibrary.ToInt64():X}");

                // 4. 在目标进程中分配内存
                byte[] dllPathBytes = Encoding.ASCII.GetBytes(dllPath);
                uint allocSize = (uint)(dllPathBytes.Length + 1); // +1 for null terminator

                pAllocatedMemory = VirtualAllocEx(hProcess, IntPtr.Zero, allocSize,
                                                 MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

                if (pAllocatedMemory == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    string error = Localize($"无法在目标进程中分配内存 (Error: {errorCode})", "ProcessInjector_664333E4");
                    logger.LogDebug($"[Injector] {error}");
                    return new InjectionResult(false, error);
                }

                logger.LogDebug($"[Injector] 分配内存地址: 0x{pAllocatedMemory.ToInt64():X}, 大小: {allocSize} 字节");

                // 5. 写入 DLL 路径到目标进程
                bool writeResult = WriteProcessMemory(hProcess, pAllocatedMemory, dllPathBytes,
                                                     allocSize, out bytesWritten);

                if (!writeResult || bytesWritten.ToInt64() != allocSize)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    string error = Localize($"无法写入进程内存 (Error: {errorCode}, 写入: {bytesWritten}/{allocSize} 字节)", "ProcessInjector_56CD8B1");
                    logger.LogDebug($"[Injector] {error}");
                    return new InjectionResult(false, error);
                }

                logger.LogDebug($"[Injector] 成功写入 DLL 路径到进程内存");

                // 6. 创建远程线程执行 LoadLibraryA
                hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, pLoadLibrary,
                                            pAllocatedMemory, 0, out threadId);

                if (hThread == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    string error = Localize($"无法创建远程线程 (Error: {errorCode})", "ProcessInjector_7E5DAFAE");
                    logger.LogDebug($"[Injector] {error}");
                    return new InjectionResult(false, error);
                }

                logger.LogDebug($"[Injector] 远程线程创建成功, 线程ID: 0x{threadId.ToInt64():X}");

                // 7. 等待线程执行完成
                uint waitResult = WaitForSingleObject(hThread, 5000); // 等待5秒
                if (waitResult != WAIT_OBJECT_0)
                {
                    logger.LogDebug($"[Injector] 警告: 线程未在超时时间内完成 (结果: {waitResult})");
                }
                else
                {
                    logger.LogDebug($"[Injector] 远程线程执行完成");

                    // 获取线程退出码（即 LoadLibrary 返回的模块句柄）
                    uint exitCode;
                    if (GetExitCodeThread(hThread, out exitCode))
                    {
                        if (exitCode == 0)
                        {
                            logger.LogDebug($"[Injector] 警告: LoadLibrary 返回 NULL，DLL 可能未正确加载");
                        }
                        else
                        {
                            logger.LogDebug($"[Injector] LoadLibrary 返回模块句柄: 0x{exitCode:X}");
                        }
                    }
                }

                // 8. 清理分配的内存（可选，DLL加载后会保持）
                // VirtualFreeEx(hProcess, pAllocatedMemory, 0, MEM_RELEASE);

                logger.LogInformation($"[Injector] DLL 注入成功完成!");
                return new InjectionResult(true, "DLL注入成功");

            }
            catch (Exception ex)
            {
                string error = Localize($"注入过程中发生异常: {ex.Message}", "ProcessInjector_C44AE295");
                logger.LogDebug($"[Injector] {error}");
                logger.LogDebug($"[Injector] 堆栈跟踪: {ex.StackTrace}");
                return new InjectionResult(false, error);
            }
            finally
            {
                // 清理句柄
                if (hThread != IntPtr.Zero) CloseHandle(hThread);
                if (hProcess != IntPtr.Zero) CloseHandle(hProcess);

                logger.LogDebug($"[Injector] 资源清理完成");
            }
        }
       

        #endregion

        #region 辅助类

        /// <summary>
        /// 注入结果
        /// </summary>
        public class InjectionResult
        {
            public bool Success { get; }
            public string Message { get; }
            public IntPtr ThreadId { get; }
            public IntPtr ModuleHandle { get; }

            public InjectionResult(bool success, string message, IntPtr threadId = default, IntPtr moduleHandle = default)
            {
                Success = success;
                Message = message;
                ThreadId = threadId;
                ModuleHandle = moduleHandle;
            }
        }

        /// <summary>
        /// 进程信息
        /// </summary>
        public class ProcessInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string WindowTitle { get; set; }
            public DateTime StartTime { get; set; }
            public bool HasWindow { get; set; }

            public override string ToString()
            {
                return $"PID: {Id}, 名称: {Name}, 窗口: {(HasWindow ? WindowTitle : "无")}";
            }
        }

        #endregion
    }
}
