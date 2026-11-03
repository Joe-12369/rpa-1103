using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    public class HIDDriveCheck : SystemComponentBase<Params.ParamEmptyModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.HIDDriveCheck;

        protected override JMessage Execute(Params.ParamEmptyModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%Tetherscript%' OR Name LIKE '%HVDK%'");
                foreach (ManagementObject mgt in searcher.Get())
                {
                    msg.Info = "true";
                    return msg;
                }
                msg.Info = "false";
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Exception(ex, eErrorMessageType.Error_HIDDriveCheck, ref msg);
            }
            return msg;
        }

    }
}
