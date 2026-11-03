using FlaUI.Core.Logging;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using FontIcon = II.RPA.Plugin.GUI.ProcessCreator.Controls.FontIcon;
using Logger = II.RPA.Logging.Logger;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementTrack
{
    internal class ElementHighlightTag : Window
    {
        private static readonly Microsoft.Extensions.Logging.ILogger logger = Logger.OfContext();
        private DockPanel m_TargrtDockPanel;
        private Rectangle m_OriginalRect;
        private string m_Tag;
        private UIElement m_ProgressBar;
        private UIElement m_FontIcon;
        private TextBlock m_TextBlock;

        internal ElementHighlightTag(Rectangle rectangle, 
            string tag, 
            string elementTypeIcon, 
            System.Windows.Media.Brush fillColor)
        {
            base.UseLayoutRounding = true;
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/II.RPA.Plugin.GUI;component/ProcessCreator/Theme/ElementHighlightDictionary.xaml",
               UriKind.RelativeOrAbsolute)
            });
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/II.RPA.Plugin.GUI;component/ProcessCreator/Controls/FontIcon.xaml",
               UriKind.RelativeOrAbsolute)
            });
            m_OriginalRect = new Rectangle(rectangle.Location, rectangle.Size);
            m_Tag = tag;
            AutomationProperties.SetAutomationId(this, "OverlayWindowTag");
            AutomationProperties.SetName(this, "OverlayWindowTag");
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
            //RePositionAndResize(_originalRect, VisualTreeHelper.GetDpi(this).DpiScaleX);
            RePositionAndResize(m_OriginalRect, TransformToPixels(this).X / 96.0);
            base.Content = new DockPanel
            {
                Children = { (UIElement)element }
            };
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            Closed += ElementHighlightTag_Closed;
        }

        private void ElementHighlightTag_Closed(object sender, EventArgs e)
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
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
        internal void SetHighlightTag(Rectangle rectangle,
          string tag,
          string elementTypeIcon,
          System.Windows.Media.Brush fillColor)
        {
            base.UseLayoutRounding = true;
            m_OriginalRect = new Rectangle(rectangle.Location, rectangle.Size);
            //logger.LogDebug($"Tag m_OriginalRect:{rectangle.Location.Y},{rectangle.Location.X},{rectangle.Size.Height},{rectangle.Size.Width}");
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
            m_TextBlock.Text = $"{m_Tag} - {aTag}";
        }
        internal void SetAITagText(string aTag)
        {
            m_TextBlock.Text = aTag;
        }

        internal void SetBusy(bool isBusy,string aText, bool anIsOrdinary)
        {
            if (isBusy)
            {
                if (anIsOrdinary)
                {
                    aText = eCommonMessageType.HighlightTag_cs_Fenxizhong.GetMessage();
                }
                else
                {
                    aText = eCommonMessageType.HighlightTag_cs_ZhiNengFenXi.GetMessage();
                }
                m_FontIcon.Visibility = Visibility.Collapsed;
                m_ProgressBar.Visibility = Visibility.Visible;
                if (anIsOrdinary)
                {
                    m_TextBlock.Text = $"{m_TextBlock.Text} - {aText}";
                }
                else
                {
                    m_TextBlock.Text = $"{aText}";
                }
            }
            else
            {
                m_ProgressBar.Visibility = Visibility.Collapsed;
                m_FontIcon.Visibility = Visibility.Visible;
                if (anIsOrdinary)
                {
                    m_TextBlock.Text = $"{m_Tag} - {aText}";
                }
                else
                {
                    m_TextBlock.Text = $"{aText}";
                }
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
            //logger.LogDebug($"Tag actual:{base.Top},{base.Left},{base.ActualHeight},{base.ActualWidth}");
        }

        private void SetWindowTransparent()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            uint windowLong = NativeMethods.GetWindowLong(handle, -20);
            NativeMethods.SetWindowLong(handle, -20, windowLong | 0x20u);
        }
    }

}
