using II.RPA.MobileDeviceManager.Models;
using IPA.Ubases.Scheduling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.MobileDeviceManager.Log;
using System.Reactive.Concurrency;

namespace II.RPA.MobileDeviceManager
{
    public class SyncMobilePageSourceService : CtsControllable, IDisposable
    {
        private CancellationTokenSource cheackCts;
        private ILogger _logger;
        public SyncMobilePageSourceService(ILogger logger)
        {
            _logger = logger;
            this.cheackCts = new CancellationTokenSource();
        }
        public void Dispose()
        {
            Stop();
            if (cheackCts != null)
            {
                cheackCts.Dispose();
            }
        }

        public override void Setup(CancellationTokenSource cts)
        {
            NewThreadScheduler.Default
               .Schedule(async () =>
               {
                   await Task.Delay(1000);
                   await SetupCore(cts.Token);
               });
        }
        private async Task SetupCore(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!MobileDeviceManager.Instance.isCvCapture
                    && MobileDeviceManager.Instance.IsConnected
                    && !MobileDeviceManager.Instance.IsConnecting)
                    {
                        MobileDeviceManager.Instance.ParseElement?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"同步手机树结构异常:{ex}");
                }
                await Task.Delay(1000, token);
            }

        }
        public override void Stop()
        {
            base.Stop();
            if (cheackCts != null && !cheackCts.IsCancellationRequested)
            {
                cheackCts.Cancel();
            }
        }
    }
}

