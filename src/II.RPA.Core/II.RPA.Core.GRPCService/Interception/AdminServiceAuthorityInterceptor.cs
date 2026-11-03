using Grpc.Core;
using Grpc.Core.Utils;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using II.RPA.Logging.Configurations;
using Rpa.Core.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPCService.Interception
{
    /// <summary>
    /// AdminService专用拦截器
    /// </summary>
    public class AdminServiceAuthorityInterceptor: Grpc.Core.Interceptors.Interceptor
    {
        private readonly ILogger m_Logger;

        public AdminServiceAuthorityInterceptor(ILogger aLogger)
        {
            m_Logger = aLogger;
        }
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            string clientId = "null";
            var requestId = CommonUtils.GenerateRandom();
            TResponse response;
            if (request is AdminRequest adminRequest)
            {
                clientId = adminRequest.Payload.ClientId;
                m_Logger.LogCommunication($"{context.Method}", new StreamScope { RequestId = requestId, Client = $"{clientId}", Action = "Accept" });
                if (Global.TryGetClientID(clientId, out ClientID clientID))
                {
                    m_Logger.LogInfo($"Request Client {clientID}");
                    m_Logger.LogInfo($"Current Client {Global.Client}");                   
                    response = await continuation(request, context);
                }
                else
                {
                    m_Logger.LogInfo($"Invalid Request Client {clientID}");
                    response = await Task.FromResult(CastToT<TResponse>(new AdminResponse 
                    {
                        Status = "-1" 
                    }));
                }
            }
            else
            {
                m_Logger.LogCommunication($"{context.Method}", new StreamScope { RequestId = requestId, Client = clientId, Action = "Send" });
                response = await continuation(request, context);
            }
            m_Logger.LogCommunication($"{context.Method}", new StreamScope { RequestId = requestId, Client = clientId, Action = "Send" });
            return response;
        }
        private T CastToT<T>(object aObject)
        {
            if (aObject is T t)
            {
                return t;
            }
            try
            {
                return (T)Convert.ChangeType(aObject, typeof(T));
            }
            catch (InvalidCastException)
            {
                return default;
            }
        }
            
    }
}
