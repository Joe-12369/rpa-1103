using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Dialog.Depends;
using II.RPA.Plugin.Dialog.Forms;
using II.RPA.Plugin.Dialog.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;


namespace II.RPA.Plugin.Dialog.UCControl
{
    /// <summary>
    /// UCDataTable.xaml 的交互逻辑
    /// </summary>
    public partial class UCDataTable : UserControl, IRpaCustomControl
    {
        private InputModel _inputModel;
        private DataTable _dataTable;
        private readonly string _path;
        private readonly ExeMode _exeModel;
        private TableSource tableSource = new TableSource();
        private static ILogger logger = Logger.OfContext();

        public UCDataTable()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
        }
        public UCDataTable(string packagePath, ExeMode exeMode)
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

        private void StackPanel_PreviewMouseWhee(object sender, MouseWheelEventArgs e)
        {
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            };
            TABLESP.RaiseEvent(eventArg);
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
                    this.LAB.Content = input.Label;
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
                MessageBox.Show(Localize($"添加行异常：{ex.Message}", "UCDataTable.xaml_99450DB7"));
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
                MessageBox.Show(Localize($"添加列异常：{ex.Message}", "UCDataTable.xaml_8EAF5570"));
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

                if (!message.Code)
                {
                    MessageBox.Show(message.Message);
                    return;
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

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel文件|*.xls;*.xlsx",
            };
            bool? ret = saveFileDialog.ShowDialog();
            if (ret.Value)
            {
                ExcelHelper.ExportData(_dataTable, saveFileDialog.FileName, out ExcelWriteMessage message);
                if (message.Code)
                {
                    MessageBox.Show("导出成功".I18n("UCDataTable.xaml_3A43A070"));
                }
                else
                {
                    MessageBox.Show(message.Message);
                }
            }
        }

        private void DelRow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ExcelHelper.IsCanDeleteRow(_dataTable.Rows.Count))
                {
                    var cell = TABLE.SelectedCells[0];
                    var dataRowView = cell.Item as DataRowView;
                    _dataTable.Rows.Remove(dataRowView.Row);
                    this.TABLE.ItemsSource = null;
                    this.TABLE.ItemsSource = _dataTable.DefaultView;
                }
                else
                {
                    MessageBox.Show("无法进行删除！".I18n("UCDataTable.xaml_F90185E2"));
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "删除行失败");
                MessageBox.Show("删除失败".I18n("UCDataTable.xaml_FA000617"));
            }
        }

        private void DelColumn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cell = TABLE.SelectedCells[0];
                var index = cell.Column.DisplayIndex;
                ExcelHelper.DeleteColumn(_dataTable, index);
                Refresh();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "删除列失败");
                MessageBox.Show("无法进行删除!".I18n("UCDataTable.xaml_641F9482"));
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
            while (element != null)
            {
                if (element == itemsControl)
                {
                    m1.Visibility = Visibility.Collapsed;
                    m2.Visibility = Visibility.Collapsed;
                    m3.Visibility = Visibility.Collapsed;
                    m4.Visibility = Visibility.Collapsed;
                    m5.Visibility = Visibility.Collapsed;
                    m6.Visibility = Visibility.Collapsed;
                    m7.Visibility = Visibility.Collapsed;
                    m8.Visibility = Visibility.Collapsed;
                    m9.Visibility = Visibility.Collapsed;
                    m10.Visibility = Visibility.Collapsed;
                    m11.Visibility = Visibility.Visible;
                    m12.Visibility = Visibility.Visible;
                    break;
                }
                element = (UIElement)VisualTreeHelper.GetParent(element);
                if (element is DataGridColumnHeader e)
                {
                    tableSource.SelectColumn = e.Column.Header.ToString();
                    m1.Visibility = Visibility.Collapsed;
                    m2.Visibility = Visibility.Collapsed;
                    m3.Visibility = Visibility.Collapsed;
                    m4.Visibility = Visibility.Collapsed;
                    m5.Visibility = Visibility.Collapsed;
                    m6.Visibility = Visibility.Visible;
                    m7.Visibility = Visibility.Visible;
                    m8.Visibility = Visibility.Visible;
                    m9.Visibility = Visibility.Visible;
                    m10.Visibility = Visibility.Visible;
                    m11.Visibility = Visibility.Collapsed;
                    m12.Visibility = Visibility.Collapsed;
                    break;
                }
                if (element is DataGridRowHeader)
                {
                    m1.Visibility = Visibility.Visible;
                    m2.Visibility = Visibility.Visible;
                    m3.Visibility = Visibility.Visible;
                    m4.Visibility = Visibility.Visible;
                    m5.Visibility = Visibility.Visible;
                    m6.Visibility = Visibility.Collapsed;
                    m7.Visibility = Visibility.Collapsed;
                    m8.Visibility = Visibility.Collapsed;
                    m9.Visibility = Visibility.Collapsed;
                    m10.Visibility = Visibility.Collapsed;
                    m11.Visibility = Visibility.Collapsed;
                    m12.Visibility = Visibility.Collapsed;
                    break;
                }
            }
        }

        private void Dg_Sorting(object sender, DataGridSortingEventArgs e)
        {
            TABLE.SelectedIndex = -1;
            TABLE.SelectedCells.Clear();
            tableSource.SelectColumn = e.Column.Header.ToString();
            e.Handled = true;
        }

        private void Row_UpInsert(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int i = 0; i++ < m3.Num;)
                {
                    var dr = _dataTable.NewRow();
                    _dataTable.Rows.InsertAt(dr, TABLE.SelectedIndex);
                }
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localize($"插入行异常：{ex.Message}", "UCDataTable.xaml_7BD8A411"));
            }
        }

        private void Row_DownInsert(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int i = 0; i++ < m4.Num;)
                {
                    var dr = _dataTable.NewRow();
                    _dataTable.Rows.InsertAt(dr, TABLE.SelectedIndex + 1);
                }
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localize($"插入行异常：{ex.Message}", "UCDataTable.xaml_7BD8A411"));
            }
        }

        private void Column_LeftInsert(object sender, RoutedEventArgs e)
        {
            int num = m8.Num;
            int columnIndex = _dataTable.Columns.IndexOf(tableSource.SelectColumn);
            for (int i = 0; i++ < num;)
            {
                var count = _dataTable.Columns.Count;
                var dc = new DataColumn(ExcelHelper.GetColumnName(count));
                _dataTable.Columns.Add(dc);
            }
            for (int i = 0; i < _dataTable.Rows.Count; i++)
            {
                for (int j = _dataTable.Columns.Count - 1; j >= columnIndex + num; j--)
                {
                    _dataTable.Rows[i][j] = _dataTable.Rows[i][j - num];
                }
                for (int j = columnIndex; j < columnIndex + num; j++)
                {
                    _dataTable.Rows[i][j] = "";
                }
            }
            Refresh();
        }

        private void Column_RightInsert(object sender, RoutedEventArgs e)
        {
            try
            {
                int num = m9.Num;
                int columnIndex = _dataTable.Columns.IndexOf(tableSource.SelectColumn) + 1;
                for (int i = 0; i++ < num;)
                {
                    var count = _dataTable.Columns.Count;
                    var dc = new DataColumn(ExcelHelper.GetColumnName(count));
                    _dataTable.Columns.Add(dc);
                }
                for (int i = 0; i < _dataTable.Rows.Count; i++)
                {
                    for (int j = _dataTable.Columns.Count - 1; j >= columnIndex + num; j--)
                    {
                        _dataTable.Rows[i][j] = _dataTable.Rows[i][j - num];
                    }
                    for (int j = columnIndex; j < columnIndex + num; j++)
                    {
                        _dataTable.Rows[i][j] = "";
                    }
                }
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localize($"插入行异常：{ex.Message}", "UCDataTable.xaml_7BD8A411"));
            }
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
                MessageBox.Show("无法进行删除！".I18n("UCDataTable.xaml_F90185E2"));
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
                    MessageBox.Show("无法进行删除！".I18n("UCDataTable.xaml_F90185E2"));
                }
            }
            else
            {
                MessageBox.Show("删除失败".I18n("UCDataTable.xaml_FA000617"));
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

