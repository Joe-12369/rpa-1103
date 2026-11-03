using II.RPA.Plugin.GUI.Commons.Params;

namespace II.RPA.Plugin.GUI.Params
{

    public class ParamGetElementModel : ParamBase
    {
        public ParamGetElementModel Clone()
        {
            return new ParamGetElementModel
            {
                Selector = Selector,
                processId = processId,
                Filter = Filter,
                ComponentLabel = ComponentLabel,
                BrowserType = BrowserType,
                RunUniqueID = RunUniqueID,
                CodeVersion = CodeVersion,
                MatchType = MatchType,
                RunParam = RunParam,
                WindowInfo = WindowInfo,
                SelectorVersion = SelectorVersion,
                SelectorGuiType = SelectorGuiType,

                relateMode = relateMode,
                relateSelector  = relateSelector 
            };
        }

        /// <summary>
        /// default
        /// cssSelector
        /// xpath
        /// </summary>
        public string QueryMode { get; set; }
        public string browserType { get; set; }
        public string webData { get; set; }
        public string cvExecutionServiceURL { get; set; }
        /// <summary>
        /// 关联元素模式
        ///  0 无 1 父元素  2 同iframe元素
        /// </summary>
        public ElementRelateMode relateMode { get; set; }
        /// <summary>
        /// 父元素
        /// </summary>
        public string relateSelector  { get; set; }
        public bool b_isAssociationParent
        {
            get
            {
                return relateMode != ElementRelateMode.None;
            }
        }
    }
}
