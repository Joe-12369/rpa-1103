using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Common
{
    public static  class CommonUtils
    {
        public readonly static int CurrentSessionId = Process.GetCurrentProcess().SessionId;
        private static Random m_Ran = new Random();
        public static string GenerateRandom()
        {
            var reqestId = $"{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}{m_Ran.Next(1, 999999),5}";
            return reqestId;
        }
        public static IEnumerable<Process> GetProcessesInCurrentSession(string processName)
        {
            try
            {
                return (from m in Process.GetProcessesByName(processName)
                        where m.SessionId == CurrentSessionId
                        select m);
            }
            catch (Exception)
            {
                //
            }
            return null;
        }

        public static bool IsProcessRunning(int processId)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检测进程时发生错误: {ex.Message}");
                return false;
            }
        }
    }
}
