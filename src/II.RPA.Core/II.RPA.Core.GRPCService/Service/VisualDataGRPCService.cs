using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using II.RPA.Core.GRPCService.Util;
using II.RPA.Core.Models;
using II.RPA.Core.Provider.PluginLoaderProvider.ClientAction;
using II.RPA.Core.Runtime;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rpa.Core.Plugin;
using Rpa.Core.Visual.Data;
using Rpa.Core.Win32;

namespace II.RPA.Core.GRPCService.Service
{

    public class VisualDataGRPCService : VisualDataService.VisualDataServiceBase, IMessageNotify
    {
        private readonly ILogger m_Logger;
        //private readonly PluginProcessProvider m_PluginLoaderProvider;
        private readonly IRuntimeHostController m_RuntimeHostController;
        private bool m_IsMessageProducerReady;
        private readonly ClientActionProviderFactory m_ProviderFactory;
        private readonly IMessageNotify m_MessageGRPCService;
        private readonly ConcurrentDictionary<int,BlockingCollection<AsyncMessageModel<string, string>>> m_ServerMessageQueue;

        public Action<string> ClientMessageSubscribeHandle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public VisualDataGRPCService(ILogger aLogger,
            IRuntimeHostController aRuntimeHostController,
            ClientActionProviderFactory aClientActionProviderFactory,
            IMessageNotify aMessageGRPCService) : base()
        {
            m_Logger = aLogger;
            //m_PluginLoaderProvider = aPluginLoaderProvider;
            m_RuntimeHostController = aRuntimeHostController;
            m_ProviderFactory = aClientActionProviderFactory;
            m_MessageGRPCService = aMessageGRPCService;
            m_ServerMessageQueue = new ConcurrentDictionary<int, BlockingCollection<AsyncMessageModel<string, string>>>();
        }
        public async override Task<VisualDataResponse> GenerateComponentUnary(GenerateComponentRequest request, ServerCallContext context)
        {
            try
            {
                m_Logger.LogDebug($"GenerateComponentUnary:{request.Payload}");
                var payloadJA = JArray.Parse(request.Payload);
                foreach (var item in payloadJA)
                {
                    JObject itemJO = item as JObject;
                    var type = itemJO["type"].ToString();
                    if (!string.IsNullOrWhiteSpace(type))
                    {
                        if (type.StartsWith("Excel_Cs_"))
                        {
                            var optionJO = new JObject
                            {
                                { "addFoldComponent", true }
                            };
                            itemJO.Add("options", optionJO);
                        }
                        else
                        {
                            var optionJO = new JObject
                            {
                                { "addFoldComponent", false }
                            };
                            itemJO.Add("options", optionJO);
                        }
                    }
                }
                var refactorParam = payloadJA.ToString();
                m_Logger.LogInfo($"GenerateComponentUnary after refactor:{refactorParam}");
                var ret = await m_MessageGRPCService.ServerMessagePublish("flowedit.plugingenerate", refactorParam);
                var retJO = JObject.Parse(ret);
                m_Logger.LogInfo($"flowedit.plugingenerate(excel) return:{ret}");
                return new VisualDataResponse
                {
                    Code = int.Parse(retJO["code"].ToString()),
                    Payload = retJO["message"]?.ToString(),
                    MessageId = request.MessageId,
                };
            }
            catch (TimeoutException tEx)
            {
                m_Logger.LogError("组件推送超时", tEx);
                return await Task.FromResult(new VisualDataResponse
                {
                    Code = 1,
                    MessageId = request.MessageId,
                    Payload = "生成组件超时，请确认是否在 实在设计器 中打开流程"
                });
            }
            catch (Exception ex)
            {
                m_Logger.LogError("推送失败", ex);
                return await Task.FromResult(new VisualDataResponse
                {
                    Code = 1,
                    MessageId = request.MessageId,
                    Payload = "生成组件失败"
                });
            }
        }
        public async override Task<VisualDataResponse> StatusControlUnary(StatusControlRequest request, ServerCallContext context)
        {
            try
            {
                var provider = m_ProviderFactory.ResolveProvider("ProcessCreator", m_RuntimeHostController);
                if (string.IsNullOrEmpty(m_RuntimeHostController.CurrentPackagePath))
                {
                    throw new Exception(eCommonMessageType.VisualDataGRPCService_cs_WeiChuShiHua.GetMessage());
                }
                provider.PackagePath = m_RuntimeHostController.CurrentPackagePath;
                string paramStr;
                m_Logger.LogInfo($"StatusControlUnary Status:{request?.Status}");
                if ("0" == request.Status)
                {
                    paramStr = "{\"action\":0,\"type\":1,\"elementTypes\":[\"normal\",\"cv\"],\"componentList\":[],\"clientType\":\"VisualDataGRPCService\"}";
                }
                else
                {
                    paramStr = "{\"action\":1,\"type\":1,\"elementTypes\":[\"normal\",\"cv\"],\"componentList\":[],\"clientType\":\"VisualDataGRPCService\"}";
                }
                var action = provider.ResolveAction(paramStr);
                m_Logger.LogDebug(paramStr);
                var paramResolved = provider.ResolveReqVal(paramStr);
                m_Logger.LogDebug(paramResolved);
                var preRetVal = provider.PreResolveRetVal(paramResolved, paramStr);

                if (preRetVal.Code != "0")
                {
                    return await Task.FromResult(new VisualDataResponse
                    {
                        Code = int.Parse(preRetVal.Code),
                        MessageId = request.MessageId,
                    });
                }

                var retval = await RunRuntimeHostStaAsync(
                    provider.PackagePath,
                    action.PluginName,
                    action.PluginAction,
                    paramResolved,
                    context.CancellationToken);
                var retResolved = provider.ResolveRetVal(retval?.ToString());

                return await Task.FromResult(new VisualDataResponse
                {
                    Code = 0,
                    MessageId = request.MessageId,
                    Payload = JsonConvert.SerializeObject(retResolved),
                });
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
                return await Task.FromResult(new VisualDataResponse
                {
                    Code = 1,
                    MessageId = request.MessageId,
                    Payload = ex.Message,
                });
            }

        }
        public override async Task<VisualDataResponse> StatusCheckUnary(StatusCheckRequest request, ServerCallContext context)
        {
            try
            {
                var statusVal = await GetProcessCreatorStatus(context.CancellationToken);
                return new VisualDataResponse
                {
                    Code = 0,
                    MessageId = request.MessageId,
                    Payload = statusVal,
                };
            }
            catch (Exception ex)
            {
                return new VisualDataResponse
                {
                    Code = 1,
                    MessageId = request.MessageId,
                    Payload = ex.Message,
                };
            }
        }
        public override async Task ServerStreaming(Message request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            var asyncMessageCollection = new BlockingCollection<AsyncMessageModel<string, string>>();
            m_ServerMessageQueue.AddOrUpdate(request.MessageId, asyncMessageCollection, (k, oldValue) =>
            {
                oldValue?.Dispose();
                return asyncMessageCollection;
            });
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                if (asyncMessageCollection.TryTake(out AsyncMessageModel<string, string> asyncMessage, -1, context.CancellationToken))
                {
                    await responseStream.WriteAsync(new Message
                    {
                        Payload = asyncMessage.Input,
                            MessageId = GetMessageId(),
                    });
                }
            }
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogInfo($"VisualData ServerStreaming canceled. MessageId={request.MessageId}");
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"VisualData ServerStreaming error. MessageId={request.MessageId}", ex);
            }
            finally
            {
                if (m_ServerMessageQueue.TryRemove(request.MessageId, out BlockingCollection<AsyncMessageModel<string, string>> outQueue))
                {
                    outQueue.Dispose();
                }
            }
        }
        public override async Task<VisualDataResponse> ExcelStatusNotifyUnary(ExcelStatusRequest request, ServerCallContext context)
        {
            try
            {
                var provider = m_ProviderFactory.ResolveProvider("ProcessCreator", m_RuntimeHostController);
                if (string.IsNullOrEmpty(m_RuntimeHostController.CurrentPackagePath))
                {
                    throw new Exception("未初始化！");
                }
                provider.PackagePath = m_RuntimeHostController.CurrentPackagePath;
                string paramStr;
                m_Logger.LogInfo($"ExcelStatusNotifyUnary Status:{request?.Status}");
                if ("0" == request.Status)
                {
                    paramStr = "{\"action\":12,\"type\":1,\"elementTypes\":[\"normal\",\"cv\"],\"componentList\":[],\"clientType\":\"VisualDataGRPCService\"}";
                }
                else
                {
                    paramStr = "{\"action\":13,\"type\":1,\"elementTypes\":[\"normal\",\"cv\"],\"componentList\":[],\"clientType\":\"VisualDataGRPCService\"}";
                }
                var action = provider.ResolveAction(paramStr);
                m_Logger.LogDebug(paramStr);
                var paramResolved = provider.ResolveReqVal(paramStr);
                m_Logger.LogDebug(paramResolved);
                var preRetVal = provider.PreResolveRetVal(paramResolved, paramStr);

                if (preRetVal.Code != "0")
                {
                    return await Task.FromResult(new VisualDataResponse
                    {
                        Code = int.Parse(preRetVal.Code),
                        MessageId = request.MessageId,
                    });
                }

                var retval = await RunRuntimeHostStaAsync(
                    provider.PackagePath,
                    action.PluginName,
                    action.PluginAction,
                    paramResolved,
                    context.CancellationToken);
                var retResolved = provider.ResolveRetVal(retval?.ToString());

                return await Task.FromResult(new VisualDataResponse
                {
                    Code = 0,
                    MessageId = request.MessageId,
                    Payload = JsonConvert.SerializeObject(retResolved),
                });
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex);
                return await Task.FromResult(new VisualDataResponse
                {
                    Code = 1,
                    MessageId = request.MessageId,
                    Payload = ex.Message,
                });
            }
        }

        public override async Task<VisualDataResponse> EventTrackerUnary(EventRequest request, ServerCallContext context)
        {
            try
            {
                m_Logger.LogDebug($"EventTrackerUnary:{request.Payload}");
                var eventJson = new JObject
                {
                    { "eventType", "data_visualization" },
                    { "eventContent", request.Payload }
                };
                await m_MessageGRPCService.ServerMessagePublish("flowedit.tracking", JsonConvert.ToString(JsonConvert.SerializeObject(eventJson)));
            }
            catch(Exception ex)
            {
                m_Logger.LogError(ex);
            }
            return new VisualDataResponse 
            {
                Code = 0,
            };
        }
        public ServerServiceDefinition BindingToService()
        {
            return VisualDataService.BindService(this);
        }
        /// <summary>
        /// 主动获取
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<string> GetProcessCreatorStatus(CancellationToken cancellationToken)
        {
            var provider = m_ProviderFactory.ResolveProvider("ProcessCreator", m_RuntimeHostController);
            if (string.IsNullOrEmpty(m_RuntimeHostController.CurrentPackagePath))
            {
                throw new Exception(eCommonMessageType.VisualDataGRPCService_cs_WeiChuShiHua.GetMessage());
            }
            provider.PackagePath = m_RuntimeHostController.CurrentPackagePath;
            string paramStr;
            paramStr = "{\"action\":11,\"type\":1,\"elementTypes\":[\"normal\",\"cv\"],\"componentList\":[]}";
            var action = provider.ResolveAction(paramStr);
            var paramResolved = provider.ResolveReqVal(paramStr);
            var retval = await RunRuntimeHostStaAsync(
                provider.PackagePath,
                action.PluginName,
                action.PluginAction,
                paramResolved,
                cancellationToken);
            var retResolved = provider.ResolveRetVal(retval?.ToString());
            return retResolved.Result;
        }

        private Task<object> RunRuntimeHostStaAsync(
            string packagePath,
            string pluginName,
            string action,
            string jsonParameters,
            CancellationToken cancellationToken = default)
        {
            return m_RuntimeHostController.RunAsync(new RuntimeHostRunRequest
            {
                PackagePath = packagePath,
                PluginName = pluginName,
                Action = action,
                JsonParameters = jsonParameters,
                RunParameters = RunParamModel.DefaultValue,
                AutoInitialize = true,
                Options = new PluginRunOptions
                {
                    IsAsync = false,
                    NeedUiThread = true
                },
                ExecutionMode = RuntimeHostExecutionMode.Direct
            }, cancellationToken);
        }
        private int GetMessageId()
        {
            return MessageIdGenerator.NextMessageId();
        }

        public void SetReady()
        {
            m_IsMessageProducerReady = true;
        }

        public void SetSuspend()
        {
            m_IsMessageProducerReady = true;
        }

        public Task<string> NotifyFactoryAndWaitReturn(string anAction, string aMessage)
        {
            throw new NotImplementedException();
        }

        public Task<EngineInvokeMessage> NotifyEngineAndWaitReturn(string anAction, string aMessage)
        {
            throw new NotImplementedException();
        }

        public Task<string> ServerMessagePublish(string anAction, string aMessage)
        {
            var asyncMessage =
                new AsyncMessageModel<string, string>(aMessage, anAction);
            m_ServerMessageQueue.ForEach(mq => mq.Value.TryAdd(asyncMessage));
            return Task.FromResult(string.Empty);
        }
        public string ServerMessagePublishSync(string anAction, string aMessage)
        {
            return null;
            //AsyncMessageModel<string, string> asyncMessage =
            //     new AsyncMessageModel<string, string>(aMessage, anAction);
            //if (m_ServerMessageQueue.TryAdd(asyncMessage))
            //{
            //    return asyncMessage.GetOutput(TimeSpan.FromSeconds(5));
            //}
            //else
            //{
            //    throw new InvalidOperationException();
            //}
        }

        public Task<string> NotifyBotAndWaitReturn(string anAction, string aMessage)
        {
            throw new NotImplementedException();
        }
    }
}
