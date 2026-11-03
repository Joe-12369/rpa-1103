using II.RPA.Logging;
using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze
{
    internal class AnalyzerForBrowserAddressBar : AnalyzerBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        /// <summary>
        /// 只此一个构造
        /// </summary>
        /// <param name="anActionParam"></param>
        /// <param name="aSelectTask"></param>
        public AnalyzerForBrowserAddressBar()
        {
            ExecutePriority = Priority.High;
        }

        public override string[] SupportGuiTypes => new string[] 
        {
            "windows"
        };

        public override bool TryExecuteAnalyzer(ActionParamModel actionParamModel,
            out List<ExecuteAction> recommendActionExecutions, 
            out Dictionary<string, string> recommendActionExecutionParams)
        {
            recommendActionExecutions = null;
            recommendActionExecutionParams = null;
            string edit_text = null;
            string browser_type = null;
            try
            {
                edit_text = actionParamModel.CurrentGuiNode?.GetText();
                browser_type = actionParamModel.ProcessName?.ToLower();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            if (LocalDataModels.SpecialElementActions.TryGetValue("browser.edit", 
                out List<ElementAction> actions))
            {
                recommendActionExecutions = actions.First().Actions;
                recommendActionExecutionParams = new Dictionary<string, string>();
                logger.LogDebug($"edit address:{edit_text}");
                if (!string.IsNullOrEmpty(edit_text))
                {
                    recommendActionExecutionParams.Add("args0.url", JsonConvert.ToString(edit_text));
                }
                else
                {
                    recommendActionExecutionParams.Add("args0.url", "\"http://www.baidu.com\"");
                }
                if (!string.IsNullOrEmpty(browser_type))
                {
                    if (browser_type.Contains("chrome"))
                    {
                        recommendActionExecutionParams.Add("args0.type", "\"chrome\"");
                    }
                    else if (browser_type.Contains("edge"))
                    {
                        recommendActionExecutionParams.Add("args0.type", "\"edge\"");
                    }
                    else
                    {
                        logger.LogError($"unsupported type:{browser_type}");
                    }
                }
                return true;
            }
            return false;
        }
    }
}
