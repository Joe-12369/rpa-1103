using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;
using System.Threading;
using FlaUI.Core.Exceptions;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class SetElementCheck : GuiComponentBase<ParamSetElementCheckModel>
    {
        protected override eComponentType ComponentType => eComponentType.SetElementCheck;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_OperationCheck_OperationTomeout_Error;

        private static ILogger logger = Logger.OfContext();

        protected override void ValidateParameter(ParamSetElementCheckModel paramModel) {
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"SetElementCheck 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamSetElementCheckModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();

            {

                GuiElement element = null;
                try
                {
                    Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId },
                        { "ComponentType",this.ComponentType}
                    };
                    var param = new GetElementFromSelectorParams
                    {
                        Selector = paramModel.Selector,
                        SelectorType = paramModel.SelectorType,
                        ExtendParam = hashtable,
                        MatchType = paramModel.MatchType,
                        RunParam = paramModel.RunParam,
                        WindowInfo = paramModel.WindowInfo
                    };
                    element = GuiElement.FromSelector(param);

                }
                catch (Exception ex)
                {
                    Exception(ex, eErrorMessageType.Error_SetElementCheckError, ref msg);
                    return msg;
                }
                try
                {
                    if (element == null)
                    {
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                    }
                    NotifyCoreServerElementFound(paramModel.RunUniqueID);
                    try
                    {
                        element.SetElementCheck(paramModel.check);
                    }
                    catch (Exception ex)
                    {
                        string result = element.GetElementCheck().ToLower();

                        if ("true" == result && paramModel.check == 1)
                        {
                            logger.LogInformation("当前勾选框状态检查：true.无须额外操作");
                            throw ex;
                        }
                        else if ("true" != result && paramModel.check == 0)
                        {
                            logger.LogInformation("当前勾选框状态检查：false.无须额外操作");
                            throw ex;
                        }
                        else
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"{ex.GetType().FullName}");
                            if (ex.InnerException != null)
                            {
                                if (ex.InnerException is InvalidOperationException 
                                    || ex.InnerException is FlaUIException)
                                {
                                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex.InnerException, "InnerException");
                                    {
                                        element.MouseClick(0, 0, new UiElementClickParams
                                        {
                                            moveMouse = false,
                                            cursorPos = 0,
                                            operateType = 1
                                        });
                                        Thread.Sleep(100);
                                        string resultAfter = element.GetElementCheck().ToLower();

                                        if ("true" == resultAfter && paramModel.check == 1)
                                        {
                                            logger.LogInformation("使用模拟点击勾选成功");
                                        }
                                        else if ("true" != resultAfter && paramModel.check == 0)
                                        {
                                            logger.LogInformation("使用模拟点击取消勾选成功");
                                        }
                                        else
                                        {
                                            logger.LogWarning("使用模拟点击 操作失败");
                                            throw ex;
                                        }
                                    }
                                }
                                else
                                {
                                    throw ex;
                                }
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Exception(ex, eErrorMessageType.Error_SetElementCheckError, ref msg);
                }
                return msg;
            }

        }
    }
}

