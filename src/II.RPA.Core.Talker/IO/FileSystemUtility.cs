using System.Text.RegularExpressions;

namespace II.RPA.Core.Talker.IO;

internal static class FileSystemUtility
{
    public static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory))
        {
            var targetFile = Path.Combine(targetDirectory, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, targetFile, overwrite: true);
        }

        foreach (var sourceSubdirectory in Directory.EnumerateDirectories(sourceDirectory))
        {
            var targetSubdirectory = Path.Combine(
                targetDirectory,
                Path.GetFileName(sourceSubdirectory));
            CopyDirectory(sourceSubdirectory, targetSubdirectory);
        }
    }

    public static IEnumerable<string> SearchFiles(string directory, Regex fileNamePattern)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (var fullPath in Directory.EnumerateFiles(directory))
        {
            if (fileNamePattern.IsMatch(Path.GetFileName(fullPath)))
            {
                yield return fullPath;
            }
        }
    }
}
