using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace II.RPA.Plugin.IE.V2.Automation.Provider.EnumWindow
{
    /// <summary>
    /// 包含枚举当前用户空间下所有窗口的方法。
    /// </summary>
    public class WindowEnumerator
    {
        /// <summary>
        /// 查找当前用户空间下所有符合条件的窗口。如果不指定条件，将仅查找可见窗口。
        /// </summary>
        /// <param name="match">过滤窗口的条件。如果设置为 null，将仅查找可见窗口。</param>
        /// <returns>找到的所有窗口信息。</returns>
        public static IReadOnlyList<WindowInfo> FindAll(Predicate<WindowInfo> match = null)
        {
            var windowList = new List<WindowInfo>();
            
            EnumWindows(OnWindowEnum, IntPtr.Zero);
            return windowList.FindAll(match ?? DefaultPredicate);

            bool OnWindowEnum(IntPtr hWnd, IntPtr lparam)
            {
                // 仅查找顶层窗口。
                //if (NativeMethods.GetParent(hWnd) == IntPtr.Zero)
                {
                    // 获取窗口类名。
                    var lpString = new StringBuilder(512);
                    NativeMethods.GetClassName(hWnd, lpString, lpString.Capacity);
                    var className = lpString.ToString();
                    //// 获取窗口标题。
                    /////有概率阻塞，目前无业务逻辑，先不获取标题
                    //var title = UiCommon.GetWindowText(hWnd).Trim();
                    // 获取窗口可见性。
                    var isVisible = NativeMethods.IsWindowVisible(hWnd);
                    // 添加到已找到的窗口列表。
                    windowList.Add(new WindowInfo(hWnd, className, isVisible));
                }

                return true;
            }
        }

        /// <summary>
        /// 默认的查找窗口的过滤条件。可见 + 非最小化 + 包含窗口标题。
        /// </summary>
        private static readonly Predicate<WindowInfo> DefaultPredicate = x => x.IsVisible;

        private delegate bool WndEnumProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32")]
        private static extern bool EnumWindows(WndEnumProc lpEnumFunc, IntPtr lParam);
 
       
    }
}
