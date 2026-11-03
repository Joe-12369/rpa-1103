using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;

namespace II.RPA.PluginLoader
{
    public sealed class PluginPackageDiscovery
    {
        public IReadOnlyList<IPluginPackage> LoadPackages(string runtimeDirectory)
        {
            var packages = new List<IPluginPackage>();
            foreach (var descriptor in Discover(runtimeDirectory))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(descriptor.AssemblyFilePath);
                    var assemblyName = assembly.GetName().Name;
                    if (!string.Equals(Path.GetFileNameWithoutExtension(descriptor.AssemblyFilePath), assemblyName, StringComparison.Ordinal))
                        throw new InvalidDataException("The plugin DLL file name must match its AssemblyName.");

                    var entryType = assembly.GetType(assemblyName + ".PluginPackage", false, false);
                    if (entryType == null || !entryType.IsPublic || entryType.IsAbstract || !typeof(IPluginPackage).IsAssignableFrom(entryType))
                        throw new InvalidDataException("The plugin package entry is invalid: " + assemblyName + ".PluginPackage");

                    var package = (IPluginPackage)Activator.CreateInstance(entryType);
                    if (!string.Equals(package.PackageName, descriptor.PackageName, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("PluginPackage.PackageName must match its logical package directory.");

                    packages.Add(package);
                }
                catch (RPAException) { throw; }
                catch (Exception exception) { throw ConfigurationError(exception); }
            }

            return packages;
        }

        public IReadOnlyList<PluginPackageDescriptor> Discover(string runtimeDirectory)
        {
            if (string.IsNullOrWhiteSpace(runtimeDirectory)) throw new ArgumentException("The runtime directory is required.", nameof(runtimeDirectory));
            if (!Directory.Exists(runtimeDirectory)) throw ConfigurationError(new DirectoryNotFoundException(runtimeDirectory));

            var packages = new List<PluginPackageDescriptor>();
            var packageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var packageDirectory in Directory.GetDirectories(runtimeDirectory))
            {
                var configPath = Path.Combine(packageDirectory, "config.json");
                if (!File.Exists(configPath)) continue;

                try
                {
                    var config = JsonConvert.DeserializeObject<PackageConfig>(File.ReadAllText(configPath));
                    var fileName = config == null ? null : config.FileName;
                    if (string.IsNullOrWhiteSpace(fileName) || !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
                        throw new InvalidDataException("config.json.fileName must be a simple file name.");

                    var packageName = Path.GetFileName(packageDirectory);
                    if (!packageNames.Add(packageName)) throw new InvalidDataException("Duplicate plugin package name: " + packageName);

                    var assemblyFilePath = Path.Combine(packageDirectory, fileName);
                    if (!File.Exists(assemblyFilePath)) throw new FileNotFoundException("The plugin package main assembly does not exist.", assemblyFilePath);

                    packages.Add(new PluginPackageDescriptor(packageName, packageDirectory, assemblyFilePath));
                }
                catch (RPAException) { throw; }
                catch (Exception exception) { throw ConfigurationError(exception); }
            }

            return packages;
        }

        private static RPAException ConfigurationError(Exception exception) =>
            new RPAException(eErrorMessageType.Error_PluginGetAbilityError, exception);

        private sealed class PackageConfig
        {
            [JsonProperty("fileName")]
            public string FileName { get; set; }
        }
    }
}
