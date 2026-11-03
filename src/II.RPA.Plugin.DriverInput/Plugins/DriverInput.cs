using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.DriverInput.HID;
using II.RPA.Plugin.DriverInput.Interceptor;
using II.RPA.Plugin.DriverInput.Params;
using II.RPA.Plugin.DriverInput.Plugins.PluginBase;
using II.RPA.Plugin.DriverInput.Utils;
using II.RPA.Plugin.DriverInput.WinRing0;
using II.RPA.WinAutomation.CommonUtility;
using InputInterceptorNS;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Device = System.Int32;

namespace II.RPA.Plugin.DriverInput.Plugins
{
    public class DriverInput : DriverInputComponentBase<ParamDriverInputModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.DriverInput;

        protected override JMessage Execute(ParamDriverInputModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();

            try
            {
                if (paramModel.InputMode == 1)//Interceptor
                {
                    //feature:ZOCO-10221 新版驱动输入集成

                    bool aiKeyboardInstall = false, interceptorInstall = false;

                    var result = AiKeyboard.ExecuteCMD("check", out bool success);
                    if (success && result?.LastOrDefault()?.Contains("\"status\":0") == true)
                    {
                        aiKeyboardInstall = true;
                    }

                    if (InputInterceptor.CheckDriverInstalled())
                    {
                        interceptorInstall = true;
                    }


                    try
                    {
                        //interceptor 输入
                        if (!paramModel.useAikeyboard)
                        {
                            if (interceptorInstall)
                            {
                                InputInterceptor.Initialize();
                                using (var hook = new KeyboardHook())
                                {
                                    if (!string.IsNullOrEmpty(paramModel.CompositeName))
                                    {
                                        Device device = InputInterceptor.SetRandomDevice(paramModel.CompositeName);
                                        if (device != -1)
                                        {
                                            hook.RandomDevice = device;
                                        }
                                    }
                                    hook.SimulateInput(paramModel.InputContent, (int)paramModel.InputInterval, paramModel.KeyDownDuration);
                                }
                                msg.Info = "true";
                                return msg;
                            }
                            else
                            {
                                //aiKeyboard 如果有安装，用AiKeyboard 进行输入
                                if (!aiKeyboardInstall)
                                {
                                    logger.LogError("Interceptor is not installed");
                                    Exception(new Exception("Interceptor is not installed"), eErrorMessageType.Error_InterceptorUnInstalled, ref msg);
                                }

                            }
                        }

                        //aiKeyboard 输入
                        if (!aiKeyboardInstall)
                        {
                            logger.LogError("AiKeyboard is not installed");
                            Exception(new Exception("AiKeyboard is not installed"), eErrorMessageType.Error_InterceptorUnInstalled, ref msg);
                        }

                        int interval = (int)paramModel.InputInterval;
                        var txt = paramModel.InputContent.Replace("\"", @"""""");
                        AiKeyboard.ExecuteCMD($"input --keys \"{txt}\" --interval {interval}", out bool sucess);
                        msg.Info = success.ToString();
                    }
                    finally
                    {
                        InputInterceptor.Dispose();
                    }
                }
                else if (paramModel.InputMode == 2)//HID
                {
                    HIDInputManager CHIDInputManager = new HIDInputManager();
                    CHIDInputManager.DriverInput(paramModel);
                    msg.Info = "true";
                }

            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "DriverInput Err");
                Exception(ex, eErrorMessageType.Error_ActiveWindow, ref msg);
            }

            return msg;
        }

        protected override void ValidateParameter(ParamDriverInputModel paramModel)
        {
            try
            {
                if (paramModel.InputMode == 1 || paramModel.InputMode == 2 && !string.IsNullOrEmpty(paramModel.InputContent))
                {
                    return;
                }
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "DriverInput Param Verrify Err");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

