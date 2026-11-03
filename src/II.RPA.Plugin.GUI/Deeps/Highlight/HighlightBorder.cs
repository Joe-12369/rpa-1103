using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;

namespace II.RPA.Plugin.GUI.Deeps.Highlight
{
    public class HighlightBorder : Window
    {
        private readonly Border _outerBorder;
        private readonly Border _innerBorder;
        private readonly DockPanel _innerPane;

        public HighlightBorder(SolidColorBrush borderColor)
        {
            AllowsTransparency = true;
            WindowStyle = WindowStyle.None;
            Topmost = true;
            ShowActivated = false;
            ShowInTaskbar = false;
            IsHitTestVisible = false;
            IsEnabled = false;
            Background = Brushes.Transparent;
            WindowStartupLocation = WindowStartupLocation.Manual;

            _innerPane = new DockPanel { Background = Brushes.Transparent };

            _innerBorder = new Border
            {
                BorderThickness = new Thickness(3.0),
                BorderBrush = borderColor,
                CornerRadius = new CornerRadius(2.0),
                Child = _innerPane
            };

            _outerBorder = new Border
            {
                BorderThickness = new Thickness(1.0),
                BorderBrush = Brushes.White,
                CornerRadius = new CornerRadius(4.0),
                Child = _innerBorder
            };

            Content = _outerBorder;
        }

        public void ResetFillBrush(SolidColorBrush brush)
        {
            _innerPane.Background = brush;
        }
    }
}
