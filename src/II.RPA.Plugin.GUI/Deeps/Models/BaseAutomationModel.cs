using FlaUI.Core.AutomationElements;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    public abstract class BaseAutomationModel
    {
        [JsonProperty("elementId")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("tag")]
        public string Tag { get; set; } = string.Empty;

        [JsonProperty("elementIcon")]
        public string Icon { get; set; } = string.Empty;

        [JsonProperty("children", NullValueHandling = NullValueHandling.Ignore)]
        public List<ElementModel> Children { get; set; } = new List<ElementModel>();

        //[JsonProperty("handle")]
        [JsonIgnore]
        public IntPtr? Handle { get; set; }

        //[JsonProperty("processId")]
        [JsonIgnore]
        public int ProcessId { get; set; }

        //[JsonProperty("admin")]
        [JsonIgnore]
        public bool IsAdmin { get; set; }

        //[JsonProperty("higher")]
        [JsonIgnore]
        public bool IsHigher { get; set; }

        //[JsonProperty("integrityLevel")]
        [JsonIgnore]
        public int IntegrityLevel { get; set; }

        //[JsonProperty("integrityName")]
        [JsonIgnore]
        public string IntegrityName { get; set; } = string.Empty;

        [JsonIgnore]
        public Rectangle Region { get; set; } = Rectangle.Empty;

        [JsonIgnore]
        //[JsonProperty("rect")]
        public string RegionText => $"{Region.Left},{Region.Top},{Region.Width},{Region.Height}";

        [JsonIgnore]
        public AutomationElement Automation { get; protected set; }
    }
}
