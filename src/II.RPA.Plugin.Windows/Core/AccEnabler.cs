using FlaUI.Core.AutomationElements;
using IAccessible2Lib;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Windows.Config;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static InteropMy.UiaCore;

namespace II.RPA.Plugin.Windows.Core
{
    internal class AccEnabler
    {
        private static ILogger logger = Logger.OfContext();
        private static ConcurrentDictionary<uint, bool> IsMatchCachedResult;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        delegate bool IsMatchDelegate(uint dwDstProcessId);

        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        delegate bool CheckForEnableAccessibilityDelegate(uint dwDstProcessId);

        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        public delegate int CheckForFindBetterHitTestedDescendantDelegate(
            IntPtr hWnd, IAccessibleInternal pAccWnd, Point pnt,
            out IAccessibleInternal ppacc, [MarshalAs(UnmanagedType.Struct)] out object child_id);

        static readonly IsMatchDelegate IsMatch;
        static readonly CheckForEnableAccessibilityDelegate CheckForEnableAccessibility;
        public static readonly CheckForFindBetterHitTestedDescendantDelegate CheckForFindBetterHitTestedDescendant;

        static AccEnabler()
        {
            // 根据进程架构选择对应的 DLL
            string dllName;
            var arch = RuntimeInformation.ProcessArchitecture;
            if (arch == Architecture.Arm64)
                dllName = "AccEnabler.ARM64.dll";
            else if (arch == Architecture.X64)
                dllName = "AccEnabler.x64.dll";
            else
                dllName = "AccEnabler.dll";
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
            IntPtr hModule = LoadLibrary(dllPath);
            if (hModule == IntPtr.Zero)
                throw new DllNotFoundException($"无法加载 {dllName}");

            IsMatch = Marshal.GetDelegateForFunctionPointer<IsMatchDelegate>(
                GetProcAddress(hModule, "IsMatch"));
            CheckForEnableAccessibility = Marshal.GetDelegateForFunctionPointer<CheckForEnableAccessibilityDelegate>(
                GetProcAddress(hModule, "CheckForEnableAccessibility"));
            CheckForFindBetterHitTestedDescendant = Marshal.GetDelegateForFunctionPointer<CheckForFindBetterHitTestedDescendantDelegate>(
                GetProcAddress(hModule, "CheckForFindBetterHitTestedDescendant"));
        }

        public static bool IsNeedSeekDepthChromiumApp(AutomationElement element)
        {
            bool result = false;
            if (element != null)
            {
                var frameworkId = element.Properties.FrameworkId.ValueOrDefault;
                if (frameworkId == "Chrome")
                {
                    result = true;
                }
                else
                {
                    string className = element.Properties.ClassName.ValueOrDefault;
                    if (className == "Chrome_RenderWidgetHostHWND")
                    {
                        result = true;
                    }
                }
                if (result)
                {
                    var acc = element.Patterns.LegacyIAccessible.PatternOrDefault?.GetIAccessible();
                    if (acc != null)
                    {
                        IServiceProvider sp = (IServiceProvider)acc;
                        if (sp != null)
                        {
                            object obj;
                            sp.QueryService(typeof(IAccessibleApplication).GUID, typeof(IAccessibleApplication).GUID, out obj);
                            var app = obj as IAccessibleApplication;
                            if (app != null)
                            {
                                try
                                {
                                    var versionString = app.appVersion;
                                    if (versionString != null)
                                    {
                                        Version version = Version.Parse(versionString);
                                        if (version != null)
                                        {
                                            if (version.Major >= 83)
                                            {
                                                result = false;
                                            }
                                        }
                                    }
                                }
                                catch (Exception)
                                {

                                }
                            }
                        }
                    }

                }
            }
            return result;
        }
        public static bool IsAccEnablerApps(AutomationElement element)
        {
            if (element.Properties.ProcessId.TryGetValue(out var pid))
            {
                return IsMatch((uint)pid);
            }
            return false;
        }
        public static void EnableOnce(AutomationElement element)
        {
            var pid = element.ProcessId;
            if (pid > 0)
            {
                uint dwDstProcessId = (uint)pid;
                bool isMatchResult;
                if (IsMatchCachedResult != null)
                {
                    if (IsMatchCachedResult.TryGetValue(dwDstProcessId, out isMatchResult))
                    {
                        logger.LogDebug($"accEnable IsMatch[by cache] {isMatchResult} ,pid:{dwDstProcessId}");
                        return;
                    }
                }
                isMatchResult = IsMatch(dwDstProcessId);
                if (IsMatchCachedResult != null)
                {
                    IsMatchCachedResult[dwDstProcessId] = isMatchResult;
                }
                if (isMatchResult)
                {
                    var result = CheckForEnableAccessibility(dwDstProcessId);
                    Debug.Assert(result);
                    logger.LogDebug($"accEnable IsMatch true ,pid:{dwDstProcessId}");
                }
                else
                {
                    logger.LogDebug($"accEnable IsMatch false ,pid:{dwDstProcessId}");
                }
            }
        }
        internal static void IsMatch_BeginUseCachedResult()
        {
            IsMatchCachedResult = new ConcurrentDictionary<uint, bool>();
        }
        internal static void IsMatch_EndUseCachedResult()
        {
            IsMatchCachedResult.Clear();
            IsMatchCachedResult = null;
        }
    }
}
