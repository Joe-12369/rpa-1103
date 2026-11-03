using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Plugins.Chatrpa
{
    internal class ClearChatRpaCacheElement : GuiComponentBase<ParamClearChatRpaCacheElementModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ClearChatRpaCacheElement;

        protected override JMessage Execute(ParamClearChatRpaCacheElementModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                GuiManager.Info.GuiChatSelectorCacheDic?.Clear();
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_NotFoundElement, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamClearChatRpaCacheElementModel paramModel) {
            try
            {
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"ClearChatRpaCacheElement 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

