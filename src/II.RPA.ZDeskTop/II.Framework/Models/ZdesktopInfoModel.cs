using Newtonsoft.Json;

namespace II.Framework.Models
{
    public struct ZdesktopInfoModel
    {
        public ZdesktopInfoModel(Rect rect,
            Status status,
            Mode mode,
            bool visible,
            int pid)
        {
            Rect = rect;
            Status = status;
            Visible = visible;
            Pid = pid;
            Mode = mode;
        }

        [JsonProperty("rect")]
        public Rect Rect
        {
            get;
            private set;
        }

        [JsonProperty("visible")]
        public bool Visible
        {
            get;
            private set;
        }
        [JsonProperty("pid")]
        public int Pid
        {
            get;
            private set;
        }

        [JsonProperty("status")]
        public Status Status
        {
            get;
            private set;
        }

        [JsonProperty("mode")]
        public Mode Mode
        {
            get;
            private set;
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
    public enum Mode : short
    {
        Normal = 1,
        Agent = 2,
    }
    public enum Status : short
    {
        [JsonProperty("starting")]
        Starting = 1,
        [JsonProperty("running")]
        Running = 2,
        [JsonProperty("exited")]
        Exited = 3
    }
    public struct Rect
    {
        [JsonProperty("x")]
        public int X
        {
            get;
            set;
        }
        [JsonProperty("y")]
        public int Y
        {
            get;
            set;
        }
        [JsonProperty("w")]
        public int W
        {
            get;
            set;
        }
        [JsonProperty("h")]
        public int H
        {
            get;
            set;
        }
    }
}
