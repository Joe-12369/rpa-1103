using II.RPA.Plugin.GUI.Commons.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{

    public interface IGuiOperator_6_1_0: IGuiQuerySelectorOperator
    {

        /// <summary>
        /// 根据选择器进行数据采集
        /// </summary>
        /// <param name="paramModel"></param>
        /// <returns></returns>
        List<List<string>> CollectDataBySelector(ParamCollectDataBySelectorModel paramModel);
        /// <summary>
        /// 滚动页面 
        /// </summary>
        /// <param name="srcoll">                        
        ///  var j = new JObject();
        /// j.Add("scrollTop", 100);
        /// j.Add("scrollLeft", 100);
        /// ScrollIntoView(j.ToString());
        ///</param>
        void ScrollIntoView(string srcoll);

    }
}
