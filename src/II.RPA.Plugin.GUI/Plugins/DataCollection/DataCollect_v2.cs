using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.DataCollection.V2;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using System.IO;
using II.RPA.Plugin.GUI.Plugins.Chatrpa;
using II.RPA.Plugin.Abstractions;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class DataCollect_v2 : AsyncElementAbilityBase<ParamDataCollect_V2Model>
    {
        private static ILogger logger = Logger.OfContext();
        private readonly IPluginInvoker pluginInvoker;

        public DataCollect_v2(IPluginInvoker pluginInvoker)
        {
            this.pluginInvoker = pluginInvoker ?? throw new ArgumentNullException(nameof(pluginInvoker));
        }
        protected override eComponentType ComponentType => eComponentType.DataCollect;
        protected override void ValidateParameter(ParamDataCollect_V2Model paramModel) {
            try
            {

                //paramModel.ToJson() = File.ReadAllText(@"D:\07.testParams\datacollect.json");
                logger.LogDebug("Start DataCollect");
                //设置默认翻页数
                if ((paramModel.TurnPageCount == 0 && paramModel.CollectDataCount <= 0)//按页采集 所有页 
                    || (paramModel.CollectDataCount > 0 && paramModel.HasTurningPageBtn)) //按条采集 有翻页按钮
                {
                    paramModel.TurnPageCount = Int32.MaxValue;
                }
                else if (paramModel.CollectDataCount > 0 && paramModel.TurnPageCount == 0) //按条采集 未指定页数（按条默认页数值固定0，写上便于理解）
                {
                    paramModel.TurnPageCount = 1;
                }
                if (paramModel.DataCollectConfig.TurningPageConfig.BtnType == TurningBtnType.LoadMoreBtn)
                {
                    paramModel.DeduplicateType = DeduplicateType.Deduplicate;
                }
                LocateDataAnalysis(paramModel);
                //设置GuiType
                if (paramModel.IsWeb)
                {
                    if (paramModel.WindowInfo == null)
                    {
                        //paramModel.WindowInfo = BrowserWindowProvider.GetFirstBrowserByZOrder();
                        paramModel.WindowInfo = BrowserWindowProvider.AutoMatchBrowserWindowBeforeExecut(
                            paramModel.DataCollectConfig.PageInfo.ExtendedTags.HasFlag(ExtendedTags.EmbeddedIE) ? "ie" : "",
                            paramModel.DataCollectConfig.PageInfo.ProcessName,
                            paramModel.DataCollectConfig.PageInfo.ExtendedTags
                            );
                    }
                    else if (!(paramModel.WindowInfo is BrowserWindow))
                    {
                        var browserWindow = BrowserWindowProvider.MatchBrowserWindowByHandle(paramModel.WindowInfo.Handle);
                        if (browserWindow == null)
                        {
                            throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                        }
                        browserWindow.SpecifiedWindow = true;
                        paramModel.WindowInfo = browserWindow;
                    }
                    paramModel.GuiType = paramModel.WindowInfo?.GuiType.ToString() ?? string.Empty;
                }
                else
                {
                    paramModel.GuiType = paramModel.DataCollectConfig.PageInfo.WindowType;
                }
                //增加默认去换行数据处理配置
                paramModel.DataCollectConfig.CollectionConfig.ColumnConfigArray?.ForEach((item) =>
                {
                    //item.DataProcessConfig.ForEach(config => config.Parameters = config.ParameterObjects?.Select(p => p.Val).ToArray());
                    item.FilterConfig.ForEach(config => config.Parameters = config.ParameterObjects?.Select(p => p.Val).ToList());
                    item.DataProcessConfig.Add(new ProcessingConfig
                    {
                        ProcessType = ProcessingType.RemoveLineBreaks,
                        Parameters = new object[] { }
                    });
                });
                paramModel.DataCollectConfig.CollectionConfig.MergeConfigMap?.ForEach((item) =>
                {
                    item.Value.FilterConfig.ForEach(config => config.Parameters = config.ParameterObjects?.Select(p => p.Val).ToList());
                });
                //设置默认垂直滚动时长（等待数据加载时长）
                if (paramModel.IsPreview)
                {
                    paramModel.DataCollectConfig.CollectionConfig.ScrollVerticalDelay = 0;
                }
                else if (paramModel.DataCollectConfig.CollectionConfig.ScrollVerticalDelay <= -1)
                {
                    paramModel.DataCollectConfig.CollectionConfig.ScrollVerticalDelay = Configurations.Info.ScrollVerticalDelayV2;
                }
                //预览数据按行设置标色时，表格的rowindex需要+1，
                if (paramModel.IsTable)
                {
                    if (paramModel.ColorGuidanceConfig?.RowIndex >= 0)
                    {
                        paramModel.ColorGuidanceConfig.RowIndex++;
                    }
                    if (paramModel.ColorGuidanceConfig?.rows?.Count > 0)
                    {
                        paramModel.ColorGuidanceConfig.rows = paramModel.ColorGuidanceConfig.rows.Select(r => r + 1).ToList();
                    }
                }
                //增加默认版本号
                if (string.IsNullOrEmpty(paramModel.DataCollectConfig.CollectionConfig.SelectorVersion))
                {
                    paramModel.DataCollectConfig.CollectionConfig.SelectorVersion = GlobleConfigurations.CurrentSelectorVersion;
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        public void LocateDataAnalysis(ParamDataCollect_V2Model paramModel)
        {
            if (paramModel.LocateData == null)
            {
                return;
            }
            if (paramModel.LocateData.TryGetValue("type", out string type) && paramModel.LocateData.TryGetValue(type, out string webData))
            {
                JObject webDataObj = JObject.Parse(webData);
                var tabId = webDataObj.Value<int>("id");
                var mainWindowHandle = new IntPtr(webDataObj.Value<long>("mainWindowHandle")); ;
                var webWindowInfo = new BrowserWindow(mainWindowHandle);
                webWindowInfo.SpecifiedTabId = tabId;
                if (!Enum.TryParse(type, true, out Commons.WindowMatch.WindowType wndType))
                {
                    wndType = Commons.WindowMatch.WindowType.Chromium;
                }
                webWindowInfo.WindowType = wndType;
                webWindowInfo.TabDetailJson = webData;
                if (BrowserWindowProvider.BrowserWindowTypeCheck(webWindowInfo))
                {
                    paramModel.WindowInfo = webWindowInfo;
                }
                else
                {
                    throw new RPAException(eErrorMessageType.Error_BrowserWindowTypeCheck_Error);
                }
            }
            else if (paramModel.LocateData.TryGetValue("windowHandle", out long windowHandle))
            {
                paramModel.WindowInfo = new WindowObject(new IntPtr(windowHandle));
            }
        }
        protected override async Task<JMessage> ExecuteAsync(ParamDataCollect_V2Model paramModel, PluginExecutionContext executionContext)
        {
            DataCollectingManagerV2.Instance.ExecutingDataCollectionApi = true;
            JMessage msg = new JMessage();

            msg = new JMessage();
            List<string> dataFileList = new List<string>();

            try
            {
                if (paramModel.IsRandomLocationClick)
                {
                    logger.LogInformation("DataCollect_v2：启用了模拟人工随机位置点击");
                    if (paramModel.DataCollectConfig != null && paramModel.DataCollectConfig.TurningPageConfig != null)
                        paramModel.DataCollectConfig.TurningPageConfig.SimulateClick = 1;
                }
                bool isWaterfallFlowLoading = paramModel.IsWeb && !paramModel.HasTurningPageBtn && paramModel.CollectDataCount > 0;

                if (isWaterfallFlowLoading)
                {
                    logger.LogDebug("DataCollect_v2：DataCollectingManagerV2 CollectWaterfallData");
                    List<List<string>> _CollectData = DataCollectingManagerV2.CollectWaterfallData(paramModel, ref dataFileList);
                }
                else
                {
                    logger.LogDebug("DataCollect_v2：DataCollectingManagerV2 CollectData");
                    List<List<string>> _CollectData = await DataCollectingManagerV2.CollectDataAsync(
                        paramModel,
                        dataFileList,
                        pluginInvoker,
                        executionContext).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (ex.IsRPAException(eErrorMessageType.Error_EmbeddedIEMode, out _))
                {
                    paramModel.GuiType = eGuiType.IE.ToString();
                    return await ExecuteAsync(paramModel, executionContext).ConfigureAwait(false);
                }
                else
                {
                    Exception(ex, eErrorMessageType.Error_DataCollection, ref msg);
                }
            }
            msg.Info = dataFileList.ToJson();
            logger.LogDebug($"数据采集结果:{msg.Info}");
            return msg;
        }
        protected override string HandlingErrorMessage(JMessage resultMsg)
        {
            DataCollectingManagerV2.Instance.ExecutingDataCollectionApi = false;
            return base.HandlingErrorMessage(resultMsg);
        }
    }
}

