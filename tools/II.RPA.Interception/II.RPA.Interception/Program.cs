using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace II.RPA.Interception
{
    [Subcommand(typeof(Check), typeof(Install), typeof(UnInstall))]
    internal class Program
    {
        public static int Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);

        public static void WriteOutput(string filePath, int exitCode)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, "{" + $"\"status\":{exitCode},\"code\":{exitCode},\"message\":\"\",\"info\":\"\"" + "}");
            }
        }

        [Command("check", Description = "检查interceptor是否已安装", ExtendedHelpText = "退出码定义:\n[0]: 已安装\n[1]: 未安装\n[-1]: 未知异常")]
        private class Check
        {
            [Option(ShortName = "o", Description = "输出文件路径, 如 ./output.json")]
            public string Output { get; } = null;

            private int OnExecute(IConsole console)
            {
                int exitCode;
                try
                {
                    exitCode = InputInterceptor.CheckDriverInstalled() ? 0 : 1;
                }
                catch (Exception e)
                {
                    console.WriteLine(e);
                    exitCode = -1;
                }
                WriteOutput(Output, exitCode);
                return exitCode;
            }
        }

        [Command("install", Description = "安装interceptor插件", ExtendedHelpText = "退出码定义:\n[0]: 正常退出\n[-1]: 未知异常")]
        private class Install
        {
            [Option(ShortName = "o", Description = "输出文件路径, 如 ./output.json")]
            public string Output { get; } = null;

            private int OnExecute(IConsole console)
            {
                int exitCode;
                try
                {
                    exitCode = InputInterceptor.InstallDriver() ? 0 : 1;
                }
                catch (Exception e)
                {
                    console.WriteLine(e);
                    exitCode = -1;
                }
                WriteOutput(Output, exitCode);
                return exitCode;
            }
        }

        [Command("uninstall", Description = "卸载interceptor插件", ExtendedHelpText = "退出码定义:\n[0]: 正常退出\n[-1]: 未知异常")]
        private class UnInstall
        {
            [Option(ShortName = "o", Description = "输出文件路径, 如 ./output.json")]
            public string Output { get; } = null;

            private int OnExecute(IConsole console)
            {
                int exitCode;
                try
                {
                    exitCode = InputInterceptor.UninstallDriver() ? 0 : 1;
                }
                catch (Exception e)
                {
                    console.WriteLine(e);
                    exitCode = -1;
                }
                WriteOutput(Output, exitCode);
                return exitCode;
            }
        }

        private int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 0;
        }
    }
}
