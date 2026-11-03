using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace II.RPA.Plugin.GUI.Deeps
{
    public static class Utils
    {
        private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private const int TOKEN_QUERY = 0x0008;
        private const int TokenIntegrityLevel = 25;

        private const int SECURITY_MANDATORY_LOW_RID = 0x1000;
        private const int SECURITY_MANDATORY_MEDIUM_RID = 0x2000;
        public const int SECURITY_MANDATORY_HIGH_RID = 0x3000;
        private const int SECURITY_MANDATORY_SYSTEM_RID = 0x4000;

        private static readonly char[] _chars =
    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

        private static readonly Random _random = new Random();

        public static string GenerateRequestId(int length = 10)
        {
            var buffer = new char[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = _chars[_random.Next(_chars.Length)];
            }
            return new string(buffer);
        }
        public static string GetElementShotPath()
        {
            var pictureRandomName = $"{_random.Next(10000, 99999)}-{ToUnixTimeSeconds(new DateTimeOffset(DateTime.UtcNow))}.png".Trim().Replace(" ", string.Empty);
            return Path.Combine(GlobleConfigurations.PackageDirectory, "res", GlobleConfigurations.CaptureUniqueID, pictureRandomName);
        }
        public static long ToUnixTimeSeconds(DateTimeOffset dateTimeOffset)
        {
            long num = dateTimeOffset.UtcDateTime.Ticks / 10000000;
            return num - 62135596800L;
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(IntPtr TokenHandle, int TokenInformationClass,
            IntPtr TokenInformation, int TokenInformationLength, out int ReturnLength);

        [DllImport("advapi32.dll")]
        private static extern IntPtr GetSidSubAuthority(IntPtr pSid, int nSubAuthority);

        [DllImport("advapi32.dll")]
        private static extern IntPtr GetSidSubAuthorityCount(IntPtr pSid);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern bool IsHungAppWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);


        public static (bool IsHigher, IntegrityInfo TargetLevel, IntegrityInfo CurrentLevel)
            CompareTargetProcessIntegrity(IntPtr hwnd)
        {
            try
            {
                GetWindowThreadProcessId(hwnd, out uint targetPid);
                if (targetPid == 0) return (false, null, null);

                var targetInfo = GetProcessIntegrityInfo((int)targetPid);
                var currentInfo = GetProcessIntegrityInfo(Process.GetCurrentProcess().Id);

                if (targetInfo == null || currentInfo == null)
                    return (false, targetInfo, currentInfo);

                return (targetInfo.Level > currentInfo.Level, targetInfo, currentInfo);
            }
            catch
            {
                return (false, null, null);
            }
        }
        public static IntegrityInfo GetProcessIntegrityInfo(int pid)
        {
            IntPtr hProcess = IntPtr.Zero;
            IntPtr hToken = IntPtr.Zero;
            IntPtr buffer = IntPtr.Zero;
            try
            {
                hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
                if (hProcess == IntPtr.Zero) return null;

                if (!OpenProcessToken(hProcess, TOKEN_QUERY, out hToken)) return null;

                GetTokenInformation(hToken, TokenIntegrityLevel, IntPtr.Zero, 0, out int length);
                buffer = Marshal.AllocHGlobal(length);

                if (!GetTokenInformation(hToken, TokenIntegrityLevel, buffer, length, out _))
                    return null;

                IntPtr pSid = Marshal.ReadIntPtr(buffer);
                byte count = Marshal.ReadByte(GetSidSubAuthorityCount(pSid));
                IntPtr pRid = GetSidSubAuthority(pSid, count - 1);
                int level = Marshal.ReadInt32(pRid);

                string name;
                if (level < SECURITY_MANDATORY_MEDIUM_RID)
                {
                    name = "Low";
                }
                else if (level < SECURITY_MANDATORY_HIGH_RID)
                {
                    name = "Medium";
                }
                else if (level < SECURITY_MANDATORY_SYSTEM_RID)
                {
                    name = "High";
                }
                else
                {
                    name = "System";
                }


                return new IntegrityInfo { Level = level, Name = name };
            }
            finally
            {
                if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
                if (hToken != IntPtr.Zero) CloseHandle(hToken);
                if (hProcess != IntPtr.Zero) CloseHandle(hProcess);
            }
        }

        public static bool IsAdminProcess(int pid)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (hProcess == IntPtr.Zero)
                return false;

            try
            {
                if (!OpenProcessToken(hProcess, TOKEN_QUERY, out IntPtr hToken))
                    return false;

                try
                {
                    GetTokenInformation(hToken, TokenIntegrityLevel, IntPtr.Zero, 0, out int length);
                    IntPtr buffer = Marshal.AllocHGlobal(length);

                    try
                    {
                        if (!GetTokenInformation(hToken, TokenIntegrityLevel, buffer, length, out _))
                            return false;

                        IntPtr pSid = Marshal.ReadIntPtr(buffer);
                        byte subAuthCount = Marshal.ReadByte(GetSidSubAuthorityCount(pSid));
                        IntPtr pRid = GetSidSubAuthority(pSid, subAuthCount - 1);
                        int integrityLevel = Marshal.ReadInt32(pRid);

                        return integrityLevel >= SECURITY_MANDATORY_HIGH_RID;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
                finally
                {
                    CloseHandle(hToken);
                }
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        public static IntegrityInfo GetCurrentProcessIntegrityInfo()
        {
            IntPtr hToken = IntPtr.Zero;
            IntPtr buffer = IntPtr.Zero;
            try
            {
                IntPtr hProcess = GetCurrentProcess();
                if (!OpenProcessToken(hProcess, TOKEN_QUERY, out hToken))
                    return null;

                GetTokenInformation(hToken, TokenIntegrityLevel, IntPtr.Zero, 0, out int length);
                buffer = Marshal.AllocHGlobal(length);

                if (!GetTokenInformation(hToken, TokenIntegrityLevel, buffer, length, out _))
                    return null;

                IntPtr pSid = Marshal.ReadIntPtr(buffer);
                byte count = Marshal.ReadByte(GetSidSubAuthorityCount(pSid));
                IntPtr pRid = GetSidSubAuthority(pSid, count - 1);
                int level = Marshal.ReadInt32(pRid);

                string name;
                if (level < SECURITY_MANDATORY_MEDIUM_RID)
                {
                    name = "Low";
                }
                else if (level < SECURITY_MANDATORY_HIGH_RID)
                {
                    name = "Medium";
                }
                else if (level < SECURITY_MANDATORY_SYSTEM_RID)
                {
                    name = "High";
                }
                else
                {
                    name = "System";
                }


                return new IntegrityInfo { Level = level, Name = name };
            }
            finally
            {
                if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
                if (hToken != IntPtr.Zero) CloseHandle(hToken);
            }
        }

        public static bool Validate(this AutomationElement automationElement)
        {
            if (automationElement == null
                || !automationElement.IsAvailable)
            {
                return false;
            }
            return automationElement.Properties.BoundingRectangle.IsSupported;
        }
        public static string ToDisplayText<T>(this IAutomationProperty<T> automationProperty)
        {
            try
            {
                bool success = automationProperty.TryGetValue(out T value);
                return success ? value == null ? string.Empty : value.ToString() : "Not Supported";
            }
            catch (Exception ex)
            {
                return $"Exception getting value ({ex.HResult})";
            }
        }
    }
    public class IntegrityInfo
    {
        public int Level { get; set; }
        public string Name { get; set; }
    }
}
