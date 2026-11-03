using II.RPA.Plugin.Core;
using II.RPA.Plugin.Window.Params;
using System;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Plugin.Window.Core;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Linq;

namespace II.RPA.Plugin.Window.Plugins
{
    public class ActiveWindow : WindowComponentBase<ParamActiveWindowModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ActiveWindow;


        protected override JMessage Execute(ParamActiveWindowModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                        logger.LogInformation($"桌面窗口直接回到桌面");
                        Type shellType = Type.GetTypeFromProgID("shell.application");
                        object shell = Activator.CreateInstance(shellType);
                        shellType.InvokeMember("ToggleDesktop", BindingFlags.InvokeMethod, null, shell, new object[] { });
                        msg.Info = "true";
                        return msg;
                    }
                    else
                    {
                        windowData = WindowOperator.MatchByData(windowData);
                    }
                }
                //windowData = WindowOperator.MatchByData(windowData);
                logger.LogInformation($"WindowHandle: {windowData.WindowHandle}");
                long.TryParse(windowData.WindowHandle, out long numHandle);
                IntPtr handle = new IntPtr(numHandle);
                WindowOperator.Activate(windowData, paramModel.IsMaximize == 0 ? true : false);

                msg.Info = "true";
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_ActiveWindow, ref msg);
            }

            return msg;
        }
    }
}
