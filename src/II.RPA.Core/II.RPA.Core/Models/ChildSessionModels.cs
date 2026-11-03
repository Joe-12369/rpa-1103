using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Models
{
    public class NamedPipeDataModel
    {
        [JsonProperty("from")]
        public string From
        {
            get;
            set;
        } = string.Empty;
        [JsonProperty("fromSession")]
        public int FromSessionId
        {
            get;
            set;
        } = 0;
        [JsonProperty("to")]
        public string To
        {
            get;
            set;
        } = string.Empty;
        [JsonProperty("pipeName")]
        public string PipeName
        {
            get;
            set;
        } = string.Empty;
        [JsonProperty("type")]
        public ActionDataType ActionTypeEnum
        {
            get;
            set;
        } = ActionDataType.UnKnown;
        [JsonProperty("dataLength")]
        public int DataLength
        {
            get;
            set;
        } = 0;
        [JsonProperty("appPathToRun")]
        public string AppPathToRun
        {
            get;
            set;
        } = string.Empty;
        [JsonProperty("data")]
        public object DataContent
        {
            get;
            set;
        } = string.Empty;
    }
    public enum ActionDataType
    {
        UnKnown = 99,
        SessionHubStatusNotify = 1,
        RunRPAEngine = 2,
        ZDeskTopClose = 3,
    }
}
