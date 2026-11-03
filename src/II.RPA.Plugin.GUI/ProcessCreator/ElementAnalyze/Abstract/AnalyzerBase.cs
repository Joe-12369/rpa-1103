using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract
{
    internal abstract class AnalyzerBase:IAnalyzer
    {
        public AnalyzerBase()
        {
            
        }
        public string AnalyzerName
        {
            get
            {
                return this.GetType().Name;
            }
        }
        public Type AnalyzerType
        {
            get
            {
                return GetType();
            }
        }
        public virtual Priority ExecutePriority
        {
            get;
            set;
        }

        public bool IsSupportCv
        {
            get
            {
                return false;
            }
        }
        public virtual string[] SupportGuiTypes
        {
            get
            {
                return new string[]
                {
                    "Windows",
                    "Java",
                    "Chrome",
                    "IE",
                    "SAP",
                    "FireFox",
                    "ZExplorer",
                    "Edge",
                    "WdtErp",
                    "Web360se",
                    "Sogou",
                };
            }
        }

        public virtual bool TryExecuteAnalyzer(
            JObject aSelectorJO,
            ActionParamModel anActionParam,
            ElementCacheProvider anElementCacheProvider,
            out List<ExecuteAction> recommendActionExecutions, 
            out Dictionary<string, string> recommendActionExecutionParams)
        {
            throw new NotImplementedException();
        }

        public virtual bool TryExecuteAnalyzer(
           JObject aSelectorJO,
           ActionParamModel anActionParam,
           IEnumerable<string> aCurrentComponents,
           out List<ExecuteAction> recommendActionExecutions)
        {
            throw new NotImplementedException();
        }

        public virtual bool TryExecuteAnalyzer(
            JObject aSelectorJO,
            ActionParamModel anActionParam,
            out List<ExecuteAction> recommendActionExecutions,
            out Dictionary<string, string> recommendActionExecutionParams,
            out string anElementClass)
        {
            throw new NotImplementedException();
        }

        public virtual bool TryExecuteAnalyzerAsAddition(
            ActionParamModel anActionParam,
            out List<ExecuteAction> recommendActionExecutions,
            out Dictionary<string, string> recommendActionExecutionParams,
            out string anElementClass,
            out bool anIsMatchCondition)
        {
            throw new NotImplementedException();
        }

        public virtual bool TryExecuteAnalyzer(
           ActionParamModel anActionParam,
           out List<ExecuteAction> recommendActionExecutions,
           out Dictionary<string, string> recommendActionExecutionParams)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string,string> GetDefaultActionParams(List<ExecuteAction> aRecommendActionExecutions, 
            Dictionary<string, string> recommendActionExecutionParams = null)
        {
            if (recommendActionExecutionParams == null)
            {
                recommendActionExecutionParams = new Dictionary<string, string>();
            }
            foreach (var action in aRecommendActionExecutions)
            {
                foreach (string param in action.ActionInput)
                {
                    var paramSplit = param.Split('=');
                    if (paramSplit != null && paramSplit.Length == 2)
                    {
                        if (!recommendActionExecutionParams.ContainsKey(paramSplit[0]))
                        {
                            recommendActionExecutionParams.Add(paramSplit[0], string.Format(paramSplit[1],$"{System.DateTime.Now:HHmmss}"));
                        }
                    }
                    else
                    {
                        if (!recommendActionExecutionParams.ContainsKey(paramSplit[0]))
                        {
                            recommendActionExecutionParams.Add(paramSplit[0], "");
                        }
                    }
                }
            }
            return recommendActionExecutionParams;
        }
    }
}
