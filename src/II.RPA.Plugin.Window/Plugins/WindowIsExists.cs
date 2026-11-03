using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Window.Core;
using II.RPA.Plugin.Window.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Window.Plugins
{
    public class WindowIsExists : WindowComponentBase<ParamWindowIsExistsModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.WindowIsExists;


        protected override JMessage Execute(ParamWindowIsExistsModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                WindowData windowData;
                try
                {
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
                            msg.Info = paramModel.ExistsType == 0 ? "true" : "false";
                            return msg;
                        }
                        else
                        {
                            windowData = WindowOperator.MatchByData(windowData);
                        }
                    }
                    var retResult = WindowOperator.IsWindowExists(windowData);
                    msg.Info = paramModel.ExistsType == 0 ?
                            retResult ? "true" : "false" :
                            retResult ? "false" : "true";
                }
                catch
                {
                    msg.Info = paramModel.ExistsType == 0 ? "false" : "true";
                }
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_WindowIsExists, ref msg);
            }
            return msg;
        }

       
    }
}
