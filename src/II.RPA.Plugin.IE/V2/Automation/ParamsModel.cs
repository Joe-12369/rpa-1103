using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.WebBrowser.Common.Basic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.IE.V2.Automation
{
    internal class ParamMultipleCloseModel : ParamsModel
    {
        public List<int> TabIds { get; set; }
    }
    internal class ParamsScrollModel : ParamsModel
    {
        public eScrollDirection direction { get; set; }
        public int value { get; set; }
        public string elementId { get; set; }
        public string targetElementId { get; set; }
        public string scrollType { get; set; }
        public int offSetX { get; set; }
        public int offSetY { get; set; }
    }
    public class CollectDataSelectorData
    {
        public string Title { get; set; } = string.Empty;
        public List<ColNameStruct> CommonTalbleTitleList = new List<ColNameStruct>();

        public string Version { get; set; } = string.Empty;

        public List<SelectorProps> Selector { get; set; } = new List<SelectorProps>();

        public CollectType CollectType { get; set; } = CollectType.Content;
        //采集类型 对应的属性名，数据来源于CollectType
        public string CollectAttrName
        {
            get
            {
                try
                {
                    string collectTypeStr = CollectType.ToString();
                    FieldInfo fieldInfo = CollectType.GetType().GetField(collectTypeStr);
                    CollectAttribute reulstAttrName = fieldInfo.GetCustomAttributes(typeof(CollectAttribute), false).FirstOrDefault() as CollectAttribute;
                    if (reulstAttrName != null)
                    {
                        return reulstAttrName.AttributeName;
                    }
                }
                catch (Exception ex)
                {

                }
                return string.Empty;
            }
        }
        /// <summary>
        /// 正则默认：null
        /// </summary>
        public string Regular { get; set; } = string.Empty;

        /// <summary>
        /// 是否已经进行文本编译（变量编译），默认 否
        /// </summary>
        //public bool IsCompiled { get; set; } = false;
    }

    internal class ParamsGetCookiesModel : ParamsModel
    {
        public string Name { get; set; }
        public string Domain { get; set; }
    }
    internal class ParamsSetCookieModel : ParamsModel
    {
        public string name { get; set; }
        public string value { get; set; }
        public string url { get; set; }
    }
    internal class ParamsGetBodyScrollModel : ParamsModel
    {
        public string Direction { get; set; }
        public string elementId { get; set; }
    }
    internal class ParamsGetChilrenModel : ParamsModel
    {
        public string filter { get; set; }
        public string index { get; set; }
    }
    internal class ParamsExecutejsModel : ParamsModel
    {
        public string code { get; set; }
    }
    internal class ParamsQueryCollectionDataModel : ParamsModel
    {
        public JArray parentSelector { get; set; }
        public JArray selectorDataList { get; set; }
    }
    internal class ParamsQueryCollectionDataV2Model : ParamsModel
    {
        public string markedElementId { get; set; }
        public CollectionConfig collectionConfig { get; set; }
        public ColorGuidanceConfig colorGuidanceConfig { get; set; }
    }
    internal class ParamsQueryElementsAllModel : ParamsModel
    {
        public string tagNames { get; set; }
        public string attrKeywords { get; set; }
    }
    internal class ParamsNavigateModel : ParamsModel
    {
        public string Url { get; set; }
    }
    internal class ParamsGetTabIdsModel : ParamsModel
    {
        public bool active { get; set; }
        public List<IntPtr> wndHandles { get; set; }
    }
    internal class ParamsBuildSelectorAllModel : ParamsModel
    {
        public JArray selector { get; set; }
        public string cssSelector { get; set; }
        public int index { get; set; }
        public string version { get; set; }
        public bool innerTextFirst { get; set; }
        public bool rawData { get; set; }
        public bool excludeInnerText { get; set; }
        public bool excludeHiddenEle { get; set; } = true;//这里作为730版本的默认行为，属性控制提供更多扩展

        public List<Dictionary<string, object>> ValidNodeAttributes { get; set; } = null;
    }
    internal class ParamsQuerySelectorModel : ParamsModel
    {
        public JArray selector { get; set; }
        public string cssSelector { get; set; }
        public int index { get; set; }
        public string version { get; set; }
        public bool discernHidden { get; set; }
    }
    internal class ParamsQuerySelectorAllModel : ParamsModel
    {
        public JArray selector { get; set; }
        public string cssSelector { get; set; }
        public int index { get; set; }
        public string version { get; set; }
        public string resultAttrName { get; set; }
        public string parent { get; set; } = "";
        public bool discernHidden { get; set; }
    }
    internal class ParamsElementFormPointModel : ParamsModel
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
    internal class ParamsScrollIntoViewIfNeededModel : ParamsModel
    {
        public string location { get; set; }
    }
    internal class ParamsGetTextModel : ParamsModel
    {
    }
    internal class ParamsGetNextElementBuildSelectorModel : ParamsModel
    {
        public string cssSelector { get; set; }
        public bool innerTextFirst { get; set; }
    }
    internal class ParamsGetTurningPageElementModel : ParamsModel
    {
        public List<string> cssSelectors { get; set; }
    }
    internal class ParamsGetSimilarElementModel : ParamsModel
    {
    }
    internal class ParamsGetIsTableNameModel : ParamsModel
    {

    }
    internal class ParamsGetTableDataModel : ParamsModel
    {

    }
    internal class ParamsGetAttributesModel : ParamsModel
    {

    }
    internal class ParamsGetAttributeModel : ParamsModel
    {
        public string Name { get; set; }
        public bool isIntact { get; set; }
        public string apiVersion { get; set; }
    }
    internal class ParamsGetHtmlModel : ParamsModel
    {
    }
    internal class ParamsGetParentIdListModel : ParamsModel
    {
        public string filter { get; set; }
        public int level { get; set; }
    }
    internal class ParamsGetSiblingsIdListModel : ParamsModel
    {
        public string filter { get; set; }
        public int position { get; set; }
    }
    internal class ParamsGetChildrenIdListModel : ParamsModel
    {
        public string filter { get; set; }
        public int index { get; set; }
    }
    internal class ParamsInputModel : ParamsModel
    {
        public string value { get; set; }
        public bool replace { get; set; }
    }
    internal class ParamsIsEditableModel : ParamsModel
    {

    }
    internal class ParamsGetPropertyModel : ParamsModel
    {
        public string name { get; set; }

    }
    internal class ParamsGetCheckStatusModel : ParamsModel
    {

    }
    internal class ParamsGetSelectOptionsModel : ParamsModel
    {
        public bool? selected { get; set; }
        public string optionType { get; set; }
    }
    internal class ParamsGetSelectOptionModel : ParamsModel
    {
        public string optionType { get; set; }
    }
    internal class ParamsClickModel : ParamsModel
    {

    }
    internal class ParamsRightClickModel : ParamsModel
    {

    }
    internal class ParamsRightDoubleClickModel : ParamsModel
    {

    }
    internal class ParamsDoubleClickModel : ParamsModel
    {

    }
    internal class ParamsSetAttributeModel : ParamsModel
    {
        public string name { get; set; }
        public string value { get; set; }
    }
    internal class ParamsSetCheckStatusModel : ParamsModel
    {
        public bool isChecked { get; set; }
    }
    internal class ParamsFocusModel : ParamsModel
    {
        public bool isChecked { get; set; }
    }
    internal class ParamsSetSelectOptionsModel : ParamsModel
    {
        public JArray options { get; set; }
    }
    internal class ParamsModel
    {
        public int TabId { get; set; }
        public string ElementId { get; set; }
        public List<string> elementIds { get; set; }
    }
}
