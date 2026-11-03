using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using DevExpress.CodeParser;
using DevExpress.Spreadsheet;
using DevExpress.Utils.FormShadow;
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
using System.Security.Policy;
using Picture = DevExpress.Spreadsheet.Picture;
using Worksheet = DevExpress.Spreadsheet.Worksheet;


namespace II.RPA.Plugin.Excel.Actions.NonSoftware
{
    public partial class NonSoftware
    {
        // Baseline implementations from 470cf3681^; used when both optimizations are disabled.

        private void LegacyWriteRow(string workbook, string sheet, string column, int row, string contents)
        {
            LegacyWriteRow(GetWorksheetByName(workbook, sheet), column, row, contents);
        }

        private void LegacyWriteColumn(string workbook, string sheet, string column, int row, string contents)
        {
            LegacyWriteColumn(GetWorksheetByName(workbook, sheet), column, row, contents);
        }

        private void LegacyWriteRange(string workbook, string sheet, int row, string column, string contents)
        {
            dynamic[][] writeData = ExcelUtils.DeSerializerDataFromFile(contents);
            if (ExcelUtils.IsJaggedArrayEmpty(writeData))
            {
                return;
            }
            var _sheet = GetWorksheetByName(workbook, sheet);
            using (ExcelUtils.MeasureApply(writeData.Length, writeData[0].Length))
            using (ExcelUtils.Performance?.Measure("cellWrite", (long)writeData.Length * writeData[0].Length,
                $"{writeData.Length}x{writeData[0].Length}"))
            {
                var _column = ExcelUtils.ColumnLetterToIndex(column);
                for (int rowIndex = 0; rowIndex < writeData.Length; rowIndex++)
                {
                    for (int columnIndex = 0; columnIndex < writeData[0].Length; columnIndex++)
                    {
                        _sheet[row - 1 + rowIndex, _column - 1 + columnIndex].Value = writeData[rowIndex][columnIndex] ?? "";
                    }
                }
            }
        }

        private void LegacyInsertRow(string workbook, string sheet, int index, int count, string contents, int direction)
        {
            var _sheet = GetWorksheetByName(workbook, sheet);
            _sheet.Rows.Insert(index + direction - 1, count);
            if (!string.IsNullOrEmpty(contents) && (File.Exists(contents) || Directory.Exists(contents)))
            {
                if (count == 1)
                {
                    LegacyWriteRow(_sheet, "A", index + direction, contents);
                }
                else
                {
                    Instance.WriteRange(workbook, sheet, index + direction, "A", contents);
                }
            }

        }

        private void LegacyInsertColumn(string workbook, string sheet, int index, int count, string contents, int direction)
        {
            var _sheet = GetWorksheetByName(workbook, sheet);
            _sheet.Columns.Insert(index + direction - 1, count);
            if (!string.IsNullOrEmpty(contents) && (File.Exists(contents) || Directory.Exists(contents)))
            {
                if (count == 1)
                {
                    LegacyWriteColumn(_sheet, ExcelUtils.ColumnIndexToLetter(index + direction), 1, contents);
                }
                else
                {
                    var rangeValue = ExcelUtils.DeSerializerDataFromFile(contents);
                    if (rangeValue.Length != 0 && rangeValue.Length == count)
                    {
                        using (ExcelUtils.MeasureApply(rangeValue.Length, rangeValue[0].Length))
                        using (ExcelUtils.Performance?.Measure("cellWrite", (long)rangeValue.Length * rangeValue[0].Length,
                            $"{rangeValue[0].Length}x{rangeValue.Length}"))
                        {
                            for (int i = 0; i < rangeValue.Length; i++)
                            {
                                for (int j = 0; j < rangeValue[i].Length; j++)
                                {
                                    _sheet.Cells[$"{ExcelUtils.ColumnIndexToLetter(index + direction + i)}{1 + i}"].Value = Convert.ToString((rangeValue[i][j]));
                                }
                            }
                        }
                    }

                }
            }
        }

        private void LegacyWriteRow(Worksheet sheet, string column, int row, string contents)
        {
            try
            {
                var rowValue = ExcelUtils.DeSerializerArrayFromFile(contents);
                if (rowValue.Length == 0) return;
                using (ExcelUtils.MeasureApply(rowValue.Length, 1))
                using (ExcelUtils.Performance?.Measure("cellWrite", rowValue.Length, $"1x{rowValue.Length}"))
                {
                    var columnIndex = ExcelUtils.ColumnLetterToIndex(column);
                    for (int i = 0; i < rowValue.Length; i++)
                    {
                        sheet.Cells[row - 1, i + columnIndex - 1].Value = Convert.ToString(rowValue[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new Exception("写入值的格式不正确".I18n("SheetEdit_ABC81526"));
            }

        }

        private void LegacyWriteColumn(Worksheet sheet, string column, int row, string contents)
        {
            var columnValue = ExcelUtils.DeSerializerArrayFromFile(contents);
            if (columnValue.Length == 0) return;
            using (ExcelUtils.MeasureApply(columnValue.Length, 1))
            using (ExcelUtils.Performance?.Measure("cellWrite", columnValue.Length, $"{columnValue.Length}x1"))
            {
                for (int i = 0; i < columnValue.Length; i++)
                {
                    sheet.Cells[$"{column}{row + i}"].Value = Convert.ToString(columnValue[i]);
                }
            }
        }
    }
}
