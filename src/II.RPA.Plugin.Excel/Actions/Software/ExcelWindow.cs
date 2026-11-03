using II.RPA.Plugin.Excel.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace II.RPA.Plugin.Excel.Actions.Software
{
    public class ExcelChildWindowsEnumerator
    {
        private readonly Func<ExcelWindow, object, bool> _childWindowsEvaluatorFunc;

        private readonly ExcelWindow _rootWindow;

        private bool _firstOnly;

        private List<ExcelWindow> _matchingWindowsFound;

        public ExcelChildWindowsEnumerator(ExcelWindow parentNativeWindow, Func<ExcelWindow, object, bool> childWindowsEvaluatorFunc)
        {
            _rootWindow = parentNativeWindow;
            _childWindowsEvaluatorFunc = childWindowsEvaluatorFunc;
        }

        public ExcelWindow FindFirstWindow()
        {
            _firstOnly = true;
            object searchData = null;
            return FindWindowsImp(ref searchData).FirstOrDefault();
        }

        public List<ExcelWindow> FindWindows()
        {
            object searchData = null;
            return FindWindows(ref searchData);
        }

        public List<ExcelWindow> FindWindows(ref object searchData)
        {
            _firstOnly = false;
            return FindWindowsImp(ref searchData);
        }

        private bool EnumChildWindowsCallbackPlug(IntPtr handle, ref object extraData)
        {
            ExcelWindow robinNativeWindow = new ExcelWindow(handle);
            if (!_childWindowsEvaluatorFunc(robinNativeWindow, extraData))
            {
                return true;
            }

            _matchingWindowsFound.Add(robinNativeWindow);
            return !_firstOnly;
        }

        private List<ExcelWindow> FindWindowsImp(ref object searchData)
        {
            _matchingWindowsFound = new List<ExcelWindow>();
            NativeUtils.EnumChildWindows(_rootWindow.Handle, EnumChildWindowsCallbackPlug, ref searchData);
            return _matchingWindowsFound;
        }
    }
    /// <summary>
    /// Excel文件窗口
    /// </summary>
    public class ExcelWindow
    {
        public const int MaxClassNameLength = 256;

        private const int MaxTitleLength = 1024;

        private const int WindowTitleRetrievalTimeout = 200;

        private int _processId = -1;

        public virtual Rectangle Bounds
        {
            get
            {
                NativeUtils.Rect rectangle = default(NativeUtils.Rect);
                if (!NativeUtils.GetWindowRect(Handle, ref rectangle))
                {
                    return Rectangle.Empty;
                }

                return new Rectangle(rectangle.left, rectangle.top, rectangle.Width, rectangle.Height);
            }
        }

        public string Class
        {
            get
            {
                if (Handle == IntPtr.Zero)
                {
                    return "";
                }

                StringBuilder stringBuilder = new StringBuilder(256);
                if (NativeUtils.GetClassName(Handle, stringBuilder, 256) != 0)
                {
                    return stringBuilder.ToString();
                }

                return string.Empty;
            }
        }

        public bool Exists => NativeUtils.IsWindow(Handle);

        public IntPtr Handle { get; }

        public virtual int ProcessId
        {
            get
            {
                if (Handle == IntPtr.Zero)
                {
                    return -1;
                }

                if (_processId > 0)
                {
                    return _processId;
                }

                NativeUtils.GetWindowThreadProcessId(Handle, out _processId);
                return _processId;
            }
        }

        public string Title
        {
            get
            {
                if (Handle == IntPtr.Zero)
                {
                    return "";
                }

                StringBuilder stringBuilder = new StringBuilder(1024);
                NativeUtils.SendMessageTimeout(Handle, 13, 1024, stringBuilder, 2, 200, IntPtr.Zero);
                return stringBuilder.ToString();
            }
        }

        public ExcelWindow(IntPtr handle)
        {
            Handle = handle;
        }

        public ExcelWindow(int handle)
        {
            Handle = (IntPtr)handle;
        }

        public static ExcelWindow FromForegroundWindow(bool persist = true)
        {
            return new ExcelWindow(persist ? ExcelUtils.GetForegroundWindowPersistant() : NativeUtils.GetForegroundWindow());
        }

        public bool BringToFront(bool ensureForegroundStatus = true, bool useWindowStateHacks = true)
        {
            if (Handle == IntPtr.Zero)
            {
                return true;
            }

            NativeUtils.SetForegroundWindow(Handle);
            IntPtr foregroundWindowPersistant = ExcelUtils.GetForegroundWindowPersistant(0);
            if (foregroundWindowPersistant == Handle || foregroundWindowPersistant == NativeUtils.GetAncestor(Handle, 3u) || foregroundWindowPersistant == NativeUtils.GetAncestor(Handle, 1u))
            {
                return true;
            }

            return ExcelUtils.Clock(3000L, 10, delegate
            {
                uint vparam = 0u;
                try
                {
                    NativeUtils.WindowPlacement position = default;
                    position.length = Marshal.SizeOf(position);
                    NativeUtils.GetWindowPlacement(Handle, ref position);
                    if (useWindowStateHacks && (!NativeUtils.IsWindowVisible(Handle) || position.showCmd == 6 || position.showCmd == 11 || position.showCmd == 2))
                    {
                        NativeUtils.ShowWindowAsync(Handle, 9);
                    }

                    NativeUtils.SystemParametersInfoGet(8192u, 0u, ref vparam, 0u);
                    NativeUtils.SystemParametersInfoSet(8193u, 0u, 0u, 0u);
                    NativeUtils.AllowSetForegroundWindow(NativeUtils.GetCurrentProcessId());
                    int processId;
                    uint windowThreadProcessId = (uint)NativeUtils.GetWindowThreadProcessId(Handle, out processId);
                    bool flag = false;
                    uint currentThreadId = NativeUtils.GetCurrentThreadId();
                    if (NativeUtils.AttachThreadInput(currentThreadId, windowThreadProcessId, fAttach: true))
                    {
                        Thread.Sleep(100);
                        flag = NativeUtils.SetForegroundWindow(Handle);
                        if (useWindowStateHacks && flag && position.showCmd == 3)
                        {
                            NativeUtils.ShowWindowAsync(Handle, 3);
                        }

                        Thread.Sleep(100);
                    }

                    NativeUtils.AttachThreadInput(currentThreadId, windowThreadProcessId, fAttach: false);
                    if (useWindowStateHacks && !flag)
                    {
                        NativeUtils.ShowWindowAsync(Handle, 6);
                        NativeUtils.ShowWindowAsync(Handle, (position.showCmd == 3) ? 3 : 9);
                    }
                }
                finally
                {
                    useWindowStateHacks = false;
                    NativeUtils.SystemParametersInfoSet(8193u, 0u, vparam, 0u);
                }

                return true;
            }, (bool _003Cp0_003E) => !ensureForegroundStatus || ExcelUtils.GetForegroundWindowPersistant() == Handle);
        }

        public void Close()
        {
            if (!(Handle == IntPtr.Zero))
            {
                NativeUtils.PostMessage(Handle, 16, IntPtr.Zero, IntPtr.Zero);
            }
        }
        public void Maximize()
        {
            if (!(Handle == IntPtr.Zero))
            {
                NativeUtils.PostMessage(Handle, 0x0112, new IntPtr(0xF030), IntPtr.Zero);
            }
        }
    }
}
