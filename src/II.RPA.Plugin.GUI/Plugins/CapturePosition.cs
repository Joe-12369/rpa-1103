using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class CapturePosition : StaElementAbilityBase<ParamCapturePositionModel>
    {
        private static ILogger logger = Logger.OfContext();
        public CapturePosition(IStaDispatcher staDispatcher)
            : base(staDispatcher)
        {
        }

        protected override eComponentType ComponentType => eComponentType.CapturePosition;

        protected override void ValidateParameter(ParamCapturePositionModel paramModel) {
            try
            {
                //if (string.IsNullOrEmpty(paramModel.FilePath))
                //{
                //    throw new RPAException(eErrorMessageType.Error_ParamsError, "缺少文件地址参数");
                //}
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamCapturePositionModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                NativeMethods.SetProcessDPIAware();
                //type 未被使用，保留参数，和之前方法签名一致，保证兼容性
                GuiElement.PositionForm.StartCapture(paramModel.Type);
                if (GuiElement.PositionForm.ShowDialog() == true)
                {
                    msg.Info = JsonHelper.JsonByNewtonJson(new
                    {
                        positiontype = paramModel.Type,
                        x = GuiElement.PositionForm.CursorResult.X,
                        y = GuiElement.PositionForm.CursorResult.Y
                    });
                }
                else
                {
                    Exception(null, eErrorMessageType.Error_USERCLOSE, ref msg);
                }
                GuiElement.PositionForm = null;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_CapturePositionError, ref msg);
            }
            return msg;
        }

    }
}

