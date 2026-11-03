using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{

    public interface IGuiOperator_6_7_0 : IGuiOperator_6_3_0
    {
        string BuildSelectors(ParamBuildSelectorsModel paramModel);
    }

    public class ParamBuildSelectorsModel
    {
        public IGuiNode GuiNode { get; set; }
        public string jsonParams { get; set; }
    }
}
