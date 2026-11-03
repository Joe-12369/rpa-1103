using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Commons;

namespace II.RPA.Plugin.Windows
{
    public sealed class GuiModule : IGuiModule
    {
        public void Configure(GuiModuleBuilder builder)
        {
            builder.AddOperator<GuiOperator, IGuiWindowsOperator>();
            builder.AddNodeFactory<IWindowsGuiNodeFactory, WindowsGuiNodeFactory>();
        }

        private sealed class WindowsGuiNodeFactory : IWindowsGuiNodeFactory
        {
            public IGuiNode Create(AutomationElement element) => new UIANode(element);
        }
    }
}
