using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading;

namespace II.RPA.WebBrowser.Common.Basic
{
    internal interface ICrxScriptInitializationRetryClock
    {
        long ElapsedMilliseconds { get; }
        void Restart();
        void Delay(int milliseconds);
    }

    internal sealed class CrxScriptInitializationRetryClock : ICrxScriptInitializationRetryClock
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;
        public void Restart() => _stopwatch.Restart();
        public void Delay(int milliseconds) => Thread.Sleep(milliseconds);
    }

    internal sealed class CrxScriptInitializer
    {
        private const int ContentRetryBudgetMilliseconds = 10000;
        private const int ContentRetryIntervalMilliseconds = 500;
        private readonly ICrxScriptInitializationRetryClock _retryClock;

        internal CrxScriptInitializer()
            : this(new CrxScriptInitializationRetryClock())
        {
        }

        internal CrxScriptInitializer(ICrxScriptInitializationRetryClock retryClock)
        {
            _retryClock = retryClock ?? throw new ArgumentNullException(nameof(retryClock));
        }

        internal CrxScriptInitializationResult Initialize(
            IBrowserConnectionNew connection,
            eGuiType guiType,
            int attempt)
        {
            var stopwatch = Stopwatch.StartNew();
            CrxScriptInitializationStage stage = CrxScriptInitializationStage.TestConnection;
            PipeResponse response = null;

            try
            {
                if (connection == null || !connection.TestConnection(guiType, 300))
                {
                    if (connection != null)
                    {
                        connection.InitScripeState = InitScripeStateType.None;
                    }

                    return connection == null
                        ? Result(CrxScriptInitializationCompletion.InitializationFailed)
                        : Result(
                            CrxScriptInitializationCompletion.ConnectionFailed,
                            exception: new BrowserConnectionException(
                                "Browser connection is unavailable during script initialization.",
                                ClientTransferCompletionReason.ConnectionClosed));
                }

                connection.InitScripeState = InitScripeStateType.Initializing;
                stage = CrxScriptInitializationStage.GetVersion;
                if (connection.CrxVersion == null)
                {
                    response = connection.Send(guiType, "getVersion", new JObject(), 1000);
                    connection.CrxVersion = response?.Result?.Content?.ToString();
                }

                if (CrxVersionHelper.GetCrxVersion(connection.CrxVersion) == CrxVersion.V3)
                {
                    connection.InitScripeState = InitScripeStateType.Complete;
                    stage = CrxScriptInitializationStage.Complete;
                    return Result(
                        CrxScriptInitializationCompletion.Success,
                        CrxScriptInitializationPath.Mv3NoLegacyInjection);
                }

                stage = CrxScriptInitializationStage.GetCodeId;
                response = connection.Send(
                    guiType,
                    "getCodeId",
                    new JObject { { "codeId", ExtensionProvider.Instance.CodeId } },
                    1000);
                if (response?.Result?.Content?.ToString() == ExtensionProvider.Instance.CodeId)
                {
                    connection.InitScripeState = InitScripeStateType.Complete;
                    stage = CrxScriptInitializationStage.Complete;
                    return Result(
                        CrxScriptInitializationCompletion.Success,
                        CrxScriptInitializationPath.CodeIdMatched);
                }

                stage = CrxScriptInitializationStage.Init;
                response = connection.Send(
                    guiType,
                    "init",
                    new JObject { { "code", ExtensionProvider.Instance.Backgroud } },
                    1000);
                if (!IsSuccessful(response))
                {
                    connection.InitScripeState = InitScripeStateType.None;
                    return Result(CrxScriptInitializationCompletion.InitializationFailed);
                }

                stage = CrxScriptInitializationStage.InitContentScript;
                _retryClock.Restart();
                BrowserConnectionException lastTimeout = null;
                while (true)
                {
                    int remaining = (int)Math.Max(0,
                        ContentRetryBudgetMilliseconds - _retryClock.ElapsedMilliseconds);
                    if (remaining == 0)
                    {
                        connection.InitScripeState = InitScripeStateType.None;
                        return Result(CrxScriptInitializationCompletion.InitializationFailed,
                            exception: lastTimeout);
                    }

                    response = null;
                    lastTimeout = null;
                    try
                    {
                        response = connection.Send(
                            guiType,
                            "initContentScript",
                            new JObject { { "code", ExtensionProvider.Instance.ContentNotIE } },
                            Math.Min(2000, remaining));
                    }
                    catch (BrowserConnectionException exception)
                        when (exception.Reason == ClientTransferCompletionReason.Timeout)
                    {
                        lastTimeout = exception;
                    }
                    if (IsSuccessful(response))
                    {
                        break;
                    }

                    if (lastTimeout == null && !IsRetryableContentError(response))
                    {
                        connection.InitScripeState = InitScripeStateType.None;
                        return Result(CrxScriptInitializationCompletion.InitializationFailed);
                    }

                    remaining = (int)Math.Max(0,
                        ContentRetryBudgetMilliseconds - _retryClock.ElapsedMilliseconds);
                    _retryClock.Delay(Math.Min(ContentRetryIntervalMilliseconds, remaining));
                    if (_retryClock.ElapsedMilliseconds >= ContentRetryBudgetMilliseconds)
                    {
                        connection.InitScripeState = InitScripeStateType.None;
                        return Result(CrxScriptInitializationCompletion.InitializationFailed,
                            exception: lastTimeout);
                    }
                }

                connection.InitScripeState = InitScripeStateType.Complete;
                stage = CrxScriptInitializationStage.Complete;
                return Result(
                    CrxScriptInitializationCompletion.Success,
                    CrxScriptInitializationPath.Injected);
            }
            catch (Exception exception)
            {
                if (connection != null)
                {
                    connection.InitScripeState = InitScripeStateType.None;
                }

                return Result(
                    IsConnectionFailure(exception)
                        ? CrxScriptInitializationCompletion.ConnectionFailed
                        : CrxScriptInitializationCompletion.InitializationFailed,
                    exception: exception);
            }

            CrxScriptInitializationResult Result(
                CrxScriptInitializationCompletion completion,
                CrxScriptInitializationPath path = CrxScriptInitializationPath.None,
                Exception exception = null)
            {
                stopwatch.Stop();
                return new CrxScriptInitializationResult(
                    completion,
                    stage,
                    path,
                    connection,
                    response,
                    exception,
                    attempt,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        private static bool IsSuccessful(PipeResponse response)
        {
            return response != null
                && response.Code == 200
                && response.Result != null
                && response.Result.Error == null;
        }

        private static bool IsRetryableContentError(PipeResponse response)
        {
            int? errorCode = response?.Result?.Error?.Code;
            return errorCode == 3 || errorCode == -100099 || errorCode == -1000100;
        }

        private static bool IsConnectionFailure(Exception exception)
        {
            var connectionException = exception as BrowserConnectionException;
            if (connectionException == null)
            {
                return false;
            }

            return connectionException.Reason == ClientTransferCompletionReason.ConnectionClosed
                || connectionException.Reason == ClientTransferCompletionReason.SendFailed
                || connectionException.Reason == ClientTransferCompletionReason.ProtocolError
                || connectionException.Reason == ClientTransferCompletionReason.Stopped;
        }
    }
}
