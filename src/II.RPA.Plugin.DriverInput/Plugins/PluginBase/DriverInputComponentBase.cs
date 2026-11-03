using II.RPA.Plugin.Core;
using II.RPA.Plugin.DriverInput.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DriverInput.Plugins.PluginBase
{
    public abstract class DriverInputComponentBase<TParam> : II.RPA.Plugin.Core.PluginBase.PluginAbilityBase<TParam> where TParam : ParamBase
    {
        protected override TParam DeserializeParameter(string jsonParam)
        {
            var paramModel = base.DeserializeParameter(jsonParam);
            if (paramModel.InputMode == 1)
            {
                paramModel.ComponentLabel = EnumInputMode.PS2.ToString();
            }
            else if(paramModel.InputMode == 2)
            {
                paramModel.ComponentLabel = EnumInputMode.HID.ToString();
            }
            return paramModel;
        }
    }

}
