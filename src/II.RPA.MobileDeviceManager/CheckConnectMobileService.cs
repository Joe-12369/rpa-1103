using DynamicData;
using II.RPA.MobileDeviceManager.Common;
using II.RPA.MobileDeviceManager.GrpcService.Clint;
using II.RPA.MobileDeviceManager.Log;
using II.RPA.MobileDeviceManager.Models;
using IPA.Ubases.Scheduling;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity;

namespace II.RPA.MobileDeviceManager
{
    public class CheckConnectMobileService : CtsControllable, IDisposable
    {

        private CancellationTokenSource cheackCts;
        private readonly CurrentDeviceGlobal currentDeviceGlobal;
        private ILogger _logger;
        public CheckConnectMobileService(ILogger logger)
        {
            _logger = logger;
            currentDeviceGlobal = App.Current.ServiceContainer.Resolve<ObjectRef<CurrentDeviceGlobal>>().Get(() => new CurrentDeviceGlobal());
            this.cheackCts = new CancellationTokenSource();
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
                var deviceList = currentDeviceGlobal.DeviceInfos;
                // if (deviceList == null || deviceList.Count < 1)
                if (deviceList == null)
                {
                    var dlist = AdbHelper.GetConnectedDevices().Where(a => a.IsReady).ToList();
                    if (dlist.Count > 0)
                    {
                        currentDeviceGlobal.DeviceInfos = dlist;
                    }
                }
                else
                {
                    var dlist = AdbHelper.GetConnectedDevices().ToList();
                    if (currentDeviceGlobal.IsCheckMobileing)
                    {
                        if (dlist.Count > 0)
                        {
                            currentDeviceGlobal.IsWaitUsbEnable = true;
                            currentDeviceGlobal.IsCheckMobileing = false;
                        }
                    }
                    else if (currentDeviceGlobal.IsWaitUsbEnable)
                    {
                        if (dlist.Any(a => a.IsReady))
                        {
                            currentDeviceGlobal.IsWaitUsbEnable = false;
                        }
                    }
                    else
                    {
                        foreach (var device in deviceList)
                        {
                            var onlineObj = dlist.FirstOrDefault(a => a.Udid == device.Udid);
                            if (onlineObj != null)
                            {
                                device.IsReady = onlineObj.IsReady;
                            }
                            else
                            {
                                device.IsReady = false;
                            }
                        }
                    }
                }
                await Task.Delay(100);
            }
        }
        public void Dispose()
        {
            throw new NotImplementedException();
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
