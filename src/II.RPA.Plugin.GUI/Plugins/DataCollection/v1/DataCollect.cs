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
using II.RPA.Plugin.Abstractions;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class DataCollect : AsyncElementAbilityBase<ParamDataCollectModel>
    {
        private static ILogger logger = Logger.OfContext();
        private readonly IPluginInvoker pluginInvoker;

        public DataCollect(IPluginInvoker pluginInvoker)
        {
            this.pluginInvoker = pluginInvoker ?? throw new ArgumentNullException(nameof(pluginInvoker));
        }

        protected override eComponentType ComponentType => eComponentType.DataCollect;
        protected override void ValidateParameter(ParamDataCollectModel paramModel) {
            try
            {
                logger.LogDebug("Start DataCollect");
                //对垂直滚动距离参数做兼容处理
                if (paramModel.ScrollVertical <= 0 && paramModel.ScrollScreenNum > 0)
                {
                    int screenHeight = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
                    try
                    {
                        paramModel.ScrollVertical = paramModel.ScrollScreenNum * screenHeight;
                    }
                    catch
                    {
                        logger.LogWarning("滚屏数转像素时发生了异常，使用默认值999999999");
                        paramModel.ScrollVertical = 999999999;
                    }
                }

                //if (string.IsNullOrEmpty(paramModel.filepath))
                //{
                //    throw new RPAException(eErrorMessageType.Error_ParamsError, "缺少文件地址参数");
                //}
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override async Task<JMessage> ExecuteAsync(ParamDataCollectModel paramModel, PluginExecutionContext executionContext)
        {
            JMessage msg = await ActiveInterfaceAsync(paramModel, executionContext).ConfigureAwait(false);
            if (msg != null)
            {
                return msg;
            }
            msg = new JMessage();
            List<string> dataFileList = new List<string>();

            try
            {
                RetDataCollectConfigurationModel dataCollectConfigurationModel = paramModel.Selector.ToObject<RetDataCollectConfigurationModel>();
                //dataCollectConfigurationModel.turnPageButtonInfo.pageCount = 1;
                if (string.IsNullOrEmpty(dataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelectorVersion))
                {
                    dataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelectorVersion = "1.0";//655添加此逻辑。版本号为数据采集上线后添加，6.1.0至 655版本之后  版本号全是1.0，
                }
                var paramCollectDataBySelector = new Commons.Params.ParamCollectDataBySelectorModel()
                {
                    WedData = paramModel.WebData,
                    NextPageBtnSelectorVersion = dataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelectorVersion,
                    NextPageBtnSelector = dataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelector,
                    Delay = paramModel.Delay,
                    ScrollVertical = paramModel.ScrollVertical,
                    //PageCount = dataCollectConfigurationModel.turnPageButtonInfo.pageCount,
                    PageCount = paramModel.PageCount,
                    DataFilePath = paramModel.DataFilePath,
                    //TurnPageConfigType = dataCollectConfigurationModel.turnPageButtonInfo.turnPageConfigType,
                    TurnPageConfigType = paramModel.TurnPageConfigType,
                    IsTable = (dataCollectConfigurationModel.collectInfo.type == "table"),
                    ScrollDelay = paramModel.ScrollDelay
                };
                var colnumCollectDataList = from item in dataCollectConfigurationModel.collectInfo.data
                                            select new CollectData_SelectorData
                                            {
                                                Title = item.title,
                                                Version = string.IsNullOrEmpty(item.version) ? "1.0" : item.version,
                                                Selector = item.selector,
                                                CollectType = item.dataType,
                                                Regular = item.regular,
                                                CommonTalbleTitleList = item.CommonTalbleTitleList
                                            };
                paramCollectDataBySelector.SelectorDataList.AddRange(colnumCollectDataList);
                if (string.IsNullOrEmpty(paramModel.WebData))
                {
                    JObject webData = new JObject() { { "type", dataCollectConfigurationModel.pageInfo.guiType } };
                    paramCollectDataBySelector.WedData = webData.ToString();
                }
                List<List<string>> _CollectData = await DataCollectingManager.CollectDataBySelectorAsync(
                    paramCollectDataBySelector,
                    dataFileList,
                    pluginInvoker,
                    executionContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_DataCollection, ref msg);
            }
            msg.Info = dataFileList.ToJson();
            logger.LogDebug($"数据采集结果:{msg.Info}");
            return msg;
        }

        private async Task<JMessage> ActiveInterfaceAsync(ParamDataCollectModel paramModel, PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                if (paramModel.CollectType != "software")
                {
                    var webDataObj = JObject.Parse(paramModel.WebData);
                    var browserType = webDataObj.Value<string>("type");
                    var webData = webDataObj.Value<string>(browserType);
                    JObject pJObj = new JObject() {
                                { "webData", webData },
                                { "browserType", browserType }
                            };
                    string result = await pluginInvoker.InvokeAsync(
                        new PluginRoute("WebBrowser", "Active"),
                        pJObj.ToString()).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(result))
                    {
                        var rpaEx = new RPAException(eErrorMessageType.Error_WindowActiveError);
                        Exception(eErrorMessageType.Error_ElementCaptureError, ref msg);
                        return msg;
                    }
                    msg = result.ToObject<JMessage>();
                    if (msg.Code != 0)
                    {
                        return msg;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError("数据采集激活浏览器异常");
                Exception(ex, eErrorMessageType.Error_ElementCaptureError, ref msg);
                return msg;
            }
        }

    }
}

