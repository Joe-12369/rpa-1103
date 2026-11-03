using II.RPA.ZDeskTop.Abstracts;
using ReactiveUI;
using System.Diagnostics;
using System.Reactive;
using System.Windows.Input;

namespace II.RPA.ZDeskTop.ViewModels;

public class MessageViewModel : ITipMessage
{
    public MessageViewModel(string title, string tipText, string linkText, string url)
    {
        Title = title;
        TipText = tipText;
        LinkText = linkText;
        Url = url;
        OpenLinkCommand = ReactiveCommand.Create<string>(u =>
        {
            using var p = Process.Start(u);
        });
    }
    public string Title
    {
        get;
    }
    public string TipText
    {
        get;
    }
    public string LinkText
    {
        get;
    }
    public string Url
    {
        get;
    }

    public ReactiveCommand<string, Unit> OpenLinkCommand
    {
        get;
    }

    ICommand ITipMessage.OpenLinkCommand => OpenLinkCommand;
}
