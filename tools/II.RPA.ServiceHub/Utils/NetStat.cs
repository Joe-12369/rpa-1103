using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.ServiceHub
{

    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public int SessionId { get; set; }
    }
    public class NetStat
    {
        private static ILogger logger = Logger.OfContext();

        public static async Task<ProcessInfo> GetUsePortProcessInfo(int port)
        {
            string portStr = port.ToString();
            return await Task.Run(() =>
             {
                 logger.LogDebug("GetUsePortProcessInfo");
                 try
                 {
                     string system32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
                     using (Process p = new Process())
                     {
                         ProcessStartInfo ps = new ProcessStartInfo();
                         ps.FileName = "cmd.exe";
                         ps.UseShellExecute = false;
                         ps.CreateNoWindow = true;
                         ps.WindowStyle = ProcessWindowStyle.Hidden;
                         ps.RedirectStandardInput = true;
                         ps.RedirectStandardOutput = true;
                         ps.RedirectStandardError = true;

                         p.StartInfo = ps;
                         p.Start();
                         StreamReader stdOutput = p.StandardOutput;
                         StreamReader stdError = p.StandardError;
                         p.StandardInput.AutoFlush = true;
                         p.StandardInput.WriteLine($"cd {system32Path}");
                         p.StandardInput.WriteLine($"netstat.exe  -a -n -o | findstr :{portStr}&exit");
                         string content = stdOutput.ReadToEnd() + stdError.ReadToEnd();
                         string exitStatus = p.ExitCode.ToString();
                         logger.LogDebug($"netstat 查询结果:{content}");
                         if (exitStatus != "0")
                         {
                             logger.LogDebug($"exitStatus:{exitStatus}");
                         }

                         using (StringReader sr = new StringReader(content))
                         {
                             while (sr.Peek() > -1)
                             {
                                 string row = sr.ReadLine();
                                 if (row.IndexOf($":{portStr} ") <= 0)
                                 {
                                     continue;
                                 }
                                 var tokens = row.Split(' ').ToList();
                                 tokens.RemoveAll(s => s == "");
                                 if (tokens.Count > 4)
                                 {
                                     var port_number = tokens[1].Split(':').Last();
                                     if (port_number == portStr)
                                     {
                                         var process_id = Convert.ToInt32(tokens.Last());
                                         LookupProcess(process_id, out var processName, out var sessionId);
                                         logger.LogDebug("GetUsePortProcessInfo-6");
                                         return new ProcessInfo
                                         {
                                             ProcessId = process_id,
                                             ProcessName = processName,
                                             SessionId = sessionId
                                         };
                                     }
                                 }
                             }
                         }
                     }
                 }
                 catch (Exception ex)
                 {
                     logger.LogError(ex.Message);
                 }
                 finally
                 {
                     logger.LogDebug("GetUsePortProcessInfo end");
                 }
                 return new ProcessInfo
                 {
                     ProcessId = 0,
                     ProcessName = "未匹配",
                     SessionId = 0
                 };
             });
        }
        public static void LookupProcess(int pid, out string processName, out int sessionId)
        {
            try
            {
                using (var process = Process.GetProcessById(pid))
                {
                    processName = process.ProcessName;
                    sessionId = process.SessionId;
                }
            }
            catch (Exception)
            {
                processName = "";
                sessionId = -1;
            }
        }
    }
}
