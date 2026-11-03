using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using System;

namespace II.RPA.WebBrowser.Common.Basic
{
    internal enum CrxScriptInitializationCompletion
    {
        Success,
        InitializationFailed,
        ConnectionFailed
    }

    internal enum CrxScriptInitializationStage
    {
        None,
        TestConnection,
        GetVersion,
        GetCodeId,
        Init,
        InitContentScript,
        Complete
    }

    internal enum CrxScriptInitializationPath
    {
        None,
        Mv3NoLegacyInjection,
        CodeIdMatched,
        Injected
    }

    internal sealed class CrxScriptInitializationResult
    {
        internal CrxScriptInitializationResult(
            CrxScriptInitializationCompletion completion,
            CrxScriptInitializationStage stage,
            CrxScriptInitializationPath path,
            IBrowserConnectionNew connection,
            PipeResponse response,
            Exception exception,
            int attempt,
            long elapsedMilliseconds)
        {
            Completion = completion;
            Stage = stage;
            Path = path;
            Connection = connection;
            Response = response;
            Exception = exception;
            Attempt = attempt;
            ElapsedMilliseconds = elapsedMilliseconds;
        }

        internal CrxScriptInitializationCompletion Completion { get; }
        internal CrxScriptInitializationStage Stage { get; }
        internal CrxScriptInitializationPath Path { get; }
        internal IBrowserConnectionNew Connection { get; }
        internal PipeResponse Response { get; }
        internal Exception Exception { get; }
        internal int Attempt { get; }
        internal long ElapsedMilliseconds { get; }
    }
}
