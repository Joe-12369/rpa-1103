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
    /// <summary>
    /// 使用软件自动化
    /// 设计器.表格编辑类组件
    /// </summary>
    public partial class SoftwareExcel : IExcel
    {
        private static void SetRangeValues(object worksheet, int row, int column, object[,] values)
        {
            using (var scope = new ComScope())
            {
                string firstColumn = ExcelUtils.ColumnIndexToLetter(column);
                string lastColumn = ExcelUtils.ColumnIndexToLetter(checked(column + values.GetLength(1) - 1));
                int lastRow = checked(row + values.GetLength(0) - 1);
                object range = scope.Track(ComLateBinding.GetProp(worksheet, "Range", $"{firstColumn}{row}:{lastColumn}{lastRow}"));
                ComLateBinding.SetProp(range, "Value", values);
            }
        }

        private void WriteVector(string workbook, string sheet, int row, string column, string contents, bool vertical)
        {
            using (var scope = new ComScope())
            {
                object worksheet = scope.Track(GetWorksheetByName(workbook, sheet));
                int firstColumn = ExcelUtils.ColumnLetterToIndex(column);
                ExcelUtils.WriteChunks(contents, true, chunk =>
                {
                    int offset = checked(chunk.RowOffset * chunk.ColumnCount);
                    var values = new object[vertical ? chunk.CellCount : 1, vertical ? 1 : chunk.CellCount];
                    for (int i = 0; i < chunk.CellCount; i++)
                        values[vertical ? i : 0, vertical ? 0 : i] = chunk.GetCell(i / chunk.ColumnCount, i % chunk.ColumnCount);
                    SetRangeValues(worksheet, checked(row + (vertical ? offset : 0)),
                        checked(firstColumn + (vertical ? 0 : offset)), values);
                });
            }
        }

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
                using (var com = new ComScope())
                {
                    object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                    object rangeObj = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{column}{row}"));
                    ComLateBinding.SetProp(rangeObj, "Value", content);
                }
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
            if (!ExcelUtils.StreamingEnabled)
            {
                LegacyWriteRow(workbook, sheet, column, row, contents);
                return;
            }
            WriteVector(workbook, sheet, row, column, contents, false);
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
            try
            {
                using (var com = new ComScope())
                {
                    object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                    switch (mode)
                    {
                        case "单独一行":
                            {
                                object rangeObj = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"A{row}"));
                                object entireRow = com.Track(ComLateBinding.GetProp(rangeObj, "EntireRow"));
                                ComLateBinding.Invoke(entireRow, "Delete");
                                break;
                            }
                        case "连续多行":
                            {
                                object cell1 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", startRow, 1));
                                object cell2 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", lastRow, 1));
                                object rangeObj = com.Track(ComLateBinding.GetProp(_sheet, "Range", cell1, cell2));
                                object entireRow = com.Track(ComLateBinding.GetProp(rangeObj, "EntireRow"));
                                ComLateBinding.Invoke(entireRow, "Delete");
                                break;
                            }
                        case "分散多行":
                            var rows_expr = ExcelUtils.ParseRowRangeText(rows);
                            var split_arry = rows_expr.Split(',');
                            List<object> result = new List<object>();
                            for (int i = 0; i < split_arry.Length; i += 15)
                            {
                                int length = Math.Min(15, split_arry.Length - i);
                                string[] chunk = new string[length];
                                Array.Copy(split_arry, i, chunk, 0, length);
                                result.Add(com.Track(ComLateBinding.GetProp(_sheet, "Range", string.Join(",", chunk))));
                            }
                            result.ForEach(r => { ComLateBinding.Invoke(r, "Delete"); });
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("HRESULT"))
                    throw new ExcelException("删除行失败；可能原因：单次删除的行数超过 Excel 软件的限制，请尝试在打开Excel 文件时取消选择【使用软件】选项，然后重试".I18n("SheetEdit_E6FFD1C8"));
                else throw ex;

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
            if (!ExcelUtils.StreamingEnabled)
            {
                LegacyWriteColumn(workbook, sheet, column, row, contents);
                return;
            }
            WriteVector(workbook, sheet, row, column, contents, true);
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
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                switch (mode)
                {
                    case "单独一列":
                        object r1 = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{column}:{column}"));
                        ComLateBinding.Invoke(r1, "Delete");
                        break;
                    case "连续多列":
                        object r2 = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{startColumn}:{lastColumn}"));
                        object cols = com.Track(ComLateBinding.GetProp(r2, "Columns"));
                        ComLateBinding.Invoke(cols, "Delete");
                        break;
                    case "分散多列":
                        object r3 = com.Track(ComLateBinding.GetProp(_sheet, "Range", ExcelUtils.ParseColumnRangeText(columns)));
                        ComLateBinding.Invoke(r3, "Delete");
                        break;
                    default:
                        break;
                }
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
        /// <param name="direction">插入方向 0 向上 1向下</param>          
        void IExcel.InsertRow(string workbook, string sheet, int index, int count, string contents, int direction)
        {
            if (!ExcelUtils.StreamingEnabled)
            {
                LegacyInsertRow(workbook, sheet, index, count, contents, direction);
                return;
            }
            int writeIndex = ExcelUtils.ConvertNegativeRow(index) + direction;
            using (var com = new ComScope())
            {
                object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                for (int i = 0; i < count; i++)
                {
                    using (var blockScope = new ComScope())
                    {
                        object rows = blockScope.Track(ComLateBinding.GetProp(worksheet, "Rows", writeIndex));
                        ComLateBinding.Invoke(rows, "Insert");
                    }
                }
                if (!string.IsNullOrEmpty(contents) && (File.Exists(contents) || Directory.Exists(contents)))
                {
                    if (count == 1) Instance.WriteRow(workbook, sheet, "A", writeIndex, contents);
                    else Instance.WriteRange(workbook, sheet, writeIndex, "A", contents);
                }
            }
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
            if (!ExcelUtils.StreamingEnabled)
            {
                LegacyInsertColumn(workbook, sheet, index, count, contents, direction);
                return;
            }
            int writeIndex = index + direction;
            using (var com = new ComScope())
            {
                object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                for (int i = 0; i < count; i++)
                {
                    using (var blockScope = new ComScope())
                    {
                        object columns = blockScope.Track(ComLateBinding.GetProp(worksheet, "Columns", writeIndex));
                        ComLateBinding.Invoke(columns, "Insert");
                    }
                }
                if (!string.IsNullOrEmpty(contents) && (File.Exists(contents) || Directory.Exists(contents)))
                {
                    if (count == 1) Instance.WriteColumn(workbook, sheet, ExcelUtils.ColumnIndexToLetter(writeIndex), 1, contents);
                    else ExcelUtils.WriteChunks(contents, false, chunk =>
                    {
                        if (chunk.RowOffset + chunk.RowCount > count) throw new ExcelException("插入列的数据数量超过插入列数".I18n("SheetEdit_CD425F76"));
                        var values = new object[chunk.ColumnCount, chunk.RowCount];
                        for (int r = 0; r < chunk.RowCount; r++)
                            for (int c = 0; c < chunk.ColumnCount; c++)
                                values[c, r] = chunk.GetCell(r, c);
                        SetRangeValues(worksheet, 1, checked(writeIndex + chunk.RowOffset), values);
                    });
                }
            }
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
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                var hidden = mode.Equals("隐藏") ? true : false;
                if (range.Equals("行"))
                {
                    var rowArr = ExcelUtils.ConvertStrToArray(rows);
                    for (int i = 0; i < rowArr.Length; i++)
                    {
                        object rng = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{rowArr[i]}:{rowArr[i]}"));
                        object entireRow = com.Track(ComLateBinding.GetProp(rng, "EntireRow"));
                        ComLateBinding.SetProp(entireRow, "Hidden", hidden);
                    }
                }
                else
                {
                    var columnArr = ExcelUtils.ConvertStrToArray(columns);
                    for (int i = 0; i < columnArr.Length; i++)
                    {
                        var _column = ExcelUtils.ConvertNegativeColumn(columnArr[i].ToString());
                        object rng = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{_column}:{_column}"));
                        object entireCol = com.Track(ComLateBinding.GetProp(rng, "EntireColumn"));
                        ComLateBinding.SetProp(entireCol, "Hidden", hidden);
                    }
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
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
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
                object cell = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{column}{row}"));
                float _left = left + (float)ComLateBinding.ToDouble(ComLateBinding.GetProp(cell, "Left"));
                float _top = top + (float)ComLateBinding.ToDouble(ComLateBinding.GetProp(cell, "Top"));
                object shapes = com.Track(ComLateBinding.GetProp(_sheet, "Shapes"));
                ComLateBinding.Invoke(shapes, "AddPicture", file, 0, 1, _left, _top, width, height);
            }
        }

        /// <summary>
        /// 删除图片
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="name">图片名称</param>            
        void IExcel.DeletePicture(string workbook, string sheet, string name)
        {
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                object picture = null;
                var deleteAll = Instance.ActionContext["__deleteAllPicture"] == 1;
                object shapes = com.Track(ComLateBinding.GetProp(_sheet, "Shapes"));
                if (deleteAll)
                {
                    foreach (object item in (System.Collections.IEnumerable)shapes)
                    {
                        ComLateBinding.Invoke(com.Track(item), "Delete");
                    }
                }
                else
                {
                    foreach (object shape in (System.Collections.IEnumerable)shapes)
                    {
                        com.Track(shape);
                        if (Convert.ToString(ComLateBinding.GetProp(shape, "Name")) == name.Replace("图片", "Picture"))
                        {
                            picture = shape;
                        }
                    }
                    if (picture == null) throw new ExcelException(Localize($"删除失败，名称为 {name} 的图片未找到", "SheetEdit_57CA76AC"));
                    else ComLateBinding.Invoke(picture, "Delete"); // it is already tracked in loop
                }
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
            if (!ExcelUtils.StreamingEnabled)
            {
                LegacyWriteRange(workbook, sheet, row, column, contents);
                return;
            }
            using (var scope = new ComScope())
            {
                object worksheet = scope.Track(GetWorksheetByName(workbook, sheet));
                int firstColumn = ExcelUtils.ColumnLetterToIndex(column);
                object[,] buffer = null;
                ExcelUtils.WriteChunks(contents, false, chunk =>
                    // Range.Value consumes the array synchronously before the next chunk reuses it.
                    SetRangeValues(worksheet, checked(row + chunk.RowOffset), firstColumn, chunk.ToMatrix(ref buffer)));
            }
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
            using (var com = new ComScope())
            {
                object rangeObj = com.Track(GetRange(workbook, sheet, startRow, startColumn, lastRow, lastColumn, type, range));
                try
                {
                    ComLateBinding.Invoke(rangeObj, "Select");
                }
                catch (Exception ex)
                {
                    ExcelPlugin.logger.LogError(ex.Message);
                }
            }
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
            using (var com = new ComScope())
            {
                object rangeObj = com.Track(GetRange(workbook, sheet, startRow, startColumn, lastRow, lastColumn, type, range));
                InvokeExcelMethod(rangeObj, "ClearContents");
                //rangeObj.ClearContents();
                if (mode.Equals("是"))
                    //rangeObj.ClearFormats();
                    InvokeExcelMethod(rangeObj, "ClearFormats");
            }
        }
        void IExcel.ClearRangeV2(string workbook, string sheet, string range, string mode)
        {
            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
            Instance.ClearRange(workbook, sheet, rangeInfo.startRow, rangeInfo.startColumn, rangeInfo.lastRow, rangeInfo.lastColumn, mode, rangeInfo.type, rangeInfo.range);
        }
        void IExcel.ClearRangeV3(string workbook, string sheet, string range, int clearType)
        {
            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
            using (var com = new ComScope())
            {
                object rangeObj = com.Track(GetRange(workbook, sheet, rangeInfo));
                switch (clearType)
                {
                    case 0:
                        InvokeExcelMethod(rangeObj, "Clear");//全部
                        break;
                    case 1:
                        InvokeExcelMethod(rangeObj, "ClearFormats");//格式
                        break;
                    case 2:
                        InvokeExcelMethod(rangeObj, "ClearContents");//内容
                        break;
                    case 3:
                        SetExcelPropertyRecursion(rangeObj, "Interior.ColorIndex".Split('.'), 1, 0);//背景填充
                        break;
                    case 4:
                        InvokeExcelMethod(rangeObj, "ClearComments");//批注
                        break;
                    case 5:
                        InvokeExcelMethod(rangeObj, "ClearHyperlinks");//超链接（不含格式）
                        break;
                    case 6:
                        object hyperlinks = com.Track(ComLateBinding.GetProp(rangeObj, "Hyperlinks"));
                        InvokeExcelMethod(hyperlinks, "Delete");//超链接（含格式）
                        break;
                }
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
            using (var com = new ComScope())
            {
                object rangeObj = com.Track(GetRange(workbook, sheet, startRow, startColumn, lastRow, lastColumn, type, range));
                switch (mode)
                {
                    case "右侧单元格左移": ComLateBinding.Invoke(rangeObj, "Delete", -4159); break;
                    case "下方单元格上移": ComLateBinding.Invoke(rangeObj, "Delete", -4162); break;
                    case "整行":
                        object entireRow = com.Track(ComLateBinding.GetProp(rangeObj, "EntireRow"));
                        ComLateBinding.Invoke(entireRow, "Delete");
                        break;
                    case "整列":
                        object entireColumn = com.Track(ComLateBinding.GetProp(rangeObj, "EntireColumn"));
                        ComLateBinding.Invoke(entireColumn, "Delete");
                        break;
                    default:
                        break;
                }
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
            using (var com = new ComScope())
            {
                object rangeObj = com.Track(GetRange(workbook, sheet, startRow, startColumn, lastRow, lastColumn, type, range));
                object interior = com.Track(ComLateBinding.GetProp(rangeObj, "Interior"));
                ComLateBinding.SetProp(interior, "Color", ExcelUtils.GetColor(color));
            }
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
            using (var com = new ComScope())
            {
                object rangeObj = com.Track(GetRange(workbook, sheet, startRow, startColumn, lastRow, lastColumn, type, range));
                object font = com.Track(ComLateBinding.GetProp(rangeObj, "Font"));
                ComLateBinding.SetProp(font, "Color", ExcelUtils.GetColor(color));
            }
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
            object _sheet = GetWorksheetByName(workbook, sheet);
            object range = null;
            using (var com = new ComScope())
            {
                object _usedRange = mode != "复制已编辑区域"
                    ? com.Track(ComLateBinding.GetProp(_sheet, "UsedRange"))
                    : ComLateBinding.GetProp(_sheet, "UsedRange");
                switch (mode)
                {
                    case "复制单元格":
                        range = ComLateBinding.GetProp(_sheet, "Range", $"{column}{row}");
                        break;
                    case "复制行":
                        object urCols1 = com.Track(ComLateBinding.GetProp(_usedRange, "Columns"));
                        object cell1 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", startRow, "A"));
                        object cell2 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", lastRow,
                            ComLateBinding.ToInt(ComLateBinding.GetProp(_usedRange, "Column")) + ComLateBinding.ToInt(ComLateBinding.GetProp(urCols1, "Count")) - 1));
                        range = ComLateBinding.GetProp(_sheet, "Range", cell1, cell2);
                        break;
                    case "复制列":
                        object urRows = com.Track(ComLateBinding.GetProp(_usedRange, "Rows"));
                        object cell3 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", 1, startColumn));
                        object cell4 = com.Track(ComLateBinding.GetProp(_sheet, "Cells",
                            ComLateBinding.ToInt(ComLateBinding.GetProp(_usedRange, "Row")) + ComLateBinding.ToInt(ComLateBinding.GetProp(urRows, "Count")) - 1, lastColumn));
                        range = ComLateBinding.GetProp(_sheet, "Range", cell3, cell4);
                        break;
                    case "复制区域":
                        object cell5 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", startRow2, startColumn2));
                        object cell6 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", lastRow2, lastColumn2));
                        range = ComLateBinding.GetProp(_sheet, "Range", cell5, cell6);
                        break;
                    case "复制已编辑区域":
                        range = _usedRange;
                        break;
                    default:
                        throw new ExcelException("无效得复制模式".I18n("SheetEdit_96E85981"));
                }
                ComLateBinding.Invoke(range, "Copy", Type.Missing);
                CopiedRange = range;
                SourceWorkbook = workbook;

                //690sp1 去掉返回复制内容
                if (!Instance.ActionContext["__IsCopyRangeV2"])
                {
                    SerializeCopiedRange(range);
                    ContextInstance.ActionContext.AddResult(ExcelUtils.TEMP_FILEPATH);
                }
            }
        }
        void IExcel.CopyRangeV2(string workbook, string sheet, string range)
        {
            var rangeAddress = ExcelUtils.ConvertExcelRangeAddress(range);
            object _sheet = GetWorksheetByName(workbook, sheet);
            object _range = ComLateBinding.GetProp(_sheet, "Range", rangeAddress);
            ComLateBinding.Invoke(_range, "Copy", Type.Missing);
            CopiedRange = _range;
            SourceWorkbook = workbook;

            //690sp1 去掉返回复制内容
            if (!Instance.ActionContext["__IsCopyRangeV2"])
            {
                using (var com = new ComScope())
                {
                    SerializeCopiedRange(_range);
                }
                ContextInstance.ActionContext.AddResult(ExcelUtils.TEMP_FILEPATH);
            }
        }
        private void SerializeCopiedRange(object range)
        {
            int batchSize = ExcelConfigurations.Info.GlobalSetting.BatchSize;
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            using (var scope = new ComScope())
            {
                object areas = scope.Track(ComLateBinding.GetProp(range, "Areas"));
                int areaCount = ComLateBinding.ToInt(ComLateBinding.GetProp(areas, "Count"));
                for (int a = 1; a <= areaCount; a++)
                {
                    using (var areaScope = new ComScope())
                    {
                        object area = areaScope.Track(GetExcelItem(areas, a));
                        object rows = areaScope.Track(ComLateBinding.GetProp(area, "Rows"));
                        object columns = areaScope.Track(ComLateBinding.GetProp(area, "Columns"));
                        object worksheet = areaScope.Track(ComLateBinding.GetProp(area, "Worksheet"));
                        int rowCount = ComLateBinding.ToInt(ComLateBinding.GetProp(rows, "Count"));
                        int columnCount = ComLateBinding.ToInt(ComLateBinding.GetProp(columns, "Count"));
                        int firstRow = ComLateBinding.ToInt(ComLateBinding.GetProp(area, "Row"));
                        int firstColumn = ComLateBinding.ToInt(ComLateBinding.GetProp(area, "Column"));
                        string startColumn = ExcelUtils.ColumnIndexToLetter(firstColumn);
                        string endColumn = ExcelUtils.ColumnIndexToLetter(firstColumn + columnCount - 1);
                        for (int offset = 0; offset < rowCount; offset += batchSize)
                        {
                            using (var blockScope = new ComScope())
                            {
                                int start = firstRow + offset;
                                int end = start + Math.Min(batchSize, rowCount - offset) - 1;
                                object block = blockScope.Track(ComLateBinding.GetProp(worksheet, "Range", $"{startColumn}{start}:{endColumn}{end}"));
                                ExcelUtils.SerializerDataToBinaryFile(ComLateBinding.GetProp(block, "Value"));
                            }
                        }
                    }
                }
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
            using (var com = new ComScope())
            {
                object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                object targetRange = com.Track(ComLateBinding.GetProp(worksheet, "Range", $"{column}{row}"));
                if (CopiedRange == null)
                {
                    throw new ExcelException("请先用复制区域组件进行复制".I18n("SheetEdit_8FAA414E"));
                }
                else
                {
                    ComLateBinding.Invoke(CopiedRange, "Copy", targetRange);
                }
                CopiedRange = null;
            }
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
            try
            {
                using (var com = new ComScope())
                {
                    object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                    object targetRange = com.Track(ComLateBinding.GetProp(worksheet, "Range", $"{column}{row}"));
                    ComLateBinding.Invoke(worksheet, "Activate");
                    if (CopiedRange == null)
                    {
                        throw new ExcelException("请先用【复制表格内容】组件复制一个区域".I18n("SheetEdit_3EA1E158"));
                    }
                    else
                    {
                        bool isExcel = false;
                        bool isClose = false;
                        try
                        {
                            object copiedApp = com.Track(ComLateBinding.GetProp(CopiedRange, "Application"));
                            isExcel = !Convert.ToString(ComLateBinding.GetProp(copiedApp, "Caption")).Contains("WPS");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex);
                            isClose = true;
                        }

                        if (isClose)
                        {
                            throw new ExcelException("复制的文件已关闭，无法完成粘贴".I18n("SheetEdit_DD065F30"));
                        }
                        //713 粘贴至可见单元格
                        if (ActionContext["visible"] == 1)
                        {
                            object visibleCells = com.Track(ComLateBinding.Invoke(CopiedRange, "SpecialCells", 12));
                            object areas = com.Track(ComLateBinding.GetProp(visibleCells, "Areas"));
                            int areaCount = ComLateBinding.ToInt(ComLateBinding.GetProp(areas, "Count"));
                            for (int a = 1; a <= areaCount; a++)
                            {
                                object current_area = com.Track(ComLateBinding.GetItem(areas, a));
                                object currentRows = com.Track(ComLateBinding.GetProp(current_area, "Rows"));
                                object currentCols = com.Track(ComLateBinding.GetProp(current_area, "Columns"));
                                object destRngStart = com.Track(ComLateBinding.GetProp(worksheet, "Range", $"{column}{ComLateBinding.GetProp(current_area, "Row")}"));
                                object destRng = com.Track(ComLateBinding.GetProp(destRngStart, "Resize",
                                    ComLateBinding.ToInt(ComLateBinding.GetProp(currentRows, "Count")),
                                    ComLateBinding.ToInt(ComLateBinding.GetProp(currentCols, "Count"))));
                                ComLateBinding.SetProp(destRng, "Value", ComLateBinding.GetProp(current_area, "Value"));
                            }
                            object app = com.Track(ComLateBinding.GetProp(worksheet, "Application"));
                            SetExcelProperty(app, "CutCopyMode", 0);
                        }
                        else
                        {
                            object copiedApp = com.Track(ComLateBinding.GetProp(CopiedRange, "Application"));
                            object app = com.Track(ComLateBinding.GetProp(worksheet, "Application"));
                            if (isExcel && !Equals(ComLateBinding.GetProp(copiedApp, "Hwnd"), ComLateBinding.GetProp(app, "Hwnd")))
                            {
                                try
                                {
                                    ComLateBinding.Invoke(worksheet, "Paste", targetRange);
                                }
                                catch (Exception ex)
                                {
                                    logger.LogWarning(ex);
                                    ComLateBinding.Invoke(CopiedRange, "Copy", Type.Missing);
                                    ComLateBinding.Invoke(worksheet, "Paste", targetRange);
                                }

                            }
                            else
                            {
                                try
                                {
                                    ComLateBinding.Invoke(targetRange, "PasteSpecial", pasteType, pasteOperation, skipBlanks, transpose);

                                }
                                catch (Exception ex)
                                {
                                    logger.LogWarning(ex);
                                    ComLateBinding.Invoke(CopiedRange, "Copy", Type.Missing);
                                    ComLateBinding.Invoke(targetRange, "PasteSpecial", pasteType, pasteOperation, skipBlanks, transpose);
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcelException(ex.Message);
            }
            finally
            {
                if (CopiedRange != null)
                {
                    ReleaseComObject(CopiedRange);
                    CopiedRange = null;
                }
            }

        }

        /// <summary>
        /// 删除重复
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="columns">待删除列名</param>  
        /// <param name="header">数据包含标题["是","否"]</param>  
        void IExcel.DeleteDuplicates(string workbook, string sheet, string columns, string header)
        {
            string numRet;
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                object usedRange = com.Track(ComLateBinding.GetProp(_sheet, "UsedRange"));
                object rows = com.Track(ComLateBinding.GetProp(usedRange, "Rows"));
                int count1 = ComLateBinding.ToInt(ComLateBinding.GetProp(rows, "Count"));
                if (count1 == 1 && ComLateBinding.GetProp(usedRange, "Value") == null)
                {
                    numRet = "0";
                    return;
                }
                var targetLastCell = GetLastCell(usedRange, 2, 2);
                if (header.Equals("是"))
                {
                    ComLateBinding.Invoke(usedRange, "RemoveDuplicates", ExcelUtils.ConvertColumnsIndexArray(columns), 1);
                }
                else
                {
                    ComLateBinding.Invoke(usedRange, "RemoveDuplicates", ExcelUtils.ConvertColumnsIndexArray(columns), 2);
                }
                var targetLastCell2 = GetLastCell(usedRange, 2, 2);

                numRet = $"{targetLastCell.Item2 - targetLastCell2.Item2}";
            }

            ContextInstance.ActionContext.AddResult("numRet", numRet);
          
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
            using (var com = new ComScope())
            {
                object worksheet = com.Track(GetWorksheetByName(workbook, sheet));
                if (isAutoFit)
                {
                    if (string.IsNullOrWhiteSpace(columns))
                    {
                        object cols = com.Track(ComLateBinding.GetProp(worksheet, "Columns"));
                        ComLateBinding.Invoke(cols, "AutoFit");
                    }
                    else
                    {
                        object rangeObj = com.Track(ComLateBinding.GetProp(worksheet, "Range", ExcelUtils.ParseColumnRangeText(columns)));
                        object cols = com.Track(ComLateBinding.GetProp(rangeObj, "Columns"));
                        ComLateBinding.Invoke(cols, "AutoFit");
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(columns))
                    {
                        object cols = com.Track(ComLateBinding.GetProp(worksheet, "Columns"));
                        ComLateBinding.SetProp(cols, "ColumnWidth", width);
                    }
                    else
                    {
                        object rangeObj = com.Track(ComLateBinding.GetProp(worksheet, "Range", ExcelUtils.ParseColumnRangeText(columns)));
                        ComLateBinding.SetProp(rangeObj, "ColumnWidth", width);
                    }
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
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                if (string.IsNullOrWhiteSpace(rows))
                {
                    object rowsObj = com.Track(ComLateBinding.GetProp(_sheet, "Rows"));
                    if (isAutoFit)
                    {
                        ComLateBinding.Invoke(rowsObj, "AutoFit");
                    }
                    else
                    {
                        ComLateBinding.SetProp(rowsObj, "RowHeight", height);
                    }
                }
                else
                {
                    var rows_expr = ExcelUtils.ParseRowRangeText(rows);
                    var split_arry = rows_expr.Split(',');
                    List<object> result = new List<object>();
                    for (int i = 0; i < split_arry.Length; i += 15)
                    {
                        int length = Math.Min(15, split_arry.Length - i);
                        string[] chunk = new string[length];
                        Array.Copy(split_arry, i, chunk, 0, length);
                        result.Add(com.Track(ComLateBinding.GetProp(_sheet, "Range", string.Join(",", chunk))));
                    }

                    if (isAutoFit)
                    {
                        result.ForEach(r =>
                        {
                            object rowsObj = com.Track(ComLateBinding.GetProp(r, "Rows"));
                            ComLateBinding.Invoke(rowsObj, "AutoFit");
                        });
                    }
                    else
                    {
                        result.ForEach(r => ComLateBinding.SetProp(r, "RowHeight", height));
                    }
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
            using (var com = new ComScope())
            {
                object ws = com.Track(GetWorksheetByName(workbook, sheet));
                object rangeObj = com.Track(ComLateBinding.GetProp(ws, "Range", $"{column}{row}"));
                object interior = com.Track(ComLateBinding.GetProp(rangeObj, "Interior"));
                ComLateBinding.SetProp(interior, "Color", ExcelUtils.GetColor(color));
            }
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
            using (var com = new ComScope())
            {
                object ws = com.Track(GetWorksheetByName(workbook, sheet));
                object rangeObj = com.Track(ComLateBinding.GetProp(ws, "Range", $"{column}{row}"));
                object font = com.Track(ComLateBinding.GetProp(rangeObj, "Font"));
                ComLateBinding.SetProp(font, "Color", ExcelUtils.GetColor(color));
            }
        }

        /// <summary>
        /// 设置单元格格式
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet 名称</param>
        /// <param name="range">区域范围</param>
        /// <param name="format">格式</param>
        void IExcel.SetCellFormat(string workbook, string sheet, string range, string format)
        {
            var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
            using (var com = new ComScope())
            {
                object rangeObj = com.Track(GetRange(workbook, sheet, rangeInfo.startRow, rangeInfo.startColumn, rangeInfo.lastRow, rangeInfo.lastColumn, rangeInfo.type, rangeInfo.range));
                ComLateBinding.SetProp(rangeObj, "NumberFormatLocal", format);
            }
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
            using (var com = new ComScope())
            {
                object rangeObj = com.Track(GetRange(workbook, sheet, rangeInfo.startRow, rangeInfo.startColumn, rangeInfo.lastRow, rangeInfo.lastColumn, rangeInfo.type, rangeInfo.range));
                object rFont = com.Track(ComLateBinding.GetProp(rangeObj, "Font"));
                object rInterior = com.Track(ComLateBinding.GetProp(rangeObj, "Interior"));

                if (cellFormatParams.FontFormat != "默认")
                {
                    ComLateBinding.SetProp(rFont, "Name", cellFormatParams.FontFormat);//字体
                }

                if (cellFormatParams.FontSize.HasValue)
                {
                    ComLateBinding.SetProp(rFont, "Size", cellFormatParams.FontSize.Value);//字号
                }

                BotAndItalicParam botAndItalicParam = new BotAndItalicParam();
                botAndItalicParam.AnalyseParam(cellFormatParams.BoldAndItalic);

                ComLateBinding.SetProp(rFont, "Bold", botAndItalicParam.Bold);//粗体
                ComLateBinding.SetProp(rFont, "Italic", botAndItalicParam.Italic);//斜体

                if (!string.IsNullOrWhiteSpace(cellFormatParams.FontColor))
                {
                    ComLateBinding.SetProp(rFont, "Color", ExcelUtils.GetColor(cellFormatParams.FontColor));//字体颜色
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
                            ComLateBinding.SetProp(rInterior, "Pattern", -4142);
                        }
                        else
                        {
                            int alpha = (A / 100) * 255;
                            ComLateBinding.SetProp(rInterior, "Color", Color.FromArgb(alpha, color.R, color.G, color.B));
                        }
                    }
                    else
                    {
                        ComLateBinding.SetProp(rInterior, "Color", ExcelUtils.GetColor(cellFormatParams.BackgroundColor));//单元格背景色
                    }
                }
                else
                {
                    ComLateBinding.SetProp(rInterior, "Pattern", -4142);
                }

                ComLateBinding.SetProp(rangeObj, "HorizontalAlignment", cellFormatParams.GetHorizontalParamOfInteropExcel(cellFormatParams.HorizontalAlignment)); //水平对齐
                ComLateBinding.SetProp(rangeObj, "VerticalAlignment", cellFormatParams.GetVerticalParamOfInteropExcel(cellFormatParams.VerticalAlignment)); //垂直对齐
                switch (cellFormatParams.TextControl)
                {
                    case 0:
                        SetExcelProperty(rangeObj, "WrapText", 0);
                        SetExcelProperty(rangeObj, "ShrinkToFit", 0);
                        break;
                    case 1:
                        SetExcelProperty(rangeObj, "WrapText", 1);
                        SetExcelProperty(rangeObj, "ShrinkToFit", 0);
                        break;
                    case 2:
                        SetExcelProperty(rangeObj, "WrapText", 0);
                        SetExcelProperty(rangeObj, "ShrinkToFit", 1);
                        break;
                    default:
                        break;
                }

                if (cellFormatParams.Borders != null)
                {
                    object rBorders = com.Track(ComLateBinding.GetProp(rangeObj, "Borders"));
                    foreach (var border in cellFormatParams.Borders)
                    {
                        if (border == null)
                        {
                            continue;
                        }
                        object borderObj = com.Track(ComLateBinding.GetProp(rBorders, "Item", border.Index));
                        if (border.Color > 0)
                        {
                            SetExcelProperty(borderObj, "Color", border.Color);
                        }
                        object colorIndex = ComLateBinding.GetProp(borderObj, "ColorIndex");
                        if (!(colorIndex is DBNull) && border.ColorIndex != Convert.ToInt32(colorIndex))
                        {
                            SetExcelProperty(borderObj, "ColorIndex", border.ColorIndex);
                        }
                        object lineStyle = ComLateBinding.GetProp(borderObj, "LineStyle");
                        if (!(lineStyle is DBNull) && border.LineStyle != Convert.ToInt32(lineStyle))
                        {
                            SetExcelProperty(borderObj, "LineStyle", border.LineStyle);
                            object weight = ComLateBinding.GetProp(borderObj, "Weight");
                            if (border.Weight != Convert.ToInt32(weight))
                                SetExcelProperty(borderObj, "Weight", border.Weight);
                        }
                        if (border.TintAndShade != null)
                        {
                            SetExcelProperty(borderObj, "TintAndShade", border.TintAndShade);
                        }

                    }
                }
                SetExcelProperty(rangeObj, "NumberFormat", cellFormatParams.DigitalFormat.GetFormatDes());
            }
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
            using (var com = new ComScope())
            {
                object rangeObj = com.Track(GetRange(workbook, sheet, rangeInfo.startRow, rangeInfo.startColumn, rangeInfo.lastRow, rangeInfo.lastColumn, rangeInfo.type, rangeInfo.range));
                object rFont = com.Track(ComLateBinding.GetProp(rangeObj, "Font"));
                object rInterior = com.Track(ComLateBinding.GetProp(rangeObj, "Interior"));

                cellFormatParams.FontFormat = Convert.ToString(ComLateBinding.GetProp(rFont, "Name"));//字体

                cellFormatParams.FontSize = Convert.ToInt32(ComLateBinding.GetProp(rFont, "Size"));//字号

                BotAndItalicParam botAndItalicParam = new BotAndItalicParam();
                botAndItalicParam.Bold = ComLateBinding.ToBool(ComLateBinding.GetProp(rFont, "Bold"));//粗体
                botAndItalicParam.Italic = ComLateBinding.ToBool(ComLateBinding.GetProp(rFont, "Italic"));//斜体
                cellFormatParams.BoldAndItalic = botAndItalicParam.GetBotAndItalicDescription();

                System.Drawing.Color fontColor = System.Drawing.ColorTranslator.FromOle(Convert.ToInt32(ComLateBinding.GetProp(rFont, "Color")));
                cellFormatParams.FontColor = ExcelUtils.TurnColorToCode(fontColor);

                System.Drawing.Color backgroundColor = System.Drawing.ColorTranslator.FromOle(Convert.ToInt32(ComLateBinding.GetProp(rInterior, "Color")));
                cellFormatParams.BackgroundColor = $"{ExcelUtils.TurnColorToCode(backgroundColor)},{(backgroundColor.A / 255) * 100}%";

                cellFormatParams.TextControl = ComLateBinding.ToBool(ComLateBinding.GetProp(rangeObj, "WrapText"))
                    ? 1
                    : ComLateBinding.ToBool(ComLateBinding.GetProp(rangeObj, "ShrinkToFit")) ? 2 : 0;

                cellFormatParams.HorizontalAlignment = cellFormatParams.GetEnumHorizontalAlignment(Convert.ToInt32(ComLateBinding.GetProp(rangeObj, "HorizontalAlignment")));
                cellFormatParams.VerticalAlignment = cellFormatParams.GetEnumVerticalAlignmentFromXlVAlign(Convert.ToInt32(ComLateBinding.GetProp(rangeObj, "VerticalAlignment")));


                cellFormatParams.DigitalFormat = new DigitalFormatClass();

                string numberFormat = Convert.ToString(ComLateBinding.GetProp(rangeObj, "NumberFormat"));
                if (cellFormatParams.DigitalFormat.GetEnumFormat(numberFormat, out EnumDigitalFormat enumElement))
                {
                    cellFormatParams.DigitalFormat.enumDigitalFormat = enumElement;
                    cellFormatParams.DigitalFormat.formatstring = numberFormat;
                }
                else
                {
                    cellFormatParams.DigitalFormat.enumDigitalFormat = EnumDigitalFormat.Custom;
                    cellFormatParams.DigitalFormat.formatstring = numberFormat;
                }
                return cellFormatParams.ToJson();
            }
        }

        /// <summary>
        /// ColumnSplit 分列
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheet"></param>     
        /// <param name="columnSplitParamJson"></param>
        void IExcel.ColumnSplit(string workbook, string sheet, string column, string columnSplitParamJson)
        {
            object _sheet = GetWorksheetByName(workbook, sheet);
            object rangeObj = ComLateBinding.GetProp(_sheet, "Range", $"{column}:{column}");

            ColumnSplitParam columnSplitParam;
            try
            {
                var columnSplitJsonParam = JsonConvert.DeserializeObject<ColumnSplitJsonParam>(columnSplitParamJson);
                columnSplitParam = columnSplitJsonParam.Reform();
            }
            catch (Exception ex)
            {
                logger.LogError($"分列参数解析失败:{ex} 参数内容:{columnSplitParamJson}");
                throw new Exception(Localize($"分列参数解析异常", "SheetEdit_286695F3"));
            }

            if ((int)columnSplitParam.xlTextFormat == 1)
            {
                if (!string.IsNullOrEmpty(columnSplitParam.xlOtherChar) && columnSplitParam.xlOtherChar.Length > 0)
                {
                    columnSplitParam.xlOtherChar = columnSplitParam.xlOtherChar.Substring(0, 1);
                }
                else
                {
                    columnSplitParam.xlOther = false;
                }

                ComLateBinding.Invoke(rangeObj, "TextToColumns", rangeObj, 1, -4142, columnSplitParam.xlConsecutiveDelimiter,
                        columnSplitParam.xlTab, columnSplitParam.xlSemicolon, columnSplitParam.xlComma, columnSplitParam.xlSpace,
                        columnSplitParam.xlOther, columnSplitParam.xlOtherChar);
            }
            else
            {
                int[,] fieldInfo = { { 0, 1 }, { columnSplitParam.xlWidth.Value, 1 } };

                // 执行固定宽度分列  
                ComLateBinding.Invoke(rangeObj, "TextToColumns", rangeObj, 2, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, fieldInfo);
            }
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
            var targetRange = JToken.Parse(destination);
            var targetRow = targetRange.Value<int?>("row");
            var targetColumn = targetRange.Value<string>("column");
            using (var com = new ComScope())
            {
                object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                if (startRow == 0)
                {
                    startRow = 1;
                }
                startRow = ExcelUtils.ConvertNegativeRow(startRow);
                if (endRow == 0)
                {
                    endRow = -1;
                }
                endRow = ExcelUtils.ConvertNegativeRangeEndRow(startRow, endRow);
                object rangeObj = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{column}{startRow}:{column}{endRow}"));
                logger.LogInformation($"split range:{ComLateBinding.GetProp(rangeObj, "Address")}");

                if (!targetRow.HasValue)
                {
                    targetRow = 1;
                }
                if (string.IsNullOrEmpty(targetColumn))
                {
                    targetColumn = ComLateBinding.GetProp(rangeObj, "Column").ToString();
                }
                targetRow = ExcelUtils.ConvertNegativeRow(targetRow);
                targetColumn = ExcelUtils.ConvertNegativeColumn(targetColumn);
                logger.LogInformation($"split target range:{targetColumn}{targetRow}");
                object _destination = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{targetColumn}{targetRow}"));
                if (insertCount > 0)
                {
                    //向右插入列
                    Instance.InsertColumn(workbook, sheet, ComLateBinding.ToInt(ComLateBinding.GetProp(_destination, "Column")), insertCount, null, 0);
                    _destination = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{targetColumn}{targetRow}"));
                }
            ColumnSplitParam columnSplitParam;
            try
            {
                var columnSplitJsonParam = JsonConvert.DeserializeObject<ColumnSplitJsonParam>(columnSplitParamJson);
                columnSplitParam = columnSplitJsonParam.Reform();
            }
            catch (Exception ex)
            {
                logger.LogError($"分列参数解析失败:{ex} 参数内容:{columnSplitParamJson}");
                throw new Exception(Localize($"分列参数解析异常", "SheetEdit_286695F3"));
            }
            var fields = JArray.Parse(fieldInfo);
            int[,] _fieldInfo = new int[fields.Count, 2];
            if (string.IsNullOrWhiteSpace(columnSplitParam.thousandsSeparator))
            {
                //添加一个空格
                columnSplitParam.thousandsSeparator = " ";
            }
            if (columnSplitParam.xlTextFormat == 1)
            {
                if (!string.IsNullOrEmpty(columnSplitParam.xlOtherChar) && columnSplitParam.xlOtherChar.Length > 0)
                {
                    columnSplitParam.xlOtherChar = columnSplitParam.xlOtherChar.Substring(0, 1);
                }
                else
                {
                    columnSplitParam.xlOther = false;
                }
                if (splitDataType == 1)
                {
                    for (int i = 0; i < fields.Count; i++)
                    {
                        _fieldInfo[i, 0] = fields[i].Value<int>("index");
                        _fieldInfo[i, 1] = fields[i].Value<int>("dataType");
                    }
                    ComLateBinding.Invoke(rangeObj, "TextToColumns", _destination, 1, columnSplitParam.xlTextQualifier, columnSplitParam.xlConsecutiveDelimiter,
                            columnSplitParam.xlTab, columnSplitParam.xlSemicolon, columnSplitParam.xlComma, columnSplitParam.xlSpace,
                            columnSplitParam.xlOther, columnSplitParam.xlOtherChar,
                            _fieldInfo,
                            columnSplitParam.decimalSeparator,
                            columnSplitParam.thousandsSeparator,
                            columnSplitParam.trailingMinusNumbers);
                }
                else
                {

                    ComLateBinding.Invoke(rangeObj, "TextToColumns", _destination, 1, columnSplitParam.xlTextQualifier, columnSplitParam.xlConsecutiveDelimiter,
                            columnSplitParam.xlTab, columnSplitParam.xlSemicolon, columnSplitParam.xlComma, columnSplitParam.xlSpace,
                            columnSplitParam.xlOther, columnSplitParam.xlOtherChar,
                            Type.Missing,
                            columnSplitParam.decimalSeparator,
                            columnSplitParam.thousandsSeparator,
                            columnSplitParam.trailingMinusNumbers);
                }

            }
            else
            {
                // 执行固定宽度分列  
                if (splitDataType == 1 || !columnSplitParam.xlWidth.HasValue)
                {
                    for (int i = 0; i < fields.Count; i++)
                    {
                        _fieldInfo[i, 0] = fields[i].Value<int>("index");
                        _fieldInfo[i, 1] = fields[i].Value<int>("dataType");
                    }
                    ComLateBinding.Invoke(rangeObj, "TextToColumns", _destination, 2,
                            columnSplitParam.xlTextQualifier,
                            Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                            _fieldInfo,
                            columnSplitParam.decimalSeparator,
                            columnSplitParam.thousandsSeparator,
                            columnSplitParam.trailingMinusNumbers);
                }
                else
                {
                    _fieldInfo = new int[2, 2] { { 0, 1 }, { columnSplitParam.xlWidth.Value, 1 } };
                    // 执行固定宽度分列  
                    ComLateBinding.Invoke(rangeObj, "TextToColumns", _destination, 2,
                            columnSplitParam.xlTextQualifier,
                            Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                            _fieldInfo,
                            columnSplitParam.decimalSeparator,
                            columnSplitParam.thousandsSeparator,
                            columnSplitParam.trailingMinusNumbers);
                }

            }
            }
        }

        /// <summary>
        /// 新建透视表
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="rule">透视表规则</param>            
        void IExcel.InsertPivotTable(string workbook, string rule, string sourceData)
        {
            try
            {
                using (var com = new ComScope())
                {
                    #region 创建前检查
                    var config = JsonConvert.DeserializeObject<PivotTableRule>(rule);
                    object wb = GetWorkbook(workbook);
                    object ws = null;
                    if (!Exist(workbook, config.SheetName))
                    {
                        object worksheets = com.Track(ComLateBinding.GetProp(wb, "Worksheets"));
                        ws = com.Track(ComLateBinding.Invoke(worksheets, "Add"));
                        ComLateBinding.SetProp(ws, "Name", config.SheetName);
                    }
                    else
                    {
                        ws = com.Track(GetWorksheetByName(workbook, config.SheetName));
                        //存在相同名称的透视表，删除后重新创建
                        object tables = com.Track(ComLateBinding.Invoke(ws, "PivotTables"));
                        foreach (object item in ComLateBinding.ToEnumerable(tables))
                        {
                            com.Track(item);
                            if (Convert.ToString(ComLateBinding.GetProp(item, "Name")) == config.Name)
                            {
                                object tableRange = com.Track(ComLateBinding.GetProp(item, "TableRange2"));
                                ComLateBinding.Invoke(tableRange, "Delete");
                                break;
                            }
                        }
                    }
                    if (config.SourceType != 1)
                    {
                        throw new Exception("创建的透视表类型无效".I18n("SheetEdit_E8015AD5"));
                    }
                    if (ComLateBinding.ToBool(ComLateBinding.GetProp(wb, "MultiUserEditing")))
                    {
                        throw new ExcelException("共享的工作簿无法创建透视表".I18n("SheetEdit_7BB536A"));
                    }
                    #endregion
                    if (!string.IsNullOrEmpty(sourceData))
                    {
                        config.SourceData = sourceData;
                    }
                    object pivotCaches = com.Track(ComLateBinding.Invoke(wb, "PivotCaches"));
                    object pivotCache = com.Track(ComLateBinding.Invoke(pivotCaches, "Create", config.SourceType, config.SourceData.Trim('=')));
                    object destination = com.Track(ComLateBinding.GetProp(ws, "Range", config.TableDestination));
                    object pivotTable = com.Track(ComLateBinding.Invoke(pivotCache, "CreatePivotTable", destination, config.Name));
                    var originFields = new List<string>();
                    object pivotFields = com.Track(ComLateBinding.Invoke(pivotTable, "PivotFields"));
                    foreach (object fieldObj in ComLateBinding.ToEnumerable(pivotFields))
                    {
                        com.Track(fieldObj);
                        originFields.Add(Convert.ToString(ComLateBinding.GetProp(fieldObj, "SourceName")));
                    }
                    //区分字段
                    List<TableField> groupFields = new List<TableField>();
                    List<TableField> normalFields = new List<TableField>();
                    foreach (var item in config.Fields)
                    {
                        string sourceName = item.SourceName?.ToString() ?? string.Empty;
                        if (originFields.Contains(sourceName))
                        {
                            normalFields.Add(item);
                        }
                        else
                        {
                            groupFields.Add(item);
                        }

                    }

                    //透视表属性相关设置
                    var availableFields = config.GetType().GetProperties().Select(f => new { name = f.Name, value = f.GetValue(config) });
                    foreach (var item in availableFields)
                    {
                        if ("SheetName,SourceType,TableDestination,VisualTotalsForSets,Fields".Contains(item.name))
                        {
                            continue;
                        }
                        SetExcelProperty(pivotTable, item.name, item.value);
                    }

                    #region 设置字段
                    var FindField = new System.Func<List<TableField>, int, TableField[]>((fields, orientation) =>
                    {
                        return fields.Where(f => f.Orientation == orientation).OrderBy(f => f.Position).Select(f => f).ToArray();
                    });
                    var FindPivotField = new System.Func<int, string, object>((orientation, name) =>
                    {
                        try
                        {
                            if (orientation == 1)
                            {
                                return ComLateBinding.Invoke(pivotTable, "RowFields", name);
                            }
                            else if (orientation == 2)
                            {
                                return ComLateBinding.Invoke(pivotTable, "ColumnFields", name);
                            }
                            else if (orientation == 3)
                            {
                                return ComLateBinding.Invoke(pivotTable, "PageFields", name);
                            }
                            else
                            {
                                return ComLateBinding.Invoke(pivotTable, "DataFields", name);
                            }
                        }
                        catch (Exception)
                        {
                            return null;
                        }
                    });
                    var SetPivotField = new System.Action<TableField[]>((fields) =>
                    {
                        foreach (var field in fields)
                        {
                            object pivotField = null;
                            try
                            {
                                //数据字段优先按显示名查找，避免 SourceName 与行/列字段同名时定位到错误字段
                                if (Convert.ToInt32(field.Orientation) == 4 && field.Name != null)
                                {
                                    pivotField = com.Track(FindPivotField(4, Convert.ToString(field.Name)));
                                }
                                if (pivotField == null && originFields.Contains(field.SourceName))
                                {
                                    pivotField = com.Track(ComLateBinding.Invoke(pivotTable, "PivotFields", field.SourceName));
                                }
                                else if (pivotField == null)
                                {
                                    for (int i = 1; i < 5; i++)
                                    {
                                        pivotField = com.Track(FindPivotField(i, field.SourceName));
                                        if (pivotField != null)
                                        {
                                            break;
                                        }
                                    }
                                    if (pivotField == null)
                                    {
                                        continue;
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                logger.LogTrace(ex.StackTrace);
                                logger.LogWarning($"set pivot table field exception:{field.SourceName}");
                                continue;
                            }

                            SetExcelProperty(pivotField, "Orientation", field.Orientation);
                            SetExcelProperty(pivotField, "BaseField", field.BaseField);
                            SetExcelProperty(pivotField, "Calculation", field.Calculation);
                            SetExcelProperty(pivotField, "Name", field.Name);
                            SetExcelProperty(pivotField, "Caption", field.Caption);
                            if (field.Subtotals != null)
                            {
                                for (int i = 0; i < field.Subtotals.Length; i++)
                                {
                                    ComLateBinding.SetPropIndexed(pivotField, "Subtotals", new object[] { i + 1 }, field.Subtotals[i]);
                                }
                            }

                            SetExcelProperty(pivotField, "NumberFormat", field.NumberFromat);
                            SetExcelProperty(pivotField, "DatabaseSort", field.DatabaseSort);
                            SetExcelProperty(pivotField, "DisplayAsTooltip", field.DisplayAsTooltip);
                            SetExcelProperty(pivotField, "DisplayInReport", field.DisplayInReport);
                            SetExcelProperty(pivotField, "DragToColumn", field.DragToColumn);
                            SetExcelProperty(pivotField, "DragToData", field.DragToData);
                            SetExcelProperty(pivotField, "DragToHide", field.DragToHide);
                            SetExcelProperty(pivotField, "DragToPage", field.DragToPage);
                            SetExcelProperty(pivotField, "DragToRow", field.DragToRow);
                            SetExcelProperty(pivotField, "EnableItemSelection", field.EnableItemSelection);
                            SetExcelProperty(pivotField, "EnableMultiplePageItems", field.EnableMultiplePageItems);
                            SetExcelProperty(pivotField, "Formula", field.Formula);
                            SetExcelProperty(pivotField, "Function", field.Function);
                            SetExcelProperty(pivotField, "Hidden", field.Hidden);
                            SetExcelProperty(pivotField, "IncludeNewItemsInFilter", field.IncludeNewItemsInFilter);
                            SetExcelProperty(pivotField, "LayoutBlankLine", field.LayoutBlankLine);
                            SetExcelProperty(pivotField, "LayoutCompactRow", field.LayoutCompactRow);
                            SetExcelProperty(pivotField, "LayoutForm", field.LayoutForm);
                            SetExcelProperty(pivotField, "LayoutPageBreak", field.LayoutPageBreak);
                            SetExcelProperty(pivotField, "LayoutSubtotalLocation", field.LayoutSubtotalLocation);
                            SetExcelProperty(pivotField, "MemberPropertyCaption", field.MemberPropertyCaption);
                            SetExcelProperty(pivotField, "PropertyOrder", field.PropertyOrder);
                            SetExcelProperty(pivotField, "RepeatLabels", field.RepeatLabels);
                            SetExcelProperty(pivotField, "ServerBased", field.ServerBased);
                            SetExcelProperty(pivotField, "ShowAllItems", field.ShowAllItems);
                            SetExcelProperty(pivotField, "ShowDetail", field.ShowDetail);
                            SetExcelProperty(pivotField, "StandardFormula", field.StandardFormula);
                            SetExcelProperty(pivotField, "SubtotalName", field.SubtotalName);
                            SetExcelProperty(pivotField, "UseMemberPropertyAsCaption", field.UseMemberPropertyAsCaption);
                            SetExcelProperty(pivotField, "Value", field.Value);
                        }
                    });

                    //row:1,column:2,page:3,data:4
                    var normal_row = FindField(normalFields, 1);
                    var normal_col = FindField(normalFields, 2);
                    var normal_data = FindField(normalFields, 4);
                    var normal_page = FindField(normalFields, 3);
                    var group_row = FindField(groupFields, 1);
                    var group_col = FindField(groupFields, 2);
                    var group_data = FindField(groupFields, 4);
                    var group_page = FindField(groupFields, 3);
                    SetPivotField(normal_row);
                    SetPivotField(normal_col);
                    SetPivotField(normal_data);
                    SetPivotField(normal_page);
                    //重新获取透视表字段
                    originFields.Clear();
                    pivotFields = com.Track(ComLateBinding.Invoke(pivotTable, "PivotFields"));
                    foreach (object fieldObj in ComLateBinding.ToEnumerable(pivotFields))
                    {
                        try
                        {
                            //WPS 有些字段异常
                            com.Track(fieldObj);
                            originFields.Add(Convert.ToString(ComLateBinding.GetProp(fieldObj, "SourceName")));
                        }
                        catch
                        {
                            continue;
                        }

                    }
                    SetPivotField(group_row);
                    SetPivotField(group_col);
                    SetPivotField(group_data);
                    SetPivotField(group_page);

                    //设置字段顺序
                    foreach (var item in config.Fields)
                    {
                        try
                        {
                            object pivotField = null;
                            if (Convert.ToInt32(item.Orientation) == 4)
                            {
                                //数据字段按显示名在 DataFields 中定位，避免 SourceName 与行/列字段同名时定位到错误字段
                                if (item.Name != null)
                                {
                                    pivotField = com.Track(FindPivotField(4, Convert.ToString(item.Name)));
                                }
                            }
                            else if (originFields.Contains(item.SourceName))
                            {
                                pivotField = com.Track(ComLateBinding.Invoke(pivotTable, "PivotFields", item.SourceName));
                            }
                            else
                            {
                                for (int i = 1; i < 5; i++)
                                {
                                    pivotField = com.Track(FindPivotField(i, item.SourceName));
                                    if (pivotField != null)
                                    {
                                        break;
                                    }
                                }
                            }
                            if (pivotField == null)
                            {
                                continue;
                            }
                            SetExcelProperty(pivotField, "Position", item.Position);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning($"set pivot field position exception:{item.Position},{ex}");
                            continue;
                        }
                    }

                    #endregion
                    ContextInstance.ActionContext.AddResult("name", config.Name);
                }
            }
            catch (Exception ex)
            {
                logger.LogTrace(ex.StackTrace);
                throw new ExcelException(Localize($"新建透视表失败:{ex.Message}", "SheetEdit_9BC09152"));
            }

        }

        /// <summary>
        /// 刷新透视表
        /// </summary>
        /// <param name="workbook">文件对象</param>
        /// <param name="sheet">Sheet</param>
        /// <param name="pivotTable">透视表</param>
        void IExcel.RefreshPivotTable(string workbook, string sheet, string pivotTable)
        {
            object worksheet = GetWorksheetByName(workbook, sheet);
            object tables = null;
            try
            {
                tables = ComLateBinding.Invoke(worksheet, "PivotTables");
                bool refreshSuccess = false;
                foreach (object table in ComLateBinding.ToEnumerable(tables))
                {
                    if (Convert.ToString(ComLateBinding.GetProp(table, "Name")).Equals(pivotTable))
                    {
                        ComLateBinding.Invoke(table, "RefreshTable");
                        refreshSuccess = true;
                        break;
                    }
                }
                if (!refreshSuccess)
                {
                    throw new ExcelException(Localize($"{ComLateBinding.GetProp(worksheet, "Name")} 中没有找到透视表 {pivotTable}", "SheetEdit_C21FE59C"));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new ExcelException(Localize($"刷新数据透视表失败：{ex.Message}", "SheetEdit_9FA50B47"));
            }
            finally
            {
                if (worksheet != null)
                {
                    ReleaseComObject(worksheet);
                }
                if (tables != null)
                {
                    ReleaseComObject(tables);
                }
            }
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
            object worksheet = GetWorksheetByName(workbook, sheet);
            bool table_exception = true;
            bool field_exception = true;
            try
            {
                object table = ComLateBinding.Invoke(worksheet, "PivotTables", pivotTable);
                table_exception = false;
                object pivotField = ComLateBinding.Invoke(table, "PivotFields", field);
                field_exception = false;
                object items = ComLateBinding.Invoke(pivotField, "PivotItems");
                var filter = ExcelUtils.DeserializeStrArray(fieldValue);
                ComLateBinding.SetProp(pivotField, "EnableMultiplePageItems", true);
                ComLateBinding.SetProp(pivotField, "ShowAllItems", false);

                if (filter.Length == 0)
                {
                    //全部
                    foreach (object item in ComLateBinding.ToEnumerable(items))
                    {
                        ComLateBinding.SetProp(item, "Visible", true);
                    }
                }
                else
                {
                    foreach (object item in ComLateBinding.ToEnumerable(items))
                    {
                        ComLateBinding.SetProp(item, "Visible", filter.Any(f => f?.ToString() == Convert.ToString(ComLateBinding.GetProp(item, "Name"))));
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                if (table_exception)
                {
                    throw new Exception(Localize($"未找到数据透视表 【{pivotTable}】 失败", "SheetEdit_7711C339"));
                }
                else if (field_exception)
                {
                    throw new ExcelException(Localize($"获取筛选字段 {field} 失败，请检查筛选字段是否在透视表中并支持筛选", "SheetEdit_80691CBD"));
                }
                else
                    throw new ExcelException("筛选数据透视表失败".I18n("SheetEdit_17CC538E"));
            }
        }

        /// <summary>
        /// 新建透视图
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="rule">透视图规则</param>            
        void IExcel.InsertPivotView(string workbook, string rule)
        {
            object wb = GetWorkbook(workbook);
            var config = JsonConvert.DeserializeObject<PivotTableRule>(rule);
            object ws = GetWorksheetByName(workbook, config.SheetName);
            object pivotCaches = ComLateBinding.Invoke(wb, "PivotCaches");
            _ = ComLateBinding.Invoke(pivotCaches, "Create", config.SourceType, config.SourceData);
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
        void IExcel.Sort(string workbook, string sheet, int sortOrder, string sortRange, int region, int header, string rule)
        {
            object worksheet = GetWorksheetByName(workbook, sheet);
            try
            {
                var editRange = ExcelPlugin.Instance.RequestContext.ExcelUsedRange;
                string fields = null;
                string setRange = null;
                var rangeObj = ExcelUtils.ConvertToExcelRange(sortRange);
                switch (rangeObj.option)
                {
                    case 0://单元格
                        {
                            fields = $"{rangeObj.column}{rangeObj.row}:{rangeObj.column}{editRange.EndRow}";
                            setRange = $"{editRange.StartColumn}1:{editRange.EndColumn}{editRange.EndRow}";
                        }
                        break;
                    case 1://行
                        {
                            if (region == 0)
                            {
                                fields = $"{editRange.StartColumn}{rangeObj.row}";
                                setRange = $"{editRange.StartColumn}{rangeObj.row}:{editRange.EndColumn}{rangeObj.row}";
                            }
                            else
                            {
                                fields = $"{editRange.StartColumn}{rangeObj.row}:{editRange.StartColumn}{editRange.EndRow}";
                                setRange = $"{editRange.StartColumn}1:{editRange.EndColumn}{editRange.EndRow}";
                            }

                        }
                        break;
                    case 2://列
                        {
                            if (region == 0)
                            {
                                fields = $"{rangeObj.column}1:{rangeObj.column}{editRange.EndRow}";
                                setRange = fields;
                            }
                            else
                            {
                                fields = $"{rangeObj.column}1:{rangeObj.column}{editRange.EndRow}";
                                setRange = $"{editRange.StartColumn}1:{editRange.EndColumn}{editRange.EndRow}";
                            }
                        }
                        break;
                    case null:
                    case 3:
                        {
                            fields = $"{rangeObj.startColumn}{rangeObj.startRow}:{rangeObj.startColumn}{rangeObj.lastRow}";
                            setRange = $"{rangeObj.startColumn}{rangeObj.startRow}:{rangeObj.lastColumn}{rangeObj.lastRow}";
                        }
                        break;
                    default:
                        {
                            fields = $"{editRange.StartColumn}1:{editRange.StartColumn}{editRange.EndRow}";
                            setRange = $"{editRange.StartColumn}1:{editRange.EndColumn}{editRange.EndRow}"; ;
                        }
                        break;
                }
                logger.LogInformation($"SortMethod Fields:{fields},Set Range:{setRange},Option:{rangeObj.option}");
                object Sort = GetExcelProperty(worksheet, "Sort");
                object SortFields = GetExcelProperty(Sort, "SortFields");
                {
                    InvokeExcelMethod(SortFields, "Clear");
                    //自定义排序
                    if (sortOrder == 3)
                    {
                        var customSort = JsonConvert.DeserializeObject<SortRule>(rule);
                        SetExcelProperty(Sort, "Header", customSort.Header);
                        SetExcelProperty(Sort, "Orientation", customSort.Orientation);
                        SetExcelProperty(Sort, "SortMethod", customSort.SortMethod);

                        foreach (var item in customSort.SortFields)
                        {
                            object key_range = ComLateBinding.ToInt(ComLateBinding.GetProp(Sort, "Orientation")) == 1
                                ? ComLateBinding.GetProp(worksheet, "Columns", item.Key)
                                : ComLateBinding.GetProp(worksheet, "Rows", item.Key);
                            // 保留原 Add 参数顺序，避免自定义排序字段行为变化。
                            object sort_field = ComLateBinding.Invoke(SortFields, "Add", key_range, item.SortOn, item.Order, JsonConvert.SerializeObject(item.CustomOrder), 0);
                            if (item.SortOnValue != null)
                            {
                                object sortOnValue = ComLateBinding.GetProp(sort_field, "SortOnValue");
                                if (item.SortOnValue.Color != null)
                                {
                                    ComLateBinding.SetProp(sortOnValue, "Color", item.SortOnValue.Color);
                                }
                                if (item.SortOn == 1 && item.SortOnValue.Pattern?.Value == -4142)
                                {
                                    ComLateBinding.SetProp(sortOnValue, "Pattern", item.SortOnValue.Pattern);
                                }
                                if (item.SortOn == 2 && item.SortOnValue.ColorIndex?.Value == -4105)
                                {
                                    ComLateBinding.SetProp(sortOnValue, "ColorIndex", item.SortOnValue.ColorIndex);
                                }

                            }
                        }
                    }
                    else
                    {
                        SetExcelProperty(Sort, "SortMethod", 1);
                        if (header == 0)
                        {
                            SetExcelProperty(Sort, "header", 1);
                        }
                        else
                        {
                            SetExcelProperty(Sort, "header", 2);
                        }
                        //添加排序字段
                        object fieldRange = ComLateBinding.GetProp(worksheet, "Range", fields);
                        InvokeExcelMethod(SortFields, "Add", new object[] { fieldRange, 0, sortOrder, Type.Missing, 0 });
                    }
                    object sortSetRange = ComLateBinding.GetProp(worksheet, "Range", setRange);
                    InvokeExcelMethod(Sort, "SetRange", new object[] { sortSetRange });
                    InvokeExcelMethod(Sort, "Apply");
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new Exception("排序失败，请检查排序参数是否正确".I18n("SheetEdit_D9830257"));
            }

        }

        /// <summary>
        /// 数据筛选
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="rule">筛选规则</param>            
        void IExcel.Filter(string workbook, string sheet, string column, string condition, string criteria, string on_header,
            string isMultiFilter, string pre_filterData, string pre_lineNum)
        {
            if (isMultiFilter.Equals("true") && (string.IsNullOrEmpty(pre_filterData) || string.IsNullOrEmpty(pre_lineNum)))
            {
                throw new ExcelException(eErrorMessageType.Error_ParamsError.GetMessage());
            }

            object workSheet = GetWorksheetByName(workbook, sheet);
            List<int> rowNos = new List<int>();
            int startIndex = on_header == "1" ? 1 : 2;
            var rules = JsonConvert.DeserializeObject<List<Criteria>>(criteria);

            if (isMultiFilter == "false")
            {
                if (ContextInstance.ActionContext.ExcelUsedRange.EndRow >= startIndex)
                {
                    object range = ComLateBinding.GetProp(workSheet, "Range", $"{column}{startIndex}:{column}{ContextInstance.ActionContext.ExcelUsedRange.EndRow}");
                    object rangeValue = ComLateBinding.GetProp(range, "Value");
                    JArray columnList = null;
                    if (rangeValue is Array array)
                    {
                        columnList = new JArray(rangeValue as Array);
                    }
                    else
                    {
                        columnList = new JArray(new object[1] { rangeValue });
                    }
                    int rowIndex = startIndex;
                    foreach (JValue jValue in columnList.Cast<JValue>())
                    {
                        string cellValue = string.Empty;
                        if (jValue?.Value != null)
                        {
                            cellValue = jValue.Value.ToString();
                        }
                        if (ExcelUtils.ValidationRule(cellValue, condition, rules))
                        {
                            rowNos.Add(rowIndex);
                            object rowRange = ComLateBinding.GetProp(workSheet, "Range", $"A{rowIndex}:{ContextInstance.ActionContext.ExcelUsedRange.EndColumn}{rowIndex}");
                            object _rangeValue = ComLateBinding.GetProp(rowRange, "Value");
                            if (_rangeValue is Array)
                            {
                                ExcelUtils.SerializerDataToBinaryFile(_rangeValue, "filterData" + rowIndex);
                            }
                            else
                            {
                                var arrData = new object[3] { 1, 1, new object[1] { _rangeValue } };
                                ExcelUtils.SerializerDataToBinaryFile(arrData, "filterData" + rowIndex);
                            }
                        }
                        rowIndex++;
                    }
                }
            }
            else
            {
                try
                {
                    List<JArray> preFilterData = JsonConvert.DeserializeObject<List<JArray>>(pre_filterData);
                    List<int> preLineNum = JsonConvert.DeserializeObject<List<int>>(pre_lineNum);
                    if (preLineNum.Count != preFilterData.Count)
                    {
                        ExcelPlugin.logger.LogError("列数量不匹配，无法处理");
                        throw new ExcelException("数据非法或者列数量不匹配，无法处理".I18n("SheetEdit_D54979B3"));
                    }

                    int columnIndex = ExcelUtils.ColumnLetterToIndex(column) - 1;
                    for (int i = 0; i < preFilterData.Count; i++)
                    {
                        var cellToken = preFilterData[i][columnIndex] as JValue;
                        string cellValue;
                        if (cellToken?.Value != null)
                        {
                            cellValue = cellToken.Value.ToString();
                        }
                        else
                        {
                            cellValue = string.Empty;
                        }

                        if (ExcelUtils.ValidationRule(cellValue, condition, rules))
                        {
                            rowNos.Add(preLineNum[i]);
                            var arrData = new object[3] { 1, preFilterData[i].Count, preFilterData[i].ToObject<object[]>() };
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
        /// <param name="name">宏名称</param> 
        /// <param name="mode">执行方式</param>          
        void IExcel.RunMacro(string workbook, string script, string args, int mode, string code)
        {
            try
            {
                object wb = GetWorkbook(workbook);
                var arrArgs = ExcelUtils.DeserializeStrArray(args);
                var macroArgs = new object[arrArgs.Length + 1];
                var baseDir = Directory.GetParent(ExcelPlugin.Instance.GetType().Assembly.Location).FullName + "\\vb";
                if (!Directory.Exists(baseDir))
                {
                    Directory.CreateDirectory(baseDir);
                }
                var RemoveModule = new System.Action<string>((name) =>
                {
                    object vbProject = ComLateBinding.GetProp(wb, "VBProject");
                    object vbComponents = ComLateBinding.GetProp(vbProject, "VBComponents");
                    foreach (object module in ComLateBinding.ToEnumerable(vbComponents))
                    {
                        object codeModule = ComLateBinding.GetProp(module, "CodeModule");
                        if (Convert.ToString(ComLateBinding.GetProp(codeModule, "Name")).Equals(name))
                        {
                            ComLateBinding.Invoke(vbComponents, "Remove", module);
                            break;
                        }
                    }
                });
                var moduleName = new Random().Next(10, 1000);
                if (mode == 0)
                {
                    var vbScript = Path.Combine(baseDir, script + ".bas");
                    if (File.Exists(vbScript))
                    {
                        RemoveModule($"RPA_{script}");
                        object vbProject = ComLateBinding.GetProp(wb, "VBProject");
                        object vbComponents = ComLateBinding.GetProp(vbProject, "VBComponents");
                        object _component = ComLateBinding.Invoke(vbComponents, "Import", vbScript);
                        object codeModule = ComLateBinding.GetProp(_component, "CodeModule");
                        ComLateBinding.SetProp(codeModule, "Name", $"RPA_{script}");
                    }
                    macroArgs[0] = script;
                }
                else
                {
                    var funName = Regex.Match(code, "(?<type>Sub|Function)\\s+(?<name>[\\w|\\d|_]+)\\s*(\\(?(?<param>.*)\\))?", RegexOptions.IgnoreCase);
                    if (!funName.Success)
                    {
                        throw new ExcelException("代码中未找到有效的 Sub 或 Function".I18n("SheetEdit_81FBB6E1"));
                    }
                    var tempFile = Path.Combine(baseDir, moduleName + ".bas");
                    File.WriteAllText(tempFile, Regex.Replace(code, "\\\"\\n\\S.*?\\\"", m => m.Value.Replace("\n", "\\n")), System.Text.Encoding.Default);
                    RemoveModule($"RPA_{moduleName}");
                    object vbProject = ComLateBinding.GetProp(wb, "VBProject");
                    object vbComponents = ComLateBinding.GetProp(vbProject, "VBComponents");
                    object _component = ComLateBinding.Invoke(vbComponents, "Import", tempFile);
                    object codeModule = ComLateBinding.GetProp(_component, "CodeModule");
                    ComLateBinding.SetProp(codeModule, "Name", $"RPA_{moduleName}");
                    macroArgs[0] = funName.Groups["name"].Value.Trim();
                    File.Delete(tempFile);
                }

                Array.Copy(arrArgs, 0, macroArgs, 1, arrArgs.Length);
                try
                {
                    using (var com = new ComScope())
                    {
                        object app = com.Track(ComLateBinding.GetProp(wb, "Application"));
                        ComLateBinding.Invoke(app, "Run", macroArgs);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    if (mode == 1)
                        RemoveModule($"RPA_{moduleName}");
                }
                
            }
            catch (Exception ex)
            {
                ExcelPlugin.logger.LogError(ex.Message);
                throw new ExcelException("执行宏发生错误。可能原因：1，VBA 代码语法错误; 2，Excel 文件中已存在相同名称的 Sub 或 Function;3，Office 执行宏权限没有打开".I18n("SheetEdit_8233F915"));
            }
        }
        void AutoFill(string workbook, string sheet, string sourceRefr, string destination)
        {
            object _sheet = GetWorksheetByName(workbook, sheet);
            var destObj = JObject.Parse(destination);
            var destType = destObj.Value<int>("type");

            //sourceType:0 单元格,1 区域         
            if (!TryParseRange(_sheet, sourceRefr, out object range))
                throw new ExcelException(Localize($"源区域信息填写无效：{sourceRefr}", "SheetEdit_6EF989FE"));

            string rangeAddress = Convert.ToString(ComLateBinding.GetProp(range, "Address"));
            var address = Regex.Match((rangeAddress.Contains(":") ? rangeAddress : $"{rangeAddress}:{rangeAddress}"), @"^\$(\w+)\$(\d+):\$(\w+)\$(\d+)$");

            //destType: 0 填充数量，1 填充区域
            string destRefr;
            if (destType == 0)
            {
                if (!int.TryParse(destObj.Value<string>("count"), out int fillCount) || fillCount < 1)
                    throw new ExcelException(Localize($"填充数量不是一个有效的整数", "SheetEdit_5FD325BF"));

                object sourceRows = ComLateBinding.GetProp(range, "Rows");
                object sourceColumns = ComLateBinding.GetProp(range, "Columns");
                int rangeRow = ComLateBinding.ToInt(ComLateBinding.GetProp(range, "Row"));
                int rangeColumn = ComLateBinding.ToInt(ComLateBinding.GetProp(range, "Column"));
                var targetRow = ComLateBinding.ToInt(ComLateBinding.GetProp(sourceRows, "Count")) * (fillCount + 1) + rangeRow - 1;
                var targetCol = ComLateBinding.ToInt(ComLateBinding.GetProp(sourceColumns, "Count")) * (fillCount + 1) + rangeColumn - 1;
                switch (destObj.Value<string>("direction"))
                {
                    case "向下填充":
                        destRefr = $"{address.Groups[1].Value}{address.Groups[2].Value}:{address.Groups[3].Value}{targetRow}";
                        break;
                    case "向左填充":
                        var colLetter = targetCol >= rangeColumn ? "A" : ExcelUtils.ColumnIndexToLetter(targetCol);
                        destRefr = $"{colLetter}{address.Groups[2].Value}:{address.Groups[3].Value}{address.Groups[4].Value}";
                        break;
                    case "向上填充":
                        var rowIndex = targetCol >= rangeRow ? 1 : targetRow;
                        destRefr = $"{address.Groups[1].Value}{rowIndex}:{address.Groups[3].Value}{address.Groups[4].Value}";
                        break;
                    case "向右填充":
                        destRefr = $"{address.Groups[1].Value}{address.Groups[2].Value}:{ExcelUtils.ColumnIndexToLetter(targetCol)}{address.Groups[4].Value}";
                        break;
                    default:
                        throw new ExcelException(Localize($"{destObj.Value<string>("direction")}不是有效的填充方向", "SheetEdit_A6BE90AB"));
                }
            }
            else
            {
                var destStartColumn = ExcelUtils.ConvertNegativeColumn(destObj.Value<string>("startColumn"));
                var destStartRow = ExcelUtils.ConvertNegativeRow(destObj.Value<int>("startRow"));
                var destLastColumn = ExcelUtils.ConvertNegativeRangeEndColumn(destStartColumn, destObj.Value<string>("lastColumn"));
                var destLastRow = ExcelUtils.ConvertNegativeRangeEndRow(destStartRow, destObj.Value<int>("lastRow"));
                destRefr = $"{destStartColumn}{destStartRow}:{destLastColumn}{destLastRow}";
            }

            if (!TryParseRange(_sheet, destRefr, out object destRange))
                throw new ExcelException("填充区域信息填写无效".I18n("SheetEdit_3E9BC490"));

            string destRangeAddress = Convert.ToString(ComLateBinding.GetProp(destRange, "Address"));
            var destAddress = Regex.Match((destRangeAddress.Contains(":") ? destRangeAddress : $"{destRangeAddress}:{destRangeAddress}"), @"^\$(\w+)\$(\d+):\$(\w+)\$(\d+)$");

            //address :A1B2  group[1]==A group[2]==1 group[3]==B group[4]==2
            // 上下填充 须满足 起始列、结束列 相同；源区域行数在 目标区域的起始行和结束行之间
            // 左右填充 须满足 起始行、结束行 相同；源区域的列在 目标区域的列之间
            bool? valid;
            if ((address.Groups[1].Value == destAddress.Groups[1].Value) && (address.Groups[3].Value == destAddress.Groups[3].Value))
            {
                //满足上下填充，起始列、结束列 相同
                int rangeRow = ComLateBinding.ToInt(ComLateBinding.GetProp(range, "Row"));
                if ((rangeRow == int.Parse(destAddress.Groups[2].Value) && int.Parse(destAddress.Groups[4].Value) > int.Parse(address.Groups[4].Value))
                    || (address.Groups[4].Value == destAddress.Groups[4].Value && int.Parse(destAddress.Groups[2].Value) < rangeRow))
                    valid = true;
                else
                    valid = false;
            }
            else if ((address.Groups[2].Value == destAddress.Groups[2].Value) && (address.Groups[4].Value == destAddress.Groups[4].Value))
            {
                //满足左右填充条件，起始行、结束行相同
                if ((destAddress.Groups[1].Value == address.Groups[1].Value && ExcelUtils.ColumnLetterToIndex(destAddress.Groups[3].Value) > ExcelUtils.ColumnLetterToIndex(address.Groups[3].Value))
                    ||
                   (destAddress.Groups[3].Value == address.Groups[3].Value && ExcelUtils.ColumnLetterToIndex(destAddress.Groups[1].Value) < ExcelUtils.ColumnLetterToIndex(address.Groups[1].Value)))
                    valid = true;
                else
                    valid = false;
            }
            else
            {
                throw new ExcelException("填充区域必须包含源区域且只能向一个方向填充".I18n("SheetEdit_6CB0AE5D"));
            }

            if (!valid.Value)
                throw new ExcelException("填充区域必须大于源区域".I18n("SheetEdit_155922F4"));

            ComLateBinding.Invoke(range, "AutoFill", destRange, 2);

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
            var sourceObj = JObject.Parse(sourceRange);
            var sourceType = sourceObj.Value<int>("type");

            var sourceStartColumn = ExcelUtils.ConvertNegativeColumn(sourceObj.Value<string>("startColumn"));
            var sourceStartRow = ExcelUtils.ConvertNegativeRow(sourceObj.Value<int>("startRow"));
            var sourceRefr = sourceType == 0
                ? $"{sourceStartColumn}{sourceStartRow}"
                : $"{sourceStartColumn}{sourceStartRow}:" +
                    $"{ExcelUtils.ConvertNegativeRangeEndColumn(sourceStartColumn, sourceObj.Value<string>("lastColumn"))}{ExcelUtils.ConvertNegativeRangeEndRow(sourceStartRow, sourceObj.Value<int>("lastRow"))}";

            AutoFill(workbook, sheet, sourceRefr, destination);
        }
        //区域的空格格式变更了
        void IExcel.AutoFillV2(string workbook, string sheet, string excelRng, string destination)
        {
            var sourceRefr = ExcelUtils.ConvertExcelRangeAddress(excelRng);
            AutoFill(workbook, sheet, sourceRefr, destination);

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
            object wb = GetWorkbook(workbook);
            try
            {
                object slicerCache = null;
                object slicerCaches = ComLateBinding.GetProp(wb, "SlicerCaches");
                foreach (object cache in ComLateBinding.ToEnumerable(slicerCaches))
                {
                    object slicers = ComLateBinding.GetProp(cache, "Slicers");
                    foreach (object sc in ComLateBinding.ToEnumerable(slicers))
                    {
                        if (Convert.ToString(ComLateBinding.GetProp(sc, "Name")).Equals(slicerName))
                        {
                            slicerCache = cache;
                            break;
                        }
                    }

                }
                if (slicerCache == null)
                {
                    throw new ExcelException(Localize($"未找到名称为{slicerName}的切片器", "SheetEdit_DD7BA021"));
                }
                if (string.IsNullOrEmpty(slicerOptions))
                {
                    //实现全部选中
                    ComLateBinding.Invoke(slicerCache, "ClearAllFilters");
                }
                else
                {
                    var options = ExcelUtils.ConvertStrToArray(slicerOptions);
                    object slicerItems = ComLateBinding.GetProp(slicerCache, "SlicerItems");
                    foreach (object item in ComLateBinding.ToEnumerable(slicerItems))
                    {
                        ComLateBinding.SetProp(item, "Selected", options.Any(o => o?.ToString() == Convert.ToString(ComLateBinding.GetProp(item, "Name"))));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new ExcelException(Localize($"设置切片器失败：{ex.Message}", "SheetEdit_7FDF99E9"));
            }
        }

        /// <summary>
        /// Excel 筛选
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="rule">筛选规则</param>
        ///<param name="startRow">起始行</param>
        ///<param name="endRow">结束行</param>
        void IExcel.ExcelFilter(string workbook, string sheet, string rule, int startRow, int endRow)
        {
            object worksheet = GetWorksheetByName(workbook, sheet);
            object filterObj = null;
            object listObj = null;
            try
            {
                var filterRule = JsonConvert.DeserializeObject<AutoFilterRule>(rule);
                Instance.EnableExcelFilter(workbook, sheet);
                if (filterRule.InTable == 0)
                {
                    filterObj = ComLateBinding.GetProp(worksheet, "Range", $"{filterRule.StartColumn}{startRow}:{filterRule.EndColumn}{endRow}");
                }
                else
                {
                    object listObjects = ComLateBinding.GetProp(worksheet, "ListObjects");
                    listObj = ComLateBinding.GetItem(listObjects, filterRule.InTable);
                    filterObj = ComLateBinding.GetProp(listObj, "Range");
                }
                int field = 0;
                foreach (var item in filterRule.Filters)
                {
                    field++;
                    if (item.On == false)
                    {
                        //没有条件，只显示下拉标识
                        ComLateBinding.Invoke(filterObj, "AutoFilter", field);
                        continue;
                    }
                    if (item.Criteria2 == null)
                    {
                        if (item.Operator == 7)
                        {
                            string[] c1 = null;
                            if (item.Criteria1 is JArray)
                            {
                                c1 = item.Criteria1.ToObject<string[]>();
                            }
                            else
                            {
                                c1 = new string[1] { item.Criteria1 };
                            }
                            ComLateBinding.Invoke(filterObj, "AutoFilter", field, c1, 7);
                        }
                        else if (item.Operator < 3)
                        {
                            ComLateBinding.Invoke(filterObj, "AutoFilter", field, item.Criteria1);
                        }
                        else if (item.Operator < 7)
                        {
                            int c1 = 10;
                            if (int.TryParse(item.Criteria1?.ToString(), out int _c1))
                            {
                                c1 = _c1;
                            }
                            ComLateBinding.Invoke(filterObj, "AutoFilter", field, c1, item.Operator);
                        }
                        else
                        {
                            int _c1, _operator = 11;
                            if (int.TryParse(item.Criteria1?.ToString(), out _c1) && int.TryParse(item.Operator?.ToString(), out _operator))
                            {
                                ComLateBinding.Invoke(filterObj, "AutoFilter", field, _c1, _operator);
                            }
                            else
                            {
                                throw new Exception("筛选条件格式不正确".I18n("SheetEdit_A462F4FB"));
                            }

                        }
                    }
                    else
                    {
                        if (item.Operator == 7)
                        {
                            string[] c1 = new string[2] { item.Criteria1, item.Criteria2 };
                            ComLateBinding.Invoke(filterObj, "AutoFilter", field, c1, 7);
                        }
                        else
                        {
                            ComLateBinding.Invoke(filterObj, "AutoFilter", field, item.Criteria1, item.Operator, item.Criteria2);
                        }
                    }
                }
                object af = null;
                if (filterRule.InTable == 0)
                {
                    af = ComLateBinding.GetProp(worksheet, "AutoFilter");
                }
                else if (listObj != null)
                {
                    af = ComLateBinding.GetProp(listObj, "AutoFilter");
                }

                if (af != null)
                {
                    try
                    {
                        object sort = ComLateBinding.GetProp(af, "Sort");
                        SetExcelProperty(sort, "SortMethod", filterRule.SortMethod);
                    }
                    catch { /* 某些 Excel 版本无 Sort 对象时忽略 */ }
                }

                if (filterRule.InTable != 0)
                {
                    object parent = ComLateBinding.GetProp(worksheet, "Parent");
                    object slicerCaches = ComLateBinding.GetProp(parent, "SlicerCaches");
                    foreach (object slicerCache in ComLateBinding.ToEnumerable(slicerCaches))
                    {
                        try
                        {
                            if (Convert.ToString(ComLateBinding.GetProp(slicerCache, "SourceName")) == Convert.ToString(ComLateBinding.GetProp(listObj, "Name")))
                            {
                                ComLateBinding.Invoke(slicerCache, "ClearManualFilter");
                                ComLateBinding.Invoke(slicerCache, "Refresh");
                            }
                        }
                        catch { /* ignore*/ }
                    }
                }
            }
            catch (Newtonsoft.Json.JsonException je)
            {
                logger.LogError(je);
                throw new ExcelException("筛选规则解析出错".I18n("SheetEdit_3DC3AEFE"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new ExcelException(Localize($"筛选条件格式不正确，Office错误消息：{ex.Message}", "SheetEdit_EE392786"));
            }
            finally
            {
                if (filterObj != null)
                {
                    ReleaseComObject(filterObj);
                }
                if (listObj != null)
                {
                    ReleaseComObject(listObj);
                }
                if (worksheet != null)
                {
                    ReleaseComObject(worksheet);
                }
            }
        }

        /// <summary>
        /// 取消Excel筛选状态
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheet"></param>
        void IExcel.EnableExcelFilter(string workbook, string sheet)
        {
            object worksheet = GetWorksheetByName(workbook, sheet);
            try
            {
                if (ComLateBinding.ToBool(ComLateBinding.GetProp(worksheet, "AutoFilterMode")))
                {
                    ComLateBinding.SetProp(worksheet, "AutoFilterMode", false);
                }
                object usedRange = ComLateBinding.GetProp(worksheet, "UsedRange");
                object listObject = ComLateBinding.GetProp(usedRange, "ListObject");
                if (listObject != null)
                {
                    ComLateBinding.SetProp(listObject, "ShowAutoFilter", false);
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex);

            }

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
            try
            {
                object worksheet = GetWorksheetByName(workbook, sheet);
                bool autoFilterMode = ComLateBinding.ToBool(ComLateBinding.GetProp(worksheet, "AutoFilterMode"));
                object usedRange = ComLateBinding.GetProp(worksheet, "UsedRange");
                object listObject = ComLateBinding.GetProp(usedRange, "ListObject");
                if (autoFilterMode || listObject != null)
                {
                    if (option == 0)
                    {
                        bool clearSuccess = false;
                        object filterRange = autoFilterMode
                            ? ComLateBinding.GetProp(ComLateBinding.GetProp(worksheet, "AutoFilter"), "Range")
                            : ComLateBinding.GetProp(ComLateBinding.GetProp(listObject, "AutoFilter"), "Range");
                        object filterColumns = ComLateBinding.GetProp(filterRange, "Columns");
                        int filterColumnCount = ComLateBinding.ToInt(ComLateBinding.GetProp(filterColumns, "Count"));
                        int firstColumn = ComLateBinding.ToInt(ComLateBinding.GetProp(filterRange, "Column"));
                        for (int i = 0; i < filterColumnCount; i++)
                        {
                            if (ExcelUtils.ColumnIndexToLetter(i + firstColumn) == column)
                            {
                                ComLateBinding.Invoke(filterRange, "AutoFilter", i + 1);
                                clearSuccess = true;
                            }
                        }
                        if (!clearSuccess)
                        {
                            throw new ExcelException(Localize($"列 {column} 不在筛选范围内", "SheetEdit_7C1BE1E7"));
                        }
                    }
                    else
                    {
                        if (listObject != null)
                        {
                            object autoFilter = ComLateBinding.GetProp(listObject, "AutoFilter");
                            ComLateBinding.Invoke(autoFilter, "ShowAllData");
                        }
                        if (autoFilterMode)
                        {
                            ComLateBinding.Invoke(worksheet, "ShowAllData");
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex);
            }

        }

        /// <summary>
        /// 设置Excel表格状态
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="table">表格名</param>
        /// <param name="headerRow">标题行</param>
        /// <param name="totalRow">汇总行</param>      
        /// <param name="firstColumn">第一列</param>
        /// <param name="lastColumn">最后一列</param>      
        /// <param name="filterButton">筛选按钮</param>
        void IExcel.ShowTableStyle(string workbook, string sheet, string table, int headerRow, int totalRow, int firstColumn, int lastColumn, int filterButton)
        {
            try
            {
                object worksheet = GetWorksheetByName(workbook, sheet);
                object listObjects = ComLateBinding.GetProp(worksheet, "ListObjects");
                object listObject = ComLateBinding.GetItem(listObjects, table);
                SetExcelProperty(listObject, "ShowHeaders", headerRow == 1);
                SetExcelProperty(listObject, "ShowTotals", totalRow == 1);
                SetExcelProperty(listObject, "ShowTableStyleFirstColumn", firstColumn == 1);
                SetExcelProperty(listObject, "ShowTableStyleLastColumn", lastColumn == 1);
                SetExcelProperty(listObject, "ShowAutoFilter", filterButton == 1);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new ExcelException("设置表格显示样式失败".I18n("SheetEdit_5618C371"));
            }

        }

        /// <summary>
        /// 刷新表格数据
        /// </summary>
        /// <param name="workbook"></param>
        void IExcel.RefreshAll(string workbook)
        {
            try
            {
                ComLateBinding.Invoke(GetWorkbook(workbook), "RefreshAll");
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }

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
            object worksheet = GetWorksheetByName(workbook, sheet);
            object range = null;
            try
            {
                if (groupType == 0)
                {
                    range = ComLateBinding.GetProp(worksheet, "Range", $"{startRow}:{endRow}");
                }
                else
                {
                    range = ComLateBinding.GetProp(worksheet, "Range", $"{startColumn}:{endColumn}");
                }
                if (group == 0)
                {
                    ComLateBinding.Invoke(range, "Group");
                }
                else
                {
                    ComLateBinding.Invoke(range, "Ungroup");
                }
            }
            catch (Exception ex)
            {
                ExcelPlugin.logger.LogError($"设置组合失败：{ex?.Message ?? string.Empty} | 内部异常：{ex?.InnerException?.Message ?? string.Empty}");
                throw new ExcelException(Localize($"设置组合失败：{ex.Message}", "SheetEdit_D831EA15"));
            }
            finally
            {
                if (worksheet != null)
                {
                    ReleaseComObject(worksheet);
                }
                if (range != null)
                {
                    ReleaseComObject(range);
                }
            }

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
            object worksheet = null;
            object mergeRange = null;
            try
            {
                worksheet = GetWorksheetByName(workbook, sheet);
                mergeRange = ComLateBinding.GetProp(worksheet, "Range", rangeObj);
                ComLateBinding.SetProp(mergeRange, "MergeCells", merge == 0);
            }
            catch (Exception ex)
            {
                throw new ExcelException(ex.Message);
            }
            finally
            {
                if (worksheet != null)
                {
                    ReleaseComObject(worksheet);
                }
                if (mergeRange != null)
                {
                    ReleaseComObject(mergeRange);
                }
            }
        }
    }
}
