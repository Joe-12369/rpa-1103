using II.RPA.WinAutomation.CommonUtility;
using System;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.Core;
using Newtonsoft.Json;
using II.RPA.Plugin.GUI.Params;
using System.Collections.Generic;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Core.Commons;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetElement : GuiComponentBase<ParamGetElementModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetElement;

        protected override void ValidateParameter(ParamGetElementModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.QueryMode))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (!Enum.TryParse(paramModel.browserType, true, out Commons.WindowMatch.WindowType wndType))
                {
                    wndType = Commons.WindowMatch.WindowType.Chromium;
                    if (string.IsNullOrEmpty(paramModel.SelectorGuiType))
                    {
                        paramModel.SelectorGuiType = wndType.ToString();
                    }
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"GetElement 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        private Props BuildProps(string name, string value, string pattern = "equal", string accurate = "true")
        {
            Props props = new Props();
            props.Name = name;
            props.Value = value;
            props.Accurate = accurate;
            props.Pattern = pattern;
            return props;
        }
        private string BuildRPACommon(ParamGetElementModel paramModel)
        {
            RPACommon rpaCommon = new RPACommon();
            rpaCommon.CommonProps.GUIType = string.IsNullOrEmpty(paramModel.SelectorGuiType) ? paramModel.browserType : paramModel.SelectorGuiType;
            rpaCommon.CommonProps.QueryMode = paramModel.QueryMode;
            rpaCommon.CommonProps.CssSelector = paramModel.Selector;
            rpaCommon.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;
            SelectorProps selectorProps = new SelectorProps();
            selectorProps.Accurate = "true";
            selectorProps.Name = "tab";
            selectorProps.Type = "web";
            selectorProps.Props = new List<Props>();
            var title = string.IsNullOrEmpty(paramModel.webData) ? "" : JsonConvert.DeserializeObject<JObject>(paramModel.webData)["title"].ToString();
            selectorProps.Props.Add(BuildProps("title", title));
            var url = string.IsNullOrEmpty(paramModel.webData) ? "" : JsonConvert.DeserializeObject<JObject>(paramModel.webData)["url"].ToString();
            selectorProps.Props.Add(BuildProps("url", url, accurate: "false"));
            List<SelectorProps> selectorPropsList = new List<SelectorProps>();
            selectorPropsList.Add(selectorProps);
            rpaCommon.Selector = JArray.Parse(JsonConvert.SerializeObject(selectorPropsList));
            return JsonConvert.SerializeObject(rpaCommon);
        }
        protected override JMessage Execute(ParamGetElementModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            string selector = string.Empty;
            string selectorType = string.Empty;
            JMessage msg = new JMessage();
            try
            {

                if (paramModel.QueryMode != "default")
                {
                    if (paramModel.browserType == "IE" && paramModel.QueryMode == "xpath")
                        throw new RPAException(eGuiType.IE, eErrorMessageType.Error_NotSupertXpath);
                    msg.Info = BuildRPACommon(paramModel);
                }
                else
                    msg.Info = paramModel.Selector;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_NotFoundElement, ref msg);
                return msg;
            }

            return msg;

        }
    }
}

