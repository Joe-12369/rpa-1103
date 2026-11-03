using System;
using System.Diagnostics;
using System.IO;

namespace II.RPA.Core.GRPCService.Service
{
    /// <summary>
    /// 解析 Z-Pid\Z-Desktop.txt 中小窗进程 pid 的纯函数工具。
    /// 陈旧 pid（小窗进程已退出但文件未清理）按"未运行"返回 false，不抛异常。
    /// </summary>
    public static class ZDeskTopProcessResolver
    {
        public static bool TryGetRunningProcessId(string pidFilePath, out int pid)
        {
            pid = -1;
            try
            {
                if (!File.Exists(pidFilePath))
                {
                    return false;
                }

                var idString = File.ReadAllText(pidFilePath);
                if (string.IsNullOrEmpty(idString))
                {
                    return false;
                }

                var processId = int.Parse(idString);
                try
                {
                    using (var process = Process.GetProcessById(processId))
                    {
                        pid = process.Id;
                        return true;
                    }
                }
                catch (ArgumentException)
                {
                    // 陈旧 pid：文件中的进程已退出，语义为"未在运行"
                    return false;
                }
            }
            catch (IOException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
