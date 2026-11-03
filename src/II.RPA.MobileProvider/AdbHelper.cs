
using II.RPA.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.MobileProvider
{

    public class AdbDeviceHelper
    {
        public static string adbPath
        {
            get
            {
                string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
                return System.IO.Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA\\mobile\\AndroidSDK\\platform-tools\\adb.exe");
            }
        }
        public static List<DeviceInfo> GetConnectedDevices()
        {
            List<string> obj = DosHelper.Execute(adbPath + " devices").Replace("List of devices attached", "").Replace("\r\n\r\n", "")
                .Replace("\r\n", "&")
                .Replace("\t", "$")?.Split('&').ToList();
            List<DeviceInfo> devices = new List<DeviceInfo>();
            foreach (string item in obj)
            {
                List<string> deviceitem = item?.Split('$').ToList();
                if (deviceitem.Count == 2)
                {
                    string udid = deviceitem[0];
                    if (!(deviceitem[1] == "offline") || !udid.Contains("62001"))
                    {
                        bool isReady = deviceitem[1] == "device";
                        string model = GetProp(udid, "ro.product.model");
                        string version = GetProp(udid, "ro.build.version.release");
                        string brand = GetProp(udid, "ro.product.brand");
                        string name = GetProp(udid, "ro.product.name");
                        devices.Add(new DeviceInfo
                        {
                            Udid = udid,
                            IsReady = isReady,
                            //WifiIsOffline = (deviceitem[1] == "offline"),
                            Model = model,
                            Version = version?.Trim(),
                            Brand = brand,
                            Name = model
                            //Name = name
                        });
                    }
                }
            }
            return devices;
        }
        public static string GetProp(string udid, string propName)
        {
            if (string.IsNullOrEmpty(udid))
            {
                return string.Empty;
            }
            return DosHelper.Execute($"{adbPath} -s {udid} shell getprop {propName}").Replace("\r", "").Replace("\n", "");
        }


        /// <summary>
        /// 执行ADB命令
        /// </summary>
        public static async Task<string> ExecuteAdbCommandAsync(string command)
        {
            try
            {
                return await ExecuteAdbCommandCoreAsync(command, null);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static Task<string> ExecuteAdbCommandWithTimeoutAsync(string command, int timeoutMilliseconds)
        {
            if (timeoutMilliseconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
            }

            return ExecuteAdbCommandCoreAsync(command, timeoutMilliseconds);
        }

        private static async Task<string> ExecuteAdbCommandCoreAsync(string command, int? timeoutMilliseconds)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                process.Start();

                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                Task exitTask = process.WaitForExitAsync();

                if (timeoutMilliseconds.HasValue)
                {
                    Task completedTask = await Task.WhenAny(exitTask, Task.Delay(timeoutMilliseconds.Value));
                    if (completedTask != exitTask)
                    {
                        TryKillProcess(process);
                        throw new TimeoutException($"ADB命令执行超时[{timeoutMilliseconds.Value}ms]: {command}");
                    }
                }

                await exitTask;
                string output = await outputTask;
                string error = await errorTask;

                if (process.ExitCode != 0)
                {
                    throw new Exception($"ADB命令执行失败 (退出码: {process.ExitCode}): {error}");
                }

                return string.IsNullOrWhiteSpace(output) ? error : output;
            }
        }

        private static void TryKillProcess(Process process)
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch
            {
                // 超时结果优先返回，进程清理由 Process.Dispose 继续处理。
            }
        }

        public static void ExecuteAdbCommand(string command, int timeoutMilliseconds)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = command,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                if (!process.WaitForExit(timeoutMilliseconds))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }
                    throw new TimeoutException("ADB command timed out.");
                }
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"ADB command failed with exit code {process.ExitCode}.");
                }
            }
        }
    }
}
