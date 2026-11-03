using FlaUI.Core.Definitions;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI
{
    public static class GuiExtension
    {
        public static string GuiNodeControlName(this IGuiNode guiNode)
        {
            string controlType = guiNode.ControlType;
            try
            {
                if (guiNode is IGuiWebNode_690)
                {
                    if (!string.IsNullOrEmpty(controlType))
                    {
                        controlType = $"{controlType}/{(guiNode as IGuiWebNode_690).Attributes}";
                    }
                }
            }
            catch
            {

            }
            return controlType;
        }
        public static bool IsWebType(this eGuiType guiType) => GuiElement.selectorType_Web.Contains(guiType.ToString(), StringComparer.OrdinalIgnoreCase);

    }
}
