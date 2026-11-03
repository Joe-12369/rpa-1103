using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.Core
{
    public class PipeResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("result")]
        public PipeResult Result { get; set; }

        public PipeResponse()
        {
            Result = new PipeResult();
        }
    }

    public class PipeResult
    {
        [JsonProperty("content")]
        public JToken Content { get; set; }

        [JsonProperty("error")]
        public RPCResponseError Error { get; set; }

        public PipeResult()
        {
            Error = new RPCResponseError();
        }
    }
    public class RPCResponseError
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }

    }
}
