using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace II.RPA.Plugin.Windows
{
    /// <summary>
    /// TMS 自定义绘制面板（TJdlsPanel 等）的 HWND 级虚拟分段。
    /// UIA 只能看到容器（Name=""、零 Pattern），子项通过 GDI Canvas.TextOut
    /// 窗口无关绘制。通过枚举子 HWND 推算每个虚拟菜单项的边界。
    /// </summary>
    internal static class TmsPanelHelper
    {
        public const string PROP_TMS_BOUNDS = "tms-bounds";

        public static bool IsTmsPanel(string className)
        {
            if (string.IsNullOrEmpty(className)) return false;
            return className.IndexOf("TJdlsPanel", StringComparison.OrdinalIgnoreCase) >= 0
                || className.IndexOf("TITSSpeedButton", StringComparison.OrdinalIgnoreCase) >= 0
                || className.IndexOf("TPopControl", StringComparison.OrdinalIgnoreCase) >= 0
                || className.IndexOf("TPopGroupTree", StringComparison.OrdinalIgnoreCase) >= 0
                || className.IndexOf("TAppStandardBanner", StringComparison.OrdinalIgnoreCase) >= 0
                || className.IndexOf("TToolBar", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>返回 null 表示坐标在真实子 HWND 内（不需要虚拟分段）。</summary>
        public static Rectangle? ComputeVirtualItem(IntPtr panelHwnd, int screenX, int screenY)
        {
            if (panelHwnd == IntPtr.Zero) return null;

            RECT wr;
            if (!GetWindowRect(panelHwnd, out wr)) return null;
            int panelX = wr.Left, panelY = wr.Top, panelW = wr.Right - wr.Left, panelH = wr.Bottom - wr.Top;
            if (panelH < 60 || panelW < 10) return null;

            var pt = new POINT { x = screenX, y = screenY };
            ScreenToClient(panelHwnd, ref pt);
            int relY = pt.y;

            var subPanels = new List<Rectangle>();
            EnumChildWindows(panelHwnd, (child, _) =>
            {
                if (!IsWindowVisible(child)) return true;
                string childCls = GetClassNameFast(child);
                if (!IsTmsPanel(childCls)) return true;
                RECT cr;
                if (GetWindowRect(child, out cr))
                {
                    int cw = cr.Right - cr.Left, ch = cr.Bottom - cr.Top;
                    if (cw > 0 && ch > 0)
                        subPanels.Add(new Rectangle(cr.Left, cr.Top, cw, ch));
                }
                return true;
            }, IntPtr.Zero);

            foreach (var r in subPanels)
            {
                if (r.Contains(screenX, screenY)) return null;
            }

            int itemH = InferItemHeight(subPanels, panelH);

            int virtualRow = relY / itemH;
            int itemTop = panelY + virtualRow * itemH;
            int itemBottom = Math.Min(itemTop + itemH, panelY + panelH);
            int actualH = itemBottom - itemTop;
            if (actualH < 10) return null;

            return new Rectangle(panelX, itemTop, panelW, actualH);
        }

        private static int InferItemHeight(List<Rectangle> subPanels, int panelH)
        {
            var heights = new List<int>();
            foreach (var r in subPanels)
            {
                // 子面板高度在 45~65 之间视为菜单项，过滤掉大容器（如 72px 的子面板头）
                if (r.Width > 20 && r.Height >= 45 && r.Height <= 65)
                    heights.Add(r.Height);
            }
            if (heights.Count > 0)
            {
                int mode = heights.GroupBy(h => h).OrderByDescending(g => g.Count()).First().Key;
                return Math.Max(45, Math.Min(60, mode));
            }
            return 52;
        }

        public static Rectangle? ParseTmsBounds(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            value = value.Trim();
            int colonIdx = value.IndexOf(':');
            if (colonIdx > 0) value = value.Substring(colonIdx + 1);
            int semiIdx = value.IndexOf(';');
            if (semiIdx > 0) value = value.Substring(0, semiIdx);
            var parts = value.Split(',');
            if (parts.Length != 4) return null;
            if (int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y)
                && int.TryParse(parts[2], out int w) && int.TryParse(parts[3], out int h))
                return new Rectangle(x, y, w, h);
            return null;
        }

        private static string GetClassNameFast(IntPtr hwnd)
        {
            var sb = new StringBuilder(256);
            GetClassName(hwnd, sb, 256);
            return sb.ToString();
        }

        // ── Local P/Invoke ──
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x, y; }

        private delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);
    }
}
