using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.UI;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class SharpUIProvider : ElementAbilityBase<ParamSharpUIProviderModel>
    {
        protected override eComponentType ComponentType =>  eComponentType.SharpUIProivder;
        private static readonly ILogger logger = Logger.OfContext();
        private readonly UIProvider m_UIProvider;

        public SharpUIProvider() 
        {
            m_UIProvider = new UIProvider();
        }

        protected override JMessage Execute(ParamSharpUIProviderModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            try
            {
                logger.LogDebug($"{paramModel.Title};{paramModel.Content};{paramModel.Level}");
                m_UIProvider.ShowTips(paramModel.Title, paramModel.Content, paramModel.Level);
            }
            catch(Exception ex) 
            {
                logger.LogError(ex);
            }
            //此action忽略异常、前端不需要关注异常
            return new JMessage
            {
                Code = 0,
                Message = "ShowTips Over",
                Info = ""
            };
        }

        protected override void ValidateParameter(ParamSharpUIProviderModel paramModel) {
            if (!string.IsNullOrEmpty(paramModel.ToJson()))
            {
                return;
            }
            throw new RPAException(eErrorMessageType.Error_ParamsError);
        }
    }
}
