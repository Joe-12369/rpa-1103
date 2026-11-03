using II.RPA.Plugin.Abstractions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.PluginBase
{
    public abstract class PluginAbilityBase<TParam> : PluginAbilityProtocolBase<TParam>
        where TParam : PluginParameterBase
    {
        protected abstract JMessage Execute(TParam parameter, PluginExecutionContext context);

        protected sealed override Task<JMessage> ExecuteCoreAsync(TParam parameter, PluginExecutionContext context)
        {
            return Task.FromResult(Execute(parameter, context));
        }
    }
}
