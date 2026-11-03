using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.Core.Commons;
using System.Drawing;
using System.Threading;
using System.Runtime.ExceptionServices;

namespace II.RPA.Plugin.GUI
{
    public class MouseClick : GuiComponentBase<ParamMouseClickModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ElementClick;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_ElementClick_OperationTomeout_Error;

        protected override void ValidateParameter(ParamMouseClickModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"MouseClick 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        private RPAMousePresses ConvertMousePresses(int value)
        {
            // 0 单击 1 双击  2三击 3按下 4弹起
            switch (value)
            {
                case 0:
                    return RPAMousePresses.Click;
                case 1:
                    return RPAMousePresses.DoubleClick;
                case 2:
                    return RPAMousePresses.ThreeClick;
                case 3:
                    return RPAMousePresses.Down;
                case 4:
                    return RPAMousePresses.Up;
                default:
                    return RPAMousePresses.Click;
            }
        }
        [HandleProcessCorruptedStateExceptions]
        protected override JMessage Execute(ParamMouseClickModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            logger.LogDebug($"获取到selectorType：{paramModel.SelectorType}");
            JMessage msg = new JMessage();
            GuiElement element = null;
            try
            {
                Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };
                paramModel.cvExecutionServiceURL = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_EXECUTION_SERVICE;
                //element = GuiElement.FromSelector(selector, selectorType, false, paramModel.cvExecutionServiceURL, extendParam: hashtable);

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
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement, eComponentType.ElementClick);
                }
                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                if (paramModel.IsEnableResultVerify)
                {
                    logger.LogInformation($"Enable action check");
                    try
                    {
                        ActionCheckInterop.remove_log_file(0);
                        var rect = UiCommon.UIAAutomation.GetDesktop().BoundingRectangle;
                        if (rect != Rectangle.Empty)
                        {
                            ActionCheckInterop.set_comp_rate(0.00001D);
                            ActionCheckInterop.set_time_min(300);
                            ActionCheckInterop.set_capture_rect(rect.X, rect.Y, rect.Width, rect.Height);
                            ActionCheckInterop.action_before();
                        }
                        else
                        {
                            logger.LogWarning($"桌面截图失败,忽略截图验证");
                        }
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Actioncheck error,ignore and continue...");
                    }
                }

                logger.LogDebug($"找到element,开始鼠标操作");
                UiElementClickParams clickParams = new UiElementClickParams();
                if (paramModel.IsRandomLocationClick)
                {
                    logger.LogInformation("MouseClick：启用了模拟人工随机位置点击");
                    paramModel.issimulate = 1;
                    paramModel.isUsePerson = 1;
                    paramModel.cursorPos = 5;//随机位置
                }
                clickParams.operateType = paramModel.issimulate;
                clickParams.cursorPos = paramModel.cursorPos;
                clickParams.isUsePerson = paramModel.isUsePerson == 1;
                clickParams.AidKeyType = paramModel.AidKeyType;

                #region [点击]
                clickParams.RPAMousePresses = ConvertMousePresses(paramModel.keyType);
                if (paramModel.keyType == MouseParamDef.CLK_TYPE_SINGLE && paramModel.clickType == MouseParamDef.CLK_BTN_LEFT)
                {
                    element.MouseClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                }
                else if (paramModel.keyType == MouseParamDef.CLK_TYPE_SINGLE && paramModel.clickType == MouseParamDef.CLK_BTN_RIGHT)
                {
                    element.MouseRightClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                }
                else if (paramModel.keyType == MouseParamDef.CLK_TYPE_SINGLE && paramModel.clickType == MouseParamDef.CLK_BTN_CENTER)
                {
                    element.MouseConterClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                }
                else if (paramModel.keyType == 1 && paramModel.clickType == MouseParamDef.CLK_BTN_LEFT)
                {
                    element.MouseDoubleClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                }
                else if (paramModel.keyType == 1 && paramModel.clickType == MouseParamDef.CLK_BTN_RIGHT)
                {
                    element.MouseRightDoubleClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                }
                else if (paramModel.keyType == 1 && paramModel.clickType == MouseParamDef.CLK_BTN_CENTER)
                {
                    element.MouseConterDoubleClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                }
                //三击
                else if (paramModel.keyType == MouseParamDef.CLK_TYPE_TRIPLE && paramModel.clickType == MouseParamDef.CLK_BTN_LEFT)
                {
                    element.MouseThreeClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                }
                else if (paramModel.keyType == MouseParamDef.CLK_TYPE_TRIPLE && paramModel.clickType == MouseParamDef.CLK_BTN_RIGHT)
                {
                    element.MouseRightThreeClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                }
                else if (paramModel.keyType == MouseParamDef.CLK_TYPE_TRIPLE && paramModel.clickType == MouseParamDef.CLK_BTN_CENTER)
                {
                    element.MouseConterThreeClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                }
                else if (paramModel.keyType == MouseParamDef.CLK_TYPE_PRESS)
                {
                    if (paramModel.clickType == MouseParamDef.CLK_BTN_LEFT)
                        element.MouseClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                    else if (paramModel.clickType == MouseParamDef.CLK_BTN_RIGHT)
                        element.MouseRightClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                    else if (paramModel.clickType == MouseParamDef.CLK_BTN_CENTER)
                        element.MouseConterClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                }
                else if (paramModel.keyType == MouseParamDef.CLK_TYPE_RELEASE)
                {
                    if (paramModel.clickType == MouseParamDef.CLK_BTN_LEFT)
                        element.MouseClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                    else if (paramModel.clickType == MouseParamDef.CLK_BTN_RIGHT)
                        element.MouseRightClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                    else if (paramModel.clickType == MouseParamDef.CLK_BTN_CENTER)
                        element.MouseConterClick(paramModel.offsetX, paramModel.offsetY, clickParams);
                }
                #endregion
                logger.LogDebug($"鼠标点击操作结束");

                if (paramModel.IsEnableResultVerify)
                {
                    var ret = ActionCheckAfter();
                    if (!ret)
                    {
                        logger.LogWarning($"Actioncheck fail:{ret}");
                    }
                    else
                    {
                        logger.LogInformation($"Actioncheck success:{ret}");
                    }
                }

                return msg;
            }
            catch (Exception e)
            {
                Exception(e, eErrorMessageType.Error_ClickError, ref msg);
                return msg;
            }

        }
        [HandleProcessCorruptedStateExceptions]

        private bool ActionCheckAfter()
        {
            try
            {
                for (int i = 1; i <= 5; i++)
                {
                    Thread.Sleep(50);
                    var ret = ActionCheckInterop.action_after();
                    if (ret == 0)
                    {
                        logger.LogWarning($"Actioncheck result fail:{ret},Start retry:{i}");
                        continue;
                    }
                    else
                    {
                        logger.LogWarning($"Actioncheck result success:{ret}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"ActionCheckAfter:{ex.Message}");
            }
            return false;
        }
    }
}

