using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.Core.EnumWindow;
using II.RPA.Plugin.GUI.Commons.Win32Tools;
using II.RPA.Plugin.Core.Commons;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Plugin.IE.V2.Automation.Core
{
    internal class IEHelper
    {
        private static ILogger logger = Logger.OfContext();
        public static bool HandleDownloadNotificationBarFinish(IntPtr handle)
        {
            try
            { 
                var mainHandle = NativeMethods.GetAncestor(handle, GetAncestorFlags.GetRootOwner);
                var mainWindow = UiCommon.UIAAutomation.FromHandle(mainHandle);
                var result = Retry.While<AutomationElement>(() =>
                {
                    List<ConditionBase> barCondition = new List<ConditionBase>();
                    barCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ClassName, "Frame Notification Bar"));
                    AndCondition barAnd = new AndCondition(barCondition);
                    var notifBar = mainWindow.FindAllDescendants(barAnd).FirstOrDefault();
                    return notifBar;
                }, ele => ele == null, TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(1000)); 

                if (result.Result != null)
                {
                    List<ConditionBase> directCondition = new List<ConditionBase>();
                    directCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ClassName, "DirectUIHWND"));
                    AndCondition directAnd = new AndCondition(directCondition);
                    var direct = result.Result.FindAllDescendants(directAnd).FirstOrDefault();
                    List<ConditionBase> textCondition = new List<ConditionBase>();
                    textCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ControlType, ControlType.Text));
                    AndCondition textAnd = new AndCondition(textCondition);
                    var text = direct.FindAllDescendants(textAnd).FirstOrDefault();
                    if (text.Patterns.LegacyIAccessible.Pattern.Value.IsSupported
                        && text.Patterns.LegacyIAccessible.Pattern.Value.ValueOrDefault.Contains("已完成"))
                    {
                        List<ConditionBase> closeButtonCondition = new List<ConditionBase>();
                        closeButtonCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.Name, "关闭"));
                        AndCondition closeButtonAnd = new AndCondition(closeButtonCondition);
                        var closeButton = direct.FindAllDescendants(closeButtonAnd).FirstOrDefault();
                        var openrect = closeButton.BoundingRectangle.Center();
                        AutoItHelper.MouseClick("left", openrect);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new RPAException(eErrorMessageType.Error_NotFindIEBar, ex);
            }
        }


        public static bool HandleDownloadNotificationWindowFinish(IntPtr handle)
        {
            try
            {
                var mainHandle = NativeMethods.GetAncestor(handle, GetAncestorFlags.GetRootOwner);
                Win32Window win32Window = new Win32Window(mainHandle);
                var childWindow = win32Window.QueryChildWindowAll(new List<string>() { "#32770" });
                var ieFinishWindow = childWindow.Where((e) =>
                  {
                      return e.GetText().Equals("下载完毕".I18n("IEHelper_80AC1598"));
                  });

                if (ieFinishWindow.Count() == 0)
                {
                    var desktop = NativeMethods.GetDesktopWindow();
                    win32Window = new Win32Window(desktop);
                    childWindow = win32Window.QueryChildWindowAll(new List<string>() { "#32770" });
                    ieFinishWindow = childWindow.Where((e) =>
                    {
                        return e.GetText().Equals("下载完毕".I18n("IEHelper_80AC1598"));
                    });
                }
                if (ieFinishWindow.Count() == 0)
                    return false;

                var mainWindow = UiCommon.UIAAutomation.FromHandle(ieFinishWindow.FirstOrDefault().Handle);
                logger.LogDebug($"waitting #32770 window");
                if (mainWindow != null)
                {
                    List<ConditionBase> textCondition = new List<ConditionBase>();
                    textCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.Name, "下载完毕".I18n("IEHelper_80AC1598")));
                    AndCondition textAnd = new AndCondition(textCondition);
                    var text = mainWindow.FindAllDescendants(textAnd).FirstOrDefault();
                    logger.LogDebug($"element {text == null}");
                    if (text != null)
                    {
                        List<ConditionBase> checkBoxButtonCondition = new List<ConditionBase>();
                        checkBoxButtonCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ControlType, ControlType.CheckBox));
                        AndCondition checkBoxButtonAnd = new AndCondition(checkBoxButtonCondition);
                        var checkBoxButton = mainWindow.FindAllDescendants(checkBoxButtonAnd).FirstOrDefault();
                        if (checkBoxButton != null)
                        {
                            var checkbox = checkBoxButton.AsCheckBox();
                            logger.LogDebug($"element convert to checkbox");
                            if (checkbox != null)
                            {
                                if (checkbox.ToggleState == ToggleState.Off)
                                {
                                    List<ConditionBase> closeButtonCondition = new List<ConditionBase>();
                                    closeButtonCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.Name, "关闭"));
                                    AndCondition closeButtonAnd = new AndCondition(closeButtonCondition);
                                    var closeButton = mainWindow.FindAllDescendants(closeButtonAnd).FirstOrDefault();
                                    var openrect = closeButton.BoundingRectangle.Center();
                                    AutoItHelper.MouseClick("left", openrect);
                                    return true;
                                }
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new RPAException(eErrorMessageType.Error_NotFindIEBar, ex);
            }
        }
        public static void HandleIEDownloadNotificationWindow(IntPtr handle)
        {
            try
            {
                var mainHandle = NativeMethods.GetAncestor(handle, GetAncestorFlags.GetRootOwner);
                var mainWindow = UiCommon.UIAAutomation.FromHandle(mainHandle);
                var result = Retry.While<AutomationElement>(() =>
                {
                    List<ConditionBase> barCondition = new List<ConditionBase>();
                    barCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ClassName, "#32770"));
                    AndCondition barAnd = new AndCondition(barCondition);
                    var notifBar = mainWindow.FindAllDescendants(barAnd).FirstOrDefault();
                    return notifBar;
                }, ele => ele == null, TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(1000));
                Thread.Sleep(500);
                AutoItHelper.Send("^{s}", 0);
            }
            catch (Exception ex)
            {
                logger.LogError("Download Find '#32770' error", ex);
            }
        }
        public static void HandleIEDownloadNotificationBar(IntPtr handle)
        {
            try
            {
                var mainHandle = NativeMethods.GetAncestor(handle, GetAncestorFlags.GetRootOwner);
                var mainWindow = UiCommon.UIAAutomation.FromHandle(mainHandle);
                var result = Retry.While<AutomationElement>(() =>
                {
                    List<ConditionBase> barCondition = new List<ConditionBase>();
                    barCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ClassName, "Frame Notification Bar"));
                    AndCondition barAnd = new AndCondition(barCondition);
                    var notifBar = mainWindow.FindAllDescendants(barAnd).FirstOrDefault();
                    return notifBar;
                }, ele => ele == null, TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(1000));

                List<ConditionBase> directCondition = new List<ConditionBase>();
                directCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ClassName, "DirectUIHWND"));
                AndCondition directAnd = new AndCondition(directCondition);
                var direct = result.Result.FindAllDescendants(directAnd).FirstOrDefault();

                List<ConditionBase> comboboxButtonCondition = new List<ConditionBase>();
                comboboxButtonCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ControlType, ControlType.SplitButton));
                comboboxButtonCondition.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.LegacyIAccessible.Role, AccessibilityRole.ROLE_SYSTEM_BUTTONDROPDOWN));
                AndCondition comboboxButtonAnd = new AndCondition(comboboxButtonCondition);
                var comboboxButton = direct.FindAllDescendants(comboboxButtonAnd).FirstOrDefault();
                var openrect = comboboxButton.BoundingRectangle.Center();
                AutoItHelper.MouseClick("left", openrect);
                Thread.Sleep(500);
                AutoItHelper.Send("{a}", 0);
            }
            catch (Exception ex)
            {
                logger.LogError("Download Find 'Frame Notification Bar' error", ex);
            }
        }
        private static string GetIEInstallPath()
        {
            string key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\IEXPLORE.EXE";
            RegistryKey dkey = Registry.LocalMachine.OpenSubKey(key, false);
            string data = dkey.GetValue("").ToString();
            return data;
        }
        internal static string GetInternetExplorerVersion()
        {
            string version = null;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer"))
            {
                if (key != null)
                {
                    Object value = key.GetValue("svcVersion");
                    if (value == null)
                    {
                        value = key.GetValue("Version");
                    }
                    version = value.ToString();
                }
            }
            if (string.IsNullOrEmpty(version))
            {
                string filePath = GetIEInstallPath();
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
                version = fileVersionInfo.FileVersion;
            }
            return version;
        }
    }
}
