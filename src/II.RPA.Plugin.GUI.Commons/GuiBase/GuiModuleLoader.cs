using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace II.RPA.Plugin.GUI.Commons
{
    public sealed class GuiModuleLoader
    {
        public void ConfigureModules(string runtimeDirectory, IEnumerable<string> logicalModuleNames, GuiModuleBuilder builder)
        {
            if (string.IsNullOrWhiteSpace(runtimeDirectory)) throw new ArgumentException("Runtime directory is required.", nameof(runtimeDirectory));
            if (logicalModuleNames == null) throw new ArgumentNullException(nameof(logicalModuleNames));
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            foreach (var logicalName in logicalModuleNames)
            {
                if (string.IsNullOrWhiteSpace(logicalName) || logicalName != logicalName.Trim() || logicalName.Contains("."))
                    throw new InvalidDataException("The GUI module logical name is invalid.");

                var moduleDirectory = Path.Combine(runtimeDirectory, logicalName);
                var configPath = Path.Combine(moduleDirectory, "config.json");
                var config = JsonConvert.DeserializeObject<ModuleConfig>(File.ReadAllText(configPath));
                var fileName = config == null ? null : config.FileName;
                if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
                    throw new InvalidDataException("config.json.fileName must be a simple file name.");

                var assemblyPath = Path.Combine(moduleDirectory, fileName);
                var assembly = Assembly.LoadFrom(assemblyPath);
                var assemblyName = assembly.GetName().Name;
                if (!string.Equals(Path.GetFileNameWithoutExtension(fileName), assemblyName, StringComparison.Ordinal))
                    throw new InvalidDataException("GUI module DLL file name must match AssemblyName.");

                var entryType = assembly.GetType(assemblyName + ".GuiModule", false, false);
                if (entryType == null || !entryType.IsPublic || entryType.IsAbstract || !typeof(IGuiModule).IsAssignableFrom(entryType))
                    throw new InvalidDataException("The fixed GUI module entry is invalid.");

                ((IGuiModule)Activator.CreateInstance(entryType)).Configure(builder);
            }
        }

        private sealed class ModuleConfig
        {
            [JsonProperty("fileName")]
            public string FileName { get; set; }
        }
    }
}
