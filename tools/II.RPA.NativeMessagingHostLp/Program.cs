using Newtonsoft.Json;
using ScreenCapture;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.NativeMessagingHostLp
{

    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            WaitPrevHost();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            try
            {
                Application.Run(new AppContext());
            }
            catch (Exception e)
            {
                DebugHelper.WriteError(e.Message);
            }
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            DebugHelper.WriteError(e.Message);
        }

        private static void Stdio_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private static void WaitPrevHost()
        {
            DebugHelper.WriteError($"WaitPrevHost");
            for (int i = 0; i < 30; i++)
            {
                Process[] processesByName = GetProcessesByName("II.RPA.NativeMessagingHostLp", onlyCurrentUser: true);
                int curPid = Process.GetCurrentProcess().Id;
                int oldConut = processesByName.Count(d => d.Id != curPid);
                if (oldConut == 0)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }

        }

        private static void LogError(Exception ex)
        {
            string logfile = Path.Combine(Path.GetTempPath(), "II.RPA.NativeMessagingHostLp.txt");
            StringBuilder stringBuilder = new StringBuilder();
            do
            {
                stringBuilder.AppendLine(ex.Message);
                stringBuilder.AppendLine(ex.StackTrace);
                ex = ex.InnerException;
            }
            while (ex != null);
            File.WriteAllText(logfile, stringBuilder.ToString());
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
                LogError(e);
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
                LogError(e);
            }
            return null;
        }

    }
}
