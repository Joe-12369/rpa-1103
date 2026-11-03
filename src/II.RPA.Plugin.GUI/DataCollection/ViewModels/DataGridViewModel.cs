using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using DataCaptureJinyun.Windows;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.DataCollection.DataTypeConverter;
using II.RPA.Plugin.GUI.DataCollection.Utils;
using II.RPA.Plugin.GUI.DataCollection.Windows;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI.DataCollection.ViewModels
{
    public class DataGridViewModel : ModelBase
    {
        private static ILogger logger = Logger.OfContext();

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.  DataGrid
        /// </summary>
        public DataGridViewModel()
        {
            //AddDataCmd = new DelegateCommand(AddData);
            #region 列操作
            GetTextContentCommand = new DelegateCommand(GetTextContentCommandExcute);
            GetLinkCommand = new DelegateCommand(GetLinkCommandExcute);
            GetImageSrcCommand = new DelegateCommand(GetImageSrcCommandExcute);
            EditColumnSelectorCommand = new DelegateCommand(EditColumnSelectorCommandExcute);
            EditRegularExpressionCommand = new DelegateCommand(EditRegularExpressionCommandExcute);
            GetSimulateItemCommand = new DelegateCommand(GetSimulateItemCommandExcute);
            EditColumnHeaderCommand = new DelegateCommand(EditColumnHeaderCommandExcute);
            MoveForewardCommand = new DelegateCommand(MoveForewardCommandExcute);
            MoveBackwardCommand = new DelegateCommand(MoveBackwardCommandExcute);
            DeleteColumnCommand = new DelegateCommand(DeleteColumnCommandExcute);
            OpenPopupCommand = new DelegateCommand(OpenPopupCommandExcute);
            #endregion

            #region 普通表格列操作
            CommonTable_OpenPopupCommand = new DelegateCommand(CommonTable_OpenPopupCommandExcute);
            //CommonTable_EditColumnHeaderCommand = new DelegateCommand(CommonTable_EditColumnHeaderCommandExcute);
            #endregion

            #region 其余操作
            DataVerifyCommand = new DelegateCommand(DataVerifyCommandExcute);
            EmptyDataGridCommand = new DelegateCommand(EmptyDataGridCommandExcute);
            EditTableSelectorCommand = new DelegateCommand(EditTableSelectorCommandExcute);
            RefreshDataGridCommand = new DelegateCommand(RefreshDataGridCommandExcute);
            #endregion
        }

        #region 绑定变量
        private bool isOpen = false;
        public bool PopupIsOpen
        {
            get { return isOpen; }
            set
            {
                isOpen = value;
                RaisePropertyChanged("PopupIsOpen");
            }
        }

        private bool _IsPageCapturePopupOpen = false;
        public bool IsPageCapturePopupOpen
        {
            get { return _IsPageCapturePopupOpen; }
            set
            {
                _IsPageCapturePopupOpen = value;
                RaisePropertyChanged("IsPageCapturePopupOpen");
            }
        }

        private Visibility _ColumnsDataVisibility = Visibility.Collapsed;
        /// <summary>
        /// 列相似元素表格显示
        /// </summary>
        public Visibility ColumnsDataVisibility
        {
            get { return _ColumnsDataVisibility; }
            set
            {
                _ColumnsDataVisibility = value;
                RaisePropertyChanged("ColumnsDataVisibility");
            }
        }

        private Visibility _TableDataVisibility = Visibility.Collapsed;
        /// <summary>
        /// 纯表格界面显示
        /// </summary>
        public Visibility TableDataVisibility
        {
            get { return _TableDataVisibility; }
            set
            {
                _TableDataVisibility = value;
                RaisePropertyChanged("TableDataVisibility");
            }
        }

        private Visibility _InitPanelVisibility = Visibility.Visible;
        /// <summary>
        /// 初始界面显示
        /// </summary>
        public Visibility InitPanelVisibility
        {
            get { return _InitPanelVisibility; }
            set
            {
                _InitPanelVisibility = value;
                RaisePropertyChanged("InitPanelVisibility");
            }
        }

        private bool _CurrentColumnContentType = true;
        /// <summary>
        /// 内容选项颜色
        /// </summary>
        public bool CurrentColumnContentType
        {
            get { return _CurrentColumnContentType; }
            set
            {
                _CurrentColumnContentType = value;
                RaisePropertyChanged("CurrentColumnContentType");
            }
        }

        private bool _CurrentColumnHrefType = false;
        /// <summary>
        /// 内容选项颜色
        /// </summary>
        public bool CurrentColumnHrefType
        {
            get { return _CurrentColumnHrefType; }
            set
            {
                _CurrentColumnHrefType = value;
                RaisePropertyChanged("CurrentColumnHrefType");
            }
        }


        private bool _CurrentColumnSrcType = false;
        /// <summary>
        /// 内容选项颜色
        /// </summary>
        public bool CurrentColumnSrcType
        {
            get { return _CurrentColumnSrcType; }
            set
            {
                _CurrentColumnSrcType = value;
                RaisePropertyChanged("CurrentColumnSrcType");
            }
        }
        #endregion

        #region 命令

        /// <summary>
        /// 添加数据 模拟操作
        /// </summary>
        public DelegateCommand AddDataCmd { get; set; }

        #region 列操作命令

        //OpenCommonTablePopupCommandExcute

        /// <summary>
        /// 普通表格 点击下拉按钮弹出下拉菜单
        /// </summary>
        public DelegateCommand CommonTable_OpenPopupCommand { get; set; }

        /// <summary>
        /// 普通表格 编辑列头
        /// </summary>
        public DelegateCommand CommonTable_EditColumnHeaderCommand { get; set; }

        /// <summary>
        /// 点击下拉按钮弹出下拉菜单
        /// </summary>
        public DelegateCommand OpenPopupCommand { get; set; }


        /// <summary>
        /// 获取文本内容
        /// </summary>
        public DelegateCommand GetTextContentCommand { get; set; }

        /// <summary>
        /// 获取链接
        /// </summary>
        public DelegateCommand GetLinkCommand { get; set; }

        /// <summary>
        /// 获取图片
        /// </summary>
        public DelegateCommand GetImageSrcCommand { get; set; }

        /// <summary>
        /// 编辑列元素
        /// </summary>
        public DelegateCommand EditColumnSelectorCommand { get; set; }

        /// <summary>
        /// 编辑正则
        /// </summary>
        public DelegateCommand EditRegularExpressionCommand { get; set; }

        /// <summary>
        /// 拾取相似元素
        /// </summary>
        public DelegateCommand GetSimulateItemCommand { get; set; }

        /// <summary>
        /// 编辑列头
        /// </summary>
        public DelegateCommand EditColumnHeaderCommand { get; set; }

        /// <summary>
        /// 前移一列
        /// </summary>
        public DelegateCommand MoveForewardCommand { get; set; }

        /// <summary>
        /// 后移一列
        /// </summary>
        public DelegateCommand MoveBackwardCommand { get; set; }

        /// <summary>
        /// 删除列
        /// </summary>
        public DelegateCommand DeleteColumnCommand { get; set; }
        #endregion

        /// <summary>
        /// 数据纠错
        /// </summary>
        public DelegateCommand DataVerifyCommand { get; set; }

        /// <summary>
        /// 清空数据
        /// </summary>
        public DelegateCommand EmptyDataGridCommand { get; set; }


        /// <summary>
        /// 编辑表格Selector
        /// </summary>
        public DelegateCommand EditTableSelectorCommand { get; set; }

        /// <summary>
        /// 刷新表格数据
        /// </summary>
        public DelegateCommand RefreshDataGridCommand { get; set; }


        #endregion

        #region 参数
        private int _ColumnIndex = 0;

        //当前操作的列序号
        public int CurrentColumnIndex
        {
            get { return _ColumnIndex; }
            set { _ColumnIndex = value; }
        }

        /// <summary>
        ///  当前操作的数据表DataGrid控件
        /// </summary>
        DataGrid CurrentDataGrid = new DataGrid();

        public int GetCurrentColumnsCount()
        {
            return RealDataTable.GetCurrentColCount();
        }
        #endregion

        #region 执行函数

        /// <summary>
        /// 模拟增加数据
        /// </summary>
        /// <returns></returns>
        private DataTable GetDataTable()
        {
            DataTable data = new DataTable("MyDataTable");
            DataColumn ID = new DataColumn("A");//第一列
            ID.DataType = System.Type.GetType("System.Int32");

            data.Columns.Add(ID);
            data.Columns.Add(new DataColumn("B", typeof(string)));//第二列
            data.Columns.Add(new DataColumn("C", typeof(string)));//第三列

            data.Rows.Add(1, "  XiaoM", "  20");
            data.Rows.Add(2, "  XiaoF", "  122");
            data.Rows.Add(3, "  XiaoA", "  29");
            data.Rows.Add(4, "  XiaoB", "  102");
            return data;
        }

        #region 列操作实现
        /// <summary>
        /// 打开弹窗
        /// </summary>
        /// <param name="objectValue"></param>
        public void OpenPopupCommandExcute(object objectValue)
        {
            try
            {
                string _ColumnName = objectValue as string;
                CurrentColumnIndex = this.RealDataTable.GetColIndexByColName(_ColumnName);
                SetCurrentColumnDataType(GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "弹出下拉框异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_DanChuXiaLaKuangYiChang.GetMessage() );
            }
        }

        /// <summary>
        /// 打开普通表格弹窗
        /// </summary>
        /// <param name="objectValue"></param>
        public void CommonTable_OpenPopupCommandExcute(object objectValue)
        {
            try
            {
                string _ColumnName = objectValue as string;
                CurrentColumnIndex = this.RealDataTableForPureTable.GetColIndexByColName(_ColumnName);

                CommonTable_EditColumnHeaderCommandExcute(_ColumnName);
                //SetCurrentColumnDataType(GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "弹出下拉框异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_DanChuXiaLaKuangYiChang.GetMessage() );
            }
        }

        public void SetCurrentColumnDataType(Core.CollectType _CollectType)
        {
            switch (_CollectType)
            {
                case Core.CollectType.Content:
                    CurrentColumnContentType = true;
                    CurrentColumnHrefType = false;
                    CurrentColumnSrcType = false;
                    break;
                case Core.CollectType.Href:
                    CurrentColumnContentType = false;
                    CurrentColumnHrefType = true;
                    CurrentColumnSrcType = false;
                    break;
                case Core.CollectType.Src:
                    CurrentColumnContentType = false;
                    CurrentColumnHrefType = false;
                    CurrentColumnSrcType = true;
                    break;
                default:
                    CurrentColumnContentType = true;
                    CurrentColumnHrefType = false;
                    CurrentColumnSrcType = false;
                    break;
            }
        }

        /// <summary>
        /// 获取文本内容
        /// </summary>
        /// <param name="objectValue"></param>
        public void GetTextContentCommandExcute(object objectValue)
        {
            try
            {
                MultiValueData _MultiValueData = objectValue as MultiValueData;
                string _ColumnName = _MultiValueData.ColName;
                CurrentColumnIndex = this.RealDataTable.GetColIndexByColName(_ColumnName);

                if (GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType == Core.CollectType.Content)
                    return;
                //DataTableHelper.GetColumnIndexbyName(this.RealDataTable.CDataTable, _ColumnName);
                //赋值内容属性 以应用于执行 
                GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType = Core.CollectType.Content;
                GlobalData.GetInstance().VM_MainWindowViewModel.ReloadGridData();
                //var compiledSRetModel = DataCollectingManager.DoStringCompile(new ParamDoStringCompileModel
                //{
                //    Text = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.ToJson(),
                //    ParamsStr = GlobalData.GetInstance().VM_MainWindowViewModel.ParamDataCollectConfigurationModel.GlobalVariable
                //});

                //string _RegularRegex = compiledSRetModel.collectInfo.data[CurrentColumnIndex].GetRegularRegex(Core.CollectType.Content);


                //List<List<string>> _ResultOfDataCollection = GlobalData.GetInstance().VM_MainWindowViewModel.GetDataCollectionBySelector(compiledSRetModel.collectInfo.data[CurrentColumnIndex].selector, false, Core.CollectType.Content, _RegularRegex);

                ////赋值内容属性 以应用于执行 
                //GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType = Core.CollectType.Content;

                //if (_ResultOfDataCollection.Count == 0)
                //{
                //    this.RealDataTable.EmptyColumn(CurrentColumnIndex);
                //    //DymDataTable = RealDataTable.CDataTable;
                //    RefreshDataGrid(0);
                //}
                //else
                //{
                //    this.RealDataTable.ReWriteColumn(CurrentColumnIndex, _ResultOfDataCollection[0]);
                //    //DymDataTable = RealDataTable.CDataTable;
                //    RefreshDataGrid(0);
                //}
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "获取文本内容出错");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_HuoQuWenBenNeiRongYiChang.GetMessage() );
            }
        }

        /// <summary>
        /// 获取链接
        /// </summary>
        /// <param name="objectValue"></param>
        public void GetLinkCommandExcute(object objectValue)
        {
            try
            {
                //MessageBox.Show(eCommonMessageType.DataGridViewModel_cs_HuoQuLianJieYiChang.GetMessage());

                MultiValueData _MultiValueData = objectValue as MultiValueData;
                string _ColumnName = _MultiValueData.ColName;
                CurrentColumnIndex = this.RealDataTable.GetColIndexByColName(_ColumnName);
                if (GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType == Core.CollectType.Href)
                    return;
                //赋值内容属性 以应用于执行 
                GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType = Core.CollectType.Href;
                GlobalData.GetInstance().VM_MainWindowViewModel.ReloadGridData();

                //var compiledSRetModel = DataCollectingManager.DoStringCompile(new ParamDoStringCompileModel
                //{
                //    Text = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.ToJson(),
                //    ParamsStr = GlobalData.GetInstance().VM_MainWindowViewModel.ParamDataCollectConfigurationModel.GlobalVariable
                //});

                ////List<SelectorProps> _SelectorProps = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].selector;
                //string _RegularRegex = compiledSRetModel.collectInfo.data[CurrentColumnIndex].GetRegularRegex(Core.CollectType.Href);
                //List<List<string>> _ResultOfDataCollection = GlobalData.GetInstance().VM_MainWindowViewModel.GetDataCollectionBySelector(compiledSRetModel.collectInfo.data[CurrentColumnIndex].selector,
                //    false, Core.CollectType.Href, _RegularRegex);

                ////赋值内容属性 以应用于执行 
                //GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType = Core.CollectType.Href;

                //if (_ResultOfDataCollection.Count == 0)
                //{
                //    this.RealDataTable.EmptyColumn(CurrentColumnIndex);
                //    //DymDataTable = RealDataTable.CDataTable;
                //    RefreshDataGrid(0);
                //}
                //else
                //{
                //    this.RealDataTable.ReWriteColumn(CurrentColumnIndex, _ResultOfDataCollection[0]);
                //    //DymDataTable = RealDataTable.CDataTable;
                //    RefreshDataGrid(0);
                //}
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"获取链接异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_HuoQuLianJieYiChang.GetMessage());
            }
        }

        /// <summary>
        /// 获取图片路径
        /// </summary>
        /// <param name="objectValue"></param>
        public void GetImageSrcCommandExcute(object objectValue)
        {
            try
            {
                MultiValueData _MultiValueData = objectValue as MultiValueData;
                string _ColumnName = _MultiValueData.ColName;
                CurrentColumnIndex = this.RealDataTable.GetColIndexByColName(_ColumnName);
                if (GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType == Core.CollectType.Src)
                    return;
                //List<SelectorProps> _SelectorProps = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].selector;
                //赋值内容属性 以应用于执行 
                GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType = Core.CollectType.Src;
                GlobalData.GetInstance().VM_MainWindowViewModel.ReloadGridData();
                //var compiledSRetModel = DataCollectingManager.DoStringCompile(new ParamDoStringCompileModel
                //{
                //    Text = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.ToJson(),
                //    ParamsStr = GlobalData.GetInstance().VM_MainWindowViewModel.ParamDataCollectConfigurationModel.GlobalVariable
                //});

                //string _RegularRegex = compiledSRetModel.collectInfo.data[CurrentColumnIndex].GetRegularRegex(Core.CollectType.Src);
                //List<List<string>> _ResultOfDataCollection = GlobalData.GetInstance().VM_MainWindowViewModel.GetDataCollectionBySelector(compiledSRetModel.collectInfo.data[CurrentColumnIndex].selector,
                //    false, Core.CollectType.Src, _RegularRegex);
                ////赋值内容属性 以应用于执行 
                //GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType = Core.CollectType.Src;

                //if (_ResultOfDataCollection.Count == 0)
                //{
                //    this.RealDataTable.EmptyColumn(CurrentColumnIndex);
                //    //DymDataTable = RealDataTable.CDataTable;
                //    RefreshDataGrid(0);
                //}
                //else
                //{
                //    this.RealDataTable.ReWriteColumn(CurrentColumnIndex, _ResultOfDataCollection[0]);
                //    //DymDataTable = RealDataTable.CDataTable;
                //    RefreshDataGrid(0);
                //}
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "获取图片内容失败");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_HuoQuTuPianNeiRongShiBai.GetMessage() );
            }
        }

        /// <summary>
        /// 编辑列元素
        /// </summary>
        /// <param name="objectValue"></param>
        public void EditColumnSelectorCommandExcute(object objectValue)
        {
            try
            {
                MultiValueData _MultiValueData = objectValue as MultiValueData;
                string _ColumnName = _MultiValueData.ColName;
                CurrentColumnIndex = this.RealDataTable.GetColIndexByColName(_ColumnName);

                GlobalData.GetInstance().VM_MainWindowViewModel.ShowLoadWaiting(true, "EditColumnSelectorCommandExcute");
                List<SelectorProps> _SelectorProps = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].selector;
                string _guiType = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.pageInfo.guiType;
                string _filePath = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].filePath;
                ParamDoElementEditorModel _ParamDoElementEditorModel = new ParamDoElementEditorModel
                {
                    guiType = _guiType,
                    filePath = _filePath,
                    selector = _SelectorProps.ToJson(),
                    isSimilar = true
                };

                GlobalData.GetInstance().VM_MainWindowViewModel.SetWindowState(true);
                Task.Factory.StartNew(() =>
                {
                    EditColumn(_ParamDoElementEditorModel, false);
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "编辑列元素失败");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_BianJiLieYuanSuShiBai.GetMessage());
                GlobalData.GetInstance().VM_MainWindowViewModel.SetWindowState(false);
                GlobalData.GetInstance().VM_MainWindowViewModel.ShowLoadWaiting(false, "EditColumnSelectorCommandExcute");
            }
        }

        /// <summary>
        /// 编辑列元素后续操作
        /// </summary>
        /// <param name="_ParamDoElementEditorModel"></param>
        public void EditColumn(ParamDoElementEditorModel _ParamDoElementEditorModel, bool _IsTable)
        {
            DataCollectingManager.StopCapture();
            try
            {
                List<SelectorProps> _RetSelector = DataCollectingManager.DoElementEditor(_ParamDoElementEditorModel);

                if (_RetSelector == null || _RetSelector.Count == 0)
                {
                    return;
                }
                int editColumnIdx = -1;
                if (_IsTable)
                {
                    GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[0].selector = _RetSelector;
                    editColumnIdx = 0;
                }
                else
                {
                    GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].selector = _RetSelector;
                    editColumnIdx = CurrentColumnIndex;
                }
                GlobalData.GetInstance().VM_MainWindowViewModel.ReloadGridData(editColumnIdx);

                ////编译数据中包含的变量
                //var compiledSRetModel = DataCollectingManager.DoStringCompile(new ParamDoStringCompileModel
                //{
                //    Text = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.ToJson(),
                //    ParamsStr = GlobalData.GetInstance().VM_MainWindowViewModel.ParamDataCollectConfigurationModel.GlobalVariable
                //});
                //var compiledSelector = compiledSRetModel.collectInfo.data[CurrentColumnIndex].selector;



                //string _RegularRegex = compiledSRetModel.collectInfo.data[CurrentColumnIndex].GetRegularRegex();

                //List<List<string>> _ResultOfDataCollection = GlobalData.GetInstance().VM_MainWindowViewModel.GetDataCollectionBySelector(compiledSelector, _IsTable,
                //    GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType, _RegularRegex);

                //if (_IsTable)
                //{
                //    DataTableHelper.ReWriteTable(ref RealDataTable.CDataTable, _ResultOfDataCollection);
                //    //DymDataTable = RealDataTable.CDataTable;
                //    RefreshDataGrid(0);
                //}
                //else
                //{
                //    if (_ResultOfDataCollection.Count == 0)
                //    {
                //        this.RealDataTable.EmptyColumn(CurrentColumnIndex);
                //        //this.RealDataTable.EmptyColumn(DymDataTable, CurrentColumnIndex);
                //        //DymDataTable = RealDataTable.CDataTable;
                //        RefreshDataGrid(0);
                //    }
                //    else
                //    {
                //        this.RealDataTable.ReWriteColumn(CurrentColumnIndex, _ResultOfDataCollection[0]);
                //        //DymDataTable = RealDataTable.CDataTable;
                //        RefreshDataGrid(0);
                //    }
                //}
            }
            catch (Exception ex)
            {
                if (_IsTable)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "编辑表格元素失败");
                    MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_BianJiBiaoGeYuanSuShiBai.GetMessage() );
                }
                else
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "编辑列元素失败");
                    MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_BianJiLieYuanSuShiBai.GetMessage());
                }

                GlobalData.GetInstance().VM_MainWindowViewModel.ShowLoadWaiting(false, "EditColumn");
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
        /// 编辑正则命令的实现
        /// </summary>
        /// <param name="objectValue"></param>
        public void EditRegularExpressionCommandExcute(object objectValue)
        {
            try
            {
                MultiValueData _MultiValueData = objectValue as MultiValueData;
                string _ColumnName = _MultiValueData.ColName;
                CurrentColumnIndex = this.RealDataTable.GetColIndexByColName(_ColumnName);

                var ColumnDataConfig = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex];

                //先检测是否当前内容设置已经匹配了正则
                RegularConfig _RegularConfig = null;
                bool HasSetRegularExpress = ColumnDataConfig.GetRegularRegex(ref _RegularConfig);

                ColumnRegexInfo _ColumnRegexInfo = null;
                if (HasSetRegularExpress)
                {
                    _ColumnRegexInfo = new ColumnRegexInfo(false, CurrentColumnIndex, _RegularConfig.regular);
                }
                else
                {
                    _ColumnRegexInfo = new ColumnRegexInfo(false, CurrentColumnIndex, null);
                }

                //弹窗 设置正则
                //GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType

                ////赋值内容属性 以应用于执行 
                //GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].dataType = Core.CollectType.Href;

                EditRegularExpressWnd __EditColumnHeaderWnd = new EditRegularExpressWnd();
                __EditColumnHeaderWnd.SetColumnRegexConfig(_ColumnRegexInfo, SetColumnRegex);
                if (MessageHelper.GetParentWnd() != null)
                {
                    MessageHelper.ShowShadow(true);
                    __EditColumnHeaderWnd.Owner = MessageHelper.GetParentWnd();
                }
                MessageHelper.SetCurrentDialogWnd(__EditColumnHeaderWnd);
                __EditColumnHeaderWnd.ShowDialog();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "编辑正则异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_BianJiZhengZeYiChang.GetMessage());
            }
            finally
            {
                MessageHelper.ShowShadow(false);
            }
        }

        /// <summary>
        /// 修改正则的实现
        /// </summary>
        /// <param name="_ColumnHeaderInfo"></param>
        private void SetColumnRegex(ColumnRegexInfo _ColumnRegexInfoParam)
        {
            if (!_ColumnRegexInfoParam.IsChanged)
            {
                return;
            }

            if (_ColumnRegexInfoParam.ColumnIndex >= GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data?.Count
                || _ColumnRegexInfoParam.ColumnIndex >= RealDataTable.GetCurrentColCount())
            {
                return;
            }

            string _RegexValue = _ColumnRegexInfoParam.CurrentRegex.Trim();

            GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[_ColumnRegexInfoParam.ColumnIndex].SetRegular(_RegexValue);
            GlobalData.GetInstance().VM_MainWindowViewModel.ReloadGridData();


            //var compiledSRetModel = DataCollectingManager.DoStringCompile(new ParamDoStringCompileModel
            //{
            //    Text = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.ToJson(),
            //    ParamsStr = GlobalData.GetInstance().VM_MainWindowViewModel.ParamDataCollectConfigurationModel.GlobalVariable
            //});

            //List<List<string>> _ResultOfDataCollection = GlobalData.GetInstance().VM_MainWindowViewModel.GetDataCollectionBySelector(compiledSRetModel.collectInfo.data[CurrentColumnIndex].selector,
            //    false, compiledSRetModel.collectInfo.data[CurrentColumnIndex].dataType, _RegexValue);

            //if (_ResultOfDataCollection.Count == 0)
            //{
            //    this.RealDataTable.EmptyColumn(CurrentColumnIndex);
            //    RefreshDataGrid(0);
            //}
            //else
            //{
            //    this.RealDataTable.ReWriteColumn(CurrentColumnIndex, _ResultOfDataCollection[0]);
            //    RefreshDataGrid(0);
            //}
        }



        /// <summary>
        /// 拾取相似元素
        /// </summary>
        /// <param name="objectValue"></param>
        public void GetSimulateItemCommandExcute(object objectValue)
        {
            MultiValueData _MultiValueData = objectValue as MultiValueData;
            string _ColumnName = _MultiValueData.ColName;
            CurrentColumnIndex = DataTableHelper.GetColumnIndexbyName(this.RealDataTable.CDataTable, _ColumnName);
            GlobalData.GetInstance().VM_MainWindowViewModel.SetWindowState(true);
            Task.Factory.StartNew(() =>
            {
                try
                {
                    //拾取相似元素
                    List<SelectorProps> _SelectorPropsList = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[CurrentColumnIndex].lastCaptureSelector;
                    GlobalData.GetInstance().VM_MainWindowViewModel.StartCaptureCollectDataElement(false, Core.CaptureElementType.SimilarElement, _SelectorPropsList);
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "拾取相似元素失败");
                    MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_ShiQuXiangSiYuanSuYiChang.GetMessage());
                    GlobalData.GetInstance().VM_MainWindowViewModel.SetWindowState(false);
                }
            });

        }

        /// <summary>
        /// 编辑列名
        /// </summary>
        /// <param name="objectValue"></param>
        public void EditColumnHeaderCommandExcute(object objectValue)
        {
            try
            {
                MultiValueData _MultiValueData = objectValue as MultiValueData;
                string _ColumnName = _MultiValueData.ColName;

                CurrentColumnIndex = DataTableHelper.GetColumnIndexbyName(RealDataTable.CDataTable, _ColumnName);

                string _ColumnNameTail = string.Empty;
                string _ColumnID = GetColumnID(_ColumnName, ref _ColumnNameTail);
                ColumnHeaderInfo _ColumnHeaderInfo = new ColumnHeaderInfo(false, _ColumnID, CurrentColumnIndex, _ColumnNameTail);

                EditColumnHeaderWnd _EditColumnHeaderWnd = new EditColumnHeaderWnd();
                _EditColumnHeaderWnd.SetColumnInfo(_ColumnHeaderInfo, SetColumnName);
                if (MessageHelper.GetParentWnd() != null)
                {
                    MessageHelper.ShowShadow(true);
                    _EditColumnHeaderWnd.Owner = MessageHelper.GetParentWnd();
                }
                MessageHelper.SetCurrentDialogWnd(_EditColumnHeaderWnd);
                _EditColumnHeaderWnd.ShowDialog();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "编辑表头失败");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_BianJiBiaoTouYiChang.GetMessage());
            }
            finally
            {
                MessageHelper.ShowShadow(false);
            }
        }

        

        /// <summary>
        /// 普通表格 编辑列名
        /// </summary>
        /// <param name="objectValue"></param>
        public void CommonTable_EditColumnHeaderCommandExcute(string _ColumnName)
        {
            try
            {
                if (GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data.Count != 1)
                {
                    logger.LogError("当前普通表格无配置数据");
                    MessageHelper.ShowWeakError(eCommonMessageType.DataGridViewModel_cs_PeiZhiXinXiYiChangQingChaKan.GetMessage(), true);
                    return;
                }

                if (GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[0].CommonTalbleTitleList.Count == 0)
                {
                    int ColCount = RealDataTableForPureTable.GetCurrentColCount();
                    for (int i = 0; i < ColCount; i++)
                    {
                        GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[0].CommonTalbleTitleList.Add(new ColNameStruct());
                    }
                }

                //MultiValueData _MultiValueData = objectValue as MultiValueData;
                //string _ColumnName = _MultiValueData.ColName;

                CurrentColumnIndex = DataTableHelper.GetColumnIndexbyName(RealDataTableForPureTable.CDataTable, _ColumnName);

                string _ColumnNameTail = string.Empty;
                string _ColumnID = GetColumnID(_ColumnName, ref _ColumnNameTail);
                ColumnHeaderInfo _ColumnHeaderInfo = new ColumnHeaderInfo(false, _ColumnID, CurrentColumnIndex, _ColumnNameTail);

                EditColumnHeaderWnd _EditColumnHeaderWnd = new EditColumnHeaderWnd();
                _EditColumnHeaderWnd.SetColumnInfo(_ColumnHeaderInfo, CommonTable_SetColumnName);
                if (MessageHelper.GetParentWnd() != null)
                {
                    MessageHelper.ShowShadow(true);
                    _EditColumnHeaderWnd.Owner = MessageHelper.GetParentWnd();
                }
                MessageHelper.SetCurrentDialogWnd(_EditColumnHeaderWnd);
                _EditColumnHeaderWnd.ShowDialog();
            }
            catch (Exception ex)
            {
                logger.LogError($"编辑表头失败  Info：{ex}");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_BianJiBiaoTouYiChang.GetMessage());
            }
            finally
            {
                MessageHelper.ShowShadow(false);
            }
        }

        public string GetColumnID(string _ColumnName, ref string _ColumnNameTail)
        {
            string _ColumnID = string.Empty;
            _ColumnNameTail  = string.Empty;
            if (_ColumnName.Contains(' '))
            {
                _ColumnID = _ColumnName.Split(' ')[0].ToString();
                _ColumnNameTail = _ColumnName.Substring(_ColumnName.IndexOf(" ") + 1);
            }
            else
            {
                _ColumnID = _ColumnName;
            }

            return _ColumnID;
        }

        /// <summary>
        /// 修改列名Action的实现
        /// </summary>
        /// <param name="_ColumnHeaderInfo"></param>
        private void SetColumnName(ColumnHeaderInfo _ColumnHeaderInfo)
        {
            string _CurrentWholeColumnName = _ColumnHeaderInfo.CurrentName.Trim();

            GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[_ColumnHeaderInfo.ColumnIndex].title = _CurrentWholeColumnName;
            if (string.IsNullOrWhiteSpace(_CurrentWholeColumnName))
            {
               RealDataTable.CDataTable.Columns[_ColumnHeaderInfo.ColumnIndex].ColumnName = $"{_ColumnHeaderInfo.ColumnID}";
            }
            else
            {
                RealDataTable.CDataTable.Columns[_ColumnHeaderInfo.ColumnIndex].ColumnName = $"{_ColumnHeaderInfo.ColumnID} {_CurrentWholeColumnName}";
            } 
            //DymDataTable = RealDataTable.CDataTable;
            RefreshDataGrid(0);
        }

        /// <summary>
        /// 修改列名Action的实现
        /// </summary>
        /// <param name="_ColumnHeaderInfo"></param>
        private void CommonTable_SetColumnName(ColumnHeaderInfo _ColumnHeaderInfo)
        {
            if (!_ColumnHeaderInfo.IsChanged)
            {
                return;
            }

            int NameListCount = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[0].CommonTalbleTitleList.Count;

            if (_ColumnHeaderInfo.ColumnIndex >= NameListCount
                || _ColumnHeaderInfo.ColumnIndex >= RealDataTableForPureTable.GetCurrentColCount())
            {
                return;
            }

            int _SelectorCount = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[0].CommonTalbleTitleList.Count;

            if (_ColumnHeaderInfo.ColumnIndex >= _SelectorCount
                || _ColumnHeaderInfo.ColumnIndex >= RealDataTableForPureTable.GetCurrentColCount())
            {
                logger.LogError("列索引超出范围");
                MessageHelper.ShowWeakError(eCommonMessageType.DataGridViewModel_cs_PeiZhiXinXiYiChangQingChaKan.GetMessage(), true);
                return;
            }

            string _CurrentWholeColumnName = _ColumnHeaderInfo.CurrentName.Trim();

            if (string.IsNullOrWhiteSpace(_CurrentWholeColumnName))
            {
                RealDataTableForPureTable.CDataTable.Columns[_ColumnHeaderInfo.ColumnIndex].ColumnName = $"{_ColumnHeaderInfo.ColumnID}";
                GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[0].CommonTalbleTitleList[_ColumnHeaderInfo.ColumnIndex] = new ColNameStruct(0, string.Empty);
            }
            else
            {
                RealDataTableForPureTable.CDataTable.Columns[_ColumnHeaderInfo.ColumnIndex].ColumnName = $"{_ColumnHeaderInfo.ColumnID} {_CurrentWholeColumnName.Trim()}";
                GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[0].CommonTalbleTitleList[_ColumnHeaderInfo.ColumnIndex] = new ColNameStruct(1, _CurrentWholeColumnName.Trim());
            }

            RefreshDataGrid(1);
        }


        /// <summary>
        /// 前移一列
        /// </summary>
        /// <param name="objectValue"></param>
        public void MoveForewardCommandExcute(object objectValue)
        {
            MultiValueData _MultiValueData = objectValue as MultiValueData;
            string _ColumnName = _MultiValueData.ColName;

            CurrentColumnIndex = DataTableHelper.GetColumnIndexbyName(this.RealDataTable.CDataTable, _ColumnName);
            if (CurrentColumnIndex == 0)
            {
                return;
            }

            try
            {
                //RealDataTable.CDataTable.Columns[CurrentColumnIndex].SetOrdinal(CurrentColumnIndex - 1);
                RealDataTable.SetOrdinal(CurrentColumnIndex, CurrentColumnIndex - 1);
                GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.SetOrder(CurrentColumnIndex, true);

                //DataTableHelper.ReGenerateColumnName(RealDataTable.CDataTable);
                //DataTableHelper.RenameTitles(GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo);
                List<string> RealNameList = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.GenerateAllColNameList();
                DataTableHelper.ReGenerateColumnNameEx(RealDataTable.CDataTable, RealNameList);
                //DymDataTable = RealDataTable.CDataTable;
                RefreshDataGrid(0);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "前移异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_CaoZuoYiChang.GetMessage() );
            }
        }

        /// <summary>
        /// 后移一列
        /// </summary>
        /// <param name="objectValue"></param>
        public void MoveBackwardCommandExcute(object objectValue)
        {
            MultiValueData _MultiValueData = objectValue as MultiValueData;
            string _ColumnName = _MultiValueData.ColName;
            CurrentColumnIndex = this.RealDataTable.GetColIndexByColName(_ColumnName);
            //CurrentColumnIndex = DataTableHelper.GetColumnIndexbyName(this.RealDataTable, _ColumnName);

            if (CurrentColumnIndex == this.RealDataTable.GetCurrentColCount() - 1)
            {
                return;
            }

            try
            {
                RealDataTable.SetOrdinal(CurrentColumnIndex, CurrentColumnIndex + 1);
                GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.SetOrder(CurrentColumnIndex, false);
                //DataTableHelper.ReGenerateColumnName(RealDataTable.CDataTable);
                //DataTableHelper.RenameTitles(GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo);
                List<string> RealNameList = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.GenerateAllColNameList();
                DataTableHelper.ReGenerateColumnNameEx(RealDataTable.CDataTable, RealNameList);
                RefreshDataGrid(0);
                //DymDataTable = RealDataTable.CDataTable;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "后移异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_CaoZuoYiChang.GetMessage());
            }
        }

        /// <summary>
        /// 删除列
        /// </summary>
        /// <param name="obj"></param>
        private void DeleteColumnCommandExcute(object objectValue)
        {
            MultiValueData _multiValueData = objectValue as MultiValueData;
            string _ColumnName = _multiValueData.ColName;
            //Popup _Popup = _multiValueData.PopupEx;
            //_Popup.Visibility = Visibility.Collapsed;
            //CurrentColumnIndex = DataTableHelper.GetColumnIndexbyName(this.RealDataTable, _ColumnName);
            CurrentColumnIndex = this.RealDataTable.GetColIndexByColName(_ColumnName);

            try
            {
                Grid _Grid = _multiValueData.GridEx;
                _Grid.Visibility = Visibility.Collapsed;
                string ColName = _multiValueData.ColName;

                if (RealDataTable.GetCurrentColCount() <= CurrentColumnIndex)
                {
                    logger.LogError($"删除列元素 序号:{CurrentColumnIndex} 超出范围");
                    MessageHelper.ShowWeakError("列序号超范围 删除失败".I18n("DataGridViewModel_DD00D9F8"));
                    return;
                }

                //RealDataTable.Columns.RemoveAt(CurrentColumnIndex);
                RealDataTable.RemoveAt(CurrentColumnIndex);
                //删除当前数据配置
                GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data.RemoveAt(CurrentColumnIndex);

                //DataTableHelper.ReGenerateColumnName(RealDataTable.CDataTable);
                //DataTableHelper.RenameTitles(GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo);
                List<string> RealNameList = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.GenerateAllColNameList();
                DataTableHelper.ReGenerateColumnNameEx(RealDataTable.CDataTable, RealNameList);

                GlobalData.GetInstance().VM_MainWindowViewModel.SetColumnWidth(RealDataTable.GetCurrentColCount(), 0);


                if (RealDataTable.GetCurrentColCount() == 0)
                {
                    DymDataTable = null;
                    GlobalData.GetInstance().VM_MainWindowViewModel.InitialMaxLineCount();
                }
                else
                {
                    //RefreshDataGrid(0);
                    //DymDataTable = RealDataTable.CDataTable;
                    GlobalData.GetInstance().VM_MainWindowViewModel.ReloadGridData();
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "删除列异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_CaoZuoYiChang.GetMessage());
            }
        }

        /// <summary>
        /// 处理表格或者列元素预览数据展示
        /// </summary>
        /// <param name="ColumnContentList">列表内容集合</param>
        /// <param name="isAddNewColumn">区分新增列还是拾取相似元素</param>
        /// <param name="isTableData">是否是普通表格(不带列编辑的那种)</param>
        public void DealCaptureBySelectorResult(List<List<string>> ColumnContentList, bool isAddNewColumn, bool isTableData = false)
        {
            //元素是否为空
            bool _IsContentEmpty = false;

            if (ColumnContentList == null || ColumnContentList.Count == 0 || ColumnContentList[0].Count == 0)//显示列名
            {
                _IsContentEmpty = true;
            }

            try
            {
                if (!isTableData)//非table
                {
                    ChangeDataView(DataGridViewType.ColumnDataView);
                    if (isAddNewColumn)//新增
                    {
                        //更新列宽配置
                        GlobalData.GetInstance().VM_MainWindowViewModel.SetColumnWidth(RealDataTable.GetCurrentColCount() + 1, 0);
                        if (!_IsContentEmpty)
                        {
                            AddNewColumnToCurrentData(ColumnContentList[0]);
                        }
                        else
                        {
                            AddNewColumnToCurrentData(new List<string>());
                        }
                    }
                    else //编辑改变列
                    {
                        if (!_IsContentEmpty)
                        {
                            ReWriteColumnToCurrentData(ColumnContentList[0]);
                        }
                        else
                        {
                            ReWriteColumnToCurrentData(new List<string>());
                        }
                    }
                }
                else
                {
                    if (ColumnContentList == null || ColumnContentList.Count == 0 || ColumnContentList[0].Count == 0)//空表格
                    {
                        GlobalData.GetInstance().VM_MainWindowViewModel.SetColumnWidth(0, 1);
                    }
                    else
                    {
                        int iColumnCount = ColumnContentList[0].Count;
                        //更新列宽配置
                        GlobalData.GetInstance().VM_MainWindowViewModel.SetColumnWidth(iColumnCount, 1);
                    }

                    InitDataView(DataGridViewType.TableDataView);
                    //IsCommonColumnsData = false;
                    //全部重写Table
                    ReWriteTableForCurrentData(ColumnContentList,new List<ColNameStruct>());
                    GlobalData.GetInstance().VM_MainWindowViewModel.ShowLoadWaiting(false, "DealCaptureBySelectorResult 003");
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "处理表格或者列元素预览数据展示异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_ShuJuJiaZaiYiChang.GetMessage());
                GlobalData.GetInstance().VM_MainWindowViewModel.ShowLoadWaiting(false, "DealCaptureBySelectorResult 004");
            }
            finally
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(500);
                    GlobalData.GetInstance().VM_MainWindowViewModel.StartCaptureCollectDataElement(false);
                });
            }
        }

        /// <summary>
        /// 切换当前数据预览页面显示
        /// </summary>
        /// <param name="_DataGridViewType"></param>
        public void InitDataView(DataGridViewType _DataGridViewType)
        {
            try
            {
                if (_DataGridViewType == DataGridViewType.ColumnDataView)///列元素界面
                {
                    ColumnsDataVisibility = Visibility.Visible;
                    TableDataVisibility = Visibility.Collapsed;
                    RealDataTableForPureTable = new RealDataStruct();
                    DymTable = new DataTable();
                }
                else
                {
                    TableDataVisibility = Visibility.Visible;
                    ColumnsDataVisibility = Visibility.Collapsed;
                    RealDataTable = new RealDataStruct();
                    DymDataTable = new DataTable();
                }
                InitPanelVisibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "切换当前数据预览页面显示异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_ShuJuJiaZaiChuXianYiChang.GetMessage() );
            }
        }

        /// <summary>
        /// 切换当前数据预览页面显示
        /// </summary>
        /// <param name="_DataGridViewType"></param>
        public void ChangeDataView(DataGridViewType _DataGridViewType)
        {
            try
            {
                if (_DataGridViewType == DataGridViewType.ColumnDataView)///列元素界面
                {
                    ColumnsDataVisibility = Visibility.Visible;
                    TableDataVisibility = Visibility.Collapsed;
                    InitPanelVisibility = Visibility.Collapsed;
                }
                else if (_DataGridViewType == DataGridViewType.TableDataView)
                {
                    TableDataVisibility = Visibility.Visible;
                    ColumnsDataVisibility = Visibility.Collapsed;
                    InitPanelVisibility = Visibility.Collapsed;
                }
                else
                {
                    InitPanelVisibility = Visibility.Visible;
                    TableDataVisibility = Visibility.Collapsed;
                    ColumnsDataVisibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "切换当前数据预览页面显示异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_ShuJuJiaZaiChuXianYiChang.GetMessage());
            }
        }

        /// <summary>
        /// 增加数据列
        /// </summary>
        /// <param name="strContentList"></param>
        private void AddNewColumnToCurrentData(List<string> strContentList)
        {
            this.RealDataTable.AddNewColumn(strContentList, "");
            RefreshDataGrid(0);
        }


        /// <summary>
        /// 增加数据列 非table
        /// </summary>
        /// <param name="strContentList"></param>
        public void AddNewColumnsToCurrentData(List<ColumnData> _ColumnDataList, bool IsNeedEmpty = false, bool IsNeedScrollToEnd = false)
        {
            if (IsNeedEmpty)
            {
                this.RealDataTable = new RealDataStruct();
            }

            for (int i = 0; i < _ColumnDataList.Count; i++)
            {
                this.RealDataTable.AddNewColumn(_ColumnDataList[i].ColContentList, _ColumnDataList[i].ColName);
            }

            ChangeDataView(DataGridViewType.ColumnDataView);
            RefreshDataGrid(0);
        }

        /// <summary>
        /// 重新生成当前普通表格
        /// </summary>
        /// <param name="ColumnContentList">列集合数据</param>
        public void ReGeneratPureTable(List<List<string>> ColumnContentList, List<ColNameStruct> ColNameList)
        {
            if (ColumnContentList == null || ColumnContentList.Count == 0 || ColumnContentList[0].Count == 0)//空表格
            {
                GlobalData.GetInstance().VM_MainWindowViewModel.SetColumnWidth(0, 1);
            }
            else
            {
                int iColumnCount = ColumnContentList[0].Count;
                //更新列宽配置
                GlobalData.GetInstance().VM_MainWindowViewModel.SetColumnWidth(iColumnCount, 1);
            }

            InitDataView(DataGridViewType.TableDataView);
            //IsCommonColumnsData = false;
            //全部重写Table
            ReWriteTableForCurrentData(ColumnContentList, ColNameList);
        }

        /// <summary>
        /// 覆盖数据列
        /// </summary>
        /// <param name="strContentList"></param>
        private void ReWriteColumnToCurrentData(List<string> strContentList)
        {
            this.RealDataTable.ReWriteColumn(CurrentColumnIndex, strContentList);
            RefreshDataGrid(0);
            //DymDataTable = RealDataTable.CDataTable;
        }

        /// <summary>
        /// 重写Table
        /// </summary>
        /// <param name="strContentList"></param>
        private void ReWriteTableForCurrentData(List<List<string>> strContentList, List<ColNameStruct> ColNameList)
        {
            DataTableHelper.ReWriteTable(ref RealDataTableForPureTable.CDataTable, strContentList, ColNameList);
            //GlobalData.GetInstance().VM_MainWindowViewModel.RefreshDataGridItems();
            //DymTable = RealDataTableForPureTable.CDataTable;
            RefreshDataGrid(1);
        }

        /// <summary>
        /// 列元素表格当前列数
        /// </summary>
        int CurrentColCount = 0;

        /// <summary>
        /// 重置列表
        /// </summary>
        public void RefreshDataGrid(int iDataGridType)
        {
            if (iDataGridType == 0)//普通列元素
            {
                //DymDataTable = null;
                //GlobalData.GetInstance().VM_MainWindowViewModel.RefreshDataGridItems();
                //if(DymDataTable == null)
                DymDataTable = RealDataTable.CDataTable;
                GlobalData.GetInstance().VM_MainWindowViewModel.RefreshDataGridItems();
                if (RealDataTable.GetCurrentColCount() >= 5 && RealDataTable.GetCurrentColCount() > CurrentColCount)
                {
                    GlobalData.GetInstance().VM_MainWindowViewModel.ScrollToEnd(true);
                }
                CurrentColCount = RealDataTable.GetCurrentColCount();
                //logger.LogDebug("滚动");
            }
            else//表格
            {
                //DymTable = null;
                //GlobalData.GetInstance().VM_MainWindowViewModel.RefreshDataGridItems();
                DymTable = RealDataTableForPureTable.CDataTable;
                GlobalData.GetInstance().VM_MainWindowViewModel.RefreshDataGridItems();
            }

            if (GlobalData.GetInstance().VM_MainWindowViewModel.IsTable)
            {
                int RowCount1 = DymTable == null ? 0 : DymTable.Rows.Count;
                GlobalData.GetInstance().VM_MainWindowViewModel.SetMaxLineCount(RowCount1);
            }
            else
            {
                int RowCount2 = DymDataTable == null ? 0 : DymDataTable.Rows.Count;
                GlobalData.GetInstance().VM_MainWindowViewModel.SetMaxLineCount(RowCount2);
            }
        }


        /// <summary>
        /// 清空Col列表显示
        /// </summary>
        public void EmptyColDataGrid()
        {
            RealDataTable = new RealDataStruct();
            RefreshDataGrid(0);
            //DymDataTable = RealDataTable.CDataTable;
        }

        /// <summary>
        /// 清空Table显示
        /// </summary>
        public void EmptyTableDataGrid()
        {
            RealDataTableForPureTable = new RealDataStruct();
            //DymTable = RealDataTableForPureTable.CDataTable;
            RefreshDataGrid(1);
        }


        #endregion

        #region 其余操作
        /// <summary>
        /// 展示流程帮助pdf
        /// </summary>
        public void DataVerifyCommandExcute(object objValue)
        {
            string HtmlAbsolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HelpInfo.html");
            Process.Start(HtmlAbsolutePath);

            //数据埋点
            GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.help_connect++;
        }

        /// <summary>
        /// 清空数据
        /// </summary>
        public void EmptyDataGridCommandExcute(object objValue)
        {
            try
            {
                //清空预览数据
                EmptyViewData();
                //清空配置数据
                GlobalData.GetInstance().VM_MainWindowViewModel.EmptyReturnConfigData();
                ChangeDataView(DataGridViewType.InitView);
                //数据埋点
                GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.clear_connect++;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "切换当前数据预览页面显示异常");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_ShuJuJiaZaiChuXianYiChang.GetMessage());
            }
            finally
            {
                Task.Factory.StartNew(() =>
                {
                    GlobalData.GetInstance().VM_MainWindowViewModel.StartCaptureCollectDataElement(true);
                });
            }
        }

        /// <summary>
        /// 刷新当前列表数据
        /// </summary>
        /// <param name="ObjValue"></param>
        public async void RefreshDataGridCommandExcute(object ObjValue)
        {
            try
            {
                DataCollectingManager.StopCapture();
                var task = Task.Run(() =>
                {
                    GlobalData.GetInstance().VM_MainWindowViewModel.ReloadGridData();
                    //数据埋点
                    GlobalData.GetInstance().VM_MainWindowViewModel.CDataBuriedModel_DataCollect.refresh_connect++;
                });
                await task;
                DataCollectingManager.StartElementCapture(GlobalData.GetInstance().VM_MainWindowViewModel.CParamStartCollectElementCaptureModel);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "刷新数据异常");
                MessageHelper.ShowWeakError(eCommonMessageType.DataGridViewModel_cs_ShuaXinShuJuYiChang.GetMessage());
            }
        }

        /// <summary>
        /// 编辑表格selector
        /// </summary>
        public void EditTableSelectorCommandExcute(object objValue)
        {
            int iCount = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data.Count;

            if (iCount == 0)
            {
                return;
            }

            try
            {
                List<SelectorProps> _SelectorProps = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[0].selector;
                string _guiType = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.pageInfo.guiType;
                string _filePath = GlobalData.GetInstance().VM_MainWindowViewModel.DataCollectConfigurationModel.collectInfo.data[0].filePath;
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
                    EditColumn(_ParamDoElementEditorModel, true);
                });
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "编辑表格selector出错");
                MessageHelper.ShowWeakError(ex, eCommonMessageType.DataGridViewModel_cs_BianJiBiaoGeYiChang.GetMessage() );
            }
        }

        public void EmptyViewData()
        {
            GlobalData.GetInstance().VM_MainWindowViewModel.CapturePageModel = new CapturePageViewModel();//清空翻页 
            this.RealDataTable.InitData();
            this.DymDataTable = null;

            this.RealDataTableForPureTable.InitData();
            this.DymTable = null;

            GlobalData.GetInstance().VM_MainWindowViewModel.InitialMaxLineCount();
        }

        #endregion

        #endregion

        #region 成员变量
        private DataTable _myDataTable;
        /// <summary>
        /// 绑定显示的数据 绑定相似元素列表
        /// </summary>
        public DataTable DymDataTable
        {
            get { return _myDataTable; }
            set
            {
                _myDataTable = value;
                RaisePropertyChanged("DymDataTable");
            }
        }

        private DataTable _DymTable;
        /// <summary>
        /// 绑定显示的数据 绑定普通表格
        /// </summary>
        public DataTable DymTable
        {
            get { return _DymTable; }
            set
            {
                _DymTable = value;
                RaisePropertyChanged("DymTable");
            }
        }

        //实际数据 列表格

        //private DataTable RealDataTable = new DataTable();
        public RealDataStruct RealDataTable = new RealDataStruct();

        //实际数据 普通表格
        //private DataTable RealDataTableForPureTable = new DataTable();
        public RealDataStruct RealDataTableForPureTable = new RealDataStruct();

        #endregion

    }

    /// <summary>
    /// 展示里面每列的数据 
    /// </summary>
    public class ColumnData
    {
        /// <summary>
        /// 列名
        /// </summary>
        public string ColName { get; set; } = string.Empty;

        /// <summary>
        /// 列内容
        /// </summary>
        public List<string> ColContentList = new List<string>();
    }

    /// <summary>
    /// 当前显示的DataGrid界面类别 列表元素预览界面 or 普通表格预览界面
    /// </summary>
    public enum DataGridViewType : int
    {
        ColumnDataView = 0,
        TableDataView = 1,
        InitView = 2
    }

    /// <summary>
    /// 记录表格实际数据 
    /// </summary>
    public class RealDataStruct
    {
        Object CLock = new object();

        /// <summary>
        /// 拾取预览数据后 每列元素的数量
        /// </summary>
        public List<int> ColItemCountList { get; set; } = new List<int>();


        /// <summary>
        /// 拾取预览数据后 记录当前数据的DataTable
        /// </summary>
        public DataTable CDataTable = new DataTable();

        /// <summary>
        /// 获取当前最长列的列元素数量
        /// </summary>
        /// <returns></returns>
        public int GetMaxValue()
        {
            int iCountMax = 0;
            for (int i = 0; i < ColItemCountList.Count; i++)
            {
                if (iCountMax < ColItemCountList[i])
                {
                    iCountMax = ColItemCountList[i];
                }
            }
            return iCountMax;
        }

        public int GetMaxValueExceptIndex(int Index)
        {
            int iCountMax = 0;
            for (int i = 0; i < ColItemCountList.Count; i++)
            {
                if (i == Index)
                {
                    continue;
                }

                if (iCountMax < ColItemCountList[i])
                {
                    iCountMax = ColItemCountList[i];
                }
            }
            return iCountMax;
        }


        public int GetCurrentColCount()
        {
            return CDataTable.Columns.Count;
        }

        public void InitData()
        {
            ColItemCountList = new List<int>();
            CDataTable = new DataTable();
        }

        /// <summary>
        /// 设置列顺序
        /// </summary>
        /// <param name="CurrentIndex"></param>
        /// <param name="ButtinIndex"></param>
        public void SetOrdinal(int CurrentIndex, int ButtinIndex)
        {
            lock (CLock)
            {
                CDataTable.Columns[CurrentIndex].SetOrdinal(ButtinIndex);
                int iCount = this.ColItemCountList[CurrentIndex];
                this.ColItemCountList.RemoveAt(CurrentIndex);
                this.ColItemCountList.Insert(ButtinIndex, iCount);
            }

        }

        /// <summary>
        /// 根据列名获取该列索引
        /// </summary>
        /// <param name="strColName"></param>
        /// <returns></returns>
        public int GetColIndexByColName(string strColName)
        {
            return DataTableHelper.GetColumnIndexbyName(CDataTable, strColName);
        }

        /// <summary>
        /// 根据列名获取该列索引
        /// </summary>
        /// <param name="strColName"></param>
        /// <returns></returns>
        public int GetCommnTableColIndexByColName(string strColName)
        {
            return 0;
            //return DataTableHelper.GetColumnIndexbyName(data, strColName);
        }

        /// <summary>
        /// 清空某列
        /// </summary>
        /// <param name="index"></param>
        public void EmptyColumn(int index)
        {
            DataTableHelper.EmptyColumn(CDataTable, index, GetMaxValueExceptIndex(index));
            ColItemCountList[index] = 0;
        }

        /// <summary>
        /// 清空某列
        /// </summary>
        /// <param name="index"></param>
        public void EmptyColumn(DataTable _DataTable, int index)
        {
            DataTableHelper.EmptyColumn(_DataTable, index, GetMaxValueExceptIndex(index));
            ColItemCountList[index] = 0;
        }

        /// <summary>
        /// 重写某列
        /// </summary>
        /// <param name="index"></param>
        public void ReWriteColumn(int index, List<string> _Contentlist)
        {
            lock (CLock)
            {
                DataTableHelper.ReWriteColumn(CDataTable, index, _Contentlist, GetMaxValueExceptIndex(index));
                ColItemCountList[index] = _Contentlist.Count;
            }
        }

        /// <summary>
        /// 重写某列
        /// </summary>
        /// <param name="index"></param>
        public void ReWriteColumn(DataTable dataTable, int index, List<string> _Contentlist)
        {
            lock (CLock)
            {
                DataTableHelper.ReWriteColumn(dataTable, index, _Contentlist, GetMaxValueExceptIndex(index));
            }
        }

        /// <summary>
        /// 增加列
        /// </summary>
        /// <param name="_ContentList"></param>
        /// <param name="strColName"></param>
        public void AddNewColumn(List<string> _ContentList, string strColName)
        {
            lock (CLock)
            {
                DataTableHelper.AddNewColumn(CDataTable, _ContentList, strColName);
                ColItemCountList.Add(_ContentList.Count);
            }
        }


        /// <summary>
        /// 移除列
        /// </summary>
        /// <param name="_Index"></param>
        public void RemoveAt(int _Index)
        {
            lock (CLock)
            {
                CDataTable.Columns.RemoveAt(_Index);
                this.ColItemCountList.RemoveAt(_Index);
                int iCount = this.GetMaxValue();

                if (CDataTable.Rows.Count > iCount)
                {
                    for (int i = CDataTable.Rows.Count - 1; i >= iCount; i--)
                    {
                        CDataTable.Rows.RemoveAt(i);
                    }
                }
            }

        }
    }
}
