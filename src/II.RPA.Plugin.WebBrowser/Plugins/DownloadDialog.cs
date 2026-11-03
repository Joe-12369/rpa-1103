using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;
using System.Management;
using System.Text.RegularExpressions;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class DownloadDialog : BrowserComponentBase<ParamDownloadDialogModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.DownloadDialog;

        protected override JMessage Execute(ParamDownloadDialogModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                var @params = new BrowserParams()
                {
                    windowId = "",
                    tabId = "",
                    browserType = paramModel.browserType,
                    webData = paramModel.webData,
                };
                var browserAction = WebBrowserManager.GetBrowserAction(@params);
                msg.Info = browserAction.DownloadDialogExcute(new GUI.Commons.Params.ParamDownloaderModel()
                {
                    saveFile = paramModel.saveFile,
                    fileName = paramModel.fileName,
                    saveMode = paramModel.saveMode,
                    clickMode = paramModel.clickMode,
                    inputMode = paramModel.inputMode,
                    interval = paramModel.interval,
                    timeout = paramModel.timeout,
                    waitDialogTime = paramModel.waitDialogTime,
                    buttonType = paramModel.buttonType
                });
                return msg;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_DownloadFail, ref msg);
                msg.AllowRetry = ((int)AllowRetryType.NotAllow).ToString();
            }
            return msg;
        }

        protected override void ValidateParameter(ParamDownloadDialogModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.saveFile))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }

                return;
            }
            catch (RPAException rpaEx)
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"下载 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

