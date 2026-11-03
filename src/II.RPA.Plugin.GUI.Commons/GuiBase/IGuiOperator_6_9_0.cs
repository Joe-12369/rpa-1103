using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    /// <summary>
    /// 后续拓展功能，不需要新增接口类，直接在此接口类中新增行为
    /// </summary>
    public interface IGuiOperator_6_9_0 : IGuiOperator_6_7_0
    {
        string GetElementsAll(GetElementsAllParams @params);
        string BuildSelectors(BulidSelectorsParams @params);

        IGuiNode GetElementById(GetElementByIdParams args);

        List<List<string>> CollectData_v2(ParamDataCollect_V2Model paramModel);
        List<SelectorProps> GetSameParentsProps_v2(ParamDataCollect_V2Model paramModel);

        List<JObject> GetSimilarOrDataCollectElements(GetElementsAllParams @params);

        IGuiNode[] QuerySelectorAll(GetElementFromSelectorParams args);
        bool VerifyWindowOfNode(GetElementFromSelectorParams args);
        string GetTargetElementInfo(GetTargetElementInfoModelParams @params);
    }
}
