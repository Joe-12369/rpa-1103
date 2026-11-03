using Grpc.Core;
using II.RPA.Communication.Endpoints;
using II.RPA.Communication.Hosting;
using II.RPA.Communication.Options;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using Rpa.Core.Runtimehost;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.GRPCServiceClient.ClientProvider
{
    /// <summary>
    /// RuntimeServiceClientProvider
    /// </summary>
    public class RuntimeServiceClientProvider : IDisposable
    {
        private readonly GrpcClient _client;
        private readonly RunTimeService.RunTimeServiceClient _runTimeServiceClient;
        private readonly ILogger _logger;
        private readonly Metadata _headers;
        private static readonly TimeSpan ShutdownCallTimeout = TimeSpan.FromSeconds(5);
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="aChannelName"></param>
        /// <param name="aLogger"></param>
        /// <param name="aConnectionTimeout"></param>
        public RuntimeServiceClientProvider(string aChannelName, ILogger aLogger, int aConnectionTimeout = -1)
        {
            var options = new ClientOptions
            {
                Endpoint = new NamedPipeEndpoint(aChannelName)
            };
            if (aConnectionTimeout > 0)
            {
                options.NamedPipeConnectionTimeoutMilliseconds = aConnectionTimeout;
            }

            _client = new GrpcClient(options);
            _runTimeServiceClient = _client.CreateClient(invoker => new RunTimeService.RunTimeServiceClient(invoker));
            _logger = aLogger;
            _logger.LogInfo($"Create new channel:{aChannelName}");

            byte[] bytes1 = Encoding.UTF8.GetBytes(aChannelName);
            byte[] bytes2 = BitConverter.GetBytes(Global.CurrentPId);
            var tokenbyte = ConcatBytes(bytes1, bytes2);
            _headers = new Metadata
            {
                { "token-bin", tokenbyte },
                { "area1-bin", BitConverter.GetBytes(bytes1.Length) },
                { "area2-bin", BitConverter.GetBytes(bytes2.Length) },
                { "area3-bin", BitConverter.GetBytes(tokenbyte.Length) },
            };
        }

        /// <summary>
        /// BeepUnary
        /// </summary>
        /// <param name="aClient"></param>
        /// <param name="aProcessId"></param>
        /// <param name="aBeepContent"></param>
        /// <param name="aTimeoutMs">大于0时设置单次调用超时(ms)，避免对端无响应时永久阻塞</param>
        /// <returns></returns>
        public string BeepUnary(string aClient, int aProcessId, string aBeepContent, int aTimeoutMs = 0)
        {
            BeepMessage beepMessage = new BeepMessage
            {
                ClientId = aClient,
                ProcessId = aProcessId,
                Payload = aBeepContent
            };
            if (aTimeoutMs <= 0)
            {
                return _runTimeServiceClient.BeepUnary(beepMessage, headers: _headers).Payload;
            }

            // Bound the entire call, including channel setup. A transport deadline alone
            // does not guarantee that a blocked synchronous call returns to the caller.
            var cancellation = new CancellationTokenSource();
            var deadline = DateTime.UtcNow.AddMilliseconds(aTimeoutMs);
            var responseTask = Task.Run(async () =>
            {
                using (var call = _runTimeServiceClient.BeepUnaryAsync(beepMessage,
                    headers: _headers, deadline: deadline, cancellationToken: cancellation.Token))
                {
                    return (await call.ResponseAsync.ConfigureAwait(false)).Payload;
                }
            });
            var timeoutTask = Task.Delay(aTimeoutMs);
            if (Task.WhenAny(responseTask, timeoutTask).GetAwaiter().GetResult() == responseTask)
            {
                try
                {
                    return responseTask.GetAwaiter().GetResult();
                }
                finally
                {
                    cancellation.Dispose();
                }
            }

            // Cancellation callbacks belong to the transport and may themselves block.
            // Keep them off the startup thread and observe any late RPC failure.
            _ = Task.Run(() =>
            {
                try { cancellation.Cancel(); }
                catch (Exception ex) { _logger.LogWarning($"Beep cancellation failed: {ex.Message}"); }
                finally
                {
                    _ = responseTask.ContinueWith(task =>
                    {
                        if (task.IsFaulted) { var observed = task.Exception; }
                        cancellation.Dispose();
                    }, TaskScheduler.Default);
                }
            });
            throw new TimeoutException($"RuntimeHost beep timed out after {aTimeoutMs}ms.");
        }

        /// <summary>
        /// InitUnaryAsync
        /// </summary>
        /// <param name="aPackagePath"></param>
        /// <param name="anInstallationPath"></param>
        /// <param name="aCancellationToken"></param>
        /// <returns></returns>
        public async Task<string> InitUnaryAsync(string aPackagePath, string anInstallationPath, bool anIsAsync, bool aNeedUiThread, CancellationToken aCancellationToken = default)
        {
            InitRequestMessage initRequest = new InitRequestMessage
            {
                IsAsync = anIsAsync,
                NeedUiThread = aNeedUiThread,
                Payload = new InitRequestPayload
                {
                    PackagePath = aPackagePath,
                    InstallDirPath = anInstallationPath
                }
            };
            var ret = await _runTimeServiceClient.InitUnaryAsync(initRequest, cancellationToken: aCancellationToken, headers: _headers);
            return ret.Payload;
        }
        /// <summary>
        /// SetParamsUnaryAsync
        /// </summary>
        /// <param name="aJsonParam"></param>
        /// <param name="aCancellationToken"></param>
        /// <returns></returns>
        public async Task SetParamsUnaryAsync(string aJsonParam, bool anIsAsync, bool aNeedUiThread, CancellationToken aCancellationToken = default)
        {
            SetParamRequestMessage setParamsRequest = new SetParamRequestMessage
            {
                IsAsync = anIsAsync,
                NeedUiThread = aNeedUiThread,
                Payload = new SetParamRequestPayload
                {
                    JsonParam = aJsonParam
                }
            };
            await _runTimeServiceClient.SetParamUnaryAsync(setParamsRequest, cancellationToken: aCancellationToken, headers: _headers);
        }

        /// <summary>
        /// RunUnaryAsync
        /// </summary>
        /// <param name="aPackagePath"></param>
        /// <param name="aPluginName"></param>
        /// <param name="anAction"></param>
        /// <param name="aJsonParam"></param>
        /// <param name="aPluginVersion"></param>
        /// <param name="aCancellationToken"></param>
        /// <returns></returns>
        public async Task<string> RunUnaryAsync(string aPackagePath, string aPluginName, string anAction, string aJsonParam, string aPluginVersion, bool anIsAsync, bool aNeedUiThread, string aRunMode = "", CancellationToken aCancellationToken = default)
        {
            using (WindowRpcDiagnostic.Begin(aPluginName, WindowRpcDiagnostic.RequestId))
            {
                long diagnosticStart = System.Diagnostics.Stopwatch.GetTimestamp();
                if (WindowRpcDiagnostic.RequestId != null)
                    GrpcDotNetNamedPipes.NamedPipeRpcDiagnostic.Observer = WindowRpcDiagnostic.TransportMark;
                WindowRpcDiagnostic.Mark("core_pipe_call_enter");
                try
                {
                    RunRequestMessage runRequest = new RunRequestMessage
                    {
                        IsAsync = anIsAsync,
                        NeedUiThread = aNeedUiThread,
                        Payload = new RunRequestPayload
                        {
                            PackagePath = aPackagePath,
                            PluginName = aPluginName,
                            PluginAction = anAction,
                            PluginParam = aJsonParam,
                            PluginVersion = aPluginVersion,
                            RunMode = aRunMode
                        }
                    };
                    var headers = _headers;
                    if (WindowRpcDiagnostic.RequestId != null)
                    {
                        headers = new Metadata();
                        foreach (var header in _headers) headers.Add(header);
                        headers.Add(WindowRpcDiagnostic.Header, WindowRpcDiagnostic.RequestId);
                    }
                    WindowRpcDiagnostic.Mark("core_pipe_await_begin", diagnosticStart);
                    var runRet = await _runTimeServiceClient.RunUnaryAsync(runRequest, cancellationToken: aCancellationToken, headers: headers);
                    WindowRpcDiagnostic.Mark("core_pipe_await_completed", diagnosticStart);
                    return runRet.Payload;
                }
                finally
                {
                    WindowRpcDiagnostic.Mark("core_pipe_call_exit", diagnosticStart);
                }
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            try
            {
                var disposeMessage = new DisposeRequestMessage();
                _runTimeServiceClient.DisposeUnary(disposeMessage, headers: _headers);
            }
            catch(Exception ex)
            {
                _logger.LogWarning($"DisposeUnary:{ex.Message}");
            }
            finally
            {
                _client.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }


        public bool ShutDown(string aPackagePath)
        {
            try
            {
                ShutDownRequestMessage shutDownRequest = new ShutDownRequestMessage
                {
                    IsAsync = false,
                    NeedUiThread = false,
                    Payload = new ShutDownRequestPayload
                    {
                        ExitCode = 0,
                        PackagePath = aPackagePath
                    }
                };
                _runTimeServiceClient.ShutDownUnary(
                    shutDownRequest,
                    deadline: DateTime.UtcNow.Add(ShutdownCallTimeout));
                return true;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"ShutDownUnary error:{ex}");
                return true;
            }
        }

        public async Task<bool> ShutDownAsync(string aPackagePath)
        {
            try
            {
                ShutDownRequestMessage shutDownRequest = new ShutDownRequestMessage
                {
                    IsAsync = false,
                    NeedUiThread = false,
                    Payload = new ShutDownRequestPayload
                    {
                        ExitCode = 0,
                        PackagePath = aPackagePath
                    }
                };
                await _runTimeServiceClient.ShutDownUnaryAsync(
                    shutDownRequest,
                    deadline: DateTime.UtcNow.Add(ShutdownCallTimeout)).ConfigureAwait(false);
                return true;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"ShutDownUnaryAsync error:{ex}");
                return true;
            }
        }

        private static byte[] ConcatBytes(byte[] array1, byte[] array2)
        {
            byte[] result = new byte[array1.Length + array2.Length];
            Buffer.BlockCopy(array1, 0, result, 0, array1.Length);
            Buffer.BlockCopy(array2, 0, result, array1.Length, array2.Length);
            return result;
        }
    }
}
