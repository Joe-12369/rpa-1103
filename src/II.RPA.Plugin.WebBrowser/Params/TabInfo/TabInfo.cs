using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Params.TabInfo
{
    public class MutedInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public string muted { get; set; }
    }

    public class TabInfo
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("active")]
        public string Active { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("audible")]
        public string Audible { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("AutoDiscardable")]
        public string autoDiscardable { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("discarded")]
        public string Discarded { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("favIconUrl")]
        public string FavIconUrl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("groupId")]
        public int GroupId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("height")]
        public int Height { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("highlighted")]
        public string Highlighted { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("incognito")]
        public string Incognito { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("index")]
        public int Index { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("lastAccessed")]
        public double LastAccessed { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("mutedInfo")]
        public MutedInfo MutedInfo { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("pinned")]
        public string Pinned { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("selected")]
        public string Selected { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }
        /// <summary>
        /// 百度一下，你就知道
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("width")]
        public int Width { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("windowId")]
        public int WindowId { get; set; }
    }
}
