using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamScreenShotExecutorModel : ParamBase
    {
        [JsonProperty("pictureSource")]
        public int PictureSource
        {
            get;
            set;
        }
        [JsonProperty("shotRange")]
        public int ShotRange
        {
            get;
            set;
        }
        [JsonProperty("imagePath")]
        public string ImagePath
        {
            get;
            set;
        }
        [JsonProperty("position")]
        public RectangleModel Position
        {
            get;
            set;
        }
        [JsonProperty("outImagePath")]
        public string OutImagePath
        {
            get;
            set;
        }
        [JsonProperty("windowObj")]
        public string WindowObj
        {
            get;
            set;
        }
        [JsonProperty("picUiSelector")]
        public string PicUiSelector
        {
            get;
            set;
        }
    }
}
