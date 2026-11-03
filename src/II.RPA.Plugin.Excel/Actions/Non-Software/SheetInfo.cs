using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using DevExpress.Spreadsheet;
using DevExpress.Utils.Extensions;
using II.RPA.Plugin.Excel.Utils;
using II.RPA.Plugin.Excel.Config;
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
    /// <summary>
    /// 不使用软件自动化
    /// 设计器.信息读取类组件
    /// </summary>
    public partial class NonSoftware : IExcel
    {

        /// <summary>
        /// 读取单元格
        /// </summary>       
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>  
        /// <param name="column">列名</param>  
        /// <param name="row">行号</param>  
        /// <param name="valueType">值类型</param>
        void IExcel.ReadCell(string workbook, string sheet, string column, string row, int valueType, bool visible)
        {
            var _sheet = GetWorksheetByName(workbook, sheet);
            var _cell = _sheet.Cells[$"{column}{row}"];
            _cell.Calculate();
            if (visible && !_sheet.Rows[row].Visible)
            {
                ContextInstance.ActionContext.AddResult("strCell", "");
            }
            else if (valueType == 1)
            {
                //705: 处理表格自定义日期格式改动
                //if (_cell.Value.DateTimeValue != default)
                //{
                //    ContextInstance.ActionContext.AddResult("strCell", _cell.Value.DateTimeValue.ToString());
                //}
                //else
                //{
                ContextInstance.ActionContext.AddResult("strCell", _cell.DisplayText);
                //}
            }
            else
            {
                ContextInstance.ActionContext.AddResult("strCell", _cell.Value.GetValue(valueType).ToString());
            }
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
            var _sheet = GetWorksheetByName(workbook, sheet);
            _sheet.Cells[$"{column}{row}"].Calculate();
            var cell = _sheet.Cells[$"{column}{row}"];
            string formula = string.Empty;
            if (cell.HasFormula)
            {
                formula = cell.Formula;
            }
            ContextInstance.ActionContext.AddResult("strFormula", formula);

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
            var _sheet = GetWorksheetByName(workbook, sheet);
            _sheet.Cells[$"{column}{row}"].Calculate();
            System.Drawing.Color color = _sheet.Cells[$"{column}{row}"].FillColor;
            if (format == 0)
            {
                ContextInstance.ActionContext.AddResult(new JProperty("strColor", new int[3] { color.R, color.G, color.B }));
            }
            else
            {
                ContextInstance.ActionContext.AddResult("strColor", $"#{color.R:X2}{color.G:X2}{color.B:X2}");
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
            //普通模式
            if (type != 1)
            {
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
            var rangeObj = GetRange(workbook, sheet, startRow, startColumn, lastRow, lastColumn, type, range);
            rangeObj.Calculate();
            GetRangeValue(rangeObj, valueType, visible);
            ContextInstance.ActionContext.AddResult(ExcelUtils.TEMP_FILEPATH);
        }

        void IExcel.ReadRangeV2(string workbook, string sheet, string range, int valueType, bool visible)
        {
            var rangeObj = ExcelUtils.ConvertToExcelRange(range);
            Instance.ReadRange(workbook, sheet, rangeObj.startRow, rangeObj.startColumn, rangeObj.lastRow, rangeObj.lastColumn, rangeObj.type, rangeObj.range, valueType, visible);
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
            var _sheet = GetWorksheetByName(workbook, sheet);
            var usedRange = _sheet.GetDataRange();
            object[] rowVal = null;
            if (visible && !_sheet.Rows[rowIndex - 1].Visible)
            {
                rowVal = new object[0];
            }
            else
            {
                var rowValues = new List<object>();
                for (int i = 0; i < usedRange.RightColumnIndex + 1; i++)
                {
                    if (visible && !_sheet.Columns[i].Visible)
                    {
                        continue;
                    }
                    _sheet.Cells[rowIndex - 1, i].Calculate();
                    if (valueType == 1)
                        rowValues.Add(_sheet.Cells[rowIndex - 1, i].DisplayText);
                    else
                        rowValues.Add(_sheet.Cells[rowIndex - 1, i].Value.GetValue(valueType));
                }
                rowVal = rowValues.ToArray();
                ContextInstance.ActionContext["__VALUE__"] = rowVal;
            }

            ExcelUtils.SerializerDataToBinaryFile(rowVal);
            ContextInstance.ActionContext.AddResult(ExcelUtils.TEMP_FILEPATH);
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
            int batchSize = ExcelConfigurations.Info.GlobalSetting.BatchSize;
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            var values = new List<object>(batchSize);
            foreach (object value in ReadColumnValues(workbook, sheet, columnName, valueType, visible))
            {
                values.Add(value);
                if (values.Count == batchSize)
                {
                    ExcelUtils.SerializerDataToBinaryFile(values.ToArray());
                    values.Clear();
                }
            }
            if (values.Count > 0 || ExcelUtils.TEMP_FILEPATH == null)
                ExcelUtils.SerializerDataToBinaryFile(values.ToArray());
            ContextInstance.ActionContext.AddResult(ExcelUtils.TEMP_FILEPATH);
        }

        private IEnumerable<object> ReadColumnValues(string workbook, string sheet, string columnName,
            int valueType = 0, bool visible = false)
        {
            var worksheet = GetWorksheetByName(workbook, sheet);
            int column = ExcelUtils.ColumnLetterToIndex(columnName) - 1;
            if (visible && !worksheet.Columns[columnName].Visible) yield break;
            int lastRow = worksheet.GetDataRange().BottomRowIndex;
            for (int r = 0; r <= lastRow; r++)
            {
                if (visible && !worksheet.Rows[r].Visible) continue;
                var cell = worksheet.Cells[r, column];
                cell.Calculate();
                yield return valueType == 1 ? (object)cell.DisplayText : GetCellValue(cell.Value);
            }
        }

        private dynamic GetCellValue(CellValue cell)
        {
            return (cell == null) ? "" : cell.GetValue();
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
            string numRet;
            var _sheet = GetWorksheetByName(workbook, sheet);
            //只获取显示的行
            bool ignoreHidden = Convert.ToBoolean(ActionContext["ignoreHidden"]);
            if (range.Equals("指定一列"))
            {
                numRet = _sheet.Columns[column].Count(ignoreHidden, false).ToString();
            }
            else
            {
                List<int> rows = new List<int>();
                var dataRng = _sheet.GetDataRange();
                for (int i = dataRng.LeftColumnIndex; i <= dataRng.RightColumnIndex; i++)
                {
                    rows.Add(_sheet.Columns[i].Count(ignoreHidden, false));
                }
                numRet = rows.Max().ToString();
            }
            ContextInstance.ActionContext.AddResult("numRet", numRet);
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
            string numRet;
            var _sheet = GetWorksheetByName(workbook, sheet);
            var dataRng = _sheet.GetDataRange();
            //empty sheet
            if (dataRng.LeftColumnIndex == 0 && dataRng.RightColumnIndex == 0 && dataRng.TopRowIndex == 0 && dataRng.BottomRowIndex == 0 && _sheet.Columns[0].Count(false, false) == 0)
            {
                numRet = "0";
            }
            else
            {
                //只获取显示的列
                bool ignoreHidden = Convert.ToBoolean(ActionContext["ignoreHidden"]);
                if (range.Equals("指定一行"))
                {
                    numRet = (_sheet.GetDataRange().LeftColumnIndex + _sheet.Rows[row.ToString()].Count(false, ignoreHidden)).ToString();
                }
                else
                {
                    var count = _sheet.GetDataRange().ColumnCount;
                    if (ignoreHidden)
                    {
                        for (int i = dataRng.LeftColumnIndex; i <= dataRng.RightColumnIndex; i++)
                        {
                            if (!_sheet.Columns[i].Visible)
                            {
                                count = count - 1;
                            }
                        }
                    }
                    numRet = (dataRng.LeftColumnIndex + count).ToString();
                }
            }
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
            dynamic firstCell;
            if (range.Equals("指定一行"))
            {
                Instance.ReadRow(workbook, sheet, row);
                var _firstIndex = ExcelUtils.FindValueInArray(ContextInstance.ActionContext["__VALUE__"], ExcelUtils.IsNullOrEmptyPredicate);
                firstCell = ExcelUtils.ColumnIndexToLetter(_firstIndex == -1 ? (ContextInstance.ActionContext["__VALUE__"].Length + 1) : _firstIndex + 1);
            }
            else
            {
                firstCell = ExcelUtils.ScanEmptyCell(ReadColumnValues(workbook, sheet, column), false);
            }
            ContextInstance.ActionContext.Result.RemoveAll();
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
                lastCell = ExcelUtils.ScanEmptyCell(ReadColumnValues(workbook, sheet, column), true).ToString();
            }
            ContextInstance.ActionContext.Result.RemoveAll();
            ContextInstance.ActionContext.AddResult("lastCell", lastCell);
        }

        /// <summary>
        /// 获取已编辑区域
        /// </summary>
        /// <param name="workbook">文件对象</param>  
        /// <param name="sheet">Sheet名称</param>       
        void IExcel.GetEditRange(string workbook, string sheet)
        {
            string startColumn, endColumn;
            int startRowNum, endRowNum;
            var usedRange = GetWorksheetByName(workbook, sheet).GetDataRange();
            startRowNum = usedRange.TopRowIndex + 1;
            startColumn = ExcelUtils.ColumnIndexToLetter(usedRange.LeftColumnIndex + 1);
            endRowNum = usedRange.BottomRowIndex + 1;
            endColumn = ExcelUtils.ColumnIndexToLetter(usedRange.RightColumnIndex + 1);
            ContextInstance.ActionContext.AddResult("startRowNum", startRowNum);
            ContextInstance.ActionContext.AddResult("startColumn", startColumn);
            ContextInstance.ActionContext.AddResult("endRowNum", endRowNum);
            ContextInstance.ActionContext.AddResult("endColumn", endColumn);
            logger.LogInformation($"Sheet Edit Range:{startColumn}{startRowNum}:{endColumn}{endRowNum}");
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
            var _sheet = GetWorksheetByName(workbook, sheet);
            StringBuilder retBuilder = new StringBuilder();
            CellRange cellRange, usedRange = _sheet.GetUsedRange();
            switch (option)
            {
                case "区域":
                    {
                        var rangeInfo = ExcelUtils.ConvertToExcelRange(range);
                        cellRange = GetRange(workbook, sheet, rangeInfo.startRow, rangeInfo.startColumn, rangeInfo.lastRow, rangeInfo.lastColumn, rangeInfo.type, rangeInfo.range);
                        break;
                    }
                case "行":
                    var __row = ExcelUtils.ConvertNegativeRow(range);
                    cellRange = _sheet[$"A{__row}:{ExcelUtils.ColumnIndexToLetter(usedRange.RightColumnIndex + 1)}{__row}"];
                    break;
                case "列":
                    var __column = ExcelUtils.ConvertNegativeColumn(range);
                    cellRange = _sheet[$"{__column}1:{__column}{usedRange.BottomRowIndex + 1}"];
                    break;
                case "已编辑区域":
                default:
                    cellRange = usedRange; break;
            }
            retBuilder.Append("[");
            var retSet = new List<string>();
            foreach (Cell cell in cellRange)
            {
                var cellValue = cell.Value.GetValue().ToString();
                if (cellValue != string.Empty && string.IsNullOrEmpty(input))
                {
                    continue;
                }
                if ((wholeWord && cellValue.Equals(input)) || (!wholeWord && cellValue.Contains(input)))
                {
                    retSet.Add(outType == 0 ? $"['{ExcelUtils.ColumnIndexToLetter(cell.ColumnIndex + 1)}',{cell.RowIndex + 1}]" : $"'{ExcelUtils.ColumnIndexToLetter(cell.ColumnIndex + 1)}{cell.RowIndex + 1}'");
                    if (mode == "查找并替换")
                    {
                        if (string.IsNullOrWhiteSpace(input) && string.IsNullOrWhiteSpace(cellValue))
                        {
                            cell.Value = (replace ?? "");
                        }
                        else
                        {
                            cell.Value = (cellValue.Replace(input, replace));
                        }
                    }
                    //only match first
                    if (matchRange.Equals("第一个"))
                        break;
                }
            }
            retBuilder.Append($"{string.Join(",", retSet)}]");
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
            ThrowExportRangeAsPictureNotSupported();
        }

        void IExcel.ExportRangeAsPictureV2(string workbook, string sheet, string range, string path, string name, bool cover)
        {
            ThrowExportRangeAsPictureNotSupported();
        }

        private static void ThrowExportRangeAsPictureNotSupported()
        {
            throw new ExcelException("【导出区域为图片】组件需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetInfo_68DD86C6"));
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
        /// <param name="matchFirst">查找全部</param>
        /// <param name="returnType">返回值类型（行号0，位置1）</param>
        void IExcel.Find(string workbook, string sheet, int mode, int option, string range, int row, string column, string what, int lookAt, int lookIn, int searchOrder, int searchDirection, int matchCase, int matchByte, string matchFormat, string replacement, string replaceFormat, int matchFirst, int returnType)
        {
            throw new ExcelException("【Excel查找/替换】组件需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetInfo_F3FB7996"));
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
            dynamic[] loopKeys = null;
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
                loopKeys = new dynamic[Math.Abs(end_col_index - start_col_index) + 1];
                var _startKey = start_col_index > end_col_index ? end_col_index : start_col_index;
                for (int i = 0; i < loopKeys.Length; i++)
                {
                    loopKeys[i] = ExcelUtils.ColumnIndexToLetter(_startKey + i);
                }
            }
            else if (regionType == 4)
            {
                loopKeys = new dynamic[Math.Abs(editRange.Item4 - editRange.Item2) + 1];
                var _startKey = editRange.Item2 > editRange.Item4 ? editRange.Item4 : editRange.Item2;
                for (int i = 0; i < loopKeys.Length; i++)
                {
                    loopKeys[i] = _startKey + i;
                }
            }
            else
            {
                loopKeys = new dynamic[Math.Abs(endRow - startRow) + 1];
                var _startKey = endRow > startRow ? startRow : endRow;
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
        /// 导出表格指定行列中图片
        /// </summary>       
        /// <param name="workbook">工作簿</param>
        /// <param name="sheet">工作表</param>
        /// <param name="path">保存的文件路径</param>
        /// <param name="range">指定行号 1 或列名“A”</param>
        void IExcel.GetShapes(string workbook, string sheet, string path, string range)
        {
            string dicShapes;
            var _sheet = GetWorksheetByName(workbook, sheet);
            List<string> shapes = new List<string>();
            foreach (var picture in _sheet.Pictures)
            {
                var fileName = System.IO.Path.Combine(path, picture.Name.Replace(" ", "") + ".jpg");
                if (int.TryParse(range, out int row))
                {
                    if (picture.TopLeftCell.RowIndex == row - 1)
                    {
                        picture.ExportToImage().NativeImage.Save(fileName);
                        shapes.Add(fileName);
                    }
                }
                else
                {
                    if (picture.TopLeftCell.ColumnIndex + 1 == ExcelUtils.ColumnLetterToIndex(range))
                    {
                        picture.ExportToImage().NativeImage.Save(fileName);
                        shapes.Add(fileName);
                    }

                }
            }
            dicShapes = JsonConvert.SerializeObject(shapes);
            ContextInstance.ActionContext.AddResult("dicShapes", dicShapes);

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
            throw new ExcelException("【Excel动态调用】组件需要依赖 Microsoft Excel 或 WPS软件".I18n("SheetInfo_74868DCF"));
        }
    }
}
