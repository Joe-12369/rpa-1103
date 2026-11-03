using AutoIt;
using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static II.RPA.Plugin.Core.NativeMethods;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch
{
    public class WindowObject : WindowInfo
    {
        public WindowObject(IntPtr handle) => this.Handle = handle;
        private static ILogger logger = Logger.OfContext();



        #region 业务字段
        private AutomationElement automationElement;
        private List<WindowObject> childWindows = null;
        #endregion

        #region 业务属性
        #region 转UIA 
        public AutomationElement ToAutomationElement()
        {
            return UiCommon.UIAAutomation.FromHandle(this.Handle);
        }
        #endregion

        public bool IsDesktop => this.Handle == NativeMethods.GetDesktopWindow();
        public WindowType WindowType { get; set; }
        public eGuiType GuiType
        {
            get
            {
                if (this.WindowType == (Commons.WindowMatch.WindowType.IE | Commons.WindowMatch.WindowType.Windows))
                {
                    return eGuiType.IE;
                }
                return (eGuiType)Enum.Parse(typeof(eGuiType), WindowType.ToString(), true);
            }
        }
        /// <summary>
        /// 判断窗口是否可见 IsWindowVisible & ClientRect.T|L|R|B > 0 
        /// </summary>
        public bool IsVisible
        {
            get
            {
                NativeMethods.GetClientRect(this.Handle, out var rect);
                return this.IsWindowVisible && rect.Right != 0 & rect.Bottom != 0;
            }
        }
        public bool IsVisibleEnabled
        {
            get
            {
                var ve = this.IsVisible && this.IsWindowEnabled;
                if (!ve)
                {
                    logger.LogDebug($"IsVisibleEnabled ：false");
                }
                return ve;
            }
        }
        public AutomationElement AutomationElement
        {
            get => automationElement = automationElement ?? ToAutomationElement();
            set => automationElement = value;
        }
        public List<WindowObject> ChildWindows
        {
            get => childWindows = childWindows ?? FindAllChildWindowEx(null).ToList();
            set => childWindows = value;
        }
        #endregion

        #region 使用功能EnumAllChildWindows 遍历子窗口

        public List<WindowObject> EnumAllChildWindows()
        {
            return EnumAllChildWindows(null, false);
        }
        public List<WindowObject> EnumAllChildWindows(Func<WindowObject, bool> predicate)
        {
            return EnumAllChildWindows(predicate, false);
        }
        public List<WindowObject> EnumAllChildWindows(Func<WindowObject, bool> predicate, bool isQueryFirst)
        {
            List<WindowObject> wndInfoList = new List<WindowObject>();
            Func<IntPtr, IntPtr, bool> callBack = null;
            callBack = (testWindowHandle, includeChildren) =>
            {
                if (isQueryFirst && wndInfoList.Count > 0)
                    return false;
                var wndInfo = new WindowObject(testWindowHandle);
                logger.LogDebugBlock(() =>
                {
                    logger.LogDebug($"遍历子窗口：{wndInfo.Handle}({wndInfo.Handle.ToString("x4")}),{wndInfo.ClassName}");
                });
                if (predicate == null || predicate(wndInfo))
                {
                    wndInfoList.Add(wndInfo);
                }
                return true;
            };
            UiCommon.EnumChildWindows(this.Handle, new UiCommon.EnumWindowsProc(callBack), IntPtr.Zero);
            return wndInfoList.Distinct().ToList();
        }

        #endregion

        #region 使用GetWindow遍历子窗口
        /// <summary>
        /// 使用GetWindow遍历子窗口  不支持嵌套子窗口 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IEnumerable<WindowObject> GetAllChildWindows(Func<WindowObject, bool> predicate)
        {
            return GetAllChildWindows(this.Handle, predicate, 1, 1);
        }
        /// <summary>
        /// 使用GetWindow遍历子窗口
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="maxlayers">子窗口最大嵌套层数</param>
        /// <returns></returns>
        public IEnumerable<WindowObject> GetAllChildWindows(Func<WindowObject, bool> predicate, uint maxlayers)
        {
            return GetAllChildWindows(this.Handle, predicate, maxlayers, 1);
        }
        /// <summary>
        /// 使用GetWindow遍历子窗口
        /// </summary>
        /// <param name="parentWnd"></param>
        /// <param name="predicate"></param>
        /// <param name="maxlayers">>子窗口最大嵌套层数</param>
        /// <param name="currentLayers">将要获取内嵌窗口所在层数</param>
        /// <returns></returns>
        public IEnumerable<WindowObject> GetAllChildWindows(IntPtr parentWnd, Func<WindowObject, bool> predicate, uint maxlayers, uint currentLayers)
        {
            if (maxlayers <= 1)
                maxlayers = 1;
            if (currentLayers < 1)
                currentLayers = 1;
            if (parentWnd != IntPtr.Zero)
            {
                currentLayers++;
                var hWndChild = NativeMethods.GetWindow(parentWnd, (uint)GET_WINDOW_CMD.GW_CHILD);
                while (hWndChild != IntPtr.Zero)
                {
                    var wndInfo = new WindowObject(hWndChild);
                    if (predicate == null || predicate(wndInfo))
                    {
                        logger.LogDebugBlock(() =>
                        {
                            logger.LogDebug($"符合要求子窗口：{wndInfo.Handle}({wndInfo.Handle.ToString("x4")}),{wndInfo.ClassName}");
                        });
                        yield return wndInfo;
                    }
                    if (currentLayers <= maxlayers)
                        foreach (var wndObj in FindAllChildWindowEx(hWndChild, predicate, maxlayers, currentLayers))
                        {
                            yield return wndObj;
                        }
                    ;
                    hWndChild = NativeMethods.GetWindow(hWndChild, (uint)GET_WINDOW_CMD.GW_HWNDNEXT);
                }
            }
        }


        #endregion

        #region 使用FindWindowEx遍历子窗口
        /// <summary>
        /// 使用FindWindowEx遍历子窗口  不支持嵌套子窗口 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IEnumerable<WindowObject> FindAllChildWindowEx(Func<WindowObject, bool> predicate)
        {
            return FindAllChildWindowEx(this.Handle, predicate, 1, 1);
        }
        /// <summary>
        /// 使用FindWindowEx遍历子窗口
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="maxlayers">子窗口最大嵌套层数</param>
        /// <returns></returns>
        public IEnumerable<WindowObject> FindAllChildWindowEx(Func<WindowObject, bool> predicate, uint maxlayers)
        {
            return FindAllChildWindowEx(this.Handle, predicate, maxlayers, 1);
        }
        /// <summary>
        /// 使用FindWindowEx遍历子窗口 
        /// </summary>
        /// <param name="parentWnd"></param>
        /// <param name="predicate"></param>
        /// <param name="maxlayers">子窗口最大嵌套层数</param>
        /// <param name="currentLayers">将要获取内嵌窗口所在层数</param>
        /// <returns></returns>
        public IEnumerable<WindowObject> FindAllChildWindowEx(IntPtr parentWnd, Func<WindowObject, bool> predicate, uint maxlayers, uint currentLayers = 1)
        {
            if (maxlayers < 1)
                maxlayers = 1;
            if (currentLayers < 1)
                currentLayers = 1;
            if (parentWnd != IntPtr.Zero)
            {
                currentLayers++;
                var hWndChild = NativeMethods.FindWindowEx(parentWnd, IntPtr.Zero, null, null);
                while (hWndChild != IntPtr.Zero)
                {
                    var wndInfo = new WindowObject(hWndChild);
                    logger.LogDebugBlock(() =>
                    {
                        logger.LogDebug($"遍历子窗口：{wndInfo.Handle}({wndInfo.Handle.ToString("x4")}),{wndInfo.ClassName}");
                    });
                    if (predicate == null || predicate(wndInfo))
                    {
                        yield return wndInfo;
                    }
                    if (currentLayers <= maxlayers)
                        foreach (var wndObj in FindAllChildWindowEx(hWndChild, predicate, maxlayers, currentLayers))
                        {
                            yield return wndObj;
                        }
                    ;
                    hWndChild = NativeMethods.FindWindowEx(parentWnd, hWndChild, null, null);
                }
            }
        }
        #endregion

        #region Activate
        public virtual void Activate(int delay = 1000)
        {
            if (this.Handle == IntPtr.Zero)
            {
                return;
            }
            if (this.ClassName == "#32769" ||
                (this.ProcessName == "explorer" && (this.ClassName == "WorkerW" || this.ClassName == "Progman")))
            {
                AutoItHelper.Send("#m", 0);
                Task.Delay(delay).Wait();
                return;
            }
            if (IsNeedRestoreWindow(this.Handle))
            {
                //已经是最大化时调用SW_RESTORE会导致窗口还原，所以此处需要判断下
                NativeMethods.ShowWindow(this.Handle, ShowWindowTypes.SW_RESTORE);
            }
            if (IsNeedForeground(this.Handle))
            {
                NativeMethods.SetForegroundWindow(this.Handle);
                RPAUtils.Retry(() => GetForegroundWindow() == this.Handle, 500, 10);
            }
            RPAUtils.Retry(delegate
            {
                if (AutoItX.WinActive(this.Handle) == 0)
                {
                    AutoItX.WinActivate(this.Handle);
                    return false;
                }
                return true;
            }, delay);
        }

        public bool IsNeedForeground(IntPtr hWnd)
        {
            IntPtr curForegroundHWnd = NativeMethods.GetForegroundWindow();
            if (curForegroundHWnd == hWnd)
            {
                return false;
            }
            var handle = curForegroundHWnd;
            List<IntPtr> handles = new List<IntPtr>
            {
                handle
            };
            while (handle != IntPtr.Zero)
            {
                handle = NativeMethods.GetParent(handle);
                handles.Add(handle);
            }
            if (handles.Contains(hWnd))
            {
                return false;
            }
            if (new WindowObject(curForegroundHWnd).RootWindowHandle == this.RootWindowHandle)
            {
                return false;
            }
            return true;
        }
        private static bool IsNeedRestoreWindow(IntPtr hWnd)
        {
            var state = NativeMethods.GetPlacement(hWnd).showCmd;
            if (state == ShowWindowCommands.Hide || state == ShowWindowCommands.Minimized)
            {
                return true;
            }
            return false;
        }
        #endregion

        #region 打印窗口信息
        public void PrintWindowInfo()
        {
            try
            {
                logger.LogDebugBlock(() =>
                {
                    logger.LogDebug($"========================窗口信息如下===========================");
                    logger.LogDebug($"======句柄：{this.Handle}({Handle.ToString("x4")})");
                    logger.LogDebug($"======ClassName：{this.ClassName}");
                    logger.LogDebug($"======ProcessId：{this.ProcessId}");
                    logger.LogDebug($"======ProcessName：{this.ProcessName}");
                    logger.LogDebug($"======IsIconic：{this.IsIconic}");
                    logger.LogDebug($"======WindowText：{this.Title}");
                    logger.LogDebug($"======AutomationName：{this.ToAutomationElement().Properties.Name.ValueOrDefault}");
                    logger.LogDebug($"===============================================================");
                    logger.LogDebug($"===============================================================");
                });
            }
            catch { }
        }
        #endregion
    }
}
