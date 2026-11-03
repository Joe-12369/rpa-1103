using FlaUI.Core.AutomationElements;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface ICVGuiNodeFactory
    {
        IGuiCVNode Create(AutomationElement element);
    }
}
