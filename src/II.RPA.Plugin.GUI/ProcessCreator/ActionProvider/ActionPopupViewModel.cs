using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.UI;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using II.RPA.Plugin.GUI.ProcessCreator.Controls;
using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using II.RPA.Plugin.GUI.ProcessCreator.Models;
using II.RPA.Plugin.GUI.ProcessCreator.Mvvm;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using ListBox = System.Windows.Controls.ListBox;

namespace II.RPA.Plugin.GUI.ProcessCreator.ActionProvider
{
    internal class ActionPopupViewModel : ActionViewModelBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        protected static readonly List<string> CurrentActions = new List<string>();
        private static ActionPopupViewModel m_ActionPopupViewModelSingleton = null;
        private static Dispatcher Dispatcher;
        private static ObservableCollection<ActionModel> loadingIcons = new ObservableCollection<ActionModel>(Enumerable.Range(0, 6).Select(x => new ActionModel { ActionLogo = "Loading" }));
        public event Action<string> ResolveOverEvent;
        public event Action<bool> NeedHoldonEvent;
        private IEnumerable<ActionModel> m_ResolvedActions = new List<ActionModel>();
        private ZbrainService m_ZbrainService;
        private readonly object m_ExeLock = new object();
        private Action<ActionParamModel> m_ActionDoneCallBack;
        private IPluginInvoker m_PluginInvoker;
        private PluginExecutionContext m_PluginExecutionContext;

        #region 获取单例
        public static ActionPopupViewModel GetActionPopupViewModelSingleton(Dispatcher dispatcher)
        {
            if (m_ActionPopupViewModelSingleton == null)
            {
                Dispatcher = dispatcher;
                m_ActionPopupViewModelSingleton = new ActionPopupViewModel();
            }
            return m_ActionPopupViewModelSingleton;
        }
        #endregion

        private ActionPopupViewModel() : base()
        {
            CurrentActions.Add("main");
            m_AnalyzerContainer = AnalyzerContainer.AnalyzerContainerInstance;

            ActionCommand = new DelegateCommand(ActionCommandExe);
            SelectionChangedCommand = new NMCommand<ExCommandParameter>(SelectionChangedCommandExe);
            MainActionCommand = new DelegateCommand(MainActionCommandExe);
            GetInputTextCommand = new DelegateCommand(GetInputTextCommandExe);
            RecordSelectionChangedCommand = new NMCommand<ExCommandParameter>(RecordSelectionChangedCommandExe);
            InputPopupLoadedCommand = new DelegateCommand(InputPopupLoadedCommandExe);
            DissCommand = new DelegateCommand(DissCommandExe);
            CheckBoxOperationCommand = new DelegateCommand(CheckBoxOperationCommandExe);
            CaptureCursorCommand = new DelegateCommand(CaptureCursorCommandExe);
            FileOrFolderMoveCommand = new DelegateCommand(FileOrFolderMoveCommandExe);
            ActionConfirmCommand = new DelegateCommand(ActionConfirmCommandExe);
            SingleBtnActionCommand = new DelegateCommand(SingleBtnActionCommandExe);

            LogicActionCollection = new ObservableCollection<ActionModel>(ActionDataModels.RecommendLogicActions);
            HistoryRecordCollection = new ObservableCollection<string>
            {
               eCommonMessageType.ActionPopupViewModel_cs_WangYe.GetMessage(),
               eCommonMessageType.ActionPopupViewModel_cs_YuanSu.GetMessage(),
               eCommonMessageType.ActionPopupViewModel_cs_LiuLanQi.GetMessage(),
               "ai",
               eCommonMessageType.ActionPopupViewModel_cs_WenBen.GetMessage(),
               eCommonMessageType.ActionPopupViewModel_cs_DianJi.GetMessage(),
               eCommonMessageType.SheetEdit_cs_TuPian.GetMessage(),
               eCommonMessageType.ActionPopupViewModel_cs_XunHuan.GetMessage(),
               eCommonMessageType.ActionPopupViewModel_cs_BianLiang.GetMessage(),
               eCommonMessageType.ActionPopupViewModel_cs_YiChang.GetMessage(),
               eCommonMessageType.ActionPopupViewModel_cs_RiZhi.GetMessage(),
            };
            //"网页","元素","浏览器","ai","文本","点击","图片","循环","变量","异常","日志"

            if (LocalDataModels.SearchHistoryList != null && LocalDataModels.SearchHistoryList.Count > 0)
            {
                LocalDataModels.SearchHistoryList.ForEach(s =>
                {
                    if (!HistoryRecordCollection.Contains(s))
                    {
                        HistoryRecordCollection.Insert(0, s);
                    }
                });
            }
            try
            {
                //PreInitActionCollection();
                ComponentCollectionView = CollectionViewSource.GetDefaultView(ActionDataModels.Components);
                if (ComponentCollectionView == null)
                {
                    logger.LogError("ComponentCollectionView == null");
                }
                Dispatcher.InvokeAsync(() =>
                {
                    ComponentCollectionView.Filter = ContainsFilter;
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ActionPopupViewModel()");
            }
            InputActionButtonVisibility = Visibility.Collapsed;
        }

        internal void BindPluginInvocation(IPluginInvoker pluginInvoker, PluginExecutionContext executionContext)
        {
            m_PluginInvoker = pluginInvoker ?? throw new ArgumentNullException(nameof(pluginInvoker));
            m_PluginExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        }

        #region binding commmands
        public ICommand ActionCommand
        {
            get;
            set;
        }
        public NMCommand<ExCommandParameter> SelectionChangedCommand
        {
            get;
            set;
        }
        public ICommand MainActionCommand
        {
            get;
            set;
        }
        public ICommand GetInputTextCommand
        {
            get;
            set;
        }
        public ICommand InputPopupLoadedCommand
        {
            get;
            set;
        }
        public ICommand DissCommand
        {
            get;
            set;
        }
        public NMCommand<ExCommandParameter> RecordSelectionChangedCommand
        {
            get;
            set;
        }
        public ICommand CheckBoxOperationCommand
        {
            get;
            set;
        }
        public ICommand ActionConfirmCommand
        {
            get;
            set;
        }
        public ICommand CaptureCursorCommand
        {
            get;
            set;
        }

        public ICommand FileOrFolderMoveCommand
        {
            get;
            set;
        }

        public ICommand SingleBtnActionCommand
        {
            get;
            set;
        }
        #endregion

        #region binding properties

        public ICollectionView ComponentCollectionView
        {
            get;
            set;
        }
        public ObservableCollection<ActionModel> LogicActionCollection
        {
            get;
            set;
        }
        public string ElementInputText
        {
            get;
            set;
        }
        public ObservableCollection<string> HistoryRecordCollection
        {
            get;
            set;
        }
        private ICollectionView actionCollectioView;
        public ICollectionView ActionCollectioView
        {
            get
            {
                return actionCollectioView;
            }
            set
            {
                actionCollectioView = value;
                RaisePropertyChanged("ActionCollectioView");
            }
        }
        private Visibility inputActionButtonVisibility;
        public Visibility InputActionButtonVisibility
        {
            get
            {
                return inputActionButtonVisibility;
            }
            set
            {
                inputActionButtonVisibility = value;
                RaisePropertyChanged("InputActionButtonVisibility");
            }
        }
        private string filter;
        public string Filter
        {
            get
            {
                return this.filter;
            }
            set
            {
                this.filter = value;
                RaisePropertyChanged("Filter");
                this.ComponentCollectionView.Refresh();
            }
        }


        //public ICommand TextChangedCommand
        //{
        //    get;
        //    set;
        //}
        #endregion

        /// <summary>
        /// BuildViewModel
        /// </summary>
        /// <param name="aDoSelectorTask"></param>
        /// <param name="anActionParam"></param>
        /// <param name="anIsSlimOnly">暂时弃用</param>
        /// <param name="aZbrainService"></param>
        public void BuildViewModel(Task<SelectElementModel> aDoSelectorTask,
            ActionParamModel anActionParam,
            bool anIsSlimOnly,
            ZbrainService aZbrainService,
            ElementCacheProvider anElementCacheProvider,
            Action<ActionParamModel> anActionDoneCallBack)
        {
            //SomeUtils.TraceThreadPool();
            logger.LogDebug("BuildViewModel Start");
            ElementClassText = eCommonMessageType.ActionPopupViewModel_cs_ZhiNengFenXiZhong.GetMessage();
            m_IsBeingExecuted = false;
            m_ZbrainService = aZbrainService;

            InitDataBinding();
            m_TaskBlockDataTask = GetCurrentTaskBlockData();
            m_TaskBlockDataTask.ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });
            SetActionCollection(loadingIcons);
            DoSelectorTask = aDoSelectorTask;
            ActionParam = anActionParam;
            m_AnalyzerContainer.InitAnalyzerContainer(ActionParam,
                DoSelectorTask,
                CurrentActions,
                aZbrainService,
                anElementCacheProvider);
            ConfigureAnalyzerContainer(m_AnalyzerContainer);
            ResolveAction(m_AnalyzerContainer).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });
            m_ActionDoneCallBack = anActionDoneCallBack;
            //ActionCollectioView = CollectionViewSource.GetDefaultView(m_ResolvedActions);
        }
        private void DissCommandExe(object obj)
        {
            if (obj is ActionModel action)
            {
                EventTrackerModel eventTraker = EventTrackerModel.SetEventModel("action.dismiss", new JObject { { "actiontype", action.ActionId } });
                MessageService.PostMessage(TerminalType.CoreServer, "flowedit.tracking", eventTraker.ToString(), _ => { });
                action.IsDiss = true;
                if (m_ResolvedActions.Count(a => !a.IsDiss) > 0)
                {
                    ActionCollectioView.Refresh();
                }
                ComponentModel component = ActionDataModels.Components.FirstOrDefault(c => c.ComponentId == action.ActionId);
                _ = m_ZbrainService.DissFeedBack(new JArray(component.ComponentLogo).ToString());
            }
            else
            {
                logger.LogInformation($"obj is not ActionModel:{obj}");
            }
        }

        private void InputPopupLoadedCommandExe(object obj)
        {
            var setvaluecomponent = ActionDataModels.Components.FirstOrDefault(c => (!string.IsNullOrEmpty(c.ComponentLogo) && c.ComponentLogo.Equals("Element_SetValue")));
            if (obj is System.Windows.Controls.TextBox box)
            {
                box.Focus();
            }
            else
            {
                logger.LogInformation($"obj is not TextBox:{obj}");
            }
        }
        private void ActionConfirmCommandExe(object objs)
        {
            logger.LogDebug($"ActionConfirmCommandExe:{objs}");
            if (objs is object[] paramList)
            {
                logger.LogDebug($"ActionConfirmCommandExe:{paramList[0]},{paramList[1]},{paramList[2]}");
                var attachValue = paramList[0].ToString();
                var tag = paramList[1].ToString();
                var content = paramList[2];

                var component = ActionDataModels.Components.FirstOrDefault(c => (!string.IsNullOrEmpty(c.ComponentLogo) && c.ComponentLogo.Equals(tag)));
                var paramDictionary = new Dictionary<string, string>();
                switch (component.ComponentLogo)
                {
                    case "logic_control_count_for":
                        paramDictionary.Add("args0.count", attachValue);
                        break;
                    case "Element_Region":
                        if (attachValue == "0")
                        {
                            paramDictionary.Add("args0.position", "2");
                        }
                        else
                        {
                            paramDictionary.Add("args0.position", "3");
                        }
                        break;
                    default:
                        if (attachValue == "0")
                        {
                            paramDictionary.Add("args0.isExists", "True");
                        }
                        else
                        {
                            paramDictionary.Add("args0.isExists", "False");
                        }
                        break;
                }

                GenerateComponentInternal(component.ComponentId, paramDictionary, true, "IPARecommend");
                RunLearnOnlineTask(component.ComponentId);
                RaiseCloseActionEvent(component.ComponentLogo);
            }
        }
        private void ActionCommandExe(object obj)
        {
            if (obj is ActionModel action)
            {
                ExecuteGenerate(action);
            }
        }
        private void SelectionChangedCommandExe(ExCommandParameter anObj)
        {
            lock (m_ExeLock)
            {
                if (anObj == null || anObj.Parameter == null)
                {
                    logger.LogInformation($"SelectionChangedCommandExe: ignore!");
                    return;
                }
                logger.LogInformation($"SelectionChangedCommandExe: {anObj?.Parameter?.GetType()}");
                try
                {
                    if (m_IsBeingExecuted)
                    {
                        logger.LogWarning($"SelectionChangedCommandExe Duplicate");
                        return;
                    }
                    if (!string.IsNullOrEmpty(Filter))
                    {
                        LocalDataModels.SearchHistoryList.Insert(0, Filter);
                    }
                    if (anObj.Parameter is ActionModel action)
                    {
                        if (action.ActionLogo != "Loading")
                        {
                            ExecuteGenerate(action);
                        }
                    }
                    else if (anObj.Parameter is ComponentModel component)
                    {
                        ExecuteGenerate(component);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
                finally
                {
                    if (anObj.EventArgs is SelectionChangedEventArgs e)
                    {
                        e.Handled = true;
                    }
                }
            }
        }
        private void CheckBoxOperationCommandExe(object obj)
        {
            logger.LogDebug($"CheckBoxOperationCommandExe:{obj}");
            if (int.TryParse($"{obj}", out int index))
            {
                if (index == -1)
                {
                    return;
                }
                var checkboxcomponent = ActionDataModels.Components.FirstOrDefault(c => (!string.IsNullOrEmpty(c.ComponentLogo) && c.ComponentLogo.Equals("Element_SetCheck")));
                var paramDictionary = new Dictionary<string, string>();
                if (index == 0)
                {
                    paramDictionary.Add("args0.operation_type", "1");
                }
                else if (index == 1)
                {
                    paramDictionary.Add("args0.operation_type", "0");
                }
                else if (index == 2)
                {
                    paramDictionary.Add("args0.operation_type", "2");
                }
                GenerateComponentInternal(checkboxcomponent.ComponentId, paramDictionary, true, "IPARecommend");
                RunLearnOnlineTask(checkboxcomponent.ComponentId);
                RaiseCloseActionEvent(checkboxcomponent.ComponentLogo);
            }
            else
            {
                logger.LogError($"CheckBoxOperationCommandExe Parse Error:{obj}");
            }
        }
        private void CaptureCursorCommandExe(object anArgs)
        {
            Point cursorLocation = default(Point);
            var win = new CaptureCursorWindow((point) =>
            {
                logger.LogDebug($"point:{point}");
                cursorLocation = point;
            });
            if (ActionDataModels.CurrentActionWindow.IsEnabled)
            {
                win.Owner = ActionDataModels.CurrentActionWindow;
            }
            win.Topmost = true;
            if (win.ShowDialog() == true)
            {
                Task.Factory.StartNew(async () =>
                {
                    if (anArgs is bool isThisLocation)
                    {
                        var checkboxcomponent = ActionDataModels.Components.FirstOrDefault(c => (!string.IsNullOrEmpty(c.ComponentLogo) && c.ComponentLogo.Equals("Mouse_DragTo")));
                        var paramDictionary = new Dictionary<string, string>
                        {
                            { "args0.position", "0" }
                        };
                        if (isThisLocation)
                        {

                            paramDictionary.Add("args0.dragType", "0");
                            paramDictionary.Add("args0.Start", $"[{(int)(ActionParam.MouseLocation.X)},{(int)(ActionParam.MouseLocation.Y)}]");
                        }
                        else
                        {
                            paramDictionary.Add("args0.dragType", "1");
                            m_UiSelector = (await DoSelectorTask).Selector;
                            paramDictionary.Add("args0.uiSelector", m_UiSelector);
                        }
                        paramDictionary.Add("args0.End", $"[{(int)(cursorLocation.X)},{(int)(cursorLocation.Y)}]");
                        GenerateComponentInternal(checkboxcomponent.ComponentId, paramDictionary, false, "IPARecommend");
                        RunLearnOnlineTask(checkboxcomponent.ComponentId);
                        RaiseCloseActionEvent(checkboxcomponent.ComponentLogo);
                    }
                }).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });

            }
            else
            {
                logger.LogDebug($"CaptureCursorCommandExe:cancel");
            }
        }

        private void FileOrFolderMoveCommandExe(object obj)
        {
            try
            {
                var actionlogo = obj.ToString();
                var setvaluecomponent = ActionDataModels.Components.FirstOrDefault(c => (!string.IsNullOrEmpty(c.ComponentLogo) && c.ComponentLogo.Equals(actionlogo)));
                string filePath = string.Empty;
                switch (actionlogo)
                {
                    case "File_MoveFolder":
                        CurrentResolvedActionParams.TryGetValue("args0.path", out filePath);
                        break;
                    case "File_File_Move":
                        CurrentResolvedActionParams.TryGetValue("args0.pathSrc", out filePath);
                        break;
                }
                if (setvaluecomponent != null)
                {
                    NeedHoldonEvent.Invoke(true);
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        string pathDest = "";
                        FolderBrowserDialog folder = new FolderBrowserDialog();
                        folder.Description = "目标文件夹".I18n("ActionPopupViewModel_C5208CDC");
                        if (folder.ShowDialog() == DialogResult.OK)
                        {
                            pathDest = folder.SelectedPath;
                            GenerateComponentInternal(setvaluecomponent.ComponentId,
                            new Dictionary<string, string> {
                                { "args0.overwrite", this.IsOverwriteFile ? "True":"Falses" },
                                { "args0.pathSrc", $"{filePath}" },
                                { "args0.pathDest", $"\"{pathDest.Replace(@"\",@"\\")}\"" }},
                            false,
                            "IPARecommend");
                            RunLearnOnlineTask(setvaluecomponent.ComponentId);
                            RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"FileOrFolderMoveCommandExe:{ex.ToString()}");
            }
            finally
            {
                NeedHoldonEvent.Invoke(false);
            }

        }

        private async void SingleBtnActionCommandExe(object obj)
        {
            UIProvider uIProvider = new UIProvider();
            var actionlogo = obj.ToString();
            var setvaluecomponent = ActionDataModels.Components.FirstOrDefault(c => (!string.IsNullOrEmpty(c.ComponentLogo) && c.ComponentLogo.Equals(actionlogo)));
            string filePath = string.Empty;
            switch (actionlogo)
            {
                case "loop_similar_elements":
                    try
                    {
                        if (setvaluecomponent != null)
                        {
                            NeedHoldonEvent.Invoke(true);
                            string captureSimilarElements = "";
                            await GuiElement.overlayForm.inspectorStatus.Dispatcher.InvokeAsync(async () =>
                            {
                                m_UiSelector = (await DoSelectorTask).Selector;
                                var captureSimilarParam = new JObject
                                        {
                                            { "selector",m_UiSelector },
                                            { "filepath",$"res\\{Guid.NewGuid().ToString()}.png" },
                                            { "isCVCapture",false },
                                            { "cvServer", "" },
                                            { "InspectStatus",0 },
                                        };
                                var captureResult = await m_PluginInvoker.InvokeAsync(
                                    new PluginRoute("Element", "CaptureSimilar"),
                                    captureSimilarParam.ToString());
                                var msg = captureResult.ToObject<JMessage>();
                                if (msg == null)
                                {
                                    uIProvider.ShowTips("循环相似元素".I18n("ActionPopupViewModel_CE268AF1"), "选择相似元素目标异常".I18n("ActionPopupViewModel_A7E35445"), UIProviders.Tips.TipLevel.ERROR);
                                }
                                if (msg.Code == 0)
                                {
                                    captureSimilarElements = ConvertJsonString(captureResult.ToObject<JMessage>()?.Info);
                                }
                                else
                                {
                                    uIProvider.ShowTips("循环相似元素".I18n("ActionPopupViewModel_CE268AF1"), msg.Message, UIProviders.Tips.TipLevel.ERROR);
                                }

                            }).Task.Unwrap();
                            if (!string.IsNullOrEmpty(captureSimilarElements))
                            {
                                JObject uiSelector = JToken.Parse(m_UiSelector) as JObject;
                                JObject similarSelector = JToken.Parse(captureSimilarElements) as JObject;
                                var picturePath = uiSelector["commonProps"]["filePath"]?.ToString();
                                similarSelector["commonProps"]["filePath"] = Path.Combine("res", Path.GetFileName(picturePath));
                                similarSelector["elementDetail"]["categoryIcon"] = Path.Combine("res", uiSelector["elementDetail"]["categoryIcon"]?.ToString());

                                GenerateComponentInternal(setvaluecomponent.ComponentId,
                                new Dictionary<string, string> { { "args0.similar_element_object", similarSelector.ToString() } },
                                false,
                                "IPARecommend");
                                RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                NeedHoldonEvent.Invoke(false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"SingleBtnActionCommandExe:{ex.ToString()}");
                        uIProvider.ShowTips("循环相似元素".I18n("ActionPopupViewModel_CE268AF1"), "选择相似元素目标异常".I18n("ActionPopupViewModel_A7E35445"), UIProviders.Tips.TipLevel.ERROR);
                    }
                    break;
            }
        }
        #region JSON格式化方法
        private string ConvertJsonString(string str)
        {
            //格式化json字符串
            JsonSerializer serializer = new JsonSerializer();
            using (TextReader tr = new StringReader(str))
            {

                using (JsonTextReader jtr = new JsonTextReader(tr))
                {
                    object obj = serializer.Deserialize(jtr);
                    if (obj != null)
                    {
                        using (StringWriter textWriter = new StringWriter())
                        {

                            using (JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                            {
                                Formatting = Formatting.Indented,
                                Indentation = 4,
                                IndentChar = ' '
                            })
                            {
                                serializer.Serialize(jsonWriter, obj);
                                return textWriter.ToString();
                            }
                        }
                    }
                    else
                    {
                        return str;
                    }
                }
            }
        }
        #endregion
        protected Task ResolveAction(AnalyzerContainer anAnalyzerContainer)
        {
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    await Dispatcher.InvokeAsync(new Action(async () =>
                    {
                        var stopwatch = Stopwatch.StartNew();
                        try
                        {
                            logger.LogInformation($"ResolveAction Start");
                            m_ResolvedActions = await anAnalyzerContainer.ResolveAction(true, _ => { ElementClassText = _; }, 12);
                            //ElementClassText = anAnalyzerContainer.ElementClassResult;
                            //CurrentResolvedActions = anAnalyzerContainer.CurrentResolvedActions;
                            CurrentResolvedActionParams = anAnalyzerContainer.CurrentResolvedActionParams;
                            SetActionCollectionView(m_ResolvedActions);
                            ProgressRingVisiblity = Visibility.Collapsed;
                            if (!string.IsNullOrEmpty(ElementClassText))
                            {
                                ResolveOverEvent?.Invoke(ElementClassText);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex);
                            RaiseCloseActionEvent($"CloseWindow:{ex.Message}");
                        }
                        finally
                        {
                            logger.LogInformation($"ResolveAction End-{stopwatch.ElapsedMilliseconds}ms");
                            stopwatch.Stop();
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ResolveAction Error");
                    ResolveOverEvent?.Invoke("error");
                    return;
                }
                finally
                {
                    //SomeUtils.TraceThreadPool();
                }
            }, TaskCreationOptions.LongRunning);

        }
        protected void PreInitActionCollection()
        {
            List<ActionModel> actions = new List<ActionModel>
            {
                new ActionModel
                {
                    ActionName = "Separator",
                    DataItemType = DataItemType.SEPARATOR
                }
            };
            actions.AddRange(ActionDataModels.RecommendActionCategories);
            //SetActionCollection(actions);
        }
        protected void SetActionCollectionView(IEnumerable<ActionModel> anActions)
        {
            Dispatcher.Invoke(() =>
            {
                if (anActions != null)
                {
                    if (RPAEnvironment.GetInstance().CurrentLanguage != LanguageCultureEnum.zh_CN)
                    {
                        anActions = anActions.Where(action => !ActionDataModels.I18NIgnoreLogoList.Contains(action.ActionLogo));
                    }
                    foreach (var action in anActions)
                    {
                        if (action.ActionLogo == "Element_SetValue")
                        {
                            action.ActionDataContext = new InputVarViewModel(action.ActionId, "text", NeedHoldonEvent);
                        }
                        else if (action.ActionLogo == "WebBrowser_Create")
                        {
                            action.ActionDataContext = new InputVarViewModel(action.ActionId, "url", NeedHoldonEvent);
                        }
                        if (action.ActionLogo == "Word_New")
                        {
                            action.ActionDataContext = new InputVarViewModel(action.ActionId, "name", NeedHoldonEvent);
                        }
                        else if (action.ActionLogo == "Excel_NewExcel")
                        {
                            action.ActionDataContext = new InputVarViewModel(action.ActionId, "name", NeedHoldonEvent);
                        }
                        else if (action.ActionLogo == "Element_SetValuePassword_V1")
                        {
                            action.ActionDataContext = new InputVarViewModel(action.ActionId, "text", NeedHoldonEvent);
                        }
                        else if (action.ActionLogo == "Element_SetCheck")
                        {
                            action.ActionDataContext = new InputVarViewModel(action.ActionId, "", NeedHoldonEvent);
                        }
                        else if (action.ActionLogo == "Mouse_DragTo")
                        {
                            action.ActionDataContext = new InputVarViewModel(action.ActionId, "", NeedHoldonEvent);
                        }
                        else if (action.ActionLogo == "File_Rename")
                        {
                            action.ActionDataContext = new InputVarViewModel(action.ActionId, "reName", NeedHoldonEvent);
                        }
                        else if (action.ActionLogo == "logic_control_if_element")
                        {
                            action.ActionDataContext = new InputVarViewModel(action.ActionId, "", NeedHoldonEvent);
                        }
                        else if (action.ActionLogo == "logic_control_if_image")
                        {
                            action.ActionDataContext = new InputVarViewModel(action.ActionId, "", NeedHoldonEvent);
                        }
                        else if (action.ActionLogo == "logic_control_count_for")
                        {
                            action.ActionDataContext = new InputVarViewModel(action.ActionId, "count", NeedHoldonEvent);
                        }
                        else if (action.ActionLogo == "loop_similar_elements")
                        {
                            action.ActionDataContext = new InputVarViewModel(action.ActionId, "", NeedHoldonEvent);
                        }
                        else if (action.ActionLogo == "Element_Region")
                        {
                            action.ActionDataContext = new InputVarViewModel(action.ActionId, "", NeedHoldonEvent);
                        }
                    }
                    ItemsCount = anActions.Count();
                    ActionCollectioView = CollectionViewSource.GetDefaultView(anActions);
                    ActionCollectioView.Filter = ActionCollectioViewFilter;
                    logger.LogInformation($"SetActionCollectionView:{ItemsCount}");
                }
                else
                {
                    logger.LogError("SetActionCollectionView:anActions is null");
                }
            });
        }
        protected void SetActionCollection(IEnumerable<ActionModel> anActions)
        {
            ActionCollectioView = CollectionViewSource.GetDefaultView(null);
            if (anActions != null)
            {
                ActionCollectioView = CollectionViewSource.GetDefaultView(anActions);
                ItemsCount = anActions.Count();
                logger.LogInformation($"SetActionCollection:{string.Join(";", anActions)}");
                //Dispatcher.CurrentDispatcher.Invoke(() =>
                //{
                //    ActionCollectionTask = new NotifyTaskCompletion<ObservableCollection<ActionModel>>(Task.Run(() => new ObservableCollection<ActionModel>(anActions)));
                //});
            }
            else
            {
                //Dispatcher.CurrentDispatcher.Invoke(() =>
                //{
                //    ActionCollectionTask = new NotifyTaskCompletion<ObservableCollection<ActionModel>>(Task.Run(() => new ObservableCollection<ActionModel>()));
                //});

                logger.LogWarning("SetActionCollection:anActions is null");
            }
        }
        protected void UpdateActionCollection(IEnumerable<ActionModel> anActions)
        {
            ItemsCount = anActions.Count();
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                anActions = anActions.Concat(ActionCollectionTask.Result);
                ActionCollectionTask = new NotifyTaskCompletion<ObservableCollection<ActionModel>>(Task.Run(() => new ObservableCollection<ActionModel>(anActions)));
                ProgressRingVisiblity = Visibility.Collapsed;
            });
        }
        private void RecordSelectionChangedCommandExe(ExCommandParameter obj)
        {
            if (obj.Parameter is string record)
            {
                Filter = record;
            }
            //此写法破坏MVVM结构
            if (obj.Sender is ListBox listbox)
            {
                listbox.SelectedIndex = -1;
            }
            if (obj.EventArgs is SelectionChangedEventArgs e)
            {
                e.Handled = true;
            }
        }

        private async void GetInputTextCommandExe(object obj)
        {
            logger.LogDebug("Enter GetInputTextCommandExe");
            object[] objs = obj as object[];
            if (objs != null && objs.Length >= 5)
            {
                logger.LogDebug($"GetInputTextCommandExe: {objs[0]};{objs[1]};{objs[2]}");
                ItemTextBoxMode mode = (ItemTextBoxMode)objs[2];
                ActionModel action = objs[4] as ActionModel;
                if (mode == ItemTextBoxMode.ItemMode)
                {
                    try
                    {
                        string variable = objs[1] as string;
                        m_UiSelector = (await DoSelectorTask).Selector;
                        var actionlogo = action.ActionLogo;
                        var setvaluecomponent = ActionDataModels.Components.FirstOrDefault(c => (!string.IsNullOrEmpty(c.ComponentLogo) && c.ComponentLogo.Equals(actionlogo)));
                        switch (actionlogo)
                        {
                            case "Element_SetValue":
                                if (setvaluecomponent != null)
                                {
                                    GenerateComponentInternalWithVariable(setvaluecomponent.ComponentId,
                                        new Dictionary<string, string> {
                                    { "args0.text", variable },
                                    { "args0.uiSelector", m_UiSelector },
                                    { "args0.isEnter", action.ActionDataContext.IsNeedEnter ? "1":"0"}},
                                        new Dictionary<string, JProperty>(new MyDictionaryComparer()) {
                                    { "args0.text", new JProperty("variableSource",action.ActionDataContext.SelectedItem.Source.GetDescription()) },
                                    { "args0.text", new JProperty("nodeKey",action.ActionDataContext.SelectedItem.NodeKey)  },
                                    { "args0.text", new JProperty("variables",action.ActionDataContext.VariableData)  }},
                                        "IPARecommend");
                                    RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                    RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                }
                                else
                                {
                                    logger.LogError("Can not find Element_SetValue");
                                }
                                break;
                            case "WebBrowser_Create":
                                if (setvaluecomponent != null)
                                {
                                    if (CurrentResolvedActionParams != null && CurrentResolvedActionParams.TryGetValue("args0.type", out string value))
                                    {
                                        GenerateComponentInternalWithVariable(setvaluecomponent.ComponentId,
                                        new Dictionary<string, string> {
                                    { "args0.url", variable },{ "args0.type", value }},
                                        new Dictionary<string, JProperty>(new MyDictionaryComparer()) {
                                    { "args0.url", new JProperty("variableSource",action.ActionDataContext.SelectedItem.Source.GetDescription()) },
                                    { "args0.url", new JProperty("nodeKey",action.ActionDataContext.SelectedItem.NodeKey)  },
                                    { "args0.url", new JProperty("variables",action.ActionDataContext.VariableData)  }},
                                        "IPARecommend");
                                    }
                                    else
                                    {
                                        GenerateComponentInternalWithVariable(setvaluecomponent.ComponentId,
                                        new Dictionary<string, string> {
                                    { "args0.url", variable }},
                                        new Dictionary<string, JProperty>(new MyDictionaryComparer()) {
                                    { "args0.url", new JProperty("variableSource",action.ActionDataContext.SelectedItem.Source.GetDescription()) },
                                    { "args0.url", new JProperty("nodeKey",action.ActionDataContext.SelectedItem.NodeKey)  },
                                    { "args0.url", new JProperty("variables",action.ActionDataContext.VariableData)  }},
                                        "IPARecommend");
                                    }
                                    RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                    RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                }
                                else
                                {
                                    logger.LogError("Can not find WebBrowser_Create");
                                }
                                break;
                            case "Element_SetValuePassword_V1":
                                if (setvaluecomponent != null)
                                {
                                    GenerateComponentInternalWithVariable(setvaluecomponent.ComponentId,
                                        new Dictionary<string, string> {
                                    { "args0.text", variable },
                                    { "args0.uiSelector", m_UiSelector },
                                    { "args0.isEnter", action.ActionDataContext.IsNeedEnter ? "1":"0"}},
                                        new Dictionary<string, JProperty>(new MyDictionaryComparer()) {
                                    { "args0.text", new JProperty("variableSource",action.ActionDataContext.SelectedItem.Source.GetDescription()) },
                                    { "args0.text", new JProperty("nodeKey",action.ActionDataContext.SelectedItem.NodeKey)  },
                                    { "args0.text", new JProperty("variables",action.ActionDataContext.VariableData)  }},
                                        "IPARecommend");
                                    RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                    RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                }
                                else
                                {
                                    logger.LogError("Can not find Element_SetValuePassword_V1");
                                }
                                break;
                            case "Word_New":
                                if (setvaluecomponent != null)
                                {
                                    var baseArgs = new Dictionary<string, string>
                                    {
                                        { "args0.name", variable }
                                    };
                                    if (CurrentResolvedActionParams.TryGetValue("args0.filePath", out string filePath))
                                    {
                                        baseArgs.Add("args0.filePath", filePath);
                                    }
                                    GenerateComponentInternalWithVariable(setvaluecomponent.ComponentId,
                                        baseArgs,
                                        new Dictionary<string, JProperty>(new MyDictionaryComparer()) {
                                    { "args0.name", new JProperty("variableSource",action.ActionDataContext.SelectedItem.Source.GetDescription()) },
                                    { "args0.name", new JProperty("nodeKey",action.ActionDataContext.SelectedItem.NodeKey)  },
                                    { "args0.name", new JProperty("variables",action.ActionDataContext.VariableData)  }},
                                        "IPARecommend");
                                    RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                    RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                }
                                else
                                {
                                    logger.LogError($"Can not find {actionlogo}");
                                }
                                break;
                            case "Excel_NewExcel":
                                if (setvaluecomponent != null)
                                {
                                    var baseArgs = new Dictionary<string, string>
                                    {
                                        { "args0.name", variable }
                                    };
                                    if (CurrentResolvedActionParams.TryGetValue("args0.path", out string filePath))
                                    {
                                        baseArgs.Add("args0.path", filePath);
                                    }

                                    GenerateComponentInternalWithVariable(setvaluecomponent.ComponentId,
                                        baseArgs,
                                        new Dictionary<string, JProperty>(new MyDictionaryComparer()) {
                                    { "args0.name", new JProperty("variableSource",action.ActionDataContext.SelectedItem.Source.GetDescription()) },
                                    { "args0.name", new JProperty("nodeKey",action.ActionDataContext.SelectedItem.NodeKey)  },
                                    { "args0.name", new JProperty("variables",action.ActionDataContext.VariableData)  }},
                                        "IPARecommend");
                                    RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                    RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                }
                                else
                                {
                                    logger.LogError($"Can not find {actionlogo}");
                                }
                                break;
                            case "File_Rename":
                                if (setvaluecomponent != null)
                                {
                                    if (CurrentResolvedActionParams.TryGetValue("args0.file_path", out string filePath))
                                    {
                                        GenerateComponentInternalWithVariable(setvaluecomponent.ComponentId,
                                        new Dictionary<string, string> {
                                        { "args0.name", variable },
                                        { "args0.file_path", $"{filePath}" }
                                        },
                                        new Dictionary<string, JProperty>(new MyDictionaryComparer()) {
                                        { "args0.name", new JProperty("variableSource",action.ActionDataContext.SelectedItem.Source.GetDescription()) },
                                        { "args0.name", new JProperty("nodeKey",action.ActionDataContext.SelectedItem.NodeKey)  },
                                        { "args0.name", new JProperty("variables",action.ActionDataContext.VariableData)  }},
                                        "IPARecommend");

                                        RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                        RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                    }
                                }
                                break;
                            case "logic_control_count_for":
                                if (setvaluecomponent != null)
                                {
                                    GenerateComponentInternalWithVariable(setvaluecomponent.ComponentId,
                                      new Dictionary<string, string> {
                                    { "args0.count", variable }},
                                      new Dictionary<string, JProperty>(new MyDictionaryComparer()) {
                                    { "args0.count", new JProperty("variableSource",action.ActionDataContext.SelectedItem.Source.GetDescription()) },
                                    { "args0.count", new JProperty("nodeKey",action.ActionDataContext.SelectedItem.NodeKey)  },
                                    { "args0.count", new JProperty("variables",action.ActionDataContext.VariableData)  }},
                                      "IPARecommend");

                                    RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                    RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                }
                                break;
                        }

                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "ItemMode");
                    }
                }
                else
                {
                    string inputText = objs[0] as string;
                    if (!string.IsNullOrEmpty(inputText))
                    {
                        m_UiSelector = (await DoSelectorTask).Selector;
                        string input = inputText.Replace(Environment.NewLine, "");
                        var actionlogo = action.ActionLogo;
                        var setvaluecomponent = ActionDataModels.Components.FirstOrDefault(c => (!string.IsNullOrEmpty(c.ComponentLogo) && c.ComponentLogo.Equals(actionlogo)));
                        switch (actionlogo)
                        {
                            case "Element_SetValue":
                                if (setvaluecomponent != null)
                                {
                                    GenerateComponentInternal(setvaluecomponent.ComponentId,
                                        new Dictionary<string, string> {
                                        { "args0.text", $"\"{input}\"" },
                                        { "args0.uiSelector", m_UiSelector },
                                        { "args0.isEnter", action.ActionDataContext.IsNeedEnter ? "1":"0"}},
                                        false,
                                        "IPARecommend");
                                    RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                }
                                else
                                {
                                    logger.LogError("Can not find Element_SetValue");
                                }
                                try
                                {
                                    RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                    Thread.Sleep(20);
                                    if (!string.IsNullOrEmpty(input))
                                    {
                                        var setTextParam = new JObject
                                        {
                                            { "selector",m_UiSelector },
                                            { "txt",input },
                                            { "isReplace",0 },
                                            { "isSimulate",1 },
                                            { "isEnter",action.ActionDataContext.IsNeedEnter ? 1:0 },
                                            { "delaybeforEnter",0 },
                                            { "isVerify",0 },
                                            { "Trigger",1 },
                                            { "browserType",null},
                                            { "elementPosition",1},
                                            { "offsetX",5},
                                            { "offsetY",5},
                                        };
                                        await m_PluginInvoker.InvokeAsync(
                                            new PluginRoute("Element", "SetText"),
                                            setTextParam.ToString());
                                        logger.LogDebug("Input over!");
                                    }
                                    Task.Factory.StartNew(async () =>
                                    {
                                        await Task.Delay(200);
                                        Dispatcher.Invoke(new Action(() =>
                                        {
                                            logger.LogDebug("Send start!");
                                            FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.LCONTROL);
                                            logger.LogDebug("Send over!");
                                        }));
                                    }, TaskCreationOptions.LongRunning).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { }); ;
                                }
                                catch (Exception ex)
                                {
                                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Fail to auto run set text!");
                                }
                                break;
                            case "WebBrowser_Create":
                                if (setvaluecomponent != null)
                                {
                                    GenerateComponentInternal(setvaluecomponent.ComponentId,
                                        new Dictionary<string, string> {
                                        { "args0.url", $"\"{input}\"" } },
                                        true,
                                        "IPARecommend");
                                    await OpenWebPageAsync(input);

                                    RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                    RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                }
                                else
                                {
                                    logger.LogError("Can not find Element_SetValue");
                                }
                                break;
                            case "Element_SetValuePassword_V1":
                                if (setvaluecomponent != null)
                                {
                                    GenerateComponentInternal(setvaluecomponent.ComponentId,
                                        new Dictionary<string, string> {
                                    { "args0.text", $"\"{input}\"" },
                                    { "args0.uiSelector", m_UiSelector },
                                    { "args0.isEnter", action.ActionDataContext.IsNeedEnter ? "1":"0"}},
                                        false,
                                        "IPARecommend");
                                    RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                    RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                    Thread.Sleep(20);
                                    if (!string.IsNullOrEmpty(input))
                                    {
                                        var setTextParam = new JObject
                                        {
                                            { "selector",m_UiSelector },
                                            { "txt",input },
                                            { "isReplace",0 },
                                            { "isSimulate",1 },
                                            { "isEnter",action.ActionDataContext.IsNeedEnter ? 1:0 },
                                            { "delaybeforEnter",0 },
                                            { "isVerify",0 },
                                            { "Trigger",1 },
                                            { "browserType",null},
                                            { "elementPosition",1},
                                            { "offsetX",5},
                                            { "offsetY",5},
                                        };
                                        await m_PluginInvoker.InvokeAsync(
                                            new PluginRoute("Element", "SetText"),
                                            setTextParam.ToString());
                                        logger.LogDebug("Input over!");
                                    }
                                    Task.Factory.StartNew(async () =>
                                    {
                                        await Task.Delay(200);
                                        Dispatcher.Invoke(new Action(() =>
                                        {
                                            logger.LogDebug("Send start!");
                                            FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.LCONTROL);
                                            logger.LogDebug("Send over!");
                                        }));
                                    }, TaskCreationOptions.LongRunning).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { }); ;
                                }
                                else
                                {
                                    logger.LogError("Can not find Element_SetValuePassword_V1");
                                }
                                break;
                            case "Word_New":
                                if (setvaluecomponent != null)
                                {
                                    if (CurrentResolvedActionParams.TryGetValue("args0.filePath", out string filePath))
                                    {
                                        string directory = filePath.Replace(@"\\", @"\").Replace("\"", "");
                                        string filename = input.Replace("\"", "");
                                        logger.LogDebug($"{directory}{filename}.docx");

                                        string targetFile = $"{Path.Combine(directory, $"{filename}.docx")}";
                                        int i = 0;
                                        string newfileName = filename;
                                        while (File.Exists(targetFile))
                                        {
                                            if (i >= 1)
                                            {
                                                newfileName = Localize($"{filename}-副本{i}", "ActionPopupViewModel_97CBF64B");
                                            }
                                            else
                                            {
                                                newfileName = Localize($"{filename}-副本", "ActionPopupViewModel_12BE5F8A");
                                            }
                                            targetFile = Path.Combine(directory, $"{newfileName}.docx");
                                            i++;
                                        }
                                        GenerateComponentInternal(setvaluecomponent.ComponentId,
                                        new Dictionary<string, string> {
                                        { "args0.name", $"\"{newfileName}.docx\"" },
                                        { "args0.filePath", $"{filePath}" }},
                                        false,
                                        "IPARecommend");
                                        RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                        RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                        Thread.Sleep(20);

                                        using (File.Create(targetFile))
                                        {

                                        }
                                    }
                                }
                                break;
                            case "Excel_NewExcel":
                                if (setvaluecomponent != null)
                                {
                                    if (CurrentResolvedActionParams.TryGetValue("args0.path", out string filePath))
                                    {
                                        string directory = filePath.Replace(@"\\", @"\").Replace("\"", "");
                                        string filename = input.Replace("\"", "");
                                        logger.LogDebug($"{directory}{filename}.xlsx");

                                        string targetFile = Path.Combine(directory, $"{filename}.xlsx");
                                        int i = 0;
                                        string newfileName = filename;
                                        while (File.Exists(targetFile))
                                        {
                                            if (i >= 1)
                                            {
                                                newfileName = Localize($"{filename}-副本{i}", "ActionPopupViewModel_97CBF64B");
                                            }
                                            else
                                            {
                                                newfileName = Localize($"{filename}-副本", "ActionPopupViewModel_12BE5F8A");
                                            }
                                            targetFile = Path.Combine(directory, $"{newfileName}.xlsx");
                                            i++;
                                        }
                                        GenerateComponentInternal(setvaluecomponent.ComponentId,
                                        new Dictionary<string, string> {
                                        { "args0.name", $"\"{newfileName}.xlsx\"" },
                                        { "args0.path", $"{filePath}" }},
                                        false,
                                        "IPARecommend");
                                        RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                        RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                        Thread.Sleep(20);


                                        using (File.Create(targetFile))
                                        {

                                        }
                                    }
                                }
                                break;

                            case "File_Rename":
                                if (setvaluecomponent != null)
                                {
                                    if (CurrentResolvedActionParams.TryGetValue("args0.file_path", out string filePath))
                                    {
                                        logger.LogDebug($"需重命名的文件路径：{filePath}");
                                        string reName = input.Replace("\"", "");

                                        GenerateComponentInternal(setvaluecomponent.ComponentId,
                                        new Dictionary<string, string> {
                                        { "args0.name", $"\"{reName}\"" },
                                        { "args0.file_path", $"{filePath}" }
                                    },
                                        false,
                                        "IPARecommend");
                                        RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                        RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                    }
                                }
                                break;
                            case "logic_control_count_for":
                                if (setvaluecomponent != null && !string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(input.Trim()))
                                {
                                    GenerateComponentInternal(setvaluecomponent.ComponentId,
                                      new Dictionary<string, string> {
                                    { "args0.count", input }},
                                      false,
                                      "IPARecommend");

                                    RunLearnOnlineTask(setvaluecomponent.ComponentId);
                                    RaiseCloseActionEvent(setvaluecomponent.ComponentLogo);
                                }
                                break;
                        }
                    }
                }
                return;
            }
            else
            { }
        }
        private void MainActionCommandExe(object obj)
        {
            if (obj is string actionparam)
            {
                GenerateComponent(actionparam, "IPARecommend");
                RunLearnOnlineTask(actionparam);
                RaiseCloseActionEvent("MainActionCommandExe");
            }
        }
        protected void GenerateComponentInternalWithVariable(string aComponentId,
            Dictionary<string, string> aComponentInputs,
            Dictionary<string, JProperty> aComponentInputParams,
            string aComponentSource)
        {
            m_ActionDoneCallBack?.Invoke(ActionParam);
            Task.Factory.StartNew(async () =>
            {
                logger.LogInformation($"CurrentDpi is {CurrentDpi}");
                ComponentModel component = ActionDataModels.Components.FirstOrDefault(c => c.ComponentId == aComponentId);
                JArray actions = new JArray();
                logger.LogInformation($"generate:{aComponentId}");
                {
                    var actionJO = new JObject
                    {
                        { "type", aComponentId }
                    };
                    var propertyJO = new JObject();
                    //预设参数
                    if (aComponentInputs != null && aComponentInputs.Count > 0)
                    {
                        aComponentInputs.ForEach(ci =>
                        {
                            JObject valueJo = new JObject
                            {
                               { "value", ci.Value }
                            };
                            foreach (var varitem in aComponentInputParams)
                            {
                                if (varitem.Key == ci.Key)
                                {
                                    valueJo.Add(varitem.Value.Name, varitem.Value.Value);
                                }
                            }
                            propertyJO.Add(ci.Key, valueJo);
                        });
                    }
                    actionJO.Add("properties", propertyJO);
                    var optionJO = new JObject
                    {
                        { "addFoldComponent", false },
                        { "sourceTracker", aComponentSource }
                    };
                    actionJO.Add("options", optionJO);
                    actions = new JArray { actionJO };
                }
                ExecuteAction(aComponentId, ActionParam.CurrentGuiNode, m_UiSelector)
                .ConfigureAwait(false)
                .GetAwaiter()
                .OnCompleted(() => { });
                if (MessageService.PostMessage(TerminalType.CoreServer,
                    "flowedit.plugingenerate",
                    actions, (ret) =>
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
                await LearnOnLine(component.ComponentLogo);
                CurrentActions.Add(component.ComponentLogo);
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false)
                .GetAwaiter()
                .OnCompleted(() => { });
        }
        protected void GenerateComponentInternal(string aComponentId,
            Dictionary<string, string> aComponentInputs,
            bool anNeedCheckAllInput,
            string aComponentSource)
        {
            m_ActionDoneCallBack?.Invoke(ActionParam);
            Task.Factory.StartNew(async () =>
            {
                logger.LogInformation($"CurrentDpi is {CurrentDpi}");
                ComponentModel component = ActionDataModels.Components.FirstOrDefault(c => c.ComponentId == aComponentId);
                JArray actions = new JArray();
                logger.LogInformation($"generate:{aComponentId}");
                {
                    var actionJO = new JObject
                    {
                        { "type", aComponentId }
                    };
                    var propertyJO = new JObject();
                    //预设参数
                    if (aComponentInputs != null && aComponentInputs.Count > 0)
                    {
                        aComponentInputs.ForEach(ci =>
                        {
                            JObject valueJo = new JObject
                            {
                               { "value", ci.Value }
                            };
                            propertyJO.Add(ci.Key, valueJo);
                        });
                    }
                    //全量参数遍历
                    if (anNeedCheckAllInput
            || aComponentInputs == null
            || aComponentInputs.Count == 0)
                    {
                        JArray args0 = component.Definition.Args0;
                        if (args0 != null && args0.Count > 0)
                        {
                            foreach (var arg in args0)
                            {
                                var input = arg["name"]?.ToString();
                                JObject valueJo = new JObject();
                                input = $"args0.{input}";
                                switch (input)
                                {
                                    case "args0.uiSelector":
                                    case "args0.similar_element_object":
                                    case "args0.verifyPicUiSelector":
                                    case "args0.backgroundUiSelector":
                                        m_UiSelector = (await DoSelectorTask).Selector;
                                        valueJo.Add("value", m_UiSelector);
                                        break;
                                    case "args0.condition":
                                        if (aComponentId.StartsWith("logic_control_if_image"))
                                        {
                                            var picturePathWithOutHighLight = Path.Combine("res", Path.GetFileName(ActionParam.PicturePath_WithOutHighLight));
                                            valueJo.Add("value", JsonConvert.ToString(picturePathWithOutHighLight));
                                        }
                                        else
                                        {
                                            m_UiSelector = (await DoSelectorTask).Selector;
                                            valueJo.Add("value", m_UiSelector);
                                        }
                                        break;
                                    case "args0.location":
                                    case "args0.End":
                                        valueJo.Add("value", $"[{(int)(ActionParam.MouseLocation.X / CurrentDpi.X)},{(int)(ActionParam.MouseLocation.Y / CurrentDpi.Y)}]");
                                        break;
                                    case "args0.image_path":
                                    case "args0.imageBackPath":
                                        var _picturePath = Path.Combine("res", Path.GetFileName(ActionParam.PicturePath));
                                        valueJo.Add("value", JsonConvert.ToString(_picturePath));
                                        break;
                                    case "args0.image":
                                        var _picturePathWithOutHighLight = Path.Combine("res", Path.GetFileName(ActionParam.PicturePath_WithOutHighLight));
                                        valueJo.Add("value", JsonConvert.ToString(_picturePathWithOutHighLight));
                                        break;
                                    case "args0.executeable":
                                        if (CurrentResolvedActionParams != null && CurrentResolvedActionParams.ContainsKey("args0.executeable"))
                                        {
                                            valueJo.Add("value", CurrentResolvedActionParams["args0.executeable"]);
                                        }
                                        else
                                        {
                                            logger.LogWarning("args0.executeable is null.");
                                        }
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
                                    case "args0.click_time":
                                        if (CurrentResolvedActionParams != null && CurrentResolvedActionParams.TryGetValue(input, out string clicktime))
                                        {
                                            valueJo.Add("value", clicktime);
                                        }
                                        else
                                        {
                                            if (ActionParam.IsExplorer)
                                            {
                                                valueJo.Add("value", "\"double\"");
                                            }
                                        }
                                        break;
                                    case "args0.clickfunc":
                                        if (CurrentResolvedActionParams != null && CurrentResolvedActionParams.TryGetValue(input, out string clickfunc))
                                        {
                                            valueJo.Add("value", clickfunc);
                                        }
                                        else
                                        {
                                            if (ActionParam.IsExplorer)
                                            {
                                                valueJo.Add("value", "1");
                                            }
                                        }
                                        break;
                                    default:
                                        if (CurrentResolvedActionParams != null && CurrentResolvedActionParams.TryGetValue(input, out string value))
                                        {
                                            valueJo.Add("value", value);
                                        }
                                        else
                                        {
                                            logger.LogError($"There is an invalid arg:{input}");
                                        }
                                        break;

                                }
                                if (valueJo.HasValues)
                                {
                                    if (!propertyJO.ContainsKey(input))
                                    {
                                        propertyJO.Add(input, valueJo);
                                    }
                                }
                            }
                        }
                    }
                    actionJO.Add("properties", propertyJO);
                    var optionJO = new JObject
                    {
                        { "addFoldComponent", false },
                        { "sourceTracker", aComponentSource }
                    };
                    actionJO.Add("options", optionJO);
                    actions = new JArray { actionJO };
                }
                ExecuteAction(aComponentId, ActionParam.CurrentGuiNode, m_UiSelector)
                .ConfigureAwait(false)
                .GetAwaiter()
                .OnCompleted(() => { });
                if (MessageService.PostMessage(TerminalType.CoreServer,
                    "flowedit.plugingenerate",
                    actions, (ret) =>
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
                ///搜索的情况下不调用在线学习
                if (aComponentSource != "IPARecSearch")
                {
                    await LearnOnLine(component.ComponentLogo);
                }
                CurrentActions.Add(component.ComponentLogo);
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false)
                .GetAwaiter()
                .OnCompleted(() => { });
        }
        protected override void GenerateComponent(string aComponentId, string aComponentSource)
        {
            if (aComponentId == LocalDataModels.Login_Comp_Type)
            {
                Task.Factory.StartNew(async () =>
                {
                    string backgroudPath = ActionParam.PictureRelativePath;
                    string loginNameSelector = "";
                    string loginPasswordSelector = "";
                    string loginButtonSelector = "";
                    if (string.IsNullOrEmpty(m_UiSelector))
                    {
                        m_UiSelector = (await DoSelectorTask).Selector;
                    }
                    m_UiSelectorJO = JObject.Parse(m_UiSelector);
                    var nodes = m_UiSelectorJO["selector"] as JArray;
                    if (nodes != null && nodes.First() != null)
                    {
                        var urlnode = nodes.First()["props"].FirstOrDefault(p => p["name"] != null && p["name"].ToString() == "url");
                        if (urlnode != null)
                        {
                            var url = urlnode["value"].ToString();
                            if (url.Contains("taobao"))
                            {
                                if (LocalDataModels.LoginDatas.TryGetValue("taobao", out LoginDataModel value))
                                {
                                    loginNameSelector = value.Selectors.UserName.ToString();
                                    loginPasswordSelector = value.Selectors.Password.ToString();
                                    loginButtonSelector = value.Selectors.LoginButton.ToString();
                                }
                            }
                        }
                    }
                    PackageLoginComponentData(backgroudPath, loginNameSelector, loginPasswordSelector, loginButtonSelector);
                }, TaskCreationOptions.LongRunning).ConfigureAwait(false)
                .GetAwaiter()
                .OnCompleted(() => { });
            }
            else if (aComponentId == "excel.record")
            {
                if (MessageService.PostMessage(TerminalType.CoreServer,
                    "flowedit.excelrecord",
                    new JObject(), (ret) =>
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
                JObject excelMessage = new JObject
                {
                    { "action", "StartExcelRecord" },
                    { "args", new JArray() }
                };
                if (MessageService.PostMessage(TerminalType.CoreServer,
                    "visualdata.excel",
                    excelMessage, (ret) =>
                    {
                        logger.LogDebug($"post to excel return : {ret}");
                    }))
                {
                    logger.LogDebug($"post to excel done");
                }
                else
                {
                    logger.LogError($"post to excel fail");
                }
            }
            else
            {
                GenerateComponentInternal(aComponentId, null, true, aComponentSource);
            }
        }
        private bool ActionCollectioViewFilter(object item)
        {
            if (!(item is ActionModel action))
            {
                return false;
            }
            else
            {
                if (action.IsDiss)
                {
                    return false;
                }
                else
                {
                    //if (ActionCollectioView.)
                    //{
                    //    return false;
                    //}
                }
                return true;
            }
        }
        private bool ContainsFilter(object item)
        {
            if (!(item is ComponentModel product))
            {
                return false;
            }

            if (string.IsNullOrEmpty(this.Filter))
            {
                return false;
            }

            if (product.SearchTags.FirstOrDefault(t => t.Contains(Filter)) != null)
            {
                return true;
            }

            return false;
        }
        private async Task LearnOnLine(string aComponentId)
        {
            try
            {
                var ret = await m_ZbrainService.LearnOnline(aComponentId);
                logger.LogDebug($"LearnOnLine ret: {ret}");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "LearnOnLine Error");
            }
        }
        private void PackageLoginComponentData(
            string aBackgroudPath,
            string aLoginNameSelector,
            string aLoginPasswordSelector,
            string aLoginButtonSelector,
            string aLoginVerifyBackgroundSelector = null,
            string aLoginVerifyThumbSelector = null)
        {
            string code = "10";
            //var cvData = await GetLoginComponentCVData();
            //var usernameJO = cvData["用户名"];
            //var passwordJO = cvData["密码"];
            //var loginbuttonJO = cvData["登录"];
            //var verifyCodeJO = cvData["验证码"];
            //if (usernameJO == null 
            //    || !usernameJO.HasValues
            //    || passwordJO == null
            //    || !passwordJO.HasValues
            //    || loginbuttonJO == null
            //    || !loginbuttonJO.HasValues)
            //{
            //    code = "10";
            //}
            JObject loginComponentData = new JObject
            {
                { "type", LocalDataModels.Login_Comp_Type },
                { "background", aBackgroudPath },
            };
            #region 
            //JObject loginComponentData = new JObject
            //{
            //    { "type", "TASK_COMPONENT_UoSljz31662033481296" },
            //    { "background", aBackgroudPath },

            //     { "用户名",
            //        new JObject
            //        {
            //            { "position", ConvertPositionData(usernameJO)},
            //            { "selector", aLoginNameSelector}
            //        }
            //    },
            //    { "密码",
            //        new JObject
            //        {
            //            { "position", ConvertPositionData(passwordJO)},
            //            { "selector", aLoginPasswordSelector}
            //        }
            //    },
            //    { "登录",
            //        new JObject
            //        {
            //            { "position", ConvertPositionData(loginbuttonJO)},
            //            { "selector", aLoginButtonSelector}
            //        }
            //    },
            //    { "验证码",
            //        new JObject
            //        {
            //            { "position", ConvertPositionData(verifyCodeJO) },
            //            { "selector", ""}
            //        }
            //    },
            //};
            #endregion

            JObject usernameProp = new JObject();
            JObject passwordProp = new JObject();
            JObject loginbuttonProp = new JObject();
            JObject verifycodeProp = new JObject();

            usernameProp.Add("position", new JObject());
            usernameProp.Add("selector", aLoginNameSelector);
            usernameProp.Add("elementshot", JsonConvert.ToString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LoginComponent\\ElementImages\\username.png")));

            passwordProp.Add("position", new JObject());
            passwordProp.Add("selector", aLoginPasswordSelector);
            passwordProp.Add("elementshot", JsonConvert.ToString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LoginComponent\\ElementImages\\password.png")));

            loginbuttonProp.Add("position", new JObject());
            loginbuttonProp.Add("selector", aLoginButtonSelector);
            loginbuttonProp.Add("elementshot", JsonConvert.ToString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LoginComponent\\ElementImages\\loginbutton.png")));

            verifycodeProp.Add("position", new JObject());
            verifycodeProp.Add("selector", new JObject());
            verifycodeProp.Add("elementshot", "");

            loginComponentData.Add("用户名", usernameProp);
            loginComponentData.Add("密码", passwordProp);
            loginComponentData.Add("登录", loginbuttonProp);
            loginComponentData.Add("验证码", verifycodeProp);

            loginComponentData.Add("code", code);

            if (MessageService.PostMessage(TerminalType.CoreServer,
                    "flowedit.sendbizcompdata",
                    loginComponentData, (ret) =>
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
        }
        //private Task<JObject> GetLoginComponentCVData()
        //{
        //    return Task.Run(async () => 
        //    {
        //        try
        //        {
        //            Dictionary<string, object> requestObject = new Dictionary<string, object>
        //            {
        //                {"image",ActionParam.ElementShotBase64},
        //                ///...
        //            };
        //            logger.LogDebug($"Start GetLoginComponentCVData");
        //            var responseString = await m_ZbrainService.GetLoginComponentData(requestObject.ToJson());
        //            var result = JObject.Parse(responseString).GetValue<JObject>("result");
        //            logger.LogDebug($"End GetLoginComponentCVData:{responseString}");
        //            return result;
        //        }
        //        catch (Exception ex)
        //        {
        //            logger.LogError(ex);
        //            return null;
        //        }
        //    });
        //}


        private async Task ExecuteAction(string anActionId, IGuiNode aGuiNode, string aSelectorString)
        {
            await Task.Factory.StartNew(() =>
            {
                logger.LogInformation($"{anActionId} is being executed");
                try
                {
                    if (anActionId.StartsWith("Element_Click") || anActionId.StartsWith("Mouse_ClickCombine"))
                    {
                        if (ActionParam.ProcessName.ToLower().StartsWith("explorer"))
                        {
                            if (!string.IsNullOrEmpty(aSelectorString) && !SomeUtils.IsWindowsContextMenu(JObject.Parse(aSelectorString)))
                            {
                                MouseDoubleClick(aGuiNode);
                                return;
                            }
                            else
                            {
                                logger.LogInformation($"ContextMenu Ignore Auto Run.");
                                return;
                            }
                        }
                        MouseClick(aGuiNode);
                    }
                    //else if (anActionId.StartsWith("WebBrowser_Create"))
                    //{
                    //    OpenWebPage();
                    //}
                    else if (anActionId.StartsWith("App_Run"))
                    {
                        if (CurrentResolvedActionParams != null && CurrentResolvedActionParams.TryGetValue("args0.executeable", out string anAppPath))
                        {
                            AppRun(anAppPath.Trim('\"'));
                        }

                    }
                    else if (anActionId.StartsWith("Excel_Cs_OpenV3"))
                    {
                        //File.Create();
                        if (CurrentResolvedActionParams.TryGetValue("args0.file", out string filePath))
                        {
                            Process.Start(filePath);
                        }
                    }
                    else if (anActionId.StartsWith("Word_Open"))
                    {
                        if (CurrentResolvedActionParams.TryGetValue("args0.filePath", out string filePath))
                        {
                            Process.Start(filePath);
                        }
                    }
                    else if (anActionId.StartsWith("File_OpenFile"))
                    {
                        if (CurrentResolvedActionParams.TryGetValue("args0.path", out string filePath))
                        {
                            Process.Start(filePath);
                        }
                    }
                    //else if (anActionId.StartsWith("Word_New"))
                    //{
                    //    //File.Create();
                    //    if (CurrentResolvedActionParams.TryGetValue("args0.filePath", out string filePath))
                    //    {
                    //        if (CurrentResolvedActionParams.TryGetValue("args0.name", out string name))
                    //        {
                    //            logger.LogDebug($"{filePath.Replace(@"\\", @"\").Replace("\"", "")}{name.Replace("\"", "")}.docx");
                    //            using (File.Create($"{Path.Combine(filePath.Replace(@"\\", @"\").Replace("\"", ""), $"{name.Replace("\"", "")}.docx")}"))
                    //            {

                    //            }
                    //        }
                    //    }
                    //}
                    //else if (anActionId.StartsWith("Excel_NewExcel"))
                    //{
                    //    if (CurrentResolvedActionParams.TryGetValue("args0.path", out string filePath))
                    //    {
                    //        if (CurrentResolvedActionParams.TryGetValue("args0.name", out string name))
                    //        {
                    //            logger.LogDebug($"{filePath.Replace(@"\\", @"\").Replace("\"", "")}{name.Replace("\"", "")}.xlsx");
                    //            using (File.Create($"{Path.Combine(filePath.Replace(@"\\", @"\").Replace("\"", ""), $"{name.Replace("\"", "")}.xlsx")}"))
                    //            {

                    //            }
                    //        }
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }, TaskCreationOptions.LongRunning);

        }
        private void MouseClick(IGuiNode aGuiNode)
        {
            FlaUI.Core.Input.Mouse.MovePixelsPerMillisecond = 0.5;
            if (ActionParam.GuiType == eGuiType.Windows)
            {
                aGuiNode.MouseClick(0, 0, new UiElementClickParams
                {
                    moveMouse = false,
                    cursorPos = 0,
                    operateType = 1
                });
            }
            else if (ActionParam.GuiType == eGuiType.IE)
            {
                Dispatcher.Invoke(() =>
                {
                    aGuiNode.MouseClick(0, 0, new UiElementClickParams
                    {
                        moveMouse = false,
                        cursorPos = 0,
                        operateType = 0
                    });
                });
            }
            else
            {
                aGuiNode.MouseClick(0, 0, new UiElementClickParams
                {
                    moveMouse = false,
                    cursorPos = 0,
                    operateType = 0
                });
            }
        }
        private void MouseDoubleClick(IGuiNode aGuiNode)
        {
            FlaUI.Core.Input.Mouse.MovePixelsPerMillisecond = 0.5;
            aGuiNode.MouseDoubleClick(0, 0, new UiElementClickParams
            {
                moveMouse = false,
                cursorPos = 0,
                operateType = 1
            });
        }
        private async Task OpenWebPageAsync(string anUrl = null)
        {
            if (CurrentResolvedActionParams != null && CurrentResolvedActionParams.TryGetValue("args0.type", out string aType))
            {
                aType = JToken.Parse(aType).ToObject<string>();
            }
            else
            {
                aType = "chrome";
            }
            if (string.IsNullOrEmpty(anUrl))
            {
                if (CurrentResolvedActionParams != null && CurrentResolvedActionParams.TryGetValue("args0.url", out anUrl))
                {
                    anUrl = JToken.Parse(anUrl).ToObject<string>();
                }
                else
                {
                    anUrl = "https://www.ai-indeed.com/";
                }
            }
            string param = JsonConvert.SerializeObject(new JObject
            {
                {"isRemote","false" },
                {"address","" },
                {"url",anUrl },
                {"browserType",aType },
                {"timeout","10000" },
            });
            logger.LogInformation(param);
            await m_PluginInvoker.InvokeAsync(
                new PluginRoute("WebBrowser", "Create"),
                param);
        }
        private void AppRun(string anAppPath)
        {
            if (File.Exists(anAppPath))
            {
                Process.Start(anAppPath);
            }
            else
            {
                logger.LogError($"Invalid path:{anAppPath}");
            }
        }
        private bool isOverwriteFile;
        public bool IsOverwriteFile
        {
            get { return isOverwriteFile; }
            set
            {
                isOverwriteFile = value;
                RaisePropertyChanged("IsOverwriteFile");
            }
        }

        public void RaiseNeedHoldEvent(bool isHold)
        {
            NeedHoldonEvent?.Invoke(isHold);
        }
    }
}

