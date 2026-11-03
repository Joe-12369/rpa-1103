using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.IPC;
using II.RPA.Logging;
using II.RPA.Logging.Formatting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace II.RPA.NativeMessageingStartup
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        IncludeExceptionDetailInFaults = true,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class HostService : IMessageExchange, IDisposable
    {
        private static ConcurrentDictionary<string, TransferEntry> connectionMap = new ConcurrentDictionary<string, TransferEntry>();
        private const int STD_IN_READ_TIMEOUT = -1;
        private readonly object msgLock = new object();
        private readonly AutoResetEvent msgProcessed = new AutoResetEvent(initialState: false);
        private readonly AutoResetEvent msgReceived = new AutoResetEvent(initialState: false);
        private object currentReply = string.Empty;
        private static ILogger logger = Logger.OfContext();
        private static readonly int defaultTimeout;

        static HostService()
        {
            defaultTimeout = Convert.ToInt32(ConfigurationManager.AppSettings["defTimeout"]);
        }

        public void Dispose()
        {
        }

        public void Pending()
        {
        }

        public IResponseResult Request(IRequestParams request)
        {
            logger.LogDebugBlock(() =>
            {
                logger.LogDebug($"New Pipe request of serivce ({GetHashCode()}), {LogBlock.Json(request.Parameters)}");
            });
            string ret = string.Empty;

            // 防止 WCF 上下文死锁
            var prev = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                TransferMessage tMsg = GetTransferMessage(request.Parameters);
                ret = MessageService.SendTransferMessageAsync(tMsg).ConfigureAwait(false)
                                   .GetAwaiter()
                                   .GetResult();

                ResponseResult responseResult = new ResponseResult()
                {
                    Result = ret
                };
                logger.LogDebugBlock(() =>
                {
                    logger.LogDebug($"New Pipe Reply, {LogBlock.Json(ret)}");
                });
                return responseResult;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"Error New Pipe request of serivce ({GetHashCode()}), {LogBlock.Json(request.Parameters)}");
                throw;
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prev);
            }
        }
        private TransferMessage GetTransferMessage(string requestParameters)
        {
            if (string.IsNullOrWhiteSpace(requestParameters))
                throw new ArgumentException("Request parameters cannot be null or empty.", nameof(requestParameters));

            try
            {
                JObject paramsObj = JObject.Parse(requestParameters);

                // 如果已经是完整 TransferMessage，直接返回
                if (paramsObj.ContainsKey("MessageId"))
                {
                    return JsonConvert.DeserializeObject<TransferMessage>(requestParameters);
                }

                int timeout = defaultTimeout;
                if (paramsObj.TryGetValue("method", out JToken method) &&
                    method?.ToString() == "elements.executeScript")
                {
                    timeout = 30 * 60 * 1000; // 或从配置读取
                }

                return new TransferMessage
                {
                    MessageId = Guid.NewGuid().ToString("N"),
                    Action = "SendTransferMessage",
                    SendData = paramsObj,
                    Timeout = timeout,
                    Timestamp = Stopwatch.GetTimestamp()
                };
            }
            catch (JsonException ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Invalid JSON in request parameters.");
                throw; // 让上层处理
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Unexpected error in GetTransferMessage.");
                throw;
            }
        }
    }

}
