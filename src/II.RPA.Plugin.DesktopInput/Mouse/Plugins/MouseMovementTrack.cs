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

namespace II.RPA.Plugin.DesktopInput.Mouse
{
    public class MouseMovementTrack : MouseComponentBase<ParamMouseMovementTrackModel>
    {
        public MouseMovementTrack(IPluginInvoker pluginInvoker) : base(pluginInvoker) { }

        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.MouseMove;

        protected override void ValidateParameter(ParamMouseMovementTrackModel paramModel)
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


        private void MouseMove(int moveModel, int speed, Point point)
        {
            switch (moveModel)
            {
                case 0:
                    II.RPA.Plugin.GUI.Commons.MouseMotions.MouseMotionHelper.PersonMoveMouse(point, speed, true, false);
                    break;
                case 1:
                    int criticalSpeed = MouseConfigurations.Info.CriticalSpeed;
                    double criticaPixel = MouseConfigurations.Info.CriticaPixel;
                    if (speed <= criticalSpeed)
                    {
                        FlaUI.Core.Input.Mouse.MovePixelsPerMillisecond = Math.Max(Math.Round(speed * criticaPixel / criticalSpeed, 1), 0.1);
                    }
                    else
                    {
                        FlaUI.Core.Input.Mouse.MovePixelsPerMillisecond = Math.Round((speed - criticalSpeed) * (10 - criticaPixel) / (100 - criticalSpeed) + criticaPixel, 1);
                    }
                    UIMouse.Mouse.MoveTo(point.X, point.Y);
                    break;
                case 2:
                    II.RPA.Plugin.GUI.Commons.MouseMotions.MouseMotionHelper.PersonMoveMouse(point, speed, false, true);
                    break;
                default:
                    break;
            }
        }
        protected override JMessage Execute(ParamMouseMovementTrackModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                if (paramModel.IsRandomMouseMove)
                {
                    logger.LogInformation("MouseMovementTrack：启用了模拟人工随机移动鼠标");
                    paramModel.moveMode = 0;
                    paramModel.speedValue = 35;
                }
                if (paramModel.positiontype == 0)  //如果是相对于屏幕左上角
                {
                    MouseMove(paramModel.moveMode, paramModel.speedValue, new Point(paramModel.x, paramModel.y));
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

                    MouseMove(paramModel.moveMode, paramModel.speedValue, new Point(x, y));
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
