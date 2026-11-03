using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace II.Framework.UI
{
    public class WindowTitleBar : HeaderedItemsControl
    {
        public ObservableCollection<object> OptionItems { get; } = new ObservableCollection<object>();
        public WindowTitleBar()
        {
            DefaultStyleKey = typeof(WindowTitleBar);
        }
    }
}
