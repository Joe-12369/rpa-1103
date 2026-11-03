using System;

namespace II.RPA.PluginLoader
{
    public sealed class PluginPackageDescriptor
    {
        public PluginPackageDescriptor(string packageName, string packageDirectory, string assemblyFilePath)
        {
            PackageName = packageName ?? throw new ArgumentNullException(nameof(packageName));
            PackageDirectory = packageDirectory ?? throw new ArgumentNullException(nameof(packageDirectory));
            AssemblyFilePath = assemblyFilePath ?? throw new ArgumentNullException(nameof(assemblyFilePath));
        }

        public string PackageName { get; }
        public string PackageDirectory { get; }
        public string AssemblyFilePath { get; }
    }
}
