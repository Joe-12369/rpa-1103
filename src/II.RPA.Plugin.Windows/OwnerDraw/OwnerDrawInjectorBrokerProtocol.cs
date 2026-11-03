using System;
using System.Globalization;
using System.Text;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    internal enum OwnerDrawInjectorBrokerOperation
    {
        Inspect,
        Start,
        Stop,
    }

    internal sealed class OwnerDrawInjectorBrokerRequest
    {
        internal OwnerDrawInjectorBrokerOperation Operation { get; set; }
        internal int TargetProcessId { get; set; }
        internal int ControllerProcessId { get; set; }
        internal int RequiredAbi { get; set; }
        internal string BridgePath { get; set; }
        internal string LegacyFileName { get; set; }
    }

    internal sealed class OwnerDrawInjectorBrokerResponse
    {
        internal OwnerDrawInjectorBrokerOperation Operation { get; set; }
        internal bool Success { get; set; }
        internal string Code { get; set; }
        internal string Value { get; set; }
        internal string Status { get; set; }
    }

    /// <summary>
    /// Dependency-free, one-request/one-response command-line contract used by the
    /// short-lived x86 Broker. Paths and diagnostic text are Base64 encoded; the
    /// remaining tokens are a fixed ASCII grammar and never pass through a shell.
    /// </summary>
    internal static class OwnerDrawInjectorBrokerProtocol
    {
        internal const string RequestVersion = "ODIB1";
        internal const string ResponseVersion = "ODIBR1";
        internal const int MaximumEncodedFieldLength = 262144;

        internal static string BuildArguments(OwnerDrawInjectorBrokerOperation operation,
            int targetProcessId, int controllerProcessId, string bridgePath,
            string legacyFileName, int requiredAbi)
        {
            if (!Enum.IsDefined(typeof(OwnerDrawInjectorBrokerOperation), operation)
                || targetProcessId <= 0 || controllerProcessId <= 0
                || string.IsNullOrEmpty(bridgePath)
                || string.IsNullOrEmpty(legacyFileName) || requiredAbi <= 0)
            {
                throw new ArgumentException("Broker request contains an invalid field.");
            }

            return RequestVersion + " " + operation + " "
                + targetProcessId.ToString(CultureInfo.InvariantCulture) + " "
                + controllerProcessId.ToString(CultureInfo.InvariantCulture) + " "
                + requiredAbi.ToString(CultureInfo.InvariantCulture) + " "
                + Encode(bridgePath) + " " + Encode(legacyFileName);
        }

        internal static bool TryParseRequest(string[] args,
            out OwnerDrawInjectorBrokerRequest request, out string status)
        {
            request = null;
            if (args == null || args.Length != 7
                || !string.Equals(args[0], RequestVersion, StringComparison.Ordinal)
                || !Enum.TryParse(args[1], false,
                    out OwnerDrawInjectorBrokerOperation operation)
                || !Enum.IsDefined(typeof(OwnerDrawInjectorBrokerOperation), operation)
                || !string.Equals(args[1], operation.ToString(),
                    StringComparison.Ordinal)
                || !TryParsePositiveInt(args[2], out int targetProcessId)
                || !TryParsePositiveInt(args[3], out int controllerProcessId)
                || !TryParsePositiveInt(args[4], out int requiredAbi)
                || !TryDecode(args[5], out string bridgePath)
                || !TryDecode(args[6], out string legacyFileName))
            {
                status = "Broker request does not match the fixed ODIB1 grammar.";
                return false;
            }

            request = new OwnerDrawInjectorBrokerRequest
            {
                Operation = operation,
                TargetProcessId = targetProcessId,
                ControllerProcessId = controllerProcessId,
                RequiredAbi = requiredAbi,
                BridgePath = bridgePath,
                LegacyFileName = legacyFileName,
            };
            status = "Broker request parsed.";
            return true;
        }

        internal static string SerializeResponse(
            OwnerDrawInjectorBrokerOperation operation, bool success,
            string code, string value, string status)
        {
            if (!Enum.IsDefined(typeof(OwnerDrawInjectorBrokerOperation), operation))
                throw new ArgumentOutOfRangeException("operation");
            code = SanitizeToken(code, "Unknown");
            value = SanitizeToken(value, "None");
            return ResponseVersion + "|" + operation + "|"
                + (success ? "1" : "0") + "|" + code + "|" + value
                + "|" + Encode(status ?? string.Empty);
        }

        internal static bool TryParseResponse(string output,
            OwnerDrawInjectorBrokerOperation expectedOperation,
            out OwnerDrawInjectorBrokerResponse response, out string status)
        {
            response = null;
            if (string.IsNullOrEmpty(output)
                || output.Length > MaximumEncodedFieldLength)
            {
                status = "Broker response is empty or exceeds the size limit.";
                return false;
            }

            string line = output.Replace("\r\n", "\n");
            if (line.EndsWith("\n", StringComparison.Ordinal))
                line = line.Substring(0, line.Length - 1);
            if (line.IndexOf('\n') >= 0 || line.IndexOf('\r') >= 0)
            {
                status = "Broker emitted extra or malformed output.";
                return false;
            }

            string[] fields = line.Split('|');
            if (fields.Length != 6
                || !string.Equals(fields[0], ResponseVersion,
                    StringComparison.Ordinal)
                || !Enum.TryParse(fields[1], false,
                    out OwnerDrawInjectorBrokerOperation operation)
                || !Enum.IsDefined(typeof(OwnerDrawInjectorBrokerOperation), operation)
                || !string.Equals(fields[1], operation.ToString(),
                    StringComparison.Ordinal)
                || operation != expectedOperation
                || (fields[2] != "0" && fields[2] != "1")
                || !IsSafeToken(fields[3]) || !IsSafeToken(fields[4])
                || !TryDecode(fields[5], out string decodedStatus))
            {
                status = "Broker response does not match the fixed ODIBR1 grammar.";
                return false;
            }

            response = new OwnerDrawInjectorBrokerResponse
            {
                Operation = operation,
                Success = fields[2] == "1",
                Code = fields[3],
                Value = fields[4],
                Status = decodedStatus,
            };
            status = "Broker response parsed.";
            return true;
        }

        private static bool TryParsePositiveInt(string value, out int parsed)
        {
            return int.TryParse(value, NumberStyles.None,
                CultureInfo.InvariantCulture, out parsed) && parsed > 0;
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static bool TryDecode(string encoded, out string value)
        {
            value = string.Empty;
            if (encoded == null || encoded.Length > MaximumEncodedFieldLength)
                return false;
            try
            {
                byte[] bytes = Convert.FromBase64String(encoded);
                var utf8 = new UTF8Encoding(false, true);
                value = utf8.GetString(bytes);
                return value.IndexOf('\0') < 0;
            }
            catch (Exception ex) when (ex is FormatException
                || ex is DecoderFallbackException)
            {
                return false;
            }
        }

        private static string SanitizeToken(string token, string fallback)
        {
            return IsSafeToken(token) ? token : fallback;
        }

        private static bool IsSafeToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length > 64)
                return false;
            for (int i = 0; i < token.Length; ++i)
            {
                char ch = token[i];
                if (!((ch >= 'A' && ch <= 'Z')
                    || (ch >= 'a' && ch <= 'z')
                    || (ch >= '0' && ch <= '9') || ch == '_'))
                    return false;
            }
            return true;
        }
    }
}
