//using II.RPA.Plugin.GUI.Commons.Params;
//using II.RPA.Plugin.GUI.Commons;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Newtonsoft.Json.Linq;

//namespace II.RPA.Plugin.Browsers.Chrome
//{
//    public partial class GuiOperator
//    {
//        public string BuildSelectors(string jsonParams)
//        {
//            var guiOperator = this.GetGuiOperator(null);
//            return guiOperator.BuildSelectors(jsonParams);
//        }
//        public string BuildSelectors(BulidSelectorsParams @params)
//        {
//            var guiOperator = this.GetGuiOperator(null);
//            return guiOperator.BuildSelectors(@params);
//        }
//        public string BuildSelectors(ParamBuildSelectorsModel paramModel)
//        {
//            var guiOperator = this.GetGuiOperator(null);
//            return guiOperator.BuildSelectors(paramModel);
//        }
//        public string GetBoundings(string jsonParams)
//        {
//            var guiOperator = this.GetGuiOperator(null);
//            return guiOperator.GetBoundings(jsonParams);
//        }
//        /// <summary>
//        /// 老版本数据采集功能使用，不在维护
//        /// </summary>
//        /// <param name="srcoll"></param>
//        /// <exception cref="NotImplementedException"></exception>
//        public void ScrollIntoView(string srcoll)
//        {
//            var guiOperator = this.GetGuiOperator(null);
//            guiOperator.ScrollIntoView(srcoll);
//        }
//        public List<JObject> GetSimilarOrDataCollectElements(GetElementsAllParams @params)
//        {
//            var guiOperator = this.GetGuiOperator(null);
//            return guiOperator.GetSimilarOrDataCollectElements(@params);
//        }
//        public string GetTargetElementInfo(GetTargetElementInfoModelParams @params)
//        {
//            var guiOperator = this.GetGuiOperator(null);
//            return guiOperator.GetTargetElementInfo(@params);
//        }
//        public string GetWebCode(GetElementsAllParams args)
//        {
//            var guiOperator = this.GetGuiOperator(null);
//            return guiOperator.GetWebCode(args);
//        }
//    }
//}
