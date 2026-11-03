using Newtonsoft.Json.Linq;
using System;
using System.IO;
using WebExtensionInstall;
using Xunit;

namespace WebExtensionInstall.Tests
{
    public class ChromiumExtensionPreferencesTests
    {
        [Fact]
        public void TryCleanProfile_PreservesInstalledExtensionDirectory()
        {
            const string extensionId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
            string profilePath = Path.Combine(
                Path.GetTempPath(),
                "WebExtensionInstall.Tests",
                Guid.NewGuid().ToString("N"));
            string extensionPath = Path.Combine(profilePath, "Extensions", extensionId, "1.0.0_0");
            Directory.CreateDirectory(extensionPath);
            File.WriteAllText(Path.Combine(profilePath, "Preferences"), "{}");
            try
            {
                bool result = ChromiumExtensionPreferences.TryCleanProfile(
                    profilePath,
                    new[] { extensionId });

                Assert.True(result);
                Assert.True(Directory.Exists(extensionPath));
            }
            finally
            {
                Directory.Delete(profilePath, true);
            }
        }

        [Fact]
        public void RemoveExtensionData_UsesCallerSuppliedIds()
        {
            const string requestedId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
            const string otherId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            JObject preferences = JObject.Parse($@"{{
              'extensions': {{
                'external_uninstalls': ['{requestedId}', '{otherId}']
              }}
            }}");

            bool changed = ChromiumExtensionPreferences.RemoveExtensionData(
                preferences,
                new[] { requestedId });

            Assert.True(changed);
            Assert.Equal(
                new[] { otherId },
                preferences["extensions"]?["external_uninstalls"]?.Values<string>());
        }

        [Fact]
        public void RemoveKnownExtensionData_RemovesOnlyKnownIds()
        {
            const string otherId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            JObject preferences = JObject.Parse($@"{{
              'extensions': {{
                'install_signature': {{ 'signature': 'keep' }},
                'external_uninstalls': [
                  '{EdgeExtensionPolicy.V2ExtensionId}',
                  '{otherId}',
                  '{EdgeExtensionPolicy.V3ExtensionId}'
                ],
                'toolbar': [
                  '{EdgeExtensionPolicy.V2ExtensionId}',
                  '{otherId}',
                  '{EdgeExtensionPolicy.V3ExtensionId}'
                ]
              }},
              'updateclientdata': {{
                'apps': {{
                  '{EdgeExtensionPolicy.V2ExtensionId}': {{ 'state': 1 }},
                  '{otherId}': {{ 'state': 1 }},
                  '{EdgeExtensionPolicy.V3ExtensionId}': {{ 'state': 1 }}
                }}
              }}
            }}");

            bool changed = ChromiumExtensionPreferences.RemoveExtensionData(
                preferences,
                new[] { EdgeExtensionPolicy.V2ExtensionId, EdgeExtensionPolicy.V3ExtensionId });

            Assert.True(changed);
            Assert.NotNull(preferences["extensions"]?["install_signature"]);
            Assert.Equal(new[] { otherId }, preferences["extensions"]?["external_uninstalls"]?.Values<string>());
            Assert.Equal(new[] { otherId }, preferences["extensions"]?["toolbar"]?.Values<string>());
            Assert.NotNull(preferences["updateclientdata"]?["apps"]?[otherId]);
            Assert.Null(preferences["updateclientdata"]?["apps"]?[EdgeExtensionPolicy.V2ExtensionId]);
            Assert.Null(preferences["updateclientdata"]?["apps"]?[EdgeExtensionPolicy.V3ExtensionId]);
        }

        [Fact]
        public void RemoveKnownExtensionData_LeavesUnrelatedPreferencesUnchanged()
        {
            JObject preferences = JObject.Parse(@"{
              'extensions': {
                'install_signature': { 'signature': 'keep' },
                'external_uninstalls': ['aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa']
              }
            }");
            string original = preferences.ToString();

            bool changed = ChromiumExtensionPreferences.RemoveExtensionData(
                preferences,
                new[] { EdgeExtensionPolicy.V2ExtensionId, EdgeExtensionPolicy.V3ExtensionId });

            Assert.False(changed);
            Assert.Equal(original, preferences.ToString());
        }

        [Fact]
        public void TryCleanFile_UpdatesValidJsonWithoutTouchingOtherExtensions()
        {
            string path = CreateTempFile($@"{{
              'extensions': {{
                'external_uninstalls': [
                  '{EdgeExtensionPolicy.V2ExtensionId}',
                  'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
                ],
                'install_signature': {{ 'signature': 'keep' }}
              }}
            }}");
            try
            {
                bool result = ChromiumExtensionPreferences.TryCleanFile(
                    path,
                    new[] { EdgeExtensionPolicy.V2ExtensionId });
                JObject cleaned = JObject.Parse(File.ReadAllText(path));

                Assert.True(result);
                Assert.Equal(
                    new[] { "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
                    cleaned["extensions"]?["external_uninstalls"]?.Values<string>());
                Assert.NotNull(cleaned["extensions"]?["install_signature"]);
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(path), true);
            }
        }

        [Fact]
        public void TryCleanFile_LeavesInvalidJsonUnchanged()
        {
            const string invalidJson = "{ invalid";
            string path = CreateTempFile(invalidJson);
            try
            {
                bool result = ChromiumExtensionPreferences.TryCleanFile(
                    path,
                    new[] { EdgeExtensionPolicy.V2ExtensionId });

                Assert.False(result);
                Assert.Equal(invalidJson, File.ReadAllText(path));
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(path), true);
            }
        }

        private static string CreateTempFile(string content)
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "WebExtensionInstall.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "Preferences");
            File.WriteAllText(path, content);
            return path;
        }
    }
}
