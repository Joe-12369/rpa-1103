using FlaUI.Core.AutomationElements;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiCVOperator650 : IGuiCVOperator
    {
        bool InspectNode(Point screenPoint, IGuiCVNode rootNode,string selector,object extendParam, out IGuiNode inspectNode);
    }
}
