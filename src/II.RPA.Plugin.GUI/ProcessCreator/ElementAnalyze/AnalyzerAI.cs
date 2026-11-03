using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze
{
    internal class AnalyzerAI : AnalyzerBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        public AnalyzerAI()
        {
        }
        public override Priority ExecutePriority { get => base.ExecutePriority; set => base.ExecutePriority = value; }

        public override string[] SupportGuiTypes => base.SupportGuiTypes;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public async Task<AnalyzeResult?> ExecuteAnalyzer(JObject aSelectorJO,
            ActionParamModel actionParamModel,
            IEnumerable<string> aCurrentComponents)
        {
            //recommendActionExecutionParams = null;
            try
            {
                string elementName = string.Empty;
                if (actionParamModel.GuiType != eGuiType.CV)
                {
                    elementName = actionParamModel.CurrentGuiNode.Name;
                }
                Dictionary<string, object> requestObject = new Dictionary<string, object>
                {
                    {"components",aCurrentComponents},
                    { "element",aSelectorJO},
                    { "elementbase",new Dictionary<string, object> 
                    {
                        {"GUIType",actionParamModel.GuiType},
                        {"processName",actionParamModel.ProcessName},
                        {"elementName",elementName},
                    } }
                };
                logger.LogDebug($"start ai recommend");
                var responseString = await AnalyzerContainer.ZbrainServiceInstance.AIRecommend(requestObject.ToJson());
                logger.LogDebug($"recommend result:{responseString}");
                var jArray = JArray.Parse(responseString);
                if (jArray != null && jArray.Any())
                {
                    var recommendActionExecutions = jArray.Where(a=>!ActionDataModels.BlackActionLogos.Contains(a["content"].ToString())).Select(p => new ExecuteAction
                    {
                        ActionId = p["content"].ToString(),
                        ActionIsAI = true
                    }).ToList();
                    return new AnalyzeResult 
                    {
                        RecommendActionExecutions = recommendActionExecutions, 
                    };
                }
                else
                {
                    logger.LogError($"componentResult:error");
                    return default;
                }

            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex,"Fail to AnalyzerAI");
            }
            return default;
        }
        public async Task<AnalyzeResult?> ExecuteAnalyzerWithNativeActions(JObject aSelectorJO,
            ActionParamModel actionParamModel,
            IEnumerable<string> aCurrentComponents,
            IEnumerable<ActionModel> aNativeActions)
        {
            //recommendActionExecutionParams = null;
            try
            {
                IEnumerable<string> nativeActionLogos = null;
                if (aNativeActions != null && aNativeActions.Any())
                {
                    nativeActionLogos = aNativeActions.Select(na => na.ActionLogo);
                }
                else
                {
                    nativeActionLogos = new string[] { };
                }
                Dictionary<string, object> requestObject = new Dictionary<string, object>
                {
                    {"components",aCurrentComponents},
                    { "element",aSelectorJO},
                    { "rule_rec_comps",nativeActionLogos}
                };
                logger.LogDebug($"start ai recommend");
                var responseString = await AnalyzerContainer.ZbrainServiceInstance.AIRecommend(requestObject.ToJson());
                logger.LogDebug($"recommend result:{responseString}");
                var jArray = JArray.Parse(responseString);
                if (jArray != null && jArray.Any())
                {
                    var recommendActionExecutions = jArray.Select(p => new ExecuteAction
                    {
                        ActionId = p["content"].ToString(),
                        ActionIsAI = true
                    }).ToList();
                    return new AnalyzeResult
                    {
                        RecommendActionExecutions = recommendActionExecutions,
                    };
                }
                else
                {
                    logger.LogError($"componentResult:error");
                    return default;
                }

            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Fail to AnalyzerAI");
            }
            return default;
        }
    }
}

