using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using System;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI
{
    public abstract class ElementAbilityBase<TParam> : PluginAbilityBase<TParam>
        where TParam : PluginParameterBase
    {
    }

    public abstract class AsyncElementAbilityBase<TParam> : AsyncPluginAbilityBase<TParam>
        where TParam : PluginParameterBase
    {
    }

    public abstract class StaElementAbilityBase<TParam> : AsyncPluginAbilityBase<TParam>
        where TParam : PluginParameterBase
    {
        private readonly IStaDispatcher staDispatcher;

        protected StaElementAbilityBase(IStaDispatcher staDispatcher)
        {
            this.staDispatcher = staDispatcher ?? throw new ArgumentNullException(nameof(staDispatcher));
        }

        protected sealed override Task<JMessage> ExecuteAsync(
            TParam parameter,
            PluginExecutionContext executionContext)
        {
            return staDispatcher.InvokeAsync(() => Execute(parameter, executionContext));
        }

        protected abstract JMessage Execute(
            TParam parameter,
            PluginExecutionContext executionContext);
    }
}
