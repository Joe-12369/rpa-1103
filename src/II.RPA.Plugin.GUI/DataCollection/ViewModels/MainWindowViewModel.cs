using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.DataCollection.Utils;
using II.RPA.Plugin.GUI.DataCollection.Windows;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;
using System.Windows.Controls;

namespace II.RPA.Plugin.GUI.DataCollection.ViewModels
{
    public class MainWindowViewModel : ModelBase
    {
        #region 公共变量
        private static ILogger logger = Logger.OfContext();

        /// <summary>
        /// 最终返回值状态判断
        /// </summary>
        public bool IsDataValid { get; set; } = false;

        public DataBuriedModel_DataCollect CDataBuriedModel_DataCollect = new DataBuriedModel_DataCollect();
        #endregion

        public MainWindowViewModel()
        {
            InitWndStateCommand = new DelegateCommand(InitWndStateCommandExcute);
            ButtomWndStateCommand = new DelegateCommand(ButtomWndStateCommandExcute);
            MaxOrNormalWndStateCommand = new DelegateCommand(MaxOrNormalWndStateCommandExcute);
            HelperCommand = new DelegateCommand(HelperCommandExcute);
        }

        #region 私有变量
        /// <summary>
        /// 列表格展示类型
        /// </summary>
        DataGridDisplayState CDataGridDisplayStateForColTable = new DataGridDisplayState();

        /// <summary>
        /// 普通表格展示类型
        /// </summary>
        DataGridDisplayState CDataGridDisplayStateForCommonTable = new DataGridDisplayState();

        /// <summary>
        /// 当前数据为列表格还是普通表格
        /// </summary>
        public bool IsTable { get; set; } = false;
        #endregion

        /// <summary>
        /// 子数据模型
        /// </summary>
        #region 数据模型 ViewModel
        private DataGridViewModel m_DataGridModel = new DataGridViewModel();
        /// <summary>
        /// 数据表格操作模型
        /// </summary>
        public DataGridViewModel DataGridModel
        {
            get { return this.m_DataGridModel; }
            set
            {
                this.m_DataGridModel = value;
                this.RaisePropertyChanged("DataGridModel");
            }
        }

        private CapturePageViewModel m_CapturePageModel = new CapturePageViewModel();
        /// <summary>
        /// 翻页拾取模型
        /// </summary>
        public CapturePageViewModel CapturePageModel
        {
            get { return this.m_CapturePageModel; }
            set
            {
                this.m_CapturePageModel = value;
                this.RaisePropertyChanged("CapturePageModel");
            }
        }


        #endregion

        #region 绑定属性
        private Visibility _LoadingLogoVisibility = Visibility.Hidden;
        /// <summary>
        /// 加载效果显隐属性
        /// </summary>
        public Visibility LoadingLogoVisibility
        {
            get { return this._LoadingLogoVisibility; }
            set
            {
                this._LoadingLogoVisibility = value;
                this.RaisePropertyChanged("LoadingLogoVisibility");
            }
        }

        private Visibility overlayVisibility = Visibility.Hidden;
        /// <summary>
        /// 蒙层效果的显隐
        /// </summary>
        public Visibility OverlayVisibility
        {
            get { return this.overlayVisibility; }
            set
            {
                this.overlayVisibility = value;
                this.RaisePropertyChanged("OverlayVisibility");
            }
        }


        private Visibility _ShowWeakErrorVisibility = Visibility.Hidden;
        /// <summary>
        /// 弱报警显隐属性
        /// </summary>
        public Visibility ShowWeakErrorVisibility
        {
            get { return this._ShowWeakErrorVisibility; }
            set
            {
                this._ShowWeakErrorVisibility = value;
                this.RaisePropertyChanged("ShowWeakErrorVisibility");
            }
        }

        private double _ShowWeakErrorMaxWidth = 100;
        /// <summary>
        /// 弱报警最大宽度
        /// </summary>
        public double ShowWeakErrorMaxWidth
        {
            get { return this._ShowWeakErrorMaxWidth; }
            set
            {
                this._ShowWeakErrorMaxWidth = value;
                this.RaisePropertyChanged("ShowWeakErrorMaxWidth");
            }
        }

        private string _ShowWeakErrorContent = string.Empty;
        /// <summary>
        /// 弱报警文字信息
        /// </summary>
        public string ShowWeakErrorContent
        {
            get { return this._ShowWeakErrorContent; }
            set
            {
                this._ShowWeakErrorContent = value;
                this.RaisePropertyChanged("ShowWeakErrorContent");
            }
        }


        private double _WeakErrorPanelWidth = 0;
        /// <summary>
        /// 
        /// </summary>
        public double WeakErrorPanelWidth
        {
            get { return this._WeakErrorPanelWidth; }
            set
            {
                this._WeakErrorPanelWidth = value;
                //logger.LogInformation("二期 修改宽度");
                ShowWeakErrorMaxWidth = this._WeakErrorPanelWidth + 60;
                this.RaisePropertyChanged("WeakErrorPanelWidth");
            }
        }

        private ImageSource _NormalStateImg = new BitmapImage(new Uri("../DataCollection/Assets/Images/normalWndB.png", UriKind.Relative));
        /// <summary>
        /// 初始化图标绑定源
        /// </summary>
        public ImageSource NormalStateImg
        {
            get { return _NormalStateImg; }
            set
            {
                _NormalStateImg = value;
                this.RaisePropertyChanged("NormalStateImg");
            }
        }

        private int _CurrentMaxLineCount = 0;
        /// <summary>
        /// 展示当前表格数据最大行数
        /// </summary>
        public int CurrentMaxLineCount
        {
            get { return this._CurrentMaxLineCount; }
            set
            {
                this._CurrentMaxLineCount = value;
                this.RaisePropertyChanged("CurrentMaxLineCount");
            }
        }

        public void GetMaxLineCount(int newValue)
        {
            CurrentMaxLineCount = Math.Max(CurrentMaxLineCount, newValue);
        }

        public void SetMaxLineCount(int newValue)
        {
            CurrentMaxLineCount = newValue;
        }

        public void InitialMaxLineCount()
        {
            CurrentMaxLineCount = 0;
        }
        #endregion

        /// <summary>
        /// 报警 ShowDialog 待优化
        /// </summary>
        #region 报警
        /// <summary>
        /// 异步报警
        /// </summary>
        public bool ShowMessage(string strMessage, string btnConfirmContent = "")
        {
            if (string.IsNullOrEmpty(btnConfirmContent))
            {
                btnConfirmContent = eCommonMessageType.CVScreenWnd_Designer_cs_QueDing.GetMessage();
            }
            bool bRet = false;
            WarningWnd.ShowWarningMessage(strMessage, (s, e) =>
            {
                if (e.Result.IsYes)
                {
                    bRet = true;
                }
                else
                {
                    //选择了No
                    bRet = false;
                }
            }, btnConfirmContent);
            return bRet;
        }

        public bool ShowDialog(string strMessage, string btnConfirmContent = "", Window parentWnd = null)
        {
            if (string.IsNullOrEmpty(btnConfirmContent))
            {
                btnConfirmContent = eCommonMessageType.CVScreenWnd_Designer_cs_QueDing.GetMessage();
            }
            //var _Result = WarningWnd.ShowWarningMessageDialog(strMessage, btnConfirmContent);
            var _Result = ShowWarningMessageDialog(strMessage, btnConfirmContent, parentWnd);

            if (_Result == null)
            {
                return false;
            }

            if (_Result.IsYes)
            {
                //选择了Yes
                return true;
            }
            else
            {
                //选择了No
                return false;
            }
        }
        public MessageResult ShowWarningMessageDialog(string strContent, string btnConfirmContent = "", Window parentWnd = null)
        {
            if (string.IsNullOrEmpty(btnConfirmContent))
            {
                btnConfirmContent = eCommonMessageType.CVScreenWnd_Designer_cs_QueDing.GetMessage();
            }

            MessageResult _MessageResult = null;
            //WarningWnd.Dispatcher.Invoke(() =>
            //{
            WarningWnd WarningWnd = new WarningWnd();
            WarningWnd.Context = strContent;
            WarningWnd.ConfirmContent = btnConfirmContent;
            if (parentWnd != null)
            {
                MessageHelper.ShowShadow(true);
                WarningWnd.Owner = parentWnd;
                if (parentWnd is DataCollectWnd)
                {
                    ((DataCollectWnd)parentWnd).ChildWindowCloseAction += WarningWnd.CloseWindow;
                }
            }

            WarningWnd.Result += (s, e) =>
            {
                _MessageResult = e.Result;
            };
            WarningWnd.ShowDialog();
            if (parentWnd is DataCollectWnd)
            {
                ((DataCollectWnd)parentWnd).ChildWindowCloseAction -= WarningWnd.CloseWindow;
            }
            MessageHelper.ShowShadow(false);
            //});
            return _MessageResult;
        }
        #endregion

        #region 命令
        /// <summary>
        /// 取消
        /// </summary>
        public DelegateCommand CancelCommand { get; set; }

        /// <summary>
        /// 取消
        /// </summary>
        public DelegateCommand ConfirmCommand { get; set; }

        /// <summary>
        /// 取消
        /// </summary>
        public DelegateCommand InitWndStateCommand { get; set; }

        /// <summary>
        /// 取消
        /// </summary>
        public DelegateCommand ButtomWndStateCommand { get; set; }

        /// <summary>
        /// 取消
        /// </summary>
        public DelegateCommand MaxOrNormalWndStateCommand { get; set; }


        /// <summary>
        /// 新数据纠错 帮助
        /// </summary>
        public DelegateCommand HelperCommand { get; set; }
        #endregion

        #region 命令实现

        public int SetCurrentColWidth(int ColCount, int DataGridType)
        {
            if (GetCurrentDataGridWidth(DataGridType) > (double)(ColCount * 100))
            {
                SetDataGridColState(DataGridState.EvenlyDivided, DataGridType);
                return 0;
            }
            else
            {
                SetDataGridColState(DataGridState.MinWidth, DataGridType);
                return 1;
            }
        }


        /// <summary>
        ///  初始化窗体位置
        /// </summary>
        /// <param name="objectValue"></param>
        #region 初始化窗体位置
        public void InitWndStateCommandExcute(object objectValue)
        {
            try
            {
                int ICountForColTable = this.DataGridModel.RealDataTable.GetCurrentColCount();
                int ICountForCommonTable = this.DataGridModel.RealDataTableForPureTable.GetCurrentColCount();

                if (ICountForColTable > 5)
                {
                    SetDataGridColState(DataGridState.MinWidth, 0);
                }
                else
                {
                    SetDataGridColState(DataGridState.EvenlyDivided, 0);
                }

                if (ICountForCommonTable > 5)
                {
                    SetDataGridColState(DataGridState.MinWidth, 1);
                }
                else
                {
                    SetDataGridColState(DataGridState.EvenlyDivided, 1);
                }

                SetMaxSizeButtonImage(0);

                SetWndPositionAndSizeState(MyWindowState.InitState);
                this.DataGridModel.RefreshDataGrid(0);
                this.DataGridModel.RefreshDataGrid(1);

            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "初始化窗体位置异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_SheZhiChuangTiWeiZhiYiChang.GetMessage());
            }
        }
        #endregion

        /// <summary>
        ///  下窗体位置
        /// </summary>
        /// <param name="objectValue"></param>
        public void ButtomWndStateCommandExcute(object objectValue)
        {
            try
            {
                SetMaxSizeButtonImage(0);
                SetWndPositionAndSizeState(MyWindowState.ButtomPanelState);

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(250);
                    try
                    {
                        SetCurrentColWidth(this.DataGridModel.RealDataTable.GetCurrentColCount(), 0);
                        SetCurrentColWidth(this.DataGridModel.RealDataTableForPureTable.GetCurrentColCount(), 1);
                        Thread.Sleep(150);
                        this.DataGridModel.RefreshDataGrid(0);
                        this.DataGridModel.RefreshDataGrid(1);
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "下拉窗体位置异常");
                        MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_SheZhiChuangTiWeiZhiYiChang.GetMessage());
                    }
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "下拉窗体位置异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_SheZhiChuangTiWeiZhiYiChang.GetMessage());
            }
        }

        /// <summary>
        ///  最大化Or还原窗体
        /// </summary>
        /// <param name="objectValue"></param>
        #region 最大化Or还原窗体
        public void MaxOrNormalWndStateCommandExcute(object objectValue)
        {
            try
            {
                if (CurrentWndPositionState == MyWindowState.MaxiState)
                {
                    SetMaxSizeButtonImage(0);
                    SetWndPositionAndSizeState(MyWindowState.NormalState);

                    Task.Factory.StartNew(() =>
                    {
                        Thread.Sleep(250);
                        try
                        {
                            SetCurrentColWidth(this.DataGridModel.RealDataTable.GetCurrentColCount(), 0);
                            SetCurrentColWidth(this.DataGridModel.RealDataTableForPureTable.GetCurrentColCount(), 1);
                            Thread.Sleep(150);
                            this.DataGridModel.RefreshDataGrid(0);
                            this.DataGridModel.RefreshDataGrid(1);
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "放大缩小窗体位置异常");
                            MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_SheZhiChuangTiWeiZhiYiChang.GetMessage());
                        }
                    });
                }
                else
                {
                    SetMaxSizeButtonImage(1);
                    SetWndPositionAndSizeState(MyWindowState.MaxiState);

                    Task.Factory.StartNew(() =>
                    {
                        Thread.Sleep(250);
                        try
                        {
                            SetCurrentColWidth(this.DataGridModel.RealDataTable.GetCurrentColCount(), 0);
                            SetCurrentColWidth(this.DataGridModel.RealDataTableForPureTable.GetCurrentColCount(), 1);
                            Thread.Sleep(150);
                            this.DataGridModel.RefreshDataGrid(0);
                            this.DataGridModel.RefreshDataGrid(1);
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "放大缩小窗体位置异常");
                            MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_SheZhiChuangTiWeiZhiYiChang.GetMessage());
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "放大缩小窗体位置异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_SheZhiChuangTiWeiZhiYiChang.GetMessage());
            }
        }
        #endregion




        /// <summary>
        /// 设置表格列宽模式
        /// </summary>
        /// <param name="CurrentColCount"></param>
        /// <param name="ColTableOrCommonTable"></param>
        #region 设置表格列宽模式

        bool NeedScroll = false;

        public void SetColumnWidth(int CurrentColCount, int ColTableOrCommonTable)
        {
            if (ColTableOrCommonTable == 0)
            {
                if (CDataGridDisplayStateForColTable.isEnChanged)
                {
                    return;
                }

                lock (CDataGridDisplayStateForColTable)
                {
                    try
                    {
                        CDataGridDisplayStateForColTable.isEnChanged = true;

                        if (!CDataGridDisplayStateForColTable.HasInit)
                        {
                            SetCurrentColWidth(CurrentColCount, 0);
                            CDataGridDisplayStateForColTable.CurrentCount = CurrentColCount;
                            CDataGridDisplayStateForColTable.HasInit = true;
                        }
                        else
                        {
                            if (SetCurrentColWidth(CurrentColCount, 0) == 1)
                            {
                                if (CDataGridDisplayStateForColTable.CurrentCount - CurrentColCount < 0)//当前是增加列操作
                                {
                                    ////判断是否是加载
                                    //if (CDataGridDisplayStateForColTable.CurrentCount == 0 && StaffAdminView.Columns.Count > 1)
                                    //{
                                    //    CDataGridDisplayStateForColTable.isReload = true;
                                    //}

                                    NeedScroll = true;
                                }
                            }

                            CDataGridDisplayStateForColTable.CurrentCount = CurrentColCount;
                        }
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "更新ColTable异常");
                    }
                    finally
                    {
                        CDataGridDisplayStateForColTable.isEnChanged = false;
                        RefreshDataGridItems();
                    }

                }
            }
            else
            {
                if (CDataGridDisplayStateForCommonTable.isEnChanged)
                {
                    return;
                }

                lock (CDataGridDisplayStateForCommonTable)
                {
                    try
                    {
                        CDataGridDisplayStateForCommonTable.isEnChanged = true;

                        SetCurrentColWidth(CurrentColCount, 1);

                        CDataGridDisplayStateForCommonTable.CurrentCount = CurrentColCount;
                        CDataGridDisplayStateForCommonTable.HasInit = true;

                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"更新CommonTable异常");
                    }
                    finally
                    {
                        CDataGridDisplayStateForCommonTable.isEnChanged = false;
                        RefreshDataGridItems();
                    }
                }
            }


        }

        #endregion


        /// <summary>
        /// 展示流程帮助pdf
        /// </summary>
        public void HelperCommandExcute(object objValue)
        {
            string HtmlAbsolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HelpInfo.html");
            Process.Start(HtmlAbsolutePath);

            //数据埋点
            GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.help_connect++;
        }
        #endregion


        /// <summary>
        /// 开始拾取元素 获取Selector 入参 每次主动开始拾取均会更新
        /// </summary>
        public ParamStartCollectElementCaptureModel CParamStartCollectElementCaptureModel = new ParamStartCollectElementCaptureModel();


        /// <summary>
        /// 所有需要保持的数据 也是最终获取结果 （保存拾取获取的各种Selector及其他参数）
        /// </summary>
        public RetDataCollectConfigurationModel DataCollectConfigurationModel { get; private set; }


        /// <summary>
        /// 执行拾取 初始化参数 （根据Selector拾取预览数据）
        /// </summary>
        public ParamDataCollectConfigurationModel ParamDataCollectConfigurationModel { get; private set; }

        internal JMessage CaptureJMessage { get; set; } = new JMessage();

        /// <summary>
        /// 核心功能
        /// </summary>
        #region 核心功能
        /// <summary>
        /// 初始化数据采集 数据模型 并决定是否进入拾取状态
        /// </summary>
        /// <param name="bStartCapture"></param>
        #region 初始化
        public void InitCaptureCollection(ParamDataCollectConfigurationModel _ParamDataCollectConfigurationModel, Func<List<Rect>> GetRectOfMainWindow,
            Action<bool> _SetWindowStateEx, Action<string> _SetTurnPageImage, Action<string> _ShowError, Action _ForceCloseWindow,
            Action<DataGridState, int> _SetDataGridColState, Action<MyWindowState> _SetWndPostionAndSizeState, Action<int> _SetMaxSizeButtonImageEx,
            Action _ScrollToEndAction, Func<int, double> _GetCurrentDataGridWidth, Action _Refresh, Func<int> _GetCurrentWindowState)
        {
            this.ParamDataCollectConfigurationModel = _ParamDataCollectConfigurationModel;
            GetMainWindowCurrentRect = GetRectOfMainWindow;
            SetWindowStateEx = _SetWindowStateEx;
            Action_SetTurnPageImageEx = _SetTurnPageImage;
            _ShowErrorEx = _ShowError;
            _ForceCloseWindowEx = _ForceCloseWindow;
            _SetDataGridColStateEx = _SetDataGridColState;
            _SetWndPostionAndSizeStateEx = _SetWndPostionAndSizeState;
            Action_SetMaxSizeButtonImageEx = _SetMaxSizeButtonImageEx;
            _ScrollToEndEx = _ScrollToEndAction;
            _GetCurrentDataGridWidthEx = _GetCurrentDataGridWidth;
            _RefreshEx = _Refresh;
            _GetCurrentWindowStateEx = _GetCurrentWindowState;

            MessageHelper.SetShowWeakErrorOnPanel(ShowErrorOnPanelEx);

            if (string.IsNullOrEmpty(_ParamDataCollectConfigurationModel.Selector))
            {
                DataCollectConfigurationModel = new RetDataCollectConfigurationModel();
            }
            else
            {
                DataCollectConfigurationModel = _ParamDataCollectConfigurationModel.Selector.ToObject<RetDataCollectConfigurationModel>();
            }

            CParamStartCollectElementCaptureModel = new ParamStartCollectElementCaptureModel()
            {
                PackagePath = ParamDataCollectConfigurationModel?.PackagePath,
                PageUrl = DataCollectConfigurationModel?.pageInfo?.url,
                PageTitle = DataCollectConfigurationModel?.pageInfo?.title,
            };


            DataCollectingManager.StartElementCapture_OnSelectored = DealCaptureResult;
        }
        #endregion

        /// <summary>
        /// 拾取元素
        /// </summary>
        /// <param name="DoCaptureFirst"> 强制重新拾取 </param>
        #region 开始拾取
        public void StartCaptureCollectDataElement(bool DoCaptureFirst = false, CaptureElementType _CaptureElementType = CaptureElementType.DataCollectingElement, List<SelectorProps> _LastCaptureSelector = null)
        {
            //logger.LogDebug("启动拾取");
            //ShowLoadWaiting(false);
            bool isFirstCapture = false;
            if (DoCaptureFirst)
            {
                isFirstCapture = true;
            }
            else
            {
                if (string.IsNullOrEmpty(DataCollectConfigurationModel.pageInfo.title) && string.IsNullOrEmpty(DataCollectConfigurationModel.pageInfo.url))
                {
                    isFirstCapture = true;
                }
                else
                {
                    isFirstCapture = false;
                }
            }

            if (isFirstCapture)
            {
                try
                {
                    CParamStartCollectElementCaptureModel = new ParamStartCollectElementCaptureModel()
                    {
                        PackagePath = ParamDataCollectConfigurationModel.PackagePath,
                        PageUrl = DataCollectConfigurationModel.pageInfo.url,
                        PageTitle = DataCollectConfigurationModel.pageInfo.title,
                        CaptureElementType = _CaptureElementType,
                        IsFirstCapture = isFirstCapture,
                        LastCaptureSelector = _LastCaptureSelector,
                        CollectType = ParamDataCollectConfigurationModel.CollectType
                    };
                    var ret = DataCollectingManager.StartElementCapture(CParamStartCollectElementCaptureModel);
                }
                catch (Exception ex)
                {
                    MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_KaiShiYuanSuShiQuYiChangQing.GetMessage());
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取元素异常");
                }
            }
            else
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        CParamStartCollectElementCaptureModel = new ParamStartCollectElementCaptureModel()
                        {
                            PackagePath = ParamDataCollectConfigurationModel.PackagePath,
                            PageUrl = DataCollectConfigurationModel.pageInfo.url,
                            PageTitle = DataCollectConfigurationModel.pageInfo.title,
                            CaptureElementType = _CaptureElementType,
                            IsFirstCapture = isFirstCapture,
                            LastCaptureSelector = _LastCaptureSelector
                        };
                        var ret = DataCollectingManager.StartElementCapture(CParamStartCollectElementCaptureModel);
                    }
                    catch (Exception ex)
                    {
                        MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_KaiShiYuanSuShiQuYiChangQing.GetMessage());
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取元素异常");
                    }
                });
            }
        }
        #endregion

        /// <summary>
        /// 清空最后返回的配置数据
        /// </summary>
        public void EmptyReturnConfigData()
        {
            this.DataCollectConfigurationModel = new RetDataCollectConfigurationModel();
            //是否需要重新启动拾取？
        }

        /// <summary>
        ///  处理拾取的结果回调
        /// </summary>
        #region 处理拾取的结果回调 
        public void DealCaptureResult(DataCollectOverlayWnd overlayWnd)
        {
            DataCollectingManager.StopCapture();
            RetElementCaptureModel _RetElementCaptureModel = new RetElementCaptureModel();
            try
            {
                if (overlayWnd.CaptureException != null)
                {
                    throw overlayWnd.CaptureException;
                }
                else if (!string.IsNullOrEmpty(overlayWnd.SelectorError))
                {
                    _RetElementCaptureModel.Code = 1;
                    _RetElementCaptureModel.Message = overlayWnd.SelectorError;
                    overlayWnd.SelectorError = null;
                }
                else if (!string.IsNullOrEmpty(overlayWnd.CaptureError))
                {
                    _RetElementCaptureModel.Code = 2;
                    _RetElementCaptureModel.Message = overlayWnd.CaptureError;
                    overlayWnd.CaptureError = null;
                }
                else
                {
                    _RetElementCaptureModel = overlayWnd.RetCaptureModel;
                }

                //页面信息
                if (_RetElementCaptureModel.Code == 0 && string.IsNullOrEmpty(DataCollectConfigurationModel.pageInfo.url))
                {
                    DataCollectConfigurationModel.pageInfo.guiType = _RetElementCaptureModel.GuiType;
                    DataCollectConfigurationModel.pageInfo.url = _RetElementCaptureModel.PageUrl;
                    DataCollectConfigurationModel.pageInfo.title = _RetElementCaptureModel.PageTitle;
                }

                switch (CParamStartCollectElementCaptureModel.CaptureElementType)
                {
                    case CaptureElementType.DataCollectingElement: //普通列元素
                        if (_RetElementCaptureModel.Code != 0)
                        {
                            SetWindowState(false);
                            Task.Factory.StartNew(() =>
                            {
                                try
                                {
                                    StartCaptureCollectDataElement(false);//重新开起拾取 是否直接停止暂未定
                                }
                                catch (Exception ex)
                                {
                                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取元素出现异常");
                                    MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_ShiQuChuXianYiChang.GetMessage());
                                }
                            });
                            ShowLoadWaiting(false, "DealCaptureResult 001");
                            return;
                        }

                        DealCaptureResult_DataCollectingElement(_RetElementCaptureModel);

                        break;
                    case CaptureElementType.SimilarElement: //相似元素
                        if (_RetElementCaptureModel.Code != 0)
                        {
                            SetWindowState(false);

                            Task.Factory.StartNew(() =>
                            {
                                try
                                {
                                    StartCaptureCollectDataElement(false);//重新开起拾取 是否直接停止暂未定
                                }
                                catch (Exception ex)
                                {
                                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取元素出现异常");
                                    MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_ShiQuChuXianYiChang.GetMessage());
                                }
                            });
                            ShowLoadWaiting(false, "DealCaptureResult 001");
                            return;
                        }

                        DealCaptureResult_SimilarElement(_RetElementCaptureModel);
                        break;

                    case CaptureElementType.PagingButtonElement://翻页元素
                        if (_RetElementCaptureModel.Code != 0)
                        {
                            SetWindowState(false);
                            Task.Factory.StartNew(() =>
                            {
                                try
                                {
                                    StartCaptureCollectDataElement(false);//重新开起拾取 是否直接停止暂未定
                                }
                                catch (Exception ex)
                                {
                                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取元素出现异常");
                                    MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_ShiQuChuXianYiChang.GetMessage());
                                }
                            });
                            ShowLoadWaiting(false, "DealCaptureResult 001");
                            return;
                        }
                        DealCaptureResult_PagingButtonElement(_RetElementCaptureModel);
                        break;
                }
            }
            catch (RPAException rpaex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, rpaex, "处理拾取的结果回调存在异常");
                //MessageHelper.ShowWeakError(ex, "翻页元素拾取失败");
                bool _IsFirstCapture = false;
                CaptureElementType _CaptureElementType = CaptureElementType.DataCollectingElement;
                List<SelectorProps> _LastCaptureSelector = null;
                //只抛出插件相关的错误
                if (rpaex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnInstalled
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_ChromeExtensionUnopen
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnInstalled
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_360ExtensionUnopen
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnInstalled
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_EdgeExtensionUnopen
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnInstalled
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_FireFoxExtensionUnopen
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_SogouExtensionUnInstalled
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_SogouExtensionUnopen
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_JavaExtensionUnInstalled
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_ServerHubUnopen
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnInstalled
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_360XExtensionUnopen
                    || rpaex.ErrorMessageType == eErrorMessageType.Error_BrowserPluginVersionTooLow)
                {
                    bool isStopCapture = false;
                    //if (!DataCollectingManager.WebBroswerExtensionPopupStatus.ContainsKey(rpaex.ErrorMessageType))
                    //{
                    isStopCapture = MessageHelper.ShowDialog("浏览器未安装插件，无法采集数据！".I18n("MainWindowViewModel_AF39B611"), "安装插件".I18n("MainWindowViewModel_BBB3F4A7"));
                    //    DataCollectingManager.WebBroswerExtensionPopupStatus[rpaex.ErrorMessageType] = isStopCapture;
                    //}
                    if (isStopCapture)
                    {
                        SetWindowState(false);
                        CaptureJMessage.Code = rpaex.ErrorCode;
                        CaptureJMessage.UnifyCode = rpaex.UnifyErrorCode;
                        CaptureJMessage.Message = rpaex.ErrorMessage;
                        IsDataValid = false;
                        ForceCloseWindow();
                        //关闭主窗口，结束本次配置操作
                        return;
                    }
                    else
                    {
                        SetWindowState(false);
                        StartCaptureCollectDataElement();
                        MessageHelper.ShowWeakError(rpaex, eCommonMessageType.MainWindowViewModel_cs_ShiQuChuXianYiChang.GetMessage());
                        //还原UI效果，并重新开始拾取
                    }
                }
                //拾取了未支持的元素
                else if (rpaex.ErrorMessageType == eErrorMessageType.Error_DataCollection_GUINoSupport)
                {
                    MessageHelper.ShowDialog(rpaex.ErrorMessageContent, eCommonMessageType.CVScreenWnd_Designer_cs_QueDing.GetMessage());
                }
                else if (rpaex.ErrorMessageType == eErrorMessageType.Error_DataCollection_ElementTypeError)
                {
                    MessageHelper.ShowDialog(rpaex.ErrorMessageContent, eCommonMessageType.CVScreenWnd_Designer_cs_QueDing.GetMessage());
                }
                else if (rpaex.ErrorMessageType == eErrorMessageType.Error_DataCollection_ElementNotTableError)
                {
                    MessageHelper.ShowDialog(rpaex.ErrorMessageContent, eCommonMessageType.CVScreenWnd_Designer_cs_QueDing.GetMessage());
                }
                //元素不是同一个类型，可能是链路不对
                else if (rpaex.ErrorMessageType == eErrorMessageType.Error_DataCollection_SimileCaptureDifferentElementTypes)
                {
                    var isOk = MessageHelper.ShowDialog(rpaex.ErrorMessageContent, "继续拾取");
                    if (isOk)
                    {
                        _IsFirstCapture = CParamStartCollectElementCaptureModel.IsFirstCapture;
                        _CaptureElementType = CParamStartCollectElementCaptureModel.CaptureElementType;
                        //拾取相似元素
                        _LastCaptureSelector = CParamStartCollectElementCaptureModel.LastCaptureSelector;
                    }
                }
                else if (rpaex.ErrorMessageType == eErrorMessageType.Error_USERCLOSE)
                {
                    CaptureJMessage.Code = rpaex.ErrorCode;
                    CaptureJMessage.UnifyCode = rpaex.UnifyErrorCode;
                    CaptureJMessage.Message = rpaex.ErrorMessageType.GetMessage();
                    IsDataValid = false;
                    ForceCloseWindow();
                    //关闭主窗口，结束本次配置操作
                    return;
                }
                else
                {
                    ShowLoadWaiting(false, "DealCaptureResult 001");
                    MessageHelper.ShowWeakError(rpaex, eCommonMessageType.MainWindowViewModel_cs_ShiQuShuJuChuLiYiChang.GetMessage());
                }
                SetWindowState(false);
                ShowLoadWaiting(false, "DealCaptureResult 001");
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        StartCaptureCollectDataElement(_IsFirstCapture, _CaptureElementType, _LastCaptureSelector);//重新开起拾取 是否直接停止暂未定
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取元素出现异常");
                        MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_ShiQuChuXianYiChang.GetMessage());
                    }
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取回调抛出异常");
                ShowLoadWaiting(false, "DealCaptureResult 001");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_ShiQuChuXianYiChang.GetMessage());

                SetWindowState(false);

                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        StartCaptureCollectDataElement();
                    }
                    catch (Exception ex2)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex2, "拾取元素出现异常");
                        MessageHelper.ShowWeakError(ex2, eCommonMessageType.MainWindowViewModel_cs_ShiQuChuXianYiChang.GetMessage());
                    }
                });
            }
            finally
            {
                SetWindowState(false);
            }
        }

        #region 处理普通元素拾取返回 
        /// <summary>
        /// 处理拾取返回的数据
        /// </summary>
        /// <param name="_RetElementCaptureModel"></param>
        public void DealCaptureResult_DataCollectingElement(RetElementCaptureModel _RetElementCaptureModel)
        {
            try
            {

                bool _IsTable = _RetElementCaptureModel.IsTable;
                IsTable = _IsTable;
                if (_IsTable)//拾取为普通表格 要清空前面所有拾取
                {
                    DataCollectConfigurationModel.collectInfo.type = "table";
                    DataCollectConfigurationModel.collectInfo.data.Clear();
                    IsTable = true;
                }

                //根据列数生成列名
                //string strColName = ColumnIDHelper.NumberToColumnID(this.DataGridModel.GetCurrentColumnsCount());

                CollectType defCollectType = CollectType.Content;
                var props = _RetElementCaptureModel.ProcessedSelector.LastOrDefault();
                if (props != null && props.Name.ToLower() == "img")
                {
                    defCollectType = CollectType.Src;
                }

                //增加拾取返回的元素配置
                DataCollectConfigurationModel.collectInfo.data.Add(new Commons.Params.DataCollect.DataCollectConfiguration_CollectInfo_Item
                {
                    filePath = _RetElementCaptureModel.ScreenShotPath,
                    title = "",
                    version = GlobleConfigurations.CurrentSelectorVersion,
                    selector = _RetElementCaptureModel.ProcessedSelector,
                    dataType = defCollectType,
                    regular = string.Empty,
                    lastCaptureSelector = _RetElementCaptureModel.OriginalSelector
                });
                if (_RetElementCaptureModel.NextPageBtnSelector != null && _RetElementCaptureModel.NextPageBtnSelector.Count > 0 && CParamStartCollectElementCaptureModel.IsFirstCapture == true)
                {
                    if (DataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelector != null && DataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelector.Count > 0)
                    {
                        //不做处理
                    }
                    else
                    {
                        DataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelector = _RetElementCaptureModel.NextPageBtnSelector;
                        DataCollectConfigurationModel.turnPageButtonInfo.filePath = _RetElementCaptureModel.NextPageBtnScreenShotPath;
                        DataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelectorVersion = GlobleConfigurations.CurrentSelectorVersion;

                    }
                    DataCollectConfigurationModel.turnPageButtonInfo.delay = DataCollectConfigurationModel.turnPageButtonInfo.delay > 0 ? DataCollectConfigurationModel.turnPageButtonInfo.delay : 1000;
                    DataCollectConfigurationModel.turnPageButtonInfo.pageCount = DataCollectConfigurationModel.turnPageButtonInfo.pageCount > 1 ? DataCollectConfigurationModel.turnPageButtonInfo.pageCount : 1;
                    this.CapturePageModel.SetTurnPageButtonInfo(DataCollectConfigurationModel.turnPageButtonInfo);
                    CapturePageModel.ShowCapturedState(DataCollectConfigurationModel.turnPageButtonInfo, CParamStartCollectElementCaptureModel.PackagePath);
                }


                JObject webData = new JObject() { { "type", DataCollectConfigurationModel.pageInfo.guiType } };
                ParamDataCollectConfigurationModel.WebData = webData.ToString();

                if (!IsTable)
                {
                    GlobalData.GetInstance().VM_MainWindowViewModel.ReloadGridData(DataCollectConfigurationModel.collectInfo.data.Count - 1);
                    return;
                }

                var _ProcessedSelector = DataCollectingManager.SelectorValueRemoveStrEscape(new ParamSelectorValueRemoveStrEscapeModel
                {
                    selector = _RetElementCaptureModel.ProcessedSelector.ToObject<List<SelectorProps>>()
                });

                ////拾取预览数据
                var selectorVersion = GlobleConfigurations.CurrentSelectorVersion;//使用最新版本号
                List<List<string>> _CollectData = this.GetDataCollectionBySelector(_ProcessedSelector, selectorVersion, _IsTable, defCollectType);///添加数据类型？

                if (_IsTable)
                {
                    int NewColCount = _CollectData[0].Count;
                    for (int i = 0; i < NewColCount; i++)
                    {
                        DataCollectConfigurationModel.collectInfo.data[0].CommonTalbleTitleList.Add(new ColNameStruct() { ColName = string.IsNullOrWhiteSpace(_CollectData[0][i]) ? ColumnIDHelper.NumberToColumnID(i) : _CollectData[0][i].Trim(), HasSet = 0 });//获取预览数据时 生成默认表头列表
                    }
                }

                //设置字段类型 
                SetFeildType(_CollectData, 0, true);
                this.DataGridModel.DealCaptureBySelectorResult(_CollectData, true, _IsTable);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取数据采集元素绑定数据部分发生异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_ShuJuCaiJiHouZhanShiYuLanShu.GetMessage());
                ShowLoadWaiting(false, "DealCaptureResult_DataCollectingElement 001");
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(500);
                    StartCaptureCollectDataElement(false);
                });
            }

        }
        #endregion

        #region 处理相似元素拾取返回
        public void DealCaptureResult_SimilarElement(RetElementCaptureModel _RetElementCaptureModel)
        {
            try
            {
                //ShowLoadWaiting(false);
                //修改现有selector
                DataCollectConfigurationModel.collectInfo.data[this.DataGridModel.CurrentColumnIndex].version = GlobleConfigurations.CurrentSelectorVersion;
                DataCollectConfigurationModel.collectInfo.data[this.DataGridModel.CurrentColumnIndex].selector = _RetElementCaptureModel.ProcessedSelector;
                DataCollectConfigurationModel.collectInfo.data[this.DataGridModel.CurrentColumnIndex].lastCaptureSelector = _RetElementCaptureModel.OriginalSelector;

                GlobalData.GetInstance().VM_MainWindowViewModel.ReloadGridData(this.DataGridModel.CurrentColumnIndex);
                //CollectType collectType = DataCollectConfigurationModel.collectInfo.data[this.DataGridModel.CurrentColumnIndex].dataType;
                //string regularRegex = DataCollectConfigurationModel.collectInfo.data[this.DataGridModel.CurrentColumnIndex].regular;
                //var _ProcessedSelector = DataCollectingManager.SelectorValueRemoveStrEscape(new ParamSelectorValueRemoveStrEscapeModel
                //{
                //    selector = _RetElementCaptureModel.ProcessedSelector.ToObject<List<SelectorProps>>()
                //});
                ////拾取预览数据
                //List<List<string>> _CollectDataSimilar = this.GetDataCollectionBySelector(_ProcessedSelector, false, collectType, regularRegex);
                //this.DataGridModel.DealCaptureBySelectorResult(_CollectDataSimilar, false, false);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取相似元素绑定数据部分发生异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_ShiQuXiangSiYuanSuBingYuLanShu.GetMessage());
                ShowLoadWaiting(false, "DealCaptureResult_SimilarElement001");
                SetWindowState(false);
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(500);
                    StartCaptureCollectDataElement(false);
                });
            }
            finally
            {


            }
        }
        #endregion

        #region 处理翻页元素拾取返回
        public void DealCaptureResult_PagingButtonElement(RetElementCaptureModel _RetElementCaptureModel)
        {
            try
            {
                SetWindowState(false);
                DataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelectorVersion = GlobleConfigurations.CurrentSelectorVersion;
                DataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelector = _RetElementCaptureModel.NextPageBtnSelector;
                DataCollectConfigurationModel.turnPageButtonInfo.filePath = _RetElementCaptureModel.ScreenShotPath;
                DataCollectConfigurationModel.pageInfo.guiType = _RetElementCaptureModel.GuiType;
                if (string.IsNullOrEmpty(CParamStartCollectElementCaptureModel.PackagePath) || string.IsNullOrEmpty(DataCollectConfigurationModel.turnPageButtonInfo.filePath))
                {
                    this.CapturePageModel.ShowCaptureResult(CaptureTurnPageState.Captured, null, null);
                    GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.page_element_value = 0;//默认
                }
                else
                {
                    this.CapturePageModel.ShowCaptureResult(CaptureTurnPageState.Captured, DataCollectConfigurationModel.turnPageButtonInfo.filePath, CParamStartCollectElementCaptureModel.PackagePath);
                    GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.page_element_value = 1;//自定义
                }
                //显示 元素图片
                //StartStayOpenCountingDown();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取翻页元素发生异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_ShiQuFanYeYuanSuFaShengLeYi.GetMessage());
            }
            finally
            {
                ShowLoadWaiting(false, "DealCaptureResult_PagingButtonElement 001");
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(500);
                    StartCaptureCollectDataElement(false);
                });
            }
        }
        #endregion

        #endregion


        /// <summary>
        /// 根据selector获取拾取结果 单列数据/或者表格数据 如果是列元素表格则数据为按列返回 如果是普通表格 数据为按行返回
        /// </summary>
        /// <returns></returns>
        #region 根据selector获取拾取结果
        public List<List<string>> GetDataCollectionBySelector(List<Core.Commons.SelectorProps> _ColumnDataSelectors, string selectorVersion, bool _IsTable, CollectType _CollectType = CollectType.Content, string _RegularRegex = "")
        {
            try
            {
                var paramCollectDataBySelector = new Commons.Params.ParamCollectDataBySelectorModel()
                {
                    WedData = ParamDataCollectConfigurationModel.WebData,
                    NextPageBtnSelectorVersion = selectorVersion,//拾取配置时 翻页逻辑是不处理的，所以这个值不用特殊传递进来
                    NextPageBtnSelector = new List<Core.Commons.SelectorProps>(),
                    Delay = 0,
                    ScrollVertical = 0,
                    PageCount = 1,
                    IsTable = _IsTable
                };

                paramCollectDataBySelector.SelectorDataList.Add(new Commons.Params.CollectData_SelectorData
                {
                    Version = selectorVersion,
                    Selector = _ColumnDataSelectors,
                    CollectType = _CollectType,
                    Regular = _RegularRegex
                });

                List<List<string>> _CollectData = DataCollectingManager.CollectDataBySelector(paramCollectDataBySelector, "");//.ColumnsDataToTwoDimensionalArray();
                return _CollectData;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "根据selector获取拾取结果失败");
                MessageHelper.ShowWeakError(eCommonMessageType.MainWindowViewModel_cs_ShuJuHuoQuYiChangKeBianJi.GetMessage());
                return new List<List<string>>();
            }
        }

        /// <summary>
        /// 根据Selector获取表格预览数据集合 多列数据/或者表格数据
        /// </summary>
        /// <param name="_DataCollectConfiguration_CollectInfo"></param>
        /// <param name="_IsTable"></param>
        /// <returns></returns>
        public List<List<string>> GetDataCollectionBySelector(List<DataCollectConfiguration_CollectInfo_Item> _DataCollectConfiguration_CollectInfo_List, bool _IsTable)
        {
            try
            {
                var paramCollectDataBySelector = new Commons.Params.ParamCollectDataBySelectorModel()
                {
                    WedData = ParamDataCollectConfigurationModel.WebData,
                    NextPageBtnSelectorVersion = GlobleConfigurations.CurrentSelectorVersion,
                    NextPageBtnSelector = new List<Core.Commons.SelectorProps>(),
                    Delay = 0,
                    ScrollVertical = 0,
                    PageCount = 1,
                    IsTable = _IsTable
                };

                foreach (var item in _DataCollectConfiguration_CollectInfo_List)
                {
                    paramCollectDataBySelector.SelectorDataList.Add(new Commons.Params.CollectData_SelectorData
                    {
                        Version = item.version,
                        Selector = item.selector,
                        CollectType = item.dataType,
                        Regular = item.regular
                    });
                }

                List<List<string>> _CollectData = DataCollectingManager.CollectDataBySelector(paramCollectDataBySelector, "");//.ColumnsDataToTwoDimensionalArray();
                return _CollectData;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "根据selector获取拾取结果失败");
                MessageHelper.ShowWeakError(eCommonMessageType.MainWindowViewModel_cs_ShuJuHuoQuYiChangKeBianJi.GetMessage());
                return new List<List<string>>();
            }
        }
        #endregion

        /// <summary>
        /// 判断是否为二次配置 是的话爬取加载预览数据
        /// </summary>
        #region 数据回填
        public void LoadPreviewConfigData()
        {

            try
            {
                ///翻页元素 反填
                this.CapturePageModel.ShowPreviewConfig(DataCollectConfigurationModel.turnPageButtonInfo, CParamStartCollectElementCaptureModel.PackagePath);
                if (DataCollectConfigurationModel?.collectInfo?.data?.Count == 0)
                {
                    ///无列数据配置信息  -----翻页元素后面考虑
                    StartCaptureCollectDataElement();
                    return;
                }
                ReloadGridData();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "反填数据异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.MainWindowViewModel_cs_JiaZaiYuLanShuJuYiChang.GetMessage());
                ShowLoadWaiting(false, "LoadPreviewConfigData 001");
            }
            finally
            {
            }
        }
        #endregion

        /// <summary>
        /// 重新加载数据 二次加载数据 根据selector填充表格数据
        /// </summary>
        #region 重新加载数据
        public void ReloadGridData(int updateDataIdx = -1)
        {
            //logger.LogDebug($"蒙层 重新加载");
            SetWindowState(false);
            if (isEnResfreshData)
            {
                ShowLoadWaiting(false, "ReloadGridData 000");
                StartCaptureCollectDataElement();
                return;
            }

            ShowLoadWaiting(true, "ReloadGridData 001");
            isEnResfreshData = true;
            //InitialMaxLineCount();

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(200);
                RetDataCollectConfigurationModel compiledSRetModel = null;
                bool _DoStringCompileSuccess = true;

                //编译数据中包含的变量
                try
                {
                    try
                    {
                        //编译可能发生异常
                        compiledSRetModel = DataCollectingManager.DoStringCompile(new ParamDoStringCompileModel
                        {
                            Text = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.ToJson(),
                            ParamsStr = GlobalData.GetInstance().VM_MainWindowViewModel.ParamDataCollectConfigurationModel.GlobalVariable
                        });
                    }
                    catch (RPAException ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "获取数据前编译selector异常，走显示默认标题逻辑");
                        _DoStringCompileSuccess = false;
                        compiledSRetModel = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel;
                    }
                    if (string.IsNullOrEmpty(ParamDataCollectConfigurationModel.WebData))
                    {
                        JObject webData = new JObject() { { "type", compiledSRetModel.pageInfo.guiType } };
                        ParamDataCollectConfigurationModel.WebData = webData.ToString();
                    }
                    else
                    {
                        JObject webData = JObject.Parse(ParamDataCollectConfigurationModel.WebData);
                        if (string.IsNullOrEmpty(webData["type"]?.ToString()))
                        {
                            webData["type"] = compiledSRetModel.pageInfo.guiType;
                            ParamDataCollectConfigurationModel.WebData = webData.ToString();
                        }
                    }
                    if (DataCollectConfigurationModel.collectInfo.type == "tag")//tag? 列元素
                    {
                        IsTable = false;
                        //列数据集合  列名，列内容-----表格的话是纯二维数据
                        List<ColumnData> _ColumnDataList = new List<ColumnData>();
                        //是否有列数据为空 有为空的提示固定提示语 ： XXX缺失字段
                        bool bHasEmptyColumn = false;

                        SetCurrentColWidth(compiledSRetModel.collectInfo.data.Count, 0);

                        List<List<string>> _ColContentList = _DoStringCompileSuccess ? this.GetDataCollectionBySelector(compiledSRetModel.collectInfo.data, false) : new List<List<string>>() { new List<string> { } };
                        //设置列的 字段类型
                        SetFeildType(_ColContentList, updateDataIdx, false);

                        InitialMaxLineCount();
                        //this.DataGridModel.EmptyColDataGrid();
                        //this.DataGridModel.EmptyTableDataGrid();

                        if (compiledSRetModel.collectInfo.data.Count != _ColContentList.Count)
                        {
                            MessageHelper.ShowWeakError("获取数据缺失字段".I18n("MainWindowViewModel_7DC7C72B"), true);
                            logger.LogError("传入Selector数量不同于数据列数量");
                            //return;
                        }

                        for (int i = 0; i < compiledSRetModel.collectInfo.data.Count; i++)
                        {
                            var item = compiledSRetModel.collectInfo.data[i];
                            ColumnData _ColumnData = null;

                            if (_ColContentList.Count == 0)
                            {
                                _ColumnData = new ColumnData
                                {
                                    ColName = item.title,
                                    ColContentList = new List<string>()
                                };

                                bHasEmptyColumn = true;
                            }
                            else
                            {
                                _ColumnData = new ColumnData
                                {
                                    ColName = item.title,
                                    ColContentList = _ColContentList[i]
                                };
                                if (_ColContentList[i].Count == 0)
                                {
                                    bHasEmptyColumn = true;
                                }
                                else
                                {
                                    // GetMaxLineCount(_ColContentList[i].Count);
                                }
                            }
                            _ColumnDataList.Add(_ColumnData);
                        }

                        this.DataGridModel.AddNewColumnsToCurrentData(_ColumnDataList, true);

                        if (bHasEmptyColumn)
                        {
                            MessageHelper.ShowWeakError("当前有字段不存在，请确认!".I18n("MainWindowViewModel_A01150B7"), true);
                        }

                    }
                    else if (DataCollectConfigurationModel.collectInfo.type == "table")//table元素
                    {
                        IsTable = true;
                        var item = compiledSRetModel.collectInfo.data[0];

                        List<List<string>> _ColContentList = this.GetDataCollectionBySelector(item.selector, item.version, true, item.dataType);
                        //设置列的 字段类型
                        SetFeildType(_ColContentList, updateDataIdx, true);

                        InitialMaxLineCount();
                        //this.DataGridModel.EmptyColDataGrid();
                        //this.DataGridModel.EmptyTableDataGrid();
                        //this.DataGridModel.DealCaptureBySelectorResult(_ColContentList, true, true);
                        List<ColNameStruct> ColNameList = this.DataCollectConfigurationModel?.collectInfo?.data != null && this.DataCollectConfigurationModel?.collectInfo?.data.Count == 1 ? this.DataCollectConfigurationModel?.collectInfo?.data[0].CommonTalbleTitleList : new List<ColNameStruct>();
                        this.DataGridModel.ReGeneratPureTable(_ColContentList, ColNameList);
                    }
                    //logger.LogDebug($"蒙层 重新加载结束");
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"刷新数据发生异常");
                    MessageHelper.ShowWeakError("刷新数据发生异常".I18n("MainWindowViewModel_F8335C6C"));
                }
                finally
                {
                    isEnResfreshData = false;
                    Task.Factory.StartNew(() =>
                    {
                        ShowLoadWaiting(false, "ReloadGridData 003");
                        StartCaptureCollectDataElement();
                    });
                }
            });
        }
        /// <summary>
        /// 设置列(表)的 字段类型
        /// </summary>
        /// <param name="_ColContentList"></param>
        /// <param name="updateDataIndex"></param>
        /// <param name="isTable"></param>
        private void SetFeildType(List<List<string>> _ColContentList, int updateDataIndex, bool isTable)
        {
            if (DataCollectConfigurationModel.collectInfo.data.Count == 0)
            {
                return;
            }
            //设置列的 字段类型
            if (isTable)
            {
                //判断index大于-1的目的是为了统一处理 拾取、编辑等触发场景，判断包含字段类型属性是为了补全老版本数据
                if ((updateDataIndex != -1 && _ColContentList.Count > updateDataIndex)
                    || !DataCollectConfigurationModel.collectInfo.data[0].ExtendedInfo.ContainsKey(ConstFields.DATACOLECTING_EXTENED_FEILDTYPE_TABLE))
                {
                    var fieldTypeValList = GetTableDataFeildTypeList(_ColContentList);
                    DataCollectConfigurationModel.collectInfo.data[0].ExtendedInfo[ConstFields.DATACOLECTING_EXTENED_FEILDTYPE_TABLE] = JToken.FromObject(fieldTypeValList);
                }
            }
            else
            {
                if (updateDataIndex != -1 && _ColContentList.Count > updateDataIndex)
                {
                    var feildType = DataCollectRegexHelper.GetFeildType(_ColContentList[updateDataIndex]);
                    DataCollectConfigurationModel.collectInfo.data[updateDataIndex].ExtendedInfo[ConstFields.DATACOLECTING_EXTENED_FEILDTYPE_COL] = (int)feildType;
                }
                if (DataCollectConfigurationModel.collectInfo.data.Count == _ColContentList.Count)
                {
                    //遍历判断 补全老版本字段类型数据
                    for (int i = 0; i < DataCollectConfigurationModel.collectInfo.data.Count; i++)
                    {
                        if (i == updateDataIndex)
                        {
                            continue;
                        }
                        var item = DataCollectConfigurationModel.collectInfo.data[i];
                        if (!item.ExtendedInfo.ContainsKey(ConstFields.DATACOLECTING_EXTENED_FEILDTYPE_COL))
                        {
                            var feildType = DataCollectRegexHelper.GetFeildType(_ColContentList[i]);
                            item.ExtendedInfo[ConstFields.DATACOLECTING_EXTENED_FEILDTYPE_COL] = (int)feildType;
                        }
                    }
                }
            }
        }
        public List<int> GetTableDataFeildTypeList(List<List<string>> colContentList)
        {
            var fieldTypeValList = new List<int>();
            var _ColContentList = colContentList.Skip(1).ToList();
            if (_ColContentList.Count == 0 && colContentList.Count == 1)
            {
                colContentList.First().ForEach((item) =>
               {
                   fieldTypeValList.Add((int)eFeildType.String);
               });
                return fieldTypeValList;
            }
            var columnDataList = _ColContentList.RowsDataToColumnsData();

            foreach (var columnData in columnDataList)
            {
                var feildType = DataCollectRegexHelper.GetFeildType(columnData);
                fieldTypeValList.Add((int)feildType);
            }
            return fieldTypeValList;
        }

        bool isEnResfreshData = false;
        #endregion

        #endregion

        #region 委托回调对外开放的接口

        /// <summary>
        /// 获取当前主窗体Rect
        /// </summary>
        /// <returns></returns>
        #region 获取当前主窗体
        public List<Rect> GetCurrentRect()
        {
            if (GetMainWindowCurrentRect == null)
            {
                return null;
            }
            else
            {
                return GetMainWindowCurrentRect();
            }
        }
        public Func<List<Rect>> GetMainWindowCurrentRect = null;
        #endregion


        /// <summary>
        /// 设置窗体显隐状态
        /// </summary>
        /// <returns></returns>
        #region 获取当前主窗体
        public void SetWindowState(bool bHideWindow)
        {
            if (SetWindowStateEx == null)
            {
                return;
            }
            else
            {
                SetWindowStateEx(bHideWindow);
            }
        }
        private Action<bool> SetWindowStateEx = null;
        #endregion


        /// <summary>
        /// 设置翻页按钮图片 SetTurnPageImage
        /// </summary>
        /// <returns></returns>
        #region 设置翻页按钮图片 SetTurnPageImage
        public void SetTurnPageImage(string _imagePath)
        {
            if (Action_SetTurnPageImageEx == null)
            {
                return;
            }
            else
            {
                Action_SetTurnPageImageEx(_imagePath);
            }
        }
        private Action<string> Action_SetTurnPageImageEx = null;

        #endregion

        #region 设置最大化按钮图标
        /// <summary>
        /// 设置最大化按钮图标 
        /// </summary>
        /// <param name="_ImgType">0:最大化图标；1：还原图标</param>
        public void SetMaxSizeButtonImage(int _ImgType)
        {
            if (Action_SetMaxSizeButtonImageEx == null)
            {
                return;
            }
            else
            {
                Action_SetMaxSizeButtonImageEx(_ImgType);
            }
        }
        private Action<int> Action_SetMaxSizeButtonImageEx = null;

        #endregion
        /// <summary>
        /// 显示报警信息
        /// </summary>
        /// <returns></returns>
        #region 弱提示

        Action<string> _ShowErrorEx = null;

        public void ShowErrorOnPanelEx(string _Message)
        {
            if (GetCurrentWindowStateEx() == 1)//窗体正常显示
            {
                //logger.LogInformation("二期 调用ShowErrorOnPanel");
                ShowErrorOnPanel(_Message);
                //logger.LogInformation("二期 调用ShowErrorOnPanel结束");
            }
            else
            {
                if (_ShowErrorEx == null)
                {
                    return;
                }
                else
                {
                    //logger.LogInformation("二期 调用_ShowErrorEx");
                    _ShowErrorEx(_Message);
                    //logger.LogInformation("二期 调用_ShowErrorEx结束");
                }
            }
        }


        private void ShowErrorOnPanel(string _Message)
        {

            ShowWeakError(false);
            this.ShowWeakErrorContent = null;
            Thread.Sleep(50);
            ShowWeakError(true);

            this.ShowWeakErrorContent = _Message;
            logger.LogInformation($"当前 文字显示 {ShowWeakErrorContent}");
            if (StopShowErrorTimer == null)
            {
                InitShowErrorTimer();
            }
            StopShowErrorTimer.Stop();
            StopShowErrorTimer.Enabled = true;
            StopShowErrorTimer.Start();
        }

        /// <summary>
        /// 停止弱提示计时器
        /// </summary>
        private System.Timers.Timer StopShowErrorTimer = null;

        public void ShowErrorOnPanelTimerTrick(object sender, System.Timers.ElapsedEventArgs e)
        {
            //logger.LogInformation("二期 错误显示计时结束");
            ShowWeakError(false);
            StopShowErrorTimer.Enabled = false;
            StopShowErrorTimer.Stop();
        }

        private void InitShowErrorTimer()
        {
            //Timer timer = new Timer(new TimerCallback(ShowErrorOnPanelTimerTrick), null, 100,100);
            StopShowErrorTimer = new System.Timers.Timer(4000);
            StopShowErrorTimer.Enabled = false;
            StopShowErrorTimer.Elapsed += ShowErrorOnPanelTimerTrick;
        }

        #endregion

        /// <summary>
        /// 显示/关闭蒙版弱提示效果 true 显示/ false 关闭ReloadGridData
        /// </summary>
        #region ShowWeakError
        public void ShowWeakError(bool _ShowLoad)
        {
            if (_ShowLoad)
            {
                //logger.LogInformation("二期 显示 错误信息");
                this.ShowWeakErrorVisibility = Visibility.Visible;
            }
            else
            {
                //logger.LogInformation("二期 隐藏 错误信息");
                this.ShowWeakErrorVisibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// 设置消息提示框宽度
        /// </summary>
        /// <param name="_ErrorMessageBoxWidth"></param>
        public void SetErrorBoxWidth(double _ErrorMessageBoxWidth)
        {
            this.ShowWeakErrorMaxWidth = _ErrorMessageBoxWidth;
        }
        #endregion

        /// <summary>
        /// 强关主窗口
        /// </summary>
        /// <returns></returns>
        #region 强关窗体
        public void ForceCloseWindow()
        {
            if (_ForceCloseWindowEx == null)
            {
                return;
            }
            else
            {
                _ForceCloseWindowEx();
            }
        }

        Action _ForceCloseWindowEx = null;
        #endregion

        /// <summary>
        /// 设置表格列模式
        /// </summary>
        /// <returns></returns>
        #region 设置表格列模式
        public void SetDataGridColState(DataGridState _DataGridState, int ColOrCommon)
        {
            if (_SetDataGridColStateEx == null)
            {
                return;
            }
            else
            {
                _SetDataGridColStateEx(_DataGridState, ColOrCommon);
            }
        }
        Action<DataGridState, int> _SetDataGridColStateEx = null;
        #endregion

        /// <summary>
        /// 显示/关闭蒙版数据缓冲效果 true 显示/ false 关闭
        /// </summary>
        #region ShowLoadWaiting
        public void ShowLoadWaiting(bool _ShowLoad, string strPositionDescription = "")
        {
            //Dispatcher.CurrentDispatcher.Invoke();
            if (_ShowLoad)
            {
                OverlayVisibility = Visibility.Visible;
                this.LoadingLogoVisibility = Visibility.Visible;
                //logger.LogDebug($"蒙层 开启{strPosition}");
            }
            else
            {
                this.LoadingLogoVisibility = Visibility.Hidden;
                OverlayVisibility = Visibility.Hidden;
                //logger.LogDebug($"蒙层 关闭{strPosition}");
            }
        }

        public void ShowShadow(bool IsOverlayActive)
        {
            if (IsOverlayActive)
            {
                OverlayVisibility = Visibility.Visible;
            }
            else
            {
                OverlayVisibility = Visibility.Hidden;
            }
        }


        #endregion

        /// <summary>
        /// 当前窗体位置状态
        /// </summary>
        #region 设置窗体当前位置与大小
        public void SetWndPositionAndSizeState(MyWindowState _MyWindowState)
        {
            if (_SetWndPostionAndSizeStateEx == null)
            {
                return;
            }
            else
            {
                _SetWndPostionAndSizeStateEx(_MyWindowState);
                CurrentWndPositionState = _MyWindowState;
            }
        }
        public Action<MyWindowState> _SetWndPostionAndSizeStateEx = null;

        MyWindowState CurrentWndPositionState = new MyWindowState();//当前State
        #endregion

        /// <summary>
        /// 当前窗体位置状态
        /// </summary>
        #region 设置窗体当前位置与大小
        public void ScrollToEnd(bool MustScroll = false)
        {
            if (_ScrollToEndEx == null)
            {
                logger.LogInformation($"滚动失败 未定义回调");
                return;
            }
            else
            {
                if (NeedScroll == true || MustScroll == true)
                {
                    _ScrollToEndEx();
                    NeedScroll = false;
                }
            }
        }
        public Action _ScrollToEndEx = null;
        #endregion

        //
        /// <summary>
        /// 当前窗体位置状态
        /// </summary>
        #region 设置窗体当前位置与大小
        public void RefreshDataGridItems()
        {
            if (_RefreshEx == null)
            {
                return;
            }
            else
            {
                _RefreshEx();
            }
        }
        public Action _RefreshEx = null;
        #endregion

        /// <summary>
        /// 获取当前表格宽度
        /// </summary>
        #region 获取当前表格宽度
        public double GetCurrentDataGridWidth(int _iDataGridType)
        {
            if (_GetCurrentDataGridWidthEx == null)
            {
                return 0;
            }
            else
            {
                return _GetCurrentDataGridWidthEx(_iDataGridType);
            }
        }
        private Func<int, double> _GetCurrentDataGridWidthEx = null;
        #endregion

        #region 获取当前窗体状态
        public int GetCurrentWindowStateEx()
        {
            if (_GetCurrentWindowStateEx == null)
            {
                return 0;
            }
            else
            {
                return _GetCurrentWindowStateEx();
            }
        }
        private Func<int> _GetCurrentWindowStateEx = null;
        #endregion

        #endregion
    }
}


