using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch.MatchRule
{
    public class ProcessMatchHandler : MatchHandler
    {
        public override bool IsMatch(WindowObject wndInfo, MatchConfig matchConfig)
        {
            if (matchConfig.ProcessFeatures.Any(pName => pName.Equals(wndInfo.ProcessName, StringComparison.OrdinalIgnoreCase)))
            {
                return this.nextHandler == null ? true : this.nextHandler.IsMatch(wndInfo, matchConfig);
            }
            return matchConfig.ProcessFeatures.Length == 0;
        }
    }
}
