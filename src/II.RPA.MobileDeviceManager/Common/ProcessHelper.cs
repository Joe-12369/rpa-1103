using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Common
{
    public class ProcessHelper
    {
        public static Process[] GetAllProcessesByName(string processName)
        {
            if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                processName = processName.Substring(0, processName.Length - 4);
            }
            Process[] processesByName = Process.GetProcessesByName(processName);
            return processesByName;
        }
        public static int GetCurrentProcessId()
        {
            using (Process process = Process.GetCurrentProcess())
            {
                return process.Id;
            }
        }

        public static void Kill(int processId)
        {
            Process process = null;
            try
            {
                process = Process.GetProcessById(processId);
                process.Kill();
            }
            catch
            {
            }
            finally
            {
                process?.Dispose();
            }
        }
        public static void KillByPName(string processName)
        {

            try
            {
                var pArray = GetAllProcessesByName(processName);
                foreach (var process in pArray)
                {
                    process.Kill();
                }
            }
            catch
            {
            }
            finally
            {
                //process?.Dispose();
            }
        }
        public static void KillProcessAndChildren(int pid)
        {
            if (pid == 0)
            {
                return;
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
                    ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                if (pid != Process.GetCurrentProcess().Id)
                {
                    Process proc = Process.GetProcessById(pid);
                    proc.Kill();
                }
            }
            catch (ArgumentException)
            {
            }
        }
        public static void KillProcessAndChildrenByName(string processName)
        {
            var pArray = GetAllProcessesByName(processName);
            foreach (var process in pArray)
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher
                    ("Select * From Win32_Process Where ParentProcessID=" + process.Id);
                ManagementObjectCollection moc = searcher.Get();
                foreach (ManagementObject mo in moc)
                {
                    KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
                }
                try
                {
                    Process proc = Process.GetProcessById(process.Id);
                    proc.Kill();
                }
                catch (ArgumentException)
                {
                }
            }

        }
    }
}
