using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
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
    public interface IGuiWebNode_690 : IGuiNode_6_6_0
    {
        string PinSelector { get; }
        string Executejs(string scriptCode, string @params, string executionEnvironment = "", bool isAsyncCode = false, float waitResultTimeout = 0, bool catchException = false);
        string ElementDownload(string saveFile, string fileName, int timeout);
        string ElementDownloader(ParamDownloaderModel param);
        IGuiNode GetTurningPageElement(List<string> cssSelectors);
        string[] GetUsableAttributes();
        List<SelectorProps> GetSimilarElementSelector(List<SelectorProps> originalSelector);
        bool TryGetTableElement(out IGuiWebNode_690 tableNode);
        string GetColletionColumnSelector();
        List<SelectorProps> CompareBuildCollectDataSelectorV2(ParamBulidCollectDataSelectorModel paramModel);
        /// <summary>
        /// 是否在可视区域
        /// </summary>
        bool IsIntoView { get; }
        string GetSimilarSelectorByAuto();
        string Attributes { get; set; }
        void ElementFileUploader(ParamFileUploaderModel param);
        string GetLineAndBarChartData(ParamGetChartDataModel param);
        string GetPieChartData(ParamGetChartDataModel param);
        string BuildSmartSelector(List<Dictionary<string, object>> validNodeAttributes);
    }
}
