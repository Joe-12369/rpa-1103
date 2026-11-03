using II.RPA.Plugin.Abstractions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.PluginBase
{
    public abstract class AsyncPluginAbilityBase<TParam> : PluginAbilityProtocolBase<TParam>
        where TParam : PluginParameterBase
    {
        protected abstract Task<JMessage> ExecuteAsync(TParam parameter, PluginExecutionContext context);

        protected sealed override Task<JMessage> ExecuteCoreAsync(TParam parameter, PluginExecutionContext context)
        {
            return ExecuteAsync(parameter, context);
        }
    }
}
