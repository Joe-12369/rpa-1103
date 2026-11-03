using II.RPA.Logging;
using II.RPA.Logging.Formatting;
using II.RPA.NativeMessageingStartup.wndMapping;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.NativeMessageingStartup
{
    internal class MessageService
    {
        private const string NATIVE_MESSAGE = "NativeMessage";
        private static readonly object msgLock = new object();
        private static readonly CancellationTokenSource _cts;
        private static ILogger logger = Logger.OfContext();
        private static readonly MessageReader s_stdInput;
        private static readonly MessageWriter s_stdOutput;
        private static readonly TransferRequestDispatcher s_dispatcher;
        private static readonly KeepAliveManager s_KeepAliveManager;

        private const int ERROR_BROKEN_PIPE = 232;
        private const int ERROR_NO_DATA = 109;
        private static bool IsPipeBroken(IOException ex) =>
            ex.Message.IndexOf("pipe", StringComparison.OrdinalIgnoreCase) != -1
            || ex.Message.IndexOf("length of stream", StringComparison.OrdinalIgnoreCase) != -1
            || ex.InnerException is System.ComponentModel.Win32Exception win32 && (win32.NativeErrorCode == ERROR_BROKEN_PIPE
            || win32.NativeErrorCode == ERROR_NO_DATA);
        static MessageService()
        {
            _cts = new CancellationTokenSource();
            s_stdInput = new MessageReader(Console.OpenStandardInput());
            s_stdOutput = new MessageWriter(Console.OpenStandardOutput());
            s_dispatcher = new TransferRequestDispatcher(SendToCrxAsync);
            //s_KeepAliveManager = new KeepAliveManager(async ct => await SendNativeMessageAsync(new NativeMessage { Method = "KeepLive" }), 3000);
        }
        internal static Task Startup()
        {
            return Task.Run(StartupMessageService);
            //return Task.Factory.StartNew(async () => await StartupMessageService(), TaskCreationOptions.LongRunning);
        }

        private static async Task StartupMessageService()
        {
            // 先启动消息读取循环，让 ReadMessage 进入阻塞监听状态
            var readLoopTask = RunReadLoopAsync();

            // 给读循环一点时间进入 ReadMessage 阻塞状态
            await Task.Delay(10);

            // 读循环已在监听，此时发送 NMHReady，浏览器响应能立即被消费
            await BrowserContextHandler.OnNMHReady();

            // 等待读循环结束（通常不会，除非浏览器断开连接）
            await readLoopTask;
        }

        private static async Task RunReadLoopAsync()
        {
            await Task.Yield(); // 确保异步上下文切换，避免阻塞调用线程
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    string rawMessage = s_stdInput.ReadMessage();

                    // 1: 检测 EOF（浏览器关闭）
                    if (rawMessage == null)
                    {
                        logger.LogInformation("Browser disconnected (EOF received). Exiting native host.");
                        Environment.Exit(0); // 符合 Chrome Native Host规范
                    }

                    // 2: 安全记录日志
                    logger.LogDebugBlock(() =>
                    {
                        logger.LogDebug($"Received message from browser: {rawMessage}");
                    });

                    // 3: 安全反序列化
                    if (string.IsNullOrWhiteSpace(rawMessage))
                    {
                        logger.LogWarning("Received empty or whitespace-only message. Skipping.");
                        continue;
                    }

                    TransferMessage tMsg = JsonConvert.DeserializeObject<TransferMessage>(rawMessage);

                    if (tMsg == null || string.IsNullOrEmpty(tMsg.Action))
                    {
                        logger.LogWarning("Failed to deserialize message. Skipping.");
                        continue;
                    }
                    if (tMsg.Action?.Equals(NATIVE_MESSAGE) == true)
                    {
                        BrowserContextHandler.HandleMessageAsync(tMsg);
                        continue;
                    }

                    if (!s_dispatcher.TryCompleteResponse(tMsg))
                    {
                        logger.LogDebug($"Dropping expired or unknown message: {tMsg.MessageId}");
                    }
                }
                catch (IOException ex)
                {
                    // 4: 任何 IO 错误都视为连接断开
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "IO error during message reading. Assuming browser disconnected.");
                    Environment.Exit(1);
                }
                catch (JsonException ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Failed to parse message as JSON. Dropping message.");
                    // 继续循环
                }
                catch (Exception ex)
                {
                    // 其他未预期异常
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Unexpected error in message loop. Exiting host.");
                    Environment.Exit(1);
                }
            }
        }

        public void SendKeepAlive()
        {
            //异步线程，每3秒发送一次KeepAlive消息
            Task.Run(async () =>
            {
                while (true)
                {
                    Thread.Sleep(3000);
                    try
                    {
                        logger.LogDebug($"Send KeepAlive from Host");
                        await MessageService.SendNativeMessageAsync(new NativeMessage { Method = "KeepLive" });
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"Error SendKeepAliveInternal");
                    }
                }
            });


        }

        internal static async Task<string> SendNativeMessageAsync(NativeMessage msg, int timeout = 100)
        {
            TransferMessage message = new TransferMessage
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Action = NATIVE_MESSAGE,
                SendData = msg,
                Timeout = timeout,
                MessageType = MessageType.NativeMessage,
                Timestamp = Stopwatch.GetTimestamp()
            };
            return await SendTransferMessageAsync(message);
        }

        internal static async Task<string> SendTransferMessageAsync(TransferMessage msg)
        {
            if (msg == null) return null;

            TransferDispatchResult result = await DispatchTransferMessageAsync(
                msg,
                CancellationToken.None).ConfigureAwait(false);
            return result.Response?.CallbackReslut?.ToString();
        }

        internal static Task<TransferDispatchResult> DispatchTransferMessageAsync(
            TransferMessage msg,
            CancellationToken connectionCancellation)
        {
            return s_dispatcher.DispatchAsync(msg, connectionCancellation);
        }

        private static async Task SendToCrxAsync(TransferMessage message)
        {
            //TODO:目前用的输出流底层并未真正实现WriteAsync,因此这里保持使用原来的BinaryWriter 同步写入，暂时不将其封装为真异步
            var json = JsonConvert.SerializeObject(message);
            try
            {
                await s_stdOutput.WriteMessageAsync(json).ConfigureAwait(false);
            }
            catch (IOException ex) when (IsPipeBroken(ex))
            {
                Debug.Assert(false, "stdout pipe broken, MESSAGE HOST EXIT(01)");
                logger.LogError("stdout pipe broken, MESSAGE HOST EXIT(01)");
                Environment.Exit(1);
            }
        }
    }
}
