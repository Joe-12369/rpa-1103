using System.Windows.Input;

namespace II.RPA.ZDeskTop.Abstracts;

public interface ITipMessage
{
    string TipText { get; }
    string LinkText { get; }
    string Url { get; }
    ICommand OpenLinkCommand { get; }
}
