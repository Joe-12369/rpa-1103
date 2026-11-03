using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI.DataCollection.ViewModels
{
    /// <summary>
    /// 翻页模型
    /// </summary>
    public class CapturePageViewModel : ModelBase
    {
        private static ILogger logger = Logger.OfContext();

        public CapturePageViewModel()
        {
            IsVisibleChangedCommand = new DelegateCommand(IsVisibleChangedExcute);

            AllPageRadioClickCommand = new DelegateCommand(ClickAllPage);
            CurrentPageRadioClickCommand = new DelegateCommand(ClickCurrentPage);
            CustomPageRadioClickCommand = new DelegateCommand(ClickCustomPage);

            OriginCaptureTurnPageButtonCommand = new DelegateCommand(OriginCaptureTurnPageButtonExcute);
            ReCaptureTurnPageButtonCommand = new DelegateCommand(ReCaptureTurnPageButtonExcute);
            EditCaptureTurnPageButtonSelectorCommand = new DelegateCommand(EditCaptureTurnPageButtonSelectorExcute);
            DeleteCaptureTurnPageButtonSelectorCommand = new DelegateCommand(DeleteCaptureTurnPageButtonSelectorExcute);

        }

        #region 私有变量
        bool BooleanHasCaptureTurnPageButton = false;//是否获取翻页元素
        #endregion

        #region 绑定变量
        private string _CapturePageInfoText = eCommonMessageType.CapturePageViewModel_cs_DangQianYe.GetMessage();//离线读码模型
        /// <summary>
        /// 翻页信息显示
        /// </summary>
        public string CapturePageInfoText
        {
            get { return this._CapturePageInfoText; }
            set
            {
                this._CapturePageInfoText = value;
                this.RaisePropertyChanged("CapturePageInfoText");
            }
        }

        private Visibility _SeparatorVisibility = Visibility.Collapsed;
        /// <summary>
        /// 分割线
        /// </summary>
        public Visibility SeparatorVisibility
        {
            get { return this._SeparatorVisibility; }
            set
            {
                this._SeparatorVisibility = value;
                this.RaisePropertyChanged("SeparatorVisibility");
            }
        }

        private Visibility _PageCountVisibility = Visibility.Collapsed;
        /// <summary>
        /// 页数显示
        /// </summary>
        public Visibility PageCountVisibility
        {
            get { return this._PageCountVisibility; }
            set
            {
                this._PageCountVisibility = value;
                this.RaisePropertyChanged("PageCountVisibility");
            }
        }

        private Visibility _PageCapturePanelVisibility = Visibility.Collapsed;
        /// <summary>
        /// 翻页下窗体显示
        /// </summary>
        public Visibility PageCapturePanelVisibility
        {
            get { return this._PageCapturePanelVisibility; }
            set
            {
                this._PageCapturePanelVisibility = value;
                this.RaisePropertyChanged("PageCapturePanelVisibility");
            }
        }

        private Visibility _OriginCaptureTurnPageButtonVisibility = Visibility.Collapsed;
        /// <summary>
        /// 原始拾取翻页按钮显示
        /// </summary>
        public Visibility OriginCaptureTurnPageButtonVisibility
        {
            get { return this._OriginCaptureTurnPageButtonVisibility; }
            set
            {
                this._OriginCaptureTurnPageButtonVisibility = value;
                this.RaisePropertyChanged("OriginCaptureTurnPageButtonVisibility");
            }
        }

        private Visibility _ImageTurnPageButtonVisibility = Visibility.Collapsed;
        /// <summary>
        /// 翻页按钮截图显示
        /// </summary>
        public Visibility ImageTurnPageButtonVisibility
        {
            get { return this._ImageTurnPageButtonVisibility; }
            set
            {
                this._ImageTurnPageButtonVisibility = value;
                this.RaisePropertyChanged("ImageTurnPageButtonVisibility");
            }
        }

        private Visibility _EditPanelTurnPageButtonVisibility = Visibility.Collapsed;
        /// <summary>
        /// 翻页蒙层编辑显示
        /// </summary>
        public Visibility EditPanelTurnPageButtonVisibility
        {
            get { return this._EditPanelTurnPageButtonVisibility; }
            set
            {
                this._EditPanelTurnPageButtonVisibility = value;
                this.RaisePropertyChanged("EditPanelTurnPageButtonVisibility");
            }
        }

        private bool _IsCheckedAllPage = false;
        /// <summary>
        /// 所有页选中状态
        /// </summary>
        public bool IsCheckedAllPage
        {
            get { return this._IsCheckedAllPage; }
            set
            {
                this._IsCheckedAllPage = value;
                this.RaisePropertyChanged("IsCheckedAllPage");
            }
        }

        private bool _IsCheckedCurrentPage = true;
        /// <summary>
        /// 当前页选中状态
        /// </summary>
        public bool IsCheckedCurrentPage
        {
            get { return this._IsCheckedCurrentPage; }
            set
            {
                this._IsCheckedCurrentPage = value;
                this.RaisePropertyChanged("IsCheckedCurrentPage");
            }
        }

        private bool _IsCheckedCustomPage = false;
        /// <summary>
        /// 自定义页选中状态
        /// </summary>
        public bool IsCheckedCustomPage
        {
            get { return this._IsCheckedCustomPage; }
            set
            {
                this._IsCheckedCustomPage = value;
                this.RaisePropertyChanged("IsCheckedCustomPage");
            }
        }

        private bool _IsPopupOpen = false;
        /// <summary>
        /// 翻页拾取弹出
        /// </summary>
        public bool IsPopupOpen
        {
            get { return this._IsPopupOpen; }
            set
            {
                Console.WriteLine($"翻页显示:{value}");
                this._IsPopupOpen = value;
                this.RaisePropertyChanged("IsPopupOpen");

            }
        }


        private bool _StaysOpen = false;
        /// <summary>
        /// 翻页拾取弹出
        /// </summary>
        public bool StaysOpen
        {
            get { return this._StaysOpen; }
            set
            {
                this._StaysOpen = value;
                this.RaisePropertyChanged("StaysOpen");

            }
        }

        private int _PageCount = 1;
        /// <summary>
        /// 翻页数量
        /// </summary>
        public int PageCount
        {
            get { return this._PageCount; }
            set
            {
                this._PageCount = value;
                CapturePageInfoText = eCommonMessageType.CapturePageViewModel_cs_ZhiDingYeShuPage.GetMessage()+ value;
                this.RaisePropertyChanged("PageCount");
            }
        }

        private int _PerPageDelayTime = 1000;
        /// <summary>
        /// 每页延时
        /// </summary>
        public int PerPageDelayTime
        {
            get { return this._PerPageDelayTime; }
            set
            {
                this._PerPageDelayTime = value;
                this.RaisePropertyChanged("PerPageDelayTime");
            }
        }

        private BitmapImage _ImageSource = null;
        /// <summary>
        /// 翻页按钮截图路径
        /// </summary>
        public BitmapImage TurnPageButtonImageSource
        {
            get { return this._ImageSource; }
            set
            {
                this._ImageSource = value;
                this.RaisePropertyChanged("TurnPageButtonImageSource");
            }
        }

        #endregion

        #region 命令
        /// <summary>
        /// 点击所有页
        /// </summary>
        public DelegateCommand AllPageRadioClickCommand { get; set; }

        /// <summary>
        /// 点击当前页
        /// </summary>
        public DelegateCommand CurrentPageRadioClickCommand { get; set; }

        /// <summary>
        /// 点击指定页
        /// </summary>
        public DelegateCommand CustomPageRadioClickCommand { get; set; }

        /// <summary>
        /// 原始 拾取翻页
        /// </summary>
        public DelegateCommand OriginCaptureTurnPageButtonCommand { get; set; }

        /// <summary>
        /// 蒙层 拾取翻页
        /// </summary>
        public DelegateCommand ReCaptureTurnPageButtonCommand { get; set; }

        /// <summary>
        /// 蒙层 编辑翻页元素
        /// </summary>
        public DelegateCommand EditCaptureTurnPageButtonSelectorCommand { get; set; }

        /// <summary>
        /// 蒙层 删除翻页元素
        /// </summary>
        public DelegateCommand DeleteCaptureTurnPageButtonSelectorCommand { get; set; }

        /// <summary>
        /// 翻页Popup 显示变化命令 暂时有问题 不适用
        /// </summary>
        public DelegateCommand IsVisibleChangedCommand { get; set; }
        #endregion

        #region 执行函数


        /// <summary>
        /// 翻页拾取 下拉菜单显示状态更改 无效弃用
        /// </summary>
        /// <param name="ObjectValue"></param>
        public void IsVisibleChangedExcute(object ObjectValue)
        {
            bool BIsOpen = Convert.ToBoolean(ObjectValue);
            Console.WriteLine($"当前显示{ObjectValue}");
        }

        /// <summary>
        /// 初始化显示
        /// </summary>
        public void InitVisibility()
        {
            IsCheckedAllPage = false;
            IsCheckedCurrentPage = true;
            IsCheckedCustomPage = false;

            SeparatorVisibility = Visibility.Collapsed;

            EditPanelTurnPageButtonVisibility = Visibility.Collapsed;
            ImageTurnPageButtonVisibility = Visibility.Collapsed;
            OriginCaptureTurnPageButtonVisibility = Visibility.Collapsed;

            PageCountVisibility = Visibility.Collapsed;
            PageCapturePanelVisibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 点击获取所有页
        /// </summary>
        /// <param name="ObjectValue"></param>
        public void ClickAllPage(object ObjectValue)
        {
            ShowPageConfigType(TurnPageConfigType.AllPage);
        }

        /// <summary>
        /// 点击当前页
        /// </summary>
        /// <param name="ObjectValue"></param>
        public void ClickCurrentPage(object ObjectValue)
        {
            ShowPageConfigType(TurnPageConfigType.CurrentPage);
        }

        /// <summary>
        /// 点击自定义页
        /// </summary>
        /// <param name="ObjectValue"></param>
        public void ClickCustomPage(object ObjectValue)
        {
            ShowPageConfigType(TurnPageConfigType.CustomPage);
        }

        /// <summary>
        /// 点击翻页拾取 按钮
        /// </summary>
        /// <param name="objectValue"></param>
        public void OriginCaptureTurnPageButtonExcute(object objectValue)
        {
            try
            {
                ///需要赋值 ImageSource_TurnPageButton 展示元素拾取截图
                GlobalData.GetInstance().VM_MainWindowViewModel.SetWindowState(true);
                GlobalData.GetInstance().VM_MainWindowViewModel.StartCaptureCollectDataElement(false, Core.CaptureElementType.PagingButtonElement);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"点击翻页拾取失败");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.CapturePageViewModel_cs_FanYeYuanSuShiQuShiBai.GetMessage());
            }
        }

        /// <summary>
        /// 点击蒙版翻页拾取 按钮 (重新拾取)
        /// </summary>
        /// <param name="objectValue"></param>
        public void ReCaptureTurnPageButtonExcute(object objectValue)
        {
            try
            {
                GlobalData.GetInstance().VM_MainWindowViewModel.SetWindowState(true);
                // 翻页元素 接收拾取结果是在MainWindowViewModel回调函数里面
                GlobalData.GetInstance().VM_MainWindowViewModel.StartCaptureCollectDataElement(false, Core.CaptureElementType.PagingButtonElement);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "点击翻页拾取失败");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.CapturePageViewModel_cs_FanYeYuanSuShiQuShiBai.GetMessage());
            }
        }

        /// <summary>
        /// 点击蒙版翻页编辑 按钮 （翻页元素编辑）
        /// </summary>
        /// <param name="objectValue"></param>
        public void EditCaptureTurnPageButtonSelectorExcute(object objectValue)
        {
            try
            {
                DataCollectingManager.StopCapture();
                ///元素编辑器 修改翻页元
                List<SelectorProps> _SelectorProps = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelector;

                string _guiType = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.pageInfo.guiType;
                string _filePath = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.turnPageButtonInfo.filePath;
                ParamDoElementEditorModel _ParamDoElementEditorModel = new ParamDoElementEditorModel
                {
                    guiType = _guiType,
                    filePath = _filePath,
                    selector = _SelectorProps.ToJson(),
                    isSimilar = false
                };
                GlobalData.GetInstance().VM_MainWindowViewModel.SetWindowState(true);

                Task.Factory.StartNew(() =>
                {
                    EditCaptureSelector(_ParamDoElementEditorModel);
                });
            }
            catch (Exception ex)
            {

                string msg = string.Empty;
                if (ex is RPAException)
                {
                    msg = ((RPAException)ex).ErrorMessageContent;
                }
                else
                {
                    msg = "翻页元素编辑发生异常，请重启程序后重试！".I18n("CapturePageViewModel_8E959CD9");
                }
                MessageHelper.ShowDialog(msg);
            }
            finally
            {
                GlobalData.GetInstance().VM_MainWindowViewModel.SetWindowState(false);
            }
        }

        /// <summary>
        /// 编辑翻页元素
        /// </summary>
        /// <param name="_ParamDoElementEditorModel"></param>
        private void EditCaptureSelector(ParamDoElementEditorModel _ParamDoElementEditorModel)
        {

            try
            {
                List<SelectorProps> _RetSelector = DataCollectingManager.DoElementEditor(_ParamDoElementEditorModel);
                if (_RetSelector == null || _RetSelector.Count == 0)
                {
                    return;
                }
                GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.turnPageButtonInfo.nextBtnSelector = _RetSelector;

            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "编辑翻页元素失败");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.CapturePageViewModel_cs_BianJiFanYeYuanSuShiBai.GetMessage());
            }
            finally
            {
                GlobalData.GetInstance().VM_MainWindowViewModel.SetWindowState(false);
                Task.Factory.StartNew(() =>
                {
                    GlobalData.GetInstance().VM_MainWindowViewModel.StartCaptureCollectDataElement(false);
                });
            }
        }

        /// <summary>
        /// 点击蒙版翻页删除 按钮 （翻页元素删除）
        /// </summary>
        /// <param name="objectValue"></param>
        public void DeleteCaptureTurnPageButtonSelectorExcute(object objectValue)
        {
            try
            {
                ///删除翻页元素 回归拾取界面
                TurnPageButtonImageSource = null;
                ShowCaptureState(CaptureTurnPageState.Origin);
                BooleanHasCaptureTurnPageButton = false;

                GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.turnPageButtonInfo.DeleteTurnPageElement();
               //数据埋点
               GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.page_element_value = 2;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "翻页元素删除异常");
                MessageHelper.ShowDialog("翻页元素删除失败".I18n("CapturePageViewModel_29715E0A"));
            }
        }

        /// <summary>
        /// 根据状态调整控件显示
        /// </summary>
        /// <param name="_TurnPageConfigType"></param>
        public void ShowPageConfigType(TurnPageConfigType _TurnPageConfigType)
        {
            try
            {
                switch (_TurnPageConfigType)
                {
                    case TurnPageConfigType.AllPage:
                        CapturePageInfoText = eCommonMessageType.CapturePageViewModel_cs_SuoYouYe.GetMessage();
                        SeparatorVisibility = Visibility.Visible;
                        PageCapturePanelVisibility = Visibility.Visible;
                        PageCountVisibility = Visibility.Collapsed;
                        if (BooleanHasCaptureTurnPageButton)
                        {
                            OriginCaptureTurnPageButtonVisibility = Visibility.Collapsed;
                            EditPanelTurnPageButtonVisibility = Visibility.Collapsed;
                            ImageTurnPageButtonVisibility = Visibility.Visible;
                        }
                        else
                        {
                            OriginCaptureTurnPageButtonVisibility = Visibility.Visible;
                        }
                        //数据埋点
                        GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.collect_page_mode = Params.PageSearchType.AllPage;
                        break;
                    case TurnPageConfigType.CurrentPage:
                        CapturePageInfoText = eCommonMessageType.CapturePageViewModel_cs_DangQianYe.GetMessage();
                        SeparatorVisibility = Visibility.Collapsed;
                        PageCapturePanelVisibility = Visibility.Collapsed;
                        PageCountVisibility = Visibility.Collapsed;

                        //数据埋点
                        GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.collect_page_mode = Params.PageSearchType.CurrentPage;
                        break;
                    case TurnPageConfigType.CustomPage:
                        CapturePageInfoText = eCommonMessageType.CapturePageViewModel_cs_ZhiDingYeShuPage.GetMessage() +" "+PageCount;
                        SeparatorVisibility = Visibility.Visible;
                        PageCapturePanelVisibility = Visibility.Visible;
                        PageCountVisibility = Visibility.Visible;
                        if (BooleanHasCaptureTurnPageButton)
                        {
                            OriginCaptureTurnPageButtonVisibility = Visibility.Collapsed;
                            EditPanelTurnPageButtonVisibility = Visibility.Collapsed;
                            ImageTurnPageButtonVisibility = Visibility.Visible;
                        }
                        else
                        {
                            OriginCaptureTurnPageButtonVisibility = Visibility.Visible;
                            EditPanelTurnPageButtonVisibility = Visibility.Collapsed;
                            ImageTurnPageButtonVisibility = Visibility.Collapsed;
                        }

                        //数据埋点
                        GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.collect_page_mode = Params.PageSearchType.CustomizedPage;
                        break;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "根据状态调整控件显示出错");
                MessageHelper.ShowDialog(eCommonMessageType.CapturePageViewModel_cs_ShuJuYiChang.GetMessage());
            }

        }

        /// <summary>
        /// 展示当前拾取状态 （控制组件显示）
        /// </summary>
        /// <param name="iState"></param>
        public void ShowCaptureState(CaptureTurnPageState iState)
        {
            try
            {
                switch (iState)
                {
                    case CaptureTurnPageState.Origin: //未拾取状态
                        OriginCaptureTurnPageButtonVisibility = Visibility.Visible;
                        EditPanelTurnPageButtonVisibility = Visibility.Collapsed;
                        ImageTurnPageButtonVisibility = Visibility.Collapsed;
                        BooleanHasCaptureTurnPageButton = false;
                        //数据埋点
                        GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.page_element_value = 2;
                        break;
                    case CaptureTurnPageState.Captured:// 已拾取状态
                        OriginCaptureTurnPageButtonVisibility = Visibility.Collapsed;
                        EditPanelTurnPageButtonVisibility = Visibility.Collapsed;
                        ImageTurnPageButtonVisibility = Visibility.Visible;
                        BooleanHasCaptureTurnPageButton = true;
                        //数据埋点
                        GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.page_element_value = GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.page_element_value== 0 ? 0 : 1;
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "展示当前拾取状态失败");
                MessageHelper.ShowDialog(eCommonMessageType.CapturePageViewModel_cs_ShuJuYiChang.GetMessage());
            }
        }

        /// <summary>
        /// 加载翻页元素配置
        /// </summary>
        /// <param name="_DataCollectConfiguration_TurnPageButtonInfo"></param>
        /// <param name="_PackagePath"></param>
        public void ShowPreviewConfig(DataCollectConfiguration_TurnPageButtonInfo _DataCollectConfiguration_TurnPageButtonInfo, string _PackagePath)
        {
            ShowCapturedState(_DataCollectConfiguration_TurnPageButtonInfo, _PackagePath);
        }

        /// <summary>
        /// 展示 已拾取状态
        /// </summary>
        /// <param name="_DataCollectConfiguration_TurnPageButtonInfo"></param>
        /// <param name="_PackagePath"></param>
        public void ShowCapturedState(DataCollectConfiguration_TurnPageButtonInfo _DataCollectConfiguration_TurnPageButtonInfo, string _PackagePath)
        {
            if (_DataCollectConfiguration_TurnPageButtonInfo.nextBtnSelector == null || _DataCollectConfiguration_TurnPageButtonInfo.nextBtnSelector.Count == 0)
            {
                ShowCaptureState(CaptureTurnPageState.Origin);
            }
            else
            {
                ShowCaptureState(CaptureTurnPageState.Captured);
            }
            ShowCaptureImg(_DataCollectConfiguration_TurnPageButtonInfo.filePath, _PackagePath);
            SetData(_DataCollectConfiguration_TurnPageButtonInfo.pageCount, _DataCollectConfiguration_TurnPageButtonInfo.delay);
            ShowPageConfigType(_DataCollectConfiguration_TurnPageButtonInfo.turnPageConfigType);
            ShowClickState(_DataCollectConfiguration_TurnPageButtonInfo.turnPageConfigType);
        }


        public void SetTurnPageButtonInfo(DataCollectConfiguration_TurnPageButtonInfo _DataCollectConfiguration_TurnPageButtonInfo)
        {
            _DataCollectConfiguration_TurnPageButtonInfo.delay = PerPageDelayTime;
            _DataCollectConfiguration_TurnPageButtonInfo.pageCount = PageCount;
            _DataCollectConfiguration_TurnPageButtonInfo.turnPageConfigType = GetState();
        }

        public void ShowClickState(TurnPageConfigType _TurnPageConfigType)
        {
            switch (_TurnPageConfigType)
            {
                case TurnPageConfigType.AllPage:
                    IsCheckedAllPage = true;
                    IsCheckedCurrentPage = false;
                    IsCheckedCustomPage = false;
                    break;
                case TurnPageConfigType.CurrentPage:
                    IsCheckedAllPage = false;
                    IsCheckedCurrentPage = true;
                    IsCheckedCustomPage = false;
                    break;
                case TurnPageConfigType.CustomPage:
                    IsCheckedAllPage = false;
                    IsCheckedCurrentPage = false;
                    IsCheckedCustomPage = true;
                    break;
            }
        }

        public TurnPageConfigType GetState()
        {
            if (IsCheckedAllPage)
            {
                return TurnPageConfigType.AllPage;
            }
            if (IsCheckedCurrentPage)
            {
                return TurnPageConfigType.CurrentPage;
            }
            if (IsCheckedCustomPage)
            {
                return TurnPageConfigType.CustomPage;
            }
            return TurnPageConfigType.CurrentPage;
        }

        public void SetData(int _PageCount, int _Delay)
        {
            this.PageCount = _PageCount;
            this.PerPageDelayTime = _Delay;
        }

        public void ShowCaptureResult(CaptureTurnPageState iState, string _ImgFileName, string _PackagePath)
        {
            ShowCaptureState(iState);

            ShowCaptureImg(_ImgFileName, _PackagePath);//显示图片
        }

        #endregion


        #region 其余操作函数

        public void ShowCaptureImg(string _ImgFileName, string _PackagePath)
        {

            if (!string.IsNullOrEmpty(_ImgFileName))
            {
                string _FullImgPath = Path.Combine(_PackagePath, _ImgFileName);
                GlobalData.GetInstance()?.VM_MainWindowViewModel?.SetTurnPageImage(_FullImgPath);
            }
            else
            {
                GlobalData.GetInstance()?.VM_MainWindowViewModel?.SetTurnPageImage(null);
            }
        }

        /// <summary>
        /// 设置当前翻页弹窗的StayOpen状态
        /// </summary>
        /// <param name="_StayOpen"></param>
        public void SetStayOpen(bool _StayOpen = true)
        {
            this.StaysOpen = _StayOpen;
        }
        #endregion
    }

    public enum CaptureTurnPageState : int
    {
        Origin = 0,
        Captured = 1
    }
}

