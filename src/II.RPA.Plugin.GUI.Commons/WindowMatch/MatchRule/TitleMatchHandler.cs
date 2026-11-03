using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch.MatchRule
{
    public class TitleMatchHandler : MatchHandler
    {
        public override bool IsMatch(WindowObject wndInfo, MatchConfig matchConfig)
        {
            foreach (var titleFeature in matchConfig.TitleFeatures)
            {
                Regex regex = new Regex(titleFeature);
                if (regex.IsMatch(wndInfo.Title))
                {
                    return this.nextHandler == null ? true : this.nextHandler.IsMatch(wndInfo, matchConfig);
                }
            }
            return matchConfig.TitleFeatures.Length == 0;
        }
    }
}
