using II.RPA.Logging;
using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze
{
    internal class AnalyzerForWindows : AnalyzerBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        private readonly string[] m_PictureSuffixes = new string[]
        {
            "jpg","jpeg","png","svg","gif","ico","bmp","tif",
        };
        /// <summary>
        /// 只此一个构造
        /// </summary>
        /// <param name="anActionParam"></param>
        /// <param name="aSelectTask"></param>
        public AnalyzerForWindows()
        {
            ExecutePriority = Priority.Lowest;
        }

        public override string[] SupportGuiTypes => new string[] 
        {
            "windows"
        };

        public override bool TryExecuteAnalyzer(JObject aSelectorJO,
            ActionParamModel actionParamModel,
            ElementCacheProvider anElementCacheProvider,
            out List<ExecuteAction> recommendActionExecutions, 
            out Dictionary<string, string> recommendActionExecutionParams)
        {
            recommendActionExecutions = null;
            recommendActionExecutionParams = null;
            if (aSelectorJO != null)
            {
                try
                {
                    var processName = actionParamModel.ProcessName;
                  
                    var control = aSelectorJO["selector"].Last["name"].ToString();
                    logger.LogInformation($"control:{control}");
                    if (LocalDataModels.NativeElementActions.TryGetValue(control.ToLower(), out List<ElementAction> elementActions))
                    {
                        logger.LogInformation($"Get NativeAction：{control}");
                        if (elementActions.Count > 1)
                        {
                            foreach (var action in elementActions)
                            {
                                if (action.Rules != null && action.Rules.Any())
                                {
                                    if (GetRulePredicate(action.Rules.First()).Invoke(aSelectorJO))
                                    {
                                        recommendActionExecutions = action.Actions;
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    recommendActionExecutions = action.Actions;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            recommendActionExecutions = elementActions.First().Actions;
                        }
                        if (recommendActionExecutions.Any())
                        {
                            if (!string.IsNullOrEmpty(processName) 
                                && (processName.ToLower().Equals("excel") 
                                || processName.ToLower().Equals("wps")
                                || processName.ToLower().Equals("et")))
                            {
                                if (LocalDataModels.SpecialElementActions.TryGetValue("excel.record", out List<ElementAction> excelRecordActions))
                                {
                                    if (excelRecordActions != null && excelRecordActions.Any())
                                    {
                                        if (!recommendActionExecutions.Any(a => a.ActionId == "excel.record"))
                                        {
                                            logger.LogDebug("Insert excel.record!");
                                            recommendActionExecutions.Insert(0, excelRecordActions.First().Actions.First());
                                            return true;
                                        }
                                        else
                                        {
                                            logger.LogWarning("Duplicate excel.record!");
                                        }
                                    }
                                }
                            }
                            return true;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(processName)
                               && (processName.ToLower().Equals("excel")
                               || processName.ToLower().Equals("wps")
                               || processName.ToLower().Equals("et")))
                            {
                                if (LocalDataModels.SpecialElementActions.TryGetValue("excel.record", out List<ElementAction> excelRecordActions))
                                {
                                    if (excelRecordActions != null && excelRecordActions.Any())
                                    {
                                        logger.LogDebug("Add excel.record!");
                                        recommendActionExecutions.Insert(0, excelRecordActions.First().Actions.First());
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    logger.LogError(ex);
                }
            }
            return false;
        }
        private Predicate<JObject> GetRulePredicate(string aRuleKey)
        {
            switch (aRuleKey)
            { 
                case "windows.explorer.picture":
                    return (selectorJO) =>
                    {
                        try
                        {
                            var rulevalue = selectorJO["selector"].Last["props"].FirstOrDefault(p => p["name"].ToString().ToLower().Equals("name"));
                            if (rulevalue != null && rulevalue["value"].ToString().Contains(".") && m_PictureSuffixes.Contains(rulevalue["value"].ToString().Split('.').Last().ToLower()))
                            {
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning("GetRulePredicate", ex);
                        }
                        return false;
                    };
                case "combobox.list.listitem":
                    return (selectorJO) =>
                    {
                        return SelectorClass(aRuleKey, selectorJO);
                    };
                case "combobox.list":
                    return (selectorJO) =>
                    {
                        return SelectorClass(aRuleKey, selectorJO);
                    };
                default:
                    logger.LogError($"GetRulePredicate unavaliable key:{aRuleKey}");
                    return param => false;
            }
        }
        private bool SelectorClass(string controlType, JObject selectorJO)
        {
            try
            {
                if (string.IsNullOrEmpty(controlType))
                {
                    return false;
                }
                string[] classes = controlType.Split('.');
                if (classes == null || !classes.Any())
                {
                    classes = new string[] { controlType };
                }
                if (classes.Length > selectorJO["selector"].Count())
                {
                    return false;
                }
                else
                {
                    var selectorNodes = selectorJO["selector"].AsEnumerable().Reverse().ToList();
                    var classes_reverse = classes.AsEnumerable().Reverse().ToList();
                    for (int i = 0; i < classes.Length; i++)
                    {
                        var node = selectorNodes[i];
                        var nodeprop = selectorNodes[i]["name"];

                        if (nodeprop != null && nodeprop.ToString().ToLower() == classes_reverse[i])
                        {
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("GetRulePredicate", ex);
            }
            return false;
        }
    }
}
