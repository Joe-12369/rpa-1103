using II.RPA.Plugin.Core.PluginBase;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    public abstract class SystemComponentBase<TParam> : PluginAbilityBase<TParam> where TParam : PluginParameterBase
    {
        protected override void ValidateParameter(TParam parameter) { }
    }
}
