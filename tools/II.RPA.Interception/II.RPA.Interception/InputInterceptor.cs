using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace II.RPA.Interception
{
    class InputInterceptor
    {
        public static Boolean CheckDriverInstalled()
        {
            var result = ExecuteInstaller("check", out bool sucess);
            return sucess && result?.LastOrDefault()?.Contains("\"status\":0") == true;
        }

        public static string[] ExecuteInstaller(string args, out bool sucess)
        {
            string localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string libPath = Path.Combine(localPath, "IIRPA", "common", "keyboard");
            var keyboard = libPath + "\\AiKeyboard.exe";
            if (!File.Exists(keyboard))
            {
                sucess = false;
                return null;
            }

            Process process = new Process();
            process.StartInfo.FileName = keyboard;
            process.StartInfo.Arguments = args;
            //process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.WorkingDirectory = libPath;
            if (!args.Equals("check"))
            {
                process.StartInfo.Verb = "runas";
            }
            process.Start();
            process.WaitForExit();
            process.Close();
            sucess = true;
            return File.ReadAllLines(keyboard.Replace("AiKeyboard.exe", "log.txt"));
        }

        public static Boolean InstallDriver()
        {
            if (!CheckDriverInstalled())
            {
                var result = ExecuteInstaller("install", out bool sucess);
                var installState = CheckDriverInstalled();
                return installState && sucess && result?.LastOrDefault()?.Contains("\"status\":0") == true;

            }
            return true;
        }

        public static Boolean UninstallDriver()
        {
            if (CheckDriverInstalled())
            {
                var result = ExecuteInstaller("uninstall", out bool sucess);
                return sucess && result?.LastOrDefault()?.Contains("\"status\":0") == true;
            }
            return true;
        }
    }
}
