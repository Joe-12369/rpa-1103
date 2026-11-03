using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Dialog.Depends;
using II.RPA.Plugin.Dialog.Forms;
using II.RPA.Plugin.Dialog.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.Dialog.UCControl;
using System.Text;

namespace II.RPA.Plugin.Dialog.UserControls
{
    /// <summary>
    /// Interaction logic for SingletonDataTable.xaml
    /// </summary>
    public partial class SingletonDataTable : UserControl, IRpaCustomControl
    {
        private InputModel _inputModel;
        private DataTable _dataTable;
        private readonly string _path;
        private readonly ExeMode _exeModel;
        private TableSource tableSource = new TableSource();
        private static ILogger logger = Logger.OfContext();
        private StringBuilder stringBuilder = new StringBuilder(3 * 1024 * 1024);

        public SingletonDataTable()
        {
            InitializeComponent();
        }
        public SingletonDataTable(string value)
        {
            InitializeComponent();
            stringBuilder = new StringBuilder(value);
        }

        public SingletonDataTable(string packagePath, ExeMode exeMode)
        {
            _path = System.IO.Path.Combine(packagePath, "diaFile\\dataTableFile");
            _exeModel = exeMode;
            InitializeComponent();
            this.DataContext = tableSource;

        }

        private void DataGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            tableSource.SelectColumn = "";
        }

        
        public void Init(InputModel input)
        {
            if (input != null)
            {
                _inputModel = input;
                if (!string.IsNullOrEmpty(input.Name))
                {
                    this.Name = input.Name;
                }
                if (!string.IsNullOrEmpty(input.Label))
                {
                    // this.LAB.Content = input.Label;
                }
                string fullPath = System.IO.Path.Combine(_path, input.Value);
                _dataTable = ExcelHelper.ImportData(fullPath, out ExcelReadMessage readMessage);
                if (readMessage.Code)
                {
                    this.TABLE.ItemsSource = _dataTable.DefaultView;
                }
                else
                {
                    logger.LogError("数据表加载异常：" + readMessage.Message);
                    throw new Exception(readMessage.Message);
                }
            }
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dr = _dataTable.NewRow();
                _dataTable.Rows.Add(dr);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localize($"添加行异常：{ex.Message}", "SingletonDataTable.xaml_99450DB7"));
            }
        }

        private void AddColumn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var count = _dataTable.Columns.Count;
                var dc = new DataColumn(ExcelHelper.GetColumnName(count));
                _dataTable.Columns.Add(dc);
                _dataTable.AcceptChanges();
                TABLE.ItemsSource = null;
                TABLE.ItemsSource = _dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localize($"添加列异常：{ex.Message}", "SingletonDataTable.xaml_8EAF5570"));
            }
        }

        public string GetResult()
        {
            throw new NotImplementedException();
        }

        public OutputModel GetOutputModel()
        {
            throw new NotImplementedException();
        }

        public void SetLast(OutputModel model, InputModel input)
        {
            throw new NotImplementedException();
        }

        public InputModel GetInputModel()
        {
            if (_exeModel == ExeMode.Exe)
            {
                string oldFileName = System.IO.Path.GetFileNameWithoutExtension(_inputModel.Value);
                if (oldFileName.Length == 14)
                {
                    DeleteTempFile();
                }
            }

            string fileName = DateTime.Now.ToString("yyyymmddhhMMss") + ".xls";
            string fullName = System.IO.Path.Combine(_path, fileName);
            ExcelHelper.ExportData(_dataTable, fullName, out ExcelWriteMessage message);
            if (!message.Code)
            {
                logger.LogError(message.Message);
            }
            _inputModel.Value = fileName;
            return _inputModel;
        }

        public void DeleteTempFile()
        {
            try
            {
                string fullPath = System.IO.Path.Combine(_path, _inputModel.Value);
                logger.LogInformation($"临时文件:{fullPath}");
                File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "临时文件删除异常");
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel文件|*.xls;*.xlsx",
            };
            bool? ret = openFileDialog.ShowDialog();
            if (ret.Value)
            {
                ExcelReadMessage message = new ExcelReadMessage();
                string sheetName = string.Empty;
                Waiting waiting = new Waiting();

                var sheetNames = ExcelHelper.GetNames(openFileDialog.FileName, out message);
                if (sheetNames.Count() > 1)
                {
                    FormImport formImport = new FormImport
                    {
                        Owner = Window.GetWindow(this)
                    };
                    if (!formImport.GetSheetName(out sheetName, sheetNames))
                    {
                        return;
                    }
                }
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        _dataTable = ExcelHelper.ImportData(openFileDialog.FileName, sheetName, out message);
                    }
                    catch (Exception ex)
                    {
                        message.SetMessage(ex);
                    }
                    finally
                    {
                        waiting.Dispatcher.Invoke(() =>
                        {
                            waiting.Close();
                        });
                    }
                })
                {
                    IsBackground = true,
                };
                thread.Start();
                waiting.ShowDialog();
                if (message.Code)
                {
                    this.TABLE.ItemsSource = null;
                    this.TABLE.ItemsSource = _dataTable.DefaultView;
                }
                else
                {
                    MessageBox.Show(message.Message);
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            int count = _dataTable.Rows.Count;
            _dataTable.Clear();
            for (int i = 0; i < count; i++)
            {
                var dr = _dataTable.NewRow();
                _dataTable.Rows.Add(dr);
            }
        }

     

        private void Refresh()
        {
            this.TABLE.ItemsSource = null;
            this.TABLE.ItemsSource = _dataTable.DefaultView;
        }

        private void TABLE_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ShowMenuItem((ItemsControl)sender, (DependencyObject)e.OriginalSource);
        }
        private void ShowMenuItem(ItemsControl itemsControl, DependencyObject element)
        {
          
        }

       
      

    

   

        

        private void Row_Copy(object sender, RoutedEventArgs e)
        {
            RowCopy();
        }

        private void RowCopy()
        {
            if (TABLE.SelectedIndex >= 0)
            {
                TABLE.CommitEdit();
                TableData tableData = new TableData
                {
                    DataType = EmDataType.Row,
                    Data = _dataTable.Rows[TABLE.SelectedIndex].ItemArray,
                };
                logger.LogTrace("ClipboardData:" + tableData.ToJson());
                Clipboard.SetText(tableData.ToJson());
            }
        }

        private void Row_Paste(object sender, RoutedEventArgs e)
        {
            RowPaste();
        }

        private void RowPaste()
        {
            if (TABLE.SelectedIndex >= 0)
            {
                string json = Clipboard.GetText();
                try
                {
                    TableData tableData = JsonConvert.DeserializeObject<TableData>(json);
                    if (tableData.DataType != EmDataType.Row)
                    {
                        return;
                    }
                    if (tableData.Data.Length != _dataTable.Columns.Count)
                    {
                        return;
                    }
                    for (int i = 0; i < tableData.Data.Length; i++)
                    {
                        _dataTable.Rows[TABLE.SelectedIndex][i] = tableData.Data[i];
                    }
                }
                catch (Exception)
                {
                    logger.LogTrace($"剪贴板数据为：{json}");
                }
            }
        }

        private void Column_Copy(object sender, RoutedEventArgs e)
        {
            ColumnCopy();
        }

        private void ColumnCopy()
        {
            if (!string.IsNullOrEmpty(tableSource.SelectColumn))
            {
                object[] columns = new object[_dataTable.Rows.Count];
                for (int i = 0; i < _dataTable.Rows.Count; i++)
                {
                    columns[i] = _dataTable.Rows[i][tableSource.SelectColumn];
                }
                TableData tableData = new TableData
                {
                    DataType = EmDataType.Column,
                    Data = columns,
                };
                Clipboard.SetText(tableData.ToJson());
            }
        }

        private void Column_Paste(object sender, RoutedEventArgs e)
        {
            ColumnPaste();
        }

        private void ColumnPaste()
        {
            if (!string.IsNullOrEmpty(tableSource.SelectColumn))
            {
                string json = Clipboard.GetText();
                try
                {
                    TableData tableData = JsonConvert.DeserializeObject<TableData>(json);
                    if (tableData.DataType != EmDataType.Column)
                    {
                        return;
                    }
                    if (tableData.Data.Length != _dataTable.Rows.Count)
                    {
                        return;
                    }
                    for (int i = 0; i < _dataTable.Rows.Count; i++)
                    {
                        _dataTable.Rows[i][tableSource.SelectColumn] = tableData.Data[i];
                    }
                }
                catch (Exception)
                {
                    logger.LogTrace($"剪贴板数据为：{json}");
                }
            }
        }

        private void Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(tableSource.SelectColumn))
            {
                ColumnCopy();
            }
            else if (TABLE.SelectedIndex >= 0)
            {
                RowCopy();
            }
        }

        private void Paste(object sender, ExecutedRoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(tableSource.SelectColumn))
            {
                ColumnPaste();
            }
            else if (TABLE.SelectedIndex >= 0)
            {
                RowPaste();
            }
        }

        private void Row_Delete(object sender, RoutedEventArgs e)
        {
            if (ExcelHelper.IsCanDeleteRow(_dataTable.Rows.Count))
            {
                if (TABLE.SelectedIndex >= 0)
                {
                    _dataTable.Rows.RemoveAt(TABLE.SelectedIndex);
                }
            }
            else
            {
                MessageBox.Show("无法进行删除！".I18n("SingletonDataTable.xaml_F90185E2"));
            }
        }

        private void Column_Delete(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(tableSource.SelectColumn))
            {
                try
                {
                    var index = _dataTable.Columns.IndexOf(tableSource.SelectColumn);
                    ExcelHelper.DeleteColumn(_dataTable, index);
                    Refresh();
                }
                catch (Exception ex)
                {
                    logger.LogError("删除列失败" + ex.Message);
                    MessageBox.Show("无法进行删除！".I18n("SingletonDataTable.xaml_F90185E2"));
                }
            }
            else
            {
                MessageBox.Show("删除失败".I18n("SingletonDataTable.xaml_FA000617"));
            }
        }

    }
    public class TableData
    {
        public EmDataType DataType { get; set; }
        public object[] Data { get; set; }
    }

    public enum EmDataType
    {
        Row = 1,
        Column = 2,
    }
}

