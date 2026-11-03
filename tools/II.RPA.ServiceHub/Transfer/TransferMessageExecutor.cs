using II.RPA.Logging;
using II.RPA.ServiceHub.Transfer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.ServiceHub
{
    public class TransferMessageExecutor
    {

        private ConcurrentDictionary<string, TransferEntry> transferEntryMap = new ConcurrentDictionary<string, TransferEntry>();
        private ConcurrentQueue<TransferEntry> messageQueue = new ConcurrentQueue<TransferEntry>();
        private readonly IHubContext<TransferHub> _hubContext;

        private ILogger logger = Logger.OfContext();


        private static AutoResetEvent _resetEvent;

        public TransferMessageExecutor(IHubContext<TransferHub> hubContext)
        {
            _hubContext = hubContext;
            _resetEvent = new AutoResetEvent(false);
            Start();
        }
        private void Start()
        {
            Task.Factory.StartNew(async () =>
            {
                _resetEvent.WaitOne();
                while (true)
                {
                    while (messageQueue.Count > 0)
                    {
                        await ConsumeMessage();
                    }
                    _resetEvent.WaitOne();
                }
            }, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (transferEntryMap.Count > 0)
                    {
                        transferEntryMap.Where(entry => entry.Value.Timeout > 0 && entry.Value.ElapsedMilliseconds >= entry.Value.Timeout + 5000)
                            .ToList()
                            .ForEach(entry =>
                            {
                                RemoveTransferEntry(entry.Key);
                            });
                    }
                    Thread.Sleep(5000);
                }
            }, TaskCreationOptions.LongRunning);
        }
        public void QueuingMessage(string connectionId, string message)
        {
            logger.LogDebug($"QueuingMessage connectionId:{connectionId}");


            //logger.LogDebug($"connectionId:{connectionId},data:{message}");
            TransferMessage tMmsg = JsonConvert.DeserializeObject<TransferMessage>(message);
            if (string.IsNullOrEmpty(tMmsg.MessageId))
            {
                tMmsg.MessageId = Guid.NewGuid().ToString("N");
            }

            ConnectionInfo connInfo = ClientConnections.GetConnectionInfo(connectionId);
            TransferEntry entry = new TransferEntry
            {
                TransferMessage = tMmsg,
                Timeout = tMmsg.Timeout,
                Timestamp = tMmsg.Timestamp
            };

            entry.ConnectionInfo = connInfo;
            //entry.ServerConn = connection;
            if (entry.MessageState == MessageState.None)
            {
                entry.MessageState = MessageState.WaitSend;
            }
            messageQueue.Enqueue(entry);
            _resetEvent.Set();
        }
        private async Task ConsumeMessage()
        {
            await Task.Run(async () =>
            {
                try
                {
                    if (!messageQueue.TryDequeue(out var transferEntry))
                    {
                        return;
                    }
                    //消息已经超时，忽略处理
                    if (transferEntry.Timeout > 0 && transferEntry.ElapsedMilliseconds >= transferEntry.Timeout)
                    {
                        RemoveTransferEntry(transferEntry.MessageId);
                        return;
                    }
                    //无超时限制，但时间已经超过5秒，不再重试
                    if (transferEntry.Timeout == 0 && transferEntry.ElapsedMilliseconds > 5000 && transferEntry.ReEnqueueCount > 1)
                    {
                        RemoveTransferEntry(transferEntry.MessageId);
                        return;
                    }
                    if (IsReEnqueue(transferEntry))//是否需要重新排队
                    {
                        return;
                    }
                    if (transferEntry.TransferMessage.MessageType == MessageType.Collback)
                    {

                        logger.LogDebug($"QueuingMessage CallbackReslut ：{transferEntry.MessageId}");
                        await Callback(transferEntry);
                    }
                    else
                    {
                        logger.LogDebug($"QueuingMessage SendData       :{transferEntry.MessageId} {JObject.FromObject(transferEntry.TransferMessage.SendData)["method"]}");
                        await Transfer(transferEntry);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"消息处理异常", ex);
                }
            });
        }

        private async Task Transfer(TransferEntry transferEntry)
        {
            var clientConns = ClientConnections.GetConnectionInfo(transferEntry.TransferMessage.TargetClientType, transferEntry.TransferMessage.TargetClientFlag, ConnectionState.Connected);
            if (clientConns.Count == 0)
            {
                return;
            }
            await _hubContext.Clients.Clients(clientConns.Select(conn => conn.ConnectionId).ToList()).SendAsync("newTransferMessage", transferEntry.TransferMessage);
            //transferEntry.ReEnqueueCount = 0;//重置
            transferEntryMap.TryAdd(transferEntry.MessageId, transferEntry);
            return;
        }
        private async Task Callback(TransferEntry transferEntry)
        {
            if (!transferEntryMap.ContainsKey(transferEntry.MessageId))
            {
                return;
            }
            var requestMsg = transferEntryMap[transferEntry.MessageId];
            if (requestMsg == null) return;
            ConnectionInfo connInfo = null;
            if (requestMsg.ConnectionInfo != null)
            {
                connInfo = ClientConnections.GetConnectionInfo(requestMsg.ConnectionInfo.ConnectionId);
            }

            if (connInfo == null)
            {
                connInfo = ClientConnections.GetConnectionInfo(
                    requestMsg.ConnectionInfo.ClientType,
                    requestMsg.ConnectionInfo.ClientFlag,
                    ConnectionState.Connected
                    ).FirstOrDefault();
                if (connInfo == null)
                {
                    return;
                }
            }
            await _hubContext.Clients.Client(connInfo.ConnectionId).SendAsync("newTransferMessage", transferEntry.TransferMessage);
            RemoveTransferEntry(transferEntry.MessageId);
        }

        private bool IsReEnqueue(TransferEntry transferEntry)
        {
            //目标端不存在，且尚未到超时时间，将数据异步放回队列末尾，不能确保最终能成功发送，因超时机制
            var clientCount = ClientConnections.GetConnectionInfoCount(transferEntry.TransferMessage.TargetClientType, transferEntry.TransferMessage.TargetClientFlag, ConnectionState.Connected);

            if (clientCount == 0)
            {
                Task.Factory.StartNew(() =>
                {
                    if (messageQueue.Count == 0)
                    {
                        Thread.Sleep(200);
                    }
                    transferEntry.ReEnqueueCount++;
                    messageQueue.Enqueue(transferEntry);
                    _resetEvent.Set();
                }).ConfigureAwait(false).GetAwaiter().OnCompleted(() => { });
                return true;
            }
            return false;
        }
        private void RemoveTransferEntry(string messageId)
        {
            if (transferEntryMap.ContainsKey(messageId))
            {
                transferEntryMap.TryRemove(messageId, out _);
            }
        }
    }
}
