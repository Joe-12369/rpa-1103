using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    /// UCTable.xaml 的交互逻辑
    /// </summary>
    public partial class UCTable : UserControl, IRpaCustomControl
    {
        private InputModel _inputModel;
        private DataTable _dataTable;
        private static ILogger logger = Logger.OfContext();

        /// <summary>
        /// 表格宽度
        /// </summary>
        private const int TABLE_WIDTH = 403;
        /// <summary>
        /// 最小列宽
        /// </summary>
        private const int MIN_COLUMN_WIDTH = 100;
        public UCTable()
        {
            InitializeComponent();
            WinAutomation.CommonUtility.XamlResource.LanguageResourceHelper.GetInstance().SetAppInstance(this);
        }

        public void Init(InputModel input)
        {
            try
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
                    if (input.Row > 0 && input.Column != null && input.Column.Length > 0)
                    {
                        CreateDataSource(input.Row, input.Column);
                    }
                    if (input.ValueTable != null)
                    {
                        _dataTable = input.ValueTable;
                        this.TABLE.ItemsSource = _dataTable.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Set Value Error");
                throw;
            }
        }

        private void CreateDataSource(int row, string[] columnName)
        {
            _dataTable = new DataTable();
            foreach (var column in columnName)
            {
                _dataTable.Columns.Add(new DataColumn(column));
            }
            if (columnName.Length < 4)
            {
                this.TABLE.ColumnWidth = TABLE_WIDTH / columnName.Length;
            }
            else
            {
                this.TABLE.ColumnWidth = MIN_COLUMN_WIDTH;
            }
            for (int i = 0; i < row; i++)
            {
                _dataTable.Rows.Add(_dataTable.NewRow());
            }
            this.TABLE.ItemsSource = _dataTable.DefaultView;
        }

        private void DataGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            // Lookup for the source to be DataGridCell
            if (e.OriginalSource.GetType() == typeof(DataGridCell))
            {
                // Starts the Edit on the row;
                DataGrid grd = (DataGrid)sender;
                grd.BeginEdit(e);
            }
        }

        public string GetResult()
        {
            string ret = "";
            try
            {
                ret = JsonConvert.SerializeObject(_dataTable);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Set Value Error");
                throw;
            }
            return ret;
        }

        public OutputModel GetOutputModel()
        {
            var outDto = new OutputModel
            {
                name = this.Name,
                value = GetResult(),
            };
            return outDto;
        }

        public InputModel GetInputModel()
        {
            _inputModel.ValueTable = _dataTable;
            return _inputModel;
        }

        public void SetLast(OutputModel model, InputModel input)
        {
            try
            {
                input.Value = model.value;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "Set Value Error");
            }
        }


        private void StackPanel_PreviewMouseWhee(object sender, MouseWheelEventArgs e)
        {
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            TABLESP.RaiseEvent(eventArg);
        }
    }
}

