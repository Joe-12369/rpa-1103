using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WebExtensionInstall;

namespace AutoInstallerV2
{
    internal class ChromeV2Executor : IInstallExecutor
    {
        public string BrowserName => "Chrome";

        public event Action<string> OnOutput;
        public event Action<Exception> OnException;
        public event Action<string> OnError;

        public ChromeV2Executor(string[] args) 
        {
            
        }

        public async Task<bool> ExecuteInstallAsync()
        {
            OnOutput?.Invoke($"Entry {nameof(ExecuteInstallAsync)}...");

            try
            {
                TaskCompletionSource<bool> taskCompletionSource = new();

                OnOutput?.Invoke("Init");
                Common.InitParams();

                OnOutput?.Invoke($"Delete old extension:{Common.ChromeExtensionId}");
                Common.DeleteExtension();

                OnOutput?.Invoke("Start to CopyHostFileToPath");
                if (!Common.CopyHostFileToPath(BrowserName, ["IIRPAChromeExtension.crx"]))
                {
                    OnError?.Invoke("Fail to CopyHostFileToPath");
                    taskCompletionSource.SetResult(false);
                    return await taskCompletionSource.Task;
                }

                var extensionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPAChromeExtension\\IIRPAChromeExtensionV2.crx");

                if (!File.Exists(extensionPath))
                {
                    OnError?.Invoke($"Extension file not found：{extensionPath}");
                    taskCompletionSource.SetResult(false);
                    return await taskCompletionSource.Task;
                }

                OnOutput?.Invoke("Start to WriteRegedit");
                if (!Common.WriteRegedit(extensionPath))
                {
                    OnError?.Invoke($"Fail to WriteRegedit");
                    taskCompletionSource.SetResult(false);
                    return await taskCompletionSource.Task;
                }
                //Common.EnsureV2ExtensionId();

                ExtensionInstall.OpenBrowser(BrowserName.ToLower());
                taskCompletionSource.SetResult(true);
                return await taskCompletionSource.Task;
            }
            catch (Exception ex)
            {
                OnException?.Invoke(ex);
                return false;
            }
            finally
            {

                OnOutput?.Invoke($"Exit {nameof(ExecuteInstallAsync)}");
            }
        }

        public async Task<bool> IsBrowserInstalledAsync()
        {
            OnOutput?.Invoke($"Entry {nameof(IsBrowserInstalledAsync)}...");

            TaskCompletionSource<bool> taskCompletionSource = new();

            Common.InitParams();

            if (Common.FindChromePath())
            {
                taskCompletionSource.SetResult(true);
            }
            else
            {
                taskCompletionSource.SetResult(false);
            }

            return await taskCompletionSource.Task;
        }

        public async Task CloseBrowserProcessAsync()
        {
            OnOutput?.Invoke($"Entry {nameof(CloseBrowserProcessAsync)}...");

            var process = Process.GetProcessesByName("chrome");

            if (process.Length > 0)
            {
                foreach (var p in process)
                {
                    p.Kill();
                }
            }
            OnOutput?.Invoke($"Exit {nameof(CloseBrowserProcessAsync)}");
            await Task.CompletedTask;
        }

        public async Task<bool> IsBrowserRunningAsync()
        {
            OnOutput?.Invoke($"Entry {nameof(IsBrowserRunningAsync)}...");

            TaskCompletionSource<bool> taskCompletionSource = new();


            if (Common.IsExistBrowserProcess(BrowserName.ToLower()))
            {
                taskCompletionSource.SetResult(true);
            }
            else
            {
                taskCompletionSource.SetResult(false);
            }

            OnOutput?.Invoke($"Exit {nameof(IsBrowserRunningAsync)}");
            return await taskCompletionSource.Task;
        }

        public async Task<bool> VerifyBrowserVersion()
        {
            OnOutput?.Invoke($"Entry {nameof(VerifyBrowserVersion)}...");
            TaskCompletionSource<bool> taskCompletionSource = new();

            var result = Common.VerifyWebBrowserVersion(BrowserName.ToLower(), out var fileMissing);

            taskCompletionSource.SetResult(result);
            OnOutput?.Invoke($"Exit {nameof(VerifyBrowserVersion)}");
            return await taskCompletionSource.Task;
        }
    }
}
