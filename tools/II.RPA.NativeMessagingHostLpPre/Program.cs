using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NativeMessagingHostLpPre
{
    class Program
    {
        static void Main(string[] args)
        {
            Process process = new Process();
            string path = AppDomain.CurrentDomain.BaseDirectory;
            string fileName = Path.Combine(path, "II.RPA.NativeMessagingHostLp.exe");
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = fileName,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            process.StartInfo = psi;
            process.Start();
            Environment.Exit(0);
        }
    }
}
