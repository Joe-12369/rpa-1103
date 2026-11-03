using II.RPA.Logging;
using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using II.RPA.WinAutomation.CommonUtility;
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
    internal class AnalyzerForDeskTopApp : AnalyzerBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        public AnalyzerForDeskTopApp()
        {

        }

        public override string[] SupportGuiTypes => new string[]
        {
            "windows"
        };
        public override bool TryExecuteAnalyzer(JObject aSelectorJO,
            ActionParamModel actionParamModel,
            ElementCacheProvider anElementCacheProvider,
            out List<ExecuteAction> recommendActionExecutions,
            out Dictionary<string,string> recommendActionExecutionParams)
        {
            recommendActionExecutions = null;
            recommendActionExecutionParams = null;
            try
            {
                var categoryName = aSelectorJO["elementDetail"]["categoryName"].ToString();
                var windowName = aSelectorJO["elementDetail"]["windowName"].ToString();
                if ((categoryName == windowName) 
                    && (windowName == "Windows桌面")
                    && aSelectorJO["selector"].First["props"].Any(p=>p["name"].ToString()=="class" && p["value"].ToString() == "Progman|WorkerW")
                    && aSelectorJO["selector"].Last["name"].ToString() == "ListItem")
                {
                    var nameNode = aSelectorJO["selector"].Last["props"].FirstOrDefault(p => p["name"].ToString() == "name");
                    if (nameNode != null)
                    {
                        var name = nameNode["value"].ToString();
                        //桌面文件夹
                        var deskTopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        var subFolders = SomeUtils.GetSubDirectory(deskTopPath);
                        if (!string.IsNullOrEmpty(name) && subFolders.ContainsKey(name))
                        {
                            logger.LogInformation($"Find sub folder:{name}");
                            if (LocalDataModels.SpecialElementActions.TryGetValue("explorer.folder", out List<ElementAction> anElementActions))
                            {
                                recommendActionExecutions = anElementActions.First().Actions;
                                recommendActionExecutionParams = new Dictionary<string, string>
                                {
                                    { "args0.path", JsonConvert.ToString(subFolders[name]) },
                                };
                                return true;
                            }
                            //if (LocalDataModels.SpecialElementActions.TryGetValue("explorer.folder", out List<ElementAction> anElementActions))
                            //{
                            //    recommendActionExecutions = anElementActions.First().Actions;
                            //    recommendActionExecutionParams = new Dictionary<string, string>
                            //{
                            //    { "args0.path", JsonConvert.ToString(subFolders[name]) },
                            //};
                            //    return true;
                            //}
                        }


                        var findKey = LocalDataModels.StartMenuPrograms.Keys.FirstOrDefault(p => p.Equals(name));
                        if (string.IsNullOrEmpty(findKey))
                        {
                            findKey = LocalDataModels.StartMenuPrograms.Keys.FirstOrDefault(p => p.Contains(name));
                        }
                        else if (string.IsNullOrEmpty(findKey))
                        {
                            findKey = LocalDataModels.StartMenuPrograms.Keys.FirstOrDefault(p => name.Contains(p));
                        }
                        if (!string.IsNullOrEmpty(findKey))
                        {
                            logger.LogDebug("desktop element and match startmenu");
                            if (LocalDataModels.SpecialElementActions.TryGetValue("desktop.app", out List<ElementAction> anElementActions))
                            {
                                if (findKey == "Google Chrome")
                                {
                                    recommendActionExecutions = anElementActions.Last().Actions;
                                    recommendActionExecutionParams = new Dictionary<string, string>
                                    {
                                        { "args0.executeable", JsonConvert.ToString(LocalDataModels.StartMenuPrograms[findKey]) },
                                        { "args0.type", "\"chrome\"" }
                                    };
                                    recommendActionExecutionParams = GetDefaultActionParams(recommendActionExecutions, recommendActionExecutionParams);
                                    return true;
                                }
                                else if (findKey == "Microsoft Edge")
                                {
                                    recommendActionExecutions = anElementActions.Last().Actions;
                                    recommendActionExecutionParams = new Dictionary<string, string>
                                    {
                                        { "args0.executeable", JsonConvert.ToString(LocalDataModels.StartMenuPrograms[findKey]) },
                                        { "args0.type", "\"edge\"" }
                                    };
                                    recommendActionExecutionParams = GetDefaultActionParams(recommendActionExecutions, recommendActionExecutionParams);
                                    return true;
                                }
                                else if (findKey == "Internet Explorer")
                                {
                                    recommendActionExecutions = anElementActions.Last().Actions;
                                    recommendActionExecutionParams = new Dictionary<string, string>
                                    {
                                        { "args0.executeable", JsonConvert.ToString(LocalDataModels.StartMenuPrograms[findKey]) },
                                        { "args0.type", "\"ie\"" }
                                    };
                                    recommendActionExecutionParams = GetDefaultActionParams(recommendActionExecutions, recommendActionExecutionParams);
                                    return true;
                                }
                                else if (findKey == "360安全浏览器")
                                {
                                    recommendActionExecutions = anElementActions.Last().Actions;
                                    recommendActionExecutionParams = new Dictionary<string, string>
                                    {
                                        { "args0.executeable", JsonConvert.ToString(LocalDataModels.StartMenuPrograms[findKey]) },
                                        { "args0.type", "\"Web360se\"" }
                                    };
                                    recommendActionExecutionParams = GetDefaultActionParams(recommendActionExecutions, recommendActionExecutionParams);
                                    return true;
                                }
                                recommendActionExecutions = anElementActions.First().Actions;
                                recommendActionExecutionParams = new Dictionary<string, string>
                                {
                                    { "args0.executeable", JsonConvert.ToString(LocalDataModels.StartMenuPrograms[findKey]) }
                                };
                                recommendActionExecutionParams = GetDefaultActionParams(recommendActionExecutions, recommendActionExecutionParams);
                                return true;
                            }
                        }
                        else
                        {
                            logger.LogDebug("desktop element but can not match startmenu");
                            return false;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return false;
        }
    }
}
