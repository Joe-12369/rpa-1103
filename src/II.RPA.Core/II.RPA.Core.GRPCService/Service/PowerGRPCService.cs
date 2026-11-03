using Grpc.Core;
using Power.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;

namespace II.RPA.Core.GRPCService.Service
{
    public class PowerGRPCService : PowerService.PowerServiceBase, IGRPCService
    {
        public PowerGRPCService()
        {
        }
        public override Task<PowerResponse> PowerUnary(PowerRequest request, ServerCallContext context)
        {
            return Task.FromResult(CreateResponse(request));
        }
        public override async Task<PowerResponse> PowerStreamingFromClient(IAsyncStreamReader<PowerRequest> requestStream, ServerCallContext context)
        {
            PowerRequest lastRequest = null;
            int streamCount = 0;
            while (await requestStream.MoveNext(context.CancellationToken).ConfigureAwait(false))
            {
                lastRequest = requestStream.Current;
                streamCount++;
            }

            if (lastRequest == null)
            {
                throw new InvalidOperationException("No client requests received.");
            }
            return CreateResponse(lastRequest, streamCount);
        }
        public override async Task PowerPingPongStreaming(IAsyncStreamReader<PowerRequest> requestStream, IServerStreamWriter<PowerResponse> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext(context.CancellationToken).ConfigureAwait(false))
            {
                await responseStream.WriteAsync(CreateResponse(requestStream.Current));
            }
        }
        public override async Task PowerStreamingFromServer(PowerRequest request, IServerStreamWriter<PowerResponse> responseStream, ServerCallContext context)
        {
            var response = CreateResponse(request);
            //while (!context.CancellationToken.IsCancellationRequested)
            //{
            //    await responseStream.WriteAsync(response);
            //}
            int i = 1;
            while (i < 10)
            {
                await responseStream.WriteAsync(response);
                i++;
            }
        }
        public override async Task PowerStreamingTwoWays(IAsyncStreamReader<PowerRequest> requestStream, IServerStreamWriter<PowerResponse> responseStream, ServerCallContext context)
        {
            var response = new PowerResponse
            {
                Payload = new Payload { Body = UnsafeByteOperations.UnsafeWrap(new byte[100]) }
            };
            var readTask = ReadClientRequests(requestStream, context.CancellationToken);
            try
            {
                while (!readTask.IsCompleted)
                {
                    await responseStream.WriteAsync(response);
                    var completed = await Task.WhenAny(readTask, Task.Delay(100, context.CancellationToken));
                    if (completed == readTask)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await readTask;
            }
        }

        private static async Task ReadClientRequests(IAsyncStreamReader<PowerRequest> requestStream, System.Threading.CancellationToken cancellationToken)
        {
            while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                // Intentionally ignore payloads and keep method to signal stream completion.
            }
        }
        private static PowerResponse CreateResponse(PowerRequest request, int StreamCount = 1)
        {
            var body = UnsafeByteOperations.UnsafeWrap(new byte[request.ResponseSize]);
            var payload = new Payload { Body = body };
            return new PowerResponse
            {
                Payload = payload,
                Username = $"yisili: {StreamCount}",
                ServerId = $"{DateTime.Now}"
            };
        }

        public ServerServiceDefinition BindingToService()
        {
            return PowerService.BindService(this);
        }
    }
}
