using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;
using System.IO;


namespace II.RPA.Plugin.GUI.Plugins
{
    public class ElementScreenshot : GuiComponentBase<ParamElementScreenshotModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ElementScreenshot;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_ElementShot_OperationTomeout_Error;
        protected override void ValidateParameter(ParamElementScreenshotModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (string.IsNullOrEmpty(paramModel.savePath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "SetAttribute 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        /// <summary>
        /// 路径可能不存在 ，不存在直接创建
        /// </summary>
        /// <param name="picPath"></param>
        /// <returns></returns>
        /// <exception cref="RPAException"></exception>
        private string VerifyFilePath(string picPath)
        {
            try
            {
                string fileName = System.IO.Path.GetFileName(picPath);
                string fileDir = picPath.Replace(fileName, "");
                if (!Directory.Exists(fileDir))
                {
                    Directory.CreateDirectory(fileDir);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new RPAException(eErrorMessageType.Error_SavePathError);
            }
            return picPath;
        }
        protected override JMessage Execute(ParamElementScreenshotModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };
                paramModel.cvExecutionServiceURL = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_EXECUTION_SERVICE;
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
                var element = GuiElement.FromSelector(param);

                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                NotifyCoreServerElementFound(paramModel.RunUniqueID);

                VerifyFilePath(paramModel.savePath);
                if (!element.IsShow())
                {
                    logger.LogWarning("隐藏元素，截图失败");
                    throw new RPAException(eErrorMessageType.Error_ScreenshotError);
                }
                element.Screenshot(paramModel.savePath, paramModel.SelectorType, paramModel.isUseVerifyCode);

                msg.Info = paramModel.savePath;
            }
            catch (RPAException ex)
            {
                Exception(ex, ref msg);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_ScreenshotError, ref msg);
            }
            return msg;

        }
    }
}

