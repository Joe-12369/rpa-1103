using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze
{
    internal class AnalyzerForNativeDefault : AnalyzerBase
    {
        public AnalyzerForNativeDefault()
        {
            ExecutePriority = Priority.Lowest;
        }

        public override string[] SupportGuiTypes => new string[] 
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

        public override bool TryExecuteAnalyzer(JObject aSelectorJO,
            ActionParamModel actionParamModel,
            ElementCacheProvider anElementCacheProvider,
            out List<ExecuteAction> recommendActionExecutions, 
            out Dictionary<string, string> recommendActionExecutionParams)
        {
            recommendActionExecutions = new List<ExecuteAction>();
            recommendActionExecutionParams = new Dictionary<string, string>();
            var guiType = aSelectorJO["commonProps"]["GUIType"].ToString().ToLower();
            if (LocalDataModels.DefaultActions.TryGetValue(guiType, out List<ElementAction> anActions))
            {
                recommendActionExecutions = anActions.First().Actions;
            }
            else
            {
                recommendActionExecutions = LocalDataModels.DefaultActions.First().Value.First().Actions; 
            }
            foreach(var action in recommendActionExecutions)
            {
                foreach (string param in action.ActionInput)
                {
                    var paramSplit = param.Split('=');
                    if (paramSplit != null && paramSplit.Length == 2)
                    {
                        if (!recommendActionExecutionParams.ContainsKey(paramSplit[0]))
                        {
                            recommendActionExecutionParams.Add(paramSplit[0], paramSplit[1]);
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
            return true;
        }
    }
}
