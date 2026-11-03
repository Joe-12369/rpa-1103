using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using AutoIt;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Window.Core;
using II.RPA.Plugin.Window.Params;
using II.RPA.WinAPI;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace II.RPA.Plugin.Window.Plugins
{
    public class SetWindowShow : WindowComponentBase<ParamSetWindowShowModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.SetWindowShow;


        protected override JMessage Execute(ParamSetWindowShowModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                        throw new Exception("窗口对象为桌面，操作失败".I18n("controlType_SetWindowShow_36126028"));
                    }
                    else
                    {
                        windowData = WindowOperator.MatchByData(windowData);
                    }
                }

                //WindowData windowData;
                //var handleData = windowData = paramModel.WindowData.ToObject<WindowData>();
                //windowData = WindowOperator.MatchByData(windowData);
                long.TryParse(windowData.WindowHandle, out long numHandle);
                IntPtr handle = new IntPtr(numHandle);

                switch (paramModel.WindowType.ToLower())
                {
                    case "show":
                        User32.ShowWindow(handle, User32.SW_SHOW);
                        break;
                    case "hide":
                        User32.ShowWindow(handle, User32.SW_HIDE);
                        break;
                    default:
                        break;
                }

                msg.Info = "true";
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_SetWindowShow, ref msg);
            }

            return msg;
        }

        protected override void ValidateParameter(ParamSetWindowShowModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.WindowType))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
            }
            catch (Exception ex)
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}
