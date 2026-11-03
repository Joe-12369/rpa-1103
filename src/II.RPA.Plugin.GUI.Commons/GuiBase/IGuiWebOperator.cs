using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public interface IGuiWebOperator : IGuiOperator
    {

        ViewElementsDataModel GetViewElementsData(GetElementsAllParams args);
        //IGuiOperator GetGuiOperator(MatchConfig matchConfig);

        (List<List<string>> collectionData, IGuiNode markedElement) GetCollectWaterfallDataFromConfigList(ParamDataCollect_V2Model paramModel);
        string GetWebCode(GetElementsAllParams args);
    }
}
