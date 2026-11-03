using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using DynamicData;
using II.RPA.MobileDeviceManager.Common;
using II.RPA.MobileDeviceManager.Controls;
using II.RPA.MobileDeviceManager.CV;
using II.RPA.MobileDeviceManager.Extentions;
using II.RPA.MobileDeviceManager.GrpcService.Clint;
using II.RPA.MobileDeviceManager.GrpcService.Interface;
using II.RPA.MobileDeviceManager.GrpcService.Service;
using II.RPA.MobileDeviceManager.Log;
using II.RPA.MobileDeviceManager.MobileInstall;
using II.RPA.MobileDeviceManager.Models;
using II.RPA.MobileDeviceManager.ScreenShot;
using II.RPA.MobileDeviceManager.Views;
using II.RPA.WinAutomation.CommonUtility;
using IPA.Ubases.States;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rpa.Core.Mobile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Unity;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace II.RPA.MobileDeviceManager.ViewModels
{
    public class MainViewModel : ModelBase, IMobileService
    {
        private readonly CurrentDeviceGlobal currentDeviceGlobal;
        private MobileDeviceManagerGRPCClint coreCLint;
        public ReactiveValue<E_MobileTool_Install_Status> rxInstallStatus;
        private CheckConnectMobileService _checkConnectMobileService;
        private readonly Subject<GlobalEventSubModel> _eventSubject;
        private readonly Subject<ConnectEventSubModel> _connectSubJect;
        private ILogger _logger;
        MobileElement elementCache = null;
        string globalSelector = "";
        string filepath = "";
        public Action<bool, string, bool, string> CaptureElement;
        public Action CapturePoint;
        public Action CancelCapture;
        private ManualResetEvent mReset = new ManualResetEvent(false);
        private string retPoint;
        private string deviceFilePath;
        public MainViewModel(ObjectRef<CurrentDeviceGlobal> currentDeviceGlobalRef,
             CheckConnectMobileService checkConnectMobileService,
             Subject<GlobalEventSubModel> eventSubject,
             Subject<ConnectEventSubModel> connectSubJect,
             ILogger logger)
        {
            try
            {
                _logger = logger;
                IsCalledByBot = CommandParamsHelper.ClientType == ClientType.Bot ? true : false;
                currentDeviceGlobal = currentDeviceGlobalRef.Get();
                _eventSubject = eventSubject;
                _connectSubJect = connectSubJect;
                InstallMobileToolCommand = new DelegateCommand(InstallMoboleToolExcute);
                ConnectCommand = new DelegateCommand(ConnectMobileExcute);
                DisConnectCommand = new DelegateCommand(DisConnectMobileExcute);
                AddDeviceCommand = new DelegateCommand(AddDeviceExcute);
                App.Current.StartGrpcServer(this);
                coreCLint = new MobileDeviceManagerGRPCClint(MobileDeviceManager.Instance.ServerPort);
                this.rxInstallStatus = new ReactiveValue<E_MobileTool_Install_Status>();
                _checkConnectMobileService = checkConnectMobileService;
                Devices = new ObservableCollection<DeviceInfo>();
                deviceFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Factory/mobileDeviceList.json");
                //同步初始化的设备
                FirstSyncDevices();
                this.rxInstallStatus.ContentObs.Value
                .Subscribe(state =>
                {
                    if (state == E_MobileTool_Install_Status.Installing)
                    {
                        IsShowInstalling = true;
                        IsShowInstallFail = false;
                        IsShowTips = false;
                    }
                    if (state == E_MobileTool_Install_Status.FailInstall)
                    {
                        IsShowInstalling = false;
                        IsShowInstallFail = true;
                        IsShowTips = false;
                    }
                    if (state == E_MobileTool_Install_Status.SuccessInstall)
                    {
                        IsShowInstalling = false;
                        IsShowInstallFail = false;
                        IsShowTips = true;
                        if (currentDeviceGlobal.DeviceInfos != null && currentDeviceGlobal.DeviceInfos.Count > 0)
                        {
                            IsInstallMobileTool = true;
                        }
                        else
                        {
                            IsInstallMobileTool = false;
                        }
                    }
                    else
                    {
                        //未安装
                    }
                });
                currentDeviceGlobal.DeviceInfosObs.Value.FirstOrDefaultAsync().Subscribe(d =>
                {
                    if (d != null && d.Count > 0)
                    {
                        if (AdbHelper.IsInstallMobileTools())
                        {
                            IsInstallMobileTool = true;
                        }
                        d.ForEach((item) =>
                        {
                            if (!Devices.Any(a => a.Udid == item.Udid))
                            {
                                Application.Current.Dispatcher.Invoke(() => { Devices.Insert(0, item); });
                            }
                        });

                        DeviceInfo deviceInfo = Devices.FirstOrDefault(a => a.IsReady);
                        if (deviceInfo != null)
                        {
                            currentDeviceGlobal.CurrentDeviceName = deviceInfo.Model;
                            deviceInfo.IsConnected = true;
                            //currentDeviceGlobal.CurrentDeviceId = deviceInfo.Udid;
                            _connectSubJect.OnNext(new ConnectEventSubModel(ConnectType.Connect, deviceInfo.Udid));
                        }

                    }
                });
                Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(async d =>
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            if (currentDeviceGlobal.DeviceInfos != null)
                            {
                                foreach (var item in currentDeviceGlobal.DeviceInfos)
                                {
                                    var citem = Devices.FirstOrDefault(a => a.Udid == item.Udid);
                                    if (citem != null)
                                    {
                                        citem.IsReady = item.IsReady;
                                        citem.IsConnected = item.IsConnected;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"同步列表连接状态异常：{ex}");
                        }
                    });
                });
                currentDeviceGlobal.IsCheckMobileingObs.Value.Subscribe(result =>
                {
                    if (result)
                    {
                        _eventSubject.OnNext(new GlobalEventSubModel(EventType.CheckMobileing, ""));
                    }
                    else
                    {
                    }
                });
                currentDeviceGlobal.IsWaitUsbEnableObs.Value.Subscribe(result =>
                {
                    if (result)
                    {
                        _eventSubject.OnNext(new GlobalEventSubModel(EventType.WaitUsbAuthorise, ""));
                    }
                    else
                    {
                        _eventSubject.OnNext(new GlobalEventSubModel(EventType.MoblieConnecting, ""));

                        var dlist = AdbHelper.GetConnectedDevices().Where(a => a.IsReady).ToList();
                        var currentIdList = currentDeviceGlobal.DeviceInfos.Select(b => b.Udid).ToList();
                        var newList = dlist.Where(a => !currentIdList.Contains(a.Udid));
                        newList.ToList().ForEach(newDevive =>
                        {
                            currentDeviceGlobal.DeviceInfos.Insert(0, newDevive);
                            Devices = Devices ?? new ObservableCollection<DeviceInfo>();
                            Application.Current.Dispatcher.Invoke(() => { Devices.Insert(0, newDevive); });
                        });
                        currentDeviceGlobal.CurrentDeviceName = currentDeviceGlobal.DeviceInfos.First()?.Name;
                        //currentDeviceGlobal.CurrentDeviceId = currentDeviceGlobal.DeviceInfos.First()?.Udid;
                        _connectSubJect.OnNext(new ConnectEventSubModel(ConnectType.Connect, currentDeviceGlobal.DeviceInfos.First()?.Udid));
                        currentDeviceGlobal.DeviceInfos.First().IsConnected = true;
                    }
                });


                Devices.CollectionChanged += Devices_CollectionChanged;
                if (MobileDeviceManager.Instance.IsInstallPlugin)
                {
                    rxInstallStatus.Content = E_MobileTool_Install_Status.SuccessInstall;
                    if (currentDeviceGlobal.DeviceInfos != null && currentDeviceGlobal.DeviceInfos.Count > 0)
                    {
                        IsInstallMobileTool = true;
                    }
                    else
                    {
                        IsInstallMobileTool = false;
                    }
                    _checkConnectMobileService.Start();
                }
                else
                {
                    rxInstallStatus.Content = E_MobileTool_Install_Status.NotInstall;
                    InstallMobileToolCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }

            OpenModifyDialogCommand = new DelegateCommand(o =>
            {
                if (o is DeviceInfo deviceInfo)
                {
                    var win = new ModifyWindow(deviceInfo.Name, deviceInfo.Udid)
                    {
                        Owner = Application.Current.MainWindow,
                        ShowActivated = true
                    };


                    if (win.ShowDialog() == true)
                    {
                        if (deviceInfo.IsConnected)
                        {
                            MsgTipsHelper.ShowWarningPageInMainFrame("当前设备连接中，请先断开连接！".I18n("MainViewModel_B4C55852"), Application.Current.MainWindow);
                            return;
                        }
                        if (string.IsNullOrEmpty(win.DeviceName))
                        {
                            MsgTipsHelper.ShowWarningPageInMainFrame("请输入手机名！".I18n("MainViewModel_DF6CFE55"), Application.Current.MainWindow);
                            return;
                        }
                        if (win.DeviceName.Length > 20)
                        {
                            MsgTipsHelper.ShowWarningPageInMainFrame("手机名长度不可超过20！".I18n("MainViewModel_EB86A133"), Application.Current.MainWindow);
                            return;
                        }
                        //deviceInfo.Name = win.DeviceName;

                        Devices.Replace(deviceInfo, new DeviceInfo
                        {
                            Udid = deviceInfo.Udid,
                            Name = win.DeviceName,
                            IsReady = deviceInfo.IsReady,
                            IsConnected = deviceInfo.IsConnected,
                            Brand = deviceInfo.Brand,
                            Model = deviceInfo.Model,
                            Version = deviceInfo.Version
                        });
                        var sDevice = currentDeviceGlobal.DeviceInfos.FirstOrDefault(a => a.Udid == deviceInfo.Udid);
                        if (sDevice != null)
                        {
                            sDevice.Name = win.DeviceName;
                        }
                        //Devices.First(a => a.Name == deviceInfo.Name).Name = win.DeviceName;
                    }
                }
            });

            DeleteCommand = new DelegateCommand(o =>
            {
                if (o is DeviceInfo deviceInfo)
                {
                    if (deviceInfo.IsConnected)
                    {
                        MsgTipsHelper.ShowWarningPageInMainFrame("当前设备连接中，请先断开连接！".I18n("MainViewModel_B4C55852"), Application.Current.MainWindow);
                        return;
                    }
                    var ditem = currentDeviceGlobal.DeviceInfos.FirstOrDefault(a => a.Udid == deviceInfo.Udid);
                    currentDeviceGlobal.DeviceInfos.Remove(ditem);
                    Devices.Remove(deviceInfo);
                }
            });
        }
        /// <summary>
        /// 首次同步历史设备信息
        /// </summary>
        private void FirstSyncDevices()
        {
            try
            {
                if (MobileDeviceManager.Instance.DeviceInfos != null && MobileDeviceManager.Instance.DeviceInfos.Count > 0)
                {
                    currentDeviceGlobal.DeviceInfos = MobileDeviceManager.Instance.DeviceInfos;
                    Devices = new ObservableCollection<DeviceInfo>(MobileDeviceManager.Instance.DeviceInfos);
                }

            }
            catch { }
        }
        /// <summary>
        /// 设备列表变动触发事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Devices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //目前几个类型处理逻辑完全一致，先区分一方后续差异化处理
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                //新增
                //消息推送前端
                try
                {
                    var lastList = sender as ObservableCollection<DeviceInfo>;
                    await SendMessageToClint(action: "2", deviceInfos: lastList);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"同步最新的设备信息至客户端异常:{ex.ToString()}");
                }
            }
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                //删除
                try
                {
                    var lastList = sender as ObservableCollection<DeviceInfo>;
                    await SendMessageToClint(action: "2", deviceInfos: lastList);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"同步最新的设备信息至客户端异常:{ex.ToString()}");
                }
            }
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move)
            {
            }
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
            {
                //修改
                try
                {
                    var lastList = sender as ObservableCollection<DeviceInfo>;
                    await SendMessageToClint(action: "2", deviceInfos: lastList);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"同步最新的设备信息至客户端异常:{ex.ToString()}");
                }

            }
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
            }
        }

        private MobileToolInstaller _installer;

        public DelegateCommand OpenModifyDialogCommand
        {
            get;
            set;
        }
        public DelegateCommand DeleteCommand
        {
            get;
            set;
        }

        /// <summary>
        /// 是否是bot调用
        /// </summary>
        private bool isCalledByBot;
        public bool IsCalledByBot
        {
            get
            {
                return isCalledByBot;
            }
            set
            {
                this.isCalledByBot = value;
                this.RaisePropertyChanged("IsCalledByBot");
            }
        }

        /// <summary>
        /// 是否安装了手机插件
        /// </summary>
        private bool isInstallMobileTool;
        public bool IsInstallMobileTool
        {
            get
            {
                return isInstallMobileTool;
            }
            set
            {
                this.isInstallMobileTool = value;
                this.RaisePropertyChanged("IsInstallMobileTool");
            }
        }

        /// <summary>
        /// 最大化窗口图标Icon
        /// </summary>
        private string maxMinIcon = "\xe707";
        public string MaxMinIcon
        {
            get
            {
                return maxMinIcon;
            }
            set
            {
                this.maxMinIcon = value;
                this.RaisePropertyChanged("MaxMinIcon");
            }
        }
        /// <summary>
        /// 钉住窗口图标Icon
        /// </summary>
        private string pinWinIcon = "\xe6dd";
        public string PinWinIcon
        {
            get
            {
                return pinWinIcon;
            }
            set
            {
                this.pinWinIcon = value;
                this.RaisePropertyChanged("PinWinIcon");
            }
        }
        /// <summary>
        /// 是否显示蒙层
        /// </summary>
        private bool isLoadingMask = false;
        public bool IsLoadingMask
        {
            get
            {
                return isLoadingMask;
            }
            set
            {
                this.isLoadingMask = value;
                this.RaisePropertyChanged("IsLoadingMask");
            }
        }

        #region 安装区域显隐控制 安装中-安装失败-安装成功

        private bool isShowTips;
        public bool IsShowTips
        {
            get
            {
                return isShowTips;
            }
            set
            {
                this.isShowTips = value;
                this.RaisePropertyChanged("IsShowTips");
            }
        }
        private bool isShowInstalling;
        public bool IsShowInstalling
        {
            get
            {
                return isShowInstalling;
            }
            set
            {
                this.isShowInstalling = value;
                this.RaisePropertyChanged("IsShowInstalling");
            }
        }
        private bool isShowInstallFail;
        public bool IsShowInstallFail
        {
            get
            {
                return isShowInstallFail;
            }
            set
            {
                this.isShowInstallFail = value;
                this.RaisePropertyChanged("IsShowInstallFail");
            }
        }

        #endregion

        #region 插件安装

        private E_MobileTool_Install_Status mobileInstallStatus = E_MobileTool_Install_Status.NotInstall;
        public E_MobileTool_Install_Status MobileInstallStatus
        {
            get
            {
                return mobileInstallStatus;
            }
            set
            {
                this.mobileInstallStatus = value;
                this.RaisePropertyChanged("MobileInstallStatus");
            }
        }
        private double downloadProgressBar = 0;
        public double DownloadProgressBar
        {
            get
            {
                return downloadProgressBar;
            }
            set
            {
                this.downloadProgressBar = value;
                this.RaisePropertyChanged("DownloadProgressBar");
            }
        }
        private int downloadProgressNum;
        public int DownloadProgressNum
        {
            get
            {
                return downloadProgressNum;
            }
            set
            {
                this.downloadProgressNum = value;
                this.RaisePropertyChanged("DownloadProgressNum");
            }
        }
        private ObservableCollection<DeviceInfo> devices;
        public ObservableCollection<DeviceInfo> Devices
        {
            get
            {
                return devices;
            }
            set
            {
                this.devices = value;
                this.RaisePropertyChanged("Devices");
            }
        }
        public async void InstallMoboleToolExcute(object obj)
        {
            try
            {
                MobileInstallStatus = E_MobileTool_Install_Status.Installing;
                rxInstallStatus.Content = E_MobileTool_Install_Status.Installing;
                await InstallMoboleTool();
            }
            catch (Exception e)
            {
                //插件安装失败
                try
                {
                    _logger.LogError($"插件安装失败：{e}");
                    DownloadProgressBar = 0.0;
                    DownloadProgressNum = 0;
                    rxInstallStatus.Content = E_MobileTool_Install_Status.FailInstall;
                    //通知前端插件安装成功结果
                    await SendMessageToClint(action: "1", resultType: 1, toClintMsg: e.Message);
                    //删除已下载的残留文件
                    await DeleteFileOfInstallFail();
                }
                catch (Exception ex)
                {
                    rxInstallStatus.Content = E_MobileTool_Install_Status.FailInstall;
                    _logger.LogError(ex);
                }
            }
        }
        public async Task InstallMoboleTool()
        {
            _installer = new MobileToolInstaller();
            _installer.DownloadProgressChanged -= Installer_ProgressChanged;
            _installer.UnpackProgressChanged -= Installer_UnpackProgressChanged;
            _installer.MobileToolInstallEnded -= Installer_Ended;
            _installer.DownloadProgressChanged += Installer_ProgressChanged;
            _installer.UnpackProgressChanged += Installer_UnpackProgressChanged;
            _installer.MobileToolInstallEnded += Installer_Ended;
            await _installer.Install();
        }
        private void Installer_UnpackProgressChanged(object sender, double e)
        {
            DownloadProgressBar = Math.Round(e * 0.2 + 80.0, 2) / 100;
            DownloadProgressNum = (int)Math.Ceiling(e * 0.2 + 80.0) / 100;
        }

        private void Installer_ProgressChanged(object sender, double e)
        {
            DownloadProgressBar = Math.Round(e * 0.8, 2) / 100;
            DownloadProgressNum = (int)Math.Ceiling(e * 0.8) / 100;
        }

        private async void Installer_Ended(object sender, object e)
        {
            DownloadProgressBar = 0.0;
            DownloadProgressNum = 0;
            MobileInstallStatus = E_MobileTool_Install_Status.SuccessInstall;
            IsInstallMobileTool = true;
            //插件安装成功
            try
            {
                rxInstallStatus.Content = E_MobileTool_Install_Status.SuccessInstall;
                //安装完成后如果有历史列表设备，则显示未连接区域界面
                if (currentDeviceGlobal.DeviceInfos != null)
                {
                    _eventSubject.OnNext(new GlobalEventSubModel(EventType.MoblieDisconnected, ""));
                }
                await MobileDeviceManager.Instance.StartAppiumServer();
                _checkConnectMobileService.Start();
                //通知前端插件安装成功结果
                await SendMessageToClint(action: "1", resultType: 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }
        public DelegateCommand InstallMobileToolCommand { get; set; }


        /// <summary>
        ///  发送插件安装结果&设备变得信息给客户端
        /// </summary>
        /// <param name="action">"1"-插件安装 "2"-历史变动信息</param>
        /// <param name="resultType"> 0-成功1-失败2-取消</param>
        /// <param name="deviceInfos"></param>
        /// <returns></returns>
        public async Task SendMessageToClint(string action = "1", int resultType = 0, ObservableCollection<DeviceInfo> deviceInfos = null, string toClintMsg = "")
        {
            try
            {
                _logger.LogInfo($"开始发送信息至客户端");
                Message message = new Message();
                message.MessageId = 1;
                message.Action = action;
                if (action == "1")
                {
                    message.PayLoad = JsonConvert.SerializeObject(new { status = resultType, msg = toClintMsg });
                }
                else
                {
                    message.PayLoad = JsonConvert.SerializeObject(deviceInfos);
                }
                await coreCLint.MobileDeviceManagerServiceClient.PluginInstallResultToClintUnaryAsync(message);
                _logger.LogInfo($"发送信息至客户端成功：{JsonConvert.SerializeObject(message)}");
                if (action == "1" && IsCalledByBot)
                {
                    //自动关闭程序
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"发送信息至客户端失败：{ex}");
                throw;
            }
        }

        public async Task DeleteFileOfInstallFail()
        {
            await Task.Run(() =>
              {
                  try
                  {
                      string mobileToolsTempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
                      string mobileToolsTargetPath = Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "II.RPA", "mobile");
                      if (Directory.Exists(mobileToolsTempPath))
                      {
                          new DirectoryInfo(mobileToolsTempPath).Delete(true);
                      }
                      if (Directory.Exists(mobileToolsTargetPath))
                      {
                          new DirectoryInfo(mobileToolsTargetPath).Delete(true);
                      }
                  }
                  catch (Exception ex)
                  {
                      _logger.LogError($"安装插件失败，删除残留文件异常:{ex}");
                  }
              });

        }

        #endregion 

        /// <summary>
        /// 连接手机
        /// </summary>
        public DelegateCommand ConnectCommand { get; set; }
        public void ConnectMobileExcute(object obj)
        {
            var device = currentDeviceGlobal.DeviceInfos.FirstOrDefault(a => a.Udid == obj.ToString());
            if (device != null && !device.IsConnected && device.IsReady)
            {
                // 断开与新连接由 DeviceScreenView 顺序执行，不能用固定延迟猜测旧投屏是否已退出。
                if (!string.IsNullOrEmpty(currentDeviceGlobal.CurrentDeviceId))
                {
                    DisConnectMobileExcute(currentDeviceGlobal.CurrentDeviceId);
                }

                device.IsConnected = true;
                currentDeviceGlobal.CurrentDeviceName = device.Name;
                _connectSubJect.OnNext(new ConnectEventSubModel(ConnectType.Connect, obj.ToString()));
            }
        }
        /// <summary>
        /// 断开连接
        /// </summary>
        public DelegateCommand DisConnectCommand { get; set; }
        public void DisConnectMobileExcute(object obj)
        {
            var device = currentDeviceGlobal.DeviceInfos.FirstOrDefault(a => a.Udid == obj.ToString());
            if (device != null)
            {
                device.IsConnected = false;
            }
            //currentDeviceGlobal.CurrentDeviceId = "";

            _connectSubJect.OnNext(new ConnectEventSubModel(ConnectType.DisConnect, obj.ToString()));


            _eventSubject.OnNext(new GlobalEventSubModel(EventType.MoblieDisconnected, ""));
        }
        /// <summary>
        /// 添加手机
        /// </summary>
        public DelegateCommand AddDeviceCommand { get; set; }
        public async void AddDeviceExcute(object obj)
        {
            #region 自测调用拾取部分，上线删除
            //await Task.Factory.StartNew(async () =>
            //{
            //    await CaptureMobile(new MobileRequest
            //    {
            //        IsCvmodel = "1",
            //        CvUrl = "http://cv-element-service.ext.ii-ai.tech/cv_element_service/",
            //        IsSssimilar = "0",
            //        FilePath = "C:\\ProgramData\\ImageTemp\\1.png"
            //    });
            //});
            //await Task.Factory.StartNew(async () =>
            //{
            //    await GetPoint(new MobileRequest
            //    {

            //    });
            //});
            //return;
            #endregion

            //先断开现有连接
            await Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(currentDeviceGlobal.CurrentDeviceId))
                {
                    DisConnectMobileExcute(currentDeviceGlobal.CurrentDeviceId);
                }
                var dlist = AdbHelper.GetConnectedDevices().ToList();
                var curListId = currentDeviceGlobal.DeviceInfos.Select(b => b.Udid).ToList();
                var anyNewList = dlist.Any(a => !curListId.Contains(a.Udid));
                if (anyNewList)
                {
                    if (dlist.Count > 0)
                    {
                        currentDeviceGlobal.IsWaitUsbEnable = true;
                    }
                    else
                    {
                        currentDeviceGlobal.IsCheckMobileing = true;
                    }
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MsgTipsHelper.ShowWarningPageInMainFrame("未检测到新设备，添加失败!".I18n("MainViewModel_6F46553B"), Application.Current.MainWindow);
                    });
                    return;
                }
            });

        }

        #region GRPC服务接口
        #region 拾取-普通&CV&相似元素
        public async Task<MobileResponse> CaptureMobile(MobileRequest request, System.Threading.CancellationToken cancellationToken = default)
        {
            var device = MobileDeviceManager.Instance;
            try
            {
                if (!device.IsConnected)
                {
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "1",
                        Message = "不存在连接的设备".I18n("MainViewModel_B35FC426"),
                        Info = "{}"
                    });
                }
                await device.ResumeDesignTimeRefreshAsync("CaptureBegin", cancellationToken);
                device.CurrentCaptureElementShot = null;
                filepath = request.FilePath;

                mReset = new ManualResetEvent(false);
                globalSelector = "";

                Application.Current.Dispatcher.Invoke(() =>
                {
                    //双重激活、置顶窗口
                    bool originTopStatus = Application.Current.MainWindow.Topmost;
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                    Application.Current.MainWindow.Topmost = true;
                    Application.Current.MainWindow.Activate();
                    Application.Current.MainWindow.Topmost = originTopStatus;
                    if (request.IsCvmodel == "1")
                    {
                        device.CvUrl = request.CvUrl;
                        device.isCvCapture = true;
                        device.ParseElement -= device.ParseXml;
                        device.ClearElementCache();
                        device.ParseElement += device.ParseCV;
                        device.ParseElement.Invoke();//立刻刷新一次
                        MobileElement.OnSelected = UIElement_OnSelected_CV;
                    }
                    else
                    {
                        //相似元素拾取 IsSssimilar   -1相似元素第一次拾取   -2相似元素第二次拾取 其他值-普通拾取
                        if (request.IsSssimilar == "2")
                        {
                            MobileElement.OnSelected = UIElement_OnSelected_Similar;
                        }
                        else
                        {
                            MobileElement.OnSelected = UIElement_OnSelected;
                        }
                    }
                    MobileElement.OnCancelSelected = UIElement_OnCancelSelected;
                    CaptureElement?.Invoke(request.IsCvmodel == "1" ? true : false, request.CvUrl, request.IsSssimilar == "1" || request.IsSssimilar == "2" ? true : false, request.InParameter);
                });
                if (WaitHandle.WaitAny(new[] { mReset, cancellationToken.WaitHandle }) == 1)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => CancelCapture?.Invoke());
                    cancellationToken.ThrowIfCancellationRequested();
                }
                if (request.IsCvmodel == "1" && MobileDeviceManager.Instance.CvException != null)
                {
                    _logger.LogError($"CV拾取异常：{MobileDeviceManager.Instance.CvException}");
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "1",
                        Message = "智能拾取失败".I18n("MainViewModel_237CA704"),
                        Info = "{}"
                    });
                }
                if (!string.IsNullOrEmpty(globalSelector))
                {
                    _logger.LogDebug($"拾取元素：{globalSelector}");
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "0",
                        Message = "拾取成功".I18n("MainViewModel_7654EBE6"),
                        Info = globalSelector
                    });
                }
                else
                {
                    string errorMsg = device.CaptureException != null ? "拾取异常".I18n("MainViewModel_1BCAC7AF") : "取消拾取";
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "1",
                        Message = errorMsg,
                        Info = "{}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"拾取异常：{ex}");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "拾取失败".I18n("MainViewModel_FC8612CB"),
                    Info = "{}"
                });
            }
            finally
            {
                MobileDeviceManager.Instance.CvException = null;
                MobileDeviceManager.Instance.CaptureException = null;
                if (request.IsCvmodel == "1")
                {
                    device.isCvCapture = false;
                    device.ParseElement -= device.ParseCV;
                    device.ParseElement += device.ParseXml;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        device.ParseElement.Invoke();//立刻刷新一次
                    });
                }
            }
        }

        private void UIElement_OnSelected(MobileElement element)
        {
            try
            {
                var device = MobileDeviceManager.Instance;
                int initCount = 0;
                while (device.CurrentCaptureElementShot == null)
                {
                    if (initCount < 10)
                    {
                        Thread.Sleep(100);
                        initCount++;
                    }
                    else
                    {
                        _logger.LogWarning("Capture MobileShot Fail");
                        break;
                    }
                }
                elementCache = element;
                if (element == null)
                {
                    _logger.LogError("未捕获到有效元素");
                    device.CaptureException = new Exception("拾取异常".I18n("MainViewModel_1BCAC7AF"));
                    throw device.CaptureException;
                }
                if (device.CurrentCaptureElementShot != null)
                {
                    using (Bitmap bitmap = new Bitmap(device.CurrentCaptureElementShot))
                    {
                        bitmap.Save(filepath);
                    }
                }
                globalSelector = AddFileAttrToSelector(device.BuildSelectorJson, filepath);
            }
            catch (Exception ex)
            {
                _logger.LogError("保存截图出现异常", ex);
                throw;
            }
            finally
            {
                mReset.Set();
            }
        }
        private void UIElement_OnCancelSelected()
        {
            mReset.Set();
        }
        private string AddFileAttrToSelector(string orgJson, string filePath)
        {
            try
            {
                RPACommon rPACommon = new RPACommon();
                rPACommon.CommonProps = BuildMobileCommonProps(filePath);
                rPACommon.ElementDetail = BuildMobileElementDetail(elementCache.ClassName);
                JArray jArray = JArray.Parse(orgJson);
                rPACommon.Selector = jArray;
                return JsonConvert.SerializeObject(rPACommon);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                return orgJson;
            }
        }


        private void UIElement_OnSelected_CV(MobileElement element)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(filepath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filepath));
                }
                var device = MobileDeviceManager.Instance;
                int initCount = 0;
                while (device.CurrentCaptureElementShot == null)
                {
                    if (initCount < 10)
                    {
                        Thread.Sleep(100);
                        initCount++;
                    }
                    else
                    {
                        _logger.LogWarning("Capture_CV MobileShot Fail");
                    }
                }
                elementCache = element;
                if (element == null)
                {
                    _logger.LogError("未捕获到有效元素");
                    device.CaptureException = new Exception("拾取异常".I18n("MainViewModel_1BCAC7AF"));
                    throw device.CaptureException;
                }
                if (device.CurrentCaptureElementShot != null)
                {
                    using (Bitmap bitmap = new Bitmap(device.CurrentCaptureElementShot))
                    {
                        bitmap.Save(filepath);
                    }
                }
                globalSelector = AddFileAttrToSelector_CV(element.GetSelector(), filepath);
            }
            catch (Exception ex)
            {
                _logger.LogError("保存截图出现异常", ex);
                throw;
            }
            finally
            {
                mReset.Set();
            }
        }
        private string AddFileAttrToSelector_CV(string orgJson, string filePath)
        {
            try
            {
                RPACommon rPACommon = new RPACommon();
                rPACommon.CommonProps = BuildMobileCommonProps(filePath);
                rPACommon.ElementDetail = BuildMobileElementDetail(elementCache?.ClassName ?? "");
                JArray jArray = JArray.Parse(orgJson);
                rPACommon.Selector = jArray;
                return JsonConvert.SerializeObject(rPACommon);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                return orgJson;
            }
        }


        private void UIElement_OnSelected_Similar(MobileElement element)
        {
            try
            {
                elementCache = element;
                if (string.IsNullOrWhiteSpace(element?.CapturedSimilarSelector))
                    throw new InvalidOperationException("Similar selector was not prepared for the captured element");
                globalSelector = AddFileAttrToSelector_Similar(element.CapturedSimilarSelector);
            }
            catch (Exception ex)
            {
                globalSelector = null;
                MobileDeviceManager.Instance.CaptureException = ex;
                _logger.LogError("处理相似元素拾取异常", ex);
                throw;
            }
            finally
            {
                mReset.Set();
            }
        }

        private string AddFileAttrToSelector_Similar(string orgJson)
        {
            try
            {
                RPACommon rPACommon = new RPACommon();
                rPACommon.CommonProps = BuildMobileCommonProps("", useCurrentSelectorRules: true);
                rPACommon.ElementDetail = BuildMobileElementDetail(elementCache.ClassName);
                JArray jArray = JArray.Parse(orgJson);
                rPACommon.Selector = jArray;
                return JsonConvert.SerializeObject(rPACommon);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        private CommonProps BuildMobileCommonProps(string filePath, bool useCurrentSelectorRules = false)
        {
            var commonProps = new CommonProps
            {
                GUIType = eGuiType.Mobile.ToString(),
                filePath = filePath,
                captureMode = 1
            };
            if (useCurrentSelectorRules)
                commonProps.Version = MobileSelectorRules.CurrentVersion;
            return commonProps;
        }

        private ElementDetail BuildMobileElementDetail(string elementName)
        {
            var appInfo = MobileDeviceManager.Instance.GetCurrentAppInfoAsync().GetAwaiter().GetResult();
            return new ElementDetail
            {
                ElementName = elementName,
                MobilePackageName = appInfo.Item1,
                MobileActivityName = appInfo.Item2
            };
        }

        #endregion
        #region 校验元素
        /// <summary>
        /// 校验元素
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MobileResponse> CheckMobile(MobileRequest request)
        {
            return await Task.Run(async () =>
             {
                 var retResp = new MobileResponse();
                 try
                 {
                     MobileDeviceManager device = MobileDeviceManager.Instance;
                     if (!device.IsConnected)
                     {
                         retResp.Info = "{}";
                         retResp.Code = "1";
                         retResp.Message = "不存在连接的设备".I18n("MainViewModel_B35FC426");
                         return retResp;
                     }
                     await device.ResumeDesignTimeRefreshAsync("ElementValidation");
                     List<Rectangle> rects = new List<Rectangle>();
                     if (request.IsCvmodel == "1")
                     {
                         CVMobileElement cVMobileElement = new CVMobileElement(request.InParameter);
                         // string screenShot = device.Driver.GetScreenshot().AsBase64EncodedString;
                         string screenShot = await device.GetScreenshotBaseString();
                         CVHelper cVHelper = new CVHelper(screenShot, request.CvUrl);
                         cVMobileElement.LoadNewBoundRectangle(cVHelper);
                         var rectangle = cVMobileElement.BoundRectangle;
                         rectangle.X = (int)(rectangle.X / device.DpiX);
                         rectangle.Y = (int)(rectangle.Y / device.DpiY);
                         rectangle.Width = (int)(rectangle.Width / device.DpiX);
                         rectangle.Height = (int)(rectangle.Height / device.DpiY);
                         rects.Add(rectangle);
                     }
                     else
                     {
                         if (request.IsSssimilar == "1")
                         {
                             rects = new List<Rectangle>();
                             // var items = device.FindSimilarElement(request.InParameter);
                             var items = await device.GetSimilarElementsRectAsync(request.InParameter);
                             if (items.Count() < 1)
                             {
                                 _logger.LogDebug("元素校验未找到匹配元素");
                                 retResp.Info = "{}";
                                 retResp.Code = "1";
                                 retResp.Message = "元素校验未找到匹配元素".I18n("MainViewModel_49618A5E");
                                 return retResp;
                             }
                             foreach (var item in items)
                             {
                                 //rects.Add(item.Rect);
                                 rects.Add(new Rectangle
                                 {
                                     X = (int)item.X,
                                     Y = (int)item.Y,
                                     Width = (int)item.Width,
                                     Height = (int)item.Height
                                 });
                             }
                         }
                         else
                         {
                             // rects.Add(device.FindElement(request.InParameter).Rect);
                             var item = await device.GetElementRectAsync(request.InParameter);
                             rects.Add(new Rectangle
                             {
                                 X = (int)item.X,
                                 Y = (int)item.Y,
                                 Width = (int)item.Width,
                                 Height = (int)item.Height
                             });
                         }
                     }
                     Application.Current.Dispatcher.Invoke(() =>
                     {
                         bool originTopStatus = Application.Current.MainWindow.Topmost;
                         Application.Current.MainWindow.WindowState = WindowState.Normal;
                         Application.Current.MainWindow.Topmost = true;
                         Application.Current.MainWindow.Activate();
                         Application.Current.MainWindow.Topmost = originTopStatus;
                         MobileElement.OnCheckElement?.Invoke(rects);
                     });
                     retResp.Info = "{}";
                     retResp.Code = "0";
                     retResp.Message = "校验成功".I18n("MainViewModel_E48DB2CD");
                     return retResp;
                 }
                 catch (Exception ex)
                 {
                     _logger.LogError(ex);
                     retResp.Code = "1";
                     retResp.Info = "{}";
                     retResp.Message = (bool)(ex.Message?.Contains("NotFoundException")) ? "元素查找失败".I18n("MainViewModel_D28F400B") : "校验异常".I18n("MainViewModel_38923208");
                     return retResp;
                 }
             });

        }
        #endregion
        #region 获取坐标
        /// <summary>
        /// 获取坐标
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MobileResponse> GetPoint(MobileRequest request, System.Threading.CancellationToken cancellationToken = default)
        {

            var device = MobileDeviceManager.Instance;
            try
            {
                if (!device.IsConnected)
                {
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "1",
                        Message = "不存在连接的设备".I18n("MainViewModel_B35FC426"),
                        Info = "{}"
                    });
                }
                await device.ResumeDesignTimeRefreshAsync("PointCaptureBegin", cancellationToken);

                mReset = new ManualResetEvent(false);
                retPoint = "";

                Application.Current.Dispatcher.Invoke(() =>
                {
                    //双重激活、置顶窗口
                    bool originTopStatus = Application.Current.MainWindow.Topmost;
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                    Application.Current.MainWindow.Topmost = true;
                    Application.Current.MainWindow.Activate();
                    Application.Current.MainWindow.Topmost = originTopStatus;

                    MobileElement.OnCancelSelected = UIElement_OnCancelSelected;
                    MobileElement.OnGetMousePointSelected = GetElementPoint;
                    CapturePoint?.Invoke();
                });
                if (WaitHandle.WaitAny(new[] { mReset, cancellationToken.WaitHandle }) == 1)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => CancelCapture?.Invoke());
                    cancellationToken.ThrowIfCancellationRequested();
                }
                if (!string.IsNullOrEmpty(retPoint))
                {
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "0",
                        Message = "拾取成功".I18n("MainViewModel_7654EBE6"),
                        Info = retPoint
                    });
                }
                else
                {
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "1",
                        Message = "取消拾取".I18n("MainViewModel_67DAE663"),
                        Info = "{}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"拾取异常：{ex}");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "拾取失败".I18n("MainViewModel_FC8612CB"),
                    Info = "{}"
                });
            }
            finally
            {
            }

        }
        private void GetElementPoint(System.Windows.Point location)
        {
            try
            {
                retPoint = $"{location.X},{location.Y}";
            }
            catch (Exception ex)
            {
                _logger.LogError($"拾取坐标异常：{ex}");
            }
            finally
            {
                MobileElement.OnGetMousePointSelected -= GetElementPoint;
                mReset.Set();
            }
        }


        public async Task<MobileResponse> ActiveMobileWind(MobileRequest request)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    bool originTopStatus = Application.Current.MainWindow.Topmost;
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                    Application.Current.MainWindow.Topmost = true;
                    Application.Current.MainWindow.Activate();
                    Application.Current.MainWindow.Topmost = originTopStatus;
                });

                return await Task.FromResult(new MobileResponse
                {
                    Code = "0",
                    Message = "激活窗口成功".I18n("MainViewModel_D2929CFF"),
                    Info = "{}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"主窗口激活失败:{ex}");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "激活窗口失败".I18n("MainViewModel_25833B76"),
                    Info = "{}"
                });
            }

        }

        public async Task<MobileResponse> CloseMobileDeviceApp(MobileRequest request)
        {
            try
            {
                Application.Current.Shutdown();
                return await Task.FromResult(new MobileResponse
                {
                    Code = "0",
                    Message = "窗口关闭成功".I18n("MainViewModel_62570378"),
                    Info = "{}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"窗口关闭失败:{ex}");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "窗口关闭失败".I18n("MainViewModel_E0AE51B9"),
                    Info = "{}"
                });
            }
        }

        public async Task<MobileResponse> GetCurrentAppPackageName(MobileRequest request)
        {
            try
            {
                string packageName = await MobileDeviceManager.Instance.GetCurrentAppPackageNameAsync();
                if (string.IsNullOrEmpty(packageName))
                {
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "1",
                        Message = "未获取到app包名".I18n("MainViewModel_CA17E58D"),
                        Info = "{}"
                    });
                }
                return await Task.FromResult(new MobileResponse
                {
                    Code = "0",
                    Message = "获取app包名成功".I18n("MainViewModel_1B1257F6"),
                    Info = packageName.ToJson()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"获取app包名失败:{ex}");
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "获取app包名失败".I18n("MainViewModel_441311BB"),
                    Info = "{}"
                });
            }
        }



        #endregion
        #region 手机图像截图和获取范围

        private ScreenShoter m_ScreenShoter;
        private ConcurrencyItemContainer<Int32Rect> m_SnapResult = new ConcurrencyItemContainer<Int32Rect>();
        private string targetImagePath;
        private string targetImageName;
        private string imageId;
        private object targetImageResolution;
        /// <summary>
        /// 手机图像截图
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MobileResponse> CaptureMobileElementShot(MobileRequest request)
        {
            targetImagePath = "";
            targetImageName = "";
            imageId = "";
            targetImageResolution = null;
            var device = MobileDeviceManager.Instance;
            try
            {
                m_ScreenShoter = new ScreenShoter();
                m_ScreenShoter.Snapped += ScreenShoter_Snapped;
                if (!device.IsConnected)
                {
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "1",
                        Message = "不存在连接的设备".I18n("MainViewModel_B35FC426"),
                        Info = "{}"
                    });
                }

                filepath = request.FilePath;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    //双重激活、置顶窗口
                    bool originTopStatus = Application.Current.MainWindow.Topmost;
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                    Application.Current.MainWindow.Topmost = true;
                    Application.Current.MainWindow.Activate();
                    Application.Current.MainWindow.Topmost = originTopStatus;
                });
                m_ScreenShoter.PreStart();
                MobileDeviceManager.Instance.Semaphore.WaitOne();
                if (MobileDeviceManager.Instance.CancelException != null)
                {
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "1",
                        Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage(),
                    });
                }
                m_ScreenShoter.Start();
                m_SnapResult.WaitForData(-1);
                var rect = m_SnapResult.TakeData();
                if (rect == Int32Rect.Empty)
                {
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "1",
                        Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage(),
                    });
                }
                else if (rect.X == -1 && rect.Y == -1)
                {
                    _logger.LogError($"手机图像截图超出手机屏幕范围");
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "1",
                        Message = "截图超出手机屏幕有效区域".I18n("MainViewModel_9801629D"),
                        Info = "{}"
                    });
                }
                else
                {
                    string packageName = await MobileDeviceManager.Instance.GetCurrentAppPackageNameAsync();
                    var data = new
                    {
                        imgName = targetImageName,
                        imgSrc = targetImagePath,
                        imgId = imageId,
                        imgResolution = targetImageResolution,
                        deviceResolution = MobileDeviceManager.Instance.mobileDeviceSoultion,
                        device = "mobile",
                        categoryName = packageName,
                        categoryIcon = ""
                    };
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "0",
                        Message = "截图已完成".I18n("MainViewModel_CBBB9AB5"),
                        Info = data.ToJson()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"手机图像截图失败", ex);
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "截图失败".I18n("MainViewModel_2AB605DF"),
                    Info = "{}"
                });
            }
            finally
            {
                MobileDeviceManager.Instance.Semaphore.SafeRelease();
            }
        }
        /// <summary>
        /// 区域范围拾取
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MobileResponse> CaptureDomainScope(MobileRequest request)
        {
            m_ScreenShoter = new ScreenShoter();
            m_ScreenShoter.Snapped += ScreenScope_Snapped;
            var device = MobileDeviceManager.Instance;
            try
            {
                if (!device.IsConnected)
                {
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "1",
                        Message = "不存在连接的设备".I18n("MainViewModel_B35FC426"),
                        Info = "{}"
                    });
                }
                filepath = request.FilePath;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    //双重激活、置顶窗口
                    bool originTopStatus = Application.Current.MainWindow.Topmost;
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                    Application.Current.MainWindow.Topmost = true;
                    Application.Current.MainWindow.Activate();
                    Application.Current.MainWindow.Topmost = originTopStatus;
                });
                m_ScreenShoter.Start();
                m_SnapResult.WaitForData(-1);
                var rect = m_SnapResult.TakeData();
                if (rect == Int32Rect.Empty)
                {
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "1",
                        Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage(),
                    });
                }
                else
                {
                    List<List<int>> scope = new List<List<int>> {
                        new List<int>{ rect.X, rect.Y },
                        new List<int>{ rect.X+rect.Width, rect.Y +rect.Height}
                    };
                    var data = new
                    {
                        scope = scope,
                        deviceResolution = MobileDeviceManager.Instance.mobileDeviceSoultion
                    };
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "0",
                        Message = "拾取范围成功".I18n("MainViewModel_6631732B"),
                        Info = data.ToJson()
                    });
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Equals(eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage()))
                {
                    return await Task.FromResult(new MobileResponse
                    {
                        Code = "1",
                        Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage(),
                    });
                }
                return await Task.FromResult(new MobileResponse
                {
                    Code = "1",
                    Message = "拾取失败".I18n("MainViewModel_FC8612CB"),
                    Info = "{}"
                });
            }
            finally
            {
            }
        }
        /// <summary>
        /// 图像校验
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MobileResponse> CheckMobileImage(MobileRequest request)
        {
            return await Task.Run(async () =>
            {
                var retResp = new MobileResponse();
                try
                {
                    List<Rectangle> rects = new List<Rectangle>();
                    MobileDeviceManager device = MobileDeviceManager.Instance;
                    if (!device.IsConnected)
                    {
                        retResp.Info = "{}";
                        retResp.Code = "1";
                        retResp.Message = "不存在连接的设备".I18n("MainViewModel_B35FC426");
                        return retResp;
                    }

                    var paramModel = JsonConvert.DeserializeObject<ParamElementShotClickModel>(request.InParameter);
                    string directoryPath = Path.GetDirectoryName(paramModel.imgSrc);
                    string fileName = Guid.NewGuid().ToString() + ".png";
                    //记得将生成的手机屏幕图片执行完删除（算法需要具体路径，所以要临时生成）
                    var bitmap = await MobileDeviceManager.Instance.GetScreenshot();
                    bitmap.SaveToLocalFile(Path.Combine(directoryPath, fileName));
                    CVImageHelper cVImageHelper = new CVImageHelper();
                    var param = new ImageMatchingParams
                    {
                        Source = Path.Combine(directoryPath, fileName),
                        Template = paramModel.imgSrc,
                        Thr = paramModel.thr,
                        DomainArea = paramModel.scopeRect == null ?
                        new List<int> {
                                0,
                                0,
                                (int)bitmap.Width,
                                (int)bitmap.Height,
                             } :
                        new List<int> {
                                paramModel.scopeRect.startX,
                                paramModel.scopeRect.startY,
                                paramModel.scopeRect.endX- paramModel.scopeRect.startX,
                                paramModel.scopeRect.endY- paramModel.scopeRect.startY,
                             },
                        OriginalPix = paramModel.deviceResolution
                    };
                    string imageRectResult = cVImageHelper.InspectImageByCV(param);
                    var rectsData = JsonConvert.DeserializeObject<List<List<List<int>>>>(imageRectResult);
                    if (rectsData != null && rectsData.Count > 0)
                    {
                        rectsData.ForEach(rectData =>
                        {
                            rects.Add(new Rectangle
                            {
                                X = (int)(rectData[0][0] / device.DpiX),
                                Y = (int)(rectData[0][1] / device.DpiY),
                                Width = (int)((rectData[1][0] - rectData[0][0]) / device.DpiX),
                                Height = (int)((rectData[1][1] - rectData[0][1]) / device.DpiY)
                            });
                        });
                    }
                    else
                    {
                        throw new Exception("图像识别失败".I18n("controlType_MainViewModel_84B8F643"));
                    }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        bool originTopStatus = Application.Current.MainWindow.Topmost;
                        Application.Current.MainWindow.WindowState = WindowState.Normal;
                        Application.Current.MainWindow.Topmost = true;
                        Application.Current.MainWindow.Activate();
                        Application.Current.MainWindow.Topmost = originTopStatus;
                        MobileElement.OnCheckElement?.Invoke(rects);
                    });
                    retResp.Info = "{}";
                    retResp.Code = "0";
                    retResp.Message = "校验成功".I18n("MainViewModel_E48DB2CD");
                    return retResp;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex);
                    retResp.Code = "1";
                    retResp.Info = "{}";
                    retResp.Message = !string.IsNullOrEmpty(ex.Message) ? ex.Message : "图像校验异常".I18n("MainViewModel_EC781305");
                    return retResp;
                }
            });

        }

        #region Action
        private async void ScreenShoter_Snapped(object sender, FunctionEventArgs<System.Windows.Media.ImageSource> e)
        {
            try
            {
                if (e.IsDone)
                {
                    CroppedBitmap image = e.Info as CroppedBitmap;
                    imageId = RandomHelper.RandomString(10);
                    string pictureRandomName = $"image-{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}.png".Trim().Replace(" ", string.Empty);

                    string resPicturePath = Path.Combine("res", imageId);
                    string absPath = Path.Combine(filepath, resPicturePath);

                    if (!Directory.Exists(absPath))
                    {
                        Directory.CreateDirectory(absPath);
                    }
                    targetImagePath = Path.Combine(resPicturePath, pictureRandomName);
                    targetImageName = pictureRandomName;
                    var deviceToScreenRect = MobileDeviceManager.Instance.mobileDeviceRect;//手机渲染到电脑桌面的区域信息
                    var dpiX = MobileDeviceManager.Instance.DpiX;
                    var dpiY = MobileDeviceManager.Instance.DpiY;
                    _logger.LogInfo($"电脑桌面dpi，X:{dpiX}, Y:{dpiY}");

                    if (image == null || deviceToScreenRect.Width <= 0 || deviceToScreenRect.Height <= 0 ||
                        dpiX <= 0 || dpiY <= 0)
                    {
                        throw new InvalidOperationException("截图坐标换算所需的信息无效");
                    }

                    // SourceRect 是桌面截图的物理像素坐标，mobileDeviceRect 是 WPF DIP。
                    // 使用 Floor/Ceiling 取得包含手机画面的物理像素边界，避免 double 到 int 截断后误判越界。
                    var deviceLeft = (int)Math.Floor(deviceToScreenRect.Left * dpiX);
                    var deviceTop = (int)Math.Floor(deviceToScreenRect.Top * dpiY);
                    var deviceRight = (int)Math.Ceiling(deviceToScreenRect.Right * dpiX);
                    var deviceBottom = (int)Math.Ceiling(deviceToScreenRect.Bottom * dpiY);

                    var selectedLeft = image.SourceRect.X;
                    var selectedTop = image.SourceRect.Y;
                    var selectedRight = image.SourceRect.X + image.SourceRect.Width;
                    var selectedBottom = image.SourceRect.Y + image.SourceRect.Height;

                    // 截图窗口初始化及 CroppedBitmap 都会发生整数截断，容许一个 DIP 换算后的像素误差。
                    var toleranceX = Math.Max(2, (int)Math.Ceiling(dpiX) + 1);
                    var toleranceY = Math.Max(2, (int)Math.Ceiling(dpiY) + 1);
                    var isOutsideDevice =
                        selectedLeft < deviceLeft - toleranceX ||
                        selectedTop < deviceTop - toleranceY ||
                        selectedRight > deviceRight + toleranceX ||
                        selectedBottom > deviceBottom + toleranceY;

                    if (isOutsideDevice)
                    {
                        _logger.LogWarning(
                            $"截图超出手机区域。选区:[{selectedLeft},{selectedTop},{selectedRight},{selectedBottom}]，" +
                            $"手机区域:[{deviceLeft},{deviceTop},{deviceRight},{deviceBottom}]，" +
                            $"容差:[{toleranceX},{toleranceY}]");
                        m_SnapResult.SetData(new Int32Rect { X = -1, Y = -1 });
                        return;
                    }

                    // 在容差范围内认为用户选择的是手机边缘，并吸附到准确边界。
                    if (Math.Abs(selectedLeft - deviceLeft) <= toleranceX) selectedLeft = deviceLeft;
                    if (Math.Abs(selectedRight - deviceRight) <= toleranceX) selectedRight = deviceRight;
                    if (Math.Abs(selectedTop - deviceTop) <= toleranceY) selectedTop = deviceTop;
                    if (Math.Abs(selectedBottom - deviceBottom) <= toleranceY) selectedBottom = deviceBottom;

                    selectedLeft = Math.Max(selectedLeft, deviceLeft);
                    selectedTop = Math.Max(selectedTop, deviceTop);
                    selectedRight = Math.Min(selectedRight, deviceRight);
                    selectedBottom = Math.Min(selectedBottom, deviceBottom);

                    if (selectedRight <= selectedLeft || selectedBottom <= selectedTop)
                    {
                        throw new ArgumentOutOfRangeException("截图区域宽度或高度无效");
                    }

                    var bitmap = await MobileDeviceManager.Instance.GetScreenshot();
                    if (bitmap == null || bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
                    {
                        throw new InvalidOperationException("未获取到有效的手机截图");
                    }

                    // 使用待裁剪图片的真实像素尺寸，并分别计算横纵缩放比例。
                    var scaleX = bitmap.PixelWidth / (double)(deviceRight - deviceLeft);
                    var scaleY = bitmap.PixelHeight / (double)(deviceBottom - deviceTop);
                    _logger.LogInfo($"手机实际屏幕与桌面渲染区域比例，X:{scaleX}, Y:{scaleY}");

                    // 按四条边换算，左/上向下取整，右/下向上取整，避免分别换算宽高造成边界丢失。
                    var startX = (int)Math.Floor((selectedLeft - deviceLeft) * scaleX);
                    var startY = (int)Math.Floor((selectedTop - deviceTop) * scaleY);
                    var endX = (int)Math.Ceiling((selectedRight - deviceLeft) * scaleX);
                    var endY = (int)Math.Ceiling((selectedBottom - deviceTop) * scaleY);

                    startX = Math.Max(0, Math.Min(startX, bitmap.PixelWidth));
                    startY = Math.Max(0, Math.Min(startY, bitmap.PixelHeight));
                    endX = Math.Max(startX, Math.Min(endX, bitmap.PixelWidth));
                    endY = Math.Max(startY, Math.Min(endY, bitmap.PixelHeight));

                    var sourceWidth = endX - startX;
                    var sourceHeight = endY - startY;
                    if (sourceWidth <= 0 || sourceHeight <= 0)
                    {
                        throw new ArgumentOutOfRangeException("换算后的手机截图区域宽度或高度无效");
                    }

                    targetImageResolution = new { width = sourceWidth, height = sourceHeight };
                    var sourceElementBitmap = CropBitmapImage(bitmap, startX, startY, sourceWidth, sourceHeight);
                    var sourceElemenImgPath = Path.Combine(absPath, pictureRandomName);
                    _logger.LogInfo($"原图抠出的图片路径为：{sourceElemenImgPath}");
                    sourceElementBitmap.SaveToLocalFile(sourceElemenImgPath);

                    m_SnapResult.SetData(image.SourceRect);
                }
                else
                {
                    m_SnapResult.SetData(Int32Rect.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("处理截图失败", ex);
                m_SnapResult.SetData(Int32Rect.Empty);
            }
        }

        private void ScreenScope_Snapped(object sender, FunctionEventArgs<System.Windows.Media.ImageSource> e)
        {
            if (e.IsDone)
            {
                CroppedBitmap image = e.Info as CroppedBitmap;
                var deviceToScreenRect = MobileDeviceManager.Instance.mobileDeviceRect;//手机渲染到电脑桌面的区域信息
                var dpi = MobileDeviceManager.Instance.DpiX;
                var sacal = MobileDeviceManager.Instance.mobileForm._bitmapImageWidth / deviceToScreenRect.Width;
                var startX = (image.SourceRect.X - (deviceToScreenRect.X * dpi)) * sacal / dpi;
                var startY = (image.SourceRect.Y - (deviceToScreenRect.Y * dpi)) * sacal / dpi;
                var sourceWidth = image.SourceRect.Width * sacal / dpi;
                var sourceHeight = image.SourceRect.Height * sacal / dpi;
                m_SnapResult.SetData(new Int32Rect { X = (int)startX, Y = (int)startY, Width = (int)sourceWidth, Height = (int)sourceHeight });
                //m_SnapResult.SetData(image.SourceRect);
            }
            else
            {
                m_SnapResult.SetData(Int32Rect.Empty);
            }
        }


        public static BitmapImage CropBitmapImage(BitmapImage sourceImage, int x, int y, int width, int height)
        {
            if (sourceImage == null)
                throw new ArgumentNullException(nameof(sourceImage));
            if (x < 0 || y < 0 || width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException("裁剪区域超出源图像范围".I18n("controlType_MainViewModel_CAE772C4"));
            }

            var rightOverflow = x + width - sourceImage.PixelWidth;
            var bottomOverflow = y + height - sourceImage.PixelHeight;

            //兼容历史调用中因坐标换算产生的单像素进位，宽高同时越界时分别修正。
            if (rightOverflow > 0)
            {
                if (rightOverflow == 1)
                {
                    width -= rightOverflow;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("裁剪区域超出源图像范围".I18n("controlType_MainViewModel_CAE772C4"));
                }
            }

            if (bottomOverflow > 0)
            {
                if (bottomOverflow == 1)
                {
                    height -= bottomOverflow;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("裁剪区域超出源图像范围".I18n("controlType_MainViewModel_CAE772C4"));
                }
            }

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException("裁剪区域超出源图像范围".I18n("controlType_MainViewModel_CAE772C4"));
            }
            CroppedBitmap croppedBitmap = new CroppedBitmap(
                sourceImage,
                new Int32Rect(x, y, width, height)
            );
            // 将CroppedBitmap转换为BitmapImage（可选，若需要BitmapImage类型）
            BitmapImage resultImage = new BitmapImage();
            using (var stream = new System.IO.MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder(); // 可根据需要选择编码器（Jpeg、Bmp等）
                encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));
                encoder.Save(stream);
                stream.Position = 0;

                resultImage.BeginInit();
                resultImage.CacheOption = BitmapCacheOption.OnLoad;
                resultImage.StreamSource = stream;
                resultImage.EndInit();
                resultImage.Freeze(); // 冻结以提高性能（若不需要修改）
            }
            return resultImage;
        }
        #endregion
        #endregion
        #region 滑动测试
        public async Task<MobileResponse> TestSlid(MobileRequest request)
        {
            return await Task.Run(async () =>
            {
                var retResp = new MobileResponse();
                try
                {
                    MobileDeviceManager device = MobileDeviceManager.Instance;
                    if (!device.IsConnected)
                    {
                        retResp.Info = "{}";
                        retResp.Code = "1";
                        retResp.Message = "不存在连接的设备".I18n("MainViewModel_B35FC426");
                        return retResp;
                    }
                    await device.TestSlid(request.InParameter);
                    retResp.Info = "{}";
                    retResp.Code = "0";
                    retResp.Message = "测试成功".I18n("MainViewModel_E48DB2CD");
                    return retResp;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex);
                    retResp.Code = "1";
                    retResp.Info = "{}";
                    retResp.Message = (bool)(ex.Message?.Contains("NotFoundException")) ? "元素查找失败".I18n("MainViewModel_D28F400B") : "校验异常".I18n("MainViewModel_38923208");
                    return retResp;
                }
            });
        }
        #endregion
        #endregion
    }
}


