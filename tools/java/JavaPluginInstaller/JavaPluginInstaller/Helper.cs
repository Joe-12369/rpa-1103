using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace JavaPluginInstaller
{
    public class InstallerException : Exception
    {
        public JavaExtensionStatue Statue { get; set; }
        public InstallerException(string message) : base(message) { }
    }

    public class Helper
    {
        private static List<Tuple<int, string, string>> SearchJVM()
        {
            var processPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", Environment.Is64BitOperatingSystem ? "Listdlls64.exe" : "Listdlls.exe"));
            var listDLLProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = processPath,
                    Arguments = "-accepteula -d jvm.dll",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            listDLLProcess.Start();
            var readResult = listDLLProcess.StandardOutput.ReadToEndAsync();
            readResult.Wait(10000);
            if (readResult.IsCompleted)
            {
                return GetResult(Regex.Split(readResult.Result, "\r?\n|\r").ToList());
            }
            listDLLProcess.Kill();
            throw new InstallerException("Listdlls timeout") { Statue = JavaExtensionStatue.ListdllsTimeout };
        }

        private static List<Tuple<int, string, string>> GetResult(List<string> lines)
        {
            var startLines = lines.Select((l, idx) => new Tuple<string, int>(l, idx)).Where(i => !string.IsNullOrWhiteSpace(i.Item1) && i.Item1.All(c => c == '-')).ToList();
            return startLines.Select(i =>
            {
                var pidLine = lines.ElementAtOrDefault(i.Item2 + 1);
                int.TryParse(new Regex(@"pid:\s(?<pid>\d+)$").Match(pidLine)?.Groups["pid"]?.Value, out var pid);
                var commandLine = lines.ElementAtOrDefault(i.Item2 + 2);
                var command = new Regex(@"^Command\sline:\s(?<command>.+)$").Match(commandLine)?.Groups["command"]?.Value;
                var dllLine = lines.ElementAtOrDefault(i.Item2 + 5);
                var dll = new Regex(@"^[^\s]+\s+[^\s]+\s+(?<dll>.+)$").Match(dllLine)?.Groups["dll"]?.Value;
                if (pid < 1 || string.IsNullOrWhiteSpace(dll)) 
                {
                    throw new InstallerException("Parse Listdlls result failed") { Statue = JavaExtensionStatue.ListdllsParseFail }; ;
                }
                return new Tuple<int, string, string>(pid, command, dll);
            }).ToList();
        }

        private static bool GetJvmPathByProcessName(out string path, out JavaExtensionStatue mes)
        {
            string[] javaList = new string[] { "javaw", "java" };
            path = "";
            foreach (var java in javaList)
            {
                var process = Process.GetProcessesByName(java);
                if (process == null || process.Length == 0)
                {
                    continue;
                }
                if (process.Length > 1)
                {
                    mes = JavaExtensionStatue.MoreJvm;
                    return false;
                }
                path = process[0].MainModule.FileName;
                path = path.Replace("javaw.exe", "java.exe");
                mes = JavaExtensionStatue.JavaExtensionInstallSuccess;
                if (!string.IsNullOrEmpty(path))
                    return true;
            }
            mes = JavaExtensionStatue.UnJvm;
            return false;
        }
        public static bool GetJvmPath(out string path, out JavaExtensionStatue mes, out object extraInfo)
        {
            path = "";
            extraInfo = null;
            try
            {
                var jvms = SearchJVM();
                if (jvms.Count() < 1)
                {
                    mes = JavaExtensionStatue.UnJvm;
                    return false;
                }
                if (jvms.GroupBy(j => j.Item3).Count() > 1)
                {
                    mes = JavaExtensionStatue.MoreJvm;

                    var jvmInfos =

                    extraInfo = 
                    jvms.Where(j =>
                        {
                            var p = Process.GetProcessById(j.Item1);
                            return p != null && !p.HasExited;
                        })
                        .Select(j => 
                        {
                            return new JvmInfo 
                            {
                                Pid = j.Item1,
                                AppName = Process.GetProcessById(j.Item1).ProcessName,
                                JvmPath = j.Item3
                            };
                        });

                    return false;
                }
                path = Path.Combine(new FileInfo(jvms.FirstOrDefault().Item3).Directory.Parent.FullName, "java.exe");
                mes = JavaExtensionStatue.JavaExtensionInstallSuccess;
                return true;
            }
            catch (Exception ex)
            {
                Program.LogError(ex);
                return GetJvmPathByProcessName(out path, out mes);
            }

        }

        /// <summary>
        /// E:\ai\Java\jdk1.8.0_291\jre\bin\java.exe -version
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static JavaInfo GetJvmVersion(string path)
        {
            JavaInfo javaInfo = new JavaInfo();
            var p = new Process();
            if (string.IsNullOrEmpty(path))
                p.StartInfo.FileName = "java";
            else
                p.StartInfo.FileName = path;
            p.StartInfo.Arguments = "-version";
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            var result = p.StandardError.ReadToEnd();
            p.Close();
            javaInfo.Version = result.Split(' ', '\r')[2].Replace("\"", "");
            javaInfo.IsJvm64 = result.Contains("64-Bit");
            javaInfo.BinPath = Path.GetDirectoryName(path);
            return javaInfo;
        }
    }
}
