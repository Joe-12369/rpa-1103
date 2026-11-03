using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Plugins
{

    public class GetElementInfo : GuiComponentBase<ParamGetElementInfoModel>
    {
        protected override eComponentType ComponentType => eComponentType.GetElementInfo;

        protected override void ValidateParameter(ParamGetElementInfoModel paramModel) {
            return;
        }
        protected override JMessage Execute(ParamGetElementInfoModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            GuiElement element = null;
            try
            {
                Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };
                var param = new GetElementFromSelectorParams
                {
                    Selector = paramModel.Selector,
                    SelectorType = paramModel.SelectorType,
                    FindOnce = false,
                    ExtendParam = hashtable,
                    MatchType = paramModel.MatchType,
                    RunParam = paramModel.RunParam,
                    WindowInfo = paramModel.WindowInfo
                };
                element = GuiElement.FromSelector(param);
            }
            catch (RPAException ex)
            {
                Exception(ex, ref msg);
                return msg;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_NotFoundElement, ref msg);
                return msg;
            }
            try
            {
                if (element == null)
                {
                    throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                }
                NotifyCoreServerElementFound(paramModel.RunUniqueID);
                string result = GetElementAttributeValue(element, paramModel.SelectorType, paramModel.AttributeName, paramModel.IsCompensationLink == 1, paramModel.apiVersion);
                msg.Info = result;
            }
            catch (RPAException ex)
            {
                Exception(ex, ref msg);
                return msg;
            }
            catch (NotImplementedException ex)
            {
                Exception(ex, eErrorMessageType.Error_GetElementInfo_NotImplemented_Error, ref msg);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetElementInfoError, ref msg);
            }
            return msg;
        }
        private string GetElementAttributeValue(GuiElement guiElement, string selectorType, string attributeName, bool isCompensationLink, string apiVersion)
        {
            string result = string.Empty;
            switch (attributeName)
            {
                case "bounding":
                    var rect = guiElement.GetRegion(3);

                    Rect rc = new Rect();
                    rc.x = rect.X.ToString();
                    rc.y = rect.Y.ToString();
                    rc.width = rect.Width.ToString();
                    rc.height = rect.Height.ToString();
                    result = rc.ToJson();
                    break;
                case "gettext":
                    result = guiElement.GetText();
                    break;
                case "code":
                    result = guiElement.GetHtml();
                    break;
                case "absoluteurl":
                    var url = guiElement.GetAttribute_V1("absoluteurl", isCompensationLink, apiVersion);
                    if (isCompensationLink)
                        url = UtilsHelper.CompleteInformation(url);
                    result = url;
                    break;
                default:
                    result = guiElement.GetAttribute_V1(attributeName, isCompensationLink, apiVersion);
                    break;
            }
            return result;
        }
    }
}
