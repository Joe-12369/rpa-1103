using II.RPA.Plugin.GUI.Commons;
using Microsoft.Extensions.DependencyInjection;

namespace II.RPA.Plugin.Java
{
    public sealed class GuiModule : IGuiModule
    {
        public void Configure(GuiModuleBuilder builder)
        {
            builder.Services.AddSingleton<IJavaUiThread, JavaUiThread>();
            builder.Services.AddSingleton(provider =>
                new GuiOperator(provider.GetRequiredService<IJavaUiThread>()));
            builder.Services.AddSingleton<IGuiOperator>(provider =>
                provider.GetRequiredService<GuiOperator>());
        }
    }
}
