using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.ProcessCreator.ViewModel;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementTrack
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private TextBlock m_TextBlock;
        private StackPanel m_StackPanel;
        private const int TagHeight = 18;
        public OverlayWindow()
        {
            InitializeComponent();
            SourceInitialized += OverlayWindow_SourceInitialized;
        }

        private void OverlayWindow_SourceInitialized(object sender, EventArgs e)
        {
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
            SetTranparent(true);
        }
        public void SetTranparent(bool transparent)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            uint extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            if (transparent)
            {
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TRANSPARENT);
            }
            else
            {
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle & ~NativeMethods.WS_EX_TRANSPARENT);
            }
        }
        public void ShowHighLightRect(System.Drawing.Rectangle aRectangle,string aTag,Color aColor)
        {
            SolidColorBrush solidColorBrush = new SolidColorBrush(aColor);
            solidColorBrush.Freeze();
            m_TextBlock = new TextBlock
            {
                Text = aTag,
                Background = solidColorBrush,
                Padding = new Thickness(7.0, 2.0, 7.0, 0.0),
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 29, 40, 122)),
                Height = TagHeight,
            };
            m_StackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { m_TextBlock }
            };
            highLightRect.Stroke = solidColorBrush;
            highLightRect.Width = aRectangle.Width;
            highLightRect.Height = aRectangle.Height;

            //System.Drawing.Rectangle rectReHandle= new System.Drawing.Rectangle();

            if (aRectangle.Top >= TagHeight)
            {
                //rectReHandle.Y = aRectangle.Top - TagHeight;
                Canvas.SetTop(m_StackPanel, aRectangle.Top - TagHeight);
                //TagRect = new Rectangle(rect.X, rect.Y - (int)m_TextBlock.Height, (int)m_TextBlock.Width, (int)m_TextBlock.Height);
            }
            else
            {
                //rectReHandle.Y = aRectangle.Top;
                m_TextBlock.Padding = new Thickness(7.0, 0.0, 7.0, 2.0);
                Canvas.SetTop(m_StackPanel, aRectangle.Top + highLightRect.Width);
                //TagRect = new Rectangle(rect.X, rect.Y + (int)m_TextBlock.Height, (int)m_TextBlock.Width, (int)m_TextBlock.Height);
            }

            Canvas.SetLeft(highLightRect, aRectangle.X);
            Canvas.SetTop(highLightRect, aRectangle.Y);
        }

        public void StopHighLightRect()
        {
            if (m_StackPanel != null)
            {
                m_StackPanel.Visibility = Visibility.Collapsed;
            }
            highLightRect.Visibility = Visibility.Collapsed;
        }
        public void ShowActionWindow(ActionViewModelBase aViewModel)
        {
            
        }
    }
}
