using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using AutoIt;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Window.Params;
using II.RPA.Plugin.Window.Plugins;
using II.RPA.WinAPI;
using II.RPA.WinAPI.Struct;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static II.RPA.Plugin.Window.Core.User32X;
using GET_WINDOW_CMD = II.RPA.Plugin.Window.Core.User32X.GET_WINDOW_CMD;
using II.RPA.Plugin.GUI.Commons.WindowMatch;

namespace II.RPA.Plugin.Window.Core
{
    internal static class WindowOperator
    {
        public static IList<WindowObject> ListAllWindows(bool visibalOnly = true)
        {
            DesktopWindow desktopWindow = new DesktopWindow();
            var result = desktopWindow.EnumAllChildWindows((wnd) =>
            {
                var ignore = (!wnd.IsWindowVisible && visibalOnly) ||
                        !wnd.IsWindow ||
                        string.IsNullOrWhiteSpace(wnd.Title) ||
                        wnd.ParentHandleByGetAncestor != desktopWindow.Handle;
                        User32X.IsInvisibleWin10Background(wnd.Handle);
                return !ignore;
            });

            var loopList = result.ToList();

            foreach (var item in loopList)
            {
                var owner = User32.GetWindow(item.Handle, (uint)GET_WINDOW_CMD.GW_OWNER);
                if (owner != IntPtr.Zero && result.Any(w => w.Handle == owner))
                {
                    result.Remove(item);
                }
            }
            return result;
        }
        public static WindowData FindWindosByKeywords(string[] kseywords, GetWindowDataList_MatchType matchType)
        {
            var wndDatas = FindByKeywords(kseywords, matchType);
            if (wndDatas.Count == 0)
            {
                throw new Exception(Localize($"找不到窗口 {matchType.ToString()},{kseywords.ToJson()}", "controlType_WindowOperator_184F711"));
            }
            return wndDatas.FirstOrDefault();
        }
        public static List<WindowData> FindByKeywords(string[] kseywords, GetWindowDataList_MatchType matchType)
        {
            var wnds = ListAllWindows().ToList();
            if (kseywords?.Length > 0)
            {
                var excludedNames = new[] { "实在设计器", "Z-Factory", "Z-Bot", "Program Manager", "MainForm" };
                wnds.RemoveAll(wnd =>
                {
                    var title = wnd.Title;
                    if (excludedNames.Any(name => name.Equals(title)))
                    {
                        return true;
                    }
                    switch (matchType)
                    {
                        case GetWindowDataList_MatchType.Contains:
                            return !kseywords.Any(keyword => title.Contains(keyword));
                        case GetWindowDataList_MatchType.Regex:
                            return !kseywords.Any(keyword => title.EqualsOrIsMatch(keyword));
                        case GetWindowDataList_MatchType.Equals:
                            return !kseywords.Any(keyword => title.Equals(keyword));
                        default: return true;
                    }
                });
            }
            return wnds.Select(hWnd => FromWindowObject(hWnd)).ToList();
        }

        public static WindowData FindByTitle(string title)
        {
            var wnd = ListAllWindows()
                .Where(x => x.Title == title)
                .FirstOrDefault();

            if (wnd == null)
            {
                throw new Exception(Localize($"找不到窗口 {title}", "controlType_WindowOperator_C75A7EA2"));
            }

            return FromWindowObject(wnd);
        }
        public static List<WindowData> FindAllByTitle(string title)
        {
            var wnds = ListAllWindows()
                .Where(x => x.Title == title)
                .ToList();

            if (wnds.Count == 0)
            {
                throw new Exception(Localize($"找不到窗口 {title}", "controlType_WindowOperator_C75A7EA2"));
            }

            return wnds.Select(wnd => FromWindowObject(wnd)).ToList();
        }

        public static WindowData FindByActive()
        {
            var intPtr = User32.GetForegroundWindow();
            return FromWindowObject(new WindowObject(intPtr));
        }

        public static WindowData MatchByData(WindowData data)
        {
            var list = ListAllWindows(false);

            var wnd = list
                .Where(x => x.Handle.ToString() == data.WindowHandle)
                .FirstOrDefault();

            if (wnd != null)
            {
                return FromWindowObject(wnd);
            }

            wnd = list
                .Where(x => x.Title == data.Title)
                .FirstOrDefault();

            if (wnd != null)
            {
                return FromWindowObject(wnd);
            }

            throw new Exception(Localize($"找不到窗口 {data.Title} ({data.WindowHandle})", "controlType_WindowOperator_FB53C1D1"));
        }

        public static WindowData FromWindowObject(WindowObject wndObj)
        {
            var windowName = wndObj.Title;
            var windowClass = wndObj.ClassName;


            var processName = string.Empty;

            try
            {
                processName = wndObj.ProcessName;

            }
            catch
            {
            }
            var result = new WindowData
            {
                WindowHandle = wndObj.Handle.ToString(),
                Title = windowName,
                ProcessId = (int)wndObj.ProcessId,
                Class = windowClass,
                ProcessName = processName,
                WindowSize = (new { Width = wndObj.WindowRect.Width, Height = wndObj.WindowRect.Height }).ToJson(),
                WindowPoint = (new { X = wndObj.WindowRect.X, Y = wndObj.WindowRect.Y }).ToJson()
            };
            return result;
        }

        public static string GetWindowClass(IntPtr intPtr)
        {
            var classSb = new StringBuilder(1024);
            User32.GetClassName(intPtr, classSb, classSb.Capacity);
            return classSb.ToString();
        }

        public static string GetWindowTitle(IntPtr intPtr)
        {
            int capacity = User32.GetWindowTextLength(intPtr) * 2 + 1;
            var desSb = new StringBuilder(capacity);
            User32.GetWindowText(intPtr, desSb, desSb.Capacity);
            return desSb.ToString();
        }

        public static RECT GetWindowRect(IntPtr intPtr)
        {
            RECT rECT;
            User32.GetWindowRect(intPtr, out rECT);
            return rECT;
        }

        public static void Activate(WindowData windowData, bool isMaximize = false)
        {
            int waitMs = 2000;

            if (windowData.Class == "#32768" || windowData.Class == "ComboLBox")
            {
                return;
            }
            if (windowData.Class == "#32769" ||
                windowData.ProcessName == "explorer" && (windowData.Class == "WorkerW" || windowData.Class == "Progman"))
            {
                AutoItX.Send("#m");
                Thread.Sleep(waitMs);
            }
            else
            {
                if (windowData.Class == "Chrome_WidgetWin_2")
                {
                    return;
                }

                if (int.TryParse(windowData.WindowHandle, out var handle))
                {
                    IntPtr winHandle = new IntPtr(handle);

                    Retry((() =>
                    {
                        if (AutoItX.WinActive(winHandle) != 0)
                        {
                            if (isMaximize)
                            {
                                AutoItX.WinSetState(winHandle, AutoItX.SW_MAXIMIZE);
                            }
                            return true;
                        }
                        AutoItX.WinActivate(winHandle);
                        return false;
                    }), waitMs);
                }
            }
        }

        public static WindowData FindBySelecter(string selecter)
        {
            JToken token = JToken.Parse(selecter);
            string strSelector = "";
            if (token is JObject jobj)
            {
                strSelector = jobj.GetValue("selector").ToString();
            }
            var winOperator = GuiRuntime.WindowsOperator;
            var rootElement = winOperator.BuilderAutomationElement(strSelector, null);
            if (rootElement != null)
            {
                var rthandle = rootElement.Properties.NativeWindowHandle.ValueOrDefault;
                return FromWindowObject(new WindowObject(rthandle));
            }
            else
            {
                throw new Exception(Localize($"根据元素结构未找不到窗口:{selecter}", "controlType_WindowOperator_9B473E14"));
            }
        }

        public static bool IsWindowExists(WindowData windowData)
        {
            if (windowData == null)
            {
                return false;
            }
            // 使用IsWindow函数检查窗口是否存在  
            //windowData = WindowOperator.MatchByData(windowData);
            long.TryParse(windowData.WindowHandle, out long numHandle);
            IntPtr handle = new IntPtr(numHandle);
            return User32.IsWindow(handle);
        }

        public static bool WaitWindowStatus(ParamWaitWindowModel paramModel)
        {
            var waitType = paramModel.WaitType;
            WindowData windowData;
            try
            {
                if (paramModel.SwitchType.ToLower() == "selecter")
                {
                    windowData = WindowOperator.FindBySelecter(paramModel.Selecter);
                }
                else if (paramModel.SwitchType.ToLower() == "title")
                {
                    windowData = WindowOperator.FindByKeywords(paramModel.Keywords, paramModel.MatchType).FirstOrDefault();
                    if (windowData == null)
                    {
                        throw new Exception(Localize($"找不到窗口 {paramModel.Keywords?.ToJson()}", "controlType_WindowOperator_FF37F51D"));
                    }
                }
                else
                {
                    windowData = paramModel.WindowData.ToObject<WindowData>();
                    if (windowData.Title == "DeskTop")
                    {
                        if (waitType == EnumWaitType.Appear)
                        {
                            return true;
                        }
                        else if (waitType == EnumWaitType.DisAppear)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        windowData = WindowOperator.MatchByData(windowData);
                    }
                }
            }
            catch
            {
                if (waitType == EnumWaitType.DisAppear)
                {
                    return true;
                }
                return false;
            }
            if (waitType == EnumWaitType.Appear)
            {
                // windowData = WindowOperator.MatchByData(windowData);
                long.TryParse(windowData.WindowHandle, out long numHandle);
                IntPtr handle = new IntPtr(numHandle);
                return User32.IsWindow(handle) && User32.IsWindowVisible(handle);
            }
            else if (waitType == EnumWaitType.DisAppear)
            {
                try
                {
                    return !IsWindowExists(windowData);
                }
                catch
                {
                    return true;
                }
            }
            else
            {
                //windowData = WindowOperator.MatchByData(windowData);
                long.TryParse(windowData.WindowHandle, out long numHandle);
                IntPtr handle = new IntPtr(numHandle);
                // 获取当前活动窗口的句柄  
                IntPtr hwndForeground = User32.GetForegroundWindow();
                if (User32.IsWindow(handle) && User32.IsWindowVisible(handle))
                {
                    return handle == hwndForeground;
                }
                return false;
            }
        }

        public static bool Retry(
            Func<bool> func,
            int timeout,
            int times = -1,
            int interval = 200,
            int delay = 0)
        {
            if (delay > 0)
            {
                Thread.Sleep(delay);
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            while (!func())
            {
                --times;
                Thread.Sleep(interval);
                if (stopwatch.ElapsedMilliseconds >= (long)timeout && times != 0)
                {
                    stopwatch.Stop();
                    return false;
                }
            }

            return true;
        }
    }
}
