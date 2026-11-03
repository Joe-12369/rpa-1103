using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public class KeyboardLayoutManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        public const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
        public const uint WM_INPUTLANGCHANGE = 0x0051;
        private const uint KLF_ACTIVATE = 0x00000001;

        /// <summary>
        /// 切换模式
        /// </summary>
        public enum SwitchMode
        {
            /// <summary>切换后不恢复（手动恢复）</summary>
            Manual,
            /// <summary>切换后自动恢复到之前的布局</summary>
            AutoRestore
        }

        private IntPtr _previousLayout;
        private IntPtr _targetWindow;
        private SwitchMode _mode;
        private bool _isActive;
        private bool _disposed;

        /// <summary>
        /// 常见键盘布局ID
        /// </summary>
        public static class LayoutIds
        {
            public const uint English_US = 0x0409;           // 美式键盘
            public const uint Chinese_Simplified = 0x0804;   // 简体中文
            public const uint Chinese_Traditional = 0x0404;  // 繁体中文
            public const uint Japanese = 0x0411;             // 日语
            public const uint Korean = 0x0412;               // 韩语
        }

        /// <summary>
        /// 使用前景窗口初始化
        /// </summary>
        public KeyboardLayoutManager(SwitchMode mode = SwitchMode.AutoRestore)
        {
            _targetWindow = GetForegroundWindow();
            _mode = mode;
            _previousLayout = GetCurrentLayout();
            _isActive = false;
        }

        /// <summary>
        /// 使用指定窗口初始化
        /// </summary>
        public KeyboardLayoutManager(IntPtr hwnd, SwitchMode mode = SwitchMode.AutoRestore)
        {
            _targetWindow = hwnd;
            _mode = mode;
            _previousLayout = GetCurrentLayout();
            _isActive = false;
        }

        /// <summary>
        /// 获取当前键盘布局
        /// </summary>
        public static IntPtr GetCurrentLayout()
        {
            return GetKeyboardLayout(0);
        }

        /// <summary>
        /// 获取当前键盘布局ID
        /// </summary>
        public static uint GetCurrentLayoutId()
        {
            IntPtr hkl = GetKeyboardLayout(0);
            return (uint)(hkl.ToInt64() & 0xFFFF);
        }

        /// <summary>
        /// 获取当前键盘布局名称
        /// </summary>
        public static string GetCurrentLayoutName()
        {
            uint id = GetCurrentLayoutId();
            return LayoutIdToName(id);
        }

        /// <summary>
        /// 布局ID转换为可读名称
        /// </summary>
        public static string LayoutIdToName(uint layoutId)
        {
            switch (layoutId)
            {
                case LayoutIds.English_US:
                    return "English (US)";
                case LayoutIds.Chinese_Simplified:
                    return "Chinese (Simplified)";
                case LayoutIds.Chinese_Traditional:
                    return "Chinese (Traditional)";
                case LayoutIds.Japanese:
                    return "Japanese";
                case LayoutIds.Korean:
                    return "Korean";
                default:
                    return $"Unknown (0x{layoutId:X})";
            }
        }

        /// <summary>
        /// 保存当前键盘布局（使用前景窗口）
        /// </summary>
        public void Save()
        {
            _previousLayout = GetCurrentLayout();
            _targetWindow = GetForegroundWindow();
        }

        /// <summary>
        /// 保存当前键盘布局（指定目标窗口）
        /// </summary>
        public void Save(IntPtr hwnd)
        {
            _previousLayout = GetCurrentLayout();
            _targetWindow = hwnd;
        }

        /// <summary>
        /// 切换到美式键盘
        /// </summary>
        public bool SwitchToEnglish()
        {
            return SwitchTo(LayoutIds.English_US);
        }
        
        /// <summary>
        /// 切换到指定键盘布局
        /// </summary>
        public bool SwitchTo(uint layoutId)
        {
            EnsureNotDisposed();

            if (_targetWindow == IntPtr.Zero)
            {
                _targetWindow = GetForegroundWindow();
            }

            if (_previousLayout == IntPtr.Zero)
            {
                _previousLayout = GetCurrentLayout();
            }

            string klid = layoutId.ToString("X8");
            // 使用 LoadKeyboardLayout 确保系统加载并激活该布局
            IntPtr hkl = LoadKeyboardLayout(klid, KLF_ACTIVATE);
            if (hkl == IntPtr.Zero)
            {
                // 如果加载失败，尝试回退到之前的 SendMessage 逻辑
                hkl = new IntPtr(layoutId);
            }
            bool result = PostMessage(_targetWindow, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hkl);

            _isActive = true;
            return result;
        }

        /// <summary>
        /// 恢复到切换前的键盘布局
        /// </summary>
        public void Restore()
        {
            if (_targetWindow == IntPtr.Zero || _previousLayout == IntPtr.Zero || !_isActive)
            {
                return;
            }
            PostMessage(_targetWindow, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, _previousLayout);
            _isActive = false;
        }

        /// <summary>
        /// 切换到中文输入法
        /// </summary>
        public bool SwitchToChinese()
        {
            return SwitchTo(LayoutIds.Chinese_Simplified);
        }

        /// <summary>
        /// 获取之前保存的键盘布局ID
        /// </summary>
        public uint PreviousLayoutId => (uint)(_previousLayout.ToInt64() & 0xFFFF);

        /// <summary>
        /// 是否已切换
        /// </summary>
        public bool IsActive => _isActive;

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(KeyboardLayoutManager));
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            // 如果是自动恢复模式，且当前处于激活状态，则恢复之前的布局
            if (_mode == SwitchMode.AutoRestore && _isActive)
            {
                Restore();
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~KeyboardLayoutManager()
        {
            Dispose();
        }
    }
}
