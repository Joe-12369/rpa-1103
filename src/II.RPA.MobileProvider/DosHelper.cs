using System;
using System.Diagnostics;

namespace II.RPA.MobileProvider
{
    public static class DosHelper
    {
        public static string ExcuteCMD(string cmd)
        {
            string output = "";
            string system32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
            using (Process pc = new Process())
            {
                cmd = cmd.Trim().TrimEnd('&') + "&exit";
                pc.StartInfo.FileName = "cmd.exe";
                pc.StartInfo.CreateNoWindow = true;
                pc.StartInfo.RedirectStandardError = true;
                pc.StartInfo.RedirectStandardInput = true;
                pc.StartInfo.RedirectStandardOutput = true;
                pc.StartInfo.UseShellExecute = false;
                pc.StartInfo.WorkingDirectory = system32Path;
                pc.Start();
                pc.StandardInput.WriteLine(cmd);
                pc.StandardInput.AutoFlush = true;
                pc.StandardInput.Close();
                output = pc.StandardOutput.ReadToEnd();
                int count = output.IndexOf(cmd) + cmd.Length;
                output = output.Remove(0, count);
                output = output.Trim();
                pc.WaitForExit();
                pc.Close();
            }
            return output;
        }

        public static string Execute(string command)
        {
            string output = "";
            if (command != null && !command.Equals(""))
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C " + command;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
                try
                {
                    if (process.Start())
                    {
                        process.WaitForExit(2000);
                        output = process.StandardOutput.ReadToEnd();
                    }
                }
                catch (Exception em)
                {
                    //Logging.Warn("Dos命令执行失败", em);
                }
                finally
                {
                    process?.Close();
                }
            }
            return output;
        }
    }
}
