using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.IPC.NamedPipe.Abstractions;
using Newtonsoft.Json;

namespace II.RPA.NativeMessageingStartup.NamedPipe
{
    internal sealed class NamedPipeMessageHandler : INamedPipeRequestHandler
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public async Task<byte[]> HandleRequestAsync(
            NamedPipeRequestContext context,
            byte[] request,
            CancellationToken cancellationToken)
        {
            TransferMessage message = ParseRequest(request);

            if (string.IsNullOrWhiteSpace(message.Action))
            {
                return SerializeError(message, NmhErrorCodes.UnprocessableMessage, "The transfer message cannot be processed.");
            }

            try
            {
                TransferDispatchResult result = await MessageService.DispatchTransferMessageAsync(
                    message,
                    cancellationToken).ConfigureAwait(false);
                return SerializeResult(message, result);
            }
            catch (IOException)
            {
                return SerializeError(message, NmhErrorCodes.CrxUnavailable, "The CRX connection is unavailable.");
            }
            catch (Exception)
            {
                return SerializeError(message, NmhErrorCodes.InternalError, "An internal transfer error occurred.");
            }
        }

        private static TransferMessage ParseRequest(byte[] request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            string json = StrictUtf8.GetString(request);
            TransferMessage message = JsonConvert.DeserializeObject<TransferMessage>(json);
            if (message == null || string.IsNullOrWhiteSpace(message.MessageId))
            {
                throw new JsonSerializationException("A TransferMessage with a MessageId is required.");
            }

            return message;
        }

        private static byte[] SerializeResult(TransferMessage request, TransferDispatchResult result)
        {
            if (result == null)
            {
                return SerializeError(request, NmhErrorCodes.InternalError, "The transfer result is unavailable.");
            }

            if (result.Status == TransferDispatchStatus.MessageIdConflict)
            {
                return SerializeError(request, NmhErrorCodes.MessageIdConflict, "The MessageId is already in use.");
            }

            if (result.Status == TransferDispatchStatus.Expired)
            {
                return SerializeError(request, NmhErrorCodes.RequestExpired, "The request has already expired.");
            }

            if (result.Response != null)
            {
                return Serialize(result.Response);
            }

            switch (result.CompletionReason)
            {
                case TransferCompletionReason.CrxTimeout:
                    return SerializeError(request, NmhErrorCodes.CrxTimeout, "The CRX response timed out.");
                case TransferCompletionReason.ConnectionClosed:
                    return SerializeError(request, NmhErrorCodes.ConnectionClosed, "The client connection was closed.");
                case TransferCompletionReason.HostStopping:
                    return SerializeError(request, NmhErrorCodes.HostStopping, "The native messaging host is stopping.");
                default:
                    return SerializeError(request, NmhErrorCodes.InternalError, "The transfer did not produce a response.");
            }
        }

        private static byte[] SerializeError(TransferMessage request, int errorCode, string message)
        {
            return Serialize(NmhErrorResponseFactory.ApplyError(request, errorCode, message));
        }

        private static byte[] Serialize(TransferMessage message)
        {
            return StrictUtf8.GetBytes(JsonConvert.SerializeObject(message));
        }
    }
}
