using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Database.Params;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Database
{
    public abstract class DataBaseComponentBase<TParam> : AsyncPluginAbilityBase<TParam>
        where TParam : ParamBase, new()
    {
        protected abstract JMessage Execute(TParam parameter, PluginExecutionContext context);

        protected sealed override Task<JMessage> ExecuteAsync(TParam parameter, PluginExecutionContext context)
        {
            return Task.Run(() => Execute(parameter, context));
        }
    }
}
