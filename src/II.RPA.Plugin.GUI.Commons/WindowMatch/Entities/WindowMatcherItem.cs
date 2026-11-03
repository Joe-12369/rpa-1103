using II.RPA.Plugin.GUI.Commons.WindowMatch.MatchRule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch.Entities
{
    public class WindowMatcherItem
    {
        public MatchConfig MatchConfig { get; set; }
        public MatchHandler WindowMatchHandler { get; set; }
        public MatchHandler EmbeddedIEMatchHandler { get; set; }
    }
}
