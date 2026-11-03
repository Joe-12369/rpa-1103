using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using System;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Excel.Plugins
{
    public abstract class StaExcelAbilityBase<TParam> : AsyncPluginAbilityBase<TParam>
        where TParam : PluginParameterBase
    {
        private readonly IStaDispatcher staDispatcher;

        protected StaExcelAbilityBase(IStaDispatcher staDispatcher)
        {
            this.staDispatcher = staDispatcher ?? throw new ArgumentNullException(nameof(staDispatcher));
        }

        protected sealed override Task<JMessage> ExecuteAsync(
            TParam parameter,
            PluginExecutionContext context)
        {
            return staDispatcher.InvokeAsync(() => Execute(parameter, context));
        }

        protected abstract JMessage Execute(
            TParam parameter,
            PluginExecutionContext context);
    }
}
