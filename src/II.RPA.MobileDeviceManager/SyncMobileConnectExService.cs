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
using II.RPA.MobileDeviceManager.Common;
using Unity;
using System.Reactive.Subjects;

namespace II.RPA.MobileDeviceManager
{
    public class SyncMobileConnectExService : CtsControllable, IDisposable
    {
        private const int DisconnectConfirmThreshold = 3;

        private readonly CurrentDeviceGlobal currentDeviceGlobal;
        private readonly Subject<GlobalExceptionSubModel> _exceptionSubject;
        private CancellationTokenSource cheackCts;
        private ILogger _logger;
        private int _missCount;
        private bool _disconnectNotified;
        public SyncMobileConnectExService(ILogger logger, Subject<GlobalExceptionSubModel> exceptionSubject)
        {
            _logger = logger;
            _exceptionSubject = exceptionSubject;
            currentDeviceGlobal = App.Current.ServiceContainer.Resolve<ObjectRef<CurrentDeviceGlobal>>().Get(() => new CurrentDeviceGlobal());
            this.cheackCts = new CancellationTokenSource();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
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
                    if (MobileDeviceManager.Instance.IsConnected
                    && !MobileDeviceManager.Instance.IsConnecting
                    && !string.IsNullOrEmpty(currentDeviceGlobal.CurrentDeviceId))
                    {
                        bool isConnect = await Task.Run(() =>
                        {
                            return AdbHelper.IsExistDevice(currentDeviceGlobal.CurrentDeviceId);
                        });
                        if (!isConnect)
                        {
                            _missCount++;
                            if (_missCount >= DisconnectConfirmThreshold && !_disconnectNotified)
                            {
                                _disconnectNotified = true;
                                _exceptionSubject.OnNext(new GlobalExceptionSubModel(ExceptionType.AppiumEx, $"{currentDeviceGlobal.CurrentDeviceId}"));
                            }
                        }
                        else
                        {
                            _missCount = 0;
                            _disconnectNotified = false;
                        }
                    }
                    else
                    {
                        _missCount = 0;
                        _disconnectNotified = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"同步手机是否断开状态:{ex}");
                }
                await Task.Delay(200);
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

