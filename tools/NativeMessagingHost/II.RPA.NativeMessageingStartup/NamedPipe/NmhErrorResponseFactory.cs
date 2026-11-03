using Newtonsoft.Json.Linq;

namespace II.RPA.NativeMessageingStartup.NamedPipe
{
    internal static class NmhErrorResponseFactory
    {
        private const int FailureCode = 500;

        internal static TransferMessage ApplyError(TransferMessage request, int errorCode, string message)
        {
            request.CallbackReslut = new JObject
            {
                ["code"] = FailureCode,
                ["status"] = "error",
                ["result"] = new JObject
                {
                    ["content"] = null,
                    ["error"] = new JObject
                    {
                        ["code"] = errorCode,
                        ["message"] = message
                    }
                }
            };
            return request;
        }
    }
}
