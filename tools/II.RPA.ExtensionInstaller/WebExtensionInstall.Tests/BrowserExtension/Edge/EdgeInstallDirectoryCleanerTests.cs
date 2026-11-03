using System;
using System.IO;
using WebExtensionInstall;
using Xunit;

namespace WebExtensionInstall.Tests
{
    public class EdgeInstallDirectoryCleanerTests : IDisposable
    {
        private readonly string _directory = Path.Combine(
            Path.GetTempPath(),
            "WebExtensionInstall.Tests",
            Guid.NewGuid().ToString("N"));

        [Fact]
        public void RemoveLegacyNativeHostFiles_DeletesOnlyKnownLegacyFiles()
        {
            Directory.CreateDirectory(_directory);
            string legacyExe = Create("II.RPA.NativeMessagingHostEdge.exe");
            string legacyConfig = Create("II.RPA.NativeMessagingHostEdge.exe.config");
            string currentExe = Create("II.RPA.NativeMessagingHost.exe");
            string unrelated = Create("unrelated.dll");

            EdgeInstallDirectoryCleaner.RemoveLegacyNativeHostFiles(_directory);

            Assert.False(File.Exists(legacyExe));
            Assert.False(File.Exists(legacyConfig));
            Assert.True(File.Exists(currentExe));
            Assert.True(File.Exists(unrelated));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        private string Create(string name)
        {
            string path = Path.Combine(_directory, name);
            File.WriteAllText(path, name);
            return path;
        }
    }
}
