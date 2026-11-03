using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiCVNode650 : IGuiCVNode604, IGuiCVNodeAddition
    {
        bool CVParse(Bitmap appImg, IntPtr appHandle,bool isRefrush);
    }
}
