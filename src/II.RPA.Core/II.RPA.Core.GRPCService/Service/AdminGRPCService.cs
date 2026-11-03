using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using II.RPA.Logging;
using Rpa.Core.Admin;
using ab = Microsoft.Extensions.Logging;

namespace II.RPA.Core.GRPCService.Service
{
    public class AdminGRPCService : AdminService.AdminServiceBase, IGRPCService, IAdminProvider
    {
        private IServiceBoot m_ServiceBoot;
        private readonly ILogger m_Logger;
        private readonly Interceptor m_Interceptor;
        private readonly object m_Lock = new object();

        public AdminGRPCService(ILogger aLogger, Interceptor anInterceptor)
        {
            m_Logger = aLogger;
            m_Interceptor = anInterceptor;
        }

        public void AuthToManageServiceBoot(IServiceBoot aServiceBoot)
        {
            m_ServiceBoot = aServiceBoot;
        }

        public ServerServiceDefinition BindingToService()
        {
            return AdminService.BindService(this).Intercept(m_Interceptor);
        }

        public override Task<AdminResponse> ShutDownUnary(AdminRequest request, ServerCallContext context)
        {
            if (CommonConstants.CLIENTID.VALUES.Contains(request.Payload.ClientId))
            {
                ///不能await、异步执行、保证结果返回给grpc-client
                m_ServiceBoot.StopAsync(500);
                return Task.FromResult(new AdminResponse
                {
                    Status = "1",
                });
            }
            return Task.FromResult(new AdminResponse
            {
                Status = "-1",
            });
        }

        public override Task<AdminResponse> SetDebugModeUnary(AdminRequest request, ServerCallContext context)
        {
            lock (m_Lock)
            {
                try
                {
                    if (!Logger.TryChangeMinLogLevel(ab::LogLevel.Trace))
                        return Task.FromResult(new AdminResponse { Status = "-1" });
                    Global.IfCurrentLogLevelIsDebug = true;
                    return Task.FromResult(new AdminResponse { Status = "1" });
                }
                catch (Exception e) { m_Logger?.LogError(e); return Task.FromResult(new AdminResponse { Status = "-1" }); }
            }
        }

        public override Task<AdminResponse> SetReleaseModeUnary(AdminRequest request, ServerCallContext context)
        {
            lock (m_Lock)
            {
                try
                {
                    if (!Logger.TryChangeMinLogLevel(ab::LogLevel.Information))
                        return Task.FromResult(new AdminResponse { Status = "-1" });
                    Global.IfCurrentLogLevelIsDebug = false;
                    return Task.FromResult(new AdminResponse { Status = "1" });
                }
                catch (Exception e) { m_Logger?.LogError(e); return Task.FromResult(new AdminResponse { Status = "-1" }); }
            }
        }

        public override Task<AdminResponse> ReStartUnary(AdminRequest request, ServerCallContext context)
        {
            try
            {
                ///不能await
                m_ServiceBoot.ReStartAsync(2000);
                return Task.FromResult(new AdminResponse
                {
                    Status = "1"
                });
            }
            catch (Exception e)
            {
                m_Logger?.LogError(e);
                return Task.FromResult(new AdminResponse
                {
                    Status = "-1"
                });
            }
        }

        public override Task<AdminResponse> SetLogLevelUnary(SetLogLevelRequest request, ServerCallContext context)
        {
            //仅支持切换DEBUG和INFO模式
            var resp = new AdminResponse
            {
                Status = "1",
            };

            var level =
                request.LogLevel == "0" ? ab::LogLevel.Debug :
                request.LogLevel == "1" ? ab::LogLevel.Information : ab::LogLevel.Information;

            if (!string.IsNullOrEmpty(request.ClientId) &&
                (request.ClientId.ToLower() == "z-factory" 
                || request.ClientId.ToLower() == "z-bot"))
            {
                resp.Status = Logger.TryChangeMinLogLevel(level) ? "1" : "-1";
            }
            else
            {
                m_Logger.LogWarning($"Unexpected client type：{request.ClientId}");
            }

            return Task.FromResult(resp);
        }
    }
}