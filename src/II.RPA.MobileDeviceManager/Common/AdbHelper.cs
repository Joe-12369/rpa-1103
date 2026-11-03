using II.RPA.MobileDeviceManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.MobileDeviceManager.Common
{

    public enum MobileNavigationKey { Back, Home, RecentApps }

    public class AdbHelper
    {
        public static string adbPath
        {
            get
            {
                string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
                return System.IO.Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA\\mobile\\AndroidSDK\\platform-tools\\adb.exe");
            }
        }

        public static async Task<byte[]> CaptureScreenAsync(string udid, int timeoutMilliseconds = 15000)
        {
            if (string.IsNullOrWhiteSpace(udid)) throw new ArgumentException("设备标识不能为空", nameof(udid));
            var startInfo = new ProcessStartInfo
            {
                FileName = adbPath,
                Arguments = "-s \"" + udid.Replace("\"", "\\\"") + "\" exec-out screencap -p",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = new Process { StartInfo = startInfo })
            using (var output = new MemoryStream())
            {
                if (!process.Start()) throw new InvalidOperationException("ADB截图进程启动失败");
                Task copyTask = process.StandardOutput.BaseStream.CopyToAsync(output);
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                Task exitTask = process.WaitForExitAsync();
                Task allTask = Task.WhenAll(copyTask, exitTask);
                Task completedTask = await Task.WhenAny(allTask, Task.Delay(timeoutMilliseconds));
                if (completedTask != allTask)
                {
                    try { process.Kill(true); } catch { }
                    throw new TimeoutException($"ADB截图超过[{timeoutMilliseconds}ms]，设备[{udid}]");
                }

                await allTask;
                string error = await errorTask;
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"ADB截图失败，退出码[{process.ExitCode}]，设备[{udid}]，错误[{error}]");
                }
                return output.ToArray();
            }
        }

        public static async Task PressNavigationKeyAsync(string udid, MobileNavigationKey key, int timeoutMilliseconds = 8000)
        {
            if (string.IsNullOrWhiteSpace(udid)) throw new ArgumentException("设备标识不能为空", nameof(udid));
            string keyCode;
            switch (key)
            {
                case MobileNavigationKey.Back:
                    keyCode = "KEYCODE_BACK";
                    break;
                case MobileNavigationKey.Home:
                    keyCode = "KEYCODE_HOME";
                    break;
                case MobileNavigationKey.RecentApps:
                    keyCode = "KEYCODE_APP_SWITCH";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key));
            }
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = adbPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                process.StartInfo.ArgumentList.Add("-s");
                process.StartInfo.ArgumentList.Add(udid);
                process.StartInfo.ArgumentList.Add("shell");
                process.StartInfo.ArgumentList.Add("input");
                process.StartInfo.ArgumentList.Add("keyevent");
                process.StartInfo.ArgumentList.Add(keyCode);

                if (!process.Start()) throw new InvalidOperationException($"ADB按键进程启动失败，设备[{udid}]，按键[{keyCode}]");
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                Task allTask = Task.WhenAll(process.WaitForExitAsync(), outputTask, errorTask);
                if (await Task.WhenAny(allTask, Task.Delay(timeoutMilliseconds)) != allTask)
                {
                    try { process.Kill(true); } catch { }
                    throw new TimeoutException($"ADB按键超过[{timeoutMilliseconds}ms]，设备[{udid}]，按键[{keyCode}]");
                }

                await allTask;
                if (process.ExitCode != 0)
                {
                    string error = await errorTask;
                    string output = await outputTask;
                    throw new InvalidOperationException($"ADB按键失败，设备[{udid}]，按键[{keyCode}]，退出码[{process.ExitCode}]，错误[{error}]，输出[{output}]");
                }
            }
        }

        // Read the dimensions of the current screen frame. SurfaceOrientation describes
        // rotation relative to a device's natural orientation (which can be landscape).
        public static async Task<Rect> GetCurrentDisplaySizeAsync(string udid, int timeoutMilliseconds = 3000)
        {
            byte[] png = await CaptureScreenAsync(udid, timeoutMilliseconds);
            return GetPngDisplaySize(png);
        }

        internal static Rect GetPngDisplaySize(byte[] png)
        {
            if (png.Length < 24 || png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71
                || png[4] != 13 || png[5] != 10 || png[6] != 26 || png[7] != 10
                || png[12] != 73 || png[13] != 72 || png[14] != 68 || png[15] != 82)
            {
                throw new InvalidDataException("Screenshot is not a valid PNG");
            }

            int width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));
            int height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4));
            if (width <= 0 || height <= 0)
            {
                throw new InvalidDataException($"Screenshot has invalid dimensions[{width}x{height}]");
            }
            return new Rect(0, 0, width, height);
        }
        public static Rect GetDeviceSize(string DeviceUuid)
        {
            Match match = Regex.Match(DosHelper.Execute(adbPath + " -s " + DeviceUuid + " shell wm size"), "(\\d+)x(\\d+)");
            int width = Convert.ToInt32(match.Groups[1].Value);
            int height = Convert.ToInt32(match.Groups[2].Value);
            return new Rect(0, 0, width, height);
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
        public static bool IsExistDevice(string deviceId)
        {
            List<string> obj = DosHelper.Execute(adbPath + " devices").Replace("List of devices attached", "").Replace("\r\n\r\n", "")
                .Replace("\r\n", "&")
                .Replace("\t", "$")?.Split('&').ToList();
            foreach (string item in obj)
            {
                List<string> deviceitem = item?.Split('$').ToList();
                if (deviceitem.Count == 2)
                {
                    string udid = deviceitem[0];
                    if (deviceId == udid)
                    {
                        bool isReady = deviceitem[1] == "device";
                        return isReady;
                    }
                }
            }
            return false;
        }

        public static string GetProp(string udid, string propName)
        {
            if (string.IsNullOrEmpty(udid))
            {
                return string.Empty;
            }
            return DosHelper.Execute($"{adbPath} -s {udid} shell getprop {propName}").Replace("\r", "").Replace("\n", "");
        }


        public static void ReleaseAppium(string udid)
        {
            DosHelper.Execute(adbPath + " -s " + udid + " shell /data/local/tmp/atx-agent server --stop");
            DosHelper.Execute(adbPath + " -s " + udid + " shell am force-stop com.github.uiautomator");
            DosHelper.Execute(adbPath + " -s " + udid + " shell am force-stop io.appium.uiautomator2.server");
        }

        public static string[] GetAllAppPackagesList()
        {
            return (from item in DosHelper.Execute(adbPath + " shell pm list packages").Split(new string[1] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    select item.Substring(8, item.Length - 8)).ToArray();
        }

        public static string[] GetThirdPartyAppPackagesList()
        {
            return (from item in DosHelper.Execute(adbPath + " shell pm list packages -3").Split(new string[1] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    select item.Substring(8, item.Length - 8)).ToArray();
        }


        public static bool IsInstallMobileTools()
        {
            string androidSdkPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA\\mobile\\AndroidSDK\\platform-tools\\adb.exe");
            string appiumPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA\\mobile\\appium\\node.exe");
            string javaSdkPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA\\mobile\\JavaSDK");
            if (File.Exists(androidSdkPath) && File.Exists(appiumPath) && Directory.Exists(javaSdkPath))
            {
                return true;
            }
            return false;
        }
        public static bool IsExistsIsOnMobiles()
        {
            List<string> obj = DosHelper.Execute(adbPath + " devices").Replace("List of devices attached", "").Replace("\r\n\r\n", "")
                .Replace("\r\n", "&")
                .Replace("\t", "$")?.Split('&').ToList();

            foreach (string item in obj)
            {
                List<string> deviceitem = item?.Split('$').ToList();
                if (deviceitem.Count == 2)
                {
                    string udid = deviceitem[0];
                    if (!(deviceitem[1] == "offline") || !udid.Contains("62001"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
