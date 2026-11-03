using II.RPA.Logging;
using II.RPA.Logging.Definitions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.Plugin.Test
{
    static class Program
    {
        private static ILogger logger = Logger.OfContext();

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Logger.ConfigWithRole(LoggingRoles.Default);
            logger.LogInformation("测试程序启动");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RPATest());
        }
    }
}
