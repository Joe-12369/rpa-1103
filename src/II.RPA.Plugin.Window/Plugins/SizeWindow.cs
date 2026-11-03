using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Window.Core;
using II.RPA.Plugin.Window.Params;
using II.RPA.WinAPI;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Window.Plugins
{
    public class SizeWindow : WindowComponentBase<ParamSizeWindowModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.SizeWindow;


        protected override JMessage Execute(ParamSizeWindowModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                        throw new Exception("窗口对象为桌面，操作失败".I18n("controlType_SizeWindow_36126028"));
                    }
                    else
                    {
                        windowData = WindowOperator.MatchByData(windowData);
                    }
                }

                //var windowData = paramModel.WindowData.ToObject<WindowData>();
                //windowData = WindowOperator.MatchByData(windowData);

                long.TryParse(windowData.WindowHandle, out long numHandle);
                IntPtr handle = new IntPtr(numHandle);

                //if (User32.IsIconic(handle))
                //    throw new WindowException(eErrorMessageType.Error_MinimizeWindowNotSize);
                User32.GetWindowRect(handle, out WinAPI.Struct.RECT rect);
                User32.SetWindowPos(
                    handle,
                    IntPtr.Zero,
                    rect.left,
                    rect.top,
                    paramModel.Width,
                    paramModel.Height,
                    User32.SWP.SHOWWINDOW);

                msg.Info = "true";
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetWindowRect, ref msg);
            }

            return msg;
        }
    }
}