using Grpc.Core;
using II.RPA.Core.Log;
using Polly;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPCService.Interception
{
    /// <summary>
    /// 弃用
    /// </summary>
    public class ServicePollyInterceptor : Grpc.Core.Interceptors.Interceptor
    {
        private readonly ILogger m_Logger;
        public ServicePollyInterceptor() : base()
        {
            
        }
        public ServicePollyInterceptor(ILogger aLogger) : this()
        {
            m_Logger = aLogger;
        }
        #region Override Methods
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var callId = Guid.NewGuid().ToString("N");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            SafeLog(() => m_Logger?.LogInfo($"gRPC unary begin; method={context.Method}; callId={callId}"));
            try
            {
                var timeoutPolicy = Policy.TimeoutAsync<TResponse>(TimeSpan.FromSeconds(3600));
                var retryPolicy = Policy<TResponse>
                    .Handle<RpcException>(s => s.StatusCode == StatusCode.Internal || s.StatusCode == StatusCode.Unknown)
                    .Or<WebException>()
                    .Or<SocketException>()
                    .OrResult(r => r == null)
                    .WaitAndRetryAsync(3, x => TimeSpan.FromSeconds(1), (result, timeSpan, current, contextPolly) =>
                    {
                        SafeLog(() =>
                        {
                            var message = $"gRPC unary retry; method={context.Method}; callId={callId}; attempt={current}";
                            if (result.Exception != null)
                            {
                                m_Logger?.LogWarning(message, result.Exception);
                            }
                            else
                            {
                                m_Logger?.LogWarning(message + "; response=null");
                            }
                        });
                    });
                var continuationResult = await retryPolicy.WrapAsync(timeoutPolicy)
                    .ExecuteAsync(() => continuation(request, context))
                    .ConfigureAwait(false);
                SafeLog(() => m_Logger?.LogInfo($"gRPC unary completed; method={context.Method}; callId={callId}; elapsedMs={stopwatch.ElapsedMilliseconds}"));
                return continuationResult;
            }
            catch (Exception ex)
            {
                SafeLog(() => m_Logger?.LogError($"gRPC unary failed; method={context.Method}; callId={callId}; elapsedMs={stopwatch.ElapsedMilliseconds}", ex));
                throw;
            }

        }
        private static void SafeLog(Action writeLog)
        {
            try
            {
                writeLog();
            }
            catch
            {
                // Logging must not change retry behavior or mask the original exception.
            }
        }
    }
    #endregion
}
