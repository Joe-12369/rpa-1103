using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using DevExpress.Spreadsheet;
using II.RPA.Logging;
using II.RPA.Plugin.Excel.Config;
using II.RPA.Plugin.Excel.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace II.RPA.Plugin.Excel.Actions.NonSoftware
{

    public static class NonVisualExtension
    {
        public static object GetValue(this CellValue value, int valueType = 2)
        {
            if (value.IsBoolean)
            {
                return value.BooleanValue;
            }
            if (value.IsDateTime)
            {
                return value.DateTimeValue;
            }
            //else
            //{
            //    if (value.DateTimeValue != default)
            //    {
            //        return value.DateTimeValue;
            //    }
            //}
            if (value.IsNumeric)
            {
                return value.NumericValue;
            }
            if (value.IsText)
            {
                return value.TextValue;
            }
            if (value.IsEmpty)
            {
                return "";
            }
            return "";
        }
    }
    /// <summary>
    /// 不使用软件自动化
    ///全局公用对象
    /// </summary>
    public partial class NonSoftware : IExcel
    {

        static NonSoftware()
        {
            // Legacy .xls files use code pages such as Windows-1252.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private static readonly Lazy<NonSoftware> instance = new Lazy<NonSoftware>(() => new NonSoftware());
        public readonly ILogger logger = Logger.OfContext();
        /// <summary>
        /// 接口实例
        /// </summary>
        public static IExcel Instance { get { return instance.Value; } }
        /// <summary>
        /// 对象实例
        /// </summary>
        public static NonSoftware ContextInstance { get { return instance.Value; } }
        public readonly Dictionary<string, Workbook> Workbooks = new Dictionary<string, Workbook>();
        Dictionary<string, EncryptionSettings> Encryptions = new Dictionary<string, EncryptionSettings>();
        public ActionContext ActionContext { get; set; }
        private CellRange CopyRange = null;
        private Worksheet GetWorksheetByName(string workbook, string sheetName)
        {
            if (!Workbooks.TryGetValue(workbook, out Workbook workbookObj))
            {
                throw new ExcelException(Localize($"{workbook} 未打开或已被关闭", "NonSoftwareExcel_FBBEB0D3"));
            }
            if (string.IsNullOrWhiteSpace(sheetName))
            {
                //throw new ExcelException("表名不能为空");
                return workbookObj.Worksheets.ActiveWorksheet;
            }
            if (!workbookObj.Worksheets.Contains(sheetName))
            {
                throw new ExcelException(Localize($"工作簿中名称为 {sheetName} 的工作表不存在", "NonSoftwareExcel_65183F57"));
            }
            var worksheet = workbookObj.Worksheets[sheetName];
            workbookObj.Worksheets.ActiveWorksheet = worksheet;
            return worksheet;
        }
        private bool ExistSheet(string workbook, string sheetName)
        {
            if (!Workbooks.ContainsKey(workbook))
            {
                throw new ExcelException(Localize($"{workbook} 未打开", "NonSoftwareExcel_5973743C"));
            }
            if (string.IsNullOrWhiteSpace(sheetName))
            {
                throw new ExcelException("表名不能为空".I18n("NonSoftwareExcel_7DCDE33E"));
            }
            return Workbooks[workbook].Worksheets.Contains(sheetName);
        }
        private CellRange GetRange(string workbook, string sheet, int startRow, string startColumn, int lastRow, string lastColumn, int type, string range)
        {
            var _sheet = GetWorksheetByName(workbook, sheet);
            CellRange rangeObj;
            try
            {
                if (type == 0)
                    rangeObj = _sheet[$"{lastColumn}{lastRow}:{startColumn}{startRow}"];
                else
                    rangeObj = _sheet[range];

                return rangeObj;
            }
            catch (Exception)
            {
                throw new ExcelException("选择区域参数格式不正确".I18n("NonSoftwareExcel_941742EB"));
            }

        }
        private Worksheet GetWorksheetByIndex(string workbook, int sheetIndex)
        {
            if (sheetIndex - 1 < 0 || sheetIndex > Workbooks[workbook].Worksheets.Count)
            {
                throw new ExcelException("工作表的序号不能小于 1 或大于工作簿中已有工作表数量".I18n("NonSoftwareExcel_B0391121"));
            }
            return Workbooks[workbook].Worksheets[sheetIndex - 1];
        }
        private void GetRangeVisibleValue(CellRange range, int valueType)
        {
            //目前先这样，等找到好的办法处理隐藏行时在优化
            int totalRows = range.RowCount;
            int totalTasks = totalRows / ExcelConfigurations.Info.GlobalSetting.BatchSize + (totalRows % ExcelConfigurations.Info.GlobalSetting.BatchSize == 0 ? 0 : 1);

            for (int i = 0; i < totalTasks; i++)
            {
                int _startRow = i * ExcelConfigurations.Info.GlobalSetting.BatchSize + 1;
                int _endRow = Math.Min((i + 1) * ExcelConfigurations.Info.GlobalSetting.BatchSize, totalRows);
                // object[,] rangeValue = new object[_endRow - _startRow + 1, range.ColumnCount];
                var data = new List<List<object>>();
                for (int row = _startRow - 1; row < _endRow; row++)
                {
                    if (!range.Worksheet.Rows[range[row, 0].RowIndex].Visible)
                    {
                        continue;
                    }
                    var rowData = new List<object>();
                    for (int column = 0; column < range.ColumnCount; column++)
                    {
                        if (!range.Worksheet.Columns[range[0, column].ColumnIndex].Visible)
                        {
                            continue;
                        }
                        if (valueType == 1)
                            rowData.Add(range[row, column].DisplayText);
                        else
                            rowData.Add(range[row, column].Value.GetValue());
                    }
                    data.Add(rowData);
                }
                // 转换为二维数组
                int visibleColumns = Enumerable.Range(0, range.ColumnCount)
                    .Count(c => range.Worksheet.Columns[range[0, c].ColumnIndex].Visible);
                object[,] result = new object[data.Count, visibleColumns];
                for (int rowIndex = 0; rowIndex < data.Count; rowIndex++)
                {
                    for (int columnIndex = 0; columnIndex < data[rowIndex].Count; columnIndex++)
                    {
                        result[rowIndex, columnIndex] = data[rowIndex][columnIndex];
                    }
                }
                ExcelUtils.SerializerDataToBinaryFile(result, $"{i + 1}-{totalTasks}");
            }


        }
        private void GetRangeValue(CellRange range, int valueType = 0, bool visible = false)
        {
            if (visible)
            {
                GetRangeVisibleValue(range, valueType);
            }
            else
            {
                int totalRows = range.RowCount;
                int totalTasks = totalRows / ExcelConfigurations.Info.GlobalSetting.BatchSize + (totalRows % ExcelConfigurations.Info.GlobalSetting.BatchSize == 0 ? 0 : 1);

                for (int i = 0; i < totalTasks; i++)
                {
                    int _startRow = i * ExcelConfigurations.Info.GlobalSetting.BatchSize + 1;
                    int _endRow = Math.Min((i + 1) * ExcelConfigurations.Info.GlobalSetting.BatchSize, totalRows);
                    object[,] rangeValue = new object[_endRow - _startRow + 1, range.ColumnCount];
                    int rowIndex = 0;
                    for (int row = _startRow - 1; row < _endRow; row++)
                    {
                        for (int column = 0; column < range.ColumnCount; column++)
                        {
                            if (valueType == 1)
                                rangeValue[rowIndex, column] = range[row, column].DisplayText;
                            else
                                rangeValue[rowIndex, column] = range[row, column].Value.GetValue();
                        }
                        rowIndex++;
                    }
                    ExcelUtils.SerializerDataToBinaryFile(rangeValue, $"{i + 1}-{totalTasks}");
                }
            }


        }
        private string GetColumnValue(Worksheet sheet, int row, object[] columns, int flag)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (object column in columns)
            {
                var val = sheet[row + flag, int.Parse(column.ToString()) - 1].Value.GetValue();
                stringBuilder.Append(val);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// 创建文件副本
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static string CreateFileCopyWithPrefix(string filePath)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string extension = Path.GetExtension(filePath);

                // 新文件名添加前缀 "sz~!_"
                string newFileName = "sz~!_" + fileName + extension;
                string newFilePath = Path.Combine(directory, newFileName);
                File.Copy(filePath, newFilePath, true);
                //// 获取文件的当前属性
                //FileAttributes attr = File.GetAttributes(newFilePath);
                //// 添加隐藏属性
                //attr |= FileAttributes.Hidden;
                //File.SetAttributes(newFilePath, attr);
                return newFilePath;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(ContextInstance.logger, ex, "创建文件副本时发生异常");
                return filePath;
            }

        }


        /// <summary>
        /// 转换Office的粘贴选项
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        PasteSpecial GetPasteSpecial(int value)
        {
            if (value == -4104)
            {
                return PasteSpecial.All;
            }
            if (value == -4123)
            {
                return PasteSpecial.Formulas;
            }
            if (value == -4163)
            {
                return PasteSpecial.Values;
            }
            if (value == -4122)
            {
                return PasteSpecial.Formats;
            }
            if (value == -4144)
            {
                return PasteSpecial.Comments;
            }
            if (value == 6)
            {
                return PasteSpecial.DataValidations;
            }
            if (value == 7)
            {
                return PasteSpecial.Borders;
            }
            if (value == 8)
            {
                return PasteSpecial.ColumnWidths;
            }
            if (value == 11)
            {
                return PasteSpecial.Formulas | PasteSpecial.NumberFormats;
            }
            if (value == 12)
            {
                return PasteSpecial.Values | PasteSpecial.NumberFormats;
            }
            throw new ExcelException(Localize($"当前模式不支持该选项，请打开 Excel 文件时选中【使用软件】选项", "NonSoftwareExcel_39960B70"));
        }

        /// <summary>
        /// 读取Excel 配置规则
        /// </summary>
        /// <param name="componentId">组件ID</param>
        /// <returns></returns>
        string IExcel.GetExcelRule(dynamic componentId)
        {
            throw new ExcelException("请在可视化模式下打开一个已经配置好规则的文件，读取相应配置规则".I18n("NonSoftwareExcel_52CC1845"));
        }

    }
}
