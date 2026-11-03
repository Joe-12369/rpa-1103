using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.II.RPA
{
    using System;
    using System.IO;

    public static class PathHelper
    {
        public static string GetPluginLoaderPath(string baseDirectory)
        {
            // baseDirectory: e.g. ...\src\Test.II.RPA\bin\Debug\net48
            var dir = new DirectoryInfo(baseDirectory);

            // 回到 Test.II.RPA
            dir = dir.Parent?.Parent?.Parent;

            if (dir == null)
                throw new DirectoryNotFoundException("无法定位到 Test.II.RPA 目录");

            // 回到 src
            dir = dir.Parent;
            if (dir == null)
                throw new DirectoryNotFoundException("无法定位到 src 目录");

            // 回到 ii.rpa 根目录
            dir = dir.Parent;
            if (dir == null)
                throw new DirectoryNotFoundException("无法定位到 ii.rpa 根目录");

            // 拼接 bin\II.RPA.PluginLoader
            var pluginLoaderPath = Path.Combine(dir.FullName, "bin", "II.RPA.PluginLoader");

            if (!Directory.Exists(pluginLoaderPath))
                throw new DirectoryNotFoundException($"插件目录不存在: {pluginLoaderPath}");

            return pluginLoaderPath;
        }
    }

}
