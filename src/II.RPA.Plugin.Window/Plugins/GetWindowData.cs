using II.RPA.Plugin.Core;
using II.RPA.Plugin.Window.Core;
using II.RPA.Plugin.Window.Params;
using II.RPA.WinAPI;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Automation;

namespace II.RPA.Plugin.Window.Plugins
{
    public class GetWindowData : WindowComponentBase<ParamGetWindowDataModel>
    {
        protected override eComponentType ComponentType => eComponentType.GetWindowData;

        private string GetWindowsTitle(IntPtr intPtr)
        {
            int length = User32.GetWindowTextLength(intPtr);
            StringBuilder sb = new StringBuilder(length + 1);
            User32.GetWindowText(intPtr, sb, sb.Capacity);
            if (!string.IsNullOrEmpty(sb.ToString()))
                return sb.ToString();
            else
                return "";
        }
        protected override JMessage Execute(ParamGetWindowDataModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                WindowData data;

                if (paramModel.SwitchType.ToLower() == "selecter")
                {
                    data = WindowOperator.FindBySelecter(paramModel.Selecter);
                }
                else if (paramModel.SwitchType.ToLower() == "title")
                {
                    data = WindowOperator.FindWindosByKeywords(paramModel.Keywords, paramModel.MatchType);
                }
                else if (paramModel.SwitchType.ToLower() == "active")
                {
                    data = WindowOperator.FindByActive();
                }
                else if (paramModel.SwitchType.ToLower() == "window")
                {
                    var windowData = paramModel.WindowData.ToObject<WindowData>();
                    if (windowData.Title == "DeskTop")
                    {
                        data = new WindowData { Title = "DeskTop", WindowHandle = User32.GetDesktopWindow().ToString() };
                    }
                    else
                    {
                        data = WindowOperator.MatchByData(windowData);
                    }
                }
                else if (paramModel.SwitchType.ToLower() == "desktop")
                {
                    data = new WindowData { Title = "DeskTop", WindowHandle = User32.GetDesktopWindow().ToString() };
                }
                //兼容老流程包版本
                else
                {
                    var windowData = paramModel.WindowData.ToObject<WindowData>();
                    data = WindowOperator.MatchByData(windowData);
                }


                msg.Info = JsonConvert.SerializeObject(data);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_GetWindowData, ref msg);
            }

            return msg;
        }

    }
}
