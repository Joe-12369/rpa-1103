using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Window.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Text;
using II.RPA.WinAPI;
using II.RPA.Plugin.Window.Core;
using AutoIt;
using Microsoft.Extensions.Logging;
using II.RPA.Logging;
using System.Linq;

namespace II.RPA.Plugin.Window.Plugins
{
    public class SetWindowState : WindowComponentBase<ParamSetWindowStateModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.GetWindowData;


        protected override JMessage Execute(ParamSetWindowStateModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                        throw new Exception("窗口对象为桌面，操作失败".I18n("controlType_SetWindowState_36126028"));
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
                switch (paramModel.WindowType.ToLower())
                {
                    case "normal":
                        //User32.ShowWindow(handle, User32.SW_SHOWNORMAL);
                        AutoItX.WinSetState(handle, (int)User32.SW_SHOWNORMAL);
                        break;
                    case "maximize":
                        //User32.ShowWindow(handle, User32.SW_SHOWMAXIMIZED);
                        AutoItX.WinSetState(handle, (int)User32.SW_SHOWMAXIMIZED);
                        break;
                    case "minimize":
                        //User32.ShowWindow(handle, User32.SW_SHOWMINIMIZED);
                        AutoItX.WinSetState(handle, (int)User32.SW_SHOWMINIMIZED);
                        break;
                    default:
                        break;
                }

                msg.Info = "true";
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetWindowData, ref msg);
            }

            return msg;
        }

        protected override void ValidateParameter(ParamSetWindowStateModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.WindowType))
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
            catch (Exception ex)
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}
