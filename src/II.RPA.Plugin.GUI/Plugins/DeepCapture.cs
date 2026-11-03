using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.GUI.Deeps;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class DeepCapture : ElementAbilityBase<ParamDeepCaptureModel>
    {
        private readonly DeepInspectService _deepInspectService;
        protected override eComponentType ComponentType => eComponentType.ProcessCreator;

        public DeepCapture()
        {
            _deepInspectService = new DeepInspectService();
        }
        protected override JMessage Execute(ParamDeepCaptureModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            switch (paramModel.Action.ToLower())
            {
                case "start":
                    _deepInspectService.Start(paramModel.Payload?.ProductNames);
                    break;
                case "stop":
                    _deepInspectService.Stop();
                    break;
                default:
                    return new JMessage
                    {
                        Code = 1,
                        Info = JsonConvert.ToString(string.Empty),
                        Message = $"Unsupported action: {paramModel.Action}"
                    };
            }
            return new JMessage 
            {
                Code = 0,
                Info = JsonConvert.ToString(string.Empty),
                Message = "Deep mode action executed successfully."
            };
        }

        protected override void ValidateParameter(ParamDeepCaptureModel paramModel) {
            if (paramModel == null || string.IsNullOrEmpty(paramModel.Action))
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
            return;
        }
    }
}
