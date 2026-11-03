using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Window.Core;
using II.RPA.Plugin.Window.Params;
using II.RPA.WinAPI;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Linq;

namespace II.RPA.Plugin.Window.Plugins
{
    public class MoveWindow : WindowComponentBase<ParamMoveWindowModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.MoveWindow;


        protected override JMessage Execute(ParamMoveWindowModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();

            try
            {

                WindowData windowData;

                if (paramModel.SwitchType.ToLower() == "selecter")
                {
                    windowData = WindowOperator.FindBySelecter(paramModel.Selecter);
                }
                else if (paramModel.SwitchType.ToLower() == "title")
                {
                    windowData = WindowOperator.FindWindosByKeywords(paramModel.Keywords, paramModel.MatchType);
                }
                else
                {
                    windowData = paramModel.WindowData.ToObject<WindowData>();
                    if (windowData.Title == "DeskTop")
                    {
                        logger.LogInformation($"窗口对象为桌面，操作失败");
                        throw new Exception("窗口对象为桌面，操作失败".I18n("controlType_MoveWindow_36126028"));
                    }
                    else
                    {
                        windowData = WindowOperator.MatchByData(windowData);
                    }
                }
                //移动前先激活窗口
                WindowOperator.Activate(windowData, false);
                long.TryParse(windowData.WindowHandle, out long numHandle);
                IntPtr handle = new IntPtr(numHandle);
                User32.GetWindowRect(handle, out WinAPI.Struct.RECT rect);
                var rectangle = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);

                User32.SetWindowPos(
                    handle,
                    IntPtr.Zero,
                    paramModel.X,
                    paramModel.Y,
                    rectangle.Width,
                    rectangle.Height,
                    User32.SWP.SHOWWINDOW);

                msg.Info = "true";
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_MoveWindow, ref msg);
            }

            return msg;
        }
    }
}
