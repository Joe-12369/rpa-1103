using II.RPA.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace II.RPA.MobileProvider
{
    internal class MobileHostApplicationContext : ApplicationContext
    {
        private readonly static ILogger _logger = Logger.OfContext();
        public static SynchronizationContext SynchronizationContext
        { get; set; }
        /// <summary>
        /// .ctor
        /// </summary>
        internal MobileHostApplicationContext()
        {
            //打开手机appium服务
            MobileServerManager mobileServerManager = new MobileServerManager();
            mobileServerManager.InitMobileServer();
            //SynchronizationContext = SynchronizationContext.Current;
        }

        protected override void ExitThreadCore()
        {
            _logger.LogInformation($"{nameof(ExitThreadCore)}");
            base.ExitThreadCore();
        }

        protected override void OnMainFormClosed(object sender, EventArgs e)
        {
            base.OnMainFormClosed(sender, e);
            _logger.LogInformation($"{nameof(OnMainFormClosed)}:uiThread is over!");
        }
    }
}


