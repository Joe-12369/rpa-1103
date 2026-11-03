using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace II.RPA.Plugin.GUI.ProcessCreator.ActionProvider
{
    public abstract class ActionViewModelBase:ModelBase
    {
        protected AnalyzerContainer m_AnalyzerContainer;
        private static readonly ILogger logger = Logger.OfContext();
        protected string m_UiSelector;
        protected JObject m_UiSelectorJO;
        protected Task<string> m_TaskBlockDataTask;
        protected volatile bool m_IsBeingExecuted = false;
        protected ActionViewModelBase()
        {
            ProgressRingVisiblity = Visibility.Visible;
            //ActionDataModels.s_Stopwatch.Stop();
            CurrentResolvedActionParams = new Dictionary<string, string>();
        }

        #region Properties
        public ObservableCollection<ComponentModel> SuggestionActions
        {
            get;set;
        }
        public int ItemsCount
        {
            get;
            set;
        }
        public Dpi CurrentDpi
        {
            get;
            set;
        }
        /// <summary>
        /// generate selector task
        /// </summary>
        public Task<SelectElementModel> DoSelectorTask
        {
            get;
            protected set;
        }
        public SelectElementModel DoSelectorResult
        {
            get
            {
                if (DoSelectorTask != null)
                {
                    return DoSelectorTask.GetAwaiter().GetResult();
                }
                else
                {
                    return default;
                }
            }
        }
        /// <summary>
        /// action param 图片位置、鼠标位置等
        /// </summary>
        public ActionParamModel ActionParam
        {
            get;
            set;
        }
        protected List<ExecuteAction> CurrentResolvedActions
        {
            get;
            set;
        }
        protected Dictionary<string,string> CurrentResolvedActionParams
        {
            get;
            set;
        }

        public event Action<string> CloseWindowActionEvent;

        public event Action<string> HideWindowActionEvent;
        public string StaticLoadCost
        {
            get;
            set;
        }
        #region Binding Property
        private Visibility m_ProgressRingVisiblity;
        public Visibility ProgressRingVisiblity
        {
            get => m_ProgressRingVisiblity;
            set
            {
                m_ProgressRingVisiblity = value;
                RaisePropertyChanged("ProgressRingVisiblity");
            }
        }
        /// <summary>
        /// 元素分类结果
        /// </summary>
        private string m_ElementClassText;
        public string ElementClassText
        {
            get => m_ElementClassText;
            set
            {
                m_ElementClassText = value;
                RaisePropertyChanged("ElementClassText");
            }
        }
        /// <summary>
        /// 异步绑定
        /// </summary>
        private NotifyTaskCompletion<ObservableCollection<ActionModel>> m_ActionCollectionTask;
        public NotifyTaskCompletion<ObservableCollection<ActionModel>> ActionCollectionTask
        {
            get => m_ActionCollectionTask;
            set
            {
                m_ActionCollectionTask = value;
                RaisePropertyChanged("ActionCollectionTask");
            }
        }
        #endregion
        #endregion
        #region virtual,abstract method
        protected void RaiseCloseActionEvent(string anEventParam)
        {
            CloseWindowActionEvent?.Invoke(anEventParam);
            CloseWindowActionEvent = null;
        }
        protected void RaiseHideActionEvent(string anEventParam)
        {
            HideWindowActionEvent?.Invoke(anEventParam);
            HideWindowActionEvent = null;
        }
        protected virtual void ConfigureAnalyzerContainer(AnalyzerContainer anAnalyzerContainer)
        {
            anAnalyzerContainer.RegisterAnalyzer();
        }
        /// <summary>
        /// todo
        /// </summary>
        /// <param name="aComponentId"></param>
        protected abstract void GenerateComponent(string aComponentId,string aComponentSource);
        protected Task<string> GetCurrentTaskBlockData()
        {
            return Task.Factory.StartNew(() =>
            {
                var ret = MessageService.SendMessage(TerminalType.CoreServer, "flowedit.shareTaskBlockData", "{}");
                if (ret != null)
                {
                    logger.LogDebug($"GetCurrentTaskBlockData done");
                    return ret.Info;
                }
                else
                {
                    logger.LogError($"GetCurrentTaskBlockData fail");
                }
                return "";
            },TaskCreationOptions.None);
        }
        protected void SaveActionDataForZbrain(
            string aSelectComponentId,
            JObject aSelectorJO,
            JObject aCvDataJO,
            JToken aCurrentTaskBlockData,
            JToken aSelectedTaskBlockData)
        {
            //logger.LogInformation("start SaveActionDataForZbrain task");
            try
            {
                logger.LogInformation("start SaveActionDataForZbrain");
                //var taskBlackData = await m_TaskBlockDataTask;
                JArray originalData = null;
                string aDataFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Z-Brain",
                    $"action_data_{ActionDataModels.PackagePath.Split('\\').Last()}.json");
                if (System.IO.File.Exists(aDataFilePath))
                {
                    logger.LogInformation($"loaded {aDataFilePath}");
                    using (FileStream fileStream = System.IO.File.Open(aDataFilePath, FileMode.Open))
                    {
                        using (StreamReader sw = new StreamReader(fileStream))
                        {
                            using (JsonTextReader jw = new JsonTextReader(sw))
                            {
                                JsonSerializer serializer = new JsonSerializer();
                                originalData = serializer.Deserialize<JArray>(jw);
                                logger.LogInformation($"action_data count: {originalData.Count}");
                            }
                        }
                    }
                }
                JObject dataItem = new JObject
                    {
                        {"componentName",aSelectComponentId },
                        {"commonProps.GUIType",aSelectorJO["commonProps"]?["GUIType"]?.ToString()},
                        {"commonProps.captureMode",aSelectorJO["commonProps"]?["captureMode"]?.ToString()},
                        {"commonProps.flag",aSelectorJO["commonProps"]?["flag"]?.ToString() },
                        {"commonProps.issmartcontrol",aSelectorJO["commonProps"]?["issmartcontrol"]?.ToString() },
                        {"commonProps.processName",aSelectorJO["commonProps"]?["processName"]?.ToString() },
                        {"commonProps.queryMode",aSelectorJO["commonProps"]?["queryMode"]?.ToString() },
                        {"cv.1.ClassName",aSelectorJO["selector"]?.Last()?["props"]?.FirstOrDefault(p=>"class" == p["name"].ToString())?["value"]?.ToString() },
                        {"cv.1.ControlType",aSelectorJO["selector"]?.Last()?["type"]?.ToString() },
                        {"cv.1.Name",aSelectorJO["selector"]?.Last()?["name"]?.ToString() },
                        //{"cv.2.AnchorObj.algorithm",aCvDataJO["props"].FirstOrDefault(p=> "anchorobj" == p["name"].ToString())["value"].ToString() },
                        //{"cv.2.ElementObj.algorithm",elementobj },
                        //{"cv.2.ElementObj.result_value.text_contents",cvtext },
                        {"not_ie.0.app.title",aSelectorJO["selector"]?.First()?["props"]?.First()?["value"]?.ToString() },
                        {"not_ie.0.webCtrl.tagName",aSelectorJO["elementDetail"]?["windowName"]?.ToString() },
                        {"selector.0.cssSelector",aSelectorJO["commonProps"]?["cssSelector"]?.ToString() },
                        {"selector.0.selector",aSelectorJO["selector"] },
                        //{"selector.2.ElementObj.result_value.is_unique",isUnique },
                        {"cv.categoryName",aSelectorJO["elementDetail"]?["categoryName"]?.ToString() },
                        {"componentSeries",aSelectedTaskBlockData },
                        {"componentJson",aCurrentTaskBlockData },
                    };
                if (aCvDataJO != null)
                {
                    var elementobj = aCvDataJO["props"].FirstOrDefault(p => "elementobj" == p["name"].ToString())["value"].ToString();
                    var anchorObj = aCvDataJO["props"].FirstOrDefault(p => "algorithm" == p["name"].ToString())["value"].ToString();
                    var valueObject = JsonConvert.DeserializeObject<JObject>(elementobj);
                    var cvtext = valueObject["result_value"]["text_contents"];
                    var isUnique = valueObject["result_value"]["is_unique"];

                    dataItem.Add("cv.2.ElementObj.algorithm", elementobj);
                    dataItem.Add("cv.2.AnchorObj.algorithm", anchorObj);
                    dataItem.Add("cv.2.ElementObj.result_value.text_contents", cvtext);
                    dataItem.Add("selector.2.ElementObj.result_value.is_unique", isUnique);
                }
                if (originalData == null)
                {
                    originalData = new JArray();
                }
                originalData.Add(dataItem);
                using (var fileStream = System.IO.File.Open(aDataFilePath, FileMode.Create))
                {
                    using (var sw = new StreamWriter(fileStream))
                    {
                        using (var jw = new JsonTextWriter(sw))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Serialize(jw, originalData);
                            logger.LogDebug($"SaveActionDataForZbrain Completely");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }
        
        #endregion
        protected void ExecuteGenerate(ActionModel actionModel,[CallerMemberName]string aCaller = null)
        {
            try
            {
                GenerateComponent(actionModel.ActionId, "IPARecommend");
                RunLearnOnlineTask(actionModel.ActionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            finally
            {
                m_IsBeingExecuted = true;
                CloseWindowActionEvent?.Invoke(aCaller);
                CloseWindowActionEvent = null;
            }
        }
        protected void ExecuteGenerate(ComponentModel component, [CallerMemberName] string aCaller = null)
        {
            try
            {
                GenerateComponent(component.ComponentId, "IPARecSearch");
                RunLearnOnlineTask(component.ComponentId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            finally
            {
                m_IsBeingExecuted = true;
                CloseWindowActionEvent?.Invoke(aCaller);
                CloseWindowActionEvent = null;
            }
        }
        /// <summary>
        /// init data binding
        /// </summary>
        protected void InitDataBinding()
        {
            SuggestionActions = new ObservableCollection<ComponentModel>(ActionDataModels.SearchSuggestionItems);
        }

        
        protected void RunLearnOnlineTask(string anActionId)
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    logger.LogDebug($"prepare SaveActionDataForZbrain");
                    var selectorModel = (await DoSelectorTask);
                    logger.LogDebug($"prepare DoSelectorTask");
                    var taskBlockDataString = await m_TaskBlockDataTask;
                    logger.LogDebug($"prepare m_TaskBlockDataTask");
                    JObject taskBlockData = JObject.Parse(taskBlockDataString);
                    logger.LogDebug($"start SaveActionDataForZbrain");
                    SaveActionDataForZbrain(anActionId,
                        JObject.Parse(selectorModel.Selector),
                        selectorModel.CvElementAdditionJO,
                        taskBlockData["taskBlockData"],
                        taskBlockData["selectedComponentIds"]);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "RunLearnOnlineTask Error");
                }
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false)
            .GetAwaiter()
            .OnCompleted(() => { });
        }

    }
}

