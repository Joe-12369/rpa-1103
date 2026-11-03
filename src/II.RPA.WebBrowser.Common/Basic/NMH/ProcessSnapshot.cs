using II.RPA.Logging;
using II.RPA.Plugin.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.NMH
{
    public class NMHProcessSnapshot
    {
        private static ILogger logger = Logger.OfContext();

        public static Process GetParentBySnapshot(int pid)
        {
            return GetParentBySnapshot(Process.GetProcessById(pid));
        }
        public static Process GetParentBySnapshot(Process proc)
        {
            try
            {
                string exePath = proc.GetExeFilePath();
                string dir = Path.GetDirectoryName(exePath);
                string jsonPath = Path.Combine(dir, "processes.json");
                List<ProcessRecord> records = ReadAlive(jsonPath);

                ProcessRecord processRecord = records.FirstOrDefault(rec => rec.Pid == proc.Id && proc.StartTime.ToUniversalTime() == rec.StartTime);
                if (processRecord == null) return null;
                logger.LogDebug($"GetParentBySnapshot: {proc.ProcessName}({proc.Id}) parent is {processRecord.Parent.Name}({processRecord.Parent.Pid})");
                return Process.GetProcessById(processRecord.Parent.Pid);
            }
            catch
            {
                // 无权限、路径异常等
                return null;
            }
        }
        public static List<ProcessRecord> ReadAlive(string jsonPath)
        {
            if (!File.Exists(jsonPath)) return new List<ProcessRecord>();

            var jsonStr = File.ReadAllText(jsonPath, Encoding.UTF8);
            var processRecords = JsonConvert.DeserializeObject<List<ProcessRecord>>(jsonStr);
            return processRecords;
        }
    }
    public sealed class ProcessRecord
    {
        public int Pid { get; set; }
        public DateTime StartTime { get; set; }
        public string BusinessKey { get; set; } = "";
        public string ProcessName { get; set; } = "";
        public ParentInfo Parent { get; set; }
    }
    public sealed class ParentInfo
    {

        public ParentInfo(string name, int pid, string exeFilePath)
        {
            this.Name = name;
            this.Pid = pid;
            this.ExeFilePath = exeFilePath;
        }
        public string Name { get; set; } = "";
        public string ExeFilePath { get; set; } = "";
        public int Pid { get; set; }
    }
}
