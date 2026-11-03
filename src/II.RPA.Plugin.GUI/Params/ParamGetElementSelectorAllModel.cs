
namespace II.RPA.Plugin.GUI.Params
{
    public class ParamGetElementSelectorAllModel : ParamBase
    {
        public bool isHighlighting { get; set; } = false;
        public string cvExecutionServiceURL { get; set; } = "";
        public int timeout { get; set; } = 10000;
    }
}
