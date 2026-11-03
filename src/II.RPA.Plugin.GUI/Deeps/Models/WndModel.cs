using System;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    public class WndModel : BaseAutomationModel
    {
        public WndModel(IntPtr hwnd)
        {
            Handle = hwnd;
        }
    }
}
