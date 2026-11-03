using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.ViewModel
{
    public class NativeActionViewModel : ActionViewModelBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        public NativeActionViewModel() : base()
        {

        }
        /// <summary>
        /// 原生元素
        /// </summary>
        /// <param name="aSelector"></param>
        public NativeActionViewModel(Task<SelectElementModel> aDoSelectorTask,
            ActionParamModel anActionParam) : base(aDoSelectorTask, anActionParam)
        {
            logger.LogDebug("NativeActionViewModel");
        }

        private string m_AsyncLoadCost;
        public string AsyncLoadCost
        {
            get => m_AsyncLoadCost;
            set
            {
                m_AsyncLoadCost = value;
                RaisePropertyChanged("AsyncLoadCost");
            }
        }
        private string m_SelectorCost;
        public string SelectorCost
        {
            get => m_SelectorCost;
            set
            {
                m_SelectorCost = value;
                RaisePropertyChanged("SelectorCost");
            }
        }
        private string m_RecommendCost;
        public string RecommendCost
        {
            get => m_RecommendCost;
            set
            {
                m_RecommendCost = value;
                RaisePropertyChanged("RecommendCost");
            }
        }
        protected override void ConfigureAnalyzerContainer(AnalyzerContainer anAnalyzerContainer)
        {
            base.ConfigureAnalyzerContainer(anAnalyzerContainer);
        }
        protected override void GenerateComponent(string aComponentId)
        {
            Task.Run(async () =>
            {
                JArray actions = new JArray();
                logger.LogInformation($"generate:{aComponentId}");
                if (CurrentResolvedActions == null || !CurrentResolvedActions.Any())
                {
                    logger.LogWarning("CurrentResolvedActions is null");
                    var actionJO = new JObject
                    {
                        { "type", aComponentId }
                    };
                    if (ElementComponents.TryGetValue(aComponentId, out string arg))
                    {
                        var propertynJO = new JObject();
                        JObject uiSelector = new JObject();
                        m_UiSelector = (await DoSelectorTask).Selector;
                        uiSelector.Add("value", m_UiSelector);
                        propertynJO.Add(arg, uiSelector);
                        actionJO.Add("properties", propertynJO);
                    }
                    actions = new JArray { actionJO };
                    logger.LogDebug($"{actionJO.ToJson()}");
                }
                else
                {
                    var actionJO = new JObject
                    {
                        { "type", aComponentId }
                    };
                    var propertynJO = new JObject();
                    ExecuteAction? currentAction = CurrentResolvedActions.FirstOrDefault(ca => ca.ActionId == aComponentId);
                    if (currentAction.HasValue && currentAction.Value.ActionInput != null)
                    {
                        logger.LogDebug($"Find {aComponentId} in CurrentResolvedActions!");
                        foreach (string input in currentAction.Value.ActionInput)
                        {
                            //有默认值
                            if (input.Contains("="))
                            {
                                string[] inputParams = input.Split('=');
                                if (inputParams.Length == 2)
                                {
                                    JObject valueJo = new JObject
                            {
                                { "value", inputParams[1] }
                            };
                                    propertynJO.Add(inputParams[0], valueJo);
                                }
                            }
                            else
                            {
                                JObject valueJo = new JObject();
                                switch (input)
                                {
                                    case "args0.uiSelector":
                                    case "args0.condition":
                                    case "args0.similar_element_object":
                                        m_UiSelector = (await DoSelectorTask).Selector;
                                        valueJo.Add("value", m_UiSelector);
                                        break;
                                    case "args0.location":
                                    case "args0.End":
                                        valueJo.Add("value", $"[{ActionParam.MouseLocation.X},{ActionParam.MouseLocation.X}]");
                                        break;
                                    case "args0.image_path":
                                    case "args0.image":
                                        var _picturePath = Path.Combine("res", Path.GetFileName(ActionParam.PicturePath));
                                        valueJo.Add("value", $"{_picturePath}");
                                        break;
                                    case "args0.executeable":
                                        valueJo.Add("value", CurrentResolvedActionParams["args0.executeable"]);
                                        break;
                                    case "args0.itemsToSelect":
                                    case "args0.text":
                                        try
                                        {
                                            var elementText = ActionParam.CurrentGuiNode.GetText();
                                            if (!string.IsNullOrEmpty(elementText))
                                            {
                                                valueJo.Add("value", $"\"{ActionParam.CurrentGuiNode.GetText()}\"");
                                            }
                                        }
                                        catch
                                        {
                                            logger.LogError($"Fail to set select item for combobox");
                                        }
                                        break;
                                    default:
                                        if (CurrentResolvedActionParams.TryGetValue(input, out string value))
                                        {
                                            valueJo.Add("value", value);
                                        }
                                        else
                                        {
                                            logger.LogError($"There is invalid arg:{input}");
                                        }
                                        break;
                                }
                                propertynJO.Add(input, valueJo);
                            }
                        }
                    }
                    else
                    {
                        if (ElementComponents.TryGetValue(aComponentId, out string arg))
                        {
                            JObject uiSelector = new JObject();
                            m_UiSelector = (await DoSelectorTask).Selector;
                            uiSelector.Add("value", m_UiSelector);
                            propertynJO.Add(arg, uiSelector);
                            //actionJO.Add("properties", propertynJO);
                            logger.LogDebug($"ElementComponents get {aComponentId}");
                        }
                        else
                        {
                            logger.LogDebug($"Cant get {aComponentId} from ElementComponents");
                        }
                        logger.LogDebug($"can not find {aComponentId} in CurrentResolvedActions.Maybe search");
                    }
                    actionJO.Add("properties", propertynJO);
                    actions = new JArray { actionJO };
                    logger.LogDebug($"{actionJO.ToJson()}");
                }
                if (MessageService.PostMessage(TerminalType.CoreServer, "flowedit.plugingenerate", actions, (ret) =>
                {
                    logger.LogDebug($"post return : {ret}");
                }))
                {
                    logger.LogDebug($"post done");
                }
                else
                {
                    logger.LogError($"post fail");
                }
            });
        }
    }
}
