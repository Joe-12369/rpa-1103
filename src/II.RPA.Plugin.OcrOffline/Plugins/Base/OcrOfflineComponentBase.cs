using II.RPA.Plugin.Core;
using II.RPA.Plugin.OcrOffline.Params;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Plugin.OcrOffline
{
    public abstract class OcrOfflineComponentBase<TParam> : II.RPA.Plugin.Core.PluginBase.AsyncPluginAbilityBase<TParam> where TParam : ParamBase
    {
        protected II.RPA.Plugin.Abstractions.IPluginInvoker PluginInvoker { get; }

        protected OcrOfflineComponentBase(II.RPA.Plugin.Abstractions.IPluginInvoker pluginInvoker)
        {
            PluginInvoker = pluginInvoker ?? throw new System.ArgumentNullException(nameof(pluginInvoker));
        }

        protected virtual JMessage Execute(TParam parameter, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            throw new System.NotSupportedException($"{GetType().FullName} 未实现 Execute 或 ExecuteAsync。");
        }

        protected override System.Threading.Tasks.Task<JMessage> ExecuteAsync(TParam parameter, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            return System.Threading.Tasks.Task.FromResult(Execute(parameter, context));
        }
    }
}
