using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.DriverInput.Params;
using II.RPA.WinAutomation.CommonUtility;
using InputInterceptorNS;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.DriverInput.Plugins
{
    public class DriverSearch : PluginAbilityBase<ParamDriverSearchModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.DriverInput;

        protected override JMessage Execute(ParamDriverSearchModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();

            try
            {
                if (!InputInterceptor.CheckDriverInstalled()) 
                {
                    logger.LogError("Interceptor is not installed");
                    Exception(new Exception("Interceptor is not installed"), eErrorMessageType.Error_InterceptorUnInstalled, ref msg);
                }
                List<string> DeviceNameList = new List<string>();
                DeviceNameList.Add("默认");
                try
                {
                    InputInterceptor.Initialize();
                    List<DeviceData> DeviceList = InputInterceptor.GetDeviceDatas();
                    DeviceList.ForEach(X => {
                        DeviceNameList.Add(X.CompositeName.Replace(@"\",@"/"));
                    });
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    InputInterceptor.Dispose();
                }
                
               
                msg.Info = DeviceNameList.ToJson();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "DriverSearch Err");
                Exception(ex, eErrorMessageType.Error_ActiveWindow, ref msg);
            }

            return msg;
        }

        protected override void ValidateParameter(ParamDriverSearchModel paramModel)
        {
        }
    }
}

