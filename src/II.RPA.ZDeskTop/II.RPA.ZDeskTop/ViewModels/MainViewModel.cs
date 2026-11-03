using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace II.RPA.ZDeskTop.ViewModels;

public class MainViewModel: ReactiveObject
{
    public readonly ILogger _logger;
    public MainViewModel(ILogger<MainViewModel> logger)
    {
        _logger = logger;
    }
    [Reactive]
    public bool IsTopMost
    {
        get;
        set;
    }
}
