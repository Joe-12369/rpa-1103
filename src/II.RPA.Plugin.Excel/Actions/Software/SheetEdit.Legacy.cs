using II.RPA.Plugin.Excel.Config;
using II.RPA.Plugin.Excel.Models;
using II.RPA.Plugin.Excel.Params;
using II.RPA.Plugin.Excel.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;


namespace II.RPA.Plugin.Excel.Actions.Software
{
    public partial class SoftwareExcel
    {
        // Baseline implementations from 470cf3681^, retained for performance comparison.

        private void LegacyWriteRow(string workbook, string sheet, string column, int row, string contents)
        {
            var rangeValue = ExcelUtils.DeSerializerArrayFromFile(contents);
            if (rangeValue.Length > 0)
            {
                using (ExcelUtils.MeasureApply(rangeValue.Length, 1))
                {
                    using (var com = new ComScope())
                    {
                        object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                        object cell1 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", row, column));
                        object cell2 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", row, ExcelUtils.ColumnLetterToIndex(column) + rangeValue.Length - 1));
                        object rangeObj = com.Track(ComLateBinding.GetProp(_sheet, "Range", cell1, cell2));
                        object cells = com.Track(ComLateBinding.GetProp(rangeObj, "Cells"));
                        ComLateBinding.SetProp(cells, "Value", rangeValue);
                    }
                }
            }
        }

        private void LegacyWriteColumn(string workbook, string sheet, string column, int row, string contents)
        {          
            var rangeValue = ExcelUtils.DeSerializerArrayFromFile(contents);
            if (rangeValue.Length > 0)
            {
                using (ExcelUtils.MeasureApply(rangeValue.Length, 1))
                {
                    // 将一维数组转为二维单列数组，使用 Excel 转置函数不稳定
                    object[,] columnData = new object[rangeValue.Length, 1];
                    for (int i = 0; i < rangeValue.Length; i++)
                        columnData[i, 0] = rangeValue[i];

                    using (var com = new ComScope())
                    {
                        object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                        object cell1 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", row, column));
                        object cell2 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", rangeValue.Length + row - 1, column));
                        object rangeObj = com.Track(ComLateBinding.GetProp(_sheet, "Range", cell1, cell2));
                        
                        ComLateBinding.SetProp(rangeObj, "Value", columnData);
                    }
                }
            }
        }

        private void LegacyWriteRange(string workbook, string sheet, int row, string column, string contents)
        {
            logger.LogInformation("write range value start...");
            object[][] writeData = ExcelUtils.DeSerializerDataFromFile(contents);
            if (ExcelUtils.IsJaggedArrayEmpty(writeData))
            {
                return;
            }
            int totalRows = writeData.Length;
            int totalTasks = totalRows / ExcelConfigurations.Info.GlobalSetting.BatchSize + (totalRows % ExcelConfigurations.Info.GlobalSetting.BatchSize == 0 ? 0 : 1);
            logger.LogInformation($"range row:{totalRows},task: {totalTasks}");
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                var _endColumn = ExcelUtils.ColumnIndexToLetter(ExcelUtils.ColumnLetterToIndex(column) + writeData[0].Length - 1);
                object[,] _data = null;
                if (totalTasks < 2)
                {
                    using (ExcelUtils.MeasureApply(totalRows, writeData[0].Length))
                    {
                        _data = new object[ExcelConfigurations.Info.GlobalSetting.BatchSize, writeData[0].Length];
                        for (int i = 0; i < totalRows; i++)
                        {
                            for (int k = 0; k < writeData[0].Length; k++)
                            {
                                _data[i, k] = writeData[i][k];
                            }
                        }

                        object _rng = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{column}{row}:{_endColumn}{row + totalRows - 1}"));
                        ComLateBinding.SetProp(_rng, "Value", _data);
                    }
                }
                else
                {
                    int startRowIndex = row;
                    for (int i = 0; i < totalTasks; i++)
                    {
                        int _startRow = i * ExcelConfigurations.Info.GlobalSetting.BatchSize;
                        int _endRow = Math.Min((i + 1) * ExcelConfigurations.Info.GlobalSetting.BatchSize, totalRows);
                        using (ExcelUtils.MeasureApply(_endRow - _startRow, writeData[0].Length, _startRow))
                        {
                            if (_data == null)
                                _data = new object[ExcelConfigurations.Info.GlobalSetting.BatchSize, writeData[0].Length];
                            int _rowIndex = 0;
                            for (; _startRow < _endRow; _startRow++)
                            {
                                for (int k = 0; k < writeData[0].Length; k++)
                                {
                                    _data[_rowIndex, k] = writeData[_startRow][k];
                                }
                                _rowIndex++;
                            }
                            object _rng = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{column}{startRowIndex}:{_endColumn}{startRowIndex + _rowIndex - 1}"));
                            startRowIndex += _rowIndex;
                            ComLateBinding.SetProp(_rng, "Value", _data);
                            Array.Clear(_data, 0, _data.Length);
                            logger.LogInformation($"task {i + 1} end");
                        }
                    }
                }
            }

            logger.LogInformation($"write range value end!");
        }

        private void LegacyInsertRow(string workbook, string sheet, int index, int count, string contents, int direction)
        {
            var writeIndex = ExcelUtils.ConvertNegativeRow(index) + direction;

            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));

                var InsertRowAction = new System.Action(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        object rowObj = com.Track(ComLateBinding.GetProp(_sheet, "Rows", writeIndex));
                        ComLateBinding.Invoke(rowObj, "Insert");
                    }
                });
                InsertRowAction();
                if (!string.IsNullOrEmpty(contents) && (File.Exists(contents) || Directory.Exists(contents)))
                {
                    if (count == 1)
                    {
                        var rangeValue = ExcelUtils.DeSerializerArrayFromFile(contents);
                        if (rangeValue.Length != 0)
                        {
                            using (ExcelUtils.MeasureApply(rangeValue.Length, 1))
                            {
                                object cell1 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", writeIndex, 1));
                                object cell2 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", writeIndex, rangeValue.Length));
                                object rangeObj = com.Track(ComLateBinding.GetProp(_sheet, "Range", cell1, cell2));
                                ComLateBinding.SetProp(rangeObj, "Value", rangeValue);
                            }
                        }
                    }
                    else
                    {
                        Instance.WriteRange(workbook, sheet, writeIndex, "A", contents);
                    }
                }
            }
        }

        private void LegacyInsertColumn(string workbook, string sheet, int index, int count, string contents, int direction)
        {
            var writeIndex = index + direction;
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));

                var InsertColumnAction = new System.Action(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        object colObj = com.Track(ComLateBinding.GetProp(_sheet, "Columns", writeIndex));
                        ComLateBinding.Invoke(colObj, "Insert");
                    }
                });
                InsertColumnAction();
                if (!string.IsNullOrEmpty(contents) && (File.Exists(contents) || Directory.Exists(contents)))
                {
                    if (count == 1)
                    {
                        var rangeValue = ExcelUtils.DeSerializerArrayFromFile(contents);
                     
                        if (rangeValue.Length != 0)
                        {
                            using (ExcelUtils.MeasureApply(rangeValue.Length, 1))
                            {
                                // 将一维数组转为二维单列数组
                                object[,] columnData = new object[rangeValue.Length, 1];
                                for (int i = 0; i < rangeValue.Length; i++)
                                    columnData[i, 0] = rangeValue[i];

                               
                                /*Excel 的函数 不稳定*/
                                //var app = com.Track(_sheet.Application);
                                //var wsFunc = com.Track(app.WorksheetFunction);
                                object cell1 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", 1, writeIndex));
                                object cell2 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", rangeValue.Length, writeIndex));
                                object rangeObj = com.Track(ComLateBinding.GetProp(_sheet, "Range", cell1, cell2));
                                ComLateBinding.SetProp(rangeObj, "Value", columnData);
                            }
                        }

                    }
                    else
                    {
                        var rangeValue = ExcelUtils.DeSerializerDataFromFile(contents);
                        if (rangeValue.Length != 0 && rangeValue.Length == count)
                        {
                            for (int i = 0; i < rangeValue.Length; i++)
                            {
                                if (rangeValue[i].Length != 0)
                                {
                                    using (ExcelUtils.MeasureApply(1, rangeValue[i].Length, i))
                                    {
                                        object cell1 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", 1, writeIndex + i));
                                        object cell2 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", rangeValue.Length, writeIndex + i));
                                        object rangeObj = com.Track(ComLateBinding.GetProp(_sheet, "Range", cell1, cell2));
                                        object app = com.Track(ComLateBinding.GetProp(_sheet, "Application"));
                                        object wsFunc = com.Track(ComLateBinding.GetProp(app, "WorksheetFunction"));
                                        ComLateBinding.SetProp(rangeObj, "Value", com.Track(ComLateBinding.Invoke(wsFunc, "Transpose", new object[] { rangeValue[i] })));
                                    }
                                }
                            }
                        }

                    }
                }
            }
        }
    }
}
