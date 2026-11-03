using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Commons;

namespace II.RPA.Plugin.CV
{
    public sealed class GuiModule : IGuiModule
    {
        public void Configure(GuiModuleBuilder builder)
        {
            builder.AddOperator<GuiOperator, IGuiCVOperator650>();
            builder.AddNodeFactory<ICVGuiNodeFactory, CVGuiNodeFactory>();
        }

        private sealed class CVGuiNodeFactory : ICVGuiNodeFactory
        {
            public IGuiCVNode Create(AutomationElement element) => new CVNode(element);
        }
    }
}
