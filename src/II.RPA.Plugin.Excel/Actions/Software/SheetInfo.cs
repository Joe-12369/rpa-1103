using DevExpress.DataAccess.Sql;
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
    /// <summary>
    /// 使用软件自动化
    /// 设计器.信息读取类组件
    /// </summary>
    public partial class SoftwareExcel : IExcel
    {
        /// <summary>
        /// 读取单元格
        /// </summary>        
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>  
        ///<param name="valueType">读取文本值</param>
        ///<param name="visible">读取隐藏值</param>
        void IExcel.ReadCell(string workbook, string sheet, string column, string row, int valueType, bool visible)
        {
            string strCell;
            using (var com = new ComScope())
            {
                object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                object cellRange = com.Track(ComLateBinding.GetProp(worksheet, "Range", $"{column}{row}"));
                object entireRow = com.Track(ComLateBinding.GetProp(cellRange, "EntireRow"));
                if (visible && ComLateBinding.ToBool(ComLateBinding.GetProp(entireRow, "Hidden")))
                {
                    strCell = "";
                }
                else
                {
                    object val = GetRangeValueByType(cellRange, valueType);
                    if (val == null) strCell = "";
                    else strCell = val.ToString();
                }

                ContextInstance.ActionContext.AddResult("strCell", strCell);
            }
        }

        /// <summary>
        /// 读取区域
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="startRow">起始单元格行号</param>  
        /// <param name="startColumn">起始单元格列名</param>  
        /// <param name="lastRow">结束单元格行号</param>  
        /// <param name="lastColumn">结束单元格列名</param>  
        /// <param name="type">区域模式，0 普通模式，1 高级模式</param>
        /// <param name="range">高级模式区域表达式</param>
        /// <param name="valueType">1:Text 0:Value2 2:Value</param>
        /// <param name="visible">是否读取隐藏的值</param>
        void IExcel.ReadRange(string workbook, string sheet, int startRow, string startColumn, int lastRow, string lastColumn, int type, string range, int valueType, bool visible)
        {
            try
            {
                //高级模式
                if (type == 1)
                {
                    var _range = ContextInstance.ValidRangeAddress(workbook, sheet, range);
                    startRow = _range.Item2;
                    startColumn = _range.Item1;
                    lastRow = _range.Item4;
                    lastColumn = _range.Item3;
                }
                else
                {
                    //已编辑区域
                    if (startRow == 0)
                    {
                        startRow = ExcelPlugin.Instance.RequestContext.ExcelUsedRange.StartRow;
                    }
                    if (lastRow == 0)
                    {
                        lastRow = ExcelPlugin.Instance.RequestContext.ExcelUsedRange.EndRow;
                    }
                    if (string.IsNullOrEmpty(startColumn))
                    {
                        startColumn = ExcelPlugin.Instance.RequestContext.ExcelUsedRange.StartColumn;
                    }
                    if (string.IsNullOrEmpty(lastColumn))
                    {
                        lastColumn = ExcelPlugin.Instance.RequestContext.ExcelUsedRange.EndColumn;
                    }
                }

                string _rangeValue = null;
                using (var com = new ComScope())
                {
                    if (valueType == 1)
                    {
                        logger.LogInformation($"use legacy reader: {ExcelConfigurations.Info.GlobalSetting.UseLegacyReader}");
                        object rangeObj = com.Track(GetRange(workbook, sheet, startRow, startColumn, lastRow, lastColumn, type, range));
                        object rangeSheet = com.Track(ComLateBinding.GetProp(rangeObj, "WorkSheet"));
                        sheet = Convert.ToString(ComLateBinding.GetProp(rangeSheet, "Name"));
                        if (visible)
                        {
                            object wb = GetWorkbook(workbook);
                            object sheets = com.Track(ComLateBinding.GetProp(wb, "Sheets"));
                            object contentSheet = com.Track(ComLateBinding.Invoke(sheets, "Add"));
                            ComLateBinding.SetProp(contentSheet, "Visible", 2);
                            object cells = com.Track(ComLateBinding.GetProp(contentSheet, "Cells"));
                            object cell1_1 = com.Track(ComLateBinding.GetProp(cells, "Item", 1, 1));
                            ComLateBinding.Invoke(rangeObj, "Copy", cell1_1);

                            // null 表示使用默认（旧版）行为；仅当显式设为 0 时使用新Reader 读取
                            if (ExcelConfigurations.Info.GlobalSetting.UseLegacyReader != 0)
                            {
                                object usedRange = com.Track(ComLateBinding.GetProp(contentSheet, "UsedRange"));
                                try
                                {
                                    _rangeValue = ContextInstance.GetRangeText(usedRange, 3);
                                }
                                catch (Exception ex)
                                {
                                    logger.LogWarning(ex);

                                    //如果基于文件流读取异常，回退到基于COM对象读取，兼容不同环境
                                    _rangeValue = ExcelUtils.GetRangeText(workbook, sheet, startColumn, startRow, lastColumn, lastRow, 3, null, null, GetWorkbookOpenPassword(workbook));
                                }

                            }
                            else
                            {
                                _rangeValue = GetRangeTextWithReaderFallback(
                                    workbook,
                                    sheet,
                                    startColumn,
                                    startRow,
                                    lastColumn,
                                    lastRow,
                                    3,
                                    com,
                                    rangeObj,
                                    startRow,
                                    startColumn);
                            }
                            DeleteSheetSilently(contentSheet);
                        }
                        else
                        {
                            if (ExcelConfigurations.Info.GlobalSetting.UseLegacyReader != 0)
                            {
                                _rangeValue = ContextInstance.GetRangeText(rangeObj, 3);
                            }
                            else
                            {
                                _rangeValue = GetRangeTextWithReaderFallback(
                                    workbook,
                                    sheet,
                                    startColumn,
                                    startRow,
                                    lastColumn,
                                    lastRow,
                                    3,
                                    com,
                                    rangeObj,
                                    startRow,
                                    startColumn);
                            }
                        }
                    }
                    else
                    {
                        _rangeValue = ContextInstance.GetRangeValue(workbook, sheet, startRow, startColumn, lastRow, lastColumn, valueType, visible);
                    }
                    ContextInstance.ActionContext.AddResult(_rangeValue);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw;
            }
        }
        void IExcel.ReadRangeV2(string workbook, string sheet, string range, int valueType, bool visible)
        {
            var rangeObj = ExcelUtils.ConvertToExcelRange(range);
            Instance.ReadRange(workbook, sheet, rangeObj.startRow, rangeObj.startColumn, rangeObj.lastRow, rangeObj.lastColumn, rangeObj.type, rangeObj.range, valueType, visible);
        }

        private string GetRangeTextWithReaderFallback(string workbook, string sheet, string startColumn, int startRow, string lastColumn, int lastRow, int mode, ComScope com, object range, int providerStartRow, string providerStartColumn)
        {
            int checkpoint = ExcelUtils.OutputCheckpoint;
            try
            {
                return ExcelUtils.GetRangeText(
                    workbook,
                    sheet,
                    startColumn,
                    startRow,
                    lastColumn,
                    lastRow,
                    mode,
                    CreateNumberFormatProvider(com, range, providerStartRow, providerStartColumn),
                    CreateDisplayTextProvider(com, range, providerStartRow, providerStartColumn),
                    GetWorkbookOpenPassword(workbook));
            }
            catch (Exception ex)
            {
                ExcelUtils.RollbackOutput(checkpoint);
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"ExcelDataReader read failed, fallback to COM Range.Text. workbook:{workbook}, sheet:{sheet}, range:{startColumn}{startRow}:{lastColumn}{lastRow}, mode:{mode}");
                return ContextInstance.GetRangeText(range, mode);
            }
        }

        private Func<int, int, string> CreateNumberFormatProvider(ComScope com, object range, int startRow, string startColumn)
        {
            int startColumnIndex = ExcelUtils.ColumnLetterToIndex(startColumn);
            bool rangeFormatChecked = false;
            string uniformRangeFormat = null;
            return (rowIndex, columnIndex) =>
            {
                int relativeRow = rowIndex - startRow + 1;
                int relativeColumn = columnIndex - startColumnIndex + 1;
                if (relativeRow < 1 || relativeColumn < 1)
                {
                    return null;
                }

                // 每次读取只在首次需要格式补偿时判断一次整个区域，不拆分、不递归。
                // 格式统一时缓存区域格式（包括 General），后续单元格不再访问 COM。
                if (!rangeFormatChecked)
                {
                    rangeFormatChecked = true;
                    try
                    {
                        // 混合格式时 Excel 返回 DBNull，WPS 返回 null，均不能复用为统一格式。
                        uniformRangeFormat = ComLateBinding.GetProp(range, "NumberFormatLocal") as string;
                    }
                    catch (Exception ex)
                    {
                        // 判断失败也不重复尝试，继续逐格获取格式，兼容不同 COM 环境。
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "ExcelDataReader get range number format failed, fallback to per-cell format.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(uniformRangeFormat))
                {
                    return uniformRangeFormat;
                }

                try
                {
                    // 混合或未知格式必须按当前单元格获取，不能把首格格式套用到整列。
                    // 逐格回退可能产生较多 COM 引用，读取后及时释放，避免积累到整个区域读取结束。
                    using (var cellScope = new ComScope())
                    {
                        object cell = cellScope.Track(ComLateBinding.GetProp(range, "Cells", relativeRow, relativeColumn));
                        string numberFormatLocal = Convert.ToString(ComLateBinding.GetProp(cell, "NumberFormatLocal"));
                        logger.LogDebug($"ExcelDataReader COM number format fallback cell:{ExcelUtils.ColumnIndexToLetter(columnIndex)}{rowIndex}, relative:{relativeRow},{relativeColumn}, numberFormatLocal:{numberFormatLocal}");
                        return numberFormatLocal;
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"ExcelDataReader get COM number format failed cell:{ExcelUtils.ColumnIndexToLetter(columnIndex)}{rowIndex}");
                    return null;
                }
            };
        }

        private Func<int, int, string> CreateDisplayTextProvider(ComScope com, object range, int startRow, string startColumn)
        {
            int startColumnIndex = ExcelUtils.ColumnLetterToIndex(startColumn);
            return (rowIndex, columnIndex) =>
            {
                int relativeRow = rowIndex - startRow + 1;
                int relativeColumn = columnIndex - startColumnIndex + 1;
                if (relativeRow < 1 || relativeColumn < 1)
                {
                    return null;
                }

                object cell = null;
                try
                {
                    using (var cellScope = new ComScope())
                    {
                        cell = cellScope.Track(ComLateBinding.GetProp(range, "Cells", relativeRow, relativeColumn));
                        return Convert.ToString(ComLateBinding.GetProp(cell, "Text"));
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"ExcelDataReader get COM display text failed cell:{ExcelUtils.ColumnIndexToLetter(columnIndex)}{rowIndex}");
                    return null;
                }
            };
        }
        /// <summary>
        /// 读取单元格公式
        /// </summary>       
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>  
        void IExcel.ReadCellFormula(string workbook, string sheet, string column, string row)
        {
            using (var com = new ComScope())
            {
                object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                object rangeObj = com.Track(ComLateBinding.GetProp(worksheet, "Range", $"{column}{row}"));
                string formula = string.Empty;
                if (ComLateBinding.ToBool(ComLateBinding.GetProp(rangeObj, "HasFormula")))
                {
                    formula = Convert.ToString(ComLateBinding.GetProp(rangeObj, "Formula"));
                }
                ContextInstance.ActionContext.AddResult("strFormula", formula);
            }
        }

        /// <summary>
        /// 读取单元格背景颜色
        /// </summary>        
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>        
        void IExcel.ReadCellBackground(string workbook, string sheet, string column, string row, int format)
        {
            using (var com = new ComScope())
            {
                object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                object rangeObj = com.Track(ComLateBinding.GetProp(worksheet, "Range", $"{column}{row}"));
                object interior = com.Track(ComLateBinding.GetProp(rangeObj, "Interior"));
                System.Drawing.Color color = System.Drawing.ColorTranslator.FromOle(ComLateBinding.ToInt(ComLateBinding.GetProp(interior, "Color")));
                if (format == 0)
                {
                    ContextInstance.ActionContext.AddResult(new JProperty("strColor", new int[3] { color.R, color.G, color.B }));
                }
                else
                {
                    ContextInstance.ActionContext.AddResult("strColor", $"#{color.R:X2}{color.G:X2}{color.B:X2}");
                }
            }
        }

        /// <summary>
        /// 读取行
        /// </summary>      
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="rowIndex">行号</param>  
        /// <param name="valueType">读取值类型</param>
        /// <param name="visible">是否读取显示的行</param>
        void IExcel.ReadRow(string workbook, string sheet, int rowIndex, int valueType, bool visible)
        {
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                var excRange = ContextInstance.ActionContext.ExcelUsedRange;
                object _usedRange = com.Track(ComLateBinding.GetProp(_sheet, "Range", excRange.Address));

                object cell1 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", rowIndex, "A"));
                object c = com.Track(ComLateBinding.GetProp(_usedRange, "Columns"));
                object cell2 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", rowIndex, ComLateBinding.ToInt(ComLateBinding.GetProp(_usedRange, "Column")) + ComLateBinding.ToInt(ComLateBinding.GetProp(c, "Count")) - 1));
                object _range = com.Track(ComLateBinding.GetProp(_sheet, "Range", cell1, cell2));

                object entireRow = com.Track(ComLateBinding.GetProp(_range, "EntireRow"));
                if (visible && ComLateBinding.ToBool(ComLateBinding.GetProp(entireRow, "Hidden")))
                {
                    //隐藏行
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
                            ContextInstance.GetRangeText(_range2, 1);
                        }
                        else
                        {
                            GetRangeTextWithReaderFallback(
                                workbook,
                                Convert.ToString(ComLateBinding.GetProp(_sheet, "Name")),
                                "A",
                                rowIndex,
                                excRange.EndColumn,
                                rowIndex,
                                1,
                                com,
                                _range,
                                rowIndex,
                                "A");
                        }
                    }
                    else
                    {
                        object rangeValue = GetRangeValueByType(_range2, valueType);
                        ContextInstance.ActionContext["__VALUE__"] = rangeValue;
                        ExcelUtils.SerializerDataToBinaryFile(rangeValue);
                    }
                }
                ContextInstance.ActionContext.AddResult(ExcelUtils.TEMP_FILEPATH);
            }
        }

        /// <summary>
        /// 读取列
        /// </summary>      
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="columnName">列名</param>  
        /// <param name="valueType">读取值类型</param>
        /// <param name="visible">是否读取显示的行</param>
        void IExcel.ReadColumn(string workbook, string sheet, string columnName, int valueType, bool visible)
        {
            if (!ExcelUtils.StreamingEnabled)
            {
                LegacyReadColumn(workbook, sheet, columnName, valueType, visible);
                return;
            }
            ReadColumnChunks(workbook, sheet, columnName, valueType, visible);
            ContextInstance.ActionContext.AddResult(ExcelUtils.TEMP_FILEPATH);
        }

        private IEnumerable<object> ReadColumnValueBlocks(object worksheet, string column, int lastRow, int valueType)
        {
            int batchSize = ExcelConfigurations.Info.GlobalSetting.BatchSize;
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            for (int start = 1; start <= lastRow; start += batchSize)
            {
                object value;
                using (var scope = new ComScope())
                {
                    int end = Math.Min(start + batchSize - 1, lastRow);
                    object range = scope.Track(ComLateBinding.GetProp(worksheet, "Range", $"{column}{start}:{column}{end}"));
                    value = GetRangeValueByType(range, valueType);
                }
                // COM returns null for an empty single cell; preserve that row
                // instead of passing null to the serializer (which skips it).
                yield return value ?? new object[] { null };
            }
        }

        private IEnumerable<object> ReadColumnValues(string workbook, string sheet, string column)
        {
            using (var scope = new ComScope())
            {
                object worksheet = scope.Track(GetWorksheetByName(workbook, sheet));
                foreach (object block in ReadColumnValueBlocks(worksheet, column,
                    ContextInstance.ActionContext.ExcelUsedRange.EndRow, 0))
                {
                    if (block is Array values)
                    {
                        foreach (object value in values) yield return value;
                    }
                    else yield return block;
                }
            }
        }

        private void ReadColumnChunks(string workbook, string sheet, string column, int valueType, bool visible)
        {
            int batchSize = ExcelConfigurations.Info.GlobalSetting.BatchSize;
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            using (var worksheetScope = new ComScope())
            {
                object worksheet = worksheetScope.Track(GetWorksheetByName(workbook, sheet));
                int lastRow = ContextInstance.ActionContext.ExcelUsedRange.EndRow;
                if (visible)
                {
                    object _sheet = worksheetScope.Track(GetWorksheetByName(workbook, sheet));
                    var excRange = ContextInstance.ActionContext.ExcelUsedRange;
                    object _usedRange = worksheetScope.Track(ComLateBinding.GetProp(_sheet, "Range", excRange.Address));

                    object cell1 = worksheetScope.Track(ComLateBinding.GetProp(_sheet, "Cells", 1, column));
                    object r = worksheetScope.Track(ComLateBinding.GetProp(_usedRange, "Rows"));
                    object cell2 = worksheetScope.Track(ComLateBinding.GetProp(_sheet, "Cells", ComLateBinding.ToInt(ComLateBinding.GetProp(_usedRange, "Row")) + ComLateBinding.ToInt(ComLateBinding.GetProp(r, "Count")) - 1, column));
                    object _range = worksheetScope.Track(ComLateBinding.GetProp(_sheet, "Range", cell1, cell2));

                    // WPS 12.x 的 COM 实现对 Worksheet.Range("A:A") 直接读 Hidden
                    // 会返回 0x80004005 (E_FAIL)。不要把这里“优化”成
                    // Range($"{column}:{column}").Hidden；必须先构造有界 Range，
                    // 再通过 EntireColumn 读取 Hidden。
                    object entireCol = worksheetScope.Track(ComLateBinding.GetProp(_range, "EntireColumn"));
                    if (ComLateBinding.ToBool(ComLateBinding.GetProp(entireCol, "Hidden"))) 
                    {

                        ExcelUtils.SerializerDataToBinaryFile(new object[0]);
                        return;
                    }

                }
                if (!visible && valueType != 1)
                {
                    foreach (object block in ReadColumnValueBlocks(worksheet, column, lastRow, valueType))
                        ExcelUtils.SerializerDataToBinaryFile(block);
                    if (ExcelUtils.TEMP_FILEPATH == null) ExcelUtils.SerializerDataToBinaryFile(new object[0]);
                    return;
                }
                if (!visible && valueType == 1 && ExcelConfigurations.Info.GlobalSetting.UseLegacyReader == 0)
                {
                    object range = worksheetScope.Track(ComLateBinding.GetProp(worksheet, "Range", $"{column}1:{column}{lastRow}"));
                    // One reader pass produces all vector chunks; reopening the
                    // file for every batch would repeatedly scan preceding rows.
                    GetRangeTextWithReaderFallback(workbook, sheet, column, 1, column, lastRow,
                        2, worksheetScope, range, 1, column);
                    return;
                }
                for (int start = 1; start <= lastRow; start += batchSize)
                {
                    using (var blockScope = new ComScope())
                    {
                        int end = Math.Min(start + batchSize - 1, lastRow);
                        object range = blockScope.Track(ComLateBinding.GetProp(worksheet, "Range", $"{column}{start}:{column}{end}"));
                        if (visible && start == end)
                        {
                            object entireRow = blockScope.Track(ComLateBinding.GetProp(range, "EntireRow"));
                            if (ComLateBinding.ToBool(ComLateBinding.GetProp(entireRow, "Hidden"))) continue;
                        }
                        // SpecialCells on a single cell may expand to the sheet's
                        // UsedRange. A one-cell tail has already been checked above.
                        if (visible && start < end)
                        {
                            object visibleRange;
                            try { visibleRange = blockScope.Track(ComLateBinding.Invoke(range, "SpecialCells", 12)); }
                            catch (System.Runtime.InteropServices.COMException ex) when (ex.ErrorCode == unchecked((int)0x800A03EC)) { continue; }
                            object areas = blockScope.Track(ComLateBinding.GetProp(visibleRange, "Areas"));
                            int areaCount = ComLateBinding.ToInt(ComLateBinding.GetProp(areas, "Count"));
                            for (int i = 1; i <= areaCount; i++)
                            {
                                using (var areaScope = new ComScope())
                                {
                                    object area = areaScope.Track(GetExcelItem(areas, i));
                                    if (valueType == 1) ContextInstance.GetRangeText(area, 2);
                                    else ExcelUtils.SerializerDataToBinaryFile(GetRangeValueByType(area, valueType));
                                }
                            }
                        }
                        else if (valueType == 1)
                        {
                            // Text providers and the reader fallback retain their
                            // existing formatting rules for this bounded range.
                            if (ExcelConfigurations.Info.GlobalSetting.UseLegacyReader != 0)
                                ContextInstance.GetRangeText(range, 2);
                            else GetRangeTextWithReaderFallback(workbook, sheet, column, start, column, end,
                                2, blockScope, range, start, column);
                        }
                        else ExcelUtils.SerializerDataToBinaryFile(GetRangeValueByType(range, valueType));
                    }
                }
                if (ExcelUtils.TEMP_FILEPATH == null) ExcelUtils.SerializerDataToBinaryFile(new object[0]);
            }
        }

        /// <summary>
        /// 读取行数
        /// </summary>      
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="range">读取范围["整个Sheet","指定一列"]</param>  
        /// <param name="column">列名</param>  
        void IExcel.ReadRowCount(string workbook, string sheet, string range, string column)
        {
            int numRet;
            //只获取显示的行
            bool ignoreHidden = Convert.ToBoolean(ActionContext["ignoreHidden"]);
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                object findRange = null;

                if (range.Equals("指定一列"))
                {
                    if (ignoreHidden)
                    {
                        object app = com.Track(ComLateBinding.GetProp(_sheet, "Application"));
                        object wsf = com.Track(ComLateBinding.GetProp(app, "WorksheetFunction"));
                        object cols = com.Track(ComLateBinding.GetProp(_sheet, "Columns"));
                        object targetCol = com.Track(ComLateBinding.GetItem(cols,column));

                        numRet = ComLateBinding.ToInt(ComLateBinding.Invoke(wsf, "Subtotal", 103, targetCol));
                    }
                    else
                    {
                        findRange = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{column}1:{column}{ActionContext.ExcelUsedRange.EndRow}"));
                        var lastCell = GetLastCell(findRange, 1, 2);
                        numRet = lastCell.Item2;
                    }
                }
                else
                {
                    if (ignoreHidden)
                    {
                        List<int> rows = new List<int>();
                        object usedRange = com.Track(ComLateBinding.GetProp(_sheet, "UsedRange"));
                        object columns = com.Track(ComLateBinding.GetProp(usedRange, "Columns"));
                        int columnsCount = ComLateBinding.ToInt(ComLateBinding.GetProp(columns, "Count"));
                        for (int i = 1; i <= columnsCount; i++)
                        {
                            object col = com.Track(GetExcelItem(columns, i));
                            object app = com.Track(ComLateBinding.GetProp(_sheet, "Application"));
                            object wsf = com.Track(ComLateBinding.GetProp(app, "WorksheetFunction"));
                            object sheetCols = com.Track(ComLateBinding.GetItem(columns, ExcelUtils.ColumnIndexToLetter(ComLateBinding.ToInt(ComLateBinding.GetProp(col, "Column")))));
                            var rowCount = ComLateBinding.ToInt(ComLateBinding.Invoke(wsf, "Subtotal", 103, sheetCols));
                            rows.Add(rowCount);
                        }
                        numRet = rows.Max();
                    }
                    else
                    {
                        findRange = com.Track(ComLateBinding.GetProp(_sheet, "Range", ContextInstance.ActionContext.ExcelUsedRange.Address));
                        numRet = ContextInstance.ActionContext.ExcelUsedRange.EndRow;
                    }
                }
                if (!ignoreHidden && numRet == 1)
                {
                    object firstCell = com.Track(ComLateBinding.GetProp(findRange, "Cells", 1, 1));
                    if (string.IsNullOrWhiteSpace(Convert.ToString(ComLateBinding.GetProp(firstCell, "Formula"))))
                    {
                        numRet = 0;
                    }
                }
            }
            ContextInstance.ActionContext.Result.RemoveAll();
            ContextInstance.ActionContext.AddResult("numRet", numRet.ToString());
        }

        /// <summary>
        /// 读取列数
        /// </summary>      
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="range">读取范围["整个Sheet","指定一行"]</param>  
        /// <param name="row">行号</param>  
        void IExcel.ReadColumnCount(string workbook, string sheet, string range, int row)
        {
            int numRet;
            //只获取显示的列
            bool ignoreHidden = Convert.ToBoolean(ActionContext["ignoreHidden"]);
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                object findRange = null;

                if (range.Equals("指定一行"))
                {
                    findRange = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{ActionContext.ExcelUsedRange.StartColumn}{row}:{ActionContext.ExcelUsedRange.EndColumn}{row}"));
                    var lastCell = GetLastCell(findRange, 2, 2);
                    numRet = lastCell.Item1;

                    if (ignoreHidden)
                    {
                        object cols = com.Track(ComLateBinding.GetProp(_sheet, "Columns"));
                        for (int i = 1; i <= numRet; i++)
                        {
                            object columnObj = com.Track(GetExcelItem(cols, i));
                            object entireCol = com.Track(ComLateBinding.GetProp(columnObj, "EntireColumn"));
                            if (ComLateBinding.ToBool(ComLateBinding.GetProp(entireCol, "Hidden")))
                            {
                                numRet = numRet - 1;
                            }
                        }
                    }

                }
                else
                {
                    findRange = com.Track(ComLateBinding.GetProp(_sheet, "Range", ContextInstance.ActionContext.ExcelUsedRange.Address));
                    numRet = ContextInstance.ActionContext.ExcelUsedRange.EndColumnIndex;

                    if (ignoreHidden)
                    {
                        object cols = com.Track(ComLateBinding.GetProp(_sheet, "Columns"));
                        for (int i = 1; i <= numRet; i++)
                        {
                            object columnObj = com.Track(GetExcelItem(cols, i));
                            object entireCol = com.Track(ComLateBinding.GetProp(columnObj, "EntireColumn"));
                            if (ComLateBinding.ToBool(ComLateBinding.GetProp(entireCol, "Hidden")))
                            {
                                numRet = numRet - 1;
                            }
                        }
                    }
                }
                if (numRet == 1)
                {
                    object firstCell = com.Track(ComLateBinding.GetProp(findRange, "Cells", 1, 1));
                    if (string.IsNullOrWhiteSpace(Convert.ToString(ComLateBinding.GetProp(firstCell, "Formula"))))
                    {
                        numRet = 0;
                    }
                }
            }
            ContextInstance.ActionContext.Result.RemoveAll();
            ContextInstance.ActionContext.AddResult("numRet", numRet);
        }

        /// <summary>
        /// 获取第一个空白单元格
        /// </summary>    
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="range">获取范围["指定一列","指定一行"]</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>  
        void IExcel.GetEmptyCell(string workbook, string sheet, string range, string column, int row)
        {
            if (!ExcelUtils.StreamingEnabled)
            {
                LegacyGetEmptyCell(workbook, sheet, range, column, row);
                return;
            }
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
                    firstCell = ExcelUtils.ScanEmptyCell(ReadColumnValues(workbook, sheet, column), false).ToString();
                }
            }
            ContextInstance.ActionContext.AddResult("firstCell", firstCell);
        }

        /// <summary>
        /// 获取最后一个空白单元格
        /// </summary>       
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="range">获取范围["指定一列","指定一行"]</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>  
        void IExcel.GetLastEmptyCell(string workbook, string sheet, string range, string column, int row)
        {
            if (!ExcelUtils.StreamingEnabled)
            {
                LegacyGetLastEmptyCell(workbook, sheet, range, column, row);
                return;
            }
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
                    lastCell = ExcelUtils.ScanEmptyCell(ReadColumnValues(workbook, sheet, column), true).ToString();
                }
            }
            ContextInstance.ActionContext.AddResult("lastCell", lastCell);
        }

        /// <summary>
        /// 获取已编辑区域
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>      
        void IExcel.GetEditRange(string workbook, string sheet)
        {
            string startColumn;
            int startRowNum, endRowNum = 0;
            int lastColumnIndex = 0;
            using (var com = new ComScope())
            {
                object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                object usedRange = com.Track(ComLateBinding.GetProp(worksheet, "UsedRange"));
                startRowNum = ComLateBinding.ToInt(ComLateBinding.GetProp(usedRange, "Row"));
                startColumn = ExcelUtils.ColumnIndexToLetter(ComLateBinding.ToInt(ComLateBinding.GetProp(usedRange, "Column")));

                object r = com.Track(ComLateBinding.GetProp(usedRange, "Rows"));
                object c = com.Track(ComLateBinding.GetProp(usedRange, "Columns"));

                if (startRowNum == 1 && ComLateBinding.ToInt(ComLateBinding.GetProp(usedRange, "Column")) == 1 && ComLateBinding.ToInt(ComLateBinding.GetProp(r, "Count")) == 1 && ComLateBinding.ToInt(ComLateBinding.GetProp(c, "Count")) == 1)
                {
                    endRowNum = 1;
                    lastColumnIndex = 1;
                }
                else
                {
                    var lastColumnCell = GetLastCell(usedRange, 2, 2);
                    var lastRowCell = GetLastCell(usedRange, 1, 2);
                    endRowNum = lastRowCell.Item2;
                    lastColumnIndex = lastColumnCell.Item1;
                }
            }
            ContextInstance.ActionContext.AddResult("startRowNum", startRowNum);
            ContextInstance.ActionContext.AddResult("startColumn", startColumn);
            ContextInstance.ActionContext.AddResult("endRowNum", endRowNum == 0 ? 1 : endRowNum);
            ContextInstance.ActionContext.AddResult("endColumn", lastColumnIndex == 0 ? "A" : ExcelUtils.ColumnIndexToLetter(lastColumnIndex));
            logger.LogInformation($"Get Sheet UsedRange:{startColumn}{startRowNum}:{ContextInstance.ActionContext.Result["endColumn"]}{endRowNum}");
        }

        /// <summary>
        /// 查找替换
        /// </summary>
        /// <param name="workbook">工作簿对象</param>
        /// <param name="sheet">工作表</param>
        /// <param name="mode">查找模式【查找、查找替换】</param>
        /// <param name="option">区域选项【区域、行、列、已编辑区域】</param>
        /// <param name="range">区域值</param>
        /// <param name="_input">查找内容</param>
        /// <param name="_replace">替换内容</param>
        /// <param name="wholeWord">全字匹配</param>
        /// <param name="matchRange">查找范围</param>
        /// <param name="outType">输出类型【行号/列名、单元格】</param>      
        void IExcel.Search(string workbook, string sheet, string mode, string option, string range, object _input, object _replace, bool wholeWord, string matchRange, int outType)
        {
            string input = _input.ToString();
            string replace = _replace.ToString();
            StringBuilder retBuilder = new StringBuilder();
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                object cellRange = null;
                object usedRange = com.Track(ComLateBinding.GetProp(_sheet, "Range", ContextInstance.ActionContext.ExcelUsedRange.Address));
                switch (option)
                {
                    case "区域":
                        {
                            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
                            cellRange = com.Track(GetRange(workbook, sheet, rangeInfo.startRow, rangeInfo.startColumn, rangeInfo.lastRow, rangeInfo.lastColumn, rangeInfo.type, rangeInfo.range));
                            break;
                        }
                    case "行":
                        var __row = ExcelUtils.ConvertNegativeRow(range);
                        object c = com.Track(ComLateBinding.GetProp(usedRange, "Columns"));
                        cellRange = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"A{__row}:{ExcelUtils.ColumnIndexToLetter(ComLateBinding.ToInt(ComLateBinding.GetProp(c, "Count")) + ComLateBinding.ToInt(ComLateBinding.GetProp(usedRange, "Column")) - 1)}{__row}"));
                        break;
                    case "列":
                        var __column = ExcelUtils.ConvertNegativeColumn(range);
                        object r = com.Track(ComLateBinding.GetProp(usedRange, "Rows"));
                        cellRange = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{__column}1:{__column}{ComLateBinding.ToInt(ComLateBinding.GetProp(r, "Count"))}"));
                        break;

                    case "已编辑区域":
                    default:
                        cellRange = usedRange; break;
                }
                retBuilder.Append("[");
                var retSet = new List<string>();
                foreach (object cell in (System.Collections.IEnumerable)cellRange)
                {
                    com.Track(cell);
                    string cellValue = Convert.ToString(ComLateBinding.GetProp(cell, "Text")) ?? "";
                    if (cellValue != string.Empty && string.IsNullOrEmpty(input))
                    {
                        continue;
                    }
                    if ((wholeWord && cellValue.Equals(input)) || (!wholeWord && cellValue.Contains(input)))
                    {
                        int cellColumn = ComLateBinding.ToInt(ComLateBinding.GetProp(cell, "Column"));
                        int cellRow = ComLateBinding.ToInt(ComLateBinding.GetProp(cell, "Row"));
                        retSet.Add(outType == 0 ? $"['{ExcelUtils.ColumnIndexToLetter(cellColumn)}',{cellRow}]" : $"'{ExcelUtils.ColumnIndexToLetter(cellColumn)}{cellRow}'");
                        if (mode == "查找并替换")
                        {
                            //替换空单元格
                            if (string.IsNullOrWhiteSpace(input) && string.IsNullOrWhiteSpace(cellValue))
                            {
                                ComLateBinding.SetProp(cell, "Value", replace);
                            }
                            else
                            {
                                ComLateBinding.SetProp(cell, "Value", cellValue.Replace(input, replace));
                            }
                        }

                        //only match first
                        if (matchRange.Equals("第一个"))
                            break;
                    }
                }
                retBuilder.Append($"{string.Join(",", retSet)}]");
            }
            ContextInstance.ActionContext.AddResult("arrayRet", retBuilder.ToString());
        }

        /// <summary>
        /// 导出区域为图片
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="range">区域</param>
        /// <param name="path">路径</param>
        /// <param name="name">文件名</param>
        /// <param name="cover">是否覆盖</param>
        void IExcel.ExportRangeAsPicture(string workbook, string sheet, string range, string path, string name, bool cover)
        {
            ExportRangeAsPicture(workbook, sheet, range, path, name, cover, ExcelUtils.GetAbsolutePath);
        }

        void IExcel.ExportRangeAsPictureV2(string workbook, string sheet, string range, string path, string name, bool cover)
        {
            ExportRangeAsPicture(
                workbook,
                sheet,
                range,
                path,
                NormalizePictureNameV2(workbook, sheet, range, name),
                cover,
                ExcelUtils.GetAbsolutePathV2);
        }

        private string NormalizePictureNameV2(string workbook, string sheet, string range, string name)
        {
            // 用户填写名称时，不拼接工作簿名、Sheet 名和区域标识。
            if (!string.IsNullOrWhiteSpace(name))
            {
                return Path.GetExtension(name).Equals(".png", StringComparison.OrdinalIgnoreCase)
                    ? SanitizeFileName(name)
                    : Path.ChangeExtension(SanitizeFileName(name), ".png");
            }

            // 未填写名称时，根据实际工作簿、工作表和区域生成默认名称。
            try
            {
                using (var com = new ComScope())
                {
                    object workbookObject = com.Track(GetWorkbook(workbook));
                    object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                    string workbookName = Path.GetFileNameWithoutExtension(Convert.ToString(ComLateBinding.GetProp(workbookObject, "Name")));
                    string sheetName = Convert.ToString(ComLateBinding.GetProp(worksheet, "Name"));
                    name = $"{workbookName}_{sheetName}_{GetPictureRangeName(range)}";
                }
            }
            catch
            {
                name = "区域图片".I18n("SheetInfo_91070030");
            }

            return Path.ChangeExtension(SanitizeFileName(name), ".png");
        }

        /// <summary>
        /// 使用 .NET 的路径 API 清理 Windows 文件名中的非法字符。
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        }

        /// <summary>
        /// 根据新版区域参数生成文件名中的区域标识。
        /// </summary>
        private string GetPictureRangeName(string range)
        {
            try
            {
                var rawRange = JObject.Parse(range);
                var rangeInfo = ExcelUtils.ConvertToExcelRange(range);

                switch (rawRange.Value<int?>("option") ?? 3)
                {
                    case 0:
                        return $"{Convert.ToString(rawRange["column"])}{Convert.ToInt32(rawRange["row"])}";
                    case 1:
                        int row = Convert.ToInt32(rawRange["row"]);
                        return row < 0
                            ? Localize($"倒数第{Math.Abs(row)}行", "SheetInfo_639FA33F")
                            : Localize($"第{row}行", "SheetInfo_867F4D00");
                    case 2:
                        string column = Convert.ToString(rawRange["column"]);
                        if (int.TryParse(column, out int columnNumber))
                        {
                            return columnNumber < 0
                                ? Localize($"倒数第{Math.Abs(columnNumber)}列", "SheetInfo_94CCFE45")
                                : Localize($"第{columnNumber}列", "SheetInfo_895C4C97");
                        }

                        return Localize($"{column}列", "SheetInfo_1652225A");
                    case 4:
                        return "已编辑区域".I18n("SheetInfo_25A4A831");
                    default:
                        return rangeInfo.type == 0
                            ? $"{rangeInfo.startColumn}{rangeInfo.startRow}_{rangeInfo.lastColumn}{rangeInfo.lastRow}"
                            : ExcelUtils.ConvertExcelRangeAddress(rangeInfo, true).Replace(":", "_");
                }
            }
            catch
            {
                return "区域图片".I18n("SheetInfo_91070030");
            }
        }

        private void ExportRangeAsPicture(string workbook, string sheet, string range, string path, string name, bool cover,
            Func<string, string, string> getAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"{DateTime.UtcNow.Ticks}.png";
            }

            if (!name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                name += ".png";
            }

            var file = getAbsolutePath(path, name);
            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (File.Exists(file))
            {
                if (!cover)
                {
                    file = ExcelUtils.GetDuplicateFileName(file);
                }
                else
                {
                    File.Delete(file);
                }
            }

            try
            {
                lock (exportRangeAsPictureSyncRoot)
                {
                    ExportRangeAsPictureCore(workbook, sheet, range, file);
                }

                if (!File.Exists(file))
                {
                    throw new ExcelException("导出图片失败！".I18n("SheetInfo_DD341061"));
                }

                ContextInstance.ActionContext.AddResult("filePath", file);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new ExcelException("导出图片失败！".I18n("SheetInfo_DD341061"));
            }
        }

        private object FindExcelCell(object cellRange, object what, object after, int lookIn, int lookAt, int searchOrder, int searchDirection, bool matchCase, bool matchByte)
        {
            return ComLateBinding.Invoke(
                cellRange,
                "Find",
                what,
                after,
                lookIn,
                lookAt,
                searchOrder,
                searchDirection,
                matchCase,
                matchByte,
                Type.Missing);
        }

        /// <summary>
        /// Excel 查找替换
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="mode">功能模式（查找0，替换1）</param>
        /// <param name="option">区域选项（行0，列1，区域2，已编辑3）</param>
        /// <param name="range">区域</param>
        /// <param name="row">行号（行选项）</param>
        /// <param name="column">列名（列选项）</param>
        /// <param name="what">查找内容</param>
        /// <param name="lookAt">全字匹配</param>
        /// <param name="lookIn">查找方式</param>
        /// <param name="searchOrder">查找顺序</param>
        /// <param name="searchDirection">查找方向</param>
        /// <param name="matchCase">匹配大小写（否0，是1）</param>
        /// <param name="matchByte">匹配字节（否0，是1）</param>
        /// <param name="matchFormat">匹配格式</param>
        /// <param name="replacement">替换内容</param>
        /// <param name="replaceFormat">替换格式</param>
        /// <param name="matchFirst">查找结果（所有0，第一个1）</param>
        /// <param name="returnType">返回值类型（行号0，位置1）</param>
        void IExcel.Find(string workbook, string sheet, int mode, int option, string range, int row, string column, string what, int lookAt, int lookIn, int searchOrder, int searchDirection, int matchCase, int matchByte, string matchFormat, string replacement, string replaceFormat, int matchFirst, int returnType)
        {
            StringBuilder retBuilder = new StringBuilder();
            using (var com = new ComScope())
            {
                object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                object cellRange = null;
                switch (option)
                {
                    case 2:
                        {
                            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
                            cellRange = com.Track(ComLateBinding.GetProp(worksheet, "Range", ExcelUtils.ConvertExcelRangeAddress(rangeInfo)));
                            break;
                        }
                    case 0:
                        var __row = ExcelUtils.ConvertNegativeRow(range);
                        cellRange = com.Track(ComLateBinding.GetProp(worksheet, "Range", $"A{__row}:{ContextInstance.ActionContext.ExcelUsedRange.EndColumn}{__row}"));
                        break;
                    case 1:
                        var __column = ExcelUtils.ConvertNegativeColumn(range);
                        cellRange = com.Track(ComLateBinding.GetProp(worksheet, "Range", $"{__column}1:{__column}{ContextInstance.ActionContext.ExcelUsedRange.EndRow}"));
                        break;

                    case 3:
                    default:
                        cellRange = com.Track(ComLateBinding.GetProp(worksheet, "Range", ContextInstance.ActionContext.ExcelUsedRange.Address)); break;
                }
                var ConvertRange = new Func<object, string>(rng =>
                {
                    if (rng == null)
                    {
                        return returnType == 0 ? "[]" : "";
                    }
                    int rngColumn = ComLateBinding.ToInt(ComLateBinding.GetProp(rng, "Column"));
                    int rngRow = ComLateBinding.ToInt(ComLateBinding.GetProp(rng, "Row"));
                    if (returnType == 0)
                    {
                        return $"['{ExcelUtils.ColumnIndexToLetter(rngColumn)}',{rngRow}]";
                    }
                    else
                    {
                        return $"'{ExcelUtils.ColumnIndexToLetter(rngColumn)}{rngRow}'";
                    }
                });
                if (mode == 0)
                {
                    object app = com.Track(ComLateBinding.GetProp(worksheet, "Application"));
                    object findFmt = com.Track(ComLateBinding.GetProp(app, "FindFormat"));
                    ComLateBinding.Invoke(findFmt, "Clear");

                    object r = com.Track(ComLateBinding.GetProp(cellRange, "Rows"));
                    object c = com.Track(ComLateBinding.GetProp(cellRange, "Columns"));
                    object startCell = searchDirection == 1
                        ? com.Track(ComLateBinding.GetProp(cellRange, "Cells", ComLateBinding.ToInt(ComLateBinding.GetProp(r, "Count")), ComLateBinding.ToInt(ComLateBinding.GetProp(c, "Count"))))
                        : com.Track(ComLateBinding.GetProp(cellRange, "Cells", 1, 1));
                    object findResult = com.Track(FindExcelCell(cellRange, what, startCell, lookIn, lookAt == 0 ? 2 : 1, searchOrder, searchDirection, matchCase == 1, matchByte == 1));
                    if (matchFirst == 0)
                    {
                        string firstResult = findResult == null ? null : Convert.ToString(ComLateBinding.GetProp(findResult, "Address"));
                        retBuilder.Append("[");
                        while (findResult != null)
                        {
#if DEBUG
                            if (ComLateBinding.ToInt(ComLateBinding.GetProp(findResult, "Row")) % 20000 == 0)
                            {
                                logger.LogInformation($"current cell :{ComLateBinding.GetProp(findResult, "Address")}");
                            }
#endif

                            retBuilder.Append(ConvertRange(findResult));
                            retBuilder.Append(",");
                            if (searchDirection == 1)
                            {
                                findResult = com.Track(ComLateBinding.Invoke(cellRange, "FindNext", findResult));
                            }
                            else
                            {
                                findResult = com.Track(ComLateBinding.Invoke(cellRange, "FindPrevious", findResult));
                            }
                            if (findResult == null || Convert.ToString(ComLateBinding.GetProp(findResult, "Address")) == firstResult)
                            {
                                break;
                            }
                        }
                        if (retBuilder.Length > 1)
                        {
                            retBuilder.Remove(retBuilder.Length - 1, 1);
                        }
                        retBuilder.Append("]");
                    }
                    else
                    {
                        retBuilder.Append(ConvertRange(findResult));
                    }
                }
                else
                {
                    if (ContextInstance.ActionContext["replacementType"] != null && ContextInstance.ActionContext["replacementType"] == 1)
                    {
                        //替换整个单元格
                        object app = com.Track(ComLateBinding.GetProp(worksheet, "Application"));
                        object findFmt = com.Track(ComLateBinding.GetProp(app, "FindFormat"));
                        ComLateBinding.Invoke(findFmt, "Clear");

                        object r = com.Track(ComLateBinding.GetProp(cellRange, "Rows"));
                        object c = com.Track(ComLateBinding.GetProp(cellRange, "Columns"));
                        object startCell = searchDirection == 1
                            ? com.Track(ComLateBinding.GetProp(cellRange, "Cells", ComLateBinding.ToInt(ComLateBinding.GetProp(r, "Count")), ComLateBinding.ToInt(ComLateBinding.GetProp(c, "Count"))))
                            : com.Track(ComLateBinding.GetProp(cellRange, "Cells", 1, 1));
                        object findResult = com.Track(FindExcelCell(cellRange, what, startCell, lookIn, lookAt == 0 ? 2 : 1, searchOrder, searchDirection, matchCase == 1, matchByte == 1));
                        string firstResult = findResult == null ? null : Convert.ToString(ComLateBinding.GetProp(findResult, "Address"));
                        while (findResult != null)
                        {
                            ComLateBinding.SetProp(findResult, "Value", replacement);
                            if (searchDirection == 1)
                            {
                                findResult = com.Track(ComLateBinding.Invoke(cellRange, "FindNext", findResult));
                            }
                            else
                            {
                                findResult = com.Track(ComLateBinding.Invoke(cellRange, "FindPrevious", findResult));
                            }
                            if (findResult == null || Convert.ToString(ComLateBinding.GetProp(findResult, "Address")) == firstResult)
                            {
                                break;
                            }
                        }
                        retBuilder.Append("True");
                    }
                    else
                    {
                        object app = com.Track(ComLateBinding.GetProp(worksheet, "Application"));
                        object replaceFmt = com.Track(ComLateBinding.GetProp(app, "ReplaceFormat"));
                        ComLateBinding.Invoke(replaceFmt, "Clear");
                        object replaceResult = ComLateBinding.Invoke(
                            cellRange,
                            "Replace",
                            what,
                            replacement,
                            lookAt == 0 ? 2 : 1,
                            searchOrder,
                            matchCase == 1,
                            matchByte == 1,
                            Type.Missing,
                            Type.Missing);
                        retBuilder.Append(replaceResult.ToString());
                    }

                }
            }

            ContextInstance.ActionContext.Result.RemoveAll();
            ContextInstance.ActionContext.AddResult("arrayRet", retBuilder.ToString());
        }


        /// <summary>
        /// 内容循环
        /// </summary>      
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="startRow">开始行</param>
        /// <param name="endRow">结束行</param>
        /// <param name="startColumn">开始列</param>
        /// <param name="endColumn">结束列</param>
        /// <param name="regionType">循环标识【1:行,2:列,3:区域,4:已编辑区域】</param>
        void IExcel.Loop(string workbook, string sheet, int startRow, int endRow, string startColumn, string endColumn, int regionType, int valueType)
        {
            Tuple<string, int, string, int> editRange = new Tuple<string, int, string, int>(
                 ContextInstance.ActionContext.ExcelUsedRange.StartColumn,
                 ContextInstance.ActionContext.ExcelUsedRange.StartRow,
                 ContextInstance.ActionContext.ExcelUsedRange.EndColumn,
                 ContextInstance.ActionContext.ExcelUsedRange.EndRow
                );
            logger.LogInformation($"loop edit ragne: {editRange}");
            object[] loopKeys = null;
            switch (regionType)
            {
                case 1:
                    {
                        Instance.ReadRange(workbook, sheet, startRow, editRange.Item1, endRow, editRange.Item3, 0, null, valueType, false);
                    }
                    break;
                case 2:
                    {
                        Instance.ReadRange(workbook, sheet, editRange.Item2, startColumn, editRange.Item4, endColumn, 0, null, valueType, false);
                    }
                    break;
                case 3:
                    {
                        Instance.ReadRange(workbook, sheet, startRow, startColumn, endRow, endColumn, 0, null, valueType, false);
                    }
                    break;
                case 4:
                    {
                        Instance.ReadRange(workbook, sheet, editRange.Item2, editRange.Item1, editRange.Item4, editRange.Item3, 0, null, valueType, false);
                    }
                    break;
                default:
                    throw new ExcelException("未知的循环标识".I18n("SheetInfo_CD99B8F8"));
            }
            if (regionType == 2)
            {
                int start_col_index = ExcelUtils.ColumnLetterToIndex(startColumn);
                int end_col_index = ExcelUtils.ColumnLetterToIndex(endColumn);
                loopKeys = new object[Math.Abs(end_col_index - start_col_index) + 1];
                var _startKey = start_col_index > end_col_index ? end_col_index : start_col_index;
                for (int i = 0; i < loopKeys.Length; i++)
                {
                    loopKeys[i] = ExcelUtils.ColumnIndexToLetter(_startKey + i);
                }
            }
            else if (regionType == 4)
            {
                loopKeys = new object[Math.Abs(editRange.Item4 - editRange.Item2) + 1];
                var _startKey = editRange.Item2 > editRange.Item4 ? editRange.Item4 : editRange.Item2;
                for (int i = 0; i < loopKeys.Length; i++)
                {
                    loopKeys[i] = _startKey + i;
                }
            }
            else
            {
                loopKeys = new object[Math.Abs(endRow - startRow) + 1];
                var _startKey = startRow > endRow ? endRow : startRow;
                for (int i = 0; i < loopKeys.Length; i++)
                {
                    loopKeys[i] = _startKey + i;
                }
            }
            ContextInstance.ActionContext.AddResult("Loop_Keys", JsonConvert.SerializeObject(loopKeys));
            ContextInstance.ActionContext.AddResult(ExcelUtils.TEMP_FILEPATH);
        }
        void IExcel.LoopV2(string workbook, string sheet, string range, int valueType)
        {
            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
            Instance.Loop(workbook, sheet, rangeInfo.startRow, rangeInfo.lastRow, rangeInfo.startColumn, rangeInfo.lastColumn, rangeInfo.option.Value, valueType);
        }


        /// <summary>
        /// 导出指定行列的图片
        /// </summary>     
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">工作表</param>
        /// <param name="filePath">保存的文件路径</param>
        /// <param name="range">导出范围 列名 或行号（'A' 或 1）</param>
        /// <exception cref="ExcelException"></exception>
        void IExcel.GetShapes(string workbook, string sheet, string filePath, string range)
        {
            throw new ExcelException("可视化暂不支持该操作，请用非可视化打开文件".I18n("SheetInfo_5E313AC3"));
        }


        /// <summary>
        /// 动态调用
        /// </summary>
        /// <param name="obj">对象类型</param>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">Sheet</param>
        /// <param name="range">区域</param>
        /// <param name="property">属性</param>
        /// <param name="value">值</param>
        void IExcel.Interop(int obj, string workbook, string sheet, string range, string property, dynamic value)
        {
            try
            {
                string[] propertyNames = null;
                object interopValue = value;
                System.Action<object> Invoke = new System.Action<object>((o) =>
                {
                    if (interopValue is Array)
                    {
                        //方法调用
                        object[] args = ((Array)interopValue).Cast<object>().ToArray();
                        var _result = InvokeExcelMethod(o, propertyNames[0], args);
                        ContextInstance.ActionContext.AddResult(new JProperty("result", _result));
                    }
                    else if (interopValue == null || (interopValue is string && string.IsNullOrWhiteSpace(Convert.ToString(interopValue))))
                    {
                        //获取属性值
                        object _value = GetExcelPropertyRecursion(o, propertyNames, 0);
                        ContextInstance.ActionContext.AddResult(new JProperty("result", _value));
                    }
                    else
                    {
                        //设置属性
                        SetExcelPropertyRecursion(o, propertyNames, 1, interopValue);
                    }

                });
                if (string.IsNullOrWhiteSpace(property))
                {
                    throw new ExcelException("调用的属性或方法名无效".I18n("SheetInfo_2A68974"));
                }
                propertyNames = property.Split(new char[1] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                ContextInstance.ActionContext.Result.RemoveAll();

                using (var com = new ComScope())
                {
                    if (obj == 0)
                    {
                        //Excel Application
                        var app = instance.Value?.RootApplication;
                        if (app != null)
                        {
                            Invoke(app);
                        }
                        else
                        {
                            if (interopValue == null || interopValue is string && (string.IsNullOrEmpty(Convert.ToString(interopValue)) || Convert.ToString(interopValue).StartsWith("[")))
                            {
                                throw new ExcelException("没有运行的Excel对象，无法调用".I18n("SheetInfo_CDD9234"));
                            }
                            else
                            {
                                //当前Excel没有运行时，设置的属性延迟到第一次创建对象
                                instance.Value.InteropArgs = new Tuple<string, object>(property, interopValue);
                                ContextInstance.ActionContext.AddResult(new JProperty("result", ""));
                            }
                        }

                    }
                    else if (obj == 1)
                    {
                        //Workbook
                        if (Workbooks.TryGetValue(workbook, out object wb))
                        {
                            Invoke(wb);
                            // wb is cached, shouldn't be released completely but since it's dictionary, it's fine for ComScope not to release it if not tracked, or just leave it untouched.
                        }
                        else
                        {
                            throw new ExcelException(Localize($"{workbook} 没有打开", "SheetInfo_9610B4E2"));

                        }

                    }
                    else if (obj == 2)
                    {
                        //Worksheet
                        object _worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                        Invoke(_worksheet);
                    }
                    else
                    {
                        //Range
                        object _worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                        object _range = com.Track(ComLateBinding.GetProp(_worksheet, "Range", range));
                        Invoke(_range);
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new Exception(Localize($"动态调用执行失败", "SheetInfo_726F9FA0"));
            }

        }
    }
}

