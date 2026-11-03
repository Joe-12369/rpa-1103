using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.UsbHub.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.UsbHub.Plugins
{
    public class LocalGetComs : PluginAbilityBase<ParamLocalGetComsModel>
    {
        protected override eComponentType ComponentType => eComponentType.LocalGetComs;

        protected override JMessage Execute(ParamLocalGetComsModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                var coms = UsbHubHelper.Instance.GetCom();
                string result = JsonHelper.JsonByNewtonJson(coms);
                msg.Info = result;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_LocalGetComsError, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamLocalGetComsModel paramModel)
        {
            return;
        }
    }
}
