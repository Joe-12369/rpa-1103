using II.Framework.Resource;
using static II.Framework.Resource.I18nExtensions;
using II.Framework;
using II.Framework.gRPC;
using II.Framework.Hosting;
using II.Framework.Service;
using II.Framework.Utility;
using II.RPA.ZDeskTop.Services;
using II.RPA.ZDeskTop.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;

namespace II.RPA.ZDeskTop;

public static class Program
{
    private const string GlobalMutexName = "II.RPA.ZDeskTop.Mutex";
    private static Mutex mutex = null;
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
            if (!EnsureSingleInstance())
            {
                mutex?.Dispose();
                Console.WriteLine($"{Global.AppName}:检测到{AppMeta.Default.AppName}已启动!");
                return 1;
            }
           

            Console.WriteLine($"{string.Join(" ", args)}");
            if (!CanCreateTask())
            {
                return 9;
            }
            var code = ChildSessionIsAccessible(args);
            if (code != 0)
            {
                return code;
            }
            EnvironmentHelper.SetupJitOptimization("II.RPA.ZDeskTop");
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            var host = ResolveHostBuilder(args).Build();
            Run(host);
            return 0;
        }
        finally
        {
            mutex?.Dispose();
            Console.WriteLine($"{Global.AppName}:Over");
            FreeConsole();
        }
    }


    private static HostApplicationBuilder ResolveHostBuilder(string[] args)
    {
        var hostBuilder = Host.CreateApplicationBuilder();
        hostBuilder.Configuration
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .AddInMemoryCollection();

        var outputObserver = Observer.Create<string>(
        line =>
        {
            var outputStream = Console.OpenStandardOutput();
            var streamWriter = new StreamWriter(outputStream, Encoding.Default);
            streamWriter.WriteLine($"{Global.AppName}:{line}");
            streamWriter.Flush();
        });
        var errorObserver = Observer.Create<string>(
        line =>
        {
            var outputStream = Console.OpenStandardError();
            var streamWriter = new StreamWriter(outputStream, Encoding.Default);
            streamWriter.WriteLine($"{Global.AppName}:{line}");
            streamWriter.Flush();
        });

        #region ioc config
        hostBuilder.Logging.ClearProviders();
        hostBuilder.Logging.SetMinimumLevel(LogLevel.Trace);
        hostBuilder.Logging.AddNLog("NLog.config");
        hostBuilder.Services.AddKeyedSingleton(Global.OutPutKey,outputObserver);
        hostBuilder.Services.AddKeyedSingleton(Global.ErrorPutKey, errorObserver);
        hostBuilder.Services.AddSingleton<BotConfigService>();
        hostBuilder.Services.AddSingleton<DialogService>();
        hostBuilder.Services.AddSingleton<ZDeskTopWindow>();
        hostBuilder.Services.AddTransient<RdpConnectionErrorView>();
        hostBuilder.Services.AddSingleton<ControlService>();
        hostBuilder.Services.AddSingleton<WorkerServiceProvider>();
        hostBuilder.Services.AddSingleton<OverlayService>();
        #endregion
        hostBuilder.Services.AddSingleton<IHostLifetime, WpfApplicationLifetime<App>>();
        hostBuilder.Services.AddSingleton(provider =>
        {
            var app = new App(provider);
            app.InitializeComponent();
            return app;
        });
        return hostBuilder;
    }
    private static void Run(IHost host)
    {
        host.RunWpfApplication<App>();
    }
    private static int ChildSessionIsAccessible(string[] args)
    {
        Console.WriteLine($"Dummy:{nameof(UnauthorizedAccessException)}");

        Console.WriteLine($"{args.Contains("--e")}&&{args.Contains("5")}");

        if (args.Contains("--e") && args.Contains("5"))
        {
            Console.WriteLine($"{nameof(UnauthorizedAccessException)}");
            Console.WriteLine($"Test-需要管理员权限,进程退出:{5}");
            return 5;
        }
        if (!ChildSessionHelper.IsEnableChildSession())
        {
            if (!ChildSessionHelper.EnableChildSession())
            {
                Console.WriteLine($"{nameof(UnauthorizedAccessException)}");
                Console.WriteLine($"开启子会话需要管理员权限,进程退出:{5}");
                return 5;
            }
        }
        return 0;
    }
    private static bool CanCreateTask()
    {
        if (!SchedulerTaskHelper.CanRegisterTask())
        {
            Console.WriteLine($"注册计划任务需要管理员权限");
            MessageBox.Show("请使用管理员权限重启客户端！".I18n("Program_8DAE7A1C"), $"{AppMeta.Default.AppName}", MessageBoxButton.OK);
            return false;
        }
        return true;
    }
    private static bool EnsureSingleInstance()
    {
        bool isExists = Mutex.TryOpenExisting(GlobalMutexName, out mutex);
        if (isExists && null != mutex)
        {
            if (MessageBox.Show($"{AppMeta.Default.AppName}已经运行！", $"{AppMeta.Default.AppName}", MessageBoxButton.OK) == MessageBoxResult.OK)
            {
                return false;
            }
            else
            {
                return false;
            }
        }
        else
        {
            mutex = new Mutex(true, GlobalMutexName);
            return true;
        }
    }
    internal const uint ATTACH_PARENT_PROCESS = 4294967295U;

    [DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    static extern bool AttachConsole(uint dwProcessId);

    [DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    static extern bool FreeConsole();
}
