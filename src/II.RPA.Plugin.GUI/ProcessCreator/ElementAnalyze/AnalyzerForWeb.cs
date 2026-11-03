using II.RPA.Logging;
using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze
{
    internal class AnalyzerForWeb : AnalyzerBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        public AnalyzerForWeb()
        { 

        }
        public override string[] SupportGuiTypes => new string[]
        {
            "Chrome",
            "IE",
            "FireFox",
            "ZExplorer",
            "Edge",
            "Web360se",
            "Sogou",
            "SuperBrowser",
        };

        public override bool TryExecuteAnalyzer(JObject selectorJO,
            ActionParamModel actionParamModel,
            ElementCacheProvider anElementCacheProvider,
            out List<ExecuteAction> recommendActionExecutions, 
            out Dictionary<string, string> recommendActionExecutionParams)
        {
            recommendActionExecutions = null;
            recommendActionExecutionParams = new Dictionary<string, string>();
            var firstNode = selectorJO["selector"].First;
            if (firstNode != null)
            {
                var urlNode = firstNode["props"].FirstOrDefault(n => n["name"].ToString() == "url");
                if (urlNode != null)
                {
                    var url = urlNode["value"].ToString();
                    if (LocalDataModels.WebActions.TryGetValue("items_list", out List<ElementAction> url_actions))
                    {
                        var action = url_actions.First();
                        if (action.Rules != null && action.Rules.Any())
                        {
                            foreach (var urlKey in action.Rules)
                            {
                                if (!string.IsNullOrEmpty(urlKey) && url.Contains(urlKey))
                                {
                                    logger.LogInformation($"Match items_list:{url}");
                                    recommendActionExecutions = action.Actions;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            if (LocalDataModels.WebActions.TryGetValue(actionParamModel.ControlType.ToLower(), out List<ElementAction> actions))
            {
                if (actions != null && actions.Any())
                {
                    if (actions.Count > 1)
                    {
                        ElementAction? actionWanted = null;
                        var lastNode = selectorJO["selector"].Last();
                        var typeNode = lastNode["props"].FirstOrDefault(n => n["name"].ToString() == "type");
                        if (typeNode != null && typeNode["value"] != null)
                        {
                            logger.LogInformation($"typeNode:{typeNode["value"]}");
                            actionWanted = actions.FirstOrDefault(a => !string.IsNullOrEmpty(a.MatchType) &&
                            a.MatchType.Equals(typeNode["value"].ToString(), StringComparison.OrdinalIgnoreCase));
                            if (!actionWanted.HasValue || actionWanted.Value.Actions == null)
                            {
                                var classNode = lastNode["props"].FirstOrDefault(n => n["name"].ToString() == "class");
                                if (classNode != null && classNode["value"] != null)
                                {
                                    logger.LogInformation($"classNode:{classNode["value"]}");
                                    actionWanted = actions.FirstOrDefault(a => a.MatchClasses!=null 
                                    && a.MatchClasses.Any() 
                                    && a.MatchClasses.Contains(classNode["value"].ToString(),
                                          new AnalyzerEqualityComparer()));
                                }
                            }
                        }
                        if (!actionWanted.HasValue || actionWanted.Value.Actions == null)
                        {
                            actionWanted = actions.First();
                            logger.LogWarning($"Find {actions.Count} actions.Can not determine which one should be picked!");
                        }
                        recommendActionExecutions = actionWanted.Value.Actions;
                    }
                    else
                    {
                        recommendActionExecutions = actions.First().Actions;
                    }
                    return true;
                }
                else
                {
                    logger.LogError($"Get empty List<ElementAction>!");
                    return false;
                }
            }
            else
            {
                logger.LogError($"Find mismatch web element type:[{actionParamModel.ControlType}]");
                return false;
            }
        }
    }
}
