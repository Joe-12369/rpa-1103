using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Window.Core;
using II.RPA.Plugin.Window.Params;
using II.RPA.WinAPI;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace II.RPA.Plugin.Window.Plugins
{
    public static class JsonRectangle
    {
        public static string ToJsonString(this Rectangle rectangle)
        {
            JObject json = new JObject();
            json.Add("X", rectangle.X.ToString(CultureInfo.CurrentCulture));
            json.Add("Y", rectangle.Y.ToString(CultureInfo.CurrentCulture));
            json.Add("Width", rectangle.Width.ToString(CultureInfo.CurrentCulture));
            json.Add("Height", rectangle.Height.ToString(CultureInfo.CurrentCulture));
            return json.ToJson();
        }
    }
    public class GetWindowRect : WindowComponentBase<ParamGetWindowRectModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.GetWindowRect;


        protected override JMessage Execute(ParamGetWindowRectModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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
                    windowData = WindowOperator.FindByTitle(paramModel.Title);

                    //windowData = WindowOperator.FindByKeywords(paramModel.Keywords, paramModel.MatchType).FirstOrDefault();
                }
                else
                {
                    windowData = paramModel.WindowData.ToObject<WindowData>();
                    if (windowData.Title == "DeskTop")
                    {
                        logger.LogInformation($"窗口对象为桌面，操作失败");
                        throw new Exception("窗口对象为桌面，操作失败".I18n("controlType_GetWindowRect_36126028"));
                    }
                    else
                    {
                        windowData = WindowOperator.MatchByData(windowData);
                    }
                }

                long.TryParse(windowData.WindowHandle, out long numHandle);
                IntPtr handle = new IntPtr(numHandle);
                User32.GetWindowRect(handle, out WinAPI.Struct.RECT rect);
                var rectangle = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
                msg.Info = rectangle.ToJsonString();
            }
            catch (Exception ex)
            {
                Exception(ex, eGuiType.Window, eErrorMessageType.Error_GetWindowRect, true, ref msg);
            }

            return msg;
        }
    }
}