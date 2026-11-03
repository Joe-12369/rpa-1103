using II.RPA.MobileDeviceManager.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.MobileDeviceManager.Models
{
    public class CurrentDeviceGlobal : ReactiveObject<CurrentDeviceGlobal>
    {
        public CurrentDeviceGlobal()
        {
            MobileTool_Install_StatusObs = new Lazy<IObservable<E_MobileTool_Install_Status>>(() => Obs(x => MobileTool_Install_Status));
            DeviceInfosObs = new Lazy<IObservable<List<DeviceInfo>>>(() => Obs(x => DeviceInfos));
            CurrentDeviceIdObs = new Lazy<IObservable<string>>(() => Obs(x => CurrentDeviceId));
            IsInstallMobileToolObs = new Lazy<IObservable<bool>>(() => Obs(x => IsInstallMobileTool));
            IsCheckMobileingObs = new Lazy<IObservable<bool>>(() => Obs(x => IsCheckMobileing));
            IsWaitUsbEnableObs = new Lazy<IObservable<bool>>(() => Obs(x => IsWaitUsbEnable));
        }

        private E_MobileTool_Install_Status mobileTool_Install_Status;
        public E_MobileTool_Install_Status MobileTool_Install_Status
        {
            get { return mobileTool_Install_Status; }
            set { SetProperty(ref mobileTool_Install_Status, value); }
        }
        public Lazy<IObservable<E_MobileTool_Install_Status>> MobileTool_Install_StatusObs { get; }

        private List<DeviceInfo> deviceInfos;
        public List<DeviceInfo> DeviceInfos
        {
            get { return deviceInfos; }
            set { SetProperty(ref deviceInfos, value); }
        }
        public Lazy<IObservable<List<DeviceInfo>>> DeviceInfosObs { get; }

        private string currentDeviceId;
        public string CurrentDeviceId
        {
            get { return currentDeviceId; }
            set { SetProperty(ref currentDeviceId, value); }
        }
        public Lazy<IObservable<string>> CurrentDeviceIdObs { get; }

        private bool isInstallMobileTool;
        public bool IsInstallMobileTool
        {
            get { return isInstallMobileTool; }
            set { SetProperty(ref isInstallMobileTool, value); }
        }
        public Lazy<IObservable<bool>> IsInstallMobileToolObs { get; }

        private string currentDeviceName;
        public string CurrentDeviceName
        {
            get { return currentDeviceName; }
            set { SetProperty(ref currentDeviceName, value); }
        }

        private bool isCheckMobileing;
        public bool IsCheckMobileing
        {
            get { return isCheckMobileing; }
            set { SetProperty(ref isCheckMobileing, value); }
        }
        public Lazy<IObservable<bool>> IsCheckMobileingObs { get; }

        private bool isWaitUsbEnable;
        public bool IsWaitUsbEnable
        {
            get { return isWaitUsbEnable; }
            set { SetProperty(ref isWaitUsbEnable, value); }
        }
        public Lazy<IObservable<bool>> IsWaitUsbEnableObs { get; }
    }
}
