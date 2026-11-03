using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI
{
    public class ReleaseGuiResources : ElementAbilityBase<Params.EmptyElementParameter>
    {
        private ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ReleaseGuiResources;

        protected override JMessage Execute(Params.EmptyElementParameter paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            GuiElement.ReleaseGuiResources();
            logger.LogInformation("ReleaseGuiResources");
            return msg;
        }

        protected override void ValidateParameter(Params.EmptyElementParameter paramModel) {
            return;
        }
    }
}
