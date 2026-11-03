using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using II.RPA.Core.Log;
using Newtonsoft.Json;

namespace II.RPA.Core.GRPCService.Interception
{
    /// <summary>
    /// 公共拦截器 目前GRPCServiceBoot中的所有Service都会加载此拦截器
    /// 主要用于http2 header的日志输出、目前意义不大，可留作备用
    /// 已经从容器中移除
    /// </summary>
    public class ServiceLogInterceptor : Grpc.Core.Interceptors.Interceptor
    {
        private readonly ILogger m_Logger;

        public ServiceLogInterceptor(ILogger aLogger)
        {
            m_Logger = aLogger;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            LogCall<TRequest, TResponse>(MethodType.Unary, context);

            try
            {
                return await continuation(request, context);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
                return await continuation(request, context);
            }
        }
        /// <summary>
        /// 对于流式通讯、只有在建立连接是会进入拦截器、建立连接后的通讯不会触发拦截器
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="requestStream"></param>
        /// <param name="context"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            LogCall<TRequest, TResponse>(MethodType.ClientStreaming, context);
            return base.ClientStreamingServerHandler(requestStream, context, continuation);
        }
        /// <summary>
        /// 对于流式通讯、只有在建立连接是会进入拦截器、建立连接后的通讯不会触发拦截器
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public override Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            LogCall<TRequest, TResponse>(MethodType.ServerStreaming, context);
            return base.ServerStreamingServerHandler(request, responseStream, context, continuation);
        }
        /// <summary>
        /// 对于流式通讯、只有在建立连接是会进入拦截器、建立连接后的通讯不会触发拦截器
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public override Task DuplexStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            LogCall<TRequest, TResponse>(MethodType.DuplexStreaming, context);
            return base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation);
        }

        private void LogCall<TRequest, TResponse>(MethodType aMethodType, ServerCallContext aContext)
            where TRequest : class
            where TResponse : class
        {
            //WriteMetadata(context.RequestHeaders, "caller-user");
            //WriteMetadata(context.RequestHeaders, "caller-machine");
            //WriteMetadata(context.RequestHeaders, "caller-os");
            WriteMetadata(aContext.RequestHeaders, "user-agent");
            WriteString(aContext.Peer, "caller-endpoint");
            void WriteMetadata(Metadata headers, string key)
            {
                var headerValue = headers.SingleOrDefault(h => h.Key == key)?.Value;
                //m_Logger.LogDebug($"{key}: {headerValue ?? "(unknown)"}");
            }
            void WriteString(string value, string key)
            {
                //m_Logger.LogDebug($"{key}: {value ?? "(unknown)"}");
            }
        }
    }
}
