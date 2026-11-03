using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace II.RPA.Plugin.Core.Wpf.Controls.Windows
{
    public class FluentWindowTitleBar : HeaderedItemsControl
    {
        public ObservableCollection<object> OptionItems { get; } = new ObservableCollection<object>();
        public FluentWindowTitleBar()
        {
            DefaultStyleKey = typeof(FluentWindowTitleBar);
        }
    }
}
