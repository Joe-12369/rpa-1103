using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Windows.Config;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Windows.Core
{
    internal class AccEnabler
    {
        private static ILogger logger = Logger.OfContext();
        private static readonly object ojb_lock;
        private static List<uint> accEnablePidList;
        static AccEnabler()
        {
            ojb_lock = new object();
            accEnablePidList = new List<uint>();
        }
        const string ShadowBotNativeHelperDllPath = "DuiLibAccEnabler.dll";
        [DllImport(ShadowBotNativeHelperDllPath, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
        extern static bool IsMatch(uint dwDstProcessId);
        [DllImport(ShadowBotNativeHelperDllPath, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
        extern static bool CheckForEnableAccessibility(uint dwDstProcessId);

        public static bool IsAccEnablerApps(AutomationElement element)
        {
            if (element.Properties.ProcessId.TryGetValue(out var pid))
            {
                var processName = UiCommon.GetProcessNameById(pid);
                return WinConfigurations.Info.AccEnablerApps.Contains(processName, StringComparer.OrdinalIgnoreCase);
            }
            return false;
        }
        public static bool IsMatch(AutomationElement element)
        {
            if (element.Properties.ProcessId.TryGetValue(out var pid))
            {
                return IsMatch((uint)pid);
            }
            return false;
        }
        public static void EnableOnce(AutomationElement element)
        {
            if (element.Properties.ProcessId.TryGetValue(out var pid))
            {
                EnableOnce((uint)pid);
            }
        }
        public static void EnableOnce(uint dwDstProcessId)
        {
            try
            {
                if (accEnablePidList.IndexOf(dwDstProcessId) != -1)
                {
                    return;
                }
                if (IsMatch(dwDstProcessId))
                {
                    CheckForEnableAccessibility(dwDstProcessId);
                    TryAddAccEnablePid(dwDstProcessId);
                    logger.LogDebug($"accEnable IsMatch true ,pid:{dwDstProcessId}");
                }
                else
                {
                    logger.LogDebug($"accEnable IsMatch false ,pid:{dwDstProcessId}");
                }
            }
            catch
            {

            }
        }
        private static void TryAddAccEnablePid(uint dwDstProcessId)
        {
            lock (ojb_lock)
            {
                if (accEnablePidList.IndexOf(dwDstProcessId) == -1)
                {
                    accEnablePidList.Add(dwDstProcessId);
                }
            }
        }
        internal static void ClearAccEnablePid()
        {
            lock (ojb_lock)
            {
                accEnablePidList.Clear();
            }
        }
    }
}
