using II.RPA.Plugin.Core;
using II.RPA.Plugin.Window.Core;
using II.RPA.Plugin.Window.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Window.Plugins
{
    public class WaitWindow : WindowComponentBase<ParamWaitWindowModel>
    {
        protected override eComponentType ComponentType => eComponentType.WaitWindow;


        protected override JMessage Execute(ParamWaitWindowModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                //var windowData = paramModel.WindowData.ToObject<WindowData>();
                var retResult = WindowOperator.WaitWindowStatus(paramModel);
                msg.Info = retResult ? "true" : "false";
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_WaitWindow, ref msg);
            }
            return msg;
        }

       
    }
}
