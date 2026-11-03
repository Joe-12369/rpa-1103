using Grpc.Core;
using Grpc.Core.Interceptors;
using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.GRPCService.Util;
using II.RPA.Core.Log;
using II.RPA.Core.Models;
using II.RPA.Core.Provider.PluginLoaderProvider.ClientAction;
using II.RPA.Core.Runtime;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rpa.Core.Process;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Message = Rpa.Core.Process.Message;

namespace II.RPA.Core.GRPCService.Service
{
    public class ProcessGRPCService : ProcessService.ProcessServiceBase, IGRPCService
    {
        private const string OK_VALUE = "OK";

        private readonly Interceptor m_ServicePollyInterceptor;
        /// <summary>
        /// service类中基本不需要输出日志、都在拦截器中做日志记录
        /// </summary>
        private readonly ILogger m_Logger;
        private readonly IMessageNotify m_MessageNotify;
        private readonly IMessageNotify m_VisualDataService;
        private readonly ClientActionProviderFactory m_ProviderFactory;
        private readonly IRuntimeHostController m_RuntimeHostController;

        private readonly ConcurrentDictionary<int, Message> m_ClientMessageQueue;
        private readonly BlockingCollection<AsyncMessageModel<string, string>> m_ServerMessageQueue;
        private readonly ConcurrentDictionary<int, AsyncMessageModel<string, string>> m_WaitingMessageQueue;
        public ProcessGRPCService()
        {
        }
        public ProcessGRPCService(ILogger aLogger,
            Interceptor anInterceptor,
            ClientActionProviderFactory aClientActionProviderFactory,
            IRuntimeHostController aRuntimeHostController,
            IMessageNotify aMessageNotify,
            IMessageNotify aVisualDataService) : this()
        {
            m_Logger = aLogger;
            m_MessageNotify = aMessageNotify;
            m_VisualDataService = aVisualDataService;
            m_ServicePollyInterceptor = anInterceptor;
            m_ProviderFactory = aClientActionProviderFactory;
            m_RuntimeHostController = aRuntimeHostController;

            m_ClientMessageQueue = new ConcurrentDictionary<int, Message>();
            m_ServerMessageQueue = new BlockingCollection<AsyncMessageModel<string, string>>();
            m_WaitingMessageQueue = new ConcurrentDictionary<int, AsyncMessageModel<string, string>>();
        }
        public override async Task<Message> ResetPluginLoaderRunTimeout(Message request, ServerCallContext context)
        {
            m_Logger.LogDebug($"ResetPluginLoaderRunTimeout:{request.Payload}");

             var msg = new BaseModel
             {
                Code = 0,
                Message = OK_VALUE,
                Result = string.Empty,
            };
            try
            {
                var payload = request.Payload;
                var payloadObject = JObject.Parse(payload);
                m_RuntimeHostController.ExtendTimeout(new RuntimeHostTimeoutExtension
                {
                    RunUniqueId = payloadObject.Value<string>("runUniqueID") ?? string.Empty,
                    NewTimeout = payloadObject.Value<long?>("newTimeout") ?? 20000,
                    TimeoutMessage = payloadObject.Value<string>("timeoutMsg")
                });
                msg.Message = $"ResetPluginLoaderRunTimeout Success";
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
                msg.Code = -1;
                msg.Message = eCommonMessageType.PluginGRPCService_cs_XiTongYiChangQingZhongShiRuo.GetMessage();
            }
            request.Payload = JsonConvert.SerializeObject(msg);
            return await Task.FromResult(request);
        }

        public override async Task<Message> NotifyClientUnary(Message request, ServerCallContext context)
        {
            try
            {
                var ret = await m_MessageNotify.NotifyFactoryAndWaitReturn(request.Action, request.Payload);
                var retMessage = new Message
                {
                    Action = request.Action,
                    Payload = ret,
                    Code = string.IsNullOrEmpty(ret) ? 1 : 0
                };
                return retMessage;
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }
        public override async Task<Message> NotifyEngineUnary(Message request, ServerCallContext context)
        {
            try
            {
                var ret = await m_MessageNotify.NotifyEngineAndWaitReturn(request.Action, request.Payload);
                var retMessage = new Message
                {
                    Action = request.Action,
                    Payload = ret.JsonMessage,
                    Code = ret.StatusCode,
                };
                return retMessage;
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }
        public override async Task<Message> NotifyCoreServerUnary(Message request, ServerCallContext context)
        {
            try
            {
                var ret = await m_MessageNotify.ServerMessagePublish(request.Action, request.Payload);
                var retMessage = new Message
                {
                    Action = request.Action,
                    Payload = ret,
                    Code = 0,
                };
                return retMessage;
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }

        }
        public override async Task<Message> NotifyVisualDataUnary(Message request, ServerCallContext context)
        {
            try
            {
                var ret = await m_VisualDataService.ServerMessagePublish(request.Action, request.Payload);
                var retMessage = new Message
                {
                    Action = request.Action,
                    Payload = ret,
                    Code = 0,
                };
                return retMessage;
            }
            catch (Exception ex) 
            {
                throw new RpcException(new Status(StatusCode.Internal,ex.Message));
            }
        }

        public override async Task<Message> NotifyBotUnary(Message request, ServerCallContext context)
        {
            try
            {
                var ret = await m_MessageNotify.NotifyBotAndWaitReturn(request.Action, request.Payload);
                var retMessage = new Message
                {
                    Action = request.Action,
                    Payload = ret,
                    Code = string.IsNullOrEmpty(ret) ? 1 : 0
                };
                return retMessage;
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }


        public ServerServiceDefinition BindingToService()
        {
            return ProcessService.BindService(this);
        }
    }
}
