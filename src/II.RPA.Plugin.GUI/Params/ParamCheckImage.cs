using II.RPA.Plugin.GUI.ScreenShot;
using Newtonsoft.Json;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamCheckImage : ParamBase
    {
        [JsonProperty("inParameter")]
        public InParameter InParameter { get; set; }
    }
    public class InParameter
    {
        [JsonProperty("imgSrc")]
        public string ImageSource { get; set; }
        [JsonProperty("thr")]
        public float Similarity { get; set; }
        [JsonProperty("resolution")]
        public Resolution Resolution { get; set; }
        [JsonProperty("scopeRect")]
        public ScopeRect Rect { get; set; }
    }

    public struct ScopeRect
    {
        [JsonProperty("startX")]
        public float StartX { get; set; }
        [JsonProperty("startY")]
        public float StartY { get; set; }
        [JsonProperty("endX")]
        public float EndX { get; set; }
        [JsonProperty("endY")]
        public float EndY { get; set; }

        public bool IsValid()
        {
            if (EndX - StartX <= 0 || EndY - StartY <= 0)
            {
                return false;
            }
            return true;
        }
    }
}
