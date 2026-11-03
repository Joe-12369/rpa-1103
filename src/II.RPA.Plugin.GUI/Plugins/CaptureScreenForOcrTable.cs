using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class CaptureScreenForOcrTable : StaElementAbilityBase<ParamCaptureScreenForOcrTableModel>
    {
        private static ILogger logger = Logger.OfContext();
        public CaptureScreenForOcrTable(IStaDispatcher staDispatcher)
            : base(staDispatcher)
        {
        }

        protected override eComponentType ComponentType => eComponentType.CaptureScreen;
        protected override void ValidateParameter(ParamCaptureScreenForOcrTableModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.imgPath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "图片保存地址为空".I18n("controlType_CaptureScreenForOcrTable_4CB9ECDF"));
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "CaptureScreen 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        protected override JMessage Execute(ParamCaptureScreenForOcrTableModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                CaptureScreenWnd wnd = new CaptureScreenWnd(paramModel.imgPath);
                if (wnd.ShowDialog() == true)
                {
                    Dictionary<string, object> dic = new Dictionary<string, object>();

                    dic["X"] = wnd.ImgRectFromScreen.X;
                    dic["Y"] = wnd.ImgRectFromScreen.Y;
                    dic["Width"] = wnd.ImgRectFromScreen.Width;
                    dic["Height"] = wnd.ImgRectFromScreen.Height;
                    msg.Info = dic.ToJson();
                }
                else
                {
                    Exception(eErrorMessageType.Error_USERCLOSE, ref msg);
                }
            }
            catch (Exception e)
            {
                Exception(e, eErrorMessageType.Error_CaptureScreen_Error, ref msg);
            }
            return msg;
        }



    }
}

