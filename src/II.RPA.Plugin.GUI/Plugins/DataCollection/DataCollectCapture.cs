using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.DataCollection.V2.Windows;
using II.RPA.Plugin.GUI.DataCollection.V2;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.Core.Commons;

namespace II.RPA.Plugin.GUI.Plugins.DataCollection
{
    public class DataCollectCapture : ElementAbilityBase<ParamDataCollectCaptureModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.Capture;

        protected override void ValidateParameter(ParamDataCollectCaptureModel paramModel) {
            try
            {
                logger.LogDebug("Start DataCollectCapture");
                if (string.IsNullOrEmpty(paramModel.ActionCmd))
                {
                    paramModel.ActionCmd = "stop";
                }
                if (paramModel.PageInfo?.WindowType == null)
                {
                    paramModel.PageInfo = null;//首次拾取，页面信息必须为空
                }
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        protected override JMessage Execute(ParamDataCollectCaptureModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage captureMessage = new JMessage();
            logger.LogInformation($"Element当前应用域：{AppDomain.CurrentDomain.FriendlyName}");
            TimeTrackHelper timeTrackHelper = new TimeTrackHelper();
            timeTrackHelper.Start("DataCollectCapture", TimeTrackLogLevel.info);
            try
            {
                logger.LogDebug($"开始拾取：Capture.DataCollectCapture");
                GlobleConfigurations.CaptureUniqueID = RandomHelper.RandomString(10);
                if (paramModel.ActionCmd.EqualIgnoreCase("Start"))
                {
                    DataCollectingManagerV2.Instance.StartDataCollect(paramModel);
                }
                else
                {
                    DataCollectingManagerV2.Instance.StopDataCollect();
                }
            }
            catch (COMException ex)
            {
                logger.LogError(ex);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_ElementCaptureError, ref captureMessage);
            }
            finally
            {
                timeTrackHelper.Stop("DataCollectCapture", TimeTrackLogLevel.info);
            }
            return captureMessage;
        }


        protected override string HandlingErrorMessage(JMessage resultMsg)
        {
            //GuiElement.ChromeRelease();
            //GuiElement.overlayForm.CaptureOperationData.is_success = (resultMsg.Code == 0 ? 1 : 0);
            //resultMsg.Extend = GuiElement.overlayForm.CaptureOperationData.ToJson();
            return base.HandlingErrorMessage(resultMsg);
        }
    }
}

