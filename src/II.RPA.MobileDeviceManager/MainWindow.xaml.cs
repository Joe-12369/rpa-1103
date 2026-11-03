using II.RPA.Logging;
using II.RPA.MobileDeviceManager.Common;
using II.RPA.MobileDeviceManager.Models;
using II.RPA.MobileDeviceManager.ViewModels;
using II.RPA.MobileDeviceManager.Views;
using II.RPA.Plugin.Core.Wpf.Controls.Windows;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui;

namespace II.RPA.MobileDeviceManager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : FluentWindow, IDisposable
    {
        private static readonly ILogger _logger = Logger.OfContext();
        private readonly Subject<GlobalEventSubModel> _eventSubject;
        public Func<MessageResult> ShowWarningWndDialogHandle;
        private MainViewModel _mainViewModel;
        private readonly IDisposable _subscription;

        public MainWindow(Subject<GlobalEventSubModel> eventSubject,
            MainViewModel mainViewModel,
            IContentDialogService contentDialogService) : base()
        {

            InitializeComponent();
            WindowChromeHeight = 35d;
            _eventSubject = eventSubject;
            DataContext = mainViewModel;
            _mainViewModel = mainViewModel;
            contentDialogService.SetContentPresenter(RootContentDialog);
            this.ShowWarningWndDialogHandle = ShowWarningWndDialog;

            SizeChanged += MainWindow_SizeChanged;
            ShowActivated = true;
            Loaded += MainWindow_Loaded;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            if (MobileDeviceManager.Instance.DeviceInfos != null
                && MobileDeviceManager.Instance.DeviceInfos.Count > 0
                && MobileDeviceManager.Instance.IsInstallPlugin)
            {
                Width = 560;
            }

            _subscription =
            _eventSubject
                .ObserveOn((System.Threading.SynchronizationContext.Current ?? new System.Threading.SynchronizationContext()))
                .Subscribe(
                onNext: t =>
                {
                    _logger.LogInformation($"{t.EventType}");
                    switch (t.EventType)
                    {
                        case EventType.MoblieConnecting:
                            Width = 600;
                            MinWidth = 600;
                            break;
                        case EventType.MoblieConnected:
                            Width = 600;
                            MinWidth = 600;
                            break;
                        case EventType.MoblieConnectFailed:
                            Width = 600;
                            MinWidth = 600;
                            break;
                        case EventType.MoblieDisconnected:
                            Width = 600;
                            MinWidth = 600;
                            break;
                        case EventType.HorizontalScreenOrientation:
                            Width = 1200;
                            break;
                        case EventType.verticalScreenOrientation:
                            Width = 600;
                            break;
                    }
                },
                onError: ex =>
                {
                    _logger.LogError(ex);
                },
                onCompleted: () =>
                {

                });
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }

        private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private async void CloseWin_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (_mainViewModel.MobileInstallStatus == E_MobileTool_Install_Status.Installing)
                {
                    _mainViewModel.IsLoadingMask = true;

                    MessageResult _MessageResult = null;
                    if (ShowWarningWndDialogHandle != null)
                    {
                        _MessageResult = ShowWarningWndDialogHandle.Invoke();
                    }
                    if (_MessageResult != null)
                    {
                        if (!_MessageResult.IsYes)
                        {
                            _mainViewModel.IsLoadingMask = false;
                            return;
                        }
                    }
                    else
                    {
                        _mainViewModel.IsLoadingMask = false;
                        return;
                    }
                    //回写取消下载安装插件结果至客户端
                    await _mainViewModel.SendMessageToClint(action: "1", resultType: 2);
                    //删除已下载的残留文件
                    await _mainViewModel.DeleteFileOfInstallFail();
                    Application.Current.Shutdown();
                    this.Close();


                }
                else
                {
                    Application.Current.Shutdown();
                    this.Close();
                }
            }
            catch
            {
                Application.Current.Shutdown();
                this.Close();
            }
        }

        private void MaxWin_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
            {
                WindowState = WindowState.Maximized;
                _mainViewModel.MaxMinIcon = "\xe70d";
            }
            else
            {
                WindowState = WindowState.Normal;
                _mainViewModel.MaxMinIcon = "\xe707";
            }
        }

        private void MinWin_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Grid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    if (WindowState == WindowState.Normal)
                    {
                        WindowState = WindowState.Maximized;
                    }
                    else
                    {
                        WindowState = WindowState.Normal;
                    }
                }
                else
                {
                    DragMove();
                }
            }
        }
        /// <summary>
        /// 钉住窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pin_Click(object sender, RoutedEventArgs e)
        {
            if (this.Topmost)
            {
                _mainViewModel.PinWinIcon = "\xe6dd";
                this.Topmost = false;
            }
            else
            {
                _mainViewModel.PinWinIcon = "\xe6dc";
                this.Topmost = true;
            }
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }

        private void Collapse_Click(object sender, RoutedEventArgs e)
        {
            List_Col_Expand.Width = new GridLength(0);
            List_Col_Collapse.Width = new GridLength(40);
            //if (Width == 560)
            if (Width == 600)
            {
                Width -= 160;
            }
        }

        private void Expand_Click(object sender, RoutedEventArgs e)
        {
            List_Col_Expand.Width = new GridLength(200);
            List_Col_Collapse.Width = new GridLength(0);
            //if (Width == 400)
            if (Width == 440)
            {
                Width += 160;
            }
        }
        private MessageResult ShowWarningWndDialog()
        {
            double[] d = new double[2];
            d[0] = this.Top + this.Height / 2;
            d[1] = this.Left + this.Width / 2;
            MessageResult _MessageResult = null;
            WarningWnd warningWnd = new WarningWnd();
            warningWnd.Result += (s, e) =>
            {
                _MessageResult = e.Result;
            };
            warningWnd.Top = d[0] - 158 / 2;
            warningWnd.Left = d[1] - 340 / 2;
            warningWnd.ShowDialog();
            return _MessageResult;
        }

        private void ReInstall_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mainViewModel.InstallMobileToolCommand?.Execute(null);
        }

        private void LinkHelp_Clinck(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://rpa-college.ai-indeed.com/doc/7345.html");
        }

    }
}

