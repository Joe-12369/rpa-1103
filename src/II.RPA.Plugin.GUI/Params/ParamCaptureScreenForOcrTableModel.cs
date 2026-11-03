using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamCaptureScreenForOcrTableModel : ParamBase
    {
        public string imgPath { get; set; }
    }


    /// <summary>
    /// 出参的模型
    /// </summary>
    public class ScreenShootStruct
    {
        public bool IsValid = false;

        public string Base64 = string.Empty;
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double ScaleX { get; set; }//缩放倍率 1, 1.25, 1.5, 1.75
        public double ScaleY { get; set; }//缩放倍率 1, 1.25, 1.5, 1.75
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }

        public Dictionary<string, object> GetKeyValuePairs()
        {
            Dictionary<string, object> _KeyValuePairs = new Dictionary<string, object>();
            _KeyValuePairs.Add("x", StartX);
            _KeyValuePairs.Add("y", StartX);
            _KeyValuePairs.Add("width", StartX);
            _KeyValuePairs.Add("height", StartX);
            _KeyValuePairs.Add("screenWidth", ScreenWidth);
            _KeyValuePairs.Add("screenHeight", ScreenHeight);
            _KeyValuePairs.Add("scale", StartX);

            return _KeyValuePairs;
        }
    }
}
