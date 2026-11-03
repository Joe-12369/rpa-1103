using II.RPA.Logging;
using II.RPA.Plugin.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Windows.Forms;

namespace II.RPA.RuntimeHost
{
    /// <summary>
    /// RunTimeHostApplicationContext
    /// </summary>
    internal class RunTimeHostApplicationContext : ApplicationContext, IRpaApplication
    {
        private readonly static ILogger _logger = Logger.OfContext();
        private readonly IApplication _serverApplication;

        /// <summary>
        /// .ctor
        /// </summary>
        internal RunTimeHostApplicationContext()
        {
            _logger.LogInformation($"{nameof(RunTimeHostApplicationContext)}.ctor");
            var applicationProvider = new ApplicationProvider();
            MainForm = applicationProvider;
            _serverApplication = applicationProvider;
        }

        public IApplication CurrentApplication
        {
            get
            {
                return _serverApplication;
            }
        }

        public SynchronizationContext MainContext => _serverApplication.MainUISynchronizationContext;

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
