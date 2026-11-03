using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI
{

    public class RemoteCapture : ElementAbilityBase<Params.EmptyElementParameter>
    {
        private static ILogger logger = Logger.OfContext();
        private readonly IStaDispatcher staDispatcher;

        public RemoteCapture(IStaDispatcher staDispatcher)
        {
            this.staDispatcher = staDispatcher;
        }

        protected override eComponentType ComponentType => eComponentType.Capture;

        protected override void ValidateParameter(Params.EmptyElementParameter paramModel) {
            try
            {
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }


        protected override JMessage Execute(Params.EmptyElementParameter paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                _ = staDispatcher.InvokeAsync(() => GuiElement.overlayForm.DoSelect());
                //var handle = result.AsyncWaitHandle;
                //handle.WaitOne();
                //session.Send(msg + "|" + requestInfo.Body);
                //终止异步调用，通过返回值取得调用结果
                //var selector = asyncDel.EndInvoke(result);

                //msg.Info = ExtractViewData.VisualViewData(selector);
                //if (element != null)
                //{
                //    var sdk = new SDK();
                //    var msg = sdk.AddFileAttrToSelector(element.Selector, element.screenShotPath);
                //    //new Action(GuiElement.overlayForm.StopHighlight).BeginInvoke(null, null);
                //    ExtentionServer.f = Name;
                //    session.Send(msg + "|" + requestInfo.Body);
                //}
            }
            catch (Exception ex)
            {
                if (GuiElement.overlayForm.Visible == true)
                {
                    GuiElement.overlayForm.Close();
                }
                //GuiElement.overlayForm.SetWndHide();
                Exception(ex, eErrorMessageType.Error_ElementCaptureError, ref msg);
            }
            return msg;
        }
        protected override string HandlingErrorMessage(JMessage resultMsg)
        {
            return base.HandlingErrorMessage(resultMsg);
        }
    }
}

