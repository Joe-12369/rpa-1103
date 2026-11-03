using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.NativeMessageingStartup
{
    public static class ProcessSnapshot
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint flags, StringBuilder exeName, ref uint nameLen);

        private static readonly string _cachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "processes.json");
        private static readonly Mutex _writeMutex = new Mutex(false, @"Global\II.RPA.NativeMessagingHost_Mutex");
        private static readonly TaskCompletionSource<bool> _snapshotReadyTcs = new TaskCompletionSource<bool>();

        internal static ParentInfo ParentProcessInfo { get; private set; }
        /// <summary>
        /// 写入当前进程的快照
        /// </summary>
        public static void WriteCurrent()
        {
            if (!Directory.Exists(Path.GetDirectoryName(_cachePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_cachePath));
            }

            try
            {
                _writeMutex.WaitOne();
                var processRecords = ReadAlive();
                var proc = Process.GetCurrentProcess();
                var record = new ProcessRecord
                {
                    Pid = proc.Id,
                    StartTime = proc.StartTime.ToUniversalTime(),
                    ProcessName = proc.ProcessName,
                    Parent = ParentProcessUtilities.GetParentInfo(proc)
                };
                ParentProcessInfo = record.Parent;
                _snapshotReadyTcs.TrySetResult(true);
                processRecords.Add(record);
                var json = JsonConvert.SerializeObject(processRecords, Formatting.None);

                File.WriteAllText(_cachePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                _writeMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 读取并过滤出“仍存活”的进程记录
        /// </summary>
        public static List<ProcessRecord> ReadAlive()
        {
            if (!File.Exists(_cachePath)) return new List<ProcessRecord>();

            var jsonStr = File.ReadAllText(_cachePath, Encoding.UTF8);
            var processRecords = JsonConvert.DeserializeObject<List<ProcessRecord>>(jsonStr);
            processRecords = processRecords
                ?? new List<ProcessRecord>();
            return processRecords.Where(rec =>
            {
                try
                {
                    var proc = Process.GetProcessById(rec.Pid);
                    if (proc.StartTime.ToUniversalTime() == rec.StartTime)
                        return true;
                }
                catch (ArgumentException) { /* PID 不存在 */ }
                return false;
            }).ToList();

        }
        internal static async Task<bool> WaitProcessSnapshotAsync(int timeoutMs = 1000)
        {
            // 如果已经就绪，立即返回 true
            if (ProcessSnapshot.ParentProcessInfo != null)
                return true;

            // 未就绪：等待事件或超时
            var delay = Task.Delay(timeoutMs);
            if (await Task.WhenAny(_snapshotReadyTcs.Task, delay) == _snapshotReadyTcs.Task)
                return true;
            return false;
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
        public ParentInfo(string name, int pid, DateTime startTime)
        {
            this.Name = name;
            this.Pid = pid;
            this.StartTime = startTime;
        }
        public string Name { get; set; } = "";
        public int Pid { get; set; }
        public DateTime StartTime { get; set; }
    }
}
