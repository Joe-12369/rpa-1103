using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch.MatchRule
{
    public abstract class MatchHandler
    {
        protected MatchHandler nextHandler;
        public void SetNextHandler(MatchHandler handler)
        {
            this.nextHandler = handler;
        }
        public abstract bool IsMatch(WindowObject wndInfo, MatchConfig matchConfig);
    }
}
