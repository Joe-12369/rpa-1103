using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamBase : MobileElementValidationOptions
    {
        /// <summary>
        /// default
        /// id
        /// description
        /// name
        /// xpath
        /// </summary>
        [JsonProperty("queryMode")]
        public string QueryMode { get; set; }
        public int timeout { get; set; }

    }
}
