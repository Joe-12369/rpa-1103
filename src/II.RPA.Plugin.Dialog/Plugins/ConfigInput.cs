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
    public class ConfigInput : StaDialogAbilityBase<ParamConfigDialogModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ConfigDialog;
        public ConfigInput(IStaDispatcher staDispatcher)
            : base(staDispatcher)
        {
        }

        protected override JMessage Execute(ParamConfigDialogModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                InputDialogOutPut result = new InputDialogOutPut() { Code = "confirm" };
                try
                {
                    FormInput input = new FormInput(paramModel.Info, paramModel.AutoOut, paramModel.Title);
                    if (input.ShowDialog() == true)
                    {
                        result.Info = input.outJson;
                    }
                    else
                    {
                        result.Code = "cancel";
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"输入输出对话框线程内异常");
                    result.Code = "error";
                }
                msg.Info = JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_UNKOWN, ref msg);
            }

            return msg;
        }

        protected override void ValidateParameter(ParamConfigDialogModel paramModel)
        {
        }
    }
}

