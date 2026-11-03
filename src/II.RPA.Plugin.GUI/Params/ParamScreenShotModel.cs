using Newtonsoft.Json;
using SharpVectors.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Params
{
    public class ParamScreenShotModel: ParamBase
    {
        /// <summary>
        /// run/manualShot
        /// </summary>
        [JsonProperty("action")]
        public string Action
        {
            get;
            set;
        }
        /// <summary>
        /// screen/file
        /// </summary>
        [JsonProperty("source")]
        public string Source
        {
            get;
            set;
        }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("imagePath")]
        public string ImagePath
        {
            get;
            set;
        }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("rectangleRegion")]
        public RectangleModel RectangleRegion
        {
            get;
            set;
        }
        [JsonProperty("packagepath")]
        public string PackagePath
        {
            get;
            set;
        }
    }
    public struct RectangleModel
    {
        [JsonProperty("x")]
        public int X
        {
            get;
            set;
        }
        [JsonProperty("y")]
        public int Y
        {
            get;
            set;
        }
        [JsonProperty("w")]
        public int Width
        {
            get;
            set;
        }
        [JsonProperty("h")]
        public int Height
        {
            get;
            set;
        }
    }
}
