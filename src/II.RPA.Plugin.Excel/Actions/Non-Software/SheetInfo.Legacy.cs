using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using DevExpress.Spreadsheet;
using DevExpress.Utils.Extensions;
using II.RPA.Plugin.Excel.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;


namespace II.RPA.Plugin.Excel.Actions.NonSoftware
{
    public partial class NonSoftware
    {
        // Baseline implementations from 470cf3681^, retained for performance comparison.

        private void LegacyReadColumn(string workbook, string sheet, string columnName, int valueType, bool visible)
        {
            var _sheet = GetWorksheetByName(workbook, sheet);
            var usedRange = _sheet.GetDataRange();
            object[] rowVal = null;
            if (visible && !_sheet.Columns[columnName].Visible)
            {
                rowVal = new object[0];
            }
            else
            {
                var rowValues = new List<object>();
                for (int i = 0; i < usedRange.BottomRowIndex + 1; i++)
                {
                    if (visible && !_sheet.Rows[i].Visible)
                    {
                        continue;
                    }
                    var _range = _sheet.Cells[i, ExcelUtils.ColumnLetterToIndex(columnName) - 1];
                    _range.Calculate();
                    if (valueType == 1)
                    {
                        rowValues.Add(_range.DisplayText);
                    }
                    else
                    {
                        rowValues.Add(GetCellValue(_range.Value));
                    }

                }
                rowVal = rowValues.ToArray();
                ContextInstance.ActionContext["__VALUE__"] = rowVal;
            }
            ExcelUtils.SerializerDataToBinaryFile(rowVal);
            ContextInstance.ActionContext.AddResult(ExcelUtils.TEMP_FILEPATH);
        }

        private void LegacyGetEmptyCell(string workbook, string sheet, string range, string column, int row)
        {
            dynamic firstCell;
            if (range.Equals("指定一行"))
            {
                Instance.ReadRow(workbook, sheet, row);
                var _firstIndex = ExcelUtils.FindValueInArray(ContextInstance.ActionContext["__VALUE__"], ExcelUtils.IsNullOrEmptyPredicate);
                firstCell = ExcelUtils.ColumnIndexToLetter(_firstIndex == -1 ? (ContextInstance.ActionContext["__VALUE__"].Length + 1) : _firstIndex + 1);
            }
            else
            {
                Instance.ReadColumn(workbook, sheet, column);
                var _firstIndex = ExcelUtils.FindValueInArray(ContextInstance.ActionContext["__VALUE__"], ExcelUtils.IsNullOrEmptyPredicate);
                firstCell = _firstIndex == -1 ? (ContextInstance.ActionContext["__VALUE__"].Length + 1) : _firstIndex + 1;
            }
            ContextInstance.ActionContext.Result.RemoveAll();
            ContextInstance.ActionContext.AddResult("firstCell", firstCell);
        }

        private void LegacyGetLastEmptyCell(string workbook, string sheet, string range, string column, int row)
        {
            dynamic lastCell;
            if (range.Equals("指定一行"))
            {
                Instance.ReadRow(workbook, sheet, row);
                var listRowVal = ContextInstance.ActionContext["__VALUE__"];
                var lastIndex = ExcelUtils.FindValueInArray(listRowVal, ExcelUtils.IsNotNullOrEmptyPredicate, true);
                lastCell = ExcelUtils.ColumnIndexToLetter(lastIndex == -1 ? 1 : lastIndex + 2);
            }
            else
            {
                Instance.ReadColumn(workbook, sheet, column);
                var listColumnVal = ContextInstance.ActionContext["__VALUE__"];
                var lastIndex = ExcelUtils.FindValueInArray(listColumnVal, ExcelUtils.IsNotNullOrEmptyPredicate, true);
                lastCell = $"{(lastIndex == -1 ? 1 : lastIndex + 2)}";
            }
            ContextInstance.ActionContext.Result.RemoveAll();
            ContextInstance.ActionContext.AddResult("lastCell", lastCell);
        }
    }
}
