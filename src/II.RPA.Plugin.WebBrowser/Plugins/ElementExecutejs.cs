using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.Abstractions;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public class ElementExecutejs : BrowserComponentBase<ParamElementExecutejsModel>
    {
        private readonly IPluginInvoker pluginInvoker;

        public ElementExecutejs(IPluginInvoker pluginInvoker)
        {
            this.pluginInvoker = pluginInvoker ?? throw new ArgumentNullException(nameof(pluginInvoker));
        }

        protected override eComponentType ComponentType => eComponentType.ElementExecutejs;

        /// <summary>
        /// 功能已经在Element中重新实现，但执行接口未变，执行跨依赖包功能跳转
        /// </summary>
        /// <param name="action"></param>
        /// <param name="jsonParam"></param>
        /// <returns></returns>
        protected override async Task<JMessage> ExecuteAsync(ParamElementExecutejsModel parameter, PluginExecutionContext context)
        {
            var result = await pluginInvoker.InvokeAsync(
                new PluginRoute("Element", "ElementExecutejs"),
                context.JsonParam).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<JMessage>(result);
        }

        protected override void ValidateParameter(ParamElementExecutejsModel paramModel)
        {
        }

        //private static ILogger logger = Logger.OfContext();
        //protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => eComponentType.ElementExecutejs;

        ////public List<JObject> ParseSelector(string selector, out string selectorType)
        ////{
        ////    selector = RecoverViewData.RestoreViewData(selector);
        ////    List<JObject> listTemp = new List<JObject>();
        ////    selectorType = string.Empty;
        ////    try
        ////    {
        ////        JArray ja = JArray.Parse(selector);
        ////        foreach (JObject obj in ja)
        ////        {
        ////            listTemp.Add(obj);
        ////            if (obj.ContainsKey("selector"))
        ////            {
        ////                selectorType = obj.Value<string>("selector").ToString();
        ////            }

        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        logger.LogError(ex);
        ////    }
        ////    return listTemp;
        ////}
        //protected override JMessage Execute(ParamElementExecutejsModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        //{
        //    JMessage msg = new JMessage();
        //    try
        //    {
        //        //ParseSelectorV1(paramModel.selector, out string selectorType, out string version);
        //        this.GetSelectorInfo(paramModel.selector, paramModel.BrowserType, out string selectorType, out string version);

        //        var @params = new BrowserParams()
        //        {
        //            windowId = "",
        //            tabId = "",
        //            browserType = selectorType,
        //            webData = paramModel.webData
        //        };
        //        var browserAction = WebBrowserManager.GetBrowserAction(@params);
        //        msg.Info = browserAction.ElementExecutejs(RecoverViewData.RestoreViewData(paramModel.selector), paramModel.scriptCode, paramModel.@params);
        //    }
        //    catch (Exception ex)
        //    {
        //        Exception(ex, eErrorMessageType.Error_ExecutejsFail, ref msg);
        //    }
        //    return msg;

        //}

        //protected override void ValidateParameter(ParamElementExecutejsModel paramModel)
        //{
        //    try
        //    {
        //        paramModel = jsonParam.ToObject<ParamElementExecutejsModel>();
        //        if (string.IsNullOrEmpty(paramModel.selector))
        //        {
        //            throw new RPAException(eErrorMessageType.Error_ParamsError);
        //        }
        //        if (string.IsNullOrEmpty(paramModel.scriptCode))
        //        {
        //            throw new RPAException(eErrorMessageType.Error_ParamsError);
        //        }
        //        return;
        //    }
        //    catch (Exception ex)
        //    {
        //        Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"执行JS 参数格式不符合要求：{paramModel.ToJson()}");
        //        throw new RPAException(eErrorMessageType.Error_ParamsError);
        //    }
        //}
    }
}
