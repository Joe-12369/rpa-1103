using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace II.Framework.Utility
{
    public static class StartUpProgramHelper
    {
        private const string RUNKEY = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string StartupApproved_RUNKEY = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run";
        private static List<ApprovedStartUpProgram> ApprovedStartUpPrograms;
        private static byte[] EnableValue = new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private static byte[] DisableValue = new byte[] { 3, 0, 0, 0, 24, 61, 6, 202, 33, 177, 215, 1 };
        public static void GetAllSartupPrograms()
        {
            using (RegistryKey startupKey = Registry.CurrentUser.OpenSubKey(StartupApproved_RUNKEY))
            {
                string[] valueNames = startupKey.GetValueNames();
                ApprovedStartUpPrograms = valueNames
                    .Where(valueName => startupKey.GetValueKind(valueName) == RegistryValueKind.Binary)
                    .Select(valueName => new ApprovedStartUpProgram 
                    { 
                        KeyName = valueName, 
                        Value = (byte[])startupKey.GetValue(valueName) 
                    }).ToList();
            }
        }
        public static void GetAndDisableAllStartupPrograms()
        {
            using (RegistryKey startupKey = Registry.CurrentUser.OpenSubKey(StartupApproved_RUNKEY,true))
            {
                string[] valueNames = startupKey?.GetValueNames();
                if (valueNames != null && valueNames.Any())
                {
                    ApprovedStartUpPrograms = valueNames?
                        .Where(valueName => startupKey?.GetValueKind(valueName) == RegistryValueKind.Binary)?
                        .Select(valueName => new ApprovedStartUpProgram
                        {
                            KeyName = valueName,
                            Value = (byte[])startupKey?.GetValue(valueName)
                        })?.ToList();
                    valueNames?.ToList()?.ForEach(v =>
                    {
                        try
                        {
                            startupKey?.SetValue(v, DisableValue);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            throw;
                        }
                    });
                }
            }
        }
        public static void RestoreAllStartupPrograms()
        {
            using (RegistryKey startupKey = Registry.CurrentUser.OpenSubKey(StartupApproved_RUNKEY,true))
            {
                ApprovedStartUpPrograms?.ToList()?.ForEach(v =>
                {
                    try
                    {
                        startupKey?.SetValue(v.KeyName, v.Value);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        //ignore
                    }
                });
            }
        }
        public static void SetStartup(string AppName, bool enable)
        {
            RegistryKey startupKey = Registry.LocalMachine.OpenSubKey(RUNKEY);
            if (enable)
            {
                if (startupKey.GetValue(AppName) == null)
                {
                    startupKey.Close();
                    startupKey = Registry.LocalMachine.OpenSubKey(RUNKEY, true);
                    // Add startup reg key
                    startupKey.SetValue(AppName, AppName);
                    startupKey.Close();
                }
            }
            else
            {
                // remove startup
                startupKey = Registry.LocalMachine.OpenSubKey(RUNKEY, true);
                startupKey.DeleteValue(AppName, false);
                startupKey.Close();
            }
        }
        public static void GetAllStartUpProgramsWMI()
        {
            ManagementClass mangnmt = new ManagementClass("Win32_StartupCommand");
            ManagementObjectCollection mcol = mangnmt.GetInstances();
            var re = mcol.Cast<object>();
            foreach (ManagementObject strt in mcol)
            {
                string[] lv = new string[5];
                lv[0] = strt["Caption"].ToString();
                lv[1] = strt["Location"].ToString();
                lv[2] = strt["Command"].ToString();
                lv[3] = strt["Description"].ToString();
                lv[4] = strt["User"].ToString();
            }
        }


    }
    public class ApprovedStartUpProgram
    {
        public string KeyName { get; set; }
        public byte[] Value { get; set; }
        public override string ToString()
        {
            return KeyName;
        }

    }
}
