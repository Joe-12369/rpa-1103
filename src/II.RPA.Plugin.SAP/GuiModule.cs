using II.RPA.Plugin.GUI.Commons;
namespace II.RPA.Plugin.SAP
{
    public sealed class GuiModule : IGuiModule
    {
        public void Configure(GuiModuleBuilder builder) => builder.AddOperator<GuiOperator>();
    }
}
