using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace II.RPA.MobileDeviceManager.Common
{
    public static class DosHelper
    {
        public static string ExcuteCMD(string cmd)
        {
            string output = "";
            using (Process pc = new Process())
            {
                cmd = cmd.Trim().TrimEnd('&') + "&exit";
                pc.StartInfo.FileName = "cmd.exe";
                pc.StartInfo.CreateNoWindow = true;
                pc.StartInfo.RedirectStandardError = true;
                pc.StartInfo.RedirectStandardInput = true;
                pc.StartInfo.RedirectStandardOutput = true;
                pc.StartInfo.UseShellExecute = false;
                pc.Start();
                pc.StandardInput.WriteLine(cmd);
                pc.StandardInput.AutoFlush = true;
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
        public static void StartMobileServer(string command)
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C " + command,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8
            };
            process.EnableRaisingEvents = true;
            //process.ErrorDataReceived += process_ErrorDataReceived;
            //process.OutputDataReceived += process_OutputDataReceived;
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.StandardInput.AutoFlush = false;
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
