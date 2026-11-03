using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch.MatchRule
{
    public class EmbeddedIEMatchHandler : MatchHandler
    {
        public override bool IsMatch(WindowObject wndInfo, MatchConfig matchConfig)
        {
            if (matchConfig.IERenderClassNames == null || matchConfig.IERenderClassNames.Length == 0)
            {
                return false;
            }
            var ie_dom_classname = matchConfig.IERenderClassNames[0];
            var childWndList = wndInfo.EnumAllChildWindows((childWndInfo) =>
            {
                if (childWndInfo.IsVisible && childWndInfo.ClassName.EqualIgnoreCase(ie_dom_classname))
                {
                    return true;
                }
                return false;
            }, false);
            return childWndList.Count > 0;
        }
    }
}
