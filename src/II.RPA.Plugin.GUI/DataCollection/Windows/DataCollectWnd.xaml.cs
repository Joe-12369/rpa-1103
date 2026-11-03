using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.DataCollection.ViewModels;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI.DataCollection.Windows
{
    /// <summary>
    /// DataCollectWnd.xaml 的交互逻辑
    /// </summary>
    public partial class DataCollectWnd : Window
    {
        private static ILogger logger = Logger.OfContext();

        public DataCollectWnd()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
        }

        //当前屏幕缩放 计算窗体Rect使用
        private double CurrentScale = 1;
        public Action ChildWindowCloseAction = () => { };
        public DataCollectWnd(ParamDataCollectConfigurationModel paramDataCollectConfigurationModel) : this()
        {
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
            GlobalData.GetInstance().VM_MainWindowViewModel = new MainWindowViewModel();
            GlobalData.GetInstance().VM_MainWindowViewModel.InitCaptureCollection(paramDataCollectConfigurationModel,
                GetRectOfMainWindow, SetWindowState, SetTurnPageImage, ShowError, ForceCloseEx,
                SetDataGridColState, SetWndPositionAndSizeState, SetMaxSizeButtonImage, ScrollToEnd,
                GetCurrentDataGridWidth, Refresh, GetCurrentWindowState);

            MessageHelper.SetShowWeakError(ShowError);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //UI线程未捕获异常处理事件
            this.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            //Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            //非UI线程未捕获异常处理事件
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

            NativeMethods.SetProcessDPIAware();
            CurrentScale = (double)NativeMethods.DpiX / 96;
            //WindowState = WindowState.Normal;
            //this.Width = 560;
            //this.Height = 388;

            Rect rc = SystemParameters.WorkArea;//获取工作区大小
            double DWidth = rc.Width;
            double DHeigth = rc.Height / 3;
            this.Top = DHeigth;
            this.Left = DWidth - this.ActualWidth;

            SetImageSourceForLoading();
            DataCollectingManager.UIThreadRunAction = RunAction;
            MessageService.UIThreadRunAction = PluginRunFunc;
            MessageHelper.SetParentWnd(this);
            //开始拾取
            this.DataContext = GlobalData.GetInstance().VM_MainWindowViewModel;

            ///调用vm里面判断二次配置的方法并反填数据
            GlobalData.GetInstance().VM_MainWindowViewModel.LoadPreviewConfigData();
            GlobalData.GetInstance().VM_MainWindowViewModel.ShowWeakError(false);
            this.Opacity = 1;
        }

        /// <summary>
        /// 测试新版报错弱提示
        /// </summary>
        private void TestNewWeakErrorMessage()
        {
            Task.Factory.StartNew(() =>
            {
                string strErrMsg = "Error Code";
                for (int i = 0; i < 10; i++)
                {
                    Thread.Sleep(1000);
                    strErrMsg = $"{i} ********" + strErrMsg;
                    logger.LogInformation("初始化调用ShowError");
                    GlobalData.GetInstance().VM_MainWindowViewModel?.ShowErrorOnPanelEx(strErrMsg);
                    Thread.Sleep(1000);
                }
            });
        }

        private void SetImageSourceForLoading()
        {
            if (!this.CheckAccess())
            {
                this.Dispatcher.Invoke(() =>
                {
                    ShowLodingLogo();
                });
                return;
            }
            else
            {
                ShowLodingLogo();
            }
        }

        private void ShowLodingLogo()
        {
            Assembly Asmb = Assembly.GetExecutingAssembly();
            Stream ManifestStream = Asmb.GetManifestResourceStream("II.RPA.Plugin.GUI.DataCollection.Windows.DualBall.gif");
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = ManifestStream;
                image.EndInit();
                ImageBehavior.SetAnimatedSource(im_RunLog, image);
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e.ExceptionObject as Exception, "触发意外的非UI线程问题");
            MessageHelper.ShowWeakError("有意外阻止采集，请保存当前数据再重新进入拾取".I18n("DataCollectWnd.xaml_4929ED2B"));
            DisposeThisWindow();
        }

        public void DisposeThisWindow()
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(5000);
                ForceCloseEx();
            });
        }


        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e.Exception, "触发意外的Task线程问题");
            MessageHelper.ShowWeakError("有意外阻止采集，请保存当前数据再重新进入拾取".I18n("DataCollectWnd.xaml_4929ED2B"));
            DisposeThisWindow();
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e.Exception, "触发意外的UI问题");
            MessageHelper.ShowWeakError("有意外阻止采集，请重新拾取".I18n("DataCollectWnd.xaml_F99DCAF5"));
            DisposeThisWindow();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            CloseWindowWithCancel();
        }

        /// <summary>
        /// 确认点击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Confirm_Click(object sender, RoutedEventArgs e)
        {
            GlobalData.GetInstance().VM_MainWindowViewModel.CapturePageModel.SetTurnPageButtonInfo(GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.turnPageButtonInfo);
            //GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.RemoveAllPrefix();
            GlobalData.GetInstance().VM_MainWindowViewModel.IsDataValid = true;
            DataCollectingManager.StopCapture();

            //数据埋点
            GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.is_success = 1;

            if (!this.CheckAccess())
            {
                this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    this.Close();
                });
                return;
            }
            else
            {
                this.Close();
            }
        }

        #region 显示/关闭 蒙版加载效果
        LoadWaitingWnd loadWaitingWnd = null;
        bool bHasShow = false;
        public void ShowLoadWaiting(bool bShow)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (bShow)
                {
                    if (!bHasShow)
                    {
                        loadWaitingWnd = new LoadWaitingWnd();
                        loadWaitingWnd.Owner = this;
                        loadWaitingWnd.ShowActivated = true;
                        loadWaitingWnd.Show();
                        bHasShow = true;
                    }
                    else
                    {
                        loadWaitingWnd.Activate();
                    }
                }
                else
                {
                    if (loadWaitingWnd != null)
                    {
                        loadWaitingWnd.Close();
                        loadWaitingWnd = null;
                    }
                    bHasShow = false;
                }
            });

        }

        #endregion

        #region 关闭操作
        public void CloseWindowWithCancel()
        {
            GlobalData.GetInstance().VM_MainWindowViewModel.IsDataValid = false;
            GlobalData.GetInstance().VM_MainWindowViewModel.CaptureJMessage.Code = 1;
            GlobalData.GetInstance().VM_MainWindowViewModel.CaptureJMessage.UnifyCode = "1";
            GlobalData.GetInstance().VM_MainWindowViewModel.CaptureJMessage.Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage();
            if (!this.CheckAccess())
            {
                this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    CloseWindowEx();
                });
                return;
            }
            else
            {
                CloseWindowEx();
            }
        }


        private void CloseWindowEx(bool IsDataValid = false)
        {
            GlobalData.GetInstance().VM_MainWindowViewModel.IsDataValid = IsDataValid;
            if (GlobalData.GetInstance().VM_MainWindowViewModel != null)
            {
                bool bRet = GlobalData.GetInstance().VM_MainWindowViewModel.ShowDialog("是否取消此次操作?".I18n("DataCollectWnd.xaml_23DCF481"), "", this);
                if (bRet)
                {
                    DataCollectingManager.StopCapture();
                    MessageHelper.Dipose();
                    this.Close();
                }
            }
            else
            {
                DataCollectingManager.StopCapture();
                MessageHelper.Dipose();
                this.Close();
            }
        }

        /// <summary>
        /// 强制关闭
        /// </summary>
        public void ForceCloseEx()
        {
            if (!this.CheckAccess())
            {
                this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    ForceCloseEx();
                });
                return;
            }
            else
            {
                this.ChildWindowCloseAction?.Invoke();
                this.Close();
            }
        }

        #endregion


        BitmapImage ArrowUpImage = new BitmapImage(new Uri("../DataCollection/Assets/Images/ArrowUp.png", UriKind.Relative));
        BitmapImage ArrowDownImage = new BitmapImage(new Uri("../DataCollection/Assets/Images/ArrowDown.png", UriKind.Relative));

        bool BIsArrowImageActive = false;

        private void ArrowDown_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!BIsArrowImageActive)
            {
                Image CImage = sender as Image;
                CImage.Source = ArrowUpImage;
                BIsArrowImageActive = true;
            }
            else
            {
                Image CImage = sender as Image;
                CImage.Source = ArrowDownImage;
                BIsArrowImageActive = false;
            }
        }

        private void btn_Close_Click(object sender, MouseButtonEventArgs e)
        {
            CloseWindowWithCancel();
        }


        private void Pop_PageCapture_Loaded(object sender, RoutedEventArgs e)
        {
            PopopHelper.SetPopupPlacementTarget(this.Pop_PageCapture, this);
        }


        /// <summary>
        /// 获取当前窗体的框体范围
        /// </summary>
        /// <returns></returns>
        public List<Rect> GetRectOfMainWindow()
        {
            List<Rect> rects = new List<Rect>();
            Rect rect = new Rect();
            if (this.WindowState == WindowState.Minimized)
            {
                rect = new Rect(new Point(0, 0), new Size(0, 0));
            }
            else
            {
                //logger.LogInformation($"Current Rect1:{this.Left} {this.Top} {this.ActualWidth} {this.ActualHeight}");
                rect = new Rect(new Point(this.Left * CurrentScale, this.Top * CurrentScale), new Size(this.ActualWidth * CurrentScale, this.ActualHeight * CurrentScale));
                //logger.LogInformation($"Current Rect2:{this.Left * CurrentScale} {this.Top * CurrentScale} {this.ActualWidth * CurrentScale} {this.ActualHeight * CurrentScale}");
                //logger.LogInformation($"Current Rect3:{rect.Left} {rect.Top} {rect.Right} {rect.Bottom}");
            }
            rects.Add(rect);
            return rects;
        }


        /// <summary>
        /// 获取当前表格的实际宽度
        /// </summary>
        /// <returns></returns>
        public double GetCurrentDataGridWidth(int iDataGridType)
        {
            double dRet = 0.0;
            this.Dispatcher.Invoke(() =>
            {
                if (iDataGridType == 0)
                {
                    dRet = this.StaffAdminView.ActualWidth;
                }
                else
                {
                    dRet = this.dg_CaptureTable.ActualWidth;
                }
            });

            return dRet;
        }

        /// <summary>
        /// 获取当前窗体状态 最小化还是正常显示状态 0 隐藏 1 正常
        /// </summary>
        /// <returns></returns>
        public int GetCurrentWindowState()
        {
            int _iState = 0;
            this.Dispatcher.Invoke(() =>
            {
                if (this.WindowState == WindowState.Minimized || this.Visibility == Visibility.Hidden || this.Visibility == Visibility.Collapsed)
                {
                    _iState = 0;
                }
                else
                {
                    _iState = 1;
                }
            });

            return _iState;
        }


        /// <summary>
        /// 移入鼠标
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_CaptureOperation_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (GlobalData.GetInstance().VM_MainWindowViewModel.CapturePageModel.ImageTurnPageButtonVisibility == Visibility.Visible)
            {
                GlobalData.GetInstance().VM_MainWindowViewModel.CapturePageModel.EditPanelTurnPageButtonVisibility = Visibility.Visible;
            }
            else
            {
                GlobalData.GetInstance().VM_MainWindowViewModel.CapturePageModel.EditPanelTurnPageButtonVisibility = Visibility.Collapsed;
            }
        }

        private void Border_CaptureOperation_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GlobalData.GetInstance().VM_MainWindowViewModel.CapturePageModel.EditPanelTurnPageButtonVisibility = Visibility.Collapsed;
        }

        private void btn_GetRects_Click(object sender, RoutedEventArgs e)
        {
            List<Rect> _Rects = GlobalData.GetInstance().VM_MainWindowViewModel.GetCurrentRect();
        }

        #region 回归UI线程操作
        internal object RunAction(Func<object> func)
        {
            return this.Dispatcher.Invoke<object>(() =>
             {
                 return func();
             });
        }
        internal string PluginRunFunc(Func<string, string, string, string> func, string pluginName, string action, string jsonParam)
        {
            return this.Dispatcher.Invoke<string>(() =>
            {
                return func(pluginName, action, jsonParam);
            });
        }
        #endregion


        /// <summary>
        /// 设置窗体是否最小化的状态
        /// </summary>
        /// <param name="bHideWindow"></param>
        private void SetWindowState(bool bHideWindow)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (bHideWindow)
                {
                    var story = (Storyboard)this.Resources["HideWindow"];
                    if (story != null)
                    {
                        story.Completed += delegate
                        {
                            this.WindowState = WindowState.Minimized;
                            //IsSetWndHide = true;
                            Pop_PageCapture.IsOpen = false;
                            Pop_PageCapture.Visibility = Visibility.Collapsed;
                            border_CaptureTurnPageBorder.Visibility = Visibility.Hidden;
                        };
                        story.Begin(this);
                    }
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    //IsSetWndHide = false;
                    Pop_PageCapture.Visibility = Visibility.Visible;
                    border_CaptureTurnPageBorder.Visibility = Visibility.Visible;
                }
            }));
        }

        /// <summary>
        /// 设置窗体状态
        /// </summary>
        /// <param name="_MyWindowState"></param>
        public void SetWndPositionAndSizeState(MyWindowState _MyWindowState)
        {
            this.Dispatcher.Invoke(() =>
            {
                switch (_MyWindowState)
                {
                    case MyWindowState.InitState:
                        WindowState = WindowState.Normal;
                        this.Width = 560;
                        this.Height = 388;

                        Rect rc = SystemParameters.WorkArea;//获取工作区大小
                        double DWidth = rc.Width;
                        double DHeigth = rc.Height / 3;
                        Thread.Sleep(100);
                        this.Top = DHeigth;
                        this.Left = DWidth - this.ActualWidth;

                        tooltip_MaxSize.Content = "最大化".I18n("DataCollectWnd.xaml_C242808");
                        SetWndPositionImgState(0);
                        break;

                    case MyWindowState.ButtomPanelState:
                        WindowState = WindowState.Normal;
                        Rect rc2 = SystemParameters.WorkArea;//获取工作区大小
                        double scale = (double)NativeMethods.DpiX / 96;
                        double DWidth2 = rc2.Width;
                        double DHeigth2 = rc2.Height / 3;

                        this.Width = DWidth2;
                        this.Height = DHeigth2;
                        Thread.Sleep(100);
                        double DTop = rc2.Bottom - this.ActualHeight;
                        this.Top = DTop;
                        this.Left = 0;

                        tooltip_MaxSize.Content = "最大化".I18n("DataCollectWnd.xaml_C242808");
                        SetWndPositionImgState(1);
                        break;

                    case MyWindowState.MaxiState:

                        CPreRectData.Width = this.ActualWidth;
                        CPreRectData.Height = this.ActualHeight;
                        CPreRectData.Left = this.Left;
                        CPreRectData.Top = this.Top;


                        WindowState = WindowState.Normal;
                        Rect CurrentWorkAreaRect = SystemParameters.WorkArea;//获取工作区大小
                        this.Left = 0;
                        this.Top = 0;
                        Thread.Sleep(50);
                        this.Width = CurrentWorkAreaRect.Width;
                        this.Height = CurrentWorkAreaRect.Height;

                        tooltip_MaxSize.Content = "还原".I18n("DataCollectWnd.xaml_D603419E");
                        SetWndPositionImgState(2);
                        break;

                    case MyWindowState.NormalState:
                        WindowState = WindowState.Normal;

                        this.Width = CPreRectData.Width;
                        this.Height = CPreRectData.Height;
                        Thread.Sleep(100);
                        this.Left = CPreRectData.Left;
                        this.Top = CPreRectData.Top;
                        SetWndPositionImgState(2);
                        tooltip_MaxSize.Content = "最大化".I18n("DataCollectWnd.xaml_C242808");
                        break;

                    default:
                        SetWndPositionImgState(2);
                        tooltip_MaxSize.Content = "最大化".I18n("DataCollectWnd.xaml_C242808");
                        break;
                }
            });
        }

        /// <summary>
        /// 更改窗体状态 最大化时 记录当前窗体状态
        /// </summary>
        PreRectData CPreRectData = new PreRectData();

        /// <summary>
        /// 音仓窗体时 记录当前窗体状态
        /// </summary>
        PreRectData CHideWndRectData = new PreRectData();

        /// <summary>
        /// 1.初始态 2.下悬浮 3.其他
        /// </summary>
        /// <param name="iState"></param>
        public void SetWndPositionImgState(int iState)
        {
            if (iState == 0)
            {
                //img_NormalStateImg.Source = new BitmapImage(new Uri("../Assets/Images/normalWndB.png", UriKind.Relative));
                //img_BottonPanelImg.Source = new BitmapImage(new Uri("../Assets/Images/popButtom.png", UriKind.Relative));
                img_NormalStateImg.Source = new Uri("../Assets/SvgIcon/WindowInitActive.svg", UriKind.Relative);
                img_BottonPanelImg.Source = new Uri("../Assets/SvgIcon/WindowButtom.svg", UriKind.Relative);
            }
            else if (iState == 1)
            {
                //img_NormalStateImg.Source = new BitmapImage(new Uri("../Assets/Images/normalWnd.png", UriKind.Relative));
                //img_BottonPanelImg.Source = new BitmapImage(new Uri("../Assets/Images/popButtomB.png", UriKind.Relative));
                img_NormalStateImg.Source = new Uri("../Assets/SvgIcon/WindowInit.svg", UriKind.Relative);
                img_BottonPanelImg.Source = new Uri("../Assets/SvgIcon/WindowButtomActive.svg", UriKind.Relative);
            }
            else
            {
                //img_NormalStateImg.Source = new BitmapImage(new Uri("../Assets/Images/normalWnd.png", UriKind.Relative));
                //img_BottonPanelImg.Source = new BitmapImage(new Uri("../Assets/Images/popButtom.png", UriKind.Relative));
                img_NormalStateImg.Source = new Uri("../Assets/SvgIcon/WindowInit.svg", UriKind.Relative);
                img_BottonPanelImg.Source = new Uri("../Assets/SvgIcon/WindowButtom.svg", UriKind.Relative);
            }
        }

        public void SetTurnPageImage(string Img_Path)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(Img_Path) || !File.Exists(Img_Path))
                {
                    this.img_CapturedButtonImage.Source =
                    new BitmapImage(new Uri("../Assets/Images/TurnPageDefultImg.png", UriKind.RelativeOrAbsolute));
                    return;//设置默认图标
                }

                //修改拾取翻页结果的翻页元素图片
                this.img_CapturedButtonImage.Source = new BitmapImage(
                    new Uri(Img_Path, UriKind.RelativeOrAbsolute));
            });
        }

        #region 报错
        ErrorMessageWnd ErrorMessageWnd = null;
        /// <summary>
        /// 报错
        /// </summary>
        /// <param name="strMessage"></param>
        /// <returns></returns>
        public void ShowError(string strMessage)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (ErrorMessageWnd == null || ErrorMessageWnd.IsActive == false)
                {
                    ErrorMessageWnd = new ErrorMessageWnd();
                    ErrorMessageWnd.ResetErrorMessage(strMessage);
                    ErrorMessageWnd.Show();
                    ErrorMessageWnd.Top = 60;
                }
                else
                {
                    ErrorMessageWnd.ResetErrorMessage(strMessage);
                }
            });
        }

        public void Refresh()
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    this.StaffAdminView.Items.Refresh();
                    this.dg_CaptureTable.Items.Refresh();
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "刷新列表元素异常");
                }
            });
        }

        public void ResetPosition()
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(500);
                this.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        double ScreenWith = SystemParameters.PrimaryScreenWidth;
                        double WndWidth = ErrorMessageWnd.ActualWidth;
                        double LeftPostion = (ScreenWith - WndWidth) / 2;
                        this.Left = LeftPostion;
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "更换位置异常");
                    }
                });
            });
        }



        #endregion


        /// <summary>
        /// 设置列宽模式 SetDataGridColState
        /// </summary>
        /// <param name="_DataGridState"></param>
        /// <param name="ColOrCommon"></param>
        #region 设置列宽模式
        public void SetDataGridColState(DataGridState _DataGridState, int ColOrCommon)
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (ColOrCommon == 0)//列表格
                    {
                        if (_DataGridState == DataGridState.EvenlyDivided)//均分
                        {
                            DataGridLength length = new DataGridLength(1, DataGridLengthUnitType.Star);
                            StaffAdminView.ColumnWidth = length;
                            StaffAdminView.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                        }
                        else
                        {
                            StaffAdminView.ColumnWidth = new DataGridLength(100);
                            StaffAdminView.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                        }
                    }
                    else if (ColOrCommon == 1)//普通表格
                    {
                        if (_DataGridState == DataGridState.EvenlyDivided)//均分
                        {
                            DataGridLength length = new DataGridLength(1, DataGridLengthUnitType.Star);
                            dg_CaptureTable.ColumnWidth = length;
                            dg_CaptureTable.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                        }
                        else
                        {
                            dg_CaptureTable.ColumnWidth = new DataGridLength(100);
                            dg_CaptureTable.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "设置列宽异常");
                }
            });
        }
        #endregion

        public void ScrollToEnd()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (StaffAdminView.Items.Count > 0)
                {
                    Thread.Sleep(100);
                    var border = VisualTreeHelper.GetChild(StaffAdminView, 0) as Decorator;
                    if (border != null)
                    {
                        var scroll = border.Child as ScrollViewer;

                        if (scroll != null)
                        {
                            Thread.Sleep(200);
                            //scroll.ScrollToEnd();//最后一行
                            //scroll.ScrollToHome();//第一行
                            //scroll.ScrollToLeftEnd();//第一列
                            scroll.ScrollToRightEnd();//最后一列
                            logger.LogDebug("滚动至最后一列");
                        }
                        else
                        {
                            logger.LogDebug("滚动失败 scroll为空");
                        }
                    }
                    else
                    {
                        logger.LogDebug("滚动失败 Decorator为空");
                    }
                }
                else
                {
                    logger.LogDebug("滚动失败 StaffAdminView无数据");
                }
            });
        }

        /// <summary>
        /// 设置最大化按钮的图标
        /// </summary>
        /// <param name="iImgType">0：最大化图标； 1：还原图标</param>
        public void SetMaxSizeButtonImage(int iImgType)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (iImgType == 0)
                {
                    this.img_MaxIcon.Source = new Uri("../Assets/SvgIcon/WindowMaxize.svg", UriKind.Relative);
                }
                else
                {
                    this.img_MaxIcon.Source = new Uri("../Assets/SvgIcon/WindowNormal.svg", UriKind.Relative);
                }
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StaffAdminView_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            try
            {
                string columnName = e.PropertyName;
                if (e.Column is DataGridBoundColumn)
                {
                    DataGridBoundColumn dataGridBoundColumn = e.Column as DataGridBoundColumn;
                    dataGridBoundColumn.Binding = new System.Windows.Data.Binding("[" + e.PropertyName + "]");
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "列名引起异常");
            }
        }

        private void dg_CaptureTable_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            try
            {
                string columnName = e.PropertyName;
                if (e.Column is DataGridBoundColumn)
                {
                    DataGridBoundColumn dataGridBoundColumn = e.Column as DataGridBoundColumn;
                    dataGridBoundColumn.Binding = new System.Windows.Data.Binding("[" + e.PropertyName + "]");
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "列名引起异常");
            }
        }

        private void ErrorMessage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //设置border最大宽度
            var labelObj = sender as System.Windows.Controls.Label;
            double LabelWidth = 0;
            if (labelObj != null)
            {
                LabelWidth = labelObj.ActualWidth;
            }
            logger.LogInformation($"获取当前Label的控件宽度{labelObj.ActualWidth}");
            GlobalData.GetInstance().VM_MainWindowViewModel.SetErrorBoxWidth(LabelWidth + 80);
        }

        private void btnMore_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("获取点击事件".I18n("DataCollectWnd.xaml_7FD26C85"));
        }
    }


    public class DataGridDisplayState
    {
        public bool HasInit = false;
        public DataGridState CDataGridState = DataGridState.EvenlyDivided;
        public bool isEnChanged = false;
        public int CurrentCount = 0;
        public bool isReload = false;
    }

    public enum DataGridState : int
    {
        EvenlyDivided = 0,
        MinWidth = 1
    }

    public class PreRectData
    {
        public double Left = 0;
        public double Top = 0;
        public double Width = 0;
        public double Height = 0;
    }
    public enum MyWindowState : int
    {
        InitState = 0,
        ButtomPanelState = 1,
        MaxiState = 2,
        NormalState = 3
    }
}


