using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NativeMessageingStartup
{
    internal class NativeMessage
    {
        [JsonProperty("method")]
        public string Method { get; set; }
        [JsonProperty("params")]
        public JObject Params { get; set; }
    }
    public class WindowInfo
    {
        public long wndId { get; set; }
        public string url { get; set; } = "";
        public string title { get; set; } = "";
        public int left { get; set; }
        public int top { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int right { get; set; }
        public int bottom { get; set; }
        internal Rectangle GetRectangle()
        {
            int left = this.left;
            int top = this.top;
            int right = this.right;
            int bottom = this.bottom;
            return new Rectangle(left, top, right - left, bottom - top);
        }
    }
}
