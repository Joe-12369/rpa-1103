using II.RPA.Plugin.DriverInput.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DriverInput.WinRing0
{
    internal class WinRing0InputManager
    {
        public void DriverInput(ParamDriverInputModel _ParamDriverInputModel)
        {
            ControlKey CControlKey = new ControlKey();
            CControlKey.InitEnvironment((int)_ParamDriverInputModel.InputInterval, _ParamDriverInputModel.KeyDownDuration);
            CControlKey.InputText(_ParamDriverInputModel.InputContent);
            CControlKey.DeinitEnvironment();
        }
    }
}
