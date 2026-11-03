using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace II.RPA.Plugin.Excel.Utils
{
    // Opt-in boundary sampling; never force GC or run a background sampler.
    internal sealed class ExcelTransferPerformance
    {
        private sealed class Stage
        {
            public long Ticks, Bytes, Cells;
            public int Count;
        }

        private sealed class Measurement : IDisposable
        {
            private ExcelTransferPerformance owner;
            private readonly string stage, shape;
            private readonly long started, cells;

            internal Measurement(ExcelTransferPerformance owner, string stage, long cells, string shape)
            {
                this.owner = owner;
                this.stage = stage;
                this.cells = cells;
                this.shape = shape;
                started = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                var performance = owner;
                if (performance == null) return;
                owner = null;
                performance.Record(stage, Stopwatch.GetTimestamp() - started, cells: cells, shape: shape);
            }
        }

        // Stable summary schema for all four optimization combinations. A zero count means
        // the operation was not executed (for example Import in the cell-by-cell baseline).
        private static readonly string[] StageNames =
        {
            "method", "afterFilters", "publishCleanup", "serialize", "deserialize",
            "apply", "cellWrite", "import", "importFallback", "beginUpdate", "endUpdate"
        };

        private readonly ILogger logger;
        private readonly bool detailed;
        private readonly string id = Guid.NewGuid().ToString("N");
        private readonly string action, mode;
        private readonly bool streamingEnabled = ExcelUtils.StreamingEnabled;
        private readonly bool devExpressBatchEnabled = ExcelUtils.DevExpressBatchEnabled;
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private readonly Dictionary<string, Stage> stages = new Dictionary<string, Stage>();
        private readonly long startManaged;
        private long managed, workingSet, privateBytes;
        private long maxManaged, maxWorkingSet, maxPrivateBytes;
        private readonly long startWorkingSet, startPrivateBytes;
        private readonly int[] collections = { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };

        internal static ExcelTransferPerformance Start(string action, string mode)
        {
            string level = Environment.GetEnvironmentVariable("II_RPA_EXCEL_PERF");
            if (level != "1" && level != "2") return null;
            try { return new ExcelTransferPerformance(Logger.OfContext(), action, mode, level == "2"); }
            catch { return null; } // Diagnostics must not change execution results.
        }

        internal ExcelTransferPerformance(ILogger logger, string action, string mode, bool detailed)
        {
            this.logger = logger;
            this.action = action;
            this.mode = mode;
            this.detailed = detailed;
            foreach (string stage in StageNames) stages.Add(stage, new Stage());
            Sample();
            startManaged = managed;
            startWorkingSet = workingSet;
            startPrivateBytes = privateBytes;
            Log("event=start " + Memory());
        }

        internal void Sample()
        {
            try
            {
                managed = GC.GetTotalMemory(false);
                using (var process = Process.GetCurrentProcess())
                {
                    workingSet = process.WorkingSet64;
                    privateBytes = process.PrivateMemorySize64;
                }
                maxManaged = Math.Max(maxManaged, managed);
                maxWorkingSet = Math.Max(maxWorkingSet, workingSet);
                maxPrivateBytes = Math.Max(maxPrivateBytes, privateBytes);
            }
            catch { }
        }

        internal IDisposable Measure(string stage, long cells = 0, string shape = null)
        {
            try
            {
                // Observe the decoded input and conversion buffers while they are still alive.
                // Sampling itself is outside the measured operation on both old and new paths.
                Sample();
                return new Measurement(this, stage, cells, shape);
            }
            catch { return null; }
        }

        internal void Record(string stage, long ticks, long bytes = 0, long cells = 0,
            string shape = null)
        {
            try
            {
                if (!stages.TryGetValue(stage, out var total)) stages[stage] = total = new Stage();
                total.Count++;
                total.Ticks += ticks;
                total.Bytes += bytes;
                total.Cells += cells;
                Sample();
                if (detailed)
                    Log($"event=stage stage={stage} index={total.Count} elapsedMs={Ms(ticks)} bytes={bytes} cells={cells} shape={shape ?? "-"} " + Memory());
            }
            catch { }
        }

        internal void Finish(bool success, int batchSize)
        {
            try
            {
                elapsed.Stop();
                Sample();
                string totals = string.Join(" ", stages.Select(pair =>
                    $"{pair.Key}Count={pair.Value.Count} {pair.Key}Ms={Ms(pair.Value.Ticks)} {pair.Key}Bytes={pair.Value.Bytes} {pair.Key}Cells={pair.Value.Cells}"));
                Log($"event=end success={success} batchSize={batchSize} elapsedMs={Ms(elapsed.ElapsedTicks)} {totals} " +
                    Memory() + $" managedDeltaBytes={managed - startManaged} workingSetDeltaBytes={workingSet - startWorkingSet} privateDeltaBytes={privateBytes - startPrivateBytes}" +
                    $" sampledMaxManagedBytes={maxManaged} sampledMaxWorkingSetBytes={maxWorkingSet} sampledMaxPrivateBytes={maxPrivateBytes}" +
                    $" gc0={GC.CollectionCount(0) - collections[0]} gc1={GC.CollectionCount(1) - collections[1]} gc2={GC.CollectionCount(2) - collections[2]}");
            }
            catch { }
        }

        private string Memory() => $"managedBytes={managed} workingSetBytes={workingSet} privateBytes={privateBytes}";
        private static string Ms(long ticks) => (ticks * 1000.0 / Stopwatch.Frequency).ToString("F3", CultureInfo.InvariantCulture);
        private void Log(string message)
        {
            try { logger.LogInformation($"[ExcelTransferPerf] id={id} action={action} mode={mode} streaming={streamingEnabled} devExpressBatch={devExpressBatchEnabled} {message}"); }
            catch { }
        }
    }
}
