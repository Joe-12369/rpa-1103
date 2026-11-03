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
    /// <summary>
    /// 不使用软件自动化
    /// 设计器.表格编辑类组件
    /// </summary>
    public partial class NonSoftware : IExcel
    {

        /// <summary>
        /// 写入单元格
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="row">行号</param>  
        /// <param name="column">列名</param>  
        /// <param name="content">写入内容</param>  
        void IExcel.WriteCell(string workbook, string sheet, int row, string column, string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                var cell = GetWorksheetByName(workbook, sheet)[$"{column}{row}"];
                cell.SetValue(content);
            }
        }

        /// <summary>
        /// 写入行
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">起始列名</param>  
        /// <param name="row">起始行号</param>  
        /// <param name="contents">写入内容</param>  
        void IExcel.WriteRow(string workbook, string sheet, string column, int row, string contents)
        {
            if (!ExcelUtils.StreamingEnabled && !ExcelUtils.DevExpressBatchEnabled)
            {
                LegacyWriteRow(workbook, sheet, column, row, contents);
                return;
            }
            WriteRow(GetWorksheetByName(workbook, sheet), column, row, contents);
        }
        void WriteRow(Worksheet sheet, string column, int row, string contents)
        {
            int firstColumn = ExcelUtils.ColumnLetterToIndex(column) - 1;
            BeginUpdate(sheet.Workbook);
            try
            {
                ExcelUtils.WriteChunks(contents, true, chunk =>
                {
                    var values = ConvertChunkToWriteStrings(chunk);
                    ImportWithFallback(sheet, values, row - 1,
                        firstColumn + chunk.RowOffset * chunk.ColumnCount, false);
                });
            }
            finally { EndUpdate(sheet.Workbook); }
        }

        /// <summary>
        /// 删除行
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="mode">删除方式["单独一行","连续多行","分散多行"]</param>  
        /// <param name="row">删除行号</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="rows">删除行号</param>  
        void IExcel.DeleteRow(string workbook, string sheet, string mode, int row, int startRow, int lastRow, string rows)
        {
            var _sheet = GetWorksheetByName(workbook, sheet);
            switch (mode)
            {
                case "单独一行": _sheet.Rows[row - 1].Delete(); break;
                case "连续多行":
                    _sheet[$"{startRow}:{lastRow}"].Delete(); break;
                case "分散多行":
                    _sheet.Range[ExcelUtils.ParseRowRangeText(rows)].Delete();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 写入列
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">起始列名</param>  
        /// <param name="row">起始行号</param>  
        /// <param name="contents">写入内容</param>  
        void IExcel.WriteColumn(string workbook, string sheet, string column, int row, string contents)
        {
            if (!ExcelUtils.StreamingEnabled && !ExcelUtils.DevExpressBatchEnabled)
            {
                LegacyWriteColumn(workbook, sheet, column, row, contents);
                return;
            }
            WriteColumn(GetWorksheetByName(workbook, sheet), column, row, contents);
        }
        void WriteColumn(Worksheet sheet, string column, int row, string contents)
        {
            int firstColumn = ExcelUtils.ColumnLetterToIndex(column) - 1;
            BeginUpdate(sheet.Workbook);
            try
            {
                ExcelUtils.WriteChunks(contents, true, chunk =>
                {
                    var values = ConvertChunkToWriteStrings(chunk);
                    ImportWithFallback(sheet, values,
                        row - 1 + chunk.RowOffset * chunk.ColumnCount, firstColumn, true);
                });
            }
            finally { EndUpdate(sheet.Workbook); }
        }

        /// <summary>
        /// 删除列
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="mode">删除方式["单独一列","连续多列","分散多列"]</param>  
        /// <param name="column">删除列名</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="lastColumn">结束列名</param>  
        /// <param name="columns">删除列名</param>  
        void IExcel.DeleteColumn(string workbook, string sheet, string mode, string column, string startColumn, string lastColumn, string columns)
        {
            var _sheet = GetWorksheetByName(workbook, sheet);
            switch (mode)
            {
                case "单独一列": _sheet.Range[$"{column}:{column}"].Delete(); break;
                case "连续多列":
                    _sheet.Range[$"{startColumn}:{lastColumn}"].Delete(); break;
                case "分散多列":
                    _sheet.Range[ExcelUtils.ParseColumnRangeText(columns)].Delete();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 插入行
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="index">在第几行前</param>  
        /// <param name="count">插入几行</param>  
        /// <param name="contents">写入内容</param>  
        /// <param name="directon">插入方向 0 向上 1向下</param>          
        void IExcel.InsertRow(string workbook, string sheet, int index, int count, string contents, int direction)
        {
            if (!ExcelUtils.StreamingEnabled && !ExcelUtils.DevExpressBatchEnabled)
            {
                LegacyInsertRow(workbook, sheet, index, count, contents, direction);
                return;
            }
            var worksheet = GetWorksheetByName(workbook, sheet);
            BeginUpdate(worksheet.Workbook);
            try
            {
                worksheet.Rows.Insert(index + direction - 1, count);
                if (!string.IsNullOrEmpty(contents) && (File.Exists(contents) || Directory.Exists(contents)))
                {
                    if (count == 1) WriteRow(worksheet, "A", index + direction, contents);
                    else WriteRange(worksheet, index + direction, "A", contents);
                }
            }
            finally { EndUpdate(worksheet.Workbook); }
        }


        /// <summary>
        /// 插入列
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="index">在第几列前</param>  
        /// <param name="count">插入几列</param>  
        /// <param name="contents">写入内容</param>  
        /// <param name="direction">插入方向：0 向左 1向右</param>  
        void IExcel.InsertColumn(string workbook, string sheet, int index, int count, string contents, int direction)
        {
            if (!ExcelUtils.StreamingEnabled && !ExcelUtils.DevExpressBatchEnabled)
            {
                LegacyInsertColumn(workbook, sheet, index, count, contents, direction);
                return;
            }
            var worksheet = GetWorksheetByName(workbook, sheet);
            BeginUpdate(worksheet.Workbook);
            try
            {
                worksheet.Columns.Insert(index + direction - 1, count);
                if (!string.IsNullOrEmpty(contents) && (File.Exists(contents) || Directory.Exists(contents)))
                {
                    if (count == 1) WriteColumn(worksheet, ExcelUtils.ColumnIndexToLetter(index + direction), 1, contents);
                    else ExcelUtils.WriteChunks(contents, false, chunk =>
                    {
                        if (chunk.RowOffset + chunk.RowCount > count)
                            throw new ExcelException("插入列的数据数量超过插入列数".I18n("SheetEdit_CD425F76"));
                        // Inputs are column-major; transpose only this bounded chunk.
                        var values = new object[chunk.ColumnCount, chunk.RowCount];
                        for (int r = 0; r < chunk.RowCount; r++)
                            for (int c = 0; c < chunk.ColumnCount; c++)
                                values[c, r] = Convert.ToString(chunk.GetCell(r, c));
                        ImportWithFallback(worksheet, values, 0, index + direction - 1 + chunk.RowOffset);
                    });
                }
            }
            finally { EndUpdate(worksheet.Workbook); }
        }

        /// <summary>
        /// 隐藏/显示行列
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="mode">隐藏/显示["隐藏","显示"]</param>  
        /// <param name="range">隐藏区域["行","列"]</param>  
        /// <param name="rows">行号</param>  
        /// <param name="columns">列名</param>  

        void IExcel.Hide(string workbook, string sheet, string mode, string range, string rows, string columns)
        {
            var _sheet = GetWorksheetByName(workbook, sheet);
            var hidden = mode.Equals("隐藏") ? false : true;
            if (range.Equals("行"))
            {
                var rowArr = ExcelUtils.ConvertStrToArray(rows);
                for (int i = 0; i < rowArr.Length; i++)
                {
                    _sheet.Rows[int.Parse(rowArr[i].ToString()) - 1].Visible = hidden;
                }
            }
            else
            {
                var columnArr = ExcelUtils.ConvertStrToArray(columns);
                for (int i = 0; i < columnArr.Length; i++)
                {
                    var _column = ExcelUtils.ConvertNegativeColumn(columnArr[i].ToString());
                    _sheet.Columns[ExcelUtils.ColumnLetterToIndex(_column) - 1].Visible = hidden;
                }
            }
        }

        /// <summary>
        /// 插入图片
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="path">图片路径</param>  
        /// <param name="row">插入单元格行号</param>  
        /// <param name="column">插入单元格列名</param>  
        /// <param name="left">向右移动距离</param>  
        /// <param name="top">向下移动距离</param>  
        /// <param name="width">图片宽度</param>  
        /// <param name="height">图片高度</param>  
        void IExcel.InsertPicture(string workbook, string sheet, string path, int row, string column, float left, float top, int width, int height)
        {
            var ws = GetWorksheetByName(workbook, sheet);
            string file = null;
            if (path.StartsWith("http") || path.StartsWith("ftp") || path.StartsWith("\\\\"))
            {
                var tmp_file = ExcelUtils.CreateTempFolder();
                var savePath = tmp_file + ".png";
                ExcelUtils.DownloadImageAsync(path, savePath);
                file = savePath;
            }
            else
            {
                file = ExcelUtils.GetAbsolutePath(null, path);
            }
            var picture = ws.Pictures.AddPicture(file, ws.Cells[$"{column}{row}"]);
            ws.Workbook.Unit = DevExpress.Office.DocumentUnit.Point;
            picture.Width = width;
            picture.Height = height;
        }

        /// <summary>
        /// 删除图片
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="name">图片名称</param>  

        void IExcel.DeletePicture(string workbook, string sheet, string name)
        {
            var _sheet = GetWorksheetByName(workbook, sheet);
            var deleteAll = Instance.ActionContext["__deleteAllPicture"] == 1;
            if (deleteAll)
            {
                var pictures = _sheet.Pictures;
                for (int i = 0; i < pictures.Count; i++)
                {
                    pictures[i].Delete();
                }
            }
            else
            {
                Picture picture = _sheet.Pictures.GetPicturesByName(name.Replace("图片", "Picture"))?[0];
                if (picture == null) throw new ExcelException(Localize($"删除失败，名称为 {name} 的图片未找到", "SheetEdit_57CA76AC"));
                else picture.Delete();
            }

        }

        /// <summary>
        /// 写入区域
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="row">起始行号</param>  
        /// <param name="column">起始列名</param>  
        /// <param name="contents">写入内容</param>  

        void IExcel.WriteRange(string workbook, string sheet, int row, string column, string contents)
        {
            if (!ExcelUtils.StreamingEnabled && !ExcelUtils.DevExpressBatchEnabled)
            {
                LegacyWriteRange(workbook, sheet, row, column, contents);
                return;
            }
            WriteRange(GetWorksheetByName(workbook, sheet), row, column, contents);
        }

        private void WriteRange(Worksheet worksheet, int row, string column, string contents)
        {
            int firstColumn = ExcelUtils.ColumnLetterToIndex(column) - 1;
            BeginUpdate(worksheet.Workbook);
            try
            {
                object[,] buffer = null;
                ExcelUtils.WriteChunks(contents, false, chunk =>
                {
                    var values = chunk.ToMatrix(ref buffer);
                    for (int r = 0; r < chunk.RowCount; r++)
                        for (int c = 0; c < chunk.ColumnCount; c++)
                            if (values[r, c] == null) values[r, c] = "";
                    ImportWithFallback(worksheet, values, row - 1 + chunk.RowOffset, firstColumn);
                });
            }
            finally { EndUpdate(worksheet.Workbook); }
        }

        // 更新锁由各入口管理；降级只重写目标数据，不重复插入、读取文件或获取锁。
        private void ImportWithFallback(Worksheet sheet, object[,] values, int rowIndex, int columnIndex)
        {
            string shape = $"{values.GetLength(0)}x{values.GetLength(1)}";
            if (!ExcelUtils.DevExpressBatchEnabled)
            {
                using (ExcelUtils.Performance?.Measure("cellWrite", values.Length, shape))
                    WriteMatrixValues(sheet, values, rowIndex, columnIndex);
                return;
            }
            try
            {
                using (ExcelUtils.Performance?.Measure("import", values.Length, shape))
                    ImportValues(sheet, values, rowIndex, columnIndex);
                return;
            }
            catch (Exception ex) when (CanFallbackFromImport(ex))
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "批量导入失败，降级为逐单元格写入。Sheet={Sheet}, Row={Row}, Column={Column}",
                    sheet.Name, rowIndex + 1, columnIndex + 1);
            }

            // Import 可能已写入部分数据，从起点覆盖整个目标数据，恢复一致的写入结果。
            using (ExcelUtils.Performance?.Measure("importFallback", values.Length, shape))
                WriteMatrixValues(sheet, values, rowIndex, columnIndex);
        }

        private static void WriteMatrixValues(Worksheet sheet, object[,] values, int rowIndex, int columnIndex)
        {
            for (int i = 0; i < values.GetLength(0); i++)
            {
                for (int j = 0; j < values.GetLength(1); j++)
                {
                    sheet[rowIndex + i, columnIndex + j].Value = (dynamic)values[i, j];
                }
            }
        }

        private void ImportWithFallback(Worksheet sheet, string[] values, int rowIndex, int columnIndex, bool vertical)
        {
            string shape = vertical ? $"{values.Length}x1" : $"1x{values.Length}";
            if (!ExcelUtils.DevExpressBatchEnabled)
            {
                using (ExcelUtils.Performance?.Measure("cellWrite", values.Length, shape))
                    for (int i = 0; i < values.Length; i++)
                        sheet[rowIndex + (vertical ? i : 0), columnIndex + (vertical ? 0 : i)].Value = values[i];
                return;
            }
            try
            {
                using (ExcelUtils.Performance?.Measure("import", values.Length, shape))
                    ImportValues(sheet, values, rowIndex, columnIndex, vertical);
                return;
            }
            catch (Exception ex) when (CanFallbackFromImport(ex))
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "批量导入失败，降级为逐单元格写入。Sheet={Sheet}, Row={Row}, Column={Column}",
                    sheet.Name, rowIndex + 1, columnIndex + 1);
            }

            using (ExcelUtils.Performance?.Measure("importFallback", values.Length, shape))
                for (int i = 0; i < values.Length; i++)
                    sheet[rowIndex + (vertical ? i : 0), columnIndex + (vertical ? 0 : i)].Value = values[i];
        }

        private static void BeginUpdate(IWorkbook workbook)
        {
            if (!ExcelUtils.DevExpressBatchEnabled) return;
            using (ExcelUtils.Performance?.Measure("beginUpdate", shape: "workbook"))
                workbook.BeginUpdate();
        }

        private static void EndUpdate(IWorkbook workbook)
        {
            if (!ExcelUtils.DevExpressBatchEnabled) return;
            using (ExcelUtils.Performance?.Measure("endUpdate", shape: "workbook"))
                workbook.EndUpdate();
        }

        private static bool CanFallbackFromImport(Exception ex)
        {
            return !(ex is OutOfMemoryException || ex is OperationCanceledException ||
                ex is System.Threading.ThreadAbortException || ex is AccessViolationException);
        }

        // 隔离 DevExpress 导入调用，便于验证版本兼容失败和部分导入失败后的降级。
        protected virtual void ImportValues(Worksheet sheet, object[,] values, int rowIndex, int columnIndex)
        {
            // Import 会把 DateTime 单元格已有的自定义数字格式改为默认日期格式。
            // 逐单元格赋值保留这些格式，因此导入成功/失败都恢复已有非 General 格式。
            var dateFormats = new List<Tuple<Cell, string>>();
            for (int r = 0; r < values.GetLength(0); r++)
            {
                for (int c = 0; c < values.GetLength(1); c++)
                {
                    if (!(values[r, c] is DateTime)) continue;
                    // 1900 年以前的日期在 Import 与 Value 赋值中的格式副作用不同。
                    // 导入前识别，整个区域沿用旧赋值语义，避免先导入污染 General 格式。
                    if (((DateTime)values[r, c]).Year < 1900)
                    {
                        WriteMatrixValues(sheet, values, rowIndex, columnIndex);
                        return;
                    }
                    var cell = sheet[rowIndex + r, columnIndex + c];
                    if (!string.IsNullOrEmpty(cell.NumberFormat) && cell.NumberFormat != "General")
                        dateFormats.Add(Tuple.Create(cell, cell.NumberFormat));
                }
            }
            try
            {
                sheet.Import(values, rowIndex, columnIndex);
            }
            finally
            {
                foreach (var dateFormat in dateFormats) dateFormat.Item1.NumberFormat = dateFormat.Item2;
            }
        }

        protected virtual void ImportValues(Worksheet sheet, string[] values, int rowIndex, int columnIndex, bool vertical)
        {
            sheet.Import(values, rowIndex, columnIndex, vertical);
        }

        private static string[] ConvertChunkToWriteStrings(CellChunk chunk)
        {
            var values = new string[chunk.CellCount];
            for (int i = 0; i < values.Length; i++)
                values[i] = Convert.ToString(chunk.GetCell(i / chunk.ColumnCount, i % chunk.ColumnCount));
            return values;
        }

        /// <summary>
        /// 选中区域
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="lastColumn">结束列名</param>  
        void IExcel.SelectRange(string workbook, string sheet, int startRow, string startColumn, int lastRow, string lastColumn, int type, string range)
        {
            var ws = GetWorksheetByName(workbook, sheet);
            var rangeObj = GetRange(workbook, sheet, startRow, startColumn, lastRow, lastColumn, type, range);
            ws.Selection = rangeObj;
        }
        void IExcel.SelectRangeV2(string workbook, string sheet, string range)
        {
            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
            Instance.SelectRange(workbook, sheet, rangeInfo.startRow, rangeInfo.startColumn, rangeInfo.lastRow, rangeInfo.lastColumn, rangeInfo.type, rangeInfo.range);
        }

        /// <summary>
        /// 清除区域
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="lastColumn">结束列名</param>  
        /// <param name="mode">是否清除格式["是","否"]</param>  
        void IExcel.ClearRange(string workbook, string sheet, int startRow, string startColumn, int lastRow, string lastColumn, string mode, int type, string range)
        {
            var rangeObj = GetRange(workbook, sheet, startRow, startColumn, lastRow, lastColumn, type, range);
            rangeObj.ClearContents();
            if (mode.Equals("是")) rangeObj.ClearFormats();
        }
        void IExcel.ClearRangeV2(string workbook, string sheet, string range, string mode)
        {
            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
            Instance.ClearRange(workbook, sheet, rangeInfo.startRow, rangeInfo.startColumn, rangeInfo.lastRow, rangeInfo.lastColumn, mode, rangeInfo.type, rangeInfo.range);
        }
        void IExcel.ClearRangeV3(string workbook, string sheet, string range, int clearType)
        {
            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
            var rangeObj = GetRange(workbook, sheet, rangeInfo.startRow, rangeInfo.startColumn, rangeInfo.lastRow, rangeInfo.lastColumn, rangeInfo.type, rangeInfo.range);
            switch (clearType)
            {
                case 0:
                    rangeObj.Clear();
                    break;
                case 1:
                    rangeObj.ClearFormats();
                    break;
                case 2:
                    rangeObj.ClearContents();
                    break;
                case 3:
                    rangeObj.FillColor = Color.Empty;
                    break;
                case 4:
                    rangeObj.ClearComments();
                    break;
                case 5:
                    rangeObj.ClearHyperlinks();
                    break;
                case 6:
                    rangeObj.ClearHyperlinks();
                    rangeObj.ClearFormats();
                    break;
            }
        }

        /// <summary>
        /// 删除区域
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="lastColumn">结束列名</param>  
        /// <param name="mode">删除模式["右侧单元格左移","下方单元格上移","整行","整列"]</param>  
        void IExcel.DeleteRange(string workbook, string sheet, int startRow, string startColumn, int lastRow, string lastColumn, string mode, int type, string range)
        {
            var rangeObj = GetRange(workbook, sheet, startRow, startColumn, lastRow, lastColumn, type, range);
            switch (mode)
            {
                case "右侧单元格左移": rangeObj.Delete(DeleteMode.ShiftCellsLeft); break;
                case "下方单元格上移": rangeObj.Delete(DeleteMode.ShiftCellsUp); break;
                case "整行": rangeObj.Delete(DeleteMode.EntireRow); break;
                case "整列": rangeObj.Delete(DeleteMode.EntireColumn); break;
                default:
                    break;
            }
        }

        void IExcel.DeleteRangeV2(string workbook, string sheet, string range, string mode)
        {
            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
            Instance.DeleteRange(workbook, sheet, rangeInfo.startRow, rangeInfo.startColumn, rangeInfo.lastRow, rangeInfo.lastColumn, mode, rangeInfo.type, rangeInfo.range);
        }

        /// <summary>
        /// 区域背景颜色
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="lastColumn">结束列名</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="color">颜色</param>  
        void IExcel.SetRangeBackground(string workbook, string sheet, string startColumn, int startRow, string lastColumn, int lastRow, string color, int type, string range)
        {
            var rangeObj = GetRange(workbook, sheet, startRow, startColumn, lastRow, lastColumn, type, range);
            DevExpress.Spreadsheet.Formatting rangeFormatting = rangeObj.BeginUpdateFormatting();
            rangeFormatting.Fill.BackgroundColor = ExcelUtils.GetColor(color);
            rangeObj.EndUpdateFormatting(rangeFormatting);
        }

        void IExcel.SetRangeBackgroundV2(string workbook, string sheet, string range, string color)
        {
            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
            Instance.SetRangeBackground(workbook, sheet, rangeInfo.startColumn, rangeInfo.startRow, rangeInfo.lastColumn, rangeInfo.lastRow, color, rangeInfo.type, rangeInfo.range);
        }
        /// <summary>
        /// 区域字体颜色
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="lastColumn">结束列名</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="color">颜色</param>  
        void IExcel.SetRangeFont(string workbook, string sheet, string startColumn, int startRow, string lastColumn, int lastRow, string color, int type, string range)
        {
            var rangeObj = GetRange(workbook, sheet, startRow, startColumn, lastRow, lastColumn, type, range);
            DevExpress.Spreadsheet.Formatting rangeFormatting = rangeObj.BeginUpdateFormatting();
            rangeFormatting.Font.Color = ExcelUtils.GetColor(color);
            rangeObj.EndUpdateFormatting(rangeFormatting);
        }

        void IExcel.SetRangeFontV2(string workbook, string sheet, string range, string color)
        {
            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
            Instance.SetRangeFont(workbook, sheet, rangeInfo.startColumn, rangeInfo.startRow, rangeInfo.lastColumn, rangeInfo.lastRow, color, rangeInfo.type, rangeInfo.range);
        }

        /// <summary>
        /// 复制表格内容
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="mode">复制模式["复制单元格","复制行","复制列","复制区域","复制已编辑区域"]</param>  
        /// <param name="column">单元格列名</param>  
        /// <param name="row">单元格行号</param>  
        /// <param name="startRow">起始行号</param>  
        /// <param name="lastRow">结束行号</param>  
        /// <param name="startColumn">起始列名</param>  
        /// <param name="lastColumn">结束列名</param>  
        /// <param name="startColumn2">起始列名</param>  
        /// <param name="startRow2">起始行号</param>  
        /// <param name="lastColumn2">结束列名</param>  
        /// <param name="lastRow2">结束行号</param>  
        void IExcel.CopyRange(string workbook, string sheet, string mode, string column, int row, int startRow, int lastRow, string startColumn, string lastColumn, string startColumn2, int startRow2, string lastColumn2, int lastRow2)
        {
            var _sheet = GetWorksheetByName(workbook, sheet);
            var range = _sheet.GetUsedRange();
            switch (mode)
            {
                case "复制单元格":
                    range = _sheet[$"{column}{row}"];
                    break;
                case "复制行":
                    range = _sheet[$"A{startRow}:{ExcelUtils.ColumnIndexToLetter(range.RightColumnIndex + 1)}{lastRow}"];
                    break;
                case "复制列":
                    range = _sheet[$"{startColumn}1:{lastColumn}{range.BottomRowIndex + 1}"];
                    break;
                case "复制区域":
                    range = _sheet[$"{startColumn2}{startRow2}:{lastColumn2}{lastRow2}"];
                    break;
                case "复制已编辑区域":
                    break;
                default:
                    throw new ExcelException("无效的复制模式".I18n("SheetEdit_C9871526"));
            }
            CopyRange = range;
            //690sp1 去掉返回复制内容
            if (!Instance.ActionContext["__IsCopyRangeV2"])
            {
                GetRangeValue(range);
                ContextInstance.ActionContext.AddResult(ExcelUtils.TEMP_FILEPATH);
            }
        }
        void IExcel.CopyRangeV2(string workbook, string sheet, string range)
        {
            var rangeAddress = ExcelUtils.ConvertExcelRangeAddress(range);
            var _sheet = GetWorksheetByName(workbook, sheet);
            var _range = _sheet.Range[rangeAddress];
            CopyRange = _range;
            //690sp1 去掉返回复制内容
            if (!Instance.ActionContext["__IsCopyRangeV2"])
            {
                GetRangeValue(_range);
                ContextInstance.ActionContext.AddResult(ExcelUtils.TEMP_FILEPATH);
            }
        }

        /// <summary>
        /// 粘贴表格内容
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">单元格列名</param>  
        /// <param name="row">单元格行号</param>  
        void IExcel.PastRange(string workbook, string sheet, string column, string row)
        {
            if (CopyRange == null)
            {
                throw new ExcelException("请先用【复制表格内容】组件复制一个区域".I18n("SheetEdit_3EA1E158"));
            }
            GetWorksheetByName(workbook, sheet)[$"{column}{row}"].CopyFrom(CopyRange, PasteSpecial.All);
            CopyRange = null;
        }

        /// <summary>
        /// 粘贴表格内容
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">单元格列名</param>  
        /// <param name="row">单元格行号</param>
        /// <param name="pasteType">粘贴类型</param>
        ///<param name="pasteOperation">运算符</param>
        ///<param name="skipBlanks">跳过空白单元格</param>
        ///<param name="transpose">是否转置</param>
        void IExcel.PastRangeV2(string workbook, string sheet, string column, string row, int pasteType, int pasteOperation, bool skipBlanks, bool transpose)
        {
            if (CopyRange == null)
            {
                throw new ExcelException("请先用【复制表格内容】组件复制一个区域".I18n("SheetEdit_3EA1E158"));
            }
            //713 粘贴至可见单元格
            if (ActionContext["visible"] == 1)
            {
                throw new ExcelException("【粘贴值到可见单元格】选项需要依赖 Microsoft Excel 或 WPS 软件".I18n("SheetEdit_4764F84E"));
            }
            else
            {
                GetWorksheetByName(workbook, sheet)[$"{column}{row}"].CopyFrom(CopyRange, GetPasteSpecial(pasteType));
            }
            CopyRange = null;
        }

        /// <summary>
        /// 删除重复
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="columns">待删除列名</param>  
        /// <param name="header">数据包含标题["是","否"]</param>  
        /// <param name="numRet">删除数量输出至</param>  

        void IExcel.DeleteDuplicates(string workbook, string sheet, string columns, string header)
        {
            var _sheet = GetWorksheetByName(workbook, sheet);
            var _usedRange = _sheet.GetUsedRange();
            var arrColumn = ExcelUtils.ConvertColumnsIndexArray(columns);
            var delRows = new List<int>();
            int flag = header.Equals("是") ? 1 : 0;
            for (int i = 0; i < _usedRange.RowCount; i++)
            {
                if (delRows.Contains(i)) continue;
                for (int j = i + 1; j < _usedRange.RowCount; j++)
                {
                    var curIVal = GetColumnValue(_sheet, i, arrColumn, flag);
                    var curJVal = GetColumnValue(_sheet, j, arrColumn, flag);
                    if (!string.IsNullOrEmpty(curIVal) && curIVal == curJVal)
                    {
                        delRows.Add(j);
                    }
                }
            }
            if (delRows.Count > 0)
            {
                _sheet.Range[ExcelUtils.ParseRowRangeText(JsonConvert.SerializeObject(delRows))].Delete();
            }
            ContextInstance.ActionContext.AddResult("numRet", delRows.Count.ToString());
        }

        /// <summary>
        /// 设置列宽
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="columns">列名</param>  
        /// <param name="width">列宽</param>  
        /// <param name="isAutoFit">自适应列宽</param>  
        void IExcel.SetWidth(string workbook, string sheet, string columns, float width, bool isAutoFit)
        {
            if (string.IsNullOrWhiteSpace(columns) || columns.Contains("None"))
            {
                if (isAutoFit)
                {
                    var _sheet = GetWorksheetByName(workbook, sheet);
                    var count = _sheet.GetDataRange().ColumnCount;
                    _sheet.Columns.AutoFit(0, count - 1);
                }
                else
                {
                    GetWorksheetByName(workbook, sheet).DefaultColumnWidthInCharacters = width;
                }

            }
            else
            {
                if (isAutoFit)
                {
                    GetWorksheetByName(workbook, sheet)[ExcelUtils.ParseColumnRangeText(columns)].AutoFitColumns();
                }
                else
                {
                    GetWorksheetByName(workbook, sheet)[ExcelUtils.ParseColumnRangeText(columns)].ColumnWidthInCharacters = width;
                }
            }
        }

        /// <summary>
        /// 设置行高
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="rows">行号</param>  
        /// <param name="height">行高</param>  
        /// <param name="isAutoFit">自适应列宽</param>  
        void IExcel.SetHeight(string workbook, string sheet, string rows, float height, bool isAutoFit)
        {
            var _sheet = GetWorksheetByName(workbook, sheet);
            _sheet.Workbook.Unit = DevExpress.Office.DocumentUnit.Point;
            if (string.IsNullOrWhiteSpace(rows) || rows.Contains("None"))
            {
                if (isAutoFit)
                {
                    var count = _sheet.GetDataRange().RowCount;
                    GetWorksheetByName(workbook, sheet).Rows.AutoFit(0, count - 1);
                }
                else
                {
                    GetWorksheetByName(workbook, sheet).DefaultRowHeight = height;
                }
            }
            else
            {
                if (isAutoFit)
                {
                    GetWorksheetByName(workbook, sheet)[ExcelUtils.ParseRowRangeText(rows)].AutoFitRows();
                }
                else
                {
                    GetWorksheetByName(workbook, sheet)[ExcelUtils.ParseRowRangeText(rows)].RowHeight = height;
                }
            }
        }

        /// <summary>
        /// 单元格背景颜色
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>  
        /// <param name="color">颜色</param>  

        void IExcel.SetCellBackground(string workbook, string sheet, string column, int row, string color)
        {
            var range = GetWorksheetByName(workbook, sheet)[$"{column}{row}"];
            DevExpress.Spreadsheet.Formatting rangeFormatting = range.BeginUpdateFormatting();
            rangeFormatting.Fill.BackgroundColor = ExcelUtils.GetColor(color);
            range.EndUpdateFormatting(rangeFormatting);
        }

        /// <summary>
        /// 单元格字体颜色
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>  
        /// <param name="color">颜色</param>  

        void IExcel.SetCellFont(string workbook, string sheet, string column, int row, string color)
        {
            var range = GetWorksheetByName(workbook, sheet)[$"{column}{row}"];
            DevExpress.Spreadsheet.Formatting rangeFormatting = range.BeginUpdateFormatting();
            rangeFormatting.Font.Color = ExcelUtils.GetColor(color);
            range.EndUpdateFormatting(rangeFormatting);
        }

        /// <summary>
        /// 设置单元格格式
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>          
        /// <param name="range">区域</param>  
        /// <param name="format">格式</param>         
        void IExcel.SetCellFormat(string workbook, string sheet, string range, string format)
        {
            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
            var rangeObj = GetRange(workbook, sheet, rangeInfo.startRow, rangeInfo.startColumn, rangeInfo.lastRow, rangeInfo.lastColumn, rangeInfo.type, rangeInfo.range);
            DevExpress.Spreadsheet.Formatting rangeFormatting = rangeObj.BeginUpdateFormatting();
            rangeFormatting.NumberFormatLocal = format;
            rangeObj.EndUpdateFormatting(rangeFormatting);

        }

        /// <summary>
        /// 设置单元格各种格式
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet 名称</param>
        /// <param name="range">区域范围</param>
        /// <param name="format">格式</param>
        void IExcel.SetCellFormatEx(string workbook, string sheet, string range, string formatJson)
        {
            CellFormatParams cellFormatParams = JsonConvert.DeserializeObject<CellFormatParams>(formatJson);

            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
            var rangeObj = GetRange(workbook, sheet, rangeInfo.startRow, rangeInfo.startColumn, rangeInfo.lastRow, rangeInfo.lastColumn, rangeInfo.type, rangeInfo.range);

            DevExpress.Spreadsheet.Formatting rangeFormatting = rangeObj.BeginUpdateFormatting();

            if (cellFormatParams.FontFormat != "默认")
            {
                rangeFormatting.NumberFormatLocal = cellFormatParams.FontFormat;//字体
            }
            if (cellFormatParams.FontSize.HasValue)
            {
                rangeFormatting.Font.Size = cellFormatParams.FontSize.Value;//字号
            }

            rangeFormatting.Font.FontStyle = BotAndItalicParam.TurnEnumBoldAndItalicToFontStyle(cellFormatParams.BoldAndItalic);//粗体斜体

            if (!string.IsNullOrWhiteSpace(cellFormatParams.FontColor))
            {
                rangeFormatting.Font.Color = ExcelUtils.GetColor(cellFormatParams.FontColor);//字体颜色
            }

            if (!string.IsNullOrEmpty(cellFormatParams.BackgroundColor) && cellFormatParams.BackgroundColor != "null")
            {
                var colorValue = cellFormatParams.BackgroundColor.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (colorValue.Length > 1)
                {
                    var color = ExcelUtils.GetColor(colorValue[0]);//单元格背景色
                    int.TryParse(colorValue[1].Replace("%", ""), out int A);
                    if (A == 0)
                    {
                        rangeFormatting.Fill.BackgroundColor = Color.Transparent;
                    }
                    else
                    {
                        int alpha = (A / 100) * 255;
                        rangeFormatting.Fill.BackgroundColor = Color.FromArgb(alpha, color.R, color.G, color.B);
                    }
                }
                else
                {
                    rangeFormatting.Fill.BackgroundColor = ExcelUtils.GetColor(cellFormatParams.BackgroundColor);//单元格背景色
                }
            }
            else
            {
                rangeFormatting.Fill.BackgroundColor = Color.Transparent;
            }
            rangeFormatting.Alignment.Horizontal = cellFormatParams.GetSpreadsheetHorAlignment(cellFormatParams.HorizontalAlignment); //水平对齐
            rangeFormatting.Alignment.Vertical = cellFormatParams.GetSpreadsheetVerticalAlignment(cellFormatParams.VerticalAlignment); //垂直对齐

            rangeObj.NumberFormat = string.IsNullOrEmpty(cellFormatParams.DigitalFormat.formatstring) ? cellFormatParams.DigitalFormat.GetFormatDes() : cellFormatParams.DigitalFormat.formatstring;

            switch (cellFormatParams.TextControl)
            {
                case 0:
                    rangeObj.Alignment.WrapText = false;
                    rangeObj.Alignment.ShrinkToFit = false;
                    break;
                case 1:
                    rangeObj.Alignment.WrapText = true;
                    rangeObj.Alignment.ShrinkToFit = false;
                    break;
                case 2:
                    rangeObj.Alignment.WrapText = false;
                    rangeObj.Alignment.ShrinkToFit = true;
                    break;
                default:
                    break;
            }
            if (cellFormatParams.Borders != null)
            {
                foreach (var border in cellFormatParams.Borders)
                {
                    if (border == null)
                    {
                        continue;
                    }
                    DevExpress.Spreadsheet.Border borderObj = null;
                    switch (border.Index)
                    {
                        case 7:
                            borderObj = rangeObj.Borders.LeftBorder; break;
                        case 8:
                            borderObj = rangeObj.Borders.TopBorder; break;
                        case 9:
                            borderObj = rangeObj.Borders.BottomBorder; break;
                        case 10:
                            borderObj = rangeObj.Borders.RightBorder; break;
                        case 11:
                            borderObj = rangeObj.Borders.InsideVerticalBorders; break;
                        case 12:
                            borderObj = rangeObj.Borders.InsideHorizontalBorders; break;
                        default:
                            continue;
                    }
                    if (border.Color > 0)
                    {
                        borderObj.Color = ExcelUtils.GetColor(ExcelUtils.ConvertNumberColor((int)border.Color));
                    }
                    //https://learn.microsoft.com/en-us/office/vba/api/excel.xlborderweight
                    //https://learn.microsoft.com/en-us/office/vba/api/excel.xllinestyle
                    if (border.LineStyle != -4142)
                    {
                        if (border.Weight == 1 && border.LineStyle == 1)
                        {
                            borderObj.LineStyle = BorderLineStyle.Hair;
                        }
                        if (border.Weight == 2 && border.LineStyle == -4118)
                        {
                            borderObj.LineStyle = BorderLineStyle.Dotted;
                        }
                        if (border.Weight == 2 && border.LineStyle == 5)
                        {
                            borderObj.LineStyle = BorderLineStyle.DashDotDot;
                        }
                        if (border.Weight == 2 && border.LineStyle == 4)
                        {
                            borderObj.LineStyle = BorderLineStyle.DashDot;
                        }
                        if (border.Weight == 2 && border.LineStyle == -4115)
                        {
                            borderObj.LineStyle = BorderLineStyle.Dashed;
                        }
                        if (border.Weight == 2 && border.LineStyle == 1)
                        {
                            borderObj.LineStyle = BorderLineStyle.Thin;
                        }

                        if (border.Weight == -4138 && border.LineStyle == 5)
                        {
                            borderObj.LineStyle = BorderLineStyle.MediumDashDotDot;
                        }
                        if (border.Weight == -4138 && border.LineStyle == 13)
                        {
                            borderObj.LineStyle = BorderLineStyle.SlantDashDot;
                        }
                        if (border.Weight == -4138 && border.LineStyle == 4)
                        {
                            borderObj.LineStyle = BorderLineStyle.MediumDashDot;
                        }
                        if (border.Weight == -4138 && border.LineStyle == -4115)
                        {
                            borderObj.LineStyle = BorderLineStyle.MediumDashed;
                        }
                        if (border.Weight == -4138 && border.LineStyle == 1)
                        {
                            borderObj.LineStyle = BorderLineStyle.Medium;
                        }
                        if (border.Weight == 4 && border.LineStyle == 1)
                        {
                            borderObj.LineStyle = BorderLineStyle.Thick;
                        }
                        if (border.Weight == 4 && border.LineStyle == -4119)
                        {
                            borderObj.LineStyle = BorderLineStyle.Double;
                        }
                    }
                    else
                    {
                        borderObj.LineStyle = BorderLineStyle.None;
                    }

                }
            }

            rangeObj.EndUpdateFormatting(rangeFormatting);
        }


        /// <summary>
        /// 设置单元格各种格式 从拾取功能或者指定单元格区域 读取格式内容
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet 名称</param>
        /// <param name="range">区域范围</param>
        /// <param name="format">格式</param>
        string IExcel.GetCellFormatEx(string workbook, string sheet, string range)
        {
            CellFormatParams cellFormatParams = new CellFormatParams();

            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
            var rangeObj = GetRange(workbook, sheet, rangeInfo.startRow, rangeInfo.startColumn, rangeInfo.lastRow, rangeInfo.lastColumn, rangeInfo.type, rangeInfo.range);


            cellFormatParams.FontFormat = rangeObj.Font.Name;//字体

            cellFormatParams.FontSize = rangeObj.Font.Size;//字号

            BotAndItalicParam botAndItalicParam = new BotAndItalicParam();
            botAndItalicParam.Bold = rangeObj.Font.Bold;//粗体
            botAndItalicParam.Italic = rangeObj.Font.Italic;//斜体
            cellFormatParams.BoldAndItalic = botAndItalicParam.GetBotAndItalicDescription();

            cellFormatParams.FontColor = ExcelUtils.TurnColorToCode(rangeObj.Font.Color);
            cellFormatParams.BackgroundColor = ExcelUtils.TurnColorToCode(rangeObj.Fill.BackgroundColor);

            cellFormatParams.HorizontalAlignment = cellFormatParams.GetEnumHorizontalAlignmentNonVisual(rangeObj.Alignment.Horizontal);
            cellFormatParams.VerticalAlignment = cellFormatParams.GetEnumVerticalAlignmentNonVisual(rangeObj.Alignment.Vertical);

            cellFormatParams.DigitalFormat = new DigitalFormatClass();
            if (cellFormatParams.DigitalFormat.GetEnumFormat(rangeObj.NumberFormat, out EnumDigitalFormat enumElement))
            {
                cellFormatParams.DigitalFormat.enumDigitalFormat = enumElement;
                cellFormatParams.DigitalFormat.formatstring = rangeObj.NumberFormat;
            }
            else
            {
                cellFormatParams.DigitalFormat.enumDigitalFormat = EnumDigitalFormat.Custom;
                cellFormatParams.DigitalFormat.formatstring = rangeObj.NumberFormat;
            }

            return cellFormatParams.ToJson();
        }


        /// <summary>
        /// Excel分列
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheet"></param>
        /// <param name="column"></param>
        /// <param name="columnSplitParamJson"></param>
        void IExcel.ColumnSplit(string workbook, string sheet, string column, string columnSplitParamJson)
        {
            throw new ExcelException("【Excel分列】组件需要依赖 Microsoft Excel 或 WPS 软件".I18n("SheetEdit_3FA088A2"));
        }

        /// <summary>
        /// Excel分列
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet名称</param>
        /// <param name="column"></param>
        /// <param name="columnSplitParamJson">分列配置</param>
        /// <param name="destination">目标区域</param>
        /// <param name="startRow">开始行号</param>
        /// <param name="endRow">结束行号</param>
        /// <param name="splitDataType">分类数据类型设置（0，常规，1自定义）</param>
        /// <param name="fieldInfo">分列格式</param>
        /// <param name="insertCount">插入列数</param>
        void IExcel.ColumnSplitV2(string workbook, string sheet, string column, string columnSplitParamJson, string destination, int startRow, int endRow, int splitDataType, string fieldInfo, int insertCount)
        {
            throw new ExcelException("【Excel分列】组件需要依赖 Microsoft Excel 或 WPS 软件".I18n("SheetEdit_3FA088A2"));
        }

        /// <summary>
        /// 新建透视表
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="rule">透视表规则</param>  

        void IExcel.InsertPivotTable(string workbook, string rule, string sourceData)
        {
            throw new ExcelException("【新建透视表】组件需要依赖 Microsoft Excel 或 WPS 软件".I18n("SheetEdit_5AA3983D"));
        }

        /// <summary>
        /// 刷新透视表
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet</param>
        /// <param name="pivotTable">透视表</param>
        void IExcel.RefreshPivotTable(string workbook, string sheet, string pivotTable)
        {
            throw new ExcelException("【刷新透视表】组件需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetEdit_D1D2D71A"));
        }

        /// <summary>
        /// 筛选透视表
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet</param>
        /// <param name="pivotTable">透视表</param>
        /// <param name="field">筛选字段</param>
        /// <param name="fieldValue">筛选字段值</param>
        void IExcel.FilterPivotTable(string workbook, string sheet, string pivotTable, string field, string fieldValue)
        {
            throw new ExcelException("【筛选透视表】组件需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetEdit_590361FF"));
        }

        /// <summary>
        /// 新建透视图
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="rule">透视图规则</param>  

        void IExcel.InsertPivotView(string workbook, string rule)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// 数据排序
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet</param>
        /// <param name="sortOrder">排序方式（升序、降序、自定义）</param>
        /// <param name="sortRange">排序列名</param>
        /// <param name="region">是否扩展区域</param>
        /// <param name="header">首行是否参与排序</param>
        /// <param name="rule">自定义规则</param>
        /// <param name="excelRng">选择区域</param>
        void IExcel.Sort(string workbook, string sheet, int sortOrder, string sortRange, int region, int header, string rule)
        {
            throw new ExcelException("【数据排序】组件需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetEdit_EB03CC8B"));
        }

        /// <summary>
        /// 数据筛选
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="rule">筛选规则</param>  

        void IExcel.Filter(string workbook, string sheet, string column, string condition, string criteria, string on_header, string isMultiFilter, string pre_filterData, string pre_lineNum)
        {
            if (isMultiFilter.Equals("true") && (string.IsNullOrEmpty(pre_filterData) || string.IsNullOrEmpty(pre_lineNum)))
            {
                throw new ExcelException(eErrorMessageType.Error_ParamsError.GetMessage());
            }

            var workSheet = GetWorksheetByName(workbook, sheet);
            var range = workSheet.GetUsedRange();
            logger.LogDebug($"StartIndex：{range.TopRowIndex}");
            int _RowCount = 0;
            if (range.TopRowIndex > 0)//TopRowIndex 从零开始
            {
                _RowCount = range.TopRowIndex - 1 + range.RowCount;
            }
            else
            {
                _RowCount = range.RowCount;
            }

            logger.LogDebug($"used row count:{range.RowCount}, total rowcount:{_RowCount}");
            List<int> rowNos = new List<int>();
            int startIndex = on_header == "1" ? 0 : 1;
            var rules = JsonConvert.DeserializeObject<List<Criteria>>(criteria);


            if (isMultiFilter == "false")
            {
                for (int i = startIndex; i < _RowCount; i++)
                {
                    string cellValue = string.Empty;
                    if (workSheet[$"{column}{i + 1}"] != null)
                    {
                        cellValue = workSheet[$"{column}{i + 1}"].Value.ToString();
                    }
                    if (ExcelUtils.ValidationRule(cellValue, condition, rules))
                    {
                        rowNos.Add(i + 1);
                        var obj = new object[1, range.ColumnCount];
                        for (int j = 0; j < range.ColumnCount; j++)
                        {
                            obj[0, j] = workSheet[i, j].Value.GetValue();
                        }
                        ExcelUtils.SerializerDataToBinaryFile(obj, "filterData" + i);
                    }
                }
            }
            else
            {
                try
                {
                    List<JArray> preFilterData = JsonConvert.DeserializeObject<List<JArray>>(pre_filterData);
                    List<int> preLineNum = JsonConvert.DeserializeObject<List<int>>(pre_lineNum);
                    if (preLineNum.Count != preFilterData.Count || preFilterData[0].Count != range.ColumnCount)
                    {
                        ExcelPlugin.logger.LogError("列数量不匹配，无法处理");
                        throw new ExcelException("数据非法或者列数量不匹配，无法处理".I18n("SheetEdit_D54979B3"));
                    }

                    int columnIndex = ExcelUtils.ColumnLetterToIndex(column) - 1;
                    for (int i = 0; i < preFilterData.Count; i++)
                    {
                        dynamic cellValue = preFilterData[i][columnIndex];
                        if (cellValue?.Value != null)
                        {
                            cellValue = cellValue.Value.ToString();
                        }
                        else
                        {
                            cellValue = string.Empty;
                        }

                        if (ExcelUtils.ValidationRule(cellValue, condition, rules))
                        {
                            rowNos.Add(preLineNum[i]);
                            var arrData = new object[3] { 1, preFilterData[i].Count, preFilterData[i].ToObject<dynamic[]>() };
                            ExcelUtils.SerializerDataToBinaryFile(arrData, "filterData" + i);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExcelPlugin.logger.LogError($"数据校验无效:{ex}");
                    throw new ExcelException("数据非法或者列数量不匹配，无法处理".I18n("SheetEdit_D54979B3"));
                }
            }

            ContextInstance.ActionContext.Result.RemoveAll();
            ExcelUtils.SerializerDataToBinaryFile(rowNos.ToArray(), "rowNos");
            ContextInstance.ActionContext.AddResult(ExcelUtils.TEMP_FILEPATH);
        }



        /// <summary>
        /// 执行宏
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="script">宏名称</param>  
        /// <param name="args">宏参数（后加）</param> 
        /// <param name="code">宏代码</param> 
        /// <param name="mode">执行方式</param>  
        void IExcel.RunMacro(string workbook, string script, string args, int mode, string code)
        {

            throw new ExcelException("【执行宏】组件需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetEdit_4B34AEC4"));
        }

        /// <summary>
        /// 自动填充
        /// </summary>
        /// <param name="workbook">工作簿对象</param>
        /// <param name="sheet">工作表</param>
        /// <param name="sourceRange">源区域</param>
        /// <param name="destination">目标区域</param>
        void IExcel.AutoFill(string workbook, string sheet, string sourceRange, string destination)
        {
            throw new ExcelException("【自动填充】需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetEdit_70184F86"));
        }
        void IExcel.AutoFillV2(string workbook, string sheet, string excelRng, string destination)
        {
            throw new ExcelException("【自动填充】需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetEdit_70184F86"));
        }

        /// <summary>
        /// Excel 筛选
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="rule">筛选规则</param>
        /// <param name="filterRange">筛选范围</param>
        void IExcel.ExcelFilter(string workbook, string sheet, string rule, int startRow, int endRow)
        {
            throw new ExcelException("【自动填充】需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetEdit_70184F86"));
        }


        /// <summary>
        /// 切片器设置
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="slicerName">切片器名称</param>
        /// <param name="slicerOptions">切片器选项</param>
        /// <exception cref="System.NotImplementedException"></exception>
        void IExcel.SetSlicer(string workbook, string slicerName, string slicerOptions)
        {
            throw new ExcelException("【Excel切片器】需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetEdit_4DCC7DBB"));
        }

        /// <summary>
        /// 取消Excel筛选状态
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheet"></param>
        void IExcel.EnableExcelFilter(string workbook, string sheet)
        {
            throw new ExcelException("【自动填充】需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetEdit_70184F86"));

        }

        /// <summary>
        /// 清除Excel 筛选条件
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheet"></param>
        /// <param name="option"></param>
        /// <param name="column"></param>
        void IExcel.ClearExcelFilter(string workbook, string sheet, int option, string column)
        {
            throw new ExcelException("【自动填充】需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetEdit_70184F86"));
        }

        /// <summary>
        /// 设置Excel表格状态
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="table">表格名</param>
        /// <param name="headerRow">标题行</param>
        /// <param name="totalRow">汇总行</param>
        /// <param name="bandedRow">镶边行</param>
        /// <param name="firstColumn">第一列</param>
        /// <param name="lastColumn">最后一列</param>
        /// <param name="bandedColumn">镶边列</param>
        /// <param name="filterButton">筛选按钮</param>
        void IExcel.ShowTableStyle(string workbook, string sheet, string table, int headerRow, int totalRow, int firstColumn, int lastColumn, int filterButton)
        {
            throw new ExcelException("【自动填充】需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetEdit_70184F86"));

        }

        /// <summary>
        /// 刷新表格数据
        /// </summary>
        /// <param name="workbook"></param>
        void IExcel.RefreshAll(string workbook)
        {
            throw new ExcelException("【自动填充】需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetEdit_70184F86"));
        }
        /// <summary>
        /// 组合行列
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">Sheet</param>
        /// <param name="group">组合0/取消组合1</param>
        /// <param name="groupType">组合类型：行0/列1</param>
        /// <param name="startRow">开始行</param>
        /// <param name="endRow">结束行</param>
        /// <param name="startColumn">开始列</param>
        /// <param name="endColumn">结束列</param>
        void IExcel.Group(string workbook, string sheet, int group, int groupType, int startRow, int endRow, string startColumn, string endColumn)
        {
            throw new ExcelException("【组合行列】需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetEdit_45984F39"));
        }
        /// <summary>
        /// 合并单元格
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">Sheet</param>
        /// <param name="merge">合并0/取消合并1</param>
        /// <param name="range">合并区域</param>       
        void IExcel.MergeCells(string workbook, string sheet, int merge, string range)
        {
            var rangeObj = ExcelUtils.ConvertExcelRangeAddress(range);
            Worksheet worksheet;
            try
            {
                worksheet = GetWorksheetByName(workbook, sheet);
                CellRange mergeRange = worksheet.Range[rangeObj];
                if (merge == 0)
                {
                    mergeRange.Merge();
                }
                else
                {
                    mergeRange.UnMerge();
                }
            }
            catch (Exception ex)
            {
                throw new ExcelException(ex.Message);
            }
        }
    }
}
