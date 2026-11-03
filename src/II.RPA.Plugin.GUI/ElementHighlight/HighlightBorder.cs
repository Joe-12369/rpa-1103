using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using static II.RPA.Plugin.GUI.ElementHighlight.HighlightUtils;
using Panel = System.Windows.Controls.Panel;

namespace II.RPA.Plugin.GUI.ElementHighlight
{
	public class HighlightBorder : Window
	{
		private static readonly ILogger logger = Logger.OfContext();
		private Rectangle m_OriginalRect;
		private bool m_IsWindowTransparent;//去掉 readonly
        private Panel _innerPane;//去掉 readonly
		private eGuiType m_GuiType;//去掉 readonly
		
		
		public HighlightBorder(Rectangle rectangle, 
            SolidColorBrush aBorderColor,
			bool anIsTransParent, 
			bool anIsBackgroundTansparent,
            eGuiType aGuiType,
            int durationInMs = 0)
		{
			m_GuiType = aGuiType;
            m_IsWindowTransparent = anIsTransParent;
			m_OriginalRect = new Rectangle(rectangle.Location, rectangle.Size);
			Title = "流程编辑".I18n("HighlightBorder_AC218C9E");
			AllowsTransparency = true;
			WindowStyle = WindowStyle.None;
			Topmost = true;
			ShowActivated = false;
			ShowInTaskbar = false;
			IsHitTestVisible = false;
			IsEnabled = false;
			Background = System.Windows.Media.Brushes.Transparent;
			WindowStartupLocation = WindowStartupLocation.Manual;
            AutomationProperties.SetAutomationId(this, "HighlightBorder");
            AutomationProperties.SetName(this, "HighlightBorder");
            SolidColorBrush solidColorBrushBorder = aBorderColor;
            _innerPane = new DockPanel
            {
                Background = System.Windows.Media.Brushes.Transparent
            };
			
			base.Content = new Border
            {
                BorderThickness = new Thickness(1.0),
                BorderBrush = System.Windows.Media.Brushes.White,
                CornerRadius = new CornerRadius(4.0),
                Child = new Border
                {
                    BorderThickness = new Thickness(3.0),
                    BorderBrush = solidColorBrushBorder,
                    CornerRadius = new CornerRadius(2.0),
                    Child = _innerPane
                }
            };
            RePositionAndResize(m_OriginalRect, TransformToPixels(this).X / 96.0);
            //Deactivated += HighlightBorder_Deactivated;
            Loaded += HighlightBorder_Loaded;
            Closed += ElementHighlighterWindow_Closed;
			Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
		}

        private void HighlightBorder_Deactivated(object sender, EventArgs e)
        {
			SetTopMost();
		}

        private void HighlightBorder_Loaded(object sender, RoutedEventArgs e)
        {
			WindowInteropHelper wndHelper = new WindowInteropHelper(this);
			wndHandle = wndHelper.Handle;
			//SetTopMost();
		}

		IntPtr wndHandle;
		public void SetTopMost()
		{
			//logger.LogDebug("进行Border置顶操作");
			NativeMethods.SetWindowPos(wndHandle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE);
		}

		public void SetCurrentElementRectBorder(Rectangle rectangle,
			SolidColorBrush aBorderColor,
			bool anIsTransParent,
			bool anIsBackgroundTansparent,
			eGuiType aGuiType,
			int durationInMs = 0)
		{
			m_GuiType = aGuiType;
			m_IsWindowTransparent = anIsTransParent;
			m_OriginalRect = new Rectangle(rectangle.Location, rectangle.Size);
			Topmost = true;
			Background = System.Windows.Media.Brushes.Transparent;
			AutomationProperties.SetAutomationId(this, "HighlightBorder");
			AutomationProperties.SetName(this, "HighlightBorder");
			SolidColorBrush solidColorBrushBorder = aBorderColor;
			_innerPane.Background = System.Windows.Media.Brushes.Transparent;
			RePositionAndResize(m_OriginalRect, TransformToPixels(this).X / 96.0);
			//Closed += ElementHighlighterWindow_Closed;
			//Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
		}

		public void SetCurrentElementRectBorderSimple(Rectangle rectangle)
		{
			m_OriginalRect = new Rectangle(rectangle.Location, rectangle.Size);
			Topmost = true;
			//Background = System.Windows.Media.Brushes.Transparent;
			//AutomationProperties.SetAutomationId(this, "HighlightBorder");
			//AutomationProperties.SetName(this, "HighlightBorder");
			//SolidColorBrush solidColorBrushBorder = aBorderColor;
			//_innerPane.Background = System.Windows.Media.Brushes.Transparent;
			RePositionAndResize(m_OriginalRect, TransformToPixels(this).X / 96.0);
		}


		public Window CurrentActionPopupWindow
		{
			get;
			private set;
		}
		public bool IsShowActionWindow
		{
			get;
			private set;
		} = false;
		public Action ActionOverEvent
		{
			private get;
			set;
		}
		public string InputText
		{
			get;
			private set;
		}
		public bool IsMouseInActionPopup
		{
			get;
			private set;
		} = false;
		private void ElementHighlighterWindow_Closed(object sender, EventArgs e)
        {
			this.Loaded -= HighlightBorder_Loaded;
			//this.Deactivated -= HighlightBorder_Deactivated;
			Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            Resources.MergedDictionaries.Clear();
            Resources.Clear();

            if (CurrentActionPopupWindow != null)
			{
                if (ActionOverEvent != null)
                {
                    ActionOverEvent?.Invoke();
                }
            }
			ActionOverEvent = null;
			this.Closed -= ElementHighlighterWindow_Closed;
		}
        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
			RePositionAndResize(m_OriginalRect, TransformToPixels(this).X/96.0);
		}
        protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			SetWindowTransparent(m_IsWindowTransparent);
		}
		#region DPI适配

		private void RePositionAndResize(Rectangle rect, double factor = 1.0)
		{
            Rectangle rectangle2 = rect.Downscale(factor);

			//logger.LogDebug($"校验 RePositionAndResize{rectangle2.ToJson()} 缩放{factor}");
            if ((double)(rectangle2.Width + 4) <= SystemParameters.WorkArea.Width)
            {
                rectangle2.Inflate(4, 0);
            }
            if ((double)(rectangle2.Height + 4) <= SystemParameters.WorkArea.Height)
            {
                rectangle2.Inflate(0, 4);
            }
            base.Left = rectangle2.Left;
            base.Top = rectangle2.Top;
            base.Width = rectangle2.Width;
            base.Height = rectangle2.Height;

		}
		public Dpi TransformToPixels(Visual aVisual)
		{
			Matrix matrix;
			var source = PresentationSource.FromVisual(aVisual);
			if (source != null)
			{
				matrix = source.CompositionTarget.TransformToDevice;
			}
			else
			{
				using (var src = new HwndSource(new HwndSourceParameters()))
				{
					matrix = src.CompositionTarget.TransformToDevice;
				}
			}

			double pixelX = (int)(matrix.M11 * 96.0);
			double pixelY = (int)(matrix.M22 * 96.0);
			//logger.LogDebug($"校验 matrix.M11{matrix.M11} pixelY{pixelY}");
			return new Dpi(pixelX, pixelY);
		}
		#endregion
		public void SetTagText(string aText)
		{
			InputText = aText;
			//m_TextBlock.Text = $"正在输入:{InputText}";
		}
		/// <summary>
		/// 设置窗口穿透
		/// </summary>
		/// <param name="aTransparent"></param>
		public void SetWindowTransparent(bool aTransparent)
		{
			IntPtr hwnd = new WindowInteropHelper(this).Handle;
			uint extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
			if (aTransparent)
			{
				//logger.LogDebug("穿透效果");
                _innerPane.Background = System.Windows.Media.Brushes.Transparent;
				NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TRANSPARENT);
				///SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
			}
			else
			{
				//logger.LogDebug("非穿透效果");
				_innerPane.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 0, 0, 139));
				NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle & ~NativeMethods.WS_EX_TRANSPARENT);
			}
		}
		/// <summary>
		/// 设置窗口背景色透明
		/// </summary>
		/// <param name="aTransparent"></param>
		public void SetBackgroundTransparent(bool aTransparent)
		{
			if (aTransparent)
			{
                _innerPane.Background = System.Windows.Media.Brushes.Transparent;
			}
			else
			{
                _innerPane.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 0, 0, 139));
			}
		}

		public void StartAILoading()
		{
			if (m_GuiType == eGuiType.Windows)
			{
				//仅在windows元素类型下才需要使用cv元素
				if (_innerPane.Children == null || _innerPane.Children.Count == 0)
				{
                    //todo
                }
			}
        }
        public void StopAILoading()
        {
            _innerPane?.Children?.Clear();
        }
    }
}
