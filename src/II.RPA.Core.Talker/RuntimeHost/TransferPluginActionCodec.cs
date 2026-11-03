using II.RPA.Core.Log;
using II.RPA.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.Core.Talker.RuntimeHost;

internal static class TransferPluginActionCodec
{
    public static bool TryResolveRequest(
        string requestJson,
        ILogger logger,
        out TransferPluginAction action)
    {
        try
        {
            var parameters = JsonConvert.DeserializeObject<TransferPluginActonParams>(requestJson);
            if (parameters == null || string.IsNullOrWhiteSpace(parameters.PluginAction))
            {
                logger.LogError($"Invalid transferPluginAction request: {requestJson}");
                action = default;
                return false;
            }

            action = new TransferPluginAction(
                parameters.PluginAction,
                "Excute",
                parameters.JsonParams ?? string.Empty);
            return true;
        }
        catch (JsonException ex)
        {
            logger.LogError($"Failed to deserialize transferPluginAction request: {ex.Message}");
            action = default;
            return false;
        }
    }

    public static TransferPluginResult ResolveResult(string resultJson, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(resultJson))
        {
            return new TransferPluginResult("99", "internal error", null);
        }

        try
        {
            var result = JObject.Parse(resultJson);
            logger.LogDebug(resultJson);
            return new TransferPluginResult(
                result.Value<string>("code") ?? "1",
                result.Value<string>("message") ??
                    "返回值解析失败，请重试。若仍未解决，请联系运营人员。",
                result["info"]?.ToString());
        }
        catch (JsonReaderException)
        {
            logger.LogError($"Fail to parse json:{resultJson}");
            return new TransferPluginResult(
                "1",
                "返回值解析失败，请重试。若仍未解决，请联系运营人员。",
                "{}");
        }
    }
}

internal readonly record struct TransferPluginAction(
    string PluginName,
    string PluginAction,
    string JsonParameters);

internal readonly record struct TransferPluginResult(
    string Code,
    string Message,
    string? Result);
