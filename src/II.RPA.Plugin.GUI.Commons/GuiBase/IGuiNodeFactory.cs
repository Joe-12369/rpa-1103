using FlaUI.Core.AutomationElements;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiNodeFactory
    {
        IGuiNode Create(AutomationElement element);
    }
}
