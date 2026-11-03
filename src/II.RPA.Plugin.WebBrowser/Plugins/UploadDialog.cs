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
    public class UploadDialog : BrowserComponentBase<ParamUploadDialogModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.FileUploadDialog;

        protected override JMessage Execute(ParamUploadDialogModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                browserAction.UploadDialogExcute(new GUI.Commons.Params.ParamFileUploaderModel()
                {
                    buttonType = paramModel.buttonType,
                    inputMode = paramModel.inputMode,
                    interval = paramModel.interval,
                    waitDialogTime = paramModel.waitDialogTime,
                    uploadType = (GUI.Commons.Params.UploadType)Enum.ToObject(typeof(GUI.Commons.Params.UploadType), (int)paramModel.uploadType),
                    filePath = paramModel.filePath
                });
                return msg;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_FileUploadFail, ref msg);
                msg.AllowRetry = ((int)AllowRetryType.NotAllow).ToString();
            }
            return msg;
        }

        protected override void ValidateParameter(ParamUploadDialogModel paramModel)
        {
            try
            {
                return;
            }
            catch (RPAException rpaEx)
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"上传 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

