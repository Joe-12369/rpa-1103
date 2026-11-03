using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Utils
{

    public class ProcessKiller
    {
        private static ILogger logger = Logger.OfContext();

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
        /// <summary>
        /// 使用指定的IMG名称和进程名称获取父亲进程的PID
        /// </summary>
        /// <param name="processIMGName">IMG名称</param>
        /// <param name="processName">进程名称</param>
        /// <returns>父亲进程PID列表</returns>
        public static List<uint> GetParentName(string processIMGName, string processName)
        {
            var ids = GetParentIds(processIMGName).Values.Distinct().ToList();
            List<uint> foundParents = new List<uint>();
            foreach (var idItem in ids)
            {
                try
                {
                    if (Process.GetProcessById((int)idItem).ProcessName == processName)
                    {
                        foundParents.Add(idItem);
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"通过PID获取进程时异常，忽略这个进程号，因为它已经不存在");
                }
            }
            return foundParents;
        }
        private static void TaskKill(StringBuilder stringBuilder, int timeout = 3000)
        {
            Process firstP = new Process();
            firstP.StartInfo.FileName = "taskkill";
            firstP.StartInfo.Arguments = stringBuilder.ToString();
            firstP.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            firstP.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            firstP.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            firstP.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            firstP.StartInfo.CreateNoWindow = true;//不显示程序窗口
            firstP.Start();//启动程序
            firstP.WaitForExit(3000);
            firstP.Dispose();
        }
        /// <summary>
        /// 使用指定的IMG名称和进程名称关闭进程
        /// </summary>
        /// <param name="processIMGName">IMG名称</param>
        /// <param name="processName">进程名称</param>
        public static void KillProcess(string processIMGName, string processName)
        {
            Task.Run(() =>
            {
                var processAll = GetProcessesBySessionId(processName);
                if (processAll.Length > 0)
                {
                    StringBuilder builderProcessIds = new StringBuilder();
                    builderProcessIds.Append("/F");
                    foreach (var findOne in processAll)
                    {
                        builderProcessIds.AppendFormat(" /PID {0}", findOne.Id);
                    }
                    processAll.Dispose();
                    TaskKill(builderProcessIds);
                }
            });
        }

        public static void KillProcessByIds(int[] processIds)
        {
            RPAUtils.ExecuteWithTimeout(() =>
            {
                if (processIds.Length == 0)
                {
                    return;
                }
                StringBuilder builderProcessIds = new StringBuilder();
                builderProcessIds.Append("/F");
                foreach (var processId in processIds)
                {
                    builderProcessIds.AppendFormat(" /PID {0}", processId);
                }
                TaskKill(builderProcessIds);
            }, 3000);
        }

        /// <summary>
        /// 简单关闭进程方法 目前仅紫鸟使用
        /// </summary>
        /// <param name="processName"></param>
        public static void KillProcessSimple(string processName)
        {
            Process[] processes = GetProcessesBySessionId(processName);
            if (processes.Length > 0)
            {
                foreach (Process process in processes)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit();
                        logger.LogDebug($"进程 {process.ProcessName} (ID: {process.Id}) 已成功停止。");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"无法停止进程 {process.ProcessName} (ID: {process.Id})。错误信息: {ex.Message}");
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            else
            {
                logger.LogWarning($"没有找到进程名称为 {processName} 的进程。");
            }
        }
        public static void KillProcessIgnoreParent(string processIMGName, string processName)
        {
            Task.Run(() =>
            {
                var findItems = GetParentName(processIMGName, processName);
                if (findItems.Count > 0)
                {
                    StringBuilder builderParentIds = new StringBuilder();
                    builderParentIds.Append("/F");
                    foreach (var findOne in findItems)
                    {
                        builderParentIds.AppendFormat(" /PID {0}", findOne);
                    }
                    TaskKill(builderParentIds);
                }
                var processAll = GetProcessesBySessionId(processName);
                if (processAll.Length > 0)
                {
                    StringBuilder builderProcessIds = new StringBuilder();
                    builderProcessIds.Append("/F");
                    foreach (var findOne in findItems)
                    {
                        builderProcessIds.AppendFormat(" /PID {0}", findOne);
                    }
                    TaskKill(builderProcessIds);
                }
            });
        }
        /// <summary>
        /// 使用指定的IMG名称获取进程PID对应父进程名称，如果没有父进程，即自己
        /// </summary>
        /// <param name="processImgName">进程IMG名称</param>
        private static Dictionary<uint, uint> GetParentIds(string processImgName)
        {
            Dictionary<uint, uint> ret = new Dictionary<uint, uint>();
            var query = string.Format("SELECT ParentProcessId,ProcessId,Name FROM Win32_Process WHERE Name = '{0}'", processImgName);
            using (var search = new ManagementObjectSearcher("root\\CIMV2", query))
            {
                using (var foundItems = search.Get())
                {
                    foreach (var queryObj in foundItems)
                    {
                        var processId = (uint)queryObj["ProcessId"];
                        #region [增加SessionId过滤进程]
                        var processAll = GetProcessesBySessionId(processImgName.Replace(".exe", ""));
                        if (processAll != null)
                        {
                            foreach (var process in processAll)
                            {
                                if (process.Id == (int)processId)
                                {
                                    var parentIdObj = queryObj["ParentProcessId"];
                                    if (parentIdObj != null)
                                    {
                                        uint defaultParentPid = 0;
                                        if (uint.TryParse(parentIdObj.ToString(), out defaultParentPid))
                                        {
                                            ret[processId] = defaultParentPid;
                                        }
                                    }
                                    else
                                    {
                                        ret[processId] = processId;
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                        #endregion
                    }
                }
            }
            return ret;
        }
    }
}

