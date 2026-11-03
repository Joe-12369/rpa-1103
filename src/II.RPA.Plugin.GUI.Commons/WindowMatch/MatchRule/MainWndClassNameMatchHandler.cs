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
    public class MainWndClassNameMatchHandler : MatchHandler
    {
        public override bool IsMatch(WindowObject wndInfo, MatchConfig matchConfig)
        {
            if (matchConfig.BrowserWndClassNames.Any(className => wndInfo.ClassName.EqualsOrIsMatch(className, true)))
            {
                return this.nextHandler == null ? true : this.nextHandler.IsMatch(wndInfo, matchConfig);
            }
            return matchConfig.BrowserWndClassNames.Length == 0;
        }
    }
}
