using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonServiceLocator;
using II.RPA.Core.Log;
using Microsoft.Owin;
using Org.BouncyCastle.Ocsp;

namespace II.RPA.Core.Provider.ZBrainProxyProvider
{
    public class LoggingMiddleware : OwinMiddleware
    {
        private ILogger Logger =>
            ServiceLocator.Current.GetInstance<ILogger>();

        public LoggingMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            var path = context.Request.Uri.AbsolutePath;
            var timer = new Stopwatch();
            timer.Start();
            Logger.LogInfo($"Accept {path}");
            await Next.Invoke(context);
            Logger.LogInfo($"Send {path}-{timer.ElapsedMilliseconds}ms");
        }

    }
}
