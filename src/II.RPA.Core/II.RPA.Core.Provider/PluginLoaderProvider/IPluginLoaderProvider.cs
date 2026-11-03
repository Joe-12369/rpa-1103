using System.Threading.Tasks;
using System.Threading;
using II.RPA.Core.Abstract;
using II.RPA.Core.Models;
using II.RPA.Plugin.Core.Entities;

namespace II.RPA.Core.Provider.PluginLoaderProvider
{
    public interface IPluginLoaderProvider
    {
        IMessageNotify MessageNotify { get; set; }
        string CurrentPackagePath { get; set; }
        Task<object> PluginLoaderRun(string aPackagePath, string aPluginName, string anAction, string aJsonParam, RunParamModel aRunParam, CancellationToken cancellationToken = default, bool anIsNeedCancel = true, string aRunMode = null);
        Task<object> PluginLoaderRunByClientSTA(string aPackagePath, string aPluginName, string anAction, string aJsonParam, int aTimeOutSecond = 0, CancellationToken cancellationToken = default);
        Task<object> PluginLoaderRunByClient(string aPackagePath, string aPluginName, string anAction, string aJsonParam, bool anIsNeedAutoInit, int aTimeOutSecond = 0, PluginRunOptions pluginRunOptions = null, CancellationToken cancellationToken = default);
    }
}
