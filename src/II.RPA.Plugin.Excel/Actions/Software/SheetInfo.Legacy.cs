using II.RPA.Plugin.Excel.Config;
using II.RPA.Plugin.Excel.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;


namespace II.RPA.Plugin.Excel.Actions.Software
{
    public partial class SoftwareExcel
    {
        // Baseline implementations from 470cf3681^, retained for performance comparison.

        private void LegacyReadColumn(string workbook, string sheet, string columnName, int valueType, bool visible)
        {
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                var excRange = ContextInstance.ActionContext.ExcelUsedRange;
                object _usedRange = com.Track(ComLateBinding.GetProp(_sheet, "Range", excRange.Address));

                object cell1 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", 1, columnName));
                object r = com.Track(ComLateBinding.GetProp(_usedRange, "Rows"));
                object cell2 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", ComLateBinding.ToInt(ComLateBinding.GetProp(_usedRange, "Row")) + ComLateBinding.ToInt(ComLateBinding.GetProp(r, "Count")) - 1, columnName));
                object _range = com.Track(ComLateBinding.GetProp(_sheet, "Range", cell1, cell2));

                object entireCol = com.Track(ComLateBinding.GetProp(_range, "EntireColumn"));
                if (visible && ComLateBinding.ToBool(ComLateBinding.GetProp(entireCol, "Hidden")))
                {
                    //隐藏的
                    ExcelUtils.SerializerDataToBinaryFile(new object[] { });
                }
                else
                {
                    object _range2 = visible ? com.Track(ComLateBinding.Invoke(_range, "SpecialCells", 12)) : _range;
                    if (valueType == 1)
                    {
                        // null 表示使用默认（旧版）行为；仅当显式设为 0 时使用新Reader 读取
                        if (ExcelConfigurations.Info.GlobalSetting.UseLegacyReader != 0)
                        {
                            ContextInstance.GetRangeText(_range2, 2);
                        }
                        else
                        {
                            GetRangeTextWithReaderFallback(
                                workbook,
                                Convert.ToString(ComLateBinding.GetProp(_sheet, "Name")),
                                columnName,
                                1,
                                columnName,
                                excRange.EndRow,
                                2,
                                com,
                                _range,
                                1,
                                columnName);
                        }

                    }
                    else
                    {
                        object rangeValue = GetRangeValueByType(_range2, valueType);
                        ContextInstance.ActionContext["__VALUE__"] = rangeValue;
                        if (visible)
                        {
                            object areas = com.Track(ComLateBinding.GetProp(_range2, "Areas"));
                            int areasCount = ComLateBinding.ToInt(ComLateBinding.GetProp(areas, "Count"));
                            for (int i = 1; i <= areasCount; i++)
                            {
                                object _area = com.Track(GetExcelItem(areas, i));
                                ExcelUtils.SerializerDataToBinaryFile(GetRangeValueByType(_area, valueType));
                            }
                        }
                        else
                        {
                            ExcelUtils.SerializerDataToBinaryFile(rangeValue);
                        }

                    }
                }

                ContextInstance.ActionContext.AddResult(ExcelUtils.TEMP_FILEPATH);
            }
        }

        private void LegacyGetEmptyCell(string workbook, string sheet, string range, string column, int row)
        {
            string firstCell;
            using (var com = new ComScope())
            {
                if (range.Equals("指定一行"))
                {
                    Instance.ReadRow(workbook, sheet, row);
                    object rowValue = ContextInstance.ActionContext["__VALUE__"];
                    if (rowValue is Array)
                    {
                        var rowArray = (Array)rowValue;
                        var firstIndex = ExcelUtils.FindValueInArray(rowArray, ExcelUtils.IsNullOrEmptyPredicate);
                        firstCell = ExcelUtils.ColumnIndexToLetter(firstIndex == -1 ? rowArray.Length + 1 : firstIndex + 1);
                    }
                    else if (rowValue != null)
                    {
                        //只有A1 有值
                        firstCell = "B";
                    }
                    else
                    {
                        //空表格
                        firstCell = "A";
                    }
                }
                else
                {
                    Instance.ReadColumn(workbook, sheet, column);
                    object columnValue = ContextInstance.ActionContext["__VALUE__"];
                    if (columnValue is Array)
                    {
                        var columnArray = (Array)columnValue;
                        var firstIndex = ExcelUtils.FindValueInArray(columnArray, ExcelUtils.IsNullOrEmptyPredicate);
                        firstCell = $"{(firstIndex == -1 ? columnArray.Length + 1 : firstIndex + 1)}";
                    }
                    else if (columnValue != null)
                    {
                        //only a1 cell has value
                        firstCell = "2";
                    }
                    else
                    {
                        //empty sheet
                        firstCell = "1";
                    }
                }
            }
            ContextInstance.ActionContext.AddResult("firstCell", firstCell);
        }

        private void LegacyGetLastEmptyCell(string workbook, string sheet, string range, string column, int row)
        {
            string lastCell;
            using (var com = new ComScope())
            {
                if (range.Equals("指定一行"))
                {
                    Instance.ReadRow(workbook, sheet, row);
                    object rowValue = ContextInstance.ActionContext["__VALUE__"];
                    if (rowValue is Array)
                    {
                        var rowArray = (Array)rowValue;
                        var lastIndex = ExcelUtils.FindValueInArray(rowArray, ExcelUtils.IsNotNullOrEmptyPredicate, true);
                        lastCell = ExcelUtils.ColumnIndexToLetter(lastIndex == -1 ? 1 : lastIndex + 2);
                    }
                    else if (rowValue != null)
                    {
                        //only A1 cell has value
                        lastCell = "B";
                    }
                    else
                    {
                        //empty sheet
                        lastCell = "A";
                    }
                }
                else
                {
                    Instance.ReadColumn(workbook, sheet, column);
                    object columnValue = ContextInstance.ActionContext["__VALUE__"];
                    if (columnValue is Array)
                    {
                        var columnArray = (Array)columnValue;
                        var lastIndex = ExcelUtils.FindValueInArray(columnArray, ExcelUtils.IsNotNullOrEmptyPredicate, true);
                        lastCell = $"{(lastIndex == -1 ? 1 : lastIndex + 2)}";
                    }
                    else if (columnValue != null)
                    {
                        //only a1 cell has value
                        lastCell = "2";
                    }
                    else
                    {
                        //empty sheet
                        lastCell = "1";
                    }
                }
            }
            ContextInstance.ActionContext.AddResult("lastCell", lastCell);
        }
    }
}
