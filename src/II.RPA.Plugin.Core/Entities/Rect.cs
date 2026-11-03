using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.Entities
{
    public class Rect
    {
        public string x { get; set; }
        public string y { get; set; }
        public string width { get; set; }
        public string height { get; set; }
    }
    public class RpaRect
    {
        public int x { get; set; }
        public int y { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        [JsonIgnore]
        public Point Center => new Point(this.width / 2 + this.x, this.height / 2 + this.y);

    }
    public struct WebBrowserRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
    public struct POINT
    {
        public int X;
        public int Y;
    }
}
