using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.Plugin.DriverInput.Interceptor
{
    internal class AiKeyboard
    {
        public static string[] ExecuteCMD(string args, out bool sucess)
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
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.WorkingDirectory = libPath;
            process.Start();
            process.WaitForExit();
            process.Close();
            sucess = true;
            return File.ReadAllLines(keyboard.Replace("AiKeyboard.exe", "log.txt"));
        }
    }
}
