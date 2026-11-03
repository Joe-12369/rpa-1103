using II.RPA.Logging;
using II.RPA.Logging.Definitions;
using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace II.RPA.JavaBridgeHost
{
    static class Program
    {
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

            }
            return null;
        }
        private static void KillPrevHost()
        {
            Process[] processesByName = GetProcessesByName("II.RPA.JavaBridgeHost", onlyCurrentUser: true);
            int curPid = Process.GetCurrentProcess().Id;
            Process[] array = processesByName;
            foreach (Process process in array)
            {
                if (process.Id != curPid)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }
                }
            }
        }
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Logger.ConfigWithRole(LoggingRoles.Default);
            KillPrevHost();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            IIRPAJavaHost form = null;
            string key = "II.RPA.JavaBridgeHost";
            //bool newMutex;
            //Mutex m = new Mutex(true, key, out newMutex);
            //if (newMutex == true)
            //{
            form = new IIRPAJavaHost();
            form.Text = key;
            Application.Run(form);
            //    m.ReleaseMutex();
            //}
        }

        [DllImport("user32.dll ")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "ShowWindow", CharSet = CharSet.Auto)]
        private static extern bool ShowWindow(IntPtr hWnd, uint cmdShow);

        public static void AsyncExec(Action method)
        {
            IAsyncResult aResult = method.BeginInvoke(delegate (IAsyncResult ar)
            {
                method.EndInvoke(ar);   //BeginInvoke和EndInvoke成对出现
            }, null);
        }

        static void SetMemory(string key, IntPtr handler)
        {
            AsyncExec(() =>
            {

                MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(key, 1024000);

                using (MemoryMappedViewStream stream = mmf.CreateViewStream()) //注意这里的偏移量
                {
                    using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
                    {
                        accessor.Write(0, ref handler);
                    }
                }
            });
        }

        static IntPtr GetMemory(string key)
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(key))
            {
                using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
                {
                    IntPtr handler = IntPtr.Zero;
                    accessor.Read(0, out handler);
                    return handler;
                }
            }
        }
    }
}
