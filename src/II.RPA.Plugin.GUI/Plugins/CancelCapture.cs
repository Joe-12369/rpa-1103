using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class CancelCapture : StaElementAbilityBase<ParamCancelCaptureModel>
    {
        protected override eComponentType ComponentType => eComponentType.CancelCapture;
        private static readonly ILogger logger = Logger.OfContext();
        public CancelCapture(IStaDispatcher staDispatcher)
            : base(staDispatcher)
        {
        }
        protected override JMessage Execute(ParamCancelCaptureModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            try
            {
                GuiElement.overlayForm?.KeyCancel();
            }
            catch(Exception ex)
            {
                logger.LogError(ex);
            }
            return new JMessage
            {
                Code = 0,
                Message = "CancelCapture Over",
                Info = ""
            };
        }

        protected override void ValidateParameter(ParamCancelCaptureModel paramModel) {
            //此action目前无入参
            return;
        }
    }
}
