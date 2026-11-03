using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using II.RPA.Plugin.GUI.ProcessCreator.Controls;
using II.RPA.Plugin.GUI.ProcessCreator.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace II.RPA.Plugin.GUI.ProcessCreator.ActionProvider
{
    public class InputVarViewModel:ModelBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        private string m_VariableNodeData = string.Empty;
        private event Action<bool> m_NeedHoldonEvent;
        public InputVarViewModel(string aComponentId,string aParamName, Action<bool> aNeedHoldOnEvent)
        {
            m_NeedHoldonEvent = aNeedHoldOnEvent;
            HighlightContent.ToHighlight = "";
            TextChangedCommand = new DelegateCommand(TextChangedCommandExe);
            InputCancelCommand = new DelegateCommand(InputCancelCommandExe);
            ListBoxItemMouseDownCommand = new DelegateCommand(ListBoxItemMouseDownCommandExe);
            MouseDownActionCommand = new DelegateCommand(MouseDownActionCommandExe);
            if (!string.IsNullOrEmpty(aParamName))
            {
                LoadVariablData(aComponentId, aParamName);
            }
        }

        private ObservableCollection<RootModel> rootCollection;
        public ObservableCollection<RootModel> RootCollection
        {
            get
            {
                return rootCollection;
            }
            set
            {
                rootCollection = value;
                RaisePropertyChanged("RootCollection");
            }
        }
        private string searchString;
        public string SearchString
        {
            get { return searchString; }
            set
            {
                searchString = value;
                RaisePropertyChanged("SearchString");
            }
        }
        private string resultString;
        public string ResultString
        {
            get { return resultString; }
            set
            {
                resultString = value;
                RaisePropertyChanged("ResultString");
            }
        }
        private bool isOpenVarPopup;
        public bool IsOpenVarPopup
        {
            get { return isOpenVarPopup; }
            set
            {
                isOpenVarPopup = value;
                RaisePropertyChanged("IsOpenVarPopup");
                Task.Factory.StartNew(
                () =>
                {
                    if (!isOpenVarPopup)
                    {
                        new ValueTask(Task.Delay(600)).GetAwaiter().GetResult();
                    }
                    else
                    {
                        new ValueTask(Task.Delay(10)).GetAwaiter().GetResult();
                    }
                    Dispatcher.CurrentDispatcher.Invoke(() => { IsHoldOnInputPopup = isOpenVarPopup; });
                },
            TaskCreationOptions.LongRunning).ConfigureAwait(false)
            .GetAwaiter()
            .OnCompleted(() => { });
            }
        }
        private bool isNeedEnter;
        public bool IsNeedEnter
        {
            get { return isNeedEnter; }
            set
            {
                isNeedEnter = value;
                RaisePropertyChanged("IsNeedEnter");
            }
        }
        private BaseVariableModel selectedVarItem;
        public BaseVariableModel SelectedVarItem
        {
            get { return selectedVarItem; }
            set
            {
                logger.LogDebug($"Enter SelectedVarItem.Set");
                if (selectedVarItem != null)
                {
                    selectedVarItem.IsSelected = false;
                }
                if (SelectedSubVarItem != null)
                {
                    SelectedSubVarItem.IsSelected = false;
                }
                selectedVarItem = value;
                RaisePropertyChanged("SelectedVarItem");
                if (selectedVarItem != null && selectedVarItem.IsEnabled)
                {
                    ResultString = selectedVarItem?.Title;
                    IsOpenVarPopup = false;
                    logger.LogDebug($"SelectedVarItem.Set:{ResultString}");
                    SelectedItem = selectedVarItem;
                    SelectedVarItem.IsSelected = false;
                }
                else
                {
                    selectedVarItem.IsSelected = false;
                    //ResultString = $"{selectedItem?.Title}({selectedItem?.IsEnabled})";
                }
            }
        }
        private BaseVariableModel selectedSubVarItem;
        public BaseVariableModel SelectedSubVarItem
        {
            get { return selectedSubVarItem; }
            set
            {
                logger.LogDebug($"Enter SelectedSubVarItem.Set");
                if (selectedSubVarItem != null)
                {
                    selectedSubVarItem.IsSelected = false;
                }
                if (SelectedVarItem != null)
                {
                    SelectedVarItem.IsSelected = false;
                }
                selectedSubVarItem = value;
                RaisePropertyChanged("SelectedSubVarItem");
                if (selectedSubVarItem != null && selectedSubVarItem.IsEnabled)
                {
                    ResultString = selectedSubVarItem?.Title;
                    IsOpenVarPopup = false;
                    logger.LogDebug($"SelectedSubVarItem.Set:{ResultString}");
                    SelectedItem = selectedSubVarItem;
                    SelectedSubVarItem.IsSelected = false;
                }
                else
                {
                    selectedSubVarItem.IsSelected = false;
                    //ResultString = $"{selectedItem?.Title}({selectedItem?.IsEnabled})";
                }
            }
        }
        public BaseVariableModel SelectedItem
        {
            get;
            set;
        }
        private int selectedIndex;
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                selectedIndex = value;
                RaisePropertyChanged("SelectedIndex");
            }
        }
        private RootModel selectedVariableRoot;
        public RootModel SelectedVariableRoot
        {
            get { return selectedVariableRoot; }
            set
            {
                selectedVariableRoot = value;
                RaisePropertyChanged("SelectedVariableRoot");
            }
        }
        private bool needHoldOn;
        public bool NeedHoldOn
        {
            get { return needHoldOn; }
            set
            {
                needHoldOn = value;
                RaisePropertyChanged("NeedHoldOn");
                logger.LogDebug($"NeedHoldonEvent:{needHoldOn}");
                m_NeedHoldonEvent?.Invoke(needHoldOn);
            }
        }
        private bool isHoldOnInputPopup = false;
        public bool IsHoldOnInputPopup
        {
            get { return isHoldOnInputPopup; }
            set
            {
                isHoldOnInputPopup = value;
                RaisePropertyChanged("IsHoldOnInputPopup");
            }
        }
        public ICommand TextChangedCommand
        {
            get;
            set;
        }
        public ICommand InputCancelCommand
        {
            get;
            set;
        }
        public ICommand ListBoxItemMouseDownCommand
        {
            get;
            set;
        }
        public ICommand MouseDownActionCommand
        {
            get;
            set;
        }
        public string VariableData
        {
            get => m_VariableNodeData;
        }
        private void InputCancelCommandExe(object obj)
        {
            logger.LogDebug($"InputCancelCommandExe:{NeedHoldOn}");
            NeedHoldOn = false;
        }
        /// <summary>
        /// LoadVariablData(setvaluecomponent.ComponentId, "text");
        /// </summary>
        /// <param name="aComponentType"></param>
        /// <param name="aComponentParam"></param>
        public void LoadVariablData(string aComponentType, string aComponentParam)
        {
            Task.Factory.StartNew(() =>
            {
                JObject paramJO = new JObject
                {
                    { "compType", aComponentType },
                    { "attrName", aComponentParam }
                };
                logger.LogDebug($"start flowedit.requestvariable");
                //var dummydata = DummyVariableDataProvider.LoadDataFromJson();
                //RootCollection = new ObservableCollection<RootModel>(dummydata);
                //for (int i = 2; i <= rootCollection.Count; i += 2)
                //{
                //    RootCollection.Insert(i - 1, new RootModel
                //    {
                //        Type = RootType.RootSeparator,
                //    });
                //}
                //return;
                var result = MessageService.SendMessage(TerminalType.CoreServer,
                   "flowedit.requestvariable",
                   paramJO);
                Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                {
                    if (result != null && !string.IsNullOrWhiteSpace(result.Info))
                    {
                        m_VariableNodeData = result.Info;
                        var data = JsonConvert.DeserializeObject<IEnumerable<RootModel>>(result.Info);
                        if (data != null)
                        {
                            //logger.LogDebug($"flowedit.requestvariable return:{result.Info}");
                            logger.LogDebug($"flowedit.requestvariable return");
                            //string variableFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Core", "variable.json");
                            //using (var fileStream = System.IO.File.Open(variableFile, FileMode.OpenOrCreate))
                            //{
                            //    using (var sw = new StreamWriter(fileStream))
                            //    {
                            //        using (var jw = new JsonTextWriter(sw))
                            //        {
                            //            JsonSerializer serializer = new JsonSerializer();
                            //            serializer.Serialize(jw, data);
                            //        }
                            //    }
                            //}
                            foreach (var root in data)
                            {
                                if (root.HasMember)
                                {
                                    root.IsExpanded = true;
                                    foreach (var variable in root.VariableCollection)
                                    {
                                        variable.Source = root.Source;
                                        variable.TypeString = string.Join("/", variable.VarTypeList.Select(v => v.Label));
                                        if (variable.HasMember)
                                        {
                                            //variable.IsExpanded = true;
                                            foreach (var subVariable in variable.MemeberCollection)
                                            {
                                                subVariable.Source = root.Source;
                                                subVariable.TypeString = string.Join("/", subVariable.VarTypeList.Select(v => v.Label));
                                            }
                                        }
                                    }
                                }
                            }
                            RootCollection = new ObservableCollection<RootModel>(data);
                            for (int i = 2; i <= rootCollection.Count; i += 2)
                            {
                                RootCollection.Insert(i - 1, new RootModel
                                {
                                    Type = RootType.RootSeparator,
                                });
                            }
                            //TextChangedCommandExe(null);
                        }
                        else
                        {
                            logger.LogWarning($"flowedit.requestvariable:fail convert data to json");
                            RootCollection = new ObservableCollection<RootModel>
                        {
                            new RootModel
                            {
                                Type = RootType.RootNull,
                            }
                        };
                        }
                    }
                    else
                    {
                        logger.LogError($"flowedit.requestvariable:result is null");
                        RootCollection = new ObservableCollection<RootModel>
                        {
                            new RootModel
                            {
                                Type = RootType.RootNull,
                            }
                        };
                    }
                }));
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });         
        }
        private void TextChangedCommandExe(object obj)
        {
            if (RootCollection != null)
            {
                foreach (var rootitem in RootCollection)
                {
                    if (rootitem.HasMember && rootitem.Type == RootType.RootCollection)
                    {
                        rootitem.SetFilter(SearchString);
                        if (string.IsNullOrEmpty(SearchString))
                        {
                            rootitem.IsExpanded = true;
                        }
                        else
                        {
                            rootitem.IsExpanded = rootitem.HasMatch;
                        }
                    }
                }
                HighlightContent.Mode = HighlightContentMode.AnyKey;
                HighlightContent.ToHighlight = SearchString;
            }
        }
        private void MouseDownActionCommandExe(object obj)
        {
            logger.LogDebug($"MouseDownActionCommandExe:{obj}");
            if (obj is RootModel)
            {
                return;
            }
            if (obj is BaseVariableModel selectedVarItem)
            {
                if (selectedVarItem != null && selectedVarItem.IsEnabled)
                {
                    //ResultString = string.Empty;
                    ResultString = selectedVarItem?.Title;
                    IsOpenVarPopup = false;
                    logger.LogDebug($"SelectedVarItem.Set:{ResultString}");
                    SelectedItem = selectedVarItem;
                }
                else
                {
                    return;
                }
            }
        }

        private void ListBoxItemMouseDownCommandExe(object obj)
        {
            logger.LogDebug($"ListBoxItemMouseDownCommandExe:{obj}");
            if (obj is RootModel)
            {
                return;
            }
            if (obj is BaseVariableModel selectedVarItem)
            {
                if (selectedVarItem != null && selectedVarItem.IsEnabled)
                {
                    //ResultString = string.Empty;
                    ResultString = selectedVarItem?.Title;
                    IsOpenVarPopup = false;
                    logger.LogDebug($"SelectedVarItem.Set:{ResultString}");
                    SelectedItem = selectedVarItem;
                }
                else
                {
                    return;
                }
            }
        }
      
    }
}
