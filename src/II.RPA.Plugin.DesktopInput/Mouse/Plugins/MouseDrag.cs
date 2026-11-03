using UIMouse = FlaUI.Core.Input;
using II.RPA.Plugin.DesktopInput.Mouse.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Drawing;
using FlaUI.UIA3;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Abstractions;
using System.Collections.Generic;
using II.RPA.Plugin.DesktopInput.Mouse.Config;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DesktopInput.Mouse
{
    public class MouseDrag : MouseComponentBase<ParamMouseDragModel>
    {
        public MouseDrag(IPluginInvoker pluginInvoker) : base(pluginInvoker) { }

        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.MouseDrag;

        protected override void ValidateParameter(ParamMouseDragModel paramModel)
        {
            try
            {
                if (paramModel.speedValue < 0 || paramModel.speedValue > 100)
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (paramModel.dragType != 0 && paramModel.dragType != 1)
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


        protected override async Task<JMessage> ExecuteAsync(ParamMouseDragModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                //var dpi = NativeMethods.DpiX / 96F;

                //paramModel.Startx = (int)(paramModel.Startx * dpi);
                //paramModel.Starty = (int)(paramModel.Starty * dpi);

                //paramModel.Endx = (int)(paramModel.Endx * dpi);
                //paramModel.Endy = (int)(paramModel.Endy * dpi);
                if (paramModel.IsRandomMouseMove)
                {
                    logger.LogInformation("MouseDrag：启用了模拟人工随机移动鼠标");
                    paramModel.speedValue = 0;
                }
                int dragType = paramModel.dragType;
                //指定目标
                if (dragType == 1)
                {
                    //string result = plugin.Run(ActionType.Excute, paramModel.selector);
                    var data = new Dictionary<string, object>
                    {
                        {"selector", paramModel.selector },
                        {"positionType",  paramModel.Positiontype == 1 ? 2 : 3}
                    };
                    logger.LogInformation($"data：{JsonHelper.JsonByNewtonJson(data)}");
                    string result = await PluginInvoker.InvokeAsync(new PluginRoute("Element", "GetRegion"), JsonHelper.JsonByNewtonJson(data)).ConfigureAwait(false);
                    //var obj = (JArray)JsonHelper.JsonDesByNewtonJson(result);
                    JMessage objRresult = result.ToObject<JMessage>();
                    RegionResult regionResult = objRresult.Info.ToObject<RegionResult>();
                    paramModel.Startx = regionResult.x + 5;
                    paramModel.Starty = regionResult.y + 5;
                }

                //SetProcessDPIAware();
                System.Drawing.Point startPoint, endPoint;
                if (paramModel.Positiontype == 0)  //如果是相对于屏幕左上角
                {
                    startPoint = new System.Drawing.Point(paramModel.Startx, paramModel.Starty);
                    endPoint = new System.Drawing.Point(paramModel.Endx, paramModel.Endy);
                }
                else
                {
                    IntPtr ptr = UiCommon.GetForegroundWindow();
                    var element = UiCommon.UIAAutomation.FromHandle(ptr);
                    Rectangle rec = Rectangle.Empty;
                    if (element != null)
                    {
                        rec = element.BoundingRectangle;
                    }
                    else
                    {
                        throw new RPAException(eErrorMessageType.Error_WindowActiveError);
                    }
                    startPoint = new System.Drawing.Point(paramModel.Startx + rec.X, paramModel.Starty + rec.Y);
                    endPoint = new System.Drawing.Point(paramModel.Endx + rec.X, paramModel.Endy + rec.Y);
                }
                string[] keys = paramModel.Keys;
                if (keys != null && keys.Length > 0)
                {
                    foreach (var key in keys)
                    {
                        if (key.ToLower() == "alt")
                        {
                            FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ALT);
                        }
                        else if (key.ToLower() == "ctrl")
                        {
                            FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                        }
                        else if (key.ToLower() == "shift")
                        {
                            FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.SHIFT);
                        }
                        else if (key.ToLower() == "win")
                        {
                            FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.LWIN);
                        }
                    }
                }
                FlaUI.Core.Input.MouseButton button = FlaUI.Core.Input.MouseButton.Left;
                switch (paramModel.Mousetype)
                {
                    case 0:
                        button = FlaUI.Core.Input.MouseButton.Left;
                        break;
                    case 1:
                        button = FlaUI.Core.Input.MouseButton.Right;
                        break;
                    case 2:
                        button = FlaUI.Core.Input.MouseButton.Middle;
                        break;
                    default:
                        break;

                }

                //NLogger.Default.Info($"paramModel.SpeedType:{paramModel.SpeedType} MouseConfigurations.Info.UniformSpeed:{MouseConfigurations.Info.UniformSpeed}" +
                //    $" MouseConfigurations.Info.VariableSpeed.Count:{MouseConfigurations.Info.VariableSpeed.Count}");
                //if (paramModel.SpeedType == 0)
                //{
                //    FlaUI.Core.Input.Mouse.Speeds = new List<double> { MouseConfigurations.Info.UniformSpeed };
                //    FlaUI.Core.Input.Mouse.Drag(startPoint, endPoint, button);
                //}
                //else
                //{
                //    FlaUI.Core.Input.Mouse.Speeds = MouseConfigurations.Info.VariableSpeed;
                //    FlaUI.Core.Input.Mouse.Drag(startPoint, endPoint, button);
                //}

                if (paramModel.speedValue == 0)
                {
                    int randomMoveMax = MouseConfigurations.Info.RandomMoveMax;
                    int randomMoveMin = MouseConfigurations.Info.RandomMoveMin;
                    int step = MouseConfigurations.Info.Step;
                    int stepX = (endPoint.X - startPoint.X) / step;
                    int stepY = (endPoint.Y - startPoint.Y) / step;
                    double randomSpeedMax = MouseConfigurations.Info.RandomSpeedMax;
                    double randomSpeedMin = MouseConfigurations.Info.RandomSpeedMin;
                    List<Point> stepPoint = new List<Point>();
                    List<double> stepSpeed = new List<double>();
                    for (int i = 0; i < step; i++)
                    {
                        //随机速度
                        //FlaUI.Core.Input.Mouse.MovePixelsPerMillisecond = Math.Round(new Random().NextDouble() * randomSpeedMax, 1) + randomSpeedMin;
                        //FlaUI.Core.Input.Mouse.Speeds = new List<double> { Math.Round(new Random().NextDouble() * randomSpeedMax, 1) + randomSpeedMin };
                        stepSpeed.Add(Math.Round(new Random().NextDouble() * randomSpeedMax, 1) + randomSpeedMin);

                        //随机漂移坐标
                        if (i == 0)
                        {
                            stepPoint.Add(startPoint);
                        }
                        else
                        {
                            int x = startPoint.X + i * stepX;
                            int y = startPoint.Y + i * stepY;
                            x += new Random().Next(randomMoveMax - randomMoveMin) + randomMoveMin;
                            y += new Random().Next(randomMoveMax - randomMoveMin) + randomMoveMin;
                            stepPoint.Add(new System.Drawing.Point(x, y));
                        }
                    }
                    stepPoint.Add(endPoint);
                    FlaUI.Core.Input.Mouse.Speeds = stepSpeed;
                    FlaUI.Core.Input.Mouse.Drag(stepPoint, button);
                }
                else
                {
                    int criticalSpeed = MouseConfigurations.Info.CriticalSpeed;
                    double criticaPixel = MouseConfigurations.Info.CriticaPixel;
                    if (paramModel.speedValue <= criticalSpeed)
                    {
                        //FlaUI.Core.Input.Mouse.MovePixelsPerMillisecond = Math.Round(paramModel.speedValue * criticaPixel / criticalSpeed, 1);
                        FlaUI.Core.Input.Mouse.Speeds = new List<double> { Math.Max(Math.Round(paramModel.speedValue * criticaPixel / criticalSpeed, 1), 0.1) };
                    }
                    else
                    {
                        //FlaUI.Core.Input.Mouse.MovePixelsPerMillisecond = Math.Round((paramModel.speedValue - criticalSpeed) * (10 - criticaPixel) / (100 - criticalSpeed) + criticaPixel, 1);
                        FlaUI.Core.Input.Mouse.Speeds = new List<double> { Math.Round((paramModel.speedValue - criticalSpeed) * (10 - criticaPixel) / (100 - criticalSpeed) + criticaPixel, 1) };
                    }
                    FlaUI.Core.Input.Mouse.Drag(startPoint, endPoint, button);
                }

                if (keys != null && keys.Length > 0)
                {
                    foreach (var key in keys)
                    {
                        if (key.ToLower() == "alt")
                        {
                            FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.ALT);
                        }
                        else if (key.ToLower() == "ctrl")
                        {
                            FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                        }
                        else if (key.ToLower() == "shift")
                        {
                            FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.SHIFT);
                        }
                        else if (key.ToLower() == "win")
                        {
                            FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.LWIN);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_MouseDragError, ref msg);
            }
            return msg;
        }
    }

    public class RegionResult
    {
        public int x { get; set; }
        public int y { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }
}
