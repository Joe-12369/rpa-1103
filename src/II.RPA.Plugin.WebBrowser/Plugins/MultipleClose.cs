using II.RPA.Plugin.Core;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using II.RPA.Plugin.WebBrowser.Params.TabInfo;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class MultipleClose : BrowserComponentBase<ParamMultipleCloseModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.Close;

        protected override JMessage Execute(ParamMultipleCloseModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                var @params = new BrowserParams()
                {
                    windowId = "",
                    tabId = "",
                    browserType = paramModel.browserType,
                    webData = paramModel.webDatas?.FirstOrDefault(),
                };
                var browserAction = WebBrowserManager.GetBrowserAction(@params);
                switch (paramModel.closeMode)
                {
                    case "select":
                        var tabInfos = paramModel.webDatas.Select((tabInfo) => { return JsonConvert.DeserializeObject<TabInfo>(tabInfo); }).ToList();
                        var tabIds = tabInfos.Select((tabinfo) => { return tabinfo.Id; }).ToList();
                        browserAction.CloseList(tabIds, paramModel.IsCloseAlert);
                        break;
                    case "active":
                        var tab = browserAction.Browser.AcquireCurrentActiveTab();
                        tab.Close(paramModel.IsCloseAlert);
                        break;
                    case "browser":
                        browserAction.CloseAll(paramModel.KillProcess == 1, isCloseAlert: paramModel.IsCloseAlert);
                        break;
                    case "other":
                        browserAction.CloseOther(paramModel.IsCloseAlert);
                        break;
                    case "right":
                        browserAction.CloseRight(paramModel.IsCloseAlert);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                if (ex.IsRPAException(eErrorMessageType.Error_PageNotFound, out _))
                {
                    logger.LogInformation($"未匹配到网页对象对应的tab信息，网页可能已被关闭");
                }
                else
                {
                    Exception(ex, eErrorMessageType.Error_CloseFail, ref msg);
                }
            }
            return msg;

        }

        protected override void ValidateParameter(ParamMultipleCloseModel paramModel)
        {
            try
            {
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"关闭网页 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }

}

