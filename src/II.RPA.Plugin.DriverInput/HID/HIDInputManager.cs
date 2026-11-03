using II.RPA.Plugin.DriverInput.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DriverInput.HID
{
    internal class HIDInputManager
    {

        public void DriverInput(ParamDriverInputModel _ParamDriverInputModel)
        {
            HIDSendHelper CHIDSendHelper = new HIDSendHelper();
            CHIDSendHelper.InitEnvironment((int)_ParamDriverInputModel.InputInterval, _ParamDriverInputModel.KeyDownDuration);
            if (_ParamDriverInputModel.IsAppend == 1)
            {
                CHIDSendHelper.ClearAllText("");
                Thread.Sleep(50);
            }
            
            CHIDSendHelper.SendText("", _ParamDriverInputModel.InputContent);
        }
    }
}
