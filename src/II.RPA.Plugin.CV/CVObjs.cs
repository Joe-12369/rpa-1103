using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.CV
{
    public class CVMessage
    {
        public string message { get; set; }
        public object result { get; set; }
        public string status { get; set; }
        public string text { get; set; } = string.Empty;
        [JsonProperty("wnd_info")]
        public WndInfo WndInfo { get; set; }

    }
    public class CVResult
    {
        [JsonProperty("wnd_info")]
        public WndInfo WndInfo { get; set; }
        [JsonProperty("action")]
        public AgentCVInfo Action { get; set; }
    }
    public class WndInfo
    {
        [JsonProperty("area")]
        public List<int> Area { get; set; }
        [JsonProperty("handle")]
        public IntPtr Handle { get; set; }
    }
    public class CVObjs
    {
        public CVObjs()
        {
            TextResults = new List<CVItem>();
        }

        public List<CVItem> TextResults { get; set; }
    }

    public class CVPoint
    {
        public CVPoint(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public float X { get; set; }
        public float Y { get; set; }
    }

    public class CVShape
    {
        public CVShape()
        {
            Points = new List<CVPoint>();
        }

        public List<CVPoint> Points { get; set; }
    }

    public class CVTextLine
    {
        public CVTextLine()
        {
            Shape = new List<CVShape>();
        }

        public List<CVShape> Shape { get; set; }
    }
    public class AgentCVInfo
    {
        [JsonProperty("elements")]
        public List<List<int>> elementsPoint { get; set; }
        public List<string> ele_ids { get; set; }
        public string action_type { get; set; }
        public string content { get; set; }
        public string message { get; set; }
        public string action_description { get; set; }
        public List<List<int>> absElementsPoint { get; set; }
        public List<string> model_output { get; set; }
    }
}