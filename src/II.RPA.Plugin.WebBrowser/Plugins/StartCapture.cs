using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class StartCapture : BrowserComponentBase<ParamStartCaptureModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.DataCapture;

        protected override JMessage Execute(ParamStartCaptureModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {

                var @params = new BrowserParams()
                {
                    windowId = "",
                    tabId = "",
                    browserType = paramModel.browserType,
                };
                var browserAction = WebBrowserManager.GetBrowserAction(@params);
                msg.Info = browserAction.StartCapture(paramModel.jsPath, paramModel.config, paramModel.url);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_DataCaptureError, ref msg);
            }
            return msg;

        }

        protected override void ValidateParameter(ParamStartCaptureModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.jsPath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                //if (string.IsNullOrEmpty(paramModel.saveFile))
                //{
                //    throw new RPAException(eErrorMessageType.Error_ParamsError);
                //}
                //if (string.IsNullOrEmpty(paramModel.fileName))
                //{
                //    throw new RPAException(eErrorMessageType.Error_ParamsError);
                //}
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"批量数据采集 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

