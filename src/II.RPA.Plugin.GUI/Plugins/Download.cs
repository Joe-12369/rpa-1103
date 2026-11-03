using II.RPA.Plugin.Core;

using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Commons;
using System.Threading;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class Download : GuiComponentBase<ParamDownloadModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.Download;

        protected override JMessage Execute(ParamDownloadModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
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
                        ExtendParam = hashtable,
                        MatchType = paramModel.MatchType,
                        RunParam = paramModel.RunParam,
                        WindowInfo = paramModel.WindowInfo
                        //TabId = paramModel.TabId  //最终会根据元素成功新匹配到一个TabID 
                    };
                    element = GuiElement.FromSelector(param);
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
                    msg.Info = element.ElementDownload(paramModel.saveFile, paramModel.fileName, paramModel.timeout);
                }
                catch (Exception ex)
                {
                    Exception(ex, eErrorMessageType.Error_ExecutejsFail, ref msg);
                }
                return msg;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_DownloadFail, ref msg);
            }
            return msg;

        }

        protected override void ValidateParameter(ParamDownloadModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                //if (string.IsNullOrEmpty(paramModel.saveFile))
                //{
                //    throw new RPAException(eErrorMessageType.Error_ParamsError);
                //}
                //if (string.IsNullOrEmpty(paramModel.fileName))
                //{
                //    throw new RPAException(eErrorMessageType.Error_ParamsError);
                //}
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"下载 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

