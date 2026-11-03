using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Management;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    public class HIDDrivePreCheck : SystemComponentBase<Params.ParamEmptyModel>
    {
        private static readonly ILogger _logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.HIDDriveCheck;

        protected override JMessage Execute(Params.ParamEmptyModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%Tetherscript%' OR Name LIKE '%HVDK%'");
                foreach (ManagementObject mgt in searcher.Get().Cast<ManagementObject>())
                {
                    msg.Info = BuildResultString(true);
                    msg.Code = 0;
                    return msg;
                }
                msg.Code = 1;
                msg.Info = BuildResultString(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                Exception(ex, eErrorMessageType.Error_HIDDriveCheck, ref msg);
            }
            return msg;
        }


        private static string BuildResultString(bool result)
        {
            var obj = new JObject
            {
                { "result", result }
            };
            return obj.ToString();
        }
    }
}
