using Grpc.Core;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rpa.Core.Process;
using System;
using System.ComponentModel;
using System.IO;
using System.Resources;
using System.Threading.Tasks;

namespace II.RPA.RuntimeHost.Services
{
    /// <summary>
    /// ProcessServiceClientProvider
    /// 请求coreserver
    /// </summary>
    public class ProcessServiceClientProvider
    {
        private ProcessService.ProcessServiceClient _client;
        private GrpcClient _connection;
        private readonly static ILogger _logger = Logger.OfContext();
        private readonly object _syncLock_SendToCoreserver = new object();

        /// <summary>
        /// .ctor
        /// </summary>
        public ProcessServiceClientProvider()
        {

        }

        /// <summary>
        /// Client
        /// </summary>
        public ProcessService.ProcessServiceClient Client
        {
            get
            {
                if (_client == null)
                {
                    if (GlobleConfigurations.CoreServerPort > 0)
                    {
                        _client = CreatClient(GlobleConfigurations.CoreServerPort);
                    }
                    else
                    {
                        int port = ReadPort();
                        if (port > 0)
                        {
                            _client = CreatClient(port);
                        }
                        else
                        {
                            throw new ArgumentException("Can not get _corePort!");
                        }
                    }
                }
                return _client;
            }
        }

        /// <summary>
        /// 当前流程包路径
        /// </summary>
        public string PackagePath
        {
            get;
            set;
        }

        /// <summary>
        /// 向coreserver发送消息、同步。建议使用异步方法
        /// </summary>
        /// <param name="aTerminalType"></param>
        /// <param name="anAction"></param>
        /// <param name="aParam"></param>
        /// <returns></returns>
        public string SendMessageHandler(TerminalType aTerminalType,
            string anAction,
            object aParam)
        {
            try
            {
                _logger.LogDebug($"Runtimehost send message: {aTerminalType};{anAction};{aParam}");
                string jsonText = "null";
                //无限超时
                var deadLine = DateTime.MaxValue;

                if (aTerminalType == TerminalType.Engine)
                {
                    var result = Client.NotifyEngineUnary(
                        new Message
                        {
                            Action = anAction,
                            Payload = aParam.ToString()
                        },
                        deadline:deadLine);

                    jsonText = result.Payload;
                    return JsonConvert.SerializeObject(new JMessage
                    {
                        Code = 0,
                        Message = jsonText,
                        Info = jsonText,
                    });
                }
                else if (aTerminalType == TerminalType.Factory)
                {
                    var result = Client.NotifyClientUnary(new Message
                    {
                        Action = anAction,
                        Payload = aParam.ToString()
                    },
                    deadline: deadLine);

                    jsonText = result.Payload;
                    return JsonConvert.SerializeObject(new JMessage
                    {
                        Code = 0,
                        Message = jsonText,
                        Info = jsonText,
                    });
                }
                else if (aTerminalType == TerminalType.CoreServer)
                {
                    try
                    {
                        var result = Client.NotifyCoreServerUnary(new Message
                        {
                            Action = anAction,
                            Payload = aParam.ToString()
                        },
                        deadline: deadLine);

                        return JsonConvert.SerializeObject(new JMessage
                        {
                            Code = 0,
                            Message = "",
                            Info = result.Payload,
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                        return JsonConvert.SerializeObject(new JMessage
                        {
                            Code = 1,
                            Message = ex.Message,
                            Info = "",
                        });
                    }
                }
                else if (aTerminalType == TerminalType.Bot)
                {
                    var result = Client.NotifyBotUnary(new Message
                    {
                        Action = anAction,
                        Payload = aParam.ToString()
                    },
                    deadline: deadLine);

                    jsonText = result.Payload;
                    return JsonConvert.SerializeObject(new JMessage
                    {
                        Code = 0,
                        Message = jsonText,
                        Info = jsonText,
                    });
                }
                else
                {
                    return JsonConvert.SerializeObject(new JMessage
                    {
                        Code = 1,
                        Message = $"Invalid terminal {aTerminalType}",
                        Info = $"Invalid terminal {aTerminalType}",
                    });
                }
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex);
                return JsonConvert.SerializeObject(new JMessage
                {
                    Code = 1,
                    Message = "time out",
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex);
                return JsonConvert.SerializeObject(new JMessage
                {
                    Code = 1,
                    Message = ex.Message,
                });
            }
        }


        /// <summary>
        /// 向coreserver发送消息、异步
        /// todo:改用TAP异步模型实现
        /// </summary>
        /// <param name="aTerminalType"></param>
        /// <param name="anAction"></param>
        /// <param name="aParam"></param>
        /// <param name="aResultAction"></param>
        /// <returns></returns>
        public bool PostMessageHandler(TerminalType aTerminalType,
            string anAction,
            object aParam,
            Action<object> aResultAction)
        {
            _logger.LogDebug($"Runtimehost post message: {aTerminalType};{anAction};{aParam}");
            var task =
            Task.Factory.StartNew(() =>
            {
                JMessage message = new JMessage();
                //5s超时
                var deadLine = DateTime.UtcNow.AddMilliseconds(5000);
                try
                {
                    if (aTerminalType == TerminalType.Engine)
                    {
                        var result = Client.NotifyEngineUnary(new Message
                        {
                            Action = anAction,
                            Payload = JsonConvert.SerializeObject(aParam)
                        },
                        deadline:deadLine);

                        message.Info = result.Payload;
                        message.Code = result.Code;
                        if (message.Code == 1)
                        {
                            message.Message = message.Info;
                        }

                        aResultAction?.Invoke(JsonConvert.SerializeObject(message));
                    }
                    else if (aTerminalType == TerminalType.Factory)
                    {
                        message.Info = Client.NotifyClientUnary(new Message
                        {
                            Action = anAction,
                            Payload = JsonConvert.SerializeObject(aParam)
                        },
                        deadline: deadLine).Payload;

                        message.Code = string.IsNullOrEmpty(message.Info) ? 1 : 0;
                        message.Message = string.IsNullOrEmpty(message.Info) ? "selector from factory is null or empty" : "ok";

                        aResultAction?.Invoke(JsonConvert.SerializeObject(message));
                    }
                    else if (aTerminalType == TerminalType.CoreServer)
                    {
                        if (anAction == "visualdata.excel")
                        {
                            var ret = Client.NotifyVisualDataUnary(new Message
                            {
                                Action = anAction,
                                Payload = JsonConvert.SerializeObject(aParam)
                            },
                            deadline: deadLine);

                            aResultAction?.Invoke(ret);
                        }
                        else
                        {
                            var ret = Client.NotifyCoreServerUnary(new Message
                            {
                                Action = anAction,
                                Payload = JsonConvert.SerializeObject(aParam)
                            },
                            deadline: deadLine);

                            aResultAction?.Invoke(ret);
                        }
                    }
                    else
                    {
                        //todo
                    }
                }
                catch (TimeoutException)
                {
                    _logger.LogError(nameof(TimeoutException));
                    message.Message = "time out";
                    message.Info = "time out";
                    message.Code = 1;

                    aResultAction?.Invoke(JsonConvert.SerializeObject(message));

                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"PostMessageHandler Return:{ex.GetType().FullName} {ex.Message}");
                    message.Message = ex.Message;
                    message.Info = ex.Message;
                    message.Code = 1;

                    aResultAction?.Invoke(JsonConvert.SerializeObject(message));
                }
                finally
                {
                    _logger.LogDebug($"Runtimehost post message return:{message.Code}");
                }
            });
            task.ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });

            return !task.IsFaulted && !task.IsCanceled;
        }

        /// <summary>
        /// 仅用于元素查找结果同步
        /// </summary>
        /// <param name="action"></param>
        /// <param name="jsonParam"></param>
        /// <returns></returns>
        public string SendToCoreserverHandler(SendMessageToCoeServerAction action,
            object jsonParam)
        {
            _logger.LogDebug($"Runtimehost send to coreserver: {action};{jsonParam}");
            //5s超时 元素查找续期通知
            var deadLine = DateTime.UtcNow.AddSeconds(5);
            Message retMsg = null;
            Message reqMsg = new Message
            {
                MessageId = GetMessageId(),
                PackagePath = PackagePath,
                ClientId = GetDescriptionByEnum(GlobleConfigurations.ClientType),
                Action = action.ToString(),
                Payload = JsonConvert.SerializeObject(jsonParam)
            };
            if (action == SendMessageToCoeServerAction.ResetPluginLoaderRunTimeout)
            {
                try
                {
                    retMsg = Client.ResetPluginLoaderRunTimeout(reqMsg, deadline: deadLine);
                    _logger.LogDebug($"Return from coreserver:{JsonConvert.SerializeObject(retMsg)}");
                }
                catch (RpcException re)
                {
                    if (re.StatusCode == StatusCode.DeadlineExceeded)
                    {
                        _logger.LogWarning($"{re.StatusCode}:{re.Message}");
                    }
                    else
                    {
                        _logger.LogWarning(re);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e);
                }
            }
            return retMsg?.Payload;
        }

        private ProcessService.ProcessServiceClient CreatClient(int aPort)
        {
            _connection = new GrpcClient(new ClientOptions
            {
                Endpoint = new TcpEndpoint("127.0.0.1", aPort)
            });
            var client = _connection.CreateClient(invoker => new ProcessService.ProcessServiceClient(invoker));
            return client;
        }
        private int GetMessageId()
        {
            var idstring = $"{ConvertDateTimeToInt()}{new Random().Next(100, 999)}";
            return int.Parse(idstring.PadRight(8, '0'));
        }
        private static int ConvertDateTimeToInt()
        {
            var time = DateTime.Now;
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, 0));
            long t = (time.Ticks - startTime.Ticks) / 10000;
            var part = t.ToString().Substring(7, 5);
            return int.Parse(part);
        }
        private int ReadPort()
        {
            var fileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\{GetDescriptionByEnum(GlobleConfigurations.ClientType)}.txt";
            if (File.Exists(fileName))
            {
                string text = File.ReadAllText(fileName);
                _logger.LogDebug($"Read File:{fileName}");
                _logger.LogDebug($"Read Port:{text}");
                if (!string.IsNullOrEmpty(text))
                {
                    return Convert.ToInt32(text);
                }
            }
            else
            {
                if (GlobleConfigurations.ClientType == ClientType.Factory)
                {
                    return 7181;
                }
                else if (GlobleConfigurations.ClientType == ClientType.Bot)
                {
                    return 7182;
                }
                else if (GlobleConfigurations.ClientType == ClientType.DeskTop)
                {
                    return 7183;
                }
                else
                {
                    return 9900;
                }
            }
            return -1;
        }
        private string GetDescriptionByEnum(Enum enumValue)
        {
            string value = enumValue.ToString();
            System.Reflection.FieldInfo field = enumValue.GetType().GetField(value);
            object[] objs = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (objs.Length == 0)
                return value;
            DescriptionAttribute descriptionAttribute = (DescriptionAttribute)objs[0];
            return descriptionAttribute.Description;
        }
    }
}
