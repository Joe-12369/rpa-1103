using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Window.Params
{
    public class ParamGetWindowDataListModel : ParamBase
    {
        //[JsonProperty("keywords")]
        //public string[] Keywords { get; set; }
        //[JsonProperty("matchType")]
        //public GetWindowDataList_MatchType MatchType { get; set; } = GetWindowDataList_MatchType.Contains;
        
    }
    public enum GetWindowDataList_MatchType
    {
        Contains = 0,
        Regex = 1,
        Equals = 2
    }
}
