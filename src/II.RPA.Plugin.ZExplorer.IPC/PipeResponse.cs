using Newtonsoft.Json;
using System;

namespace II.RPA.Plugin.ZExplorer.IPC
{
    [Serializable]
    public class PipeResponse : MarshalByRefObject
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
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class PipeResult
    {
        private object _content;
        [JsonProperty("content")]
        public object Content
        {
            get { return _content; }
            set
            {
                _content = value ?? "";
            }
        }

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
    public class JSResponse
    {
        public int code { get; set; } = 0;
        public object result { get; set; }
    }
    public class JSResponse_CrossDomain
    {
        public int code { get; set; } = -1;
        public Frame next { get; set; }
    }
    public class Frame
    {
        public string frameIndex { get; set; }

        [JsonProperty("params")]
        public object param { get; set; }
    }
}
