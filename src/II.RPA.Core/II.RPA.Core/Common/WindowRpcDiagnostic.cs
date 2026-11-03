using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace II.RPA.Core.Common
{
    /// <summary>Bounded, asynchronous diagnostics for the Window.WaitWindow return path.</summary>
    public static class WindowRpcDiagnostic
    {
        public const string Header = "x-window-rpc-diagnostic-id";
        private static readonly AsyncLocal<string> Current = new AsyncLocal<string>();
        private static readonly BlockingCollection<string> Events = new BlockingCollection<string>(2048);
        private static readonly int Pid = Process.GetCurrentProcess().Id;
        private static int started;
        private static int dropped;
        private static int poolProbePending;

        public static IDisposable Begin(string pluginName, string id = null)
        {
            if (pluginName != "Window.WaitWindow") return new Scope(false, null);
            var previous = Current.Value;
            Current.Value = id ?? Guid.NewGuid().ToString("N");
            if (Interlocked.CompareExchange(ref started, 1, 0) == 0)
            {
                new Thread(WriteLoop) { IsBackground = true, Name = "WindowRpcDiagnosticWriter" }.Start();
                new Thread(ProbeLoop) { IsBackground = true, Name = "WindowRpcDiagnosticProbe" }.Start();
            }
            return new Scope(true, previous);
        }

        public static string RequestId => Current.Value;

        public static void Mark(string stage, long start = 0, string detail = null)
        {
            var id = Current.Value;
            if (id == null) return;
            Record(id, stage, start, detail);
        }

        public static void TransportMark(string id, string stage) => Record(id, stage, 0, null);

        private static void Record(string id, string stage, long start, string detail)
        {
            // Never perform file IO on the RPC thread. Missing records are explicitly counted.
            try
            {
                long tick = Stopwatch.GetTimestamp();
                double elapsed = start == 0 ? 0 : (tick - start) * 1000.0 / Stopwatch.Frequency;
                ThreadPool.GetAvailableThreads(out int workers, out int io);
                string line = string.Format(CultureInfo.InvariantCulture,
                    "utc={0:O} tick={1} pid={2} tid={3} request={4} stage={5} elapsedMs={6:F3} workers={7} io={8} gc={9}/{10}/{11} detail={12}",
                    DateTime.UtcNow, tick, Pid, Thread.CurrentThread.ManagedThreadId, id, stage,
                    elapsed, workers, io, GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2), detail ?? "");
                if (!Events.TryAdd(line)) Interlocked.Increment(ref dropped);
            }
            catch { /* Diagnostics must not change RPC behavior. */ }
        }

        private static void ProbeLoop()
        {
            while (true)
            {
                long start = Stopwatch.GetTimestamp();
                Thread.Sleep(1000);
                double elapsed = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                if (elapsed > 1500) Record("process", "dedicated_thread_delay", start, "expectedMs=1000");
                if (Interlocked.CompareExchange(ref poolProbePending, 1, 0) == 0)
                {
                    long queued = Stopwatch.GetTimestamp();
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            if ((Stopwatch.GetTimestamp() - queued) * 1000.0 / Stopwatch.Frequency > 500)
                                Record("process", "threadpool_delay", queued, "thresholdMs=500");
                        }
                        finally { Interlocked.Exchange(ref poolProbePending, 0); }
                    });
                }
            }
        }

        private static void WriteLoop()
        {
            // Keep at most three 8 MiB files per process, independent of diagnostic log level.
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "II.RPA", "Diagnostics", "WindowRpc");
            string path = Path.Combine(directory, "window-rpc-" + Pid + ".log");
            while (true)
            {
                try
                {
                    long writeStart = Stopwatch.GetTimestamp();
                    Directory.CreateDirectory(directory);
                    if (File.Exists(path) && new FileInfo(path).Length >= 8 * 1024 * 1024)
                    {
                        if (File.Exists(path + ".2")) File.Delete(path + ".2");
                        if (File.Exists(path + ".1")) File.Move(path + ".1", path + ".2");
                        File.Move(path, path + ".1");
                    }
                    using (var writer = new StreamWriter(path, true))
                    {
                        string line;
                        if (Events.TryTake(out line, 1000))
                        {
                            writer.WriteLine(line);
                            for (int i = 0; i < 255 && Events.TryTake(out line); i++) writer.WriteLine(line);
                        }
                        int lost = Interlocked.Exchange(ref dropped, 0);
                        if (lost > 0) writer.WriteLine("utc=" + DateTime.UtcNow.ToString("O") + " stage=dropped count=" + lost);
                        writer.WriteLine("flushUtc=" + DateTime.UtcNow.ToString("O"));
                    }
                    if ((Stopwatch.GetTimestamp() - writeStart) * 1000.0 / Stopwatch.Frequency > 1500)
                        Record("process", "writer_batch_delay", writeStart, "includesQueueWaitMs<=1000");
                }
                catch { Thread.Sleep(1000); }
                Thread.Sleep(250);
            }
        }

        private sealed class Scope : IDisposable
        {
            private readonly bool active;
            private readonly string previous;
            public Scope(bool active, string previous) { this.active = active; this.previous = previous; }
            public void Dispose() { if (active) Current.Value = previous; }
        }
    }
}
