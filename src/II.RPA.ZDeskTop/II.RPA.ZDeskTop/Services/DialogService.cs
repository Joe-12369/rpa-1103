using II.RPA.ZDeskTop.Abstracts;
using II.RPA.ZDeskTop.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace II.RPA.ZDeskTop.Services;

public class DialogService
{
    private MessageDialog _dialogWindow;
    private readonly SemaphoreSlim _semaphore;
    private readonly IServiceProvider _serviceProvider;
    public DialogService(IServiceProvider serviceProvider)
    {
        _semaphore = new SemaphoreSlim(1, 1);
        _serviceProvider = serviceProvider;
    }

    public bool? Show<T, TVM>(TVM context, Window owner = null) where T : UserControl where TVM : ITipMessage
    {
        _semaphore.Wait();
        try
        {
            var view = _serviceProvider.GetRequiredService<T>();
            view.DataContext = context;
            _dialogWindow = new MessageDialog(view);
            if (owner != null)
            {
                _dialogWindow.Owner = owner;
            }
            return _dialogWindow.ShowDialog();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
