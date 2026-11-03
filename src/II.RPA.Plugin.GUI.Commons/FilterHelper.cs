using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public class FilterHelper
    {
        private string filter;
        private List<JObject> filterNodes;
        private const string supporttargetTypes = "|child|after|before|parent|";
        public FilterHelper(string filterStr)
        {
            filter = filterStr;
            filterNodes = new List<JObject>();
            Init();
        }
        private void Init()
        {
            string _filter = Regex.Replace(this.filter.ToLower().Replace('：', ':'), @"\s", "").Trim('"');
            var nodesStrArray = _filter.Split(':').ToList();
            if (nodesStrArray.Count > 0)//忽略首节点
            {
                nodesStrArray.RemoveAt(0);
            }
            if (nodesStrArray.Count == 0)
            {
                throw new RPAException(eErrorMessageType.Error_GetParentElements_LevelValFail_Error);
            }
            foreach (var nodesStr in nodesStrArray)
            {
                JObject jObj = new JObject();
                var nodeInfo = nodesStr.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                if (nodeInfo.Length >= 1)
                {
                    if (supporttargetTypes.IndexOf($"|{nodeInfo[0]}|") >= 0)
                    {
                        jObj.Add("targetType", nodeInfo[0]);
                    }
                    else
                    {
                        throw new Exception(Localize($"过滤器关键字不符合规范，关键字：{nodeInfo[0]}", "controlType_FilterHelper_11DC4270"));
                    }
                }
                if (nodeInfo.Length >= 2)
                {
                    if (nodeInfo[0] == "child" && (!int.TryParse(nodeInfo[1], out var _num) || _num <= 0))
                    {
                        NLogger.Default.Error($"过滤器不符合规范，关键字：{nodeInfo[0]}[{nodeInfo[1]}]");
                        throw new RPAException(eErrorMessageType.Error_GetSubElements_IndexValFail_Error);
                    }
                    jObj.Add("index", nodeInfo[1]);
                }
                filterNodes.Add(jObj);
            }
        }
        public string ToFilterNodesString()
        {
            return filterNodes.ToJsonNoneFormating();
        }
    }
}
