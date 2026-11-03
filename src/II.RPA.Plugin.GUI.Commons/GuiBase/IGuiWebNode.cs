using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiWebNode : IGuiNode
    {
        string GetHtml();
        string GetCleanHtml();
    }
}
