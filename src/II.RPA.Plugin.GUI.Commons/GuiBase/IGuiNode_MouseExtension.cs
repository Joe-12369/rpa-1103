using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiNode_MouseExtension : IGuiNode
    {
        void MouseThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null);
        void MouseRightThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null);
        void MouseConterThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null);
    }
}
