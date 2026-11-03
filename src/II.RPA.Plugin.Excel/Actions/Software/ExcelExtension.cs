using II.RPA.Plugin.Excel.Config;
using II.RPA.Plugin.Excel.Models;
using II.RPA.Plugin.Excel.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;

namespace II.RPA.Plugin.Excel.Actions.Software
{
    /// <summary>
    /// Excel公用的方法
    /// </summary>
    public partial class SoftwareExcel
    {
        public void ReleaseComObject(object o)
        {
            if (o != null)
            {
                try
                {
                    Marshal.ReleaseComObject(o);
                }
                catch
                {
                }
                finally
                {
                    o = null;
                }
            }
        }
        private void DeleteSheetSilently(object worksheet)
        {
            if (worksheet == null)
            {
                return;
            }

            object app = null;
            bool restoreDisplayAlerts = false;
            bool originalDisplayAlerts = false;

            try
            {
                app = ComLateBinding.GetProp(worksheet, "Application");
                if (app != null)
                {
                    try
                    {
                        originalDisplayAlerts = ComLateBinding.ToBool(ComLateBinding.GetProp(app, "DisplayAlerts"));
                        restoreDisplayAlerts = true;
                    }
                    catch
                    {
                    }

                    ComLateBinding.SetProp(app, "DisplayAlerts", false);
                }

                ComLateBinding.SetProp(worksheet, "Visible", -1);
                ComLateBinding.Invoke(worksheet, "Delete");
            }
            finally
            {
                if (restoreDisplayAlerts && app != null)
                {
                    try
                    {
                        ComLateBinding.SetProp(app, "DisplayAlerts", originalDisplayAlerts);
                    }
                    catch
                    {
                    }
                }
            }
        }
        private void RegisterExcelEvent(object obj, string eventName, string method)
        {
            try
            {
                var eventInfo = obj.GetType().GetEvent(eventName);
                MethodInfo methodInfo = typeof(SoftwareExcel).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
                Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, methodInfo);
                eventInfo.RemoveEventHandler(obj, handler);
                eventInfo.AddEventHandler(obj, handler);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"register excel {eventName} error!");
            }

        }
        private object GetExcelPropertyRecursion(object field, string[] propertys, int level)
        {
            try
            {
                if (propertys.Length > level)
                {
                    object _value = GetExcelProperty(field, propertys[level]);
                    if (_value == null)
                    {
                        return "";
                    }
                    else if (_value is string)
                    {
                        return _value;
                    }
                    else if (_value is bool)
                    {
                        return Convert.ToBoolean(_value) ? 1 : 0;
                    }
                    else if (_value.GetType().IsValueType)
                    {
                        return _value;
                    }
                    else
                    {
                        var count = GetExcelProperty(_value, "Count");
                        if (count is int c)
                        {
                            object[] retValus = new object[c];
                            int index = 0;
                            foreach (object _item in (System.Collections.IEnumerable)_value)
                            {
                                var subValue = GetExcelPropertyRecursion(_item, propertys, level + 1);
                                retValus[index++] = subValue;
                                ReleaseComObject(_item);
                            }
                            return retValus;
                        }
                        else
                        {
                            return GetExcelPropertyRecursion(_value, propertys, level + 1);
                        }
                    }

                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"get excel object property exception:{ex},propertys {propertys[level]}");
                return "";
            }

        }
        private void SetExcelPropertyRecursion(object field, string[] propertys, int level, object value)
        {
            try
            {
                if (propertys.Length == level)
                {
                    SetExcelProperty(field, propertys.Last(), value);
                }
                else
                {
                    object obj = GetExcelProperty(field, propertys[level - 1]);
                    SetExcelPropertyRecursion(obj, propertys, level + 1, value);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"get excel object property exception:{ex},propertys {propertys[level]}");
                return;
            }

        }
        private object GetExcelProperty(object field, string name)
        {
            try
            {
                var value = ComLateBinding.GetProp(field, name);
                logger.LogInformation($"get excel object property:{name},{value}");
                return value;
            }
            catch (Exception ex)
            {
                logger.LogError($"get excel object property exception:{ex}");
                return null;
            }

        }
        private void SetExcelProperty(object field, string name, object value)
        {
            try
            {
                if (value == null || value.ToString() == "")
                {
                    return;
                }
                object propertyValue = value.Equals("null") ? Type.Missing : value;
                ComLateBinding.SetProp(field, name.Trim(), propertyValue);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"set excel property {name} exception: {ex}");
            }

        }
        private object InvokeExcelMethod(object field, string name)
        {
            return InvokeExcelMethod(field, name, new object[0]);
        }
        private object InvokeExcelMethod(object field, string name, string value)
        {
            try
            {
                object[] args;
                if (value == null)
                {
                    args = new object[0];
                }
                else
                {
                    args = value.Trim(new char[] { '[', ']' }).Split(',');
                }

                return InvokeExcelMethod(field, name, args);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"call excel method, name:{name},value:{value}");
                throw ex;
            }

        }
        private object InvokeExcelMethod(object field, string name, object[] param)
        {
            try
            {
                return ComLateBinding.Invoke(field, name, param);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"call excel method, name:{name},parm lenth:{param.Length}");
                throw ex;
            }

        }
        private object GetExcelItem(object @object, int ordinal)
        {
            try
            {
                return ComLateBinding.GetItem(@object, ordinal);
            }
            catch
            {
                return @object.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, @object, new object[1] { ordinal }, new CultureInfo("en-US"));
            }
        }
        private object GetExcelItemByName(object obj, string name)
        {
            try
            {
                return ComLateBinding.GetItem(obj, name);
            }
            catch
            {
                return obj.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, obj, new object[1] { name }, new CultureInfo("en-US"));
            }
        }
        /// 获取区域显示的值       
        public string GetRangeText(object range, int mode = 1)
        {
            try
            {
                using (var com = new ComScope())
                {
                    if (mode == 1)//读取行值
                    {
                        object columnsObj = com.Track(ComLateBinding.GetProp(range, "Columns"));
                        int columnsCount = ComLateBinding.ToInt(ComLateBinding.GetProp(columnsObj, "Count"));
                        var rangeValue = new object[columnsCount];
                        for (int c = 0; c < columnsCount; c++)
                        {
                            rangeValue[c] = GetCellText(com.Track(ComLateBinding.GetProp(range, "Cells", 1, c + 1)));
                        }
                        ExcelUtils.SerializerDataToBinaryFile(rangeValue);
                    }
                    else if (mode == 2)//读取列值
                    {
                        object rowsObj = com.Track(ComLateBinding.GetProp(range, "Rows"));
                        int rowsCount = ComLateBinding.ToInt(ComLateBinding.GetProp(rowsObj, "Count"));
                        if (!ExcelUtils.StreamingEnabled)
                        {
                            var values = new object[rowsCount];
                            for (int r = 0; r < rowsCount; r++)
                                values[r] = GetCellText(com.Track(ComLateBinding.GetProp(range, "Cells", r + 1, 1)));
                            ExcelUtils.SerializerDataToBinaryFile(values);
                            return ExcelUtils.TEMP_FILEPATH;
                        }
                        int batchSize = ExcelConfigurations.Info.GlobalSetting.BatchSize;
                        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
                        for (int start = 0; start < rowsCount; start += batchSize)
                        {
                            var rangeValue = new object[Math.Min(batchSize, rowsCount - start)];
                            using (var blockScope = new ComScope())
                            {
                                for (int r = 0; r < rangeValue.Length; r++)
                                    rangeValue[r] = GetCellText(blockScope.Track(ComLateBinding.GetProp(range, "Cells", start + r + 1, 1)));
                            }
                            ExcelUtils.SerializerDataToBinaryFile(rangeValue);
                        }
                        if (rowsCount == 0) ExcelUtils.SerializerDataToBinaryFile(new object[0]);
                    }
                    else
                    {
                        object rowsObj = com.Track(ComLateBinding.GetProp(range, "Rows"));
                        object columnsObj = com.Track(ComLateBinding.GetProp(range, "Columns"));
                        int totalRows = ComLateBinding.ToInt(ComLateBinding.GetProp(rowsObj, "Count"));
                        int columns = ComLateBinding.ToInt(ComLateBinding.GetProp(columnsObj, "Count"));
                        int totalTasks = totalRows / ExcelConfigurations.Info.GlobalSetting.BatchSize + (totalRows % ExcelConfigurations.Info.GlobalSetting.BatchSize == 0 ? 0 : 1);


                        if (totalTasks < 2)
                        {
                            var pool = ArrayPool<object>.Shared;
                            var array = pool.Rent(totalRows * columns);
                            for (int r = 0; r < totalRows; r++)
                            {
                                for (int c = 0; c < columns; c++)
                                {
                                    array[r * columns + c] = GetCellText(com.Track(ComLateBinding.GetProp(range, "Cells", r + 1, c + 1)));
                                }
                            }
                            var data = new SerializeData
                            {
                                Rows = totalRows,
                                Columns = columns,
                                Data = array,
                            };
                            ExcelUtils.SerializerDataToBinaryFile(data);
                            pool.Return(array, true);
                            data = null;
                        }
                        else
                        {
                            var pool = ArrayPool<object>.Shared;
                            for (int i = 0; i < totalTasks; i++)
                            {
                                int _startRow = i * ExcelConfigurations.Info.GlobalSetting.BatchSize + 1;
                                int _endRow = Math.Min((i + 1) * ExcelConfigurations.Info.GlobalSetting.BatchSize, totalRows);
                                logger.LogInformation($"read content,start row num:{_startRow},end row num:{_endRow}");
                                int rowCount = _endRow - _startRow + 1;
                                var array = pool.Rent(rowCount * columns);
                                for (int r = 0; r < rowCount; r++)
                                {
                                    for (int c = 0; c < columns; c++)
                                    {
                                        array[r * columns + c] = GetCellText(com.Track(ComLateBinding.GetProp(range, "Cells", _startRow + r, c + 1)));
                                    }
                                }
                                var data = new SerializeData
                                {
                                    Rows = rowCount,
                                    Columns = columns,
                                    Data = array,
                                };
                                ExcelUtils.SerializerDataToBinaryFile(data, $"{i + 1}-{totalTasks}");
                                pool.Return(array, true);
                                data = null;
                            }
                        }
                    }
                }
                return ExcelUtils.TEMP_FILEPATH;
            }
            catch (OutOfMemoryException oe)
            {
                logger.LogError(oe.StackTrace);
                throw new ExcelException(Localize($"读取Excel内容出错：没有足够的内容用于读取数据", "ExcelExtension_2050F9C3"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                throw new ExcelException(Localize($"读取Excel内容出错：{ex.Message}", "ExcelExtension_F0553C52"));
            }

        }

        private string GetCellText(object _Range)
        {
            object value = _Range == null ? null : ComLateBinding.GetProp(_Range, "Value");
            return value == null ? "" : ComLateBinding.ToText(ComLateBinding.GetProp(_Range, "Text"));
        }
        /// 获取表格使用的最后一个单元格(列索引，行索引) <summary>
        /// 获取表格使用的最后一个单元格(列索引，行索引)
        /// </summary>
        /// <param name="worksheet"></param>
        /// <param name="usedRange"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        [Obsolete("710使用的方法，710 以后加了是否读取隐藏的选项")]
        public Tuple<int, int> GetLastCell_Old(object worksheet, object usedRange, int direction)
        {
            int endRowNum;
            int lastColumnIndex;
            try
            {
                var searchDirection = direction == 1 ? 1 : 2;
                using (var com = new ComScope())
                {
                    if (ComLateBinding.ToBool(ComLateBinding.GetProp(worksheet, "AutoFilterMode")))
                    {
                        //取消筛选
                        try
                        {
                            ComLateBinding.Invoke(worksheet, "ShowAllData");
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning($"get usedrange exception when cancel autofilter!,{ex}");
                        }


                        //获取最后有内容的行号
                        object _lastRow = com.Track(FindRangeCell(usedRange, searchDirection, 1));
                        endRowNum = _lastRow == null ? ComLateBinding.ToInt(ComLateBinding.GetProp(usedRange, "Row")) : ComLateBinding.ToInt(ComLateBinding.GetProp(_lastRow, "Row"));

                        //获取有内容得列索引
                        object _lastColumn = com.Track(FindRangeCell(usedRange, searchDirection, 2));

                        lastColumnIndex = _lastColumn == null ? 1 : ComLateBinding.ToInt(ComLateBinding.GetProp(_lastColumn, "Column"));

                        //取消筛选
                        try
                        {
                            ComLateBinding.Invoke(com.Track(ComLateBinding.GetProp(worksheet, "Application")), "Undo");
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning($"get usedrange exception when undo!,{ex}");
                        }
                    }
                    else if (ComLateBinding.GetProp(com.Track(ComLateBinding.GetProp(worksheet, "UsedRange")), "ListObject") != null)
                    {

                        //取消筛选
                        try
                        {
                            object usedRangeListObject = com.Track(ComLateBinding.GetProp(com.Track(ComLateBinding.GetProp(worksheet, "UsedRange")), "ListObject"));
                            ComLateBinding.SetProp(usedRangeListObject, "ShowAutoFilter", false);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning($"get usedrange exception when cancel listobject autofilter!,{ex}");
                        }

                        //获取最后有内容的行号
                        object _lastRow = com.Track(FindRangeCell(usedRange, searchDirection, 1));
                        endRowNum = _lastRow == null ? ComLateBinding.ToInt(ComLateBinding.GetProp(usedRange, "Row")) : ComLateBinding.ToInt(ComLateBinding.GetProp(_lastRow, "Row"));

                        //获取有内容得列索引
                        object _lastColumn = com.Track(FindRangeCell(usedRange, searchDirection, 2));

                        lastColumnIndex = _lastColumn == null ? 1 : ComLateBinding.ToInt(ComLateBinding.GetProp(_lastColumn, "Column"));

                        //取消筛选
                        try
                        {
                            ComLateBinding.Invoke(com.Track(ComLateBinding.GetProp(worksheet, "Application")), "Undo");
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning($"get usedrange exception when undo!,{ex}");
                        }
                    }
                    else
                    {
                        //获取最后有内容的行号
                        object _lastRow = com.Track(FindRangeCell(usedRange, searchDirection, 1));
                        endRowNum = _lastRow == null ? ComLateBinding.ToInt(ComLateBinding.GetProp(usedRange, "Row")) : ComLateBinding.ToInt(ComLateBinding.GetProp(_lastRow, "Row"));

                        //获取有内容得列索引
                        object _lastColumn = com.Track(FindRangeCell(usedRange, searchDirection, 2));

                        lastColumnIndex = _lastColumn == null ? 1 : ComLateBinding.ToInt(ComLateBinding.GetProp(_lastColumn, "Column"));
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogError($"get usedrange row error!:{ex}");
                //获取失败直接使用系统提供的
                using (var com = new ComScope())
                {
                    object rowsObj = com.Track(ComLateBinding.GetProp(usedRange, "Rows"));
                    object columnsObj = com.Track(ComLateBinding.GetProp(usedRange, "Columns"));
                    endRowNum = ComLateBinding.ToInt(ComLateBinding.GetProp(usedRange, "Row")) + ComLateBinding.ToInt(ComLateBinding.GetProp(rowsObj, "Count")) - 1;
                    lastColumnIndex = ComLateBinding.ToInt(ComLateBinding.GetProp(usedRange, "Column")) + ComLateBinding.ToInt(ComLateBinding.GetProp(columnsObj, "Count")) - 1;
                }
            }
            return new Tuple<int, int>(lastColumnIndex, endRowNum);
        }
        /// <summary>
        /// 获取最后一个有内容的单元格
        /// </summary>
        /// <param name="usedRange">查找的区域</param>
        /// <param name="order">1：行，2：列</param>
        /// <param name="direction">1：前，2：后</param>
        /// <returns></returns>
        public Tuple<int, int> GetLastCell(object cellRange, int searchOrder, int direction)
        {

            logger.LogDebug($"get last cell of range:{ComLateBinding.GetProp(cellRange, "Address")}");
            int endRowNum;
            int lastColumnIndex;
            object worksheet = null;
            try
            {
                var searchDirection = direction == 1 ? 1 : 2;

                using (var com = new ComScope())
                {
                    worksheet = com.Track(ComLateBinding.GetProp(cellRange, "Worksheet"));
                    object app = com.Track(ComLateBinding.GetProp(worksheet, "Application"));
                    ComLateBinding.Invoke(com.Track(ComLateBinding.GetProp(app, "FindFormat")), "Clear");
                    object rowsObj = com.Track(ComLateBinding.GetProp(cellRange, "Rows"));
                    object columnsObj = com.Track(ComLateBinding.GetProp(cellRange, "Columns"));
                    object startCell = searchDirection == 1
                        ? com.Track(ComLateBinding.GetProp(cellRange, "Cells", ComLateBinding.ToInt(ComLateBinding.GetProp(rowsObj, "Count")), ComLateBinding.ToInt(ComLateBinding.GetProp(columnsObj, "Count"))))
                        : com.Track(ComLateBinding.GetProp(cellRange, "Cells", 1, 1));
                    object findResult = com.Track(FindRangeCell(cellRange, searchDirection, searchOrder, startCell));

                    if (findResult == null)
                    {
                        endRowNum = ComLateBinding.ToInt(ComLateBinding.GetProp(cellRange, "Row"));
                        lastColumnIndex = 1;
                    }
                    else
                    {
                        endRowNum = ComLateBinding.ToInt(ComLateBinding.GetProp(findResult, "Row"));
                        lastColumnIndex = ComLateBinding.ToInt(ComLateBinding.GetProp(findResult, "Column"));
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogError($"GetLastCell error!:{ex}");
                //获取失败直接使用系统提供的
                using (var com = new ComScope())
                {
                    object rowsObj = com.Track(ComLateBinding.GetProp(cellRange, "Rows"));
                    object columnsObj = com.Track(ComLateBinding.GetProp(cellRange, "Columns"));
                    endRowNum = ComLateBinding.ToInt(ComLateBinding.GetProp(cellRange, "Row")) + ComLateBinding.ToInt(ComLateBinding.GetProp(rowsObj, "Count")) - 1;
                    lastColumnIndex = ComLateBinding.ToInt(ComLateBinding.GetProp(cellRange, "Column")) + ComLateBinding.ToInt(ComLateBinding.GetProp(columnsObj, "Count")) - 1;
                }
            }
            return new Tuple<int, int>(lastColumnIndex, endRowNum);
        }

        private object FindRangeCell(object range, int searchDirection, int searchOrder, object after = null)
        {
            return ComLateBinding.Invoke(
                range,
                "Find",
                "*",
                after ?? Type.Missing,
                -4123,
                2,
                searchOrder,
                searchDirection,
                Type.Missing,
                Type.Missing,
                Type.Missing);
        }

        private object GetRangeValueByType(object range, int valueType)
        {
            return ComLateBinding.GetProp(range, valueType == 1 ? "Text" : valueType == 2 ? "Value" : "Value2");
        }

        private object GetRange(string workbook, string sheet, int startRow, string startColumn, int lastRow, string lastColumn, int type, string range)
        {
            object _sheet = GetWorksheetByName(workbook, sheet);
            object rangeObj;
            try
            {
                if (type == 0)
                {
                    using (var com = new ComScope())
                    {
                        object cell1 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", startRow, startColumn));
                        object cell2 = com.Track(ComLateBinding.GetProp(_sheet, "Cells", lastRow, lastColumn));
                        rangeObj = ComLateBinding.GetProp(_sheet, "Range", cell1, cell2);
                    }
                }
                else
                    rangeObj = ComLateBinding.GetProp(_sheet, "Range", range);

                return rangeObj;
            }
            catch (Exception)
            {
                throw new ExcelException("选择区域参数格式不正确".I18n("ExcelExtension_941742EB"));
            }

        }
        private object GetRange(string workbook, string sheet, ExcelRange excelRange)
        {
            object _sheet = GetWorksheetByName(workbook, sheet);
            try
            {
                string rangeAddr = ExcelUtils.ConvertExcelRangeAddress(excelRange);
                return ComLateBinding.GetProp(_sheet, "Range", rangeAddr);
            }
            catch (Exception)
            {
                throw new ExcelException("选择区域参数格式不正确".I18n("ExcelExtension_941742EB"));
            }
        }
        void GetRangeValue(object contentSheet, int valueType = 0)
        {
            using (var com = new ComScope())
            {
                object usedRange = com.Track(ComLateBinding.GetProp(contentSheet, "UsedRange"));
                object usedRows = com.Track(ComLateBinding.GetProp(usedRange, "Rows"));
                object usedColumns = com.Track(ComLateBinding.GetProp(usedRange, "Columns"));
                int totalRows = ComLateBinding.ToInt(ComLateBinding.GetProp(usedRows, "Count"));
                int startRow = 1, lastRow = totalRows;
                string startColumn = "A", lastColumn = ExcelUtils.ColumnIndexToLetter(ComLateBinding.ToInt(ComLateBinding.GetProp(usedColumns, "Count")));
                int totalTasks = totalRows / ExcelConfigurations.Info.GlobalSetting.BatchSize + (totalRows % ExcelConfigurations.Info.GlobalSetting.BatchSize == 0 ? 0 : 1);
                if (totalTasks < 2)
                {
                    object _rng = com.Track(ComLateBinding.GetProp(contentSheet, "Range", $"{startColumn}{startRow}:{lastColumn}{lastRow}"));
                    ExcelUtils.SerializerDataToBinaryFile(GetRangeValueByType(_rng, valueType));
                }
                else
                {
                    for (int i = 0; i < totalTasks; i++)
                    {
                        int _startRow = i * ExcelConfigurations.Info.GlobalSetting.BatchSize + startRow;
                        int _endRow = Math.Min((i + 1) * ExcelConfigurations.Info.GlobalSetting.BatchSize + startRow - 1, lastRow);
                        logger.LogInformation($"read content,start row num:{_startRow},end row num:{_endRow}");
                        object _rng = com.Track(ComLateBinding.GetProp(contentSheet, "Range", $"{startColumn}{_startRow}:{lastColumn}{_endRow}"));
                        ExcelUtils.SerializerDataToBinaryFile(GetRangeValueByType(_rng, valueType), $"{i + 1}-{totalTasks}");
                    }
                }
                DeleteSheetSilently(contentSheet);
            }
            ReleaseComObject(contentSheet);
        }
        string GetRangeValue(string workbook, string sheet, int startRow, string startColumn, int lastRow, string lastColumn, int valueType = 0, bool ignoreHidden = false)
        {
            int totalRows = lastRow - startRow + 1;
            int totalTasks = totalRows / ExcelConfigurations.Info.GlobalSetting.BatchSize + (totalRows % ExcelConfigurations.Info.GlobalSetting.BatchSize == 0 ? 0 : 1);
            object contentSheet = null;

            using (var com = new ComScope())
            {
                if (ignoreHidden)
                {
                    object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                    object wb = GetWorkbook(workbook);
                    object sheets = com.Track(ComLateBinding.GetProp(wb, "Sheets"));
                    contentSheet = ComLateBinding.Invoke(sheets, "Add");
                    ComLateBinding.SetProp(contentSheet, "Visible", 2);
                    object _rng = com.Track(ComLateBinding.GetProp(_sheet, "Range", $"{startColumn}{startRow}:{lastColumn}{lastRow}"));
                    object cell1 = com.Track(ComLateBinding.GetProp(contentSheet, "Cells", 1, 1));
                    ComLateBinding.Invoke(_rng, "Copy", cell1);
                    GetRangeValue(contentSheet, valueType);
                }
                else
                {
                    contentSheet = GetWorksheetByName(workbook, sheet);
                    if (totalTasks < 2)
                    {
                        object _rng = com.Track(ComLateBinding.GetProp(contentSheet, "Range", $"{startColumn}{startRow}:{lastColumn}{lastRow}"));
                        ExcelUtils.SerializerDataToBinaryFile(GetRangeValueByType(_rng, valueType));
                    }
                    else
                    {
                        for (int i = 0; i < totalTasks; i++)
                        {
                            int _startRow = i * ExcelConfigurations.Info.GlobalSetting.BatchSize + startRow;
                            int _endRow = Math.Min((i + 1) * ExcelConfigurations.Info.GlobalSetting.BatchSize + startRow - 1, lastRow);
                            logger.LogInformation($"read content,start row num:{_startRow},end row num:{_endRow}");
                            object _rng = com.Track(ComLateBinding.GetProp(contentSheet, "Range", $"{startColumn}{_startRow}:{lastColumn}{_endRow}"));
                            ExcelUtils.SerializerDataToBinaryFile(GetRangeValueByType(_rng, valueType), $"{i + 1}-{totalTasks}");
                        }
                    }
                    ReleaseComObject(contentSheet);
                }
            }

            return ExcelUtils.TEMP_FILEPATH;
        }
        private bool TryParseRange(object sheet, string refr, out object range)
        {
            try
            {
                range = ComLateBinding.GetProp(sheet, "Range", refr);
                return true;
            }
            catch (Exception)
            {
                range = null;
                return false;
            }
        }
        private object TryGetInternalExcelWindow(IntPtr excelWindowHandle)
        {
            if (excelWindowHandle == IntPtr.Zero)
            {
                return null;
            }
            var regex = new Regex("EXCEL\\s*7", RegexOptions.IgnoreCase);
            List<ExcelWindow> list = new ExcelChildWindowsEnumerator(new ExcelWindow(excelWindowHandle), (ExcelWindow childWindow, object _) => regex.IsMatch(childWindow.Class)).FindWindows();
            if (list.Count == 0)
            {
                return null;
            }

            object result = null;
            //list[0].BringToFront();
            if (NativeUtils.AccessibleObjectFromWindow(list[0].Handle, NativeUtils.OBJID_NATIVEOM, ref NativeUtils.IID_IDispatch, ref result) >= 0)
            {
                return result;
            }

            return null;
        }
        private static bool IsRunningAsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        private void CopyRegistryKey(string sourceKeyPath, string destinationKeyPath, RegistryView registryView)
        {
            try
            {
                using (RegistryKey sourceKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, registryView).OpenSubKey(sourceKeyPath, false))
                {
                    if (sourceKey != null)
                    {
                        using (RegistryKey destinationBaseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView))
                        {
                            using (RegistryKey destinationKey = destinationBaseKey.OpenSubKey(destinationKeyPath, false))
                            {
                                if (destinationKey != null)
                                {
                                    Console.WriteLine($"Destination key '{destinationKeyPath}' already exists. Skipping copy.");
                                    return;
                                }
                            }

                            using (RegistryKey newDestinationKey = destinationBaseKey.CreateSubKey(destinationKeyPath))
                            {
                                foreach (string valueName in sourceKey.GetValueNames())
                                {
                                    object value = sourceKey.GetValue(valueName);
                                    RegistryValueKind valueKind = sourceKey.GetValueKind(valueName);
                                    newDestinationKey.SetValue(valueName, value, valueKind);
                                }

                                foreach (string subKeyName in sourceKey.GetSubKeyNames())
                                {
                                    CopyRegistryKey($"{sourceKeyPath}\\{subKeyName}", $"{destinationKeyPath}\\{subKeyName}", registryView);
                                }
                            }
                        }
                    }
                    else
                    {
                        logger.LogWarning($"Source key '{sourceKeyPath}' not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"An error occurred: {ex.Message}");
            }
        }
        private void SetWPSRegistry()
        {
            if (IsRunningAsAdministrator())
            {
                RegistryView registryView = RegistryView.Default;
                CopyRegistryKey(@"SOFTWARE\Classes\KET.Application", @"SOFTWARE\Classes\KET.Application", registryView);
                CopyRegistryKey(@"SOFTWARE\Classes\KET.Application.9", @"SOFTWARE\Classes\KET.Application.9", registryView);
                CopyRegistryKey(@"SOFTWARE\Classes\CLSID\{45540001-5750-5300-4B49-4E47534F4655}", @"SOFTWARE\Classes\CLSID\{45540001-5750-5300-4B49-4E47534F4655}", registryView);
            }
        }
        private Tuple<string, int, string, int> ValidRangeAddress(string workbook, string sheet, string address)
        {
            try
            {
                using (var com = new ComScope())
                {
                    object _sheet = com.Track(GetWorksheetByName(workbook, sheet));
                    object _range = com.Track(ComLateBinding.GetProp(_sheet, "Range", address));
                    string pattern = @"^(\$?(?<sc>[a-z]+)?\$?(?<sr>\d+)?)?:?(\$(?<lc>[a-z]+)?\$?(?<lr>\d+)?)?$";
                    var match = Regex.Match(ComLateBinding.ToText(ComLateBinding.GetProp(_range, "Address")), pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var startColumn = match.Groups["sc"].Value;
                        var startRow = int.Parse(match.Groups["sr"].Value);
                        var lastColumn = match.Groups["lc"].Value;

                        //填入单元格地址时，区域信息无最后列，最后行信息
                        int.TryParse(match.Groups["lr"].Value, out int lastRow);
                        if (string.IsNullOrEmpty(lastColumn))
                        {
                            lastColumn = startColumn;
                        }
                        if (lastRow == 0)
                        {
                            lastRow = startRow;
                        }
                        return new Tuple<string, int, string, int>(startColumn, startRow, lastColumn, lastRow);
                    }
                }

                throw new ExcelException("区域地址格式不正确".I18n("ExcelExtension_17E43C67"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new ExcelException("区域地址格式不正确".I18n("ExcelExtension_17E43C67"));
            }
        }
    }
}

