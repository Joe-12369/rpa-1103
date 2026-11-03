using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch.MatchRule
{
    public class AppExePathMatchHandler : MatchHandler
    {
        public override bool IsMatch(WindowObject wndInfo, MatchConfig matchConfig)
        {
            if (string.IsNullOrEmpty(matchConfig.AppExePath)
                || wndInfo.AppExePath.Equals(matchConfig.AppExePath, StringComparison.OrdinalIgnoreCase))
            {
                return this.nextHandler == null ? true : this.nextHandler.IsMatch(wndInfo, matchConfig);
            }
            return false;
        }
    }

}
