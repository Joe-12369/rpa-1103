using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoInstallerV2
{
    internal interface IInstallExecutor
    {
        string BrowserName { get; }
        Task<bool> IsBrowserInstalledAsync();
        Task<bool> IsBrowserRunningAsync();
        Task<bool> VerifyBrowserVersion();
        Task CloseBrowserProcessAsync();
        Task<bool> ExecuteInstallAsync();
        event Action<string> OnOutput;
        event Action<Exception> OnException;
        event Action<string> OnError;
    }
}
