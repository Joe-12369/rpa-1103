using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction
{
    [ClientActionName("checkDrive")]
    public class CheckDriveProvider : BaseActionProvider
    {
        public CheckDriveProvider(ILogger aLogger) : base(aLogger)
        {

        }
        public CheckDriveProvider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {

        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "RPASystem.HIDDriveCheck",
            };
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseval = base.ResolveRetVal(aRetValFromPlugin);
            if (baseval.Code == "0")
            {
                if (baseval.Result == "false")
                {
                    baseval.Code = "1";
                    baseval.Message = "HID驱动未安装";
                }
                else
                {
                    baseval.Code = "0";
                    baseval.Message = "已安装";
                }
            }
            return baseval;
        }
    }
}
