using FlaUI.Core.AutomationElements;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IWindowsGuiNodeFactory
    {
        IGuiNode Create(AutomationElement element);
    }
}
