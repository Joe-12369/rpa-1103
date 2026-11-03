using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Security.Policy;
using System.Threading;
using II.RPA.WebBrowser.Common.Basic.Params;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class Create : BrowserComponentBase<ParamCreateModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.Create;

        protected override JMessage Execute(ParamCreateModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                var @params = new BrowserParams()
                {
                    windowId = "",
                    tabId = "",
                    browserType = paramModel.browserType,
                    isBackend = paramModel.isBackend,
                    webData = paramModel.webData,
                    arguments = paramModel.arguments,
                    timeout= paramModel.RunParam.Timeout,
                };

                var browserAction = WebBrowserManager.GetBrowserAction(@params);
                if (browserAction == null)
                {
                    throw new RPAException(eErrorMessageType.Error_CreateFail);
                }
                bool isWaitingload = paramModel.waitingload == 1;
                bool isOpenNewTab = paramModel.openNewTab == 1;

                var param = new ParamCreateTabModel()
                {
                    url = paramModel.url,
                    CreateProcessArgs =  @params.arguments,
                    IsWaitingload = isWaitingload,
                    IsOpenNewTab = isOpenNewTab,
                    timeout = paramModel.timeout,
                    RunParam = paramModel.RunParam
                };

                msg.Info = browserAction.Create(param);
            }
            catch (RPAException RpaEx)
            {
                Exception(RpaEx, RpaEx.ErrorMessageType, ref msg);
                return msg;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_CreateFail, ref msg);
            }
            return msg;

        }

        protected override string HandlingErrorMessage(JMessage resultMsg)
        {
            resultMsg.AllowRetry = ((int)AllowRetryType.Allow).ToString();
            return base.HandlingErrorMessage(resultMsg);
        }

        protected override void ValidateParameter(ParamCreateModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.url))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"打开新的浏览器 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

