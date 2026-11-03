using II.RPA.Plugin.GUI.Commons.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiQuerySelectorOperator : IGuiOperator
    {
        string GetBoundings(string jsonParams);
        string BuildSelectors(string jsonParams);
    }
}
