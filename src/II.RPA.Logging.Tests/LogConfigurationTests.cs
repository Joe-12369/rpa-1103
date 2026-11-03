using System;
using System.IO;
using System.Threading;
using II.RPA.Logging.Configurations;
using Microsoft.Extensions.Logging;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace II.RPA.Logging.Tests
{
    public class LogConfigurationTests : IDisposable
    {
        private readonly string original = File.Exists(LogSettingsHelper.LogConfigFilePath)
            ? File.ReadAllText(LogSettingsHelper.LogConfigFilePath) : null;
        private readonly string role = "LoggingTests-" + Guid.NewGuid().ToString("N");

        private void Configure(bool core)
        {
            LogSettingsHelper.CurrentSettings = new LogSettingModel { Level = LogLevel.Information };
            LogSettingsHelper.SaveSettings(LogSettingsHelper.CurrentSettings);
            if (core) Logger.ConfigCoreServerWithRole(role);
            else Logger.ConfigWithRole(role);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExplicitChangeAppliesImmediatelyToExistingLogger(bool core)
        {
            Configure(core);
            var logger = Logger.OfContext();
            Assert.False(logger.IsEnabled(LogLevel.Debug));
            Logger.ChangeMinLogLevel(LogLevel.Debug);
            Assert.True(logger.IsEnabled(LogLevel.Debug));
            Logger.ChangeMinLogLevel(LogLevel.Information);
            Assert.False(logger.IsEnabled(LogLevel.Debug));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TraceEnablesTraceAndDebug(bool core)
        {
            Configure(core);
            Logger.ChangeMinLogLevel(LogLevel.Trace);
            Assert.True(Logger.OfContext().IsEnabled(LogLevel.Trace));
            Assert.True(Logger.OfContext().IsEnabled(LogLevel.Debug));
            Assert.Equal(LogLevel.Trace, Logger.Level);
        }

        [Fact]
        public void ApplyFailureReturnsFalseAndPreservesEffectiveLevel()
        {
            Configure(true);
            var field = typeof(Logger).GetField("createConfiguredLogger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalFactory = field.GetValue(null);
            try
            {
                field.SetValue(null, new Func<Serilog.Core.Logger>(() =>
                    throw new InvalidOperationException("simulated construction failure")));
                Assert.False(Logger.TryChangeMinLogLevel(LogLevel.Debug));
                Assert.Equal(LogLevel.Information, Logger.Level);
                Assert.Equal(LogLevel.Information, LogSettingsHelper.CurrentSettings.Level);
                Assert.False(Logger.OfContext().IsEnabled(LogLevel.Debug));
            }
            finally
            {
                field.SetValue(null, originalFactory);
                Logger.ChangeMinLogLevel(LogLevel.Information);
            }
        }

        [Fact]
        public void ExternalChangeStillAppliesAfterGc()
        {
            Configure(true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var logger = Logger.OfContext();
            File.WriteAllText(LogSettingsHelper.LogConfigFilePath,
                "{\"serilog:minimum-level\":1}");
            Assert.True(SpinWait.SpinUntil(() => logger.IsEnabled(LogLevel.Debug), 5000));
        }

        [Fact]
        public void SaveFailureDoesNotClaimSuccess()
        {
            Configure(true);
            FileStream locked = null;
            Assert.True(SpinWait.SpinUntil(() =>
            {
                try { locked = File.Open(LogSettingsHelper.LogConfigFilePath,
                    FileMode.Open, FileAccess.ReadWrite, FileShare.None); return true; }
                catch (IOException) { return false; }
            }, 5000));
            using (locked)
            {
                Assert.False(Logger.TryChangeMinLogLevel(LogLevel.Debug));
                Assert.Equal(LogLevel.Information, LogSettingsHelper.CurrentSettings.Level);
                Assert.False(Logger.OfContext().IsEnabled(LogLevel.Debug));
            }
        }

        [Fact]
        public void AtomicReplacementIsObserved()
        {
            Configure(true);
            var replacement = LogSettingsHelper.LogConfigFilePath + ".tmp";
            File.WriteAllText(replacement, "{\"serilog:minimum-level\":1}");
            File.Move(replacement, LogSettingsHelper.LogConfigFilePath, true);
            Assert.True(SpinWait.SpinUntil(() => Logger.OfContext().IsEnabled(LogLevel.Debug), 5000));
        }

        [Fact]
        public void RapidChangesApplyTheLastValue()
        {
            Configure(true);
            Logger.ChangeMinLogLevel(LogLevel.Debug);
            Logger.ChangeMinLogLevel(LogLevel.Warning);
            Logger.ChangeMinLogLevel(LogLevel.Information);
            File.WriteAllText(LogSettingsHelper.LogConfigFilePath, "{\"serilog:minimum-level\":1}");
            Assert.True(SpinWait.SpinUntil(() => Logger.OfContext().IsEnabled(LogLevel.Debug), 5000));
        }

        [Fact]
        public void DebugMessageReachesTheLogFile()
        {
            Configure(true);
            Logger.ChangeMinLogLevel(LogLevel.Debug);
            var marker = "debug-proof-" + Guid.NewGuid().ToString("N");
            Microsoft.Extensions.Logging.LoggerExtensions.LogDebug(Logger.OfContext(), marker);
            // Reconfiguration closes and flushes the previous file sink.
            Logger.ChangeMinLogLevel(LogLevel.Information);
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                role, "logs", DateTime.Now.ToString("yyyy-MM-dd"), "dll_logs");
            var file = Directory.GetFiles(folder, "*.txt")[0];
            using (var stream = File.Open(file, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream))
            {
                var contents = reader.ReadToEnd();
                Assert.Contains("DBG", contents);
                Assert.Contains(marker, contents);
            }
        }

        [Fact]
        public void DisposedWatcherDoesNotReceiveChanges()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            File.WriteAllText(path, "{}");
            int calls = 0;
            var watcher = new AppConfigWatcher(path, _ => Interlocked.Increment(ref calls));
            watcher.Dispose();
            File.WriteAllText(path, "{\"changed\":true}");
            Assert.False(SpinWait.SpinUntil(() => Volatile.Read(ref calls) != 0, 300));
            File.Delete(path);
        }

        [Fact]
        public void InvalidExternalEditPreservesCurrentConfiguration()
        {
            Configure(true);
            Logger.ChangeMinLogLevel(LogLevel.Debug);
            File.WriteAllText(LogSettingsHelper.LogConfigFilePath, "{unfinished");
            Thread.Sleep(400);
            Assert.True(Logger.OfContext().IsEnabled(LogLevel.Debug));
            Assert.Equal("{unfinished", File.ReadAllText(LogSettingsHelper.LogConfigFilePath));
        }

        [Fact]
        public void IncompleteSharedWriteIsRetriedWithoutOverwritingTheFile()
        {
            Configure(true);
            using (var stream = File.Open(LogSettingsHelper.LogConfigFilePath, FileMode.Create,
                FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write("{\"serilog:minimum-level\":");
                writer.Flush();
                Thread.Sleep(350);
                Assert.False(Logger.OfContext().IsEnabled(LogLevel.Debug));
                writer.Write("1}");
                writer.Flush();
            }
            Assert.True(SpinWait.SpinUntil(() => Logger.OfContext().IsEnabled(LogLevel.Debug), 5000));
            Assert.Equal("{\"serilog:minimum-level\":1}", File.ReadAllText(LogSettingsHelper.LogConfigFilePath));
        }

        public void Dispose()
        {
            Assert.True(SpinWait.SpinUntil(() =>
            {
                try
                {
                    if (original == null) File.Delete(LogSettingsHelper.LogConfigFilePath);
                    else File.WriteAllText(LogSettingsHelper.LogConfigFilePath, original);
                    return true;
                }
                catch (IOException) { return false; }
            }, 5000));
        }
    }
}
