using UIMouse = FlaUI.Core.Input;
using II.RPA.Plugin.DesktopInput.Mouse.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Drawing;
using FlaUI.UIA3;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.DesktopInput.Mouse.Config;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DesktopInput.Mouse
{
    public class MouseMove : MouseComponentBase<ParamMouseMoveModel>
    {
        public MouseMove(IPluginInvoker pluginInvoker) : base(pluginInvoker) { }

        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.MouseMove;

        protected override void ValidateParameter(ParamMouseMoveModel paramModel)
        {
            try
            {
                if (paramModel.speedValue < 0 || paramModel.speedValue > 100)
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception)
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        protected override async Task<JMessage> ExecuteAsync(ParamMouseMoveModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                //FlaUI.Core.Input.Mouse.MovePixelsPerMillisecond = MouseConfigurations.Info.MovingSpeed;
                //var dpi = NativeMethods.DpiX / 96F;

                //paramModel.x = (int)(paramModel.x * dpi);
                //paramModel.y = (int)(paramModel.y * dpi);
                if (paramModel.IsRandomMouseMove)
                {
                    logger.LogInformation("MouseMove：启用了模拟人工随机移动鼠标");
                    paramModel.isUsePerson = 1;
                    paramModel.speedValue = 35;
                }
                if (paramModel.isUsePerson == 1)
                {
                    await PluginInvoker.InvokeAsync(new PluginRoute("Mouse", "MouseMovementTrack"), paramModel.ToJson()).ConfigureAwait(false);
                    return msg;
                }
                if (paramModel.positiontype == 0)  //如果是相对于屏幕左上角
                {
                    Point point = new Point(paramModel.x, paramModel.y);
                    II.RPA.Plugin.GUI.Commons.MouseMotions.MouseMotionHelper.PersonMoveMouse(false, point);
                }
                else
                {
                    IntPtr ptr = UiCommon.GetForegroundWindow();
                    UIA3Automation uia3Automation = new UIA3Automation();
                    var element = uia3Automation.FromHandle(ptr);
                    Rectangle rec = Rectangle.Empty;
                    if (element != null)
                    {
                        rec = element.BoundingRectangle;
                    }
                    else
                    {
                        throw new RPAException(eErrorMessageType.Error_WindowActiveError);
                    }
                    int x = rec.X + paramModel.x;
                    int y = rec.Y + paramModel.y;

                    Point point = new Point(x, y);
                    II.RPA.Plugin.GUI.Commons.MouseMotions.MouseMotionHelper.PersonMoveMouse(false, point);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Exception(ex, eErrorMessageType.Error_MouseMoveError, ref msg);
            }
            return msg;
        }
    }
}
