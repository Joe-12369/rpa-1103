using II.RPA.Logging.Formatting;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace II.RPA.Core.Common
{
    public static class TaskUtils
    {
        private static readonly ILogger logger = Logging.Logger.OfContext();
        public static void TraceThreadPool(string aTraceKey = null,
            [CallerMemberName] string aCallerMemberName = null,
            [CallerFilePath] string aCallerFilePath = null,
            [CallerLineNumber] int aCallerLineNumber = -1)
        {
            Task.Run(() => 
            {
                try
                {
                    ThreadPool.GetAvailableThreads(out int availableWorkerThreads,
                        out int availableCompletionPortThreads);
                    //ThreadPool.GetMaxThreads(out int maxWorkerThreads,
                    //    out int maxCompletionPortThreads);
                    //ThreadPool.GetMinThreads(out int minWorkerThreads,
                    //    out int minCompletionPortThreads);

                    logger.LogDebug(LogBlock.Rows(
                        LogBlock.DefaultValueFormatter,
                        "Start".Tag("####TraceThreadPool####"),
                        $"[{aTraceKey}]".Tag("TraceKey"),
                        //$"[{aCallerFilePath}]".Tag(nameof(aCallerFilePath)),
                        //$"[{aCallerMemberName}]".Tag(nameof(aCallerMemberName)),
                        //$"[{aCallerLineNumber}]".Tag(nameof(aCallerLineNumber)),
                        $"[{availableWorkerThreads}]".Tag(nameof(availableWorkerThreads)),
                        "Over".Tag("####TraceThreadPool####")
                        ));
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "TraceThreadPool Error");
                }
            }).ConfigureAwait(false);
        }
    }
}

