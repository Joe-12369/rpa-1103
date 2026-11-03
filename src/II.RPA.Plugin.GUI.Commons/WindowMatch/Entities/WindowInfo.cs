using II.RPA.Logging;
using II.RPA.Plugin.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch.Entities
{
    public class WindowInfo
    {
        private string _title = null;
        private string _className = null;
        private uint _processId = 0;
        private string _processName = null;
        private Rectangle _windowRect = Rectangle.Empty;
        private string _appExePath = null;
        private string _appExeProductName = null;
        private static ILogger logger = Logger.OfContext();

        #region 窗口 数据 属性
        public IntPtr Handle { get; set; }
        public string Title
        {
            get
            {
                if (string.IsNullOrEmpty(_title))
                {
                    _title = UiCommon.GetWindowText(this.Handle);
                }
                return _title;
            }
            set
            {
                _title = value;
            }
        }
        public string ClassName
        {
            get
            {
                if (string.IsNullOrEmpty(_className))
                {
                    _className = UiCommon.GetClassName(this.Handle);
                }
                return _className;
            }
        }
        public int ProcessId
        {
            get
            {
                if (this._processId == 0)
                {
                    UiCommon.GetWindowThreadProcessId(this.Handle, out _processId);
                }
                return (int)this._processId;
            }
        }
        public string ProcessName
        {
            get
            {
                if (string.IsNullOrEmpty(this._processName))
                {
                    this._processName = UiCommon.GetProcessNameByCache(this.Handle);
                }
                return this._processName;
            }
        }
        public string AppExePath
        {
            get
            {
                if (this.Handle == IntPtr.Zero || !IsWindow)
                {
                    return string.Empty;
                }
                if (string.IsNullOrEmpty(this._appExePath))
                {
                    this._appExePath = UiCommon.GetProgramAddressByCache(this.Handle);
                }
                return this._appExePath;
            }
        }
        public string AppExeProductName
        {
            get
            {
                if (string.IsNullOrEmpty(this._appExeProductName))
                {
                    this._appExeProductName = UiCommon.GetProductNameByCache(this.Handle);
                }
                return this._appExeProductName;
            }
        }
        /// <summary>
        /// NativeMethods.GetAncestor(this.Handle, GetAncestorFlags.GetRoot);
        /// </summary>
        public IntPtr RootWindowHandle => NativeMethods.GetAncestor(this.Handle, GetAncestorFlags.GetRoot);
        /// <summary>
        /// NativeMethods.GetAncestor(this.Handle, GetAncestorFlags.GetParent);
        /// </summary>
        public IntPtr ParentHandleByGetAncestor => NativeMethods.GetAncestor(this.Handle, GetAncestorFlags.GetParent);

        /// <summary>
        ///  NativeMethods.GetParent(this.Handle)
        /// </summary>
        public IntPtr ParentHandle => NativeMethods.GetParent(this.Handle);
        /// <summary>
        /// GetWindowRect
        /// </summary>
        public Rectangle WindowRect
        {
            get
            {
                if (_windowRect.IsEmpty)
                {
                    this._windowRect = RectHelper.GetWindowRect(this.Handle);
                }
                return _windowRect;
            }
        }

        #endregion

        #region 窗口状态

        public bool IsWindow => NativeMethods.IsWindow(this.Handle);
        public bool IsWindowVisible => NativeMethods.IsWindowVisible(this.Handle);
        public bool IsIconic => NativeMethods.IsIconic(this.Handle);
        public bool IsToolWindow => (NativeMethods.GetWindowLong(this.Handle, NativeMethods.GWL_EXSTYLE) & NativeMethods.WS_EX_TOOLWINDOW) != 0;
        public bool IsWindowEnabled => NativeMethods.IsWindowEnabled(this.Handle);
        #endregion

    }
}
