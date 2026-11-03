using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.NativeMessageingStartup
{
    internal class BrowserContextHandler
    {


        private static readonly Dictionary<string, Func<JObject, Task<JObject>>> s_myActions;
        private static readonly ConcurrentDictionary<IntPtr, long> s_windowMap;
        private static readonly HashSet<IntPtr> s_processedWindow;
        private static ILogger logger = Logger.OfContext();
        private static readonly Mutex s_registerWindowMutex;

        internal ConcurrentDictionary<IntPtr, long> MyProperty => s_windowMap;
        static BrowserContextHandler()
        {
            s_windowMap = new ConcurrentDictionary<IntPtr, long>();
            s_processedWindow = new HashSet<IntPtr>();
            s_registerWindowMutex = new Mutex(false, @"Global\II.RPA.NativeMessagingHost_registerWindowMutex");
            s_myActions = new Dictionary<string, Func<JObject, Task<JObject>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["registerWindow"] = OnRegisterWindow,
                ["setWindowMap"] = OnSetWindowMapw,
                ["removeWindow"] = OnRemoveWindow
            };
        }

        /// <summary>
        /// 处理浏览器主动推送的消息
        /// </summary>
        /// <param name="msg">TransferMessage</param>
        /// <returns>
        /// </returns>
        public static async void HandleMessageAsync(TransferMessage msg)
        {
            NativeMessage nativeMessage = JObject.FromObject(msg.SendData).ToObject<NativeMessage>();
            if (nativeMessage == null ||
                string.IsNullOrEmpty(nativeMessage.Method) ||
                !s_myActions.ContainsKey(nativeMessage.Method))
            {
                return;
            }
            var parameter = nativeMessage.Params;
            var actioin = s_myActions[nativeMessage.Method];
            JObject ret = await actioin.Invoke(parameter);
            if (ret != null && msg.MessageType == MessageType.NativeMessage)
            {
                msg.MessageType = MessageType.NativeMessageCallback;
                msg.Timestamp = Stopwatch.GetTimestamp();
                msg.Timeout = 5000;
                msg.CallbackReslut = ret;
                await MessageService.SendTransferMessageAsync(msg);
            }
            return;
        }
        #region 具体业务方法
        internal static async Task OnNMHReady()
        {
            var crxVersion = await MessageService.SendTransferMessageAsync(new TransferMessage
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Action = "SendTransferMessage",
                SendData = new NativeMessage { Method = "getVersion" },
                Timeout = 1000,
                Timestamp = Stopwatch.GetTimestamp()
            });
            if (TryParseCrxVersion(crxVersion, out var version) && version >= new Version("4.0.1.204"))
            {
                await MessageService.SendNativeMessageAsync(new NativeMessage { Method = "NMHReady" });
            }
        }

        private static bool TryParseCrxVersion(string jsonResponse, out Version version)
        {
            version = null;
            if (string.IsNullOrEmpty(jsonResponse))
            {
                return false;
            }
            try
            {
                var obj = JObject.Parse(jsonResponse);
                var content = obj["result"]?["content"]?.ToString();
                if (string.IsNullOrEmpty(content))
                {
                    return false;
                }
                return Version.TryParse(content, out version);
            }
            catch
            {
                return false;
            }
        }

        private static async Task<JObject> OnRegisterWindow(JObject @params)
        {
            try
            {
                await Task.Delay(50);

                await ProcessSnapshot.WaitProcessSnapshotAsync(3000);
                WindowInfo windowInfo = @params.ToObject<WindowInfo>();

                //Rectangle wndRect = windowInfo.GetRectangle();

                s_registerWindowMutex.WaitOne();
                IntPtr targetWnd = IntPtr.Zero;
                for (int i = 0; i < 3; i++)
                {
                    targetWnd = NativeMethods.MatchDesktopWindowInZorder(hWnd => hWnd)
                                                    .Where(hWnd => !NativeMethods.GetWindowText(hWnd).StartsWith("DevTools - ")
                                                                   && IsSameParentWnd(hWnd)
                                                                   && !s_windowMap.ContainsKey(hWnd))
                                                    .FirstOrDefault();
                    if (targetWnd != IntPtr.Zero) break;
                    Thread.Sleep(50);
                }

                //var targetWnd = candidateHandles.FirstOrDefault(hWnd => !s_processedWindow.Contains(hWnd)
                //                                              && NativeMethods.GetWindowRect(hWnd).Equals(wndRect));
                //if (targetWnd == IntPtr.Zero)
                //    targetWnd = candidateHandles.FirstOrDefault(hWnd => !s_windowMap.ContainsKey(hWnd)
                //                                              && NativeMethods.GetWindowRect(hWnd).Equals(wndRect));

                if (targetWnd != IntPtr.Zero)
                {
                    return new JObject() { { "wndId", windowInfo.wndId }, { "wndHandle", targetWnd.ToInt64() } };
                }
                return ErrorObj("未能匹配到合适的窗口");
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "注册新窗口异常");
                return ErrorObj("注册新窗口异常");
            }
            finally
            {
                s_registerWindowMutex.ReleaseMutex();
            }
        }
        private static async Task<JObject> OnSetWindowMapw(JObject @params)
        {
            await Task.CompletedTask;
            try
            {
                Dictionary<long, long> map = @params.ToObject<Dictionary<long, long>>();
                foreach (var kv in map)
                {
                    s_windowMap.AddOrUpdate(new IntPtr(kv.Value), kv.Key, (k, v) => kv.Key);
                }
                return SuccessObj("set suc");
            }
            catch (Exception ex)
            {
                return ErrorObj(ex.Message);
            }
        }

        private static async Task<JObject> OnRemoveWindow(JObject @params)
        {
            await Task.CompletedTask;
            try
            {
                var wndId = @params.Value<long>("wndId");
                var kv = s_windowMap.FirstOrDefault(item => item.Value == wndId);
                s_windowMap.TryRemove(kv.Key, out var _);
                return SuccessObj("set suc");
            }
            catch (Exception ex)
            {
                return ErrorObj(ex.Message);
            }
        }

        #endregion
        private static async Task<bool> WaitProcessSnapshot(int timeoutMs = 1000)
        {
            using (var cts = new CancellationTokenSource(timeoutMs))
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        if (ProcessSnapshot.ParentProcessInfo == null)
                        {
                            await Task.Delay(10, cts.Token);
                            continue;
                        }
                        return true;
                    }
                }
                catch //(TaskCanceledException)
                {
                    // 超时是正常情况，不记录异常
                }
                return false;
            }
        }
        private static bool IsSameParentWnd(IntPtr hWnd)
        {
            try
            {
                logger.LogDebug("IsSameInstanceWnd" + hWnd.ToString());

                NativeMethods.GetWindowThreadProcessId(hWnd, out var procId);
                return procId == ProcessSnapshot.ParentProcessInfo?.Pid;
            }
            catch (Exception ex) //(TaskCanceledException)
            {
                logger.LogDebug($"IsSameInstanceWnd 异常:{ex}");
                // 超时是正常情况，不记录异常
            }
            return false;
        }

        private static JObject SuccessObj(string msg) => new JObject { ["success"] = msg };
        private static JObject ErrorObj(string msg) => new JObject { ["error"] = msg };
    }
}
