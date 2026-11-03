using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.MobileDeviceManager.Common;
using II.RPA.MobileDeviceManager.Models;
using II.RPA.MobileDeviceManager.Views;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Android;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using Unity;
using II.RPA.MobileDeviceManager.Log;
using static System.Windows.Forms.LinkLabel;
using System.Diagnostics;

namespace II.RPA.MobileDeviceManager.ViewModels
{
    public class DeviceScreenViewModel : ModelBase
    {
        private readonly Subject<GlobalEventSubModel> _eventSubject;
        private SyncMobilePageSourceService _syncMobilePageSourceService;
        private SyncMobileConnectExService _syncMobileConnectExService;
        private ILogger _logger;
        private int _toolbarCommandRunning;
        public DeviceScreenViewModel(Subject<GlobalEventSubModel> eventSubject, SyncMobilePageSourceService syncMobilePageSourceService, SyncMobileConnectExService syncMobileConnectExService, ILogger logger)
        {
            _eventSubject = eventSubject;
            _syncMobilePageSourceService = syncMobilePageSourceService;
            _syncMobileConnectExService = syncMobileConnectExService;
            _logger = logger;
            BackCommand = new DelegateCommand(BackCommandExcute);
            HomeCommand = new DelegateCommand(HomeCommandExcute);
            SwitchCommand = new DelegateCommand(SwitchCommandExcute);
            RevolveScreenCommand = new DelegateCommand(RevolveScreenCommandExcute);
            if (MobileDeviceManager.Instance.DeviceInfos != null && MobileDeviceManager.Instance.DeviceInfos.Count > 0)
            {
                this.IsShowDisConnect = true;
                this.IsShowConnectFail = false;
                this.IsShowConnectSuccess = false;
                this.IsShowConnecting = false;
                this.IsShowAnimeTipsFirst = false;
                this.isShowAnimeTipsSecond = false;
            }
            //Observable.Interval(TimeSpan.FromSeconds(3)).Subscribe(async d =>
            //{
            //    await Task.Run(() =>
            //    {
            //        try
            //        {
            //            if (!MobileDeviceManager.Instance.isCvCapture
            //            && MobileDeviceManager.Instance.IsConnected
            //            && !MobileDeviceManager.Instance.IsConnecting)
            //            {
            //                MobileDeviceManager.Instance.ParseElement?.Invoke();
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            throw ex;
            //        }
            //    });
            //});
            _syncMobilePageSourceService.Start();
            _syncMobileConnectExService.Start();
            _eventSubject
                .ObserveOn(DefaultScheduler.Instance)
                .Subscribe(
                onNext: t =>
                {
                    if (t.EventType == EventType.CheckMobileing)
                    {
                        this.IsShowAnimeTipsFirst = true;
                        this.IsShowConnecting = false;
                        this.IsShowAnimeTipsSecond = false;
                        this.IsShowConnectFail = false;
                        this.IsShowConnectSuccess = false;
                        this.IsShowDisConnect = false;
                    }
                    if (t.EventType == EventType.WaitUsbAuthorise)
                    {
                        this.IsShowAnimeTipsSecond = true;
                        this.IsShowAnimeTipsFirst = false;
                        this.IsShowConnecting = false;
                        this.IsShowConnectFail = false;
                        this.IsShowConnectSuccess = false;
                        this.IsShowDisConnect = false;
                    }
                    if (t.EventType == EventType.MoblieConnecting)
                    {
                        this.IsShowConnecting = true;
                        this.IsShowAnimeTipsFirst = false;
                        this.IsShowAnimeTipsSecond = false;
                        this.IsShowConnectFail = false;
                        this.IsShowConnectSuccess = false;
                        this.IsShowDisConnect = false;
                    }
                    if (t.EventType == EventType.MoblieConnected)
                    {
                        this.IsShowConnectSuccess = true;
                        this.IsShowConnecting = true;
                        this.IsShowAnimeTipsFirst = false;
                        this.IsShowAnimeTipsSecond = false;
                        this.IsShowConnectFail = false;
                        this.IsShowDisConnect = false;
                    }
                    if (t.EventType == EventType.MoblieConnectFailed)
                    {
                        this.IsShowConnectFail = true;
                        this.IsShowConnectSuccess = false;
                        this.IsShowConnecting = false;
                        this.IsShowAnimeTipsFirst = false;
                        this.IsShowAnimeTipsSecond = false;
                        this.IsShowDisConnect = false;
                    }
                    if (t.EventType == EventType.MoblieDisconnected)
                    {
                        this.IsShowDisConnect = true;
                        this.IsShowConnectFail = false;
                        this.IsShowConnectSuccess = false;
                        this.IsShowConnecting = false;
                        this.IsShowAnimeTipsFirst = false;
                        this.IsShowAnimeTipsSecond = false;
                    }
                },
                onError: ex =>
                {

                },
                onCompleted: () =>
                {

                });
        }
        private string deviceName;
        public string DeviceName
        {
            get
            {
                return deviceName;
            }
            set
            {
                this.deviceName = value;
                this.RaisePropertyChanged("DeviceName");
            }
        }

        private bool isConnected;
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
            set
            {
                this.isConnected = value;
                this.RaisePropertyChanged("IsConnected");
            }
        }

        private int connectStatus;
        public int ConnectStatus
        {
            get
            {
                return connectStatus;
            }
            set
            {
                this.connectStatus = value;
                this.RaisePropertyChanged("ConnectStatus");
            }
        }

        #region Prop-不同的展示状态

        private bool isShowAnimeTipsFirst;
        public bool IsShowAnimeTipsFirst
        {
            get
            {
                return isShowAnimeTipsFirst;
            }
            set
            {
                this.isShowAnimeTipsFirst = value;
                this.RaisePropertyChanged("IsShowAnimeTipsFirst");
            }
        }
        private bool isShowAnimeTipsSecond;
        public bool IsShowAnimeTipsSecond
        {
            get
            {
                return isShowAnimeTipsSecond;
            }
            set
            {
                this.isShowAnimeTipsSecond = value;
                this.RaisePropertyChanged("IsShowAnimeTipsSecond");
            }
        }
        private bool isShowConnecting;
        public bool IsShowConnecting
        {
            get
            {
                return isShowConnecting;
            }
            set
            {
                this.isShowConnecting = value;
                this.RaisePropertyChanged("IsShowConnecting");
            }
        }

        private bool isShowConnectSuccess;
        public bool IsShowConnectSuccess
        {
            get
            {
                return isShowConnectSuccess;
            }
            set
            {
                this.isShowConnectSuccess = value;
                this.RaisePropertyChanged("IsShowConnectSuccess");
            }
        }

        private bool isShowConnectFail;
        public bool IsShowConnectFail
        {
            get
            {
                return isShowConnectFail;
            }
            set
            {
                this.isShowConnectFail = value;
                this.RaisePropertyChanged("IsShowConnectFail");
            }
        }
        private bool isShowDisConnect;
        public bool IsShowDisConnect
        {
            get
            {
                return isShowDisConnect;
            }
            set
            {
                this.isShowDisConnect = value;
                this.RaisePropertyChanged("IsShowDisConnect");
            }
        }

        #endregion

        public DelegateCommand BackCommand { get; set; }
        public async void BackCommandExcute(object obj)
        {
            await ExecuteToolbarCommandAsync("返回", "ToolbarBack",
                () => MobileDeviceManager.Instance.PressNavigationKeyAsync(MobileNavigationKey.Back));
        }

        public DelegateCommand HomeCommand { get; set; }
        public async void HomeCommandExcute(object obj)
        {
            await ExecuteToolbarCommandAsync("Home", "ToolbarHome",
                () => MobileDeviceManager.Instance.PressNavigationKeyAsync(MobileNavigationKey.Home));
        }

        public DelegateCommand SwitchCommand { get; set; }
        public async void SwitchCommandExcute(object obj)
        {
            await ExecuteToolbarCommandAsync("任务栏", "ToolbarSwitch",
                () => MobileDeviceManager.Instance.PressNavigationKeyAsync(MobileNavigationKey.RecentApps));
        }

        public DelegateCommand RevolveScreenCommand { get; set; }
        public async void RevolveScreenCommandExcute(object obj)
        {
            await ExecuteToolbarCommandAsync("旋转屏幕", "ToolbarRotate",
                () => MobileDeviceManager.Instance.PressKeyCode("4"));
        }

        private async Task ExecuteToolbarCommandAsync(string commandName, string dirtyReason, Func<Task> action)
        {
            // One toolbar action at a time, including rotation, so rapid clicks cannot reorder them.
            if (Interlocked.CompareExchange(ref _toolbarCommandRunning, 1, 0) != 0)
                return;

            bool attempted = false;
            try
            {
                var manager = MobileDeviceManager.Instance;
                if (manager.IsResetNewDriver || !manager.IsConnected || manager.IsConnecting)
                    return;

                attempted = true;
                await action();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{commandName}功能键异常:{ex}");
                try
                {
                    string title = commandName == "旋转屏幕"
                        ? "旋转失败！".I18n("DeviceScreenViewModel_41C64157")
                        : "按键异常".I18n("controlType_MobileDeviceManager_502D7D52");
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        MsgTipsHelper.ShowWarningPageInMainFrame($"{title}：{ex.Message}", Application.Current.MainWindow));
                }
                catch (Exception displayEx)
                {
                    _logger.LogError($"{commandName}错误提示显示失败:{displayEx}");
                }
            }
            finally
            {
                // An operation may have reached the device even when its response failed.
                try
                {
                    if (attempted)
                        MobileDeviceManager.Instance.MarkPageSourceDirty(dirtyReason);
                }
                finally
                {
                    Interlocked.Exchange(ref _toolbarCommandRunning, 0);
                }
            }
        }
    }
}
