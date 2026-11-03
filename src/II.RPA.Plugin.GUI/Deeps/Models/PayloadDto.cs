using Newtonsoft.Json;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    public class PayloadDto
    {
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
        [JsonProperty("code")]
        public int Code { get; set; } = 0;

        public static PayloadDto Failed(string message = "Failed")
        {
            return new PayloadDto { Code = 1, Message = message };
        }
        public static PayloadDto Success(string message = "Success")
        {
            return new PayloadDto { Code = 0, Message = "Success" };
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
