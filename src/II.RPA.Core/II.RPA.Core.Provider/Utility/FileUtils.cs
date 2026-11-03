using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.Utility
{
    public class FileUtils
    {
        public static void CopyFolderCore(
            string sourceFolder,
            string targetFolder)
        {
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            var files = Directory.GetFiles(sourceFolder);

            foreach (var file in files)
            {
                var destFile = Path.Combine(targetFolder, Path.GetFileName(file));

                File.Copy(file, destFile, true);
            }

            var subDirs = Directory.GetDirectories(sourceFolder);

            foreach (var dir in subDirs)
            {
                var destDir = Path.Combine(targetFolder, new DirectoryInfo(dir).Name);
                CopyFolderCore(dir, destDir);
            }
        }

        public static IEnumerable<string> SearchFiles(
            string folder,
            Regex regex)
        {
            foreach (var fullPath in Directory.EnumerateFiles(folder))
            {
                var fileName = Path.GetFileName(fullPath);
                var match = regex.Match(fileName);
                if (match.Success)
                {
                    yield return fullPath;
                }
            }
        }
    }
}
