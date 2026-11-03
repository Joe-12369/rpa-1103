using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiCVNode : IGuiNode
    {
        Bitmap CacheImage { get; set; }
        Bitmap CVSubElementToBitmap(string filePath, Bitmap bitmap);
        bool CVParse(Bitmap appImg);
        string GetText(string cvServer);
    }
}
