using System;
using System.IO;
using WebExtensionInstall;
using Xunit;

namespace WebExtensionInstall.Tests
{
    public class NativeHostManifestEditorTests : IDisposable
    {
        private readonly string _directory = Path.Combine(
            Path.GetTempPath(),
            "WebExtensionInstall.Tests",
            Guid.NewGuid().ToString("N"));

        [Fact]
        public void EnsureAllowedOrigins_PreservesExistingAndAddsRequestedIds()
        {
            Directory.CreateDirectory(_directory);
            string manifestPath = Path.Combine(_directory, "manifest.json");
            File.WriteAllText(manifestPath, @"{
              'name': 'ii.rpa.chromenativemsg',
              'allowed_origins': ['chrome-extension://aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/']
            }");

            bool result = NativeHostManifestEditor.EnsureAllowedOrigins(
                manifestPath,
                new[] { "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", "cccccccccccccccccccccccccccccccc" });

            Assert.True(result);
            string manifest = File.ReadAllText(manifestPath);
            Assert.Contains("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", manifest);
            Assert.Contains("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", manifest);
            Assert.Contains("cccccccccccccccccccccccccccccccc", manifest);
        }

        [Fact]
        public void EnsureAllowedOrigins_LeavesInvalidJsonUnchanged()
        {
            Directory.CreateDirectory(_directory);
            string manifestPath = Path.Combine(_directory, "manifest.json");
            const string invalidJson = "{ invalid";
            File.WriteAllText(manifestPath, invalidJson);

            bool result = NativeHostManifestEditor.EnsureAllowedOrigins(
                manifestPath,
                new[] { "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb" });

            Assert.False(result);
            Assert.Equal(invalidJson, File.ReadAllText(manifestPath));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }
    }
}
