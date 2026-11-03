using II.RPA.Logging;
using II.RPA.Logging.Formatting;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.FacDialog;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.DataCollection.Utils;
using II.RPA.Plugin.GUI.DataCollection.V2.DataFilter;
using II.RPA.Plugin.GUI.DataCollection.V2.DataProcessing;
using II.RPA.Plugin.GUI.DataCollection.V2.Windows;
using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using II.RPA.Plugin.GUI.HighLight;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.UIProviders.ScanAnimation;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Shell;
using System.Windows.Threading;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;

namespace II.RPA.Plugin.GUI.DataCollection.V2
{
    internal class DataCollectingManagerV2
    {
        private static ILogger logger = Logger.OfContext();
        private readonly static Lazy<DataCollectingManagerV2> _instance = new Lazy<DataCollectingManagerV2>(() => new DataCollectingManagerV2());
        public static DataCollectingManagerV2 Instance => _instance.Value;

        private readonly Thread m_UIThread;
        private Dispatcher m_UIDispatcher;

        private DataCollectHighLightWnd m_DataCollectHighLightWnd;
        internal DataCollectHighLightWnd DataCollectHighLightWnd => m_DataCollectHighLightWnd;

        internal bool ExecutingDataCollectionApi = false;

        #region 构造函数，初始化拾取STA线程

        private DataCollectingManagerV2()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ManualResetEventSlim m_StartedEvent = new ManualResetEventSlim(false);
            m_UIThread = new Thread(() =>
                {
                    SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                    m_UIDispatcher = Dispatcher.CurrentDispatcher;
                    m_StartedEvent.Set();
                    Dispatcher.Run();
                });
            m_UIThread.SetApartmentState(ApartmentState.STA);
            m_UIThread.IsBackground = true;
            m_UIThread.Start();
            m_StartedEvent.Wait(5000);
            logger.LogDebug($"DataCollectingManagerV2 {stopwatch.ElapsedMilliseconds}");
            Task.Run(() =>
            {
                Task.Delay(5000).Wait();
                m_StartedEvent.Dispose();
            });
        }
        #endregion

        #region 开始采集拾取效果
        private ParamDataCollectCaptureModel CaptureParam { get; set; }
        internal void RestartCapture()
        {
            if (CaptureParam != null)
            {
                StartDataCollect(CaptureParam);//这里通过重调StartCapture 初始化拾取状态、初始效果等，或许会有多余操作，但便于统一维护
            }
        }
        public void StartDataCollect(ParamDataCollectCaptureModel paramModel)
        {
            CaptureParam = paramModel;
            Task.Run(() =>
            {
                GuiElement.ReadyToCapture();
                DataCollectingCaptureTaskV2.Instance.Start();
                logger.LogDebug($"StartCapture");
            });
            m_UIDispatcher.Invoke(new Action(() =>
            {
                if (m_DataCollectHighLightWnd == null)
                {
                    m_DataCollectHighLightWnd = new DataCollectHighLightWnd();
                    m_DataCollectHighLightWnd.DoSelectAction = DoSelect;
                    m_DataCollectHighLightWnd.Show();
                }
                ShowWnd();
                m_DataCollectHighLightWnd.StartCapture(paramModel);
            }));

        }

        #endregion

        #region 停止采集效果

        public void StopDataCollect()
        {
            if (m_DataCollectHighLightWnd == null)
            {
                return;
            }
            m_UIDispatcher.Invoke(new Action(() =>
            {
                m_DataCollectHighLightWnd?.SuspendCapture();
                RPAUtils.Retry(() =>
                {
                    m_DataCollectHighLightWnd?.Hide();
                    RPAUtils.Delay(50);
                    return (!m_DataCollectHighLightWnd.IsActive || m_DataCollectHighLightWnd.Visibility == System.Windows.Visibility.Hidden);
                }, 500);
            }));
        }

        #endregion

        #region 显示并置顶窗口 ShowWnd()
        public void ShowWnd()
        {
            if (!m_DataCollectHighLightWnd.IsActive || m_DataCollectHighLightWnd.Visibility == System.Windows.Visibility.Hidden)
            {
                m_DataCollectHighLightWnd.Show();
            }

            if (!m_DataCollectHighLightWnd.Topmost)
            {
                m_DataCollectHighLightWnd.Topmost = true;
            }
        }
        #endregion

        #region 判断是否是不用拾取的区域

        internal bool IsNonCapturePosition()
        {
            var point = System.Windows.Forms.Cursor.Position;
            IntPtr hWnd = NativeMethods.WindowFromPoint(point);
            string pName = UiCommon.GetProcessNameByCache(hWnd);
            return Configurations.Info.ClientProcessNames.Any(pn => pName.Contains(pn));
        }

        #endregion

        #region 拾取元素，采集数据
        internal void DoSelect(IGuiNode highlightElement, ParamDataCollectCaptureModel captureParam)
        {
            logger.LogDebug("DoSelect begin");
            logger.LogDebug(LogBlock.NullCheck(highlightElement, "CurrentHighlightElement"));
            //DoSelect 后是否需要fac重新开始拾取
            bool facRestart = true;
            RetDataCollectConfigurationModel_v2 RetCaptureModel = new RetDataCollectConfigurationModel_v2();
            try
            {
                var node = highlightElement as IGuiNode_6_1_0;
                var isGuiWebNode = node is IGuiWebNode_690;

                if (!captureParam.IsFirstCapture)
                {
                    if (captureParam.PageInfo?.WindowType == "web" && !isGuiWebNode)
                    {
                        //MessageHelper.ShowDialog("选择采集类型与采集目标元素类型不符，无法采集此类元素数据。");
                        throw new RPAException(eErrorMessageType.Error_DataCollection_ElementTypeError);
                    }
                    if (captureParam.PageInfo?.WindowType != "web" && isGuiWebNode)
                    {
                        //MessageHelper.ShowDialog("选择采集类型与采集目标元素类型不符，无法采集此类元素数据。");
                        throw new RPAException(eErrorMessageType.Error_DataCollection_ElementTypeError);
                    }
                    if (captureParam.PageInfo?.WindowType != "web" && captureParam.PageInfo?.WindowType.ToLower() != node.GuiType.ToString().ToLower())
                    {
                        //MessageHelper.ShowDialog("无法跨页面采集，返回原页面【继续采集】或清空并重新采集。");
                        throw new RPAException(eErrorMessageType.Error_DataCollectionV2_DifferentPages);
                    }
                }

                if (captureParam.IsTable)
                {
                    var rpaex = new RPAException(eErrorMessageType.Error_DataCollection_CapturedTableElement);
                    throw rpaex;
                }
                var element = new GuiElement(highlightElement);
                //首次数据采集拾取时，判断是否是表格
                bool isUseTableElement = false;
                if (captureParam.IsFirstCapture)
                {
                    isUseTableElement = this.UseTableElement(element, isGuiWebNode);
                }
                logger.LogDebug($"get selector UIElement_OnSelected");
                string orgSelector = GetCaptureElementSelector(element, isUseTableElement, isGuiWebNode);
                var pageInfo = this.GetPageInfo(element, orgSelector, out var selectorObject);
                var isSamePage = this.IsSamePage(captureParam.PageInfo, pageInfo);
                if (!isSamePage)
                {
                    var rpaex = new RPAException(eErrorMessageType.Error_DataCollectionV2_DifferentPages);
                    throw rpaex;
                }
                if (isUseTableElement)
                {
                    RetCaptureModel.CollectionConfig.TableSelector = selectorObject;
                }
                else
                {
                    var retSelectorObj = ProcessSelector(element, selectorObject, captureParam);
                    RetCaptureModel.CollectionConfig.ColumnSelectorArray.Add(retSelectorObj);
                    var defColumnConfig = new ColumnConfig();
                    RetCaptureModel.CollectionConfig.ColumnConfigArray.Add(defColumnConfig);
                    if (isGuiWebNode)
                    {
                        defColumnConfig.ColumnAttributes.AddRange((node as IGuiWebNode_690).GetUsableAttributes());
                        switch (retSelectorObj?.Last()?.Name?.ToLower())
                        {
                            case "img":
                                defColumnConfig.DataAttrName = "src";
                                break;
                            case "a":
                                if (captureParam.IsSimilarCapture)
                                    break;
                                //添加链接列
                                RetCaptureModel.CollectionConfig.ColumnSelectorArray.Add(retSelectorObj);
                                var defColumnConfigClone = defColumnConfig.ToObject<ColumnConfig>();
                                defColumnConfigClone.DataAttrName = "href";
                                RetCaptureModel.CollectionConfig.ColumnConfigArray.Add(defColumnConfigClone);
                                break;
                        }
                    }
                }
                if (captureParam.IsFirstCapture)
                {
                    RetCaptureModel.PageInfo = pageInfo;
                    ////保留原格式
                    //RetCaptureModel.TurningPageConfig = new TurningPageConfig
                    //{
                    //    Selector = ""
                    //};
                    this.GetTurningPageElementSelector(element, captureParam);
                }
                RetCaptureModel.CollectionConfig.SelectorVersion = GlobleConfigurations.CurrentSelectorVersion;
                logger.LogDebug($"RetCaptureModel ：{RetCaptureModel.ToJson()}");
                this.StopDataCollect();
                if (captureParam.IsSimilarCapture)
                {
                    FacMessageHelper.SendSimilarCaptureDataToFactory(RetCaptureModel);
                }
                else
                {
                    FacMessageHelper.SendCaptureDataToFactory(RetCaptureModel);
                }
                logger.LogDebug($"Send Capture Data To Factory End");
            }
            catch (Exception ex)
            {
                string statusCode = "0";
                RPAException rpaEx;
                if (!(ex is RPAException))
                {
                    rpaEx = new RPAException(eErrorMessageType.Error_ElementCaptureError, ex);
                }
                else if (ex.IsRPAException(eErrorMessageType.Error_DataCollection_SimileCaptureDifferentElementTypes, out rpaEx))
                {
                    facRestart = false;
                }
                else if (captureParam.IsSimilarCapture && ex.IsRPAException(eErrorMessageType.Error_DataCollection_ElementTypeError, out rpaEx))
                {
                    facRestart = false;
                }
                else if (ex.IsRPAException(eErrorMessageType.Error_DataCollectionV2_DifferentPages, out rpaEx))
                {
                    statusCode = "2";
                }
                this.StopDataCollect();
                FacMessageHelper.SendExceptionToFactory(statusCode, rpaEx.ErrorCodeSuffixMessage, facRestart);
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "完成拾取动作时发生了异常，本次拾取结束");
            }
            finally
            {
                if (facRestart)
                {
                    this.CaptureParam = null;//fac Restart ,DLL 不在需要记录拾取参数
                }
                else
                {
                    this.RestartCapture();
                }
            }

        }

        private string GetCaptureElementSelector(GuiElement element, bool isUseTableElement, bool isGuiWebNode)
        {
            if (!isGuiWebNode)
            {
                return isUseTableElement ? element.TableSelector : element.Selector;
            }
            if (isUseTableElement)
            {
                element.TryGetTableElement(out var tableNode);
                var tableElement = new GuiElement((IGuiNode)tableNode);
                return tableElement.GetColletionColumnSelector();
            }
            else
            {
                return element.GetColletionColumnSelector();
            }
        }
        private bool UseTableElement(GuiElement element, bool isGuiWebNode)
        {
            var isTable = element.ElementIsTable();
            if (!isTable)
                return isTable;
            if (!isGuiWebNode)
            {
                if (!isTable && ((IGuiNode_6_1_0)element.uiNode).GuiType != eGuiType.Windows)
                {
                    //通知前端异常
                    var rpaEx = new RPAException(eErrorMessageType.Error_DataCollection_ElementNotTableError);
                    //FacMessageHelper.SendExceptionToFactory("0", rpaEx.ErrorCodeSuffixMessage);
                    throw rpaEx;
                }
                if (element.uiNode is IGuiWindowsNode uiaNode && uiaNode.ChromePageTable)
                {
                    return ShowCollectElementOrTableDialog();
                }
            }
            else
            {
                return ShowCollectElementOrTableDialog();
            }
            return isTable;
        }
        private bool ShowCollectElementOrTableDialog()
        {
            WindowDialog dialog = new WindowDialog(new ParamShowWindowDialogModel
            {
                Title = "提示",
                Type = WindowDialogType.warning.ToString(),
                Content = eCommonMessageType.DataCollectingManagerV2_cs_ChengGongShiBieBiaoGeNeiYuanSu.GetMessage(),
            },
            ButtonType.UseTableDialog_CollectElement,
            ButtonType.UseTableDialog_CollectTable);

            return dialog.ShowDialog() == ButtonType.UseTableDialog_CollectTable;
        }
        private PageInfo GetPageInfo(GuiElement guiElement, string selectorJson, out List<SelectorProps> selectorObject)
        {
            try
            {
                PageInfo pageInfo = new PageInfo();
                JToken jToken = JToken.Parse(selectorJson);
                if (jToken is JObject)
                {
                    var common = selectorJson.ToObject<RPACommon>();
                    selectorObject = common.Selector.ToObject<List<SelectorProps>>();
                    pageInfo.WindowType = common.CommonProps.GUIType.ToLower();
                    pageInfo.ProcessName = common.CommonProps.ProcessName;
                }
                else
                {
                    selectorObject = selectorJson.ToObject<List<SelectorProps>>();
                }
                var tabProps = selectorObject.Find(o => o.Name == ConstFields.DATACOLECTING_SELECTORPROPS_TAB);
                if (tabProps != null)
                {
                    selectorObject.Remove(tabProps);
                    var title = tabProps.Props.Find(p => p.Name == ConstFields.DATACOLECTING_PAGE_TITLE)?.Value ?? string.Empty;
                    var url = tabProps.Props.Find(p => p.Name == ConstFields.DATACOLECTING_PAGE_URL)?.Value ?? string.Empty;
                    pageInfo.WindowTitle = title;
                    pageInfo.PageUrl = url;
                    pageInfo.WindowType = "web";
                    pageInfo.ProcessName = guiElement.ProcessName;
                    if (guiElement.IsGuiType(eGuiType.IE.ToString()))
                    {
                        var hWnd = ((ElementGuiNode)guiElement.uiNode).PageElement.TabPage.BrowserWindowInfo.Handle;
                        BrowserWindow browserWindow = BrowserWindowProvider.MatchBrowserWindowByHandle(hWnd);
                        if (browserWindow == null || browserWindow.WindowType == (Commons.WindowMatch.WindowType.Windows | Commons.WindowMatch.WindowType.IE))
                        {
                            pageInfo.ExtendedTags |= ExtendedTags.EmbeddedIE;
                        }
                    }
                }
                else
                {
                    var title = selectorObject.FirstOrDefault()?.Props.Find(p => p.Name == ConstFields.DATACOLECTING_WINDOW_TITLE)?.Value ?? string.Empty;
                    pageInfo.WindowTitle = title;
                }
                return pageInfo;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw ex;
            }
        }

        private bool IsSamePage(PageInfo pageInfo1, PageInfo pageInfo2)
        {
            if (pageInfo1 == null || pageInfo2 == null)
            {
                return true;
            }
            if (pageInfo1.WindowType == "web")
            {
                string currentUrl = pageInfo2.PageUrl;
                if (!string.IsNullOrEmpty(currentUrl))
                {
                    var count = currentUrl.IndexOf('?');
                    if (count > 0)
                        currentUrl = currentUrl.Substring(0, count);
                }

                bool isSamePage = false;
                if (pageInfo1.WindowTitle == pageInfo2.WindowTitle)
                {
                    isSamePage = true;
                }

                if (!isSamePage && pageInfo1.PageUrl.Contains(currentUrl))
                {
                    isSamePage = true;
                }
                return isSamePage;
            }
            else
            {
                return pageInfo1.WindowType == pageInfo2.WindowType && pageInfo1.WindowTitle == pageInfo2.WindowTitle;

            }
        }

        private List<SelectorProps> ProcessSelector(GuiElement element, List<SelectorProps> selectorObject, ParamDataCollectCaptureModel captureParam)
        {
            try
            {
                List<SelectorProps> processedSelector = null;
                //相似元素拾取  对比生成
                if (captureParam.IsSimilarCapture)
                {
                    if (element.uiNode is IGuiWebNode_690)
                    {
                        processedSelector = element.CompareBulidCollectDataSelectorV2(new ParamBulidCollectDataSelectorModel
                        {
                            OriginalSelector = captureParam.SimilarSelector,//副本
                            CompareOriginalSelector = selectorObject
                        });
                    }
                    else
                    {
                        //非网页类元素，依旧使用老逻辑
                        processedSelector = element.CompareBulidCollectDataSelector(new ParamBulidCollectDataSelectorModel
                        {
                            OriginalSelector = captureParam.SimilarSelector,//副本
                            CompareOriginalSelector = selectorObject
                        });
                    }
                }
                //数据普通元素  自动生成相似元素
                else
                {
                    if (!(element.uiNode is IGuiWebNode_690))
                    {
                        processedSelector = selectorObject;
                    }
                    else
                    {
                        try
                        {
                            processedSelector = element.GetSimilarElementSelector(selectorObject);
                        }
                        catch (RPAException rpaex)
                        {
                            if (rpaex.ErrorMessageType == eErrorMessageType.Error_ElementNode_Error)
                            {
                                return selectorObject;
                            }
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, rpaex, "自动获取相似元素异常");
                            throw rpaex;
                        }
                    }

                }

                return processedSelector;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "处理拾取得到的selector异常");
                //MessageHelper.ShowWeakError(ex, "拾取数据处理异常");
                throw ex;
            }
        }


        private void GetTurningPageElementSelector(GuiElement element, ParamDataCollectCaptureModel captureParam)
        {
            Task.Run(() =>
            {
                try
                {
                    //只有数据采集拾取，且为首次拾取才执行获取下一页逻辑
                    if (!captureParam.IsFirstCapture || captureParam.IsSimilarCapture)
                    {
                        return string.Empty;
                    }
                    var guiNode = element.GetTurningPageElement();
                    if (guiNode == null)
                    {
                        return string.Empty;
                    }
                    string selector = (guiNode as IGuiNode_6_6_0).GetSelector(true);
                    if (!string.IsNullOrEmpty(selector))
                    {
                        var common = JsonConvert.DeserializeObject<RPACommon>(selector);
                        common.CommonProps.ResDirPath = $@"res\xxxxx";//没有路径前端可能会误删
                        string screenShotPath = string.Empty;//缩略图地址
                        string fileName = Guid.NewGuid().ToString() + ".png";
                        screenShotPath = Path.Combine("res", fileName);
                        string screenShotFilePath = System.IO.Path.Combine(GlobleConfigurations.PackageDirectory, screenShotPath);
                        using (Stream sm = this.GetType().Assembly.GetManifestResourceStream($"II.RPA.Plugin.GUI.DataCollection.Assets.Images.TurnPageDefultImg_Embedded.png"))
                        {
                            using (var img = new Bitmap(sm))
                            {
                                if (!Directory.Exists(Directory.GetParent(screenShotFilePath).FullName))
                                {
                                    Directory.CreateDirectory(Directory.GetParent(screenShotFilePath).FullName);
                                }
                                img.Save(screenShotFilePath);
                            }
                        }
                        common.CommonProps.filePath = screenShotPath;
                        selector = common.ToJson();
                        FacMessageHelper.SendPageTurnButtonElement(selector);
                    }
                    return selector;
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "自动获取翻页按钮逻辑发生异常，用户可以人为再次拾取，忽略此异常");
                    return string.Empty;
                }
            }).ConfigureAwait(false);
        }


        #endregion

        #region 执行数据采集
        /// <summary>
        /// 使用选择器采集数据
        /// </summary>
        /// <param name="paramModel"></param>
        /// <param name="dataFileList">本地静态文件名列表，按照采集顺序排序</param>
        /// <returns></returns>
        internal static async Task<List<List<string>>> CollectDataAsync(
            ParamDataCollect_V2Model paramModel,
            List<string> dataFileList,
            IPluginInvoker pluginInvoker,
            PluginExecutionContext executionContext)
        {
            GlobleConfigurations.RunningStateManager.WaitForRunning();

            int collectTotalCount = 0;
            List<List<string>> collectDataTotalList = new List<List<string>>();
            List<List<string>> tempList = new List<List<string>>();
            dataFileList = dataFileList ?? new List<string>();
            IGuiOperator_6_9_0 guiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(paramModel.GuiType, paramModel.WindowInfo);

            if (guiOperator == null)
            {
                FacMessageHelper.SendNotCollectionPageToFactory("未匹配到可用的浏览器".I18n("DataCollectingManagerV2_F80485CB"));
                logger.LogDebug($"未匹配到可用的浏览器");
                return new List<List<string>>();
            }
            if (!paramModel.IsTable)// && SupportBlockDataGuiTypes.Any(t => t == guiOperator.GuiType))
            {
                List<SelectorProps> sameParentsProps = new List<SelectorProps>();
                //获取相同的父级
                sameParentsProps = guiOperator.GetSameParentsProps_v2(paramModel);
                paramModel.DataCollectConfig.CollectionConfig.CommonSelector = sameParentsProps;
                if (sameParentsProps.Count > 0)
                {
                    paramModel.DataCollectConfig.CollectionConfig.ColumnSelectorArray.ForEach((selector) =>
                    {
                        selector.RemoveRange(0, sameParentsProps.Count);
                    });
                }
            }
            int columnCount = paramModel.DataCollectConfig.CollectionConfig.ColumnConfigArray.Count;
            bool isWeb = paramModel.DataCollectConfig.PageInfo.WindowType == "web";
            DataProcessor dataProcessor = new DataProcessor(paramModel);
            logger.LogDebug($"开始采集");
            for (int pageNumber = 0; pageNumber < paramModel.TurnPageCount; pageNumber++)
            {
                GlobleConfigurations.RunningStateManager.WaitForRunning();
                logger.LogDebug($"开始采集第{pageNumber + 1}页数据");
                if (pageNumber != 0)
                {
                    var clickState = await ElementMouseClickAsync(
                        paramModel.DataCollectConfig.TurningPageConfig,
                        pluginInvoker,
                        executionContext).ConfigureAwait(false);
                    {
                        if (!clickState)//没找到翻页按钮，结束拾取

                            break;
                    }
                    logger.LogDebug($"翻页点击，点击第{pageNumber + 1}次");
                    RPAUtils.Delay(isWeb ? 200 : paramModel.DataCollectConfig.CollectionConfig.ScrollVerticalDelay);
                }

                //采集数据
                List<List<string>> collectDataList = null;
                bool hasNewdata = false;
                GlobleConfigurations.RunningStateManager.WaitForRunning();
                //采集新数据  或者 网页端标记颜色
                collectDataList = CollectPageData(paramModel, guiOperator, tempList, pageNumber, out hasNewdata);

                if (!hasNewdata)
                {
                    break;
                }
                tempList.Clear();
                tempList.AddRange(collectDataList.ToObject<List<List<string>>>());
                //非网页  表格 且有采集数据，且有列配置信息
                if (!isWeb && paramModel.IsTable && collectDataList.Count > 0 && columnCount > 0)
                {
                    GetCollecttionTableData(paramModel, collectDataList, collectDataTotalList);
                }
                else
                {
                    collectDataTotalList.AddRange(collectDataList);
                }



                //表格  有采集数据 有列配置等移除表头 。表格首次预览时 没有列配置，需要使用表头数据
                if (paramModel.IsTable && collectDataTotalList.Count > 0 && columnCount > 0)
                {
                    collectDataTotalList.RemoveAt(0);
                }

                var processData = dataProcessor.ProcessData(collectDataTotalList);
                collectDataTotalList = processData.collectDataTotalList;
                List<int> invalidRowIdxs = processData.invalidRowIdxs;

                if (isWeb && paramModel.IsPreview && paramModel.ColorGuidanceConfig.Highlight == HighlightType.Highlight)
                {
                    Task.Run(() =>
                    {
                        paramModel.ColorGuidanceConfig.ActionCmd = "set";
                        if (paramModel.IsTable && collectDataTotalList.Count > 0 && columnCount > 0)
                        {
                            invalidRowIdxs = invalidRowIdxs.Select(idx => idx + 1).ToList();
                        }
                        paramModel.ColorGuidanceConfig.InvalidRowIdxs = invalidRowIdxs;
                        CollectPageData(paramModel, guiOperator, tempList, pageNumber, out _);
                    });
                }
                collectTotalCount += collectDataTotalList.Count;
                if (paramModel.CollectDataCount > 0 && collectTotalCount > paramModel.CollectDataCount)
                {
                    int removeStart = collectDataTotalList.Count - (collectTotalCount - paramModel.CollectDataCount);
                    collectDataTotalList.RemoveRange(removeStart, collectTotalCount - paramModel.CollectDataCount);
                }

                //拾取第一页 有列配置 。表格首次预览没有列配置
                if (pageNumber == 0 && columnCount > 0)
                {
                    List<string> titleRows = dataProcessor.GetTitleRows();
                    collectDataTotalList.Insert(0, titleRows);
                }
                string fileName = $"datafile_{pageNumber}.json";
                SaveDataFile(collectDataTotalList, paramModel.DataFilePath, fileName);
                dataFileList.Add(fileName);
                collectDataTotalList.Clear();
                if (paramModel.CollectDataCount > 0 && collectTotalCount >= paramModel.CollectDataCount)
                {
                    break;// 按条采集完成，采集结束
                }

                logger.LogDebug($"采集第{pageNumber + 1}页数据 结束");
            }
            return collectDataTotalList;
        }
        internal static List<List<string>> CollectWaterfallData(ParamDataCollect_V2Model paramModel, ref List<string> dataFileList)
        {
            GlobleConfigurations.RunningStateManager.WaitForRunning();

            List<List<string>> collectDataTotalList = new List<List<string>>();
            List<List<string>> tempList = new List<List<string>>();
            dataFileList = dataFileList ?? new List<string>();
            IGuiOperator_6_9_0 guiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(paramModel.GuiType, paramModel.WindowInfo);
            if (guiOperator == null)
            {
                FacMessageHelper.SendNotCollectionPageToFactory("未匹配到可用的浏览器".I18n("DataCollectingManagerV2_F80485CB"));
                return new List<List<string>>();
            }


            if (!paramModel.IsTable)// && SupportBlockDataGuiTypes.Any(t => t == guiOperator.GuiType))
            {
                List<SelectorProps> sameParentsProps = new List<SelectorProps>();
                //获取相同的父级
                sameParentsProps = guiOperator.GetSameParentsProps_v2(paramModel);
                paramModel.DataCollectConfig.CollectionConfig.CommonSelector = sameParentsProps;
                if (sameParentsProps.Count > 0)
                {
                    paramModel.DataCollectConfig.CollectionConfig.ColumnSelectorArray.ForEach((selector) =>
                    {
                        selector.RemoveRange(0, sameParentsProps.Count);
                    });
                }
            }
            int columnCount = paramModel.DataCollectConfig.CollectionConfig.ColumnConfigArray.Count;
            bool isWeb = paramModel.DataCollectConfig.PageInfo.WindowType == "web";
            DataProcessor dataProcessor = new DataProcessor(paramModel, true);
            int scrollVerticalDelay = paramModel.DataCollectConfig.CollectionConfig.ScrollVerticalDelay;

            paramModel.DataCollectConfig.CollectionConfig.ScrollVerticalDelay = 0;
            GlobleConfigurations.RunningStateManager.WaitForRunning();
            Stopwatch stopwatch = Stopwatch.StartNew();
            logger.LogDebugLazy(() => $"CollectDataCount:{paramModel.CollectDataCount}");
            do
            {
                //采集数据
                List<List<string>> collectDataList = null;
                GlobleConfigurations.RunningStateManager.WaitForRunning();

                //collectDataList = guiOperator.CollectData_v2(paramModel);
                var collectData = ((IGuiWebOperator)guiOperator).GetCollectWaterfallDataFromConfigList(paramModel);
                collectDataList = collectData.collectionData;
                if (collectData.markedElement != null)
                {
                    paramModel.MarkedElement = collectData.markedElement;//不同滚动模式下，这不一定是真正的lastElement ,
                }
                //非网页  表格 且有采集数据，且有列配置信息
                if (!isWeb && paramModel.IsTable && collectDataList.Count > 0 && columnCount > 0)
                {
                    GetCollecttionTableData(paramModel, collectDataList, tempList);
                }
                else
                {
                    tempList.AddRange(collectDataList);
                }

                //表格  有采集数据 有列配置等移除表头 。表格首次预览时 没有列配置，需要使用表头数据
                if (paramModel.IsTable && tempList.Count > 0 && columnCount > 0)
                {
                    tempList.RemoveAt(0);
                }
                int collectSourceDataCount = dataProcessor.CollectSourceDataCount;

                var processData = dataProcessor.ProcessData(tempList);
                tempList.Clear();

                logger.LogDebugLazy(() => $"lasttime collectSourceDataCount:{collectSourceDataCount},new collectSourceDataCount:{dataProcessor.CollectSourceDataCount},new CollectDataCount:{dataProcessor.CollectDataCount}");
                if (dataProcessor.CollectDataCount >= paramModel.CollectDataCount)
                {
                    dataProcessor.CollectDataTotalList.RemoveRange(paramModel.CollectDataCount, dataProcessor.CollectDataCount - paramModel.CollectDataCount);
                    break;
                }
                if (collectSourceDataCount != dataProcessor.CollectSourceDataCount)
                {
                    logger.LogDebugLazy(() => $"Restart timer ,CollectDataCount:{paramModel.CollectDataCount},CollectSourceDataCount:{dataProcessor.CollectSourceDataCount}");
                    stopwatch.Restart();
                }
            } while (stopwatch.ElapsedMilliseconds <= scrollVerticalDelay);
            stopwatch.Stop();
            collectDataTotalList = dataProcessor.CollectDataTotalList;
            logger.LogDebug($"采集数据 结束");
            if (columnCount > 0)
            {
                List<string> titleRows = dataProcessor.GetTitleRows();
                collectDataTotalList.Insert(0, titleRows);
            }
            string fileName = $"datafile_0.json";
            SaveDataFile(collectDataTotalList, paramModel.DataFilePath, fileName);
            dataFileList.Add(fileName);
            collectDataTotalList.Clear();
            return collectDataTotalList;
        }
        private static void GetCollecttionTableData(ParamDataCollect_V2Model paramModel, List<List<string>> rawDataList, List<List<string>> collectDataTotalList)
        {
            int columnCount = paramModel.DataCollectConfig.CollectionConfig.ColumnConfigArray.Count;
            if (rawDataList[0].Count > paramModel.DataCollectConfig.CollectionConfig.ColumnConfigArray.Max(c => c.DefaultColumnIndex))
            {
                List<string> rowList;
                for (int i = 0; i < rawDataList.Count; i++)
                {
                    rowList = new List<string>();
                    for (int j = 0; j < columnCount; j++)
                    {
                        rowList.Add(rawDataList[i][paramModel.DataCollectConfig.CollectionConfig.ColumnConfigArray[j].DefaultColumnIndex]);
                    }
                    collectDataTotalList.Add(rowList);
                }
            }
        }
        /// <summary>
        /// 点击元素
        /// </summary>
        /// <param name="selectorProps"></param>
        /// <param name="guiType"></param>
        /// <returns></returns>
        internal static async Task<bool> ElementMouseClickAsync(
            TurningPageConfig turningPageConfig,
            IPluginInvoker pluginInvoker,
            PluginExecutionContext executionContext)
        {
            ParamMouseClickModel paramMouseClick = new ParamMouseClickModel()
            {
                Selector = turningPageConfig.Selector,
                clickType = MouseParamDef.CLK_BTN_LEFT,
                keyType = MouseParamDef.CLK_TYPE_SINGLE,
                issimulate = turningPageConfig.SimulateClick,
                cursorPos = MouseParamDef.CUR_POS_CENTER,
                timeout = 10000,
                IsEnableResultVerify = false
            };
            JMessage msg = new JMessage();
            try
            {
                string jsonParam = paramMouseClick.ToJson();
                for (var attempt = 0; attempt < 100; attempt++)
                {
                    string result = await pluginInvoker.InvokeAsync(
                        new PluginRoute("Element", "MouseClick"),
                        jsonParam).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(result))
                    {
                        throw new RPAException(eErrorMessageType.Error_DataCollection_NextBtnNotFound);
                    }
                    msg = result.ToObject<JMessage>();
                    if (msg.Code == 0) return true;
                    if (attempt < 99) await Task.Delay(2000).ConfigureAwait(false);
                }
                if (msg.UnifyCode.EndsWith(((int)eErrorMessageType.Error_NotFoundElement).ToString())) return false;
                throw new RPAException(eErrorMessageType.Error_DataCollection_NextBtnNotFound);
            }
            catch (RPAException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "数据采集-下一页按钮点击失败");
                var rpaEx = new RPAException(eErrorMessageType.Error_DataCollection_NextBtnNotFound, ex);
                throw rpaEx;
            }
        }
        private static void SaveDataFile(List<List<string>> collectDataTotalList, string dataFilePath, string fileName)
        {
            if (!Directory.Exists(dataFilePath))
            {
                Directory.CreateDirectory(dataFilePath);
            }
            string filePath = Path.Combine(dataFilePath, fileName);
            File.WriteAllText(filePath, collectDataTotalList.ToJson(), Encoding.UTF8);
            logger.LogDebug($"{fileName},共保存{collectDataTotalList.Count}行数据（采集首页含标题）");

        }


        private static List<List<string>> CollectPageData(ParamDataCollect_V2Model paramModel, IGuiOperator_6_9_0 guiOperator, List<List<string>> previousDataList, int pageNum, out bool hasNewData)
        {
            List<List<string>> collectDataList = new List<List<string>>();
            //int timeout = 5 * 1000;//按条加载默认最多等待时间（主要考虑瀑布流场景）
            int timeout = paramModel.DataCollectConfig.CollectionConfig.ScrollVerticalDelay;

            using (var cts = new CancellationTokenSource(timeout))
            {
                hasNewData = RPAUtils.Retry(() =>
                {
                    try
                    {
                        GlobleConfigurations.RunningStateManager.WaitForRunning();
                        collectDataList = guiOperator.CollectData_v2(paramModel);
                        if (paramModel.IsColorGuidance)
                        {
                            return true;
                        }

                        /* 1、第一页数据不需要进行相似度对比
                         * 2、无有效上一页数据也不需要对比
                         * 3、采集到数据才有对比意义，网页在脚本中已等待，应用才翻页后已延时等待，未采集到数据则认为无数据 采集结束
                         */
                        if (pageNum == 0 || previousDataList.Count == 0 || collectDataList.Count == 0)
                        {
                            return true;
                        }
                        //与上一次采集数据是否相同，相同认为已经是最后一页；
                        if (pageNum != 0)
                        {
                            //判断本次数据与上一页数据是否完全相同
                            var isSame = IsSameList(previousDataList, collectDataList);
                            if (!isSame)
                            {
                                return true;
                            }
                            logger.LogDebug("两次采集数据完全相同，有可能是数据没有加载完，会再次尝试获取新的数据，最大耗时>=10s");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.IsRPAException(eErrorMessageType.Error_DataCollectionV2_NotCollectionPage, out RPAException rpaEx)
                            || ex.IsRPAException(eErrorMessageType.Error_PageNotFound, out rpaEx))
                        {
                            FacMessageHelper.SendNotCollectionPageToFactory(rpaEx.ErrorCodeSuffixMessage);
                            return true;
                        }
                        else
                        {
                            logger.LogError(ex);
                            throw ex;
                        }
                    }
                    return false;
                }, cts.Token, 300);
            }

            return collectDataList;
        }
        private static List<List<string>> CollectPageDataByPage(ParamDataCollect_V2Model paramModel, IGuiOperator_6_9_0 guiOperator, List<List<string>> previousDataList, int pageNum, out bool hasNewData)
        {
            List<List<string>> collectDataList = new List<List<string>>();
            hasNewData = RPAUtils.Retry(() =>
            {
                try
                {
                    collectDataList = guiOperator.CollectData_v2(paramModel);
                }
                catch (Exception ex)
                {
                    if (ex.IsRPAException(eErrorMessageType.Error_DataCollectionV2_NotCollectionPage, out RPAException rpaEx)
                        || ex.IsRPAException(eErrorMessageType.Error_PageNotFound, out rpaEx))
                    {
                        FacMessageHelper.SendNotCollectionPageToFactory(rpaEx.ErrorCodeSuffixMessage);
                        return true;
                    }
                    else
                    {
                        logger.LogError(ex);
                        throw ex;
                    }
                }
                if (pageNum == 0 || previousDataList.Count == 0)
                {
                    return true;
                }
                //与上一次采集数据是否相同，相同认为已经是最后一页
                if (pageNum != 0)
                {
                    //判断本次数据与上一页数据是否完全相同
                    var isSame = IsSameList(previousDataList, collectDataList);
                    if (!isSame)
                    {
                        return true;
                    }
                    logger.LogDebug("两次采集数据完全相同，有可能是数据没有加载完，会再次尝试获取新的数据，最大耗时>=10s");
                }
                return false;
            }, 10000, 300);

            return collectDataList;
        }
        private static bool IsSameList(List<List<string>> list1, List<List<string>> list2)
        {
            if (list1.Count != list2.Count)
            {
                return false;
            }
            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i].Count != list2[i].Count)
                {
                    return false;
                }
                for (int j = 0; j < list1[i].Count; j++)
                {
                    if (!list1[i][j].Equals(list2[i][j]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        #endregion

        #region 一些辅助方法 如：获取新图片地址
        internal static string GetNewImgPath(params string[] dirs)
        {
            string fileName = Guid.NewGuid().ToString() + ".png";
            string dirPath = Path.Combine(dirs);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            string filePaht = Path.Combine(dirPath, fileName);
            return filePaht;
        }
        #endregion
    }
}


