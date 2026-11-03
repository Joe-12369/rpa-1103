using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using static II.RPA.Plugin.GUI.ElementHighlight.HighlightUtils;

namespace II.RPA.Plugin.GUI.ElementHighlight
{
    internal class HighlightTag : Window
    {

        private Rectangle m_OriginalRect;
        private string m_Tag;
        private UIElement m_ProgressBar;
        private UIElement m_FontIcon;
        private TextBlock m_TextBlock;

        internal HighlightTag(Rectangle rectangle, 
            string tag, 
            string elementTypeIcon, 
            System.Windows.Media.Brush fillColor)
        {
            base.UseLayoutRounding = true;
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/II.RPA.Plugin.GUI;component/ElementHighlight/HighlightStyle.xaml",
                UriKind.RelativeOrAbsolute)
            });
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/II.RPA.Plugin.GUI;component/ElementHighlight/FontIcon.xaml",
                UriKind.RelativeOrAbsolute)
            });
            m_OriginalRect = new Rectangle(rectangle.Location, rectangle.Size);
            m_Tag = tag;
            AutomationProperties.SetAutomationId(this, "HighlightTag");
            AutomationProperties.SetName(this, "HighlightTag");
            base.AllowsTransparency = true;
            base.WindowStyle = WindowStyle.None;
            base.Topmost = true;
            base.ShowActivated = false;
            base.ShowInTaskbar = false;
            base.Background = System.Windows.Media.Brushes.Transparent;
            base.Height = 26.0;
            m_FontIcon = new FontIcon
            {
                CodeString = elementTypeIcon,
                Margin = new Thickness(3.0, 0.0, 3.0, 0.0),
                HighContrastForeground = new SolidColorBrush(Colors.Black)
            };
            m_ProgressBar = new ProgressBar
            {
                Visibility = Visibility.Collapsed,
                Style = (Style)this.TryFindResource("ProgressBarRingStyleSmall"),
                Margin = new Thickness(3.0, 0.0, 3.0, 0.0)
            };
            m_TextBlock = new TextBlock
            {
                Text = m_Tag,
                Margin = new Thickness(3.0, 0.0, 3.0, 0.0)
            };
            Border element = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                BorderThickness = new Thickness(1.0),
                Padding = new Thickness(3.0, 2.0, 3.0, 2.0),
                BorderBrush = System.Windows.Media.Brushes.White,
                CornerRadius = new CornerRadius(4.0),
                Background = fillColor,
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        m_FontIcon,
                        m_ProgressBar,
                        (UIElement)m_TextBlock
                    }
                }
            };
            TextBlock.SetForeground(element, System.Windows.Media.Brushes.White);
            RePositionAndResize(m_OriginalRect, TransformToPixels(this).X / 96.0);
            base.Content = new DockPanel
            {
                Children = { (UIElement)element }
            };
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            Closed += ElementHighlightTag_Closed;
            //Deactivated += HighlightBorder_Deactivated;
            Loaded += HighlightTag_Loaded;
        }

        internal void  SetHighlightTag(Rectangle rectangle,
          string tag,
          string elementTypeIcon,
          System.Windows.Media.Brush fillColor)
        {
            base.UseLayoutRounding = true;
            m_OriginalRect = new Rectangle(rectangle.Location, rectangle.Size);
            m_Tag = tag;
            AutomationProperties.SetAutomationId(this, "HighlightTag");
            AutomationProperties.SetName(this, "HighlightTag");
            base.Background = System.Windows.Media.Brushes.Transparent;
            //base.Height = 26.0;
            m_FontIcon = null;
            m_FontIcon = new FontIcon
            {
                CodeString = elementTypeIcon,
                Margin = new Thickness(3.0, 0.0, 3.0, 0.0),
                HighContrastForeground = new SolidColorBrush(Colors.Black)
            };
            m_ProgressBar = null;
            m_ProgressBar = new ProgressBar
            {
                Visibility = Visibility.Collapsed,
                Style = (Style)this.TryFindResource("ProgressBarRingStyleSmall"),
                Margin = new Thickness(3.0, 0.0, 3.0, 0.0)
            };
            m_TextBlock = null;
            m_TextBlock = new TextBlock
            {
                Text = m_Tag,
                Margin = new Thickness(3.0, 0.0, 3.0, 0.0)
            };

            Border element = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                BorderThickness = new Thickness(1.0),
                Padding = new Thickness(3.0, 2.0, 3.0, 2.0),
                BorderBrush = System.Windows.Media.Brushes.White,
                CornerRadius = new CornerRadius(4.0),
                Background = fillColor,
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        m_FontIcon,
                        m_ProgressBar,
                        (UIElement)m_TextBlock
                    }
                }
            };

            TextBlock.SetForeground(element, System.Windows.Media.Brushes.White);
            RePositionAndResize(m_OriginalRect, TransformToPixels(this).X / 96.0);

            if (m_TargrtDockPanel == null)
            {
                m_TargrtDockPanel = new DockPanel
                {
                    Children = { (UIElement)element }
                };

                base.Content = m_TargrtDockPanel;
            }
            else
            {
                m_TargrtDockPanel.Children.Clear();
                m_TargrtDockPanel.Children.Add((UIElement)element);
            }
        }
        DockPanel m_TargrtDockPanel;

        private void ElementHighlightTag_Closed(object sender, EventArgs e)
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            this.Loaded -= HighlightTag_Loaded;
            this.Closed -= ElementHighlightTag_Closed;
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
        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            RePositionAndResize(m_OriginalRect, TransformToPixels(this).X / 96.0);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            SetWindowTransparent();
        }
        internal void SetTagText(string aTag)
        {
            m_TextBlock.Text = aTag;
        }

        internal void SetBusy(bool isBusy,string aText = "")
        {

            if (string.IsNullOrEmpty(aText))
            {
                aText = eCommonMessageType.HighlightTag_cs_ZhiNengFenXi.GetMessage();
            }
         

            if (isBusy)
            {
                m_FontIcon.Visibility = Visibility.Collapsed;
                m_ProgressBar.Visibility = Visibility.Visible;
                m_TextBlock.Text = aText;
            }
            else
            {
                m_ProgressBar.Visibility = Visibility.Collapsed;
                m_FontIcon.Visibility = Visibility.Visible;
                m_TextBlock.Text = m_Tag;
            }
        }
        private void RePositionAndResize(Rectangle rectangle, double factor = 1.0)
        {
            Rectangle rectangle2 = rectangle.Downscale(factor);
            rectangle2.Inflate(4, 4);
            base.Left = rectangle2.Left;
            int num = rectangle2.Top - 26 - 1;
            int num2 = rectangle2.Bottom + 1;
            if (num <= 0)
            {
                if ((double)num2 > SystemParameters.WorkArea.Height)
                {
                    base.Top = rectangle2.Top + 10;
                    base.Left = rectangle2.Left + 10;
                }
                else
                {
                    base.Top = num2;
                }
            }
            else
            {
                base.Top = num;
            }
        }

        public void SetWindowTransparent()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            uint windowLong = NativeMethods.GetWindowLong(handle, -20);
            NativeMethods.SetWindowLong(handle, -20, windowLong | 0x20u);
        }





        IntPtr wndHandle;
        public void SetTopMost()
        {
            NativeMethods.SetWindowPos(wndHandle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE);
        }

        private void HighlightTag_Deactivated(object sender, EventArgs e)
        {
            SetTopMost();
        }

        private void HighlightTag_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            wndHandle = wndHelper.Handle;
            //SetTopMost();
        }
    }

}
