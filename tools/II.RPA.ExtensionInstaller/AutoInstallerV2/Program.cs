using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoInstallerV2
{
    internal class Program
    {
        private static IInstallExecutor executor;
        private static ConsoleColor OriginalColor = Console.ForegroundColor;
        static async Task<int> Main(string[] args)
        {
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            try
            {
                executor = ResolveExecutor(args);
                executor.OnOutput += Executor_OnOutput;
                executor.OnException += Executor_OnException;
                executor.OnError += Executor_OnError;

                if (!await executor.IsBrowserInstalledAsync())
                {
                    WriteError($"未安装 {executor.BrowserName}");
                    ReadyforOver();
                    return 1;
                }
                if (await executor.IsBrowserRunningAsync())
                {
                    Console.WriteLine($"检测到 {executor.BrowserName} 正在运行,确认强制关闭进程继续安装? Y:强制关闭并继续,按其他键取消本次操作!");
                    ConsoleKeyInfo keyInfo = Console.ReadKey();
                    Console.WriteLine();

                    if (keyInfo.Key == ConsoleKey.Y)
                    {
                        await executor.CloseBrowserProcessAsync();
                    }
                    else
                    {
                        WriteInformation("用户取消");
                        ReadyforOver();
                        return 1;
                    }
                }
                if (await executor.ExecuteInstallAsync())
                {
                    WriteInformation("安装成功");
                    ReadyforOver();
                    return 0;
                }
                else
                {
                    WriteError("安装失败");
                    ReadyforOver();
                    return 1;
                }
            }
            catch(Exception ex)
            {
                WriteError(ex);
                ReadyforOver();
                return 2;
            }
        }

        private static void Executor_OnError(string obj)
        {
            WriteError(obj);
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            WriteError(e);
            e.SetObserved();
        }

        private static void Executor_OnException(Exception obj)
        {
            WriteError($"Executor_OnException:{obj}");
        }

        private static void Executor_OnOutput(string obj)
        {
            WriteInformation(obj);
        }

        private static IInstallExecutor ResolveExecutor(string[] args)
        {
            if (args != null && args.Any())
            {
                var type = args[0].ToLower();
                return type switch
                {
                    "chrome" => new ChromeV2Executor(args),
                    //case "edge":
                    //case "firefox":
                    _ => new ChromeV2Executor(args),
                };
            }
            else
            {
                return new ChromeV2Executor([]);
            }
        }

        private static void WriteError(object error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(error);
            Console.ForegroundColor = OriginalColor;
        }

        private static void WriteInformation(object infomation)
        {
            Console.ForegroundColor = OriginalColor;
            Console.WriteLine(infomation);
        }

        private static void ReadyforOver()
        {
            WriteInformation("按回车键结束");
            Console.ReadLine();
        }
    }
}
