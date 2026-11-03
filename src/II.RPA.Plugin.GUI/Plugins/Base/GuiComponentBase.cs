using FlaUI.Core;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.MatchBrowser;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using Shell32;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.PluginBase;

namespace II.RPA.Plugin.GUI
{
    public abstract class GuiComponentBase<TParam> : AsyncPluginAbilityBase<TParam> where TParam : ParamBase, new()
    {

        protected virtual eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_None;

        private static ILogger logger = Logger.OfContext();
        protected sealed override async Task<JMessage> PrepareExecutionAsync(TParam paramModel, PluginExecutionContext context)
        {
            NativeMethods.SetProcessDPIAware();
            logger.LogInformation($"jsonParam：{context.JsonParam}");
            var guiPreparationResult = await PrepareGuiExecutionAsync(paramModel, context).ConfigureAwait(false);
            if (guiPreparationResult != null)
            {
                return guiPreparationResult;
            }
            return await PrepareAbilityExecutionAsync(paramModel, context).ConfigureAwait(false);
        }

        protected virtual async Task<JMessage> PrepareGuiExecutionAsync(TParam paramModel, PluginExecutionContext context)
        {
            this.GetSelectorInfo(paramModel);
            this.LocateDataParsing(paramModel);
            //网页类型元素
            if (paramModel.IsWebSelectorType)
            {
                this.LocateBrowserWindowInfo(paramModel);
            }
            //非网页类型元素、关联网页对象,需要激活网页
            else if (paramModel.WindowInfo != null && paramModel.WindowInfo is BrowserWindow browserWindow)
            {
                var message = await PluginActionHelper.WebBrowserActiveAsync(
                    GuiManager.PluginInvoker,
                    browserWindow.WindowType.ToString(),
                    browserWindow.TabDetailJson).ConfigureAwait(false);
                if (message.Code != 0)
                {
                    return message;
                }
            }
            return null;
        }

        protected virtual Task<JMessage> PrepareAbilityExecutionAsync(TParam paramModel, PluginExecutionContext context)
        {
            return Task.FromResult<JMessage>(null);
        }

        protected virtual JMessage Execute(TParam paramModel, PluginExecutionContext executionContext)
        {
            throw new NotSupportedException($"{GetType().FullName} 未实现 Execute 或 ExecuteAsync。");
        }

        protected override Task<JMessage> ExecuteAsync(TParam paramModel, PluginExecutionContext executionContext)
        {
            return Task.FromResult(Execute(paramModel, executionContext));
        }


        private string GetExecutrSelector(string selector)
        {
            selector = RecoverViewData.RestoreViewData(selector);
            var ja = JArray.Parse(selector);

            //var j = ja.FirstOrDefault();
            ((JObject)ja[0]).Remove("remoteName");
            ((JObject)ja[0]).Remove("remoteAddress");
            return ExtractViewData.VisualViewData(ja.ToString());
        }
        protected override string HandlingResultMessage(TParam paramModel, JMessage resultMsg)
        {
            try
            {
                if (paramModel != null)
                {
                    var aiBuriedKey = $"{paramModel.RunParam.StartTimeStamp}{paramModel.RunParam.CurrentAttempt}";
                    GlobleConfigurations.AiBuriedDataCacheDic.TryGetValue(aiBuriedKey, out string aiBuriedData);
                    resultMsg.AiBuriedData = aiBuriedData;
                }
            }
            catch
            {
            }
            return HandlingErrorMessage(resultMsg);
        }
        protected override string HandlingErrorMessage(JMessage resultMsg)
        {
            if (CacheRequest.IsCachingActive)
            {
                CacheRequest.Pop();
            }
            return base.HandlingErrorMessage(resultMsg);
        }


        protected override void Exception(Exception ex, eGuiType guiType, eErrorMessageType errorMsgType, ref JMessage msg)
        {
            if (ex != null && ex is RPAException)
            {
                var rpaex = (RPAException)ex;
                if (rpaex.ErrorMessageType == eErrorMessageType.Error_NotFoundElement)
                {
                    //rpaex.ErrorMessageType =;
                }
            }
            base.Exception(ex, guiType, errorMsgType, ref msg);
        }



        protected GuiElement GetGuiElement(Func<GuiElement> fromSlectorFunc, string runUniqueID, int newTimeout = 0)
        {
            var guiElement = fromSlectorFunc.Invoke();
            if (guiElement != null)
            {
                NotifyCoreServerElementFound(runUniqueID, newTimeout);
            }
            return guiElement;
        }
        protected void NotifyCoreServerElementFound(string runUniqueID, long newTimeout = -1)
        {
            if (string.IsNullOrEmpty(runUniqueID))
            {
                return;
            }
            logger.LogDebug($"ResetPluginLoaderRunTimeout");
            if (newTimeout <= 0)
            {
                newTimeout = Configurations.Info.OperationTimeout;
            }
            var sendRet = MessageService.SendMessageToCoeServer(
                SendMessageToCoeServerAction.ResetPluginLoaderRunTimeout,
                new ResetPluginLoaderRunTimeoutPayload()
                {
                    RunUniqueID = runUniqueID ?? string.Empty,
                    NewTimeout = newTimeout,
                    TimeoutMsg = OperationTimeoutMsgType.GetMessage()
                });
            logger.LogDebug($"ResetPluginLoaderRunTimeout sendRet :{sendRet}");
        }

        /// <summary>
        /// 获取selector信息，如提供浏览器类型，
        /// </summary>
        /// <param name="paramModel"></param>
        /// <returns></returns>
        protected string GetSelectorInfo(ParamBase paramModel)
        {
            /*logs:
            -- 680临时废掉元素的浏览器类型属性（browserType），改用自动匹配浏览器
            -- 696(700)版本使用WebWindowInfo 传递使用窗口信息
             */
            string selector = CommonParseSelector.ParseSelectorV1(paramModel.Selector, out var selectorType, out var version, out var processName, out var extendedTags, out string discernHidden, out CommonProps commonProps);
            //if (!string.IsNullOrEmpty(version) && RPAUtils.CompareVersionNumber(version, GlobleConfigurations.CurrentSelectorVersion) > 0)
            //{
            //    throw new RPAException(eErrorMessageType.Error_SelectorVersion_NotSupported);
            //}
            paramModel.Selector = selector;
            paramModel.SelectorVersion = version;
            paramModel.SelectorProcessName = processName;
            if (!string.IsNullOrEmpty(selectorType))
                paramModel.SelectorGuiType = selectorType;
            paramModel.ExtendedTags = extendedTags;
            paramModel.DiscernHidden = discernHidden.Equals("true");
            if (paramModel.ExtendedTags.HasFlag(ExtendedTags.PinPath) && paramModel.LocateData == null)
            {
                paramModel.LocateData = ImitateLocateData(commonProps.PinPath);
            }
            return selector;
        }

        private JObject ImitateLocateData(PinPath pinPath)
        {
            JObject webData = new JObject {
                { "id",pinPath.TabId},
                { "mainWindowHandle",pinPath.MainWindowHandle}
            };

            JObject locateData = new JObject
            {
                { "type", pinPath.GuiType.ToString() },
                {pinPath.GuiType.ToString(), webData.ToString()}
            };
            return locateData;
        }
        /// <summary>
        /// 定位数据处理
        /// </summary>
        /// <param name="paramModel"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected ParamBase LocateDataParsing(ParamBase paramModel)
        {
            if (paramModel.LocateData == null)
            {
                return paramModel;
            }
            if (paramModel.LocateData.TryGetValue("type", out string type) && paramModel.LocateData.TryGetValue(type, out string webData))
            {
                JObject webDataObj = JObject.Parse(webData);
                var tabId = webDataObj.Value<int>("id");
                var mainWindowHandle = new IntPtr(webDataObj.Value<long>("mainWindowHandle"));
                var webWindowInfo = new BrowserWindow(mainWindowHandle);
                webWindowInfo.SpecifiedTabId = tabId;

                if (!Enum.TryParse(type, true, out Commons.WindowMatch.WindowType wndType))
                {
                    wndType = Commons.WindowMatch.WindowType.Chromium;
                }
                if (string.IsNullOrEmpty(paramModel.SelectorGuiType))
                {
                    paramModel.SelectorGuiType = wndType.ToString();
                }
                webWindowInfo.WindowType = wndType;
                webWindowInfo.TabDetailJson = webData;
                if (BrowserWindowProvider.BrowserWindowTypeCheck(webWindowInfo))
                {
                    paramModel.WindowInfo = webWindowInfo;
                }
                else
                {
                    throw new RPAException(eErrorMessageType.Error_BrowserWindowTypeCheck_Error);
                }
            }
            else if (paramModel.LocateData.TryGetValue("windowHandle", out int windowHandle))
            {
                paramModel.WindowInfo = new WindowObject((IntPtr)windowHandle);
            }

            return paramModel;
        }
        /// <summary>
        /// 定位浏览器窗口信息
        /// </summary>
        /// <param name="paramModel"></param>
        /// <returns></returns>
        /// <exception cref="RPAException"></exception>
        protected ParamBase LocateBrowserWindowInfo(ParamBase paramModel)
        {
            if (paramModel.WindowInfo == null)
            {
                paramModel.WindowInfo = BrowserWindowProvider.AutoMatchBrowserWindowBeforeExecut(paramModel.SelectorGuiType, paramModel.SelectorProcessName, paramModel.ExtendedTags);
            }
            else if (!(paramModel.WindowInfo is BrowserWindow))//关联 非网页对象
            {
                var browserWindowNew = BrowserWindowProvider.MatchBrowserWindowByHandle(paramModel.WindowInfo.Handle, false);
                if (browserWindowNew == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                browserWindowNew.SpecifiedWindow = true;
                paramModel.WindowInfo = browserWindowNew;
            }
            return paramModel;
        }
    }
}
