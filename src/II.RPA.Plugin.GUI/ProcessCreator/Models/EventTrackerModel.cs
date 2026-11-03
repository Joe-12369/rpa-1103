using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.Models
{
    public struct EventTrackerModel
    {
        [JsonProperty("eventType")]
        public string EventType
        {
            get;
            set;
        }
        [JsonProperty("eventContent")]
        public object EventContent
        {
            get;
            set;
        }
        public static EventTrackerModel SetEventModel(string anEventType,object anEventContent)
        {
            return new EventTrackerModel
            {
                EventType = anEventType,
                EventContent = anEventContent,
            };
        }
        public override string ToString()
        {
            try
            {
                return JsonConvert.SerializeObject(this);
            }
            catch 
            {
                return string.Empty;
            }
        }
    }

}
