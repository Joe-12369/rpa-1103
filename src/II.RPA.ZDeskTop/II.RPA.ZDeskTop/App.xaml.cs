using II.Framework;
using II.Framework.Utility;
using II.RPA.ZDeskTop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Application = System.Windows.Application;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace II.RPA.ZDeskTop;

public partial class App : Application
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IObserver<string> _outputObserver;
    private readonly IObserver<string> _errorObserver;

    public App(IServiceProvider serviceProvider)
    {
        // Bind ReactiveUI to WPF before any commands or UI subscriptions are created.
        // The generic host starts before the Dispatcher synchronization context is installed.
        RxApp.MainThreadScheduler = new SynchronizationContextScheduler(
            new DispatcherSynchronizationContext(Dispatcher));
        Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _serviceProvider = serviceProvider;
        _logger = _serviceProvider.GetService<ILogger<App>>();
        _outputObserver = _serviceProvider.GetRequiredKeyedService<IObserver<string>>(Global.OutPutKey);
        _errorObserver = _serviceProvider.GetRequiredKeyedService<IObserver<string>>(Global.ErrorPutKey);

        _logger.LogInformation($"base:{AppDomain.CurrentDomain.BaseDirectory}");
        _logger.LogInformation($"work:{Directory.GetCurrentDirectory()}");
        
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        RxApp.DefaultExceptionHandler = new RxExceptionHandler(_serviceProvider.GetRequiredService<ILogger<RxExceptionHandler>>());
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);


        Timeline.DesiredFrameRateProperty.OverrideMetadata(
           typeof(Timeline),
           new FrameworkPropertyMetadata { DefaultValue = 30 }
           );

        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        Exit += App_Exit;

        Observable.Interval(TimeSpan.FromSeconds(10)).Subscribe(i => 
        {
            _outputObserver.OnNext(DateTime.Now.ToString());
        });
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _logger.LogInformation("########## ZDesktop-OnStartup ##########");
        base.OnStartup(e);

        if (e.Args != null && e.Args.Count() > 0)
        {
            _logger.LogInformation($"Commandline:{string.Join(" ", e.Args) }");
        }
        Start();
    }
    private void App_Exit(object sender, ExitEventArgs e)
    {
        _logger.LogInformation("########## ZDesktop-Exit ##########");

    }

    private void Start()
    {
        try
        {
            // Ensure localization is initialized before the first window binding validation runs.
            _ = AppMeta.Default;
            if (!WindowsLogon.CheckCurrentCache())
            {
                LoginWindow messageWindow = new(true);
                if (messageWindow.ShowDialog() == true)
                {
                    _logger.LogInformation("User have entered Username&Password!");
                }
                else
                {
                    _logger.LogWarning("User cancel!");
                }
            }
            ZDeskTopWindow mainWindow = _serviceProvider.GetRequiredService<ZDeskTopWindow>();
            Current.MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            _errorObserver.OnNext($"{ex.GetType().Name}");
            _logger?.LogError(new EventId(9), ex, "ZDeskTopWindow.Show");
            if (MessageBox.Show($"{AppMeta.Default.AppName} 启动遇到问题，请尝试重启!", "错误", MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
            {
                Current.Shutdown(-1);
            }
        }
    }



    private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError(new EventId(9), e.Exception, "Current_DispatcherUnhandledException");
    }
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex && ex is not null)
        { 
            _logger?.LogError(new EventId(9), ex, "CurrentDomain_UnhandledException");
        }
        if (e.ExceptionObject is UnauthorizedAccessException operationException)
        {
            if (MessageBox.Show(operationException.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
            {
                Current.Shutdown(-1);
            }
        }
    }
    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.LogError(new EventId(0), e.Exception, "TaskScheduler_UnobservedTaskException");
    }
    public class RxExceptionHandler : IObserver<Exception>
    {
        private readonly ILogger _logger;
        public RxExceptionHandler(ILogger<RxExceptionHandler> logger) 
        {
            _logger = logger;
        }
        public void OnNext(Exception value)
        {
            if (Debugger.IsAttached) Debugger.Break();

            _logger.LogError(new EventId(1), value, $"RxHandler.OnNext");

            RxApp.MainThreadScheduler.Schedule(() => { throw value; });
        }

        public void OnError(Exception error)
        {
            if (Debugger.IsAttached) Debugger.Break();

            _logger.LogError(new EventId(1), error, $"RxHandler.OnError");

            RxApp.MainThreadScheduler.Schedule(() => { throw error; });
        }

        public void OnCompleted()
        {
            if (Debugger.IsAttached) Debugger.Break();
        }
    }

}
