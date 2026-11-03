using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    public static class WndHelper
    {
        public static List<ElementModel> GetRootWindows(bool onlyValid = true)
        {
            var results = new List<ElementModel>();

            var currentLevel = Utils.GetCurrentProcessIntegrityInfo();

            UiCommon.EnumWindows((hWnd, lParam) =>
            {
                try
                {
                    var tid = UiCommon.GetWindowThreadProcessId(hWnd, out uint processId);

                    var targetInfo = Utils.GetProcessIntegrityInfo((int)processId);

                    bool isAdmin = targetInfo != null && targetInfo.Level >= Utils.SECURITY_MANDATORY_HIGH_RID;
                    bool isHigher = targetInfo != null && targetInfo.Level > currentLevel.Level;

                    NativeMethods.RECT rect = new NativeMethods.RECT();
                    bool visible = NativeMethods.IsWindowVisible(hWnd);
                    bool hasRect = NativeMethods.GetWindowRect(hWnd, out rect);
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;

                    if (onlyValid)
                    {
                        if (!visible || !hasRect || width <= 0 || height <= 0 || tid == 0 || isHigher)
                            return true;
                    }

                    var name = UiCommon.GetWindowText(hWnd) ?? string.Empty;
                    var automationElement = UiCommon.UIAAutomation.FromHandle(hWnd);
                    if (string.IsNullOrEmpty(name))
                    {
                        name = automationElement.Properties.Name.ValueOrDefault;
                    }
                    if (string.IsNullOrEmpty(name))
                    {
                        //todo delete
                        name = $"0x{hWnd.ToString("X")}";
                    }
                    results.Add(new ElementModel(automationElement)
                    {
                        Name = name,
                        Tag = "Window",
                        Handle = hWnd,
                        ProcessId = (int)processId,
                        IsAdmin = isAdmin,
                        IsHigher = isHigher,
                        IntegrityLevel = targetInfo?.Level ?? 0,
                        IntegrityName = targetInfo?.Name ?? string.Empty,
                        Region = new System.Drawing.Rectangle(rect.Left, rect.Top, width, height)
                    });
                }
                catch
                {

                }

                return true;
            }, IntPtr.Zero);

            return results;
        }
    }
}
