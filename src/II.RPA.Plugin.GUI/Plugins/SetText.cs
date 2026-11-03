using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Plugin.Abstractions;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class SetText : GuiComponentBase<ParamSetTextModel>
    {
        private const int retryTimes = 3;
        private readonly IPluginInvoker pluginInvoker;

        public SetText(IPluginInvoker pluginInvoker)
        {
            this.pluginInvoker = pluginInvoker ?? throw new ArgumentNullException(nameof(pluginInvoker));
        }

        protected override eComponentType ComponentType => eComponentType.SetText;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_SetText_OperationTomeout_Error;


        private static ILogger logger = Logger.OfContext();

        protected override void ValidateParameter(ParamSetTextModel paramModel)
        {
            if (string.IsNullOrEmpty(paramModel.Selector))
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
            if (paramModel.txt == null)
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override void NormalizeParameter(ParamSetTextModel paramModel)
        {
            if (paramModel.isSimulate != 0) return;
            paramModel.ElementPosition = 0;
            paramModel.OffsetX = 0;
            paramModel.OffsetY = 0;
        }

        protected override async Task<JMessage> ExecuteAsync(ParamSetTextModel paramModel, PluginExecutionContext executionContext)
        {

            JMessage msg = new JMessage();
            await SetInputStateAsync(paramModel.inputState, executionContext).ConfigureAwait(false);
            GuiElement element = null;
            long operationTimeout = 0;
            try
            {
                Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };
                paramModel.cvExecutionServiceURL = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_EXECUTION_SERVICE;
                //element = GuiElement.FromSelector(selector, selectorType, false, paramModel.cvExecutionServiceURL, extendParam: hashtable);
                operationTimeout = Configurations.Info.OperationTimeout;
                if (paramModel.isSimulate == 1 || paramModel.isSimulate == 0)//非模拟（后台）输入，暂时也先加上，windows接口输入不彻底
                {
                    int overTimeUnit = paramModel.interval < 50 ? 50 : paramModel.interval;
                    operationTimeout += (paramModel.txt.Length + 4) * overTimeUnit * retryTimes;
                }
                operationTimeout += retryTimes * paramModel.FocusTimeout;
                paramModel.RunParam = new RunParamModel
                {
                    StartTimeStamp = paramModel.RunParam.StartTimeStamp,
                    RemainTime = paramModel.RunParam.RemainTime,
                    CurrentAttempt = paramModel.RunParam.CurrentAttempt,
                    TotalTime = (int)operationTimeout,
                    EnableAIRepair = paramModel.RunParam.EnableAIRepair,
                    EnableNormal = paramModel.RunParam.EnableNormal,
                };
                var param = new GetElementFromSelectorParams
                {
                    Selector = paramModel.Selector,
                    SelectorType = paramModel.SelectorType,
                    FindOnce = false,
                    CVExecutionServiceURL = paramModel.cvExecutionServiceURL,
                    ExtendParam = hashtable,
                    MatchType = paramModel.MatchType,
                    RunParam = paramModel.RunParam,
                    WindowInfo = paramModel.WindowInfo
                };
                element = GuiElement.FromSelector(param);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_NotFoundElement, ref msg);
                return msg;
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                if (paramModel.IsRandomLocationClick)
                {
                    logger.LogInformation("SetText：启用了模拟人工随机位置点击");
                    paramModel.isSimulate = 1;//模拟点击
                    if (paramModel.ComponentLabel != "输入密码")
                        paramModel.ElementPosition = 5;//随机位置
                }
                //var simulate = paramModel.isSimulate == 1 ? true : false;
                //element.SetText(paramModel.txt, paramModel.isReplace, simulate);
                logger.LogInformation($"回传coreserver超时时间:{operationTimeout}ms.");
                NotifyCoreServerElementFound(paramModel.RunUniqueID, operationTimeout);
                var result = SetTextInternal(element, paramModel, retryTimes);
                if (paramModel.isEnter == 1)
                {
                    logger.LogDebug($"Prepare to Enter.Delay {paramModel.delaybeforEnter}ms.");
                    Thread.Sleep(paramModel.delaybeforEnter);
                    AutoItHelper.Send("{enter}");
                    logger.LogDebug($"Enter done.");
                }
                if (paramModel.isVerify == 0)
                {
                    msg.Info = "";
                    return msg;
                }
                else
                {
                    msg.Info = result.ToString();
                    return msg;
                }
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_SetTextError, ref msg);
            }
            return msg;
        }


        private async Task SetInputStateAsync(string inputState, PluginExecutionContext executionContext)
        {
            if (!string.IsNullOrEmpty(inputState))
            {
                var parameter = new JObject { { "state", inputState } };
                await pluginInvoker.InvokeAsync(
                    new PluginRoute("DesktopInput", "SetState"),
                    parameter.ToString()).ConfigureAwait(false);
            }
        }
        private bool SetTextInternal(GuiElement anElement, ParamSetTextModel aParamModel, int aRetryTimes = 3)
        {
            var param = new Commons.Params.ParamSetTextModel()
            {
                text = aParamModel.txt,
                inputType = aParamModel.isSimulate,
                isReplace = aParamModel.isReplace,
                isVerify = aParamModel.isVerify,
                Retry = aParamModel.Retry,
                isEnter = aParamModel.isEnter,
                Trigger = aParamModel.Trigger,
                Interval = aParamModel.interval,
                delaybeforEnter = aParamModel.delaybeforEnter,
                ElementPosition = aParamModel.ElementPosition,
                OffsetX = aParamModel.OffsetX,
                OffsetY = aParamModel.OffsetY,
                FocusTimeout = aParamModel.FocusTimeout,
                ForceSwitchENG = aParamModel.forceSwitchENG
            };
            bool result = false;
            for (int i = 0; i < aRetryTimes; i++)
            {
                result = anElement.SetText(param);
                if (result)
                {
                    logger.LogInformation($"SetTextInternal result check success!");
                    break;
                }
                else if (param.isReplace != 0)
                {
                    logger.LogInformation($"SetTextInternal result check failed.isReplace：{param.isReplace}!");
                    break;
                }
                else
                {
                    if (param.inputType == 1 && param.isVerify == 1)
                    {
                        Thread.Sleep(100);
                        logger.LogInformation($"SetTextInternal result check failed:retry {i}");
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return result;
        }
    }
}

