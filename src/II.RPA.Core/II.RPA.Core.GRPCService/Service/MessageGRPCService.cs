using CommonServiceLocator;
using Grpc.Core;
using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.GRPCService.Util;
using II.RPA.Core.Log;
using II.RPA.Core.Provider.PluginLoaderProvider;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Contracts.Talker.Engine;
using II.RPA.Core.Contracts.Talker.Engine.Message.Action;
using II.RPA.Core.Talker;
using II.RPA.Core.Talker.Talkers;
using Newtonsoft.Json;
using Rpa.Core.Message;
using Rpa.Core.Plugin;
using Rpa.Core.Worker.Zbrain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPCService.Service
{
        public class MessageGRPCService : MessageService.MessageServiceBase, IMessageNotify
        {
        private volatile bool m_IsStartClientStreaming;
        private volatile bool m_IsStartEngineStreaming;
        private volatile bool m_IsStartBotStreaming;

        private bool m_IsMessageProducerReady;
        private readonly ILogger m_Logger;
        private BlockingCollection<FactoryMessageModel> m_FactoryMessageCollection;
        private BlockingCollection<EngineMessageModel> m_EngineMessageCollection;
        private BlockingCollection<BotMessageModel> m_BotMessageCollection;

        private readonly ConcurrentDictionary<int,Message> m_ClientMessageQueue;
        private readonly ConcurrentDictionary<int, AsyncMessageModel<string,string>> m_WaitingMessageQueue;
        private readonly BlockingCollection<AsyncMessageModel<string,string>> m_ServerMessageQueue;
        private readonly TalkerManager _talkerManager;

        public Action<string> ClientMessageSubscribeHandle 
        { 
            get; 
            set; 
        }

        public MessageGRPCService(ILogger aLogger) : base()
        {
            m_Logger = aLogger;
            m_FactoryMessageCollection = new BlockingCollection<FactoryMessageModel>();
            m_EngineMessageCollection = new BlockingCollection<EngineMessageModel>();
            m_BotMessageCollection= new BlockingCollection<BotMessageModel>();

            m_ClientMessageQueue = new ConcurrentDictionary<int, Message>();
            m_ServerMessageQueue = new BlockingCollection<AsyncMessageModel<string, string>>();
            m_WaitingMessageQueue = new ConcurrentDictionary<int, AsyncMessageModel<string, string>>();

            _talkerManager = ServiceLocator.Current.GetInstance<TalkerManager>();
        }
        public override async Task ClientMessageStreaming(IAsyncStreamReader<ClientMessage> requestStream, IServerStreamWriter<ServerMessage> responseStream, ServerCallContext context)
        {
            m_Logger.LogDebug("factory start subscribe...");
            m_IsStartClientStreaming = true;
            m_FactoryMessageCollection = new BlockingCollection<FactoryMessageModel>();
            var bufferedResponses = new Dictionary<int, ClientMessage>();
            try
            {
                while (m_IsStartClientStreaming && !context.CancellationToken.IsCancellationRequested)
                {
                    if (m_FactoryMessageCollection.TryTake(out var message, -1, context.CancellationToken))
                    {
                        var serverMessage = message.Message;
                        m_Logger.LogDebug($"ServerMessage:{serverMessage.MessageId};{serverMessage.Action}");
                        context.CancellationToken.ThrowIfCancellationRequested();
                        await responseStream.WriteAsync(serverMessage);

                        var response = await ReadMatchedResponseAsync(
                            requestStream,
                            bufferedResponses,
                            serverMessage.MessageId,
                            m => m.MessageId,
                            context.CancellationToken);

                        if (response == null)
                        {
                            break;
                        }

                        message.ReturnValue = response;
                        message.FinishedSignal.Set();
                        message.FinishedSignal.Reset();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogInfo($"ClientMessageStreaming canceled. MessageId={requestStream?.Current?.MessageId}");
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"ClientMessageStreaming error. MessageId={requestStream?.Current?.MessageId}", ex);
            }
            finally
            {
                m_IsStartClientStreaming = false;
            }
        }
        public override async Task EngineMessageStreaming(IAsyncStreamReader<EngineMessage> requestStream, IServerStreamWriter<ServerMessage> responseStream, ServerCallContext context)
        {
            m_Logger.LogDebug("engine start subscribe...");
            m_IsStartEngineStreaming = true;
            m_EngineMessageCollection = new BlockingCollection<EngineMessageModel>();
            var bufferedResponses = new Dictionary<int, EngineMessage>();
            try
            {
                while (m_IsStartEngineStreaming && !context.CancellationToken.IsCancellationRequested)
                {
                    if (m_EngineMessageCollection.TryTake(out var message, -1, context.CancellationToken))
                    {
                        var serverMessage = message.Message;
                        m_Logger.LogDebug($"ServerMessage:{serverMessage.MessageId};{serverMessage.Action}");
                        context.CancellationToken.ThrowIfCancellationRequested();
                        await responseStream.WriteAsync(serverMessage);

                        var response = await ReadMatchedResponseAsync(
                            requestStream,
                            bufferedResponses,
                            serverMessage.MessageId,
                            m => m.MessageId,
                            context.CancellationToken);

                        if (response == null)
                        {
                            break;
                        }

                        message.ReturnValue = response;
                        message.FinishedSignal.Set();
                        message.FinishedSignal.Reset();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogInfo($"EngineMessageStreaming canceled. MessageId={requestStream?.Current?.MessageId}");
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"EngineMessageStreaming error. MessageId={requestStream?.Current?.MessageId}", ex);
            }
            finally
            {
                m_IsStartEngineStreaming = false;
            }
        }
        public override async Task<Message> ClientStreaming(IAsyncStreamReader<Message> requestStream, ServerCallContext context)
        {
            try
            {
                while (await requestStream.MoveNext(context.CancellationToken).ConfigureAwait(false))
                {
                    var request = requestStream.Current;
                    if (m_WaitingMessageQueue.TryRemove(request.MessageId, out AsyncMessageModel<string, string> aWaitingMessage))
                    {
                        m_Logger.LogDebug("response message from client");
                        m_Logger.LogDebug(request.Payload);
                        aWaitingMessage.SetOutput(request.Payload);
                    }
                    else
                    {
                        m_Logger.LogWarning("accidental message from client");
                        ClientMessageSubscribeHandle?.Invoke(JsonConvert.SerializeObject(request));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogInfo($"ClientStreaming canceled. MessageId={requestStream?.Current?.MessageId}");
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"ClientStreaming error. MessageId={requestStream?.Current?.MessageId}", ex);
            }

            return new Message
            {
                Action = "clientstreaming.over",
                MessageId = GetMessageId(),
                ClientId = "coreserver",
            };
        }

        public override async Task BotMessageStreaming(IAsyncStreamReader<BotMessage> requestStream, IServerStreamWriter<ServerMessage> responseStream, ServerCallContext context)
        {
            m_Logger.LogDebug("bot start subscribe...");
            m_IsStartBotStreaming = true;
            m_BotMessageCollection = new BlockingCollection<BotMessageModel>();
            var bufferedResponses = new Dictionary<int, BotMessage>();
            try
            {
                while (m_IsStartBotStreaming && !context.CancellationToken.IsCancellationRequested)
                {
                    if (m_BotMessageCollection.TryTake(out var message, -1, context.CancellationToken))
                    {
                        var serverMessage = message.Message;
                        m_Logger.LogDebug($"ServerMessage:{serverMessage.MessageId};{serverMessage.Action}");
                        context.CancellationToken.ThrowIfCancellationRequested();
                        await responseStream.WriteAsync(serverMessage);

                        var response = await ReadMatchedResponseAsync(
                            requestStream,
                            bufferedResponses,
                            serverMessage.MessageId,
                            m => m.MessageId,
                            context.CancellationToken);

                        if (response == null)
                        {
                            break;
                        }

                        message.ReturnValue = response;
                        message.FinishedSignal.Set();
                        message.FinishedSignal.Reset();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogInfo($"BotMessageStreaming canceled. MessageId={requestStream?.Current?.MessageId}");
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"BotMessageStreaming error. MessageId={requestStream?.Current?.MessageId}", ex);
            }
            finally
            {
                m_IsStartBotStreaming = false;
            }
        }

        public override async Task ServerStreaming(Message request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    if (m_ServerMessageQueue.TryTake(out AsyncMessageModel<string, string> asyncMessage, -1, context.CancellationToken))
                    {
                        //todo 等待返回的messageid集合
                        var messageId = GetMessageId();
                        if (m_WaitingMessageQueue.TryAdd(messageId, asyncMessage))
                        {
                            m_Logger.LogInfo($"Add to waitingQueue. MessageId={messageId}");
                        }
                        await responseStream.WriteAsync(new Message
                        {
                            Payload = asyncMessage.Input,
                            MessageId = messageId,
                            Action = asyncMessage.ActionName
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogInfo($"ServerStreaming canceled. MessageId={request.MessageId}");
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"ServerStreaming error. RequestMessageId={request.MessageId}", ex);
            }
        }
        public Task<string> ServerMessagePublish(string anAction, string aMessage)
        {
            var task = Task.Factory.StartNew(() => 
            {
                AsyncMessageModel<string, string> asyncMessage = 
                new AsyncMessageModel<string, string>(aMessage, anAction);
                if (m_ServerMessageQueue.TryAdd(asyncMessage))
                {
                    return asyncMessage.GetOutput(TimeSpan.FromSeconds(5));
                }
                else
                {
                    throw new InvalidOperationException();
                }
            },TaskCreationOptions.LongRunning);
            task.ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });
            return task;
        }
        public string ServerMessagePublishSync(string anAction, string aMessage)
        {
            AsyncMessageModel<string, string> asyncMessage =
                new AsyncMessageModel<string, string>(aMessage, anAction);
            if (m_ServerMessageQueue.TryAdd(asyncMessage))
            {
                return asyncMessage.GetOutput(TimeSpan.FromSeconds(5));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        public ServerServiceDefinition BindingToService()
        {
            return MessageService.BindService(this);
        }

        public Task<string> NotifyFactoryAndWaitReturn(string anAction, string aMessage)
        {
            return Task.Run(() =>
            {
                if (m_IsStartClientStreaming)
                {
                    var messageModel = new FactoryMessageModel
                    {
                        Message = new ServerMessage
                        {
                            MessageId = GetMessageId(),
                            Action = anAction,
                            Message = aMessage,
                        }
                    };
                    if (m_FactoryMessageCollection.TryAdd(messageModel))
                    {
                        //factory的消息等待时长为永久
                        if (messageModel.FinishedSignal.WaitOne())
                        {
                            return messageModel.ReturnValue.Message;
                        }
                        else
                        {
                            //正常不会走到这里
                            m_Logger.LogError($"wait time out");
                        }
                    }
                    else
                    {
                        m_Logger.LogError($"Fail to add message to Queue! {aMessage}");
                    }
                }
                else
                {
                    m_Logger.LogError($"Factory Streaming has not been started! {m_IsStartClientStreaming} {aMessage}");
                }
                return null;
            });
        }


        public Task<EngineInvokeMessage> NotifyEngineAndWaitReturn(string anAction, string aMessage)
        {
            m_Logger.LogDebug("Enter NotifyEngine...");
            return Task.Run(() =>
            {
                string errorText = "unknow error";
                if (m_IsStartEngineStreaming)
                {
                    var messageModel = new EngineMessageModel
                    {
                        Message = new ServerMessage
                        {
                            MessageId = GetMessageId(),
                            Action = anAction,
                            Message = aMessage,
                        }
                    };
                    if (m_EngineMessageCollection.TryAdd(messageModel))
                    {
                        //engine的消息等待时间为10s
                        if (messageModel.FinishedSignal.WaitOne(TimeSpan.FromSeconds(10)))
                        {
                            return new EngineInvokeMessage
                            {
                                JsonMessage = messageModel.ReturnValue.Message,
                                StatusCode = string.IsNullOrEmpty(messageModel.ReturnValue.StatusCode) ? 1 : int.Parse(messageModel.ReturnValue.StatusCode)
                            };
                        }
                        else
                        {
                            errorText = "Wait time out";
                        }
                    }
                    else
                    {
                        errorText = $"Fail to add message to Queue! {aMessage}";
                    }
                }
                else
                {
                    errorText = $"Engine Streaming has not been started! {m_IsStartEngineStreaming} {aMessage}";
                }
                m_Logger.LogError(errorText);
                return new EngineInvokeMessage
                {
                    JsonMessage = errorText,
                    StatusCode = 1
                };
             
            });
        }

        public async Task<string> NotifyBotAndWaitReturn(string anAction, string aMessage)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<ParamAiEnhancedMessage>(aMessage);
                var actionMessage = new ActionMessage
                {
                    Action = anAction,
                    Payload = II.RPA.Core.Contracts.Talker.Engine.Message.EngineMessage.Serialize(
                        new II.RPA.Core.Contracts.Talker.Engine.Message.EngineMessage<ParamAiEnhancedMessage>(anAction, message))
                };

                m_Logger.LogDebug($"发送：{JsonConvert.SerializeObject(actionMessage)}");
                await _talkerManager.GetTalker<HostTalker>().Notify(new FrameMessage<ActionMessage>
                {
                    Payload = actionMessage,
                    Id = Guid.NewGuid(),
                    Type = FrameType.EVENT,
                });
            }
            catch
            { }
            
            return JsonConvert.SerializeObject(new ClientMessage() { Action = anAction });
        }

        public void SetReady()
        {
            m_IsMessageProducerReady = true;
        }

        public void SetSuspend()
        {
            m_IsMessageProducerReady = false;
        }
        private int GetMessageId()
        {
            return MessageIdGenerator.NextMessageId();
        }

        private static async Task<TResponse> ReadMatchedResponseAsync<TResponse>(
            IAsyncStreamReader<TResponse> requestStream,
            Dictionary<int, TResponse> bufferedResponses,
            int expectedMessageId,
            Func<TResponse, int> getMessageId,
            System.Threading.CancellationToken cancellationToken)
            where TResponse : class
        {
            while (true)
            {
                if (bufferedResponses.TryGetValue(expectedMessageId, out var bufferedResponse))
                {
                    bufferedResponses.Remove(expectedMessageId);
                    return bufferedResponse;
                }

                if (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false))
                {
                    var response = requestStream.Current;
                    var messageId = getMessageId(response);
                    if (messageId == expectedMessageId)
                    {
                        return response;
                    }

                    bufferedResponses[messageId] = response;
                    continue;
                }

                return null;
            }
        }
    }
    
    public class FactoryMessageModel
    {
        public ServerMessage Message
        {
            get; 
            set; 
        }
        public ManualResetEvent FinishedSignal
        {
            get;
            set;
        }
        public ClientMessage ReturnValue
        {
            get;
            set;
        }
        public FactoryMessageModel()
        {
            FinishedSignal = new ManualResetEvent(false);
        }
    }
    public class EngineMessageModel
    {
        public ServerMessage Message
        {
            get;
            set;
        }
        public ManualResetEvent FinishedSignal
        {
            get;
            set;
        }
        public EngineMessage ReturnValue
        {
            get;
            set;
        }
        public EngineMessageModel()
        {
            FinishedSignal = new ManualResetEvent(false);
        }
    }
    public class BotMessageModel
    {
        public ServerMessage Message
        {
            get;
            set;
        }
        public ManualResetEvent FinishedSignal
        {
            get;
            set;
        }
        public BotMessage ReturnValue
        {
            get;
            set;
        }
        public BotMessageModel()
        {
            FinishedSignal = new ManualResetEvent(false);
        }
    }
}
