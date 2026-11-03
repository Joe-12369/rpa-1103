using System.Windows;
using System.Windows.Controls;

namespace II.RPA.Plugin.Dialog.Themes
{
    public class AutoSwitchStackPanel : StackPanel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            double totalWidth = 0;
            foreach (UIElement child in InternalChildren)
            {
                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                totalWidth += child.DesiredSize.Width + 2;
                
            }

            if (totalWidth > MaxWidth)
            {
                if (Orientation == Orientation.Horizontal)
                {
                    Orientation = Orientation.Vertical;
                }
            }
            else
            {
                if (Orientation == Orientation.Vertical)
                {
                    Orientation = Orientation.Horizontal;
                }
            }

            var result = base.MeasureOverride(availableSize);
            return result;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);
            foreach (UIElement child in InternalChildren)
            {
                if (child is FrameworkElement frameworkElement)
                {
                    frameworkElement.HorizontalAlignment = HorizontalAlignment.Left;
                    frameworkElement.Margin = Orientation == Orientation.Horizontal ? new Thickness(0, 0, 2, 0) : new Thickness(0, 0, 2, 2);
                }
            }
            return result;
        }
    }
}
