using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using II.RPA.Plugin.Abstractions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class HighlightScriptElement : GuiComponentBase<ParamHighlightScriptElementModel>
    {
        private static ILogger logger = Logger.OfContext();
        private readonly IPluginInvoker pluginInvoker;

        public HighlightScriptElement(IPluginInvoker pluginInvoker)
        {
            this.pluginInvoker = pluginInvoker ?? throw new ArgumentNullException(nameof(pluginInvoker));
        }

        protected override eComponentType ComponentType => eComponentType.IsSimilarElementExist;

        protected override async Task<JMessage> ExecuteAsync(ParamHighlightScriptElementModel paramModel, PluginExecutionContext executionContext)
        {
            ParamGetElementModel paramGetElementModel = new ParamGetElementModel()
            {
                Selector = paramModel.WebScript,
                QueryMode = paramModel.QueryMode,
                browserType = string.IsNullOrEmpty(paramModel.BrowserType) ? "chrome" : paramModel.BrowserType,
            };
            string result = await pluginInvoker.InvokeAsync(
                new PluginRoute("Element", "GetElement"),
                JsonConvert.SerializeObject(paramGetElementModel)).ConfigureAwait(false);
            var info = JsonConvert.DeserializeObject<JMessage>(result);

            ParamIsSimilarElementExistModel paramIsSimilarElementExistModel = new ParamIsSimilarElementExistModel()
            {
                Selector = info.Info,
                isHighlighting = true,
            };
            result = await pluginInvoker.InvokeAsync(
                new PluginRoute("Element", "IsSimilarElementExist"),
                JsonConvert.SerializeObject(paramIsSimilarElementExistModel)).ConfigureAwait(false);
            var resultMessage = JsonConvert.DeserializeObject<JMessage>(result);
            var ret = new
            {
                code = resultMessage.Code,
                message = resultMessage.Message,
                info = resultMessage.Info,
                extend = resultMessage.Extend
            }; 
            resultMessage.Info = JsonConvert.SerializeObject(ret);
            return resultMessage;
        }

        protected override void ValidateParameter(ParamHighlightScriptElementModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.WebScript))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"元素校验 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

