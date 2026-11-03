using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;
using Newtonsoft.Json.Linq;
using IWshRuntimeLibrary;
using II.RPA.Plugin.GUI.Commons.Params;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class GetText : GuiComponentBase<ParamGetTextModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetText;

        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_GetText_OperationTomeout_Error;


        protected override void ValidateParameter(ParamGetTextModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"GetText 鍙傛暟鏍煎紡涓嶇鍚堣姹傦細{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamGetTextModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();


            GuiElement element = null;
            try
            {
                Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };
                paramModel.cvExecutionServiceURL = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_EXECUTION_SERVICE;
                var param = new GetElementFromSelectorParams
                {
                    Selector = paramModel.Selector,
                    SelectorType = paramModel.SelectorType,
                    FindOnce = false,
                    CVExecutionServiceURL = paramModel.cvExecutionServiceURL,
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
                string result = element.GetText();
                msg.Info = result;
            }
            catch (RPAException ex)
            {
                Exception(ex, ref msg);
                return msg;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetAttributeError, ref msg);
            }
            return msg;

        }
    }
}

