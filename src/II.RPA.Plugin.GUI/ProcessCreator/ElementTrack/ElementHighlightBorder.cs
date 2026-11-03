using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.ProcessCreator.ActionProvider;
using II.RPA.Plugin.GUI.ProcessCreator.Controls;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Panel = System.Windows.Controls.Panel;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementTrack
{
	public class ElementHighlightBorder : Window
	{
		private static readonly ILogger logger = Logger.OfContext();
		private Rectangle m_OriginalRect;
		private bool m_IsWindowTransparent;
        private readonly Panel _innerPane;
        private eGuiType m_GuiType;

		public ElementHighlightBorder(Rectangle rectangle, 
            SolidColorBrush aBorderColor,
			bool anIsTransParent, 
			bool anIsBackgroundTansparent,
            eGuiType aGuiType,
            int durationInMs = 0)
		{
			m_GuiType = aGuiType;
            //m_IsContextMenuOpen = false;
            m_IsWindowTransparent = anIsTransParent;
			m_OriginalRect = new Rectangle(rectangle.Location, rectangle.Size);
            Title = "流程编辑".I18n("ElementHighlightBorder_AC218C9E");
			AllowsTransparency = true;
			WindowStyle = WindowStyle.None;
			Topmost = true;
			ShowActivated = false;
			ShowInTaskbar = false;
			IsHitTestVisible = false;
			IsEnabled = false;
			Background = System.Windows.Media.Brushes.Transparent;
			WindowStartupLocation = WindowStartupLocation.Manual;
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
			Loaded += OverlayRectangleWindow_Loaded;
            Closed += ElementHighlighterWindow_Closed;
			Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
		}
		public ActionPopupWindow CurrentActionPopupWindow
		{
			get;
			private set;
		}
		public bool IsShowActionWindow
		{
			get;
			private set;
		} = false;
		/// <summary>
		/// action结束事件
		/// </summary>
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
			Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            Resources.MergedDictionaries.Clear();
            Resources.Clear();
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
		private void OverlayRectangleWindow_Loaded(object sender, RoutedEventArgs e)
		{

        }
		#region DPI适配

		private void RePositionAndResize(Rectangle rect, double factor = 1.0)
		{
            Rectangle rectangle2 = rect.Downscale(factor);
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
            //logger.LogDebug($"Border actual:{base.Top},{base.Left},{base.Height},{base.Width}");
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
                _innerPane.Background = System.Windows.Media.Brushes.Transparent;
				NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TRANSPARENT);
			}
			else
			{
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
        public void SetCurrentElementRectBorder(Rectangle rectangle,
            eGuiType aGuiType)
        {
            if (CurrentActionPopupWindow != null)
            {
                if (ActionOverEvent != null)
                {
                    ActionOverEvent?.Invoke();
                }
                CurrentActionPopupWindow.Close();
            }
            ActionOverEvent = null;
            m_GuiType = aGuiType;
            m_OriginalRect = new Rectangle(rectangle.Location, rectangle.Size);
            //logger.LogDebug($"Border m_OriginalRect:{rectangle.Location.Y},{rectangle.Location.X},{rectangle.Size.Height},{rectangle.Size.Width}");
            Topmost = true;
            Background = System.Windows.Media.Brushes.Transparent;
            _innerPane.Background = System.Windows.Media.Brushes.Transparent;
            RePositionAndResize(m_OriginalRect, TransformToPixels(this).X / 96.0);
        }
        public void ShowHoverWindow(ActionViewModelBase anActionViewModel, IntPtr anOwnerHandle)
		{
            Dispatcher.InvokeAsync(() =>
            {
				try
				{
					IsShowActionWindow = true;
					{
						anActionViewModel.CurrentDpi = new Dpi(TransformToPixels(this).X / 96.0, TransformToPixels(this).Y / 96.0);
						//anActionViewModel.InputText = InputText;
						CurrentActionPopupWindow = new ActionPopupWindow(anActionViewModel, anOwnerHandle)
						{
							Owner = this,
							Topmost = true,
							ShowActivated = false,
						};
						CurrentActionPopupWindow.Closed += CurrentActionPopupWindow_Closed;
                        IsMouseInActionPopup = true;
						CurrentActionPopupWindow.CaptureMouse();
						CurrentActionPopupWindow.Show();
						ActionDataModels.CurrentActionWindow = CurrentActionPopupWindow;

                    }
				}
				catch (Exception ex)
				{
					Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ShowHoverWindow");
				}
            });
        }

		private void CurrentActionPopupWindow_Closed(object sender, EventArgs e)
		{
            if (CurrentActionPopupWindow != null)
            {
                if (ActionOverEvent != null)
                {
                    ActionOverEvent?.Invoke();
                }
            }
            ActionOverEvent = null;
        }

		public bool IsPopupWindowCapture()
		{
			if (CurrentActionPopupWindow != null)
			{
				var point = System.Windows.Forms.Control.MousePosition;
				//var point = Mouse.GetPosition(CurrentActionPopupWindow);
				if (point.X / (TransformToPixels(this).X / 96.0) < CurrentActionPopupWindow.Left
					|| point.Y / (TransformToPixels(this).X / 96.0) < CurrentActionPopupWindow.Top)
				{
					return false;
				}
				else if (point.X / (TransformToPixels(this).X / 96.0) > CurrentActionPopupWindow.Left + CurrentActionPopupWindow.ActualWidth
					|| point.Y / (TransformToPixels(this).X / 96.0) > CurrentActionPopupWindow.Top + CurrentActionPopupWindow.ActualHeight)
				{
					return false;
				}
				else
				{
					return true;
				}
			}
			return false;
        }
        private void ActionPopupWindow_Closed(object sender, EventArgs e)
        {
			logger.LogInformation("ActionPopupWindow.Closed");
            CurrentActionPopupWindow = null;
            if (ActionOverEvent != null)
			{
                ActionOverEvent?.Invoke();
			}
			else
			{
				logger.LogError("ElementHighlighterWindow is renew");
			}
		}

		public void StartAILoading()
		{
			if (m_GuiType == eGuiType.Windows)
			{
				//仅在windows元素类型下才需要使用cv元素
				if (_innerPane.Children == null || _innerPane.Children.Count == 0)
				{
					ScanAnimationControl scanAnimationControl = new ScanAnimationControl(this.ActualHeight,this.ActualWidth);
                    _innerPane.Children.Add(scanAnimationControl);
                }
			}
        }
        public void StopAILoading()
        {
            _innerPane?.Children?.Clear();
        }
    }
}

