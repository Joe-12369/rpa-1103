using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.DesktopInput.Mouse.Params;
using System;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DesktopInput.Mouse
{
    public abstract class MouseComponentBase<TParam> : AsyncPluginAbilityBase<TParam> where TParam : ParamBase
    {
        protected IPluginInvoker PluginInvoker { get; }

        protected MouseComponentBase(IPluginInvoker pluginInvoker)
        {
            PluginInvoker = pluginInvoker ?? throw new ArgumentNullException(nameof(pluginInvoker));
        }

        protected virtual JMessage Execute(TParam paramModel, PluginExecutionContext context)
        {
            throw new NotSupportedException($"{GetType().FullName} 未实现 Execute 或 ExecuteAsync。");
        }

        protected override Task<JMessage> ExecuteAsync(TParam paramModel, PluginExecutionContext context)
        {
            return Task.FromResult(Execute(paramModel, context));
        }
    }
}
