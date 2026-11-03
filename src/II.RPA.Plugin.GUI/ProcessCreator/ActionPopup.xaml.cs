using II.RPA.Plugin.GUI.ProcessCreator.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace II.RPA.Plugin.GUI.ProcessCreator
{
    /// <summary>
    /// Interaction logic for ActionPopup.xaml
    /// </summary>
    public partial class ActionPopup : Popup
    {
        //[DllImport("user32.dll", SetLastError = true)]
        //static extern IntPtr SetFocus(IntPtr hWnd);
        ActionViewModelBase m_ViewModel;
        public ActionPopup(ActionViewModelBase aViewModel, string aPrepareCost)
        {
            InitializeComponent();
            m_ViewModel = aViewModel;
            this.DataContext = m_ViewModel;
            m_ViewModel.CloseWindowAction = (str) => { this.IsOpen = false; };
            //.Loaded += OnPopupLoaded;
            //Unloaded += OnPopupUnloaded;
            GotFocus += ActionPopup_GotFocus;
        }
        [DllImport("User32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        IntPtr GetHwnd(Popup popup)
        {
            HwndSource source = (HwndSource)PresentationSource.FromVisual(popup.Child);
            return source.Handle;
        }
        private void ActionPopup_GotFocus(object sender, RoutedEventArgs e)
        {
            SetFocus(GetHwnd(this));
        }

        /// <summary>
        /// 应用状态
        /// </summary>
        private bool? _appliedTopMost;
        /// <summary>
        /// 是否已经加载
        /// </summary>
        private bool _alreadyLoaded;
        /// <summary>
        /// popup所在的窗体
        /// </summary>
        private Window _parentWindow;

        /// <summary>
        /// 是否顶置
        /// </summary>
        public bool IsTopmost
        {
            get { return (bool)GetValue(IsTopmostProperty); }
            set { SetValue(IsTopmostProperty, value); }
        }
        /// <summary>
        /// 是否顶置依赖属性（默认不顶置）
        /// </summary>
        public static readonly DependencyProperty IsTopmostProperty = DependencyProperty.Register("IsTopmost", typeof(bool), typeof(ActionPopup), new FrameworkPropertyMetadata(false, OnIsTopmostChanged));

        /// <summary>
        /// 是否跟随父窗体移动
        /// </summary>
        public bool IsMove
        {
            get { return (bool)GetValue(IsMoveProperty); }
            set { SetValue(IsMoveProperty, value); }
        }
        public static readonly DependencyProperty IsMoveProperty =
            DependencyProperty.Register("IsMove", typeof(bool), typeof(ActionPopup), new PropertyMetadata(false));

        /// <summary>
        /// popup加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnPopupLoaded(object sender, RoutedEventArgs e)
        {
            //if (_alreadyLoaded)
            //    return;

            //_alreadyLoaded = true;

            //if (Child != null)
            //{
            //    Child.AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnChildPreviewMouseLeftButtonDown), true);
            //}

            //_parentWindow = Window.GetWindow(this);
            //if (IsMove)
            //    _parentWindow.LocationChanged += delegate
            //    {
            //        var offset = this.HorizontalOffset;
            //        this.HorizontalOffset = offset + 1;
            //        this.HorizontalOffset = offset;
            //    };

            //if (_parentWindow == null)
            //    return;

            //_parentWindow.Activated += OnParentWindowActivated;
            //_parentWindow.Deactivated += OnParentWindowDeactivated;
            //ActivatePopup(this);
        }

        private void OnPopupUnloaded(object sender, RoutedEventArgs e)
        {
            if (_parentWindow == null)
                return;
            _parentWindow.Activated -= OnParentWindowActivated;
            _parentWindow.Deactivated -= OnParentWindowDeactivated;
        }
        /// <summary>
        /// 主窗体激活事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnParentWindowActivated(object sender, EventArgs e)
        {
            //SetTopmostState(true);
        }
        /// <summary>
        /// 主窗体不在激活状态事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnParentWindowDeactivated(object sender, EventArgs e)
        {
            //if (IsTopmost == false)
            //{
            //    SetTopmostState(IsTopmost);
            //}
            IsOpen = false;
        }
        /// <summary>
        /// 子元素的鼠标左键按下事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnChildPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetTopmostState(true);

            if (!_parentWindow.IsActive && IsTopmost == false)
            {
                _parentWindow.Activate();
            }
        }
        /// <summary>
        /// IsTopmost属性改变事件
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="e"></param>
        private static void OnIsTopmostChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var thisobj = (ActionPopup)obj;

            thisobj.SetTopmostState(thisobj.IsTopmost);
        }
        /// <summary>
        /// 重写open事件
        /// </summary>
        /// <param name="e"></param>
        //protected override void OnOpened(EventArgs e)
        //{
        //    //设置状态
        //    SetTopmostState(IsTopmost);
        //    base.OnOpened(e);
        //    HwndSource source = (HwndSource)HwndSource.FromVisual(txt);
        //    IntPtr hwnd = source.Handle;
        //    SetFocus(hwnd);
        //    txt.Focus();
        //}
        private void SetTopmostState(bool isTop)
        {
            // 如果状态与输入状态相同，则不要应用状态
            if (_appliedTopMost.HasValue && _appliedTopMost == isTop)
            {
                return;
            }

            if (Child == null)
                return;

            var hwndSource = (PresentationSource.FromVisual(Child)) as HwndSource;

            if (hwndSource == null)
                return;
            var hwnd = hwndSource.Handle;

            RECT rect;

            if (!GetWindowRect(hwnd, out rect))
                return;


            if (isTop)
            {
                SetWindowPos(hwnd, HWND_TOPMOST, rect.Left, rect.Top, (int)Width, (int)Height, TOPMOST_FLAGS);
            }
            else
            {
                /*
                 z顺序只会在点击时得到刷新/反射
                 标题栏（与外部的其他部分相对比窗口）除非我先将弹出窗口设置为hwndbottom
                 然后HWND_TOP HWND_NOTOPMOST之前
                 */
                SetWindowPos(hwnd, HWND_BOTTOM, rect.Left, rect.Top, (int)Width, (int)Height, TOPMOST_FLAGS);
                SetWindowPos(hwnd, HWND_TOP, rect.Left, rect.Top, (int)Width, (int)Height, TOPMOST_FLAGS);
                SetWindowPos(hwnd, HWND_NOTOPMOST, rect.Left, rect.Top, (int)Width, (int)Height, TOPMOST_FLAGS);
            }

            _appliedTopMost = isTop;
        }
        #region P / Invoke 入口和定义
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT

        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X,
            int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        static readonly IntPtr HWND_TOP = new IntPtr(0);
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        private const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOZORDER = 0x0004;
        const UInt32 SWP_NOREDRAW = 0x0008;
        const UInt32 SWP_NOACTIVATE = 0x0010;

        const UInt32 SWP_FRAMECHANGED = 0x0020; /* The frame changed: send WM_NCCALCSIZE */
        const UInt32 SWP_SHOWWINDOW = 0x0040;
        const UInt32 SWP_HIDEWINDOW = 0x0080;
        const UInt32 SWP_NOCOPYBITS = 0x0100;
        const UInt32 SWP_NOOWNERZORDER = 0x0200; /* Don’t do owner Z ordering */
        const UInt32 SWP_NOSENDCHANGING = 0x0400; /* Don’t send WM_WINDOWPOSCHANGING */

        const UInt32 TOPMOST_FLAGS =
            SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSIZE | SWP_NOMOVE | SWP_NOREDRAW | SWP_NOSENDCHANGING;
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.IsOpen = false;
        }
        [DllImport("USER32.DLL")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void ActivatePopup(Popup popup)
        {
            //try to get a handle on the popup itself (via its child)
            HwndSource source = (HwndSource)PresentationSource.FromVisual(popup.Child);
            IntPtr handle = source.Handle;

            //activate the popup
            SetForegroundWindow(handle);
        }
    }
}
