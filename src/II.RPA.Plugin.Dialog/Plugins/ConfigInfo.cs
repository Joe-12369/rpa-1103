using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.Dialog.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.Dialog.Plugins
{
    public class ConfigInfo : StaDialogAbilityBase<ParamInfoDialogModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ConfigDialog;
        public ConfigInfo(IStaDispatcher staDispatcher)
            : base(staDispatcher)
        {
        }

        protected override JMessage Execute(ParamInfoDialogModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                try
                {
                    FormInfo input = new FormInfo(paramModel);
                    if (input.ShowDialog() == true)
                    {
                        msg.Info = input.outJson;
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"消息对话框线程内异常");
                    msg.Message = "对话框显示异常，请检查配置信息是否正确！".I18n("ConfigInfo_3249D9BF");
                }
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_UNKOWN, ref msg);
            }

            return msg;
        }

        protected override void ValidateParameter(ParamInfoDialogModel paramModel)
        {
        }
    }
}

