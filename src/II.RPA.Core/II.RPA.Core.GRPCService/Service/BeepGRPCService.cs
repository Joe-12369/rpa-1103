using Grpc.Core;
using Grpc.Core.Interceptors;
using II.RPA.Core.Abstract;
using II.RPA.Core.Log;
using Newtonsoft.Json;
using Rpa.Core.Beep;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPCService.Service
{
    /// <summary>
    /// 心跳服务
    /// 目前仅使用pingpong实现心跳
    /// </summary>
    public class BeepGRPCService:BeepService.BeepServiceBase,IGRPCService
    {
        private readonly Interceptor m_ServicePollyInterceptor;
        private readonly ILogger m_Logger;

        public BeepGRPCService()
        {

        }
        public BeepGRPCService(ILogger aLogger, Interceptor anInterceptor):this()
        {
            m_Logger = aLogger;
            m_ServicePollyInterceptor = anInterceptor;
        }
        
        public override async Task BeepPingPongStreaming(IAsyncStreamReader<Beep> requestStream, IServerStreamWriter<Beep> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext(context.CancellationToken).ConfigureAwait(false))
            {
                var currentRequest = requestStream.Current;
                m_Logger.LogDebug($"Beep {currentRequest.Payload.ClientId}");
                await responseStream.WriteAsync(CreateResponse(currentRequest));
                await Task.Delay(100, context.CancellationToken);
            }
        }
        #region 暂时不用
        public override Task<Beep> BeepUnary(Beep request, ServerCallContext context)
        {
            return Task.FromResult(CreateResponse(request));
        }
        public override async Task<Beep> BeepStreamingFromClient(IAsyncStreamReader<Beep> requestStream, ServerCallContext context)
        {
            Beep lastRequest = null;
            int streamCount = 0;
            while (await requestStream.MoveNext(context.CancellationToken).ConfigureAwait(false))
            {
                lastRequest = requestStream.Current;
                streamCount++;
                await Task.Delay(100, context.CancellationToken);
            }

            if (lastRequest == null)
            {
                throw new InvalidOperationException("No client requests received.");
            }
            return CreateResponse(lastRequest, streamCount);
        }
        public override async Task BeepStreamingFromServer(Beep request, IServerStreamWriter<Beep> responseStream, ServerCallContext context)
        {
            var response = CreateResponse(request);
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await responseStream.WriteAsync(response);
                await Task.Delay(200, context.CancellationToken);
            }
        }
        #endregion
        #region IGRPCService
        public ServerServiceDefinition BindingToService()
        {
            return BeepService.BindService(this);
            ///心跳不需要polly拦截器
            //.Intercept(m_ServicePollyInterceptor);
        }
        #endregion

        private static Beep CreateResponse(Beep request, int StreamCount = 1)
        {
            return new Beep
            {
                Beep_ = $"{request.Beep_}-{DateTime.Now.ToUniversalTime()}-{StreamCount}",
                Payload = new Payload
                {
                    ClientId = request.Payload.ClientId,
                }
            };
        }

    }
}
