using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Window.Core;
using II.RPA.Plugin.Window.Params;
using II.RPA.WinAPI;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;

namespace II.RPA.Plugin.Window.Plugins
{
    internal class ProcessKiller
    {
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
                if (Process.GetProcessById((int)idItem).ProcessName == processName)
                {
                    foundParents.Add(idItem);
                }
            }
            return foundParents;
        }

        /// <summary>
        /// 使用指定的IMG名称和进程名称关闭进程
        /// </summary>
        /// <param name="processIMGName">IMG名称</param>
        /// <param name="processName">进程名称</param>
        public static void KillProcess(string processIMGName, string processName)
        {
            var findItems = GetParentName(processIMGName, processName);
            if (findItems.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("/F ");
                foreach (var findOne in findItems)
                {
                    builder.Append("/PID ").Append(findOne);
                }
                Process firstP = new Process();
                firstP.StartInfo.FileName = "taskkill";
                firstP.StartInfo.Arguments = builder.ToString();
                firstP.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
                firstP.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
                firstP.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
                firstP.StartInfo.RedirectStandardError = true;//重定向标准错误输出
                firstP.StartInfo.CreateNoWindow = true;//不显示程序窗口
                firstP.Start();//启动程序
                firstP.WaitForExit(3000);
            }

            Thread.Sleep(1000);

            Process p = new Process();
            p.StartInfo.FileName = "taskkill";
            p.StartInfo.Arguments = "/F /IM " + processIMGName;
            p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            p.StartInfo.CreateNoWindow = true;//不显示程序窗口
            p.Start();//启动程序
            p.WaitForExit(3000);
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

                        var parentIdObj = queryObj["ParentProcessId"];
                        uint defaultParentPid = 0;
                        if (parentIdObj != null)
                        {
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
                }
            }
            return ret;
        }
    }
    public class CloseWindow : WindowComponentBase<ParamCloseWindowModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.CloseWindow;


        protected override JMessage Execute(ParamCloseWindowModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {


                WindowData windowData;

                if (paramModel.SwitchType.ToLower() == "selecter")
                {
                    windowData = WindowOperator.FindBySelecter(paramModel.Selecter);
                }
                else if (paramModel.SwitchType.ToLower() == "title")
                {
                    windowData = WindowOperator.FindWindosByKeywords(paramModel.Keywords, paramModel.MatchType);
                }
                else
                {
                    windowData = paramModel.WindowData.ToObject<WindowData>();
                    if (windowData.Title == "DeskTop")
                    {
                        logger.LogInformation($"窗口对象为桌面，操作失败");
                        throw new Exception("窗口对象为桌面，操作失败".I18n("controlType_CloseWindow_36126028"));
                    }
                    else
                    {
                        windowData = WindowOperator.MatchByData(windowData);
                    }
                }
                //var windowData = paramModel.WindowData.ToObject<WindowData>();
                //windowData = WindowOperator.MatchByData(windowData);

                long.TryParse(windowData.WindowHandle, out long numHandle);
                IntPtr handle = new IntPtr(numHandle);

                const int WM_CLOSE = 0x0010;
                const int WM_QUIT = 0x12;
                User32.GetWindowThreadProcessId(handle, out uint processId);
                Process process = Process.GetProcessById((int)processId);
                var name = process.ProcessName;
                // p.MainWindowHandle is 0
                //User32.CloseWindow(handle);
                User32.SendMessage(handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                if (paramModel.IsKillProcess == 0)
                {
                    process.Kill();
                }
                // does not work
                //User32.PostMessage(handle, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                var initIndex = 0;
                while (stopWatch.ElapsedMilliseconds <= paramModel.timeout)
                {
                    while (initIndex < 5)
                    {
                        if (User32.GetForegroundWindow() != handle)
                        {
                            msg.Info = "true";
                            return msg;
                        }
                        //添加延时（部分场景存在窗口关闭后但下面获取置顶窗口句柄方法还是当前关闭的窗口句柄）
                        Thread.Sleep(600);
                        initIndex++;
                    }


                    if ("explorer".Contains(name))
                    {
                        stopWatch.Stop();
                        msg.Info = "true";
                        return msg;
                    }
                    if (process.MainWindowHandle == IntPtr.Zero)
                    {
                        stopWatch.Stop();
                        msg.Info = "true";
                        return msg;
                    }
                    ProcessKiller.KillProcess($"{name}.exe", name);
                    Thread.Sleep(100);
                }
                stopWatch.Stop();
                throw new WindowException(eErrorMessageType.Error_WaitTimeout);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_CloseWindow, ref msg);
            }
            return msg;
        }

    }

}
