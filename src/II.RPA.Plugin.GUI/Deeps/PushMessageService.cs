using II.RPA.Logging;
using II.RPA.Plugin.Core.Services;
using II.RPA.Plugin.GUI.Deeps.Actions;
using II.RPA.Plugin.GUI.Deeps.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Deeps
{
    public class PushMessageService
    {
        private static readonly ILogger _logger = Logger.OfContext();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingResponses
                    = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
        private Task<string> SpeakAsync(string action, string payload)
        {
            var requestId = Utils.GenerateRequestId();
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            _pendingResponses.TryAdd(requestId, tcs);

            RuntimeMessageService.SendMessage(new RuntimeMessageModel
            {
                Action = action,
                Payload = payload,
                RequestId = requestId
            });

            return tcs.Task;
        }
        public bool TryHandleResponse(RuntimeMessageModel response)
        {
            if (_pendingResponses.TryRemove(response.RequestId, out var tcs))
            {
                _logger.LogDebug($"receive response: {response.RequestId}");
                tcs.SetResult(response.Payload);
                return true;
            }
            return false;
        }
        public Task NotifyPeerElementAsync(UiaTreeResultDto peerElement)
        {
            try
            {
                var payload = JsonConvert.SerializeObject(peerElement);
                return SpeakAsync(ActionNames.PeerElement, payload);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, "NotifyPeerElementAsync failed!");
                return Task.CompletedTask;
            }
        }


    }
}

