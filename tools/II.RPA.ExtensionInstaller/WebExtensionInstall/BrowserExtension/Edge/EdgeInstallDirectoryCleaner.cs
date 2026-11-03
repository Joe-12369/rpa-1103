using System.IO;

namespace WebExtensionInstall
{
    public static class EdgeInstallDirectoryCleaner
    {
        private static readonly string[] LegacyNativeHostFileNames =
        {
            "II.RPA.NativeMessagingHostEdge.exe",
            "II.RPA.NativeMessagingHostEdge.exe.config"
        };

        public static void RemoveLegacyNativeHostFiles(string installDirectory)
        {
            foreach (string fileName in LegacyNativeHostFileNames)
            {
                string path = Path.Combine(installDirectory, fileName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
