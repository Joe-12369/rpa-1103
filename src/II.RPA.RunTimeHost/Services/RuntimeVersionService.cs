using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace II.RPA.RuntimeHost.Services
{
    internal class RuntimeVersionService
    {
        private static readonly ILogger _logger = Logger.OfContext();
        private readonly string _runtimePath;
        private readonly string _baseVersion;
        private readonly string _extractedVersion;

        public RuntimeVersionService(string runtimePath, string baseVersion)
        {
            _runtimePath = runtimePath;
            _baseVersion = baseVersion;
            _extractedVersion = ExtractVersionFromPath(runtimePath);

            _logger.LogInformation($"RuntimeVersionService initialized - Path: {runtimePath}, BaseVersion: {baseVersion}, ExtractedVersion: {_extractedVersion ?? "null"}");
        }

        private static string ExtractVersionFromPath(string pluginPath)
        {
            if (string.IsNullOrEmpty(pluginPath))
            {
                _logger.LogWarning("Plugin path is null or empty");
                return null;
            }

            try
            {
                string directoryName = Path.GetFileName(pluginPath.TrimEnd(Path.DirectorySeparatorChar));

                if (string.IsNullOrEmpty(directoryName))
                {
                    _logger.LogWarning($"Cannot extract directory name from path: {pluginPath}");
                    return null;
                }

                _logger.LogDebug($"Extracting version from directory: {directoryName}");

                var versionMatch = Regex.Match(directoryName, @"(\d+\.\d+\.\d+\.\d+)");

                if (versionMatch.Success)
                {
                    string version = versionMatch.Groups[1].Value;
                    _logger.LogInformation($"Extracted version: {version} from path: {pluginPath}");
                    return version;
                }
                else
                {
                    _logger.LogWarning($"No version pattern found in directory: {directoryName}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"Error extracting version from path: {pluginPath}");
                return null;
            }
        }

        private static int CompareVersions(string version1, string version2)
        {
            if (string.IsNullOrEmpty(version1) && string.IsNullOrEmpty(version2))
                return 0;
            if (string.IsNullOrEmpty(version1))
                return -1;
            if (string.IsNullOrEmpty(version2))
                return 1;

            try
            {
                var v1 = new Version(version1);
                var v2 = new Version(version2);

                return v1.CompareTo(v2);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"Error comparing versions: '{version1}' vs '{version2}'");

                return string.Compare(version1, version2, StringComparison.Ordinal);
            }
        }

        public bool IsLegacyPackage()
        {
            if (string.IsNullOrEmpty(_extractedVersion))
            {
                _logger.LogWarning("Cannot determine if package is legacy because extracted version is null or empty");
                return true;
            }

            if (string.IsNullOrEmpty(_baseVersion))
            {
                _logger.LogWarning("Base version is null or empty, cannot compare versions");
                return false;
            }

            int comparisonResult = CompareVersions(_extractedVersion, _baseVersion);
            bool isLegacy = comparisonResult < 0;

            _logger.LogInformation($"Version comparison: {_extractedVersion} vs {_baseVersion} = {comparisonResult}, IsLegacy: {isLegacy}");

            return isLegacy;
        }

        public string GetExtractedVersion()
        {
            return _extractedVersion;
        }

        public string GetBaseVersion()
        {
            return _baseVersion;
        }

        public string GetRuntimePath()
        {
            return _runtimePath;
        }

        public bool IsVersionEqual()
        {
            if (string.IsNullOrEmpty(_extractedVersion) || string.IsNullOrEmpty(_baseVersion))
                return false;

            return CompareVersions(_extractedVersion, _baseVersion) == 0;
        }

        public bool IsVersionGreater()
        {
            if (string.IsNullOrEmpty(_extractedVersion) || string.IsNullOrEmpty(_baseVersion))
                return false;

            return CompareVersions(_extractedVersion, _baseVersion) > 0;
        }

        public bool IsVersionInRange(string minVersion, string maxVersion)
        {
            if (string.IsNullOrEmpty(_extractedVersion))
                return false;

            int minComparison = CompareVersions(_extractedVersion, minVersion);
            int maxComparison = CompareVersions(_extractedVersion, maxVersion);

            return minComparison >= 0 && maxComparison <= 0;
        }

        public string GetVersionStatus()
        {
            if (string.IsNullOrEmpty(_extractedVersion))
                return "Unknown (No version extracted)";

            if (string.IsNullOrEmpty(_baseVersion))
                return _extractedVersion + " (No base version for comparison)";

            int comparison = CompareVersions(_extractedVersion, _baseVersion);

            if (comparison < 0)
                return _extractedVersion + " (Legacy - below " + _baseVersion + ")";
            else if (comparison == 0)
                return _extractedVersion + " (Equal to base version)";
            else
                return _extractedVersion + " (Newer than base version " + _baseVersion + ")";
        }

        public static bool IsLegacy(string runtimePath)
        {
            var service = new RuntimeVersionService(runtimePath, "7.3.0.0");
            return service.IsLegacyPackage();
        }
    }
}
