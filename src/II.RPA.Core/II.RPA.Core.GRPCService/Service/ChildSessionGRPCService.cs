using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using II.RPA.Core.Abstract;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using II.RPA.Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rpa.Core.Childsession;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using II.RPA.Core.GRPCService.Util;
using System.Threading;
using System.Threading.Tasks;
using Unity.Interception.Utilities;

namespace II.RPA.Core.GRPCService.Service
{
    public class ChildSessionGRPCService : ChildSessionService.ChildSessionServiceBase, IGRPCService
    {
        private static readonly string processFileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Pid\\Z-Desktop.txt";
        private static readonly string clientFileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\Client.txt";
        private static string m_CurrentDeskTopClient;
        private readonly ILogger m_Logger;
        private Interceptor m_Interceptor;
        private Process m_EngineProcess;
        private readonly ConcurrentDictionary<int, BlockingCollection<AsyncMessageModel<string, string>>> m_ServerMessageQueue;

        private readonly BlockingCollection<AsyncMessage<Message, Message>> m_MainEngineMessageQueue;
        private readonly BlockingCollection<AsyncMessage<Message, Message>> m_SubEngineMessageQueue;

        private readonly BlockingCollection<AsyncMessage<Message, Message>> m_ZdesktopMessageQueue;

        public ChildSessionGRPCService(ILogger aLogger, Interceptor anInterceptor)
        {
            m_Logger = aLogger;
            m_Interceptor = anInterceptor;
            m_MainEngineMessageQueue = new BlockingCollection<AsyncMessage<Message, Message>>();
            m_SubEngineMessageQueue = new BlockingCollection<AsyncMessage<Message, Message>>();
            m_ZdesktopMessageQueue = new BlockingCollection<AsyncMessage<Message, Message>>();
            m_ServerMessageQueue = new ConcurrentDictionary<int, BlockingCollection<AsyncMessageModel<string, string>>>();
        }

        public override async Task<GetZdesktopResponse> GetZdesktopUnary(GetZdesktopRequest request, ServerCallContext context)
        {
            int pid = -1;
            try
            {
                if (ZDeskTopProcessResolver.TryGetRunningProcessId(processFileName, out pid))
                {
                    m_Logger.LogInfo($"Detect Zdesktop process:{pid}!");
                }
                else if (File.Exists(processFileName))
                {
                    // 陈旧 pid 或空文件：按"未在运行"处理，属正常生命周期状态，不记错误
                    m_Logger.LogInfo("Z-Desktop.txt 对应进程未运行，按未运行处理");
                }
                await Task.Yield();
                if (pid > 0)
                {
                    if (!string.IsNullOrEmpty(m_CurrentDeskTopClient) && m_CurrentDeskTopClient != Global.ClientIDString)
                    {
                        m_Logger.LogInfo($"m_CurrentDeskTopClient:{m_CurrentDeskTopClient};{Global.ClientIDString}!");
                        return new GetZdesktopResponse
                        {
                            IsRunnning = true,
                            Pid = pid,
                        };
                    }
                    if (File.Exists(clientFileName))
                    {
                        string clientString = File.ReadAllText(clientFileName);

                        m_Logger.LogInfo($"clientFileName:{clientString};{Global.ClientIDString}!");

                        if (!string.IsNullOrEmpty(clientString) && clientString != Global.ClientIDString)
                        {
                            return new GetZdesktopResponse
                            {
                                IsRunnning = true,
                                Pid = pid,
                            };
                        }
                    }
                }
                m_Logger.LogInfo($"GetZdesktopUnary:{false}!");
            }
            catch(Exception ex ) 
            {
                m_Logger.LogError(ex);
            }
            return new GetZdesktopResponse
            {
                IsRunnning = false,
                Pid = -1,
            };
        }

        public override async Task<Message> ChildSessionReadyUnary(Message request, ServerCallContext context)
        {
            m_CurrentDeskTopClient = request.ClientId;
            m_Logger.LogInfo($"ChildSessionReadyUnary:{m_CurrentDeskTopClient}");
            await ServerMessagePublish("ChildSessionStatusNotify", JsonConvert.SerializeObject(new JObject 
            { 
                { "status", 0 },
                { "type", "SessionHubStatusNotify" }
            }));
            return new Message { MessageId = request.MessageId };
        }
        public override async Task<Message> ChildSessionCloseUnary(Message request, ServerCallContext context)
        {
            m_CurrentDeskTopClient = string.Empty;
            await ServerMessagePublish("ChildSessionStatusNotify", JsonConvert.SerializeObject(new JObject
            {
                { "status", 1 },
                { "type", "ZDeskTopClose" }
            }));
            return new Message { MessageId = request.MessageId };
        }
        public override async Task<RunResponse> StartZDeskTop(RunRequest request, ServerCallContext context)
        {
            DirectoryInfo currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var parentDir = currentDir.Parent;
            var target = Path.Combine(parentDir.FullName, "II.RPA.ZDeskTop\\II.RPA.ZDeskTop.exe");
            await Task.Yield();
            var p = Process.Start(target, Global.ClientIDString);
            return new RunResponse { ClientId = request.ClientId, Code = "0",Message = $"{p.Id}" };
        }
        public override async Task<RunResponse> RunUnary(RunRequest request, ServerCallContext context)
        {
            await Task.Yield();
            try
            {
                var dataContent = JObject.Parse(request.Payload);
                m_Logger.LogInfo($"dataContent:{Environment.NewLine}{dataContent}");

                var data = dataContent["data"];
                m_Logger.LogDebug($"{data}");

                var path = dataContent["appPathToRun"];
                m_Logger.LogDebug($"appPathToRun:{path}");

                var cwd = dataContent["cwd"];
                m_Logger.LogDebug($"cwd:{cwd}");

                var startParam = dataContent["startParam"];
                m_Logger.LogDebug($"startParam:{startParam}");

                if (!File.Exists(path.ToString()))
                {
                    m_Logger.LogError(new FileNotFoundException(path.ToString()));
                    return new RunResponse
                    {
                        Code = "1",
                        Message = $"{path} does not exist!"
                    };
                }
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = path.ToString(),
                    Arguments = $"-s 1",
                    UseShellExecute = false,
                    WorkingDirectory = cwd.ToString(),
                };
                m_EngineProcess = Process.Start(startInfo);
                m_EngineProcess.EnableRaisingEvents = true;
                m_EngineProcess.Exited += EngineProcess_Exited;
                m_Logger.LogInfo($"start engine");
                return new RunResponse
                {
                    Code = "0",
                    Message = $"Start engine successfully.Pid is {m_EngineProcess.Id}"
                };
            }
            catch (Exception ex) 
            {
                m_Logger.LogError(ex);
                return new RunResponse
                {
                    Code = "1",
                    Message = $"{ex}"
                };
            }
        }

        private async void EngineProcess_Exited(object sender, EventArgs e)
        {
            m_Logger.LogInfo($"engine exited!");
            if (Global.RootClientString == "Z-Bot")
            {
                var autoClose = await BotService.Singleton.GetConfigOfAutoClose();
                if (!autoClose)
                {
                    m_Logger.LogInfo($"AutoClose is {autoClose}.ShutDown!");
                    Environment.Exit(0);
                }
            }
        }

        public override async Task CoreServerStreaming(Message request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
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
                m_Logger.LogInfo($"CoreServerStreaming canceled. MessageId={request.MessageId}");
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"CoreServerStreaming error. MessageId={request.MessageId}", ex);
            }
            finally
            {
                if (m_ServerMessageQueue.TryRemove(request.MessageId, out BlockingCollection<AsyncMessageModel<string, string>> outQueue))
                {
                    outQueue.Dispose();
                }
            }
        }

        public override async Task<Message> MainEngineRequestUnary(Message request, ServerCallContext context)
        {
            var message = new AsyncMessage<Message, Message>(request.MessageId);
            message.AddInput(request);
            if (m_MainEngineMessageQueue.TryAdd(message))
            {
                return await Task.FromResult(new Message
                {
                    MessageId = request.MessageId,
                    Action = request.Action,
                });
            }
            else
            {
                throw new InvalidOperationException($"Fail to add message {request.MessageId}");
            }
        }
        public override async Task MainEngineStreaming(Message request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var asyncMessage = m_MainEngineMessageQueue.Take(context.CancellationToken);
                    if (asyncMessage != null)
                    {
                        var inputMessage = asyncMessage.GetInput();
                        await responseStream.WriteAsync(new Message
                        {
                            Payload = inputMessage.Payload,
                            MessageId = inputMessage.MessageId,
                            Action = inputMessage.Action,
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogInfo($"MainEngineStreaming canceled. MessageId={request.MessageId}");
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"MainEngineStreaming error. MessageId={request.MessageId}", ex);
            }
        }

        public override async Task<Message> SubEngineRequestUnary(Message request, ServerCallContext context)
        {
            var message = new AsyncMessage<Message, Message>(request.MessageId);
            message.AddInput(request);
            if (m_SubEngineMessageQueue.TryAdd(message))
            {
                return await Task.FromResult(new Message
                {
                    MessageId = request.MessageId,
                    Action = request.Action,
                });
            }
            else
            {
                throw new InvalidOperationException($"Fail to add message {request.MessageId}");
            }
        }
        public override async Task SubEngineStreaming(Message request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var asyncMessage = m_SubEngineMessageQueue.Take(context.CancellationToken);
                    if (asyncMessage != null)
                    {
                        await responseStream.WriteAsync(asyncMessage.GetInput());
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogInfo($"SubEngineStreaming canceled. MessageId={request.MessageId}");
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"SubEngineStreaming error. MessageId={request.MessageId}", ex);
            }
        }


        public override Task<Message> ChildSessionControlUnary(Message request, ServerCallContext context)
        {
            m_Logger.LogInfo($"ChildSessionControlUnary:{request.MessageId};{request.Action}");
            AsyncMessage<Message, Message> asyncMessage = new AsyncMessage<Message, Message>(request.MessageId);
            asyncMessage.AddInput(request);
            if (m_ZdesktopMessageQueue.TryAdd(asyncMessage))
            {
                return Task.FromResult(request);
            }
            else
            {
                throw new InvalidOperationException($"Fail to add message {request.MessageId}");
            }
        }

        public override async Task ServerStreamingToZdesktop(Message request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var asyncMessage = m_ZdesktopMessageQueue.Take(context.CancellationToken);
                    if (asyncMessage != null)
                    {
                        m_Logger.LogInfo($"ServerStreamingToZdesktop:{asyncMessage.Id}");
                        await responseStream.WriteAsync(asyncMessage.GetInput());
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogInfo($"ServerStreamingToZdesktop canceled. MessageId={request.MessageId}");
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"ServerStreamingToZdesktop error. MessageId={request.MessageId}", ex);
            }
        }

        public ServerServiceDefinition BindingToService()
        {
            return ChildSessionService.BindService(this);
            //return ChildSessionService.BindService(this).Intercept(m_Interceptor);
        }

        public Task<string> ServerMessagePublish(string anAction, string aMessage)
        {
            var asyncMessage =
                new AsyncMessageModel<string, string>(aMessage, anAction);
            m_ServerMessageQueue.ForEach(mq => mq.Value.TryAdd(asyncMessage));
            return Task.FromResult(string.Empty);
        }

        private int GetMessageId()
        {
            return MessageIdGenerator.NextMessageId();
        }

    }
}
