using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Core.Provider.Utility;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Excel.Config;
using II.RPA.Plugin.Excel.Models;
using ExcelNumberFormat;
using MessagePack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ExcelDataReader;

namespace II.RPA.Plugin.Excel.Utils
{


    internal class ExcelUtils
    {
        static ExcelUtils()
        {
        }
        private static ILogger logger = Logger.OfContext();
        private const int MaxReaderFormatLogSamples = 20;
        private sealed class TransferContext
        {
            // Snapshot once per action so BeginUpdate/EndUpdate always use the same setting.
            public readonly bool StreamingEnabled = Environment.GetEnvironmentVariable("II_RPA_EXCEL_STREAMING") != "0";
            public readonly bool DevExpressBatchEnabled = Environment.GetEnvironmentVariable("II_RPA_EXCEL_DEVEXPRESS_BATCH") != "0";
            public string Directory;
            public CellChunkManifest Manifest = new CellChunkManifest();
            public HashSet<string> ConsumedInputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public bool Executing;
            public ExcelTransferPerformance Performance;
        }
        private static readonly AsyncLocal<TransferContext> transfer = new AsyncLocal<TransferContext>();
        private static TransferContext Transfer => transfer.Value ?? (transfer.Value = new TransferContext());
        public static string TEMP_FILEPATH
        {
            get => Transfer.Directory;
            set => Transfer.Directory = value;
        }

        internal static void BeginTransfer()
        {
            transfer.Value = new TransferContext { Executing = true };
        }

        internal static ExcelTransferPerformance Performance => transfer.Value?.Performance;
        internal static bool StreamingEnabled => Transfer.StreamingEnabled;
        internal static bool DevExpressBatchEnabled => Transfer.DevExpressBatchEnabled;
        internal static void StartPerformance(string action, string mode)
        {
            Transfer.Performance = ExcelTransferPerformance.Start(action, mode);
        }

        internal static void CompleteTransfer()
        {
            CellChunkFiles.Publish(Transfer.Directory, Transfer.Manifest);
            foreach (string input in Transfer.ConsumedInputs)
                TryFuncIgnoreException(() => CellChunkFiles.DeleteInput(input));
        }

        internal static void EndTransfer()
        {
            transfer.Value = null;
        }

        internal static void CompleteInput(string input)
        {
            if (Transfer.Executing) Transfer.ConsumedInputs.Add(Path.GetFullPath(input));
            else TryFuncIgnoreException(() => CellChunkFiles.DeleteInput(input));
        }

        internal static void WriteChunks(string input, bool vector, Action<CellChunk> write)
        {
            // The baseline decodes a whole input before applying it, as before the streaming refactor.
            var chunks = StreamingEnabled
                ? CellChunkFiles.ReadChunks(input, ExcelConfigurations.Info.GlobalSetting.BatchSize, vector)
                : CellChunkFiles.ReadWhole(input, vector);
            foreach (var chunk in chunks)
                if (chunk.ColumnCount > 0)
                {
                    using (MeasureApply(chunk.RowCount, chunk.ColumnCount, chunk.RowOffset))
                        write(chunk);
                }
            CompleteInput(input);
        }

        // One scope per decoded batch, never per cell. Legacy and streaming writes share
        // this boundary: conversion/target preparation + assignment, excluding input decoding.
        internal static IDisposable MeasureApply(int rows, int columns, int rowOffset = 0)
        {
            return Performance?.Measure("apply", (long)rows * columns, $"{rows}x{columns}@{rowOffset}");
        }

        internal static int OutputCheckpoint => Transfer.Manifest.Chunks.Count;
        internal static void RollbackOutput(int checkpoint)
        {
            var chunks = Transfer.Manifest.Chunks;
            for (int i = chunks.Count - 1; i >= checkpoint; i--)
            {
                File.Delete(Path.Combine(Transfer.Directory, chunks[i].FileName));
                chunks.RemoveAt(i);
            }
            if (Transfer.Directory != null)
                foreach (string staging in Directory.EnumerateFiles(Transfer.Directory, "*.part.writing")) File.Delete(staging);
            Transfer.Manifest.Recalculate();
        }

        public static bool IsDbNull(object value)
        {
            return value == null || value is System.DBNull;
        }

        public static T SafeGet<T>(Func<dynamic> getter, T defaultValue = default)
        {
            try
            {
                var value = getter();
                if (IsDbNull(value))
                    return defaultValue;

                return (T)value;
            }
            catch
            {
                return defaultValue;
            }
        }


        public static Predicate<dynamic> IsNullOrEmptyPredicate = item => item == null || string.IsNullOrWhiteSpace(Convert.ToString(item));
        public static Predicate<dynamic> IsNotNullOrEmptyPredicate = item => item != null && !string.IsNullOrWhiteSpace(Convert.ToString(item));

        public static bool IsJaggedArrayEmpty(dynamic[][] jaggedArray)
        {
            if (jaggedArray == null || jaggedArray.Length == 0)
            {
                return true;
            }
            foreach (var innerArray in jaggedArray)
            {
                if (innerArray != null && innerArray.Length > 0)
                {
                    return false;
                }
            }
            return true;
        }
        public static dynamic[][] DeSerializerDataFromFile(string filePath)
        {
            try
            {
                var deserializedData = !StreamingEnabled && CellChunkFiles.IsManifest(filePath)
                    ? CellChunkFiles.ReadWhole(filePath, false).SelectMany(chunk =>
                        Enumerable.Range(0, chunk.RowCount).Select(r =>
                            Enumerable.Range(0, chunk.ColumnCount).Select(c => chunk.GetCell(r, c)).ToArray())).ToArray()
                    : CellChunkFiles.Read<object[][]>(filePath);
                if (!StreamingEnabled) CompleteInput(filePath);
                return deserializedData;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new ExcelException("写入数据是无效的二维列表".I18n("ExcelUtils_F9B07F1"));
            }

        }
        public static dynamic[] DeSerializerArrayFromFile(string filePath)
        {
            try
            {
                var deserializedData = !StreamingEnabled && CellChunkFiles.IsManifest(filePath)
                    ? CellChunkFiles.ReadWhole(filePath, true).SelectMany(chunk => chunk.Cells).ToArray()
                    : CellChunkFiles.Read<object[]>(filePath);
                if (!StreamingEnabled) CompleteInput(filePath);
                return deserializedData;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new ExcelException("写入数据是无效的列表".I18n("ExcelUtils_AC5D0ED9"));
            }

        }
        public static string CreateTempFolder()
        {
            var requestId = Guid.NewGuid().ToString("N");

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var candidates = new[]
            {
                Environment.GetEnvironmentVariable("Z_TEMP_ROOT"),                 // 可选的显式指定
                Path.GetTempPath(),                                                // Windows 系统解析出的可用临时目录
                Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData), "Z-Temp"),    // 原方案
                Path.Combine(userProfile, ".z-temp"),                              // 用户主目录兜底
                Path.Combine(AppContext.BaseDirectory, "Temp"),                    // 程序目录兜底
            };
        
            foreach (var baseDir in candidates)
            {
                if (string.IsNullOrWhiteSpace(baseDir))
                    continue;

                try
                {
                    var folder = Path.Combine(baseDir, "Z-Temp", requestId);
                    Directory.CreateDirectory(folder);

                    // 写入探针：CreateDirectory 成功不代表有写权限
                    var probe = Path.Combine(folder, $".write-test-{Guid.NewGuid():N}.tmp");
                    File.WriteAllText(probe, string.Empty);
                    File.Delete(probe);

                    File.WriteAllText(Path.Combine(folder, CellChunkFiles.OwnerFile), DateTime.UtcNow.ToString("O"));
                    CleanupExpiredTransfers(Path.GetDirectoryName(folder));

                    return folder;
                }
                catch (Exception ex)
                {                  
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"创建临时目录失败，尝试下一候选路径: {baseDir}");
                }
            }

            throw new ExcelException("创建数据文件夹失败".I18n("ExcelUtils_6520DEE"));
        }

        private static readonly Dictionary<string, DateTime> transferCleanupTimes = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        internal static void CleanupExpiredTransfers(string root, bool force = false)
        {
            root = Path.GetFullPath(root);
            var now = DateTime.UtcNow;
            lock (transferCleanupTimes)
            {
                if (!force && transferCleanupTimes.TryGetValue(root, out var previous) && now - previous < TimeSpan.FromHours(1)) return;
                transferCleanupTimes[root] = now;
            }
            TryFuncIgnoreException(() =>
            {
                foreach (string folder in Directory.EnumerateDirectories(root))
                {
                    TryFuncIgnoreException(() =>
                    {
                        string marker = Path.Combine(folder, CellChunkFiles.OwnerFile);
                        if (!File.Exists(marker) || File.GetLastWriteTimeUtc(marker) > DateTime.UtcNow.AddDays(-7)) return;
                        // Only owned transfer files, never the entire shared Z-Temp tree.
                        foreach (string file in Directory.EnumerateFiles(folder))
                        {
                            string name = Path.GetFileName(file);
                            if (name == "manifest.json" || name == "manifest.json.writing" ||
                                name.EndsWith(".part", StringComparison.OrdinalIgnoreCase) ||
                                name.EndsWith(".part.writing", StringComparison.OrdinalIgnoreCase) ||
                                name.EndsWith(".debug", StringComparison.OrdinalIgnoreCase)) File.Delete(file);
                        }
                        // Retain the ownership marker if any deletion above fails,
                        // so a later sweep can retry this directory.
                        File.Delete(marker);
                        if (!Directory.EnumerateFileSystemEntries(folder).Any()) Directory.Delete(folder);
                    });
                }
            });
        }

        /// <summary>
        /// 序列化数据到临时文件
        /// </summary>
        /// <param name="data">数据</param>       
        public static void SerializerDataToBinaryFile(dynamic data, string fileName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TEMP_FILEPATH))
                {
                    TEMP_FILEPATH = CreateTempFolder();
                }
                if (data == null) return;
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = $"chunk-{Transfer.Manifest.Chunks.Count + 1:D6}";
                }
                if (ExcelConfigurations.Info.GlobalSetting.Debug?.Equals("true") == true)
                {
                    File.WriteAllText($"{TEMP_FILEPATH}\\{fileName}.debug", MessagePackSerializer.SerializeToJson(data));
                }
                CellChunkFiles.Write(TEMP_FILEPATH, (object)data, fileName, Transfer.Manifest);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new ExcelException("序列化数据时异常".I18n("ExcelUtils_D743922F"));
            }

        }

        /// <summary>
        /// 序列化数据到临时文件
        /// </summary>
        /// <param name="data">数据</param>       
        public static string SerializerDataToFile(JObject result)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TEMP_FILEPATH))
                {
                    TEMP_FILEPATH = CreateTempFolder();
                }
                var temp = $"{TEMP_FILEPATH}\\{DateTime.Now:HHmmssfff}.part";
                //过滤组件（京及真）需要的逻辑
                var retResult = result.ToString();
                File.WriteAllText(temp, retResult);
                return temp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw new ExcelException("序列化数据时异常".I18n("ExcelUtils_D743922F"));
            }

        }
        /// <summary>
        ///从Excel 读取得数据中查找指定条件数据
        /// </summary>
        /// <param name="array">读取的Excel数据</param>
        /// <param name="reverse">是否逆序</param>
        /// <returns></returns>
        internal static int ScanEmptyCell(IEnumerable<object> values, bool afterLastValue)
        {
            int row = 0, lastValueRow = 0;
            foreach (object value in values)
            {
                row++;
                bool empty = IsNullOrEmptyPredicate(value);
                if (!afterLastValue && empty) return row;
                if (!empty) lastValueRow = row;
            }
            return afterLastValue ? lastValueRow + 1 : row + 1;
        }

        public static int FindValueInArray(dynamic array, Predicate<dynamic> predicate, bool reverse = false)
        {
            var _array = array as Array;
            if (array != null)
            {
                var dimensions = _array.Rank;
                if (dimensions == 1)
                {

                    if (reverse)
                    {
                        return Array.FindLastIndex(_array as dynamic[], predicate);
                    }
                    else
                    {
                        return Array.FindIndex(_array as dynamic[], predicate);
                    }
                }
                else
                {
                    var twoDimensionArray = _array as dynamic[,];
                    int rowCount = twoDimensionArray.GetLength(0);
                    int columnCount = twoDimensionArray.GetLength(1);
                    dynamic[] _arrayData = null;
                    if (rowCount == 1)//行数据
                    {
                        _arrayData = new dynamic[columnCount];
                        for (int i = 0; i < columnCount; i++)
                        {
                            _arrayData[i] = twoDimensionArray[1, i + 1];
                        }
                    }
                    else if (columnCount == 1)//列数据
                    {
                        _arrayData = new dynamic[rowCount];
                        for (int i = 0; i < rowCount; i++)
                        {
                            _arrayData[i] = twoDimensionArray[i + 1, 1];
                        }
                    }
                    else //区域数据
                    {
                        //暂不需要，todo:
                    }

                    if (reverse)
                    {
                        return Array.FindLastIndex(_arrayData, predicate);
                    }
                    else
                    {
                        return Array.FindIndex(_arrayData, predicate);
                    }
                }
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// 解析前端传入的区域表达式
        /// </summary>
        /// <param name="range">区域对象</param>
        /// <returns>Excel区域对象</returns>
        /// <exception cref="ExcelException">参数无法解析</exception>
        public static ExcelRange ConvertToExcelRange(string range)
        {
            try
            {
                //type(0普通模式 | 1高级模式)
                var rangeObj = JObject.Parse(range);
                var option = rangeObj.Value<int?>("option");
                ExcelRange excelRange = new ExcelRange();
                excelRange.type = rangeObj.Value<int>("type");
                if (option.HasValue)
                {
                    //displayType: excelRng 控件(新)
                    excelRange.option = option.Value;
                    //根据选项不同，只校验必要字段信息，兼容前端切换选项把行列默认值置为空的需求（zoco-17004）
                    if (option == 0)
                    {
                        excelRange.row = ConvertNegativeRow(rangeObj.Value<object>("row"));
                        excelRange.column = ConvertNegativeColumn(rangeObj.Value<string>("column"));
                    }
                    else if (option == 1)
                    {
                        excelRange.row = ConvertNegativeRow(rangeObj.Value<object>("row"));
                    }
                    else if (option == 2)
                    {
                        excelRange.column = ConvertNegativeColumn(rangeObj.Value<string>("column"));
                    }
                    else if (option == 3)
                    {
                        //区域：只校验区域信息字段
                        if (excelRange.type == 0)
                        {
                            excelRange.startRow = ConvertNegativeRow(rangeObj.Value<object>("startRow"));
                            excelRange.startColumn = ConvertNegativeColumn(rangeObj.Value<string>("startColumn"));
                            excelRange.lastRow = ConvertNegativeRangeEndRow(excelRange.startRow, rangeObj.Value<object>("lastRow"));
                            excelRange.lastColumn = ConvertNegativeRangeEndColumn(excelRange.startColumn, rangeObj.Value<string>("lastColumn"));
                        }
                    }
                    else
                    {
                        //获取已编辑区域
                    }
                    excelRange.range = rangeObj.Value<string>("range");
                }
                else
                {
                    //displayType: chooseRange 控件（旧）
                    excelRange.option = 3;//区域
                    if (excelRange.type == 0)
                    {
                        excelRange.startRow = ConvertNegativeRow(rangeObj.Value<object>("startRow"));
                        excelRange.startColumn = ConvertNegativeColumn(rangeObj.Value<string>("startColumn"));
                        excelRange.lastRow = ConvertNegativeRangeEndRow(excelRange.startRow, rangeObj.Value<object>("lastRow"));
                        excelRange.lastColumn = ConvertNegativeRangeEndColumn(excelRange.startColumn, rangeObj.Value<string>("lastColumn"));
                    }
                    excelRange.range = rangeObj.Value<string>("range");
                }
                //统一转成 Adress形式，兼容旧组件
                if (excelRange.type == 0)
                {
                    excelRange.range = ConvertExcelRangeAddress(excelRange);
                    excelRange.type = 1;
                }
                return excelRange;
            }
            catch (Exception ex)
            {
                if (ex is ExcelException)
                    throw ex;
                throw new ExcelException("区域参数格式不正确".I18n("ExcelUtils_7FD961ED"));
            }

        }

        #region 转换Excel区域格式返回给前端
        public static ExcelRange GetExcelRange(string rngAddress)
        {
            string pattern = @"^(\$?(?<sc>[a-z]+)?\$?(?<sr>\d+)?)?:?(\$(?<lc>[a-z]+)?\$?(?<lr>\d+)?)?$";
            var match = Regex.Match(rngAddress, pattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                ExcelRange rng = new ExcelRange();
                if (string.IsNullOrEmpty(match.Groups["sc"].Value))
                {
                    rng.option = 1;
                    rng.row = int.Parse(match.Groups["sr"].Value);
                    rng.column = "\"A\"";
                    rng.startColumn = rng.column;
                    rng.startRow = rng.row;
                    rng.lastColumn = "\"-1\"";
                    rng.lastRow = int.Parse(match.Groups["lr"].Value);
                    rng.range = $"\"{rng.row}:{rng.lastRow}\"";
                }
                else if (string.IsNullOrEmpty(match.Groups["lc"].Value))
                {
                    rng.option = 0;
                    rng.column = $"\"{match.Groups["sc"].Value}\"";
                    rng.row = int.Parse(match.Groups["sr"].Value);
                    rng.startColumn = rng.column;
                    rng.startRow = rng.row;
                    rng.lastRow = rng.row;
                    rng.lastColumn = rng.column;
                    rng.range = $"\"{match.Groups["sc"].Value}{rng.row}\"";
                }
                else if (string.IsNullOrEmpty(match.Groups["sr"].Value))
                {
                    rng.option = 2;
                    rng.column = $"\"{match.Groups["sc"].Value}\"";
                    rng.row = 1;
                    rng.startColumn = rng.column;
                    rng.startRow = rng.row;
                    rng.lastRow = -1;
                    rng.lastColumn = $"\"{match.Groups["lc"].Value}\"";
                    rng.range = $"\"{match.Groups["sc"].Value}:{match.Groups["lc"].Value}\"";
                }
                else
                {
                    rng.option = 3;
                    rng.startColumn = $"\"{match.Groups["sc"].Value}\"";
                    rng.column = rng.startColumn;
                    rng.startRow = int.Parse(match.Groups["sr"].Value);
                    rng.row = rng.startRow;
                    rng.lastColumn = $"\"{match.Groups["lc"].Value}\"";
                    rng.lastRow = int.Parse(match.Groups["lr"].Value);
                    rng.range = $"\"{match.Groups["sc"].Value}{rng.row}:{match.Groups["lc"].Value}{rng.lastRow}\"";
                }
                return rng;
            }
            return null;
        }
        public static (string column, int row) GetLastCellInfo(string address)
        {
            address = address?.Trim();
            if (address.Contains(':'))
            {
                var parts = address.Split(':');
                address = parts[1];
            }

            if (Regex.IsMatch(address, @"^\$?\d+\$?$"))
            {
                return (ColumnIndexToLetter(16384), int.Parse(address.TrimStart('$').TrimEnd('$')));
            }
            else if (Regex.IsMatch(address, @"^\$?[a-zA-Z]+\$?$"))
            {
                return (address.TrimStart('$').ToUpper(), 1048576);
            }

            // 解析列和行
            var match = Regex.Match(address, @"(\$?[a-zA-Z]+)(\$?\d+)");
            if (!match.Success)
            {
                throw new ArgumentException("Invalid address format.");
            }

            string columnStr = match.Groups[1].Value.TrimStart('$').ToUpper();
            int rowNumber = int.Parse(match.Groups[2].Value.TrimStart('$'));

            return (columnStr, rowNumber);
        }
        #endregion


        /// <summary>
        /// 转换ExcelRange 对象 为 Excel区域地址
        /// </summary>
        /// <param name="range"></param>
        /// <param name="usedRange"></param>
        /// <returns></returns>
        public static string ConvertExcelRangeAddress(ExcelRange range, bool usedRange = false)
        {
            string rangeAddress;
            switch (range.option)
            {
                case 0://单元格
                    {
                        rangeAddress = $"{range.column}{range.row}";
                    }
                    break;
                case 1://行
                    {
                        if (usedRange)
                        {
                            var editRange = ExcelPlugin.Instance.RequestContext.ExcelUsedRange;
                            rangeAddress = $"{editRange.StartColumn}{range.row}:{editRange.EndColumn}{range.row}";
                        }
                        else
                        {
                            rangeAddress = $"{range.row}:{range.row}";
                        }


                    }
                    break;
                case 2://列
                    {
                        if (usedRange)
                        {
                            var editRange = ExcelPlugin.Instance.RequestContext.ExcelUsedRange;
                            rangeAddress = $"{range.column}{editRange.StartRow}:{range.column}{editRange.EndRow}";
                        }
                        else
                        {
                            rangeAddress = $"{range.column}:{range.column}";
                        }

                    }
                    break;
                case null://display:chooseRange 的控件没有此选项值
                case 3://区域
                    {
                        if (range.type == 0)
                        {
                            rangeAddress = $"{range.startColumn}{range.startRow}:{range.lastColumn}{range.lastRow}";
                        }
                        else
                        {
                            rangeAddress = range.range;
                        }

                    }
                    break;
                default://已编辑区域
                    {
                        var editRange = ExcelPlugin.Instance.RequestContext.ExcelUsedRange;
                        rangeAddress = $"{editRange.StartColumn}{editRange.StartRow}:{editRange.EndColumn}{editRange.EndRow}";
                    }
                    break;
            }
            logger.LogInformation($"Excel Range Address:{rangeAddress}");
            return rangeAddress;
        }

        /// <summary>
        /// 转换 excelRange属性类型为Excel区域地址
        /// </summary>
        /// <param name="range"></param>
        /// <param name="usedRange">为True,行和列使用以编辑区域</param>
        /// <returns></returns>
        public static string ConvertExcelRangeAddress(string range, bool usedRange = false)
        {
            return ConvertExcelRangeAddress(ConvertToExcelRange(range), usedRange);
        }
        public static string ConvertNumberColor(int color)
        {
            int red = 0xFF & color;
            int green = 0xFF00 & color;
            green >>= 8;
            int blue = 0xFF0000 & color;
            blue >>= 16;
            return $"[{red},{green},{blue}]";
        }
        /// <summary>
        /// 色码转颜色对象
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static System.Drawing.Color GetColor(string color)
        {
            try
            {
                if (color.StartsWith("#"))
                {
                    return ColorTranslator.FromHtml(color);
                }
                else
                {
                    var arr = DeserializeStrArray(color);
                    return System.Drawing.Color.FromArgb(int.Parse(arr[0].ToString()), int.Parse(arr[1].ToString()), int.Parse(arr[2].ToString()));
                }
            }
            catch (Exception)
            {

                throw new ExcelException("输入的颜色格式不正确，请查看说明文档".I18n("ExcelUtils_D02F4DE7"));
            }

        }

        /// <summary>
        /// 颜色对象转色码
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string TurnColorToCode(System.Drawing.Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public static dynamic[] DeserializeStrArray(string text)
        {
            try
            {
                var arrVal = JsonConvert.DeserializeObject<object[]>(text);
                return arrVal;
            }
            catch (Exception)
            {
                throw new ExcelException("输入的参数内容格式不正确（无效的列表），请查看说明文档".I18n("ExcelUtils_C7793E17"));
            }

        }
        public static string ParseColumnRangeText(string range)
        {
            try
            {
                var arr = ConvertColumnsNameArray(range);
                var list = from _col in arr
                           select $"{_col}:{_col}";
                return string.Join(",", list);
            }
            catch (ExcelException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                return $"{ConvertNegativeColumn(range)}:{ConvertNegativeColumn(range)}";
            }

        }
        public static string ParseRowRangeText(string range)
        {
            try
            {
                var arr = DeserializeStrArray(range);
                var list = from _row in arr
                           select $"{ConvertNegativeRow(_row)}:{ConvertNegativeRow(_row)}";
                return string.Join(",", list);
            }
            catch (Exception)
            {
                return $"{ConvertNegativeRow(range)}:{ConvertNegativeRow(range)}";
            }

        }
        public static T ConvertStrToArray<T>(string text)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(text);
            }
            catch (Exception)
            {
                throw new ExcelException("输入的参数内容格式不正确（写入内容），请输入有效的二维列表或查看说明文档".I18n("ExcelUtils_5A20608A"));
            }
        }


        #region 判断是否是合法的N维数组 IsValidArrayJson
        /// <summary>
        /// 判断是否是合法的N维数组
        /// </summary>
        /// <param name="text">数组Json</param>
        /// <param name="rank">维数</param>
        /// <returns></returns>
        public static bool IsValidArrayJson(string text, int rank)
        {
            try
            {
                var array = JsonConvert.DeserializeObject<JArray>(text);

                if (rank > 0)
                {
                    int rankobj = CalculateDimensions(JsonConvert.DeserializeObject<object>(text));
                    Console.WriteLine(rankobj);
                    if (rankobj == rank)
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        public static int CalculateDimensions(object array)
        {
            bool bRet = TrunToList(array, out List<object> arrayB);
            if (!bRet)
            {
                return 0;
            }
            else
            {
                int maxSubDimensions = 0;
                if (arrayB == null)
                {
                    return 0;
                }
                foreach (object arrayBItem in arrayB)
                {
                    int subDimensions = CalculateDimensions(arrayBItem);
                    maxSubDimensions = Math.Max(maxSubDimensions, subDimensions);
                }
                return maxSubDimensions + 1;
            }
        }

        public static bool TrunToList(object array, out List<object> arrayList)
        {
            try
            {
                arrayList = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(array));
                if (arrayList.Count > -1)
                {
                    return true;
                }
            }
            catch
            {
                arrayList = null;
            }

            return false;
        }
        #endregion

        public static int ConvertNegativeRow(object row, bool required = true)
        {
            if (row == null)
            {
                return 0;
            }
            if (int.TryParse(row.ToString(), out int __row))
            {
                if (__row <= 0)
                {
                    if (ExcelPlugin.Instance.RequestContext.ExcelUsedRange.EndRow + __row + 1 <= 0)
                    {
                        throw new ExcelException(Localize($" 行号：{row} 无效，最小输入值为 -{ExcelPlugin.Instance.RequestContext.ExcelUsedRange.EndRow}", "ExcelUtils_14F8648B"));
                    }
                    return ExcelPlugin.Instance.RequestContext.ExcelUsedRange.EndRow + __row + 1;
                }
                return __row;
            }
            else
            {
                throw new ExcelException(Localize($" {row} 不是有效的行号，行号应为小于 1048576 的整数", "ExcelUtils_76766F4"));
            }


        }

        public static int ConvertNegativeRangeEndRow(object startRow, object endRow)
        {
            var convertedEndRow = ConvertNegativeRow(endRow);
            if ((ExcelConfigurations.Info?.GlobalSetting?.UseLegacyRange ?? 0) != 0)
            {
                return convertedEndRow;
            }

            var convertedStartRow = ConvertNegativeRow(startRow);
            return IsNegativeNumber(endRow) && convertedEndRow < convertedStartRow ? convertedStartRow : convertedEndRow;
        }

        private static bool IsNegativeNumber(object value)
        {
            return int.TryParse(value?.ToString(), out var number) && number < 0;
        }

        //列名支持输入索引
        public static string ConvertNegativeColumn(object columnObj)
        {
            string input = columnObj?.ToString();
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }
            else
            {
                input = TrimBom(input);
            }

            string column = null;
            if (int.TryParse(input, out int _columnIndex))
            {
                if (_columnIndex < 0)
                {
                    _columnIndex = ColumnLetterToIndex(ExcelPlugin.Instance.RequestContext.ExcelUsedRange.EndColumn) + _columnIndex + 1;
                    if (_columnIndex <= 0)
                        throw new ExcelException(Localize($" 列索引：{input} 无效; 原因：当前Sheet列数{ExcelPlugin.Instance.RequestContext.ExcelUsedRange.EndColumnIndex}", "ExcelUtils_7E6DF182"));

                }
                column = ColumnIndexToLetter(_columnIndex);
            }
            if (!ValidateColumn(column: column ?? input))
                throw new ExcelException(Localize($"{input} 不是有效的列名，列名应为A到XFD之间的字母或者是小于 16384 的正整数", "ExcelUtils_B04084EE"));
            return column ?? input;
        }

        public static string ConvertNegativeRangeEndColumn(object startColumn, object endColumn)
        {
            var convertedEndColumn = ConvertNegativeColumn(endColumn);
            if ((ExcelConfigurations.Info?.GlobalSetting?.UseLegacyRange ?? 0) != 0)
            {
                return convertedEndColumn;
            }

            var convertedStartColumn = ConvertNegativeColumn(startColumn);
            if (string.IsNullOrWhiteSpace(convertedStartColumn) || string.IsNullOrWhiteSpace(convertedEndColumn))
            {
                return convertedEndColumn;
            }

            return IsNegativeNumber(endColumn) && ColumnLetterToIndex(convertedEndColumn) < ColumnLetterToIndex(convertedStartColumn)
                ? convertedStartColumn
                : convertedEndColumn;
        }
        public static int ColumnLetterToIndex(string columnLetter)
        {

            columnLetter = TrimBom(columnLetter);
            if (!ValidateColumn(columnLetter)) throw new ExcelException(Localize($"{columnLetter} 无法转成有效的列名", "ExcelUtils_4BF711EF"));
            columnLetter = columnLetter.ToUpper().Trim();
            int sum = 0;
            for (int i = 0; i < columnLetter.Length; i++)
            {
                sum *= 26;
                sum += (columnLetter[i] - 'A' + 1);
            }
            return sum;
        }
        public static string ColumnIndexToLetter(int colIndex)
        {
            int div = colIndex;
            string colLetter = null;
            int mod = 0;
            while (div > 0)
            {
                mod = (div - 1) % 26;
                colLetter = (char)(65 + mod) + colLetter;
                div = (div - mod) / 26;
            }
            return colLetter;
        }

        /// <summary>
        /// 转化填入列索引为实际列位置
        /// </summary>
        /// <param name="colIndex"></param>
        /// <param name="totalColumnCount"></param>
        /// <returns></returns>
        public static int GetRealColumnIndex(int colIndex, int totalColumnCount)
        {
            int targetColumn = -1;
            if (colIndex > 0)
            {
                targetColumn = colIndex;
            }
            else if (colIndex < 0)
            {
                ;
                if (totalColumnCount + colIndex < 1)
                {
                    targetColumn = 1;
                }
                else
                {
                    targetColumn = totalColumnCount + colIndex;
                }
            }
            else
            {
                targetColumn = 1;
            }
            return targetColumn;
        }

        public static object[] ConvertColumnsIndexArray(string columns)
        {
            try
            {

                var strArr = columns.Trim(new char[] { '[', ']', '(', ')', }).Replace("'", "").Replace("\"", "");
                var arr = (from c in strArr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                           select int.TryParse(c.Trim(), out int r) ? r : (object)ColumnLetterToIndex(c.Trim()));
                return arr.ToArray(); ;
            }
            catch (Exception ex)
            {
                throw new ExcelException(Localize($"输入的参数内容格式不正确:{ex.Message}", "ExcelUtils_29FE5342"));
            }
        }
        public static object[] ConvertStrToArray(string columns)
        {
            try
            {

                var strArr = columns.Trim(new char[] { '[', ']', '(', ')', }).Replace("'", "").Replace("\"", "");
                var arr = from c in strArr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                          select c.Trim();
                return arr.ToArray();
            }
            catch (Exception)
            {
                throw new ExcelException("输入的参数内容格式不正确，请查看说明文档".I18n("ExcelUtils_F3521065"));
            }
        }
        public static object[] ConvertColumnsNameArray(string columns)
        {
            try
            {
                columns = columns.Replace("\"", "").Replace("'", "");
                var strArr = columns.Trim(new char[] { '[', ']', '(', ')', });
                var arr = (from c in strArr.Split(new char[] { ',' })
                           select ConvertNegativeColumn(c.Trim()));
                return arr.ToArray();
            }
            catch (ExcelException ex)
            {
                throw ex;
            }

            catch (Exception)
            {

                throw new ExcelException("列名参数格式输入不正确，请查看说明文档".I18n("ExcelUtils_80F082D1"));
            }

        }
        public static string EncodeBase64(string code, string code_type = "utf-8")
        {
            string encode = "";
            byte[] bytes = Encoding.GetEncoding(code_type).GetBytes(code);
            try
            {
                encode = Convert.ToBase64String(bytes);
            }
            catch
            {
                encode = code;
            }
            return encode;
        }
        public static string EncodeSM4(string code)
        {
            try
            {
                Sm4Crypto sm = new Sm4Crypto();
                sm.Key = ExcelConfigurations.Info?.GlobalSetting?.Key4 ?? "INPUT_PARAM_KEY4";
                sm.Data = code;
                var valueStr = Sm4Crypto.EncryptECB(sm);
                return valueStr;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex);
                return EncodeBase64(code);
            }
        }
        public static string DecodeBase64(string code)
        {
            try
            {
                Sm4Crypto sm = new Sm4Crypto();
                sm.Key = ExcelConfigurations.Info?.GlobalSetting?.Key4 ?? "INPUT_PARAM_KEY4";
                sm.Data = code;
                var valueStr = Sm4Crypto.DecryptECB(sm, Encoding.UTF8);
                return valueStr;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex);
                return DecodeBase64(code, "utf-8");
            }
        }
        public static string DecodeBase64(string code, string code_type)
        {

            string decode = "";
            byte[] bytes = Convert.FromBase64String(code);
            try
            {
                decode = Encoding.GetEncoding(code_type).GetString(bytes);
            }
            catch
            {
                decode = code;
            }
            return decode;
        }
        public static bool ValidateRow(object row)
        {
            return int.TryParse(row.ToString(), out int rowVal) && rowVal > 0 && rowVal <= 1048576;
        }

        /// <summary>
        /// 字符串转16进制字符
        /// </summary>
        /// <param name="_str">字符串</param>
        /// <param name="encode">编码格式</param>
        /// <returns></returns>
        private static string StringToHexString(string _str, Encoding encode)
        {
            //去掉空格
            _str = _str.Replace(" ", "");
            //将字符串转换成字节数组。
            byte[] buffer = encode.GetBytes(_str);
            //定义一个string类型的变量，用于存储转换后的值。
            string result = string.Empty;
            for (int i = 0; i < buffer.Length; i++)
            {
                //将每一个字节数组转换成16进制的字符串，以空格相隔开。
                result += Convert.ToString(buffer[i], 16);
            }
            return result;
        }

        /// <summary>
        /// 滤除bom字符
        /// </summary>
        /// <param name="column"></param>
        public static string TrimBom(string column)
        {
            if (column != null && column.Contains('\uFEFF'))
            {
                logger.LogInformation("检测到BOM字符");
                column = column.Trim(new char[] { '\uFEFF', ' ' });
            }
            return column;
        }

        public static bool ValidateColumn(string column)
        {
            column = TrimBom(column);
            //return !string.IsNullOrWhiteSpace(column) && column.Length < 4 && Regex.IsMatch(column.PadLeft(3, 'A'), "^[a-zA-Z]+$") && string.Compare(column.PadLeft(3, 'A'), "XFD", true) < 1;
            if (string.IsNullOrWhiteSpace(column))
            {
                return false;
            }
            if (column.Length >= 4)
            {
                return false;
            }
            if (!Regex.IsMatch(column.PadLeft(3, 'A'), "^[a-zA-Z]+$"))
            {
                return false;
            }
            if (string.Compare(column.PadLeft(3, 'A'), "XFD", true) >= 1)
            {
                return false;
            }
            return true;
        }
        public static bool ValidateContent(string contents)
        {
            return string.IsNullOrEmpty(contents) || contents.Equals("null") || contents.Replace(" ", "").Equals("[]") || contents.Replace(" ", "").Equals("[[]]");
        }
        public static string GetPackageDirectory()
        {
            try
            {
                //6.7.5 流程包目录
                return GlobleConfigurations.PackageDirectory;
            }
            catch (Exception)
            {
                //6.7.5 之前流程包目录
                return new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).Parent.Parent.Parent.FullName;
            }
        }
        public static string GetAbsolutePath(string path, string file)
        {
            path = path ?? "";
            if (string.IsNullOrEmpty(file))
            {
                throw new ExcelException("文件名不能为空".I18n("ExcelUtils_71D0621"));
            }
            if (!Regex.IsMatch(path, @"^[c-z]:[\\|/]", RegexOptions.IgnoreCase))
            {
                if (!(path.StartsWith("/") || path.StartsWith("\\")))
                {
                    path = "/" + path;
                }
                try
                {
                    //6.7.5 流程包目录
                    path = GlobleConfigurations.PackageDirectory + path;
                }
                catch (Exception)
                {
                    //6.7.5 之前流程包目录
                    path = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).Parent.Parent.Parent.FullName + path;
                }
            }
            if (!Regex.IsMatch(file, @"^[c-z]:[\\|/]", RegexOptions.IgnoreCase))
            {
                if (!(file.StartsWith("/") || file.StartsWith("\\")))
                {
                    file = "/" + file;
                }
                file = path + file;
            }
            var extension = Path.GetExtension(file);
            if (string.IsNullOrEmpty(extension))
            {
                file += ".xlsx";
            }
            return Path.GetFullPath(file);
        }

        /// <summary>
        /// 按 Windows 标准路径语义获取文件的绝对路径。
        /// 盘符绝对路径、UNC 路径以及以单个目录分隔符开头的路径直接解析，
        /// 其他路径相对于流程包目录解析。
        /// </summary>
        public static string GetAbsolutePathV2(string path, string file)
        {
            if (!string.IsNullOrEmpty(path) && char.IsWhiteSpace(path, 0))
            {
                throw new ExcelException("创建数据文件夹失败".I18n("ExcelUtils_6520DEE"));
            }

            if (string.IsNullOrEmpty(file))
            {
                throw new ExcelException("文件名不能为空".I18n("ExcelUtils_71D0621"));
            }

            var absolutePath = Path.GetFullPath(Path.Combine(GetPackageDirectory(), path ?? string.Empty, file));

            if (string.IsNullOrEmpty(Path.GetExtension(absolutePath)))
            {
                absolutePath += ".xlsx";
            }

            return absolutePath;
        }

        public static bool IsAbsolutePath(string file)
        {
            return Regex.IsMatch(file, @"^[c-z]:[\\|/]", RegexOptions.IgnoreCase);
        }
        public static string GetDuplicateFileName(string orignName)
        {
            var extension = Path.GetExtension(orignName);
            var dir = Path.GetDirectoryName(orignName);
            var name = Path.GetFileNameWithoutExtension(orignName);
            if (File.Exists(orignName))
            {
                name = Localize($"{name}(副本)", "ExcelUtils_9D41D69A");
                int index = 1;
                var newName = Path.Combine(dir, $"{name}{extension}");
                while (File.Exists(newName))
                {
                    newName = Path.Combine(dir, $"{name}_{index++}{extension}");
                }
                return newName;

            }
            return orignName;
        }
        public static string GetSaveAsName(string orignName, string destFile)
        {
            //if (IsAbsolutePath(destFile))
            //{
            //    if (Directory.Exists(destFile))
            //    {
            //        var _orignName = Path.GetFileNameWithoutExtension(orignName);
            //        var _extension=Path.GetExtension(destFile);
            //        return Path.Combine(destFile,_orignName, _extension);
            //    }

            //}
            //else
            //{ 
            //}
            return null;
        }
        public static string Replace(string input, string pattern, string replacement = "\"\"")
        {
            if (input == null) return "";
            return Regex.Replace(input, $@"[\[|,]?{pattern}[,|\]]", m => m.Value.Replace($"{pattern}", replacement));
        }

        #region 数据筛选组件验证

        /// <summary>
        /// 检测匹配规则
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="condition"></param>
        /// <param name="criterias"></param>
        /// <returns></returns>
        public static bool ValidationRule(string cell, string condition, List<Criteria> criterias)
        {
            if (condition == "0")
            {
                foreach (Criteria criteria in criterias)
                {
                    if (!IsValidation(criteria, cell))
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                foreach (Criteria criteria in criterias)
                {
                    if (IsValidation(criteria, cell))
                    {
                        return true;
                    }
                }
                return false;
            }
        }


        public static bool IsValidation(Criteria criteria, string target)
        {
            if (criteria.Operator == CriteriaOperator.Equal)
            {
                return criteria.Value == target;
            }
            else if (criteria.Operator == CriteriaOperator.NotEqual)
            {
                return criteria.Value != target;
            }
            else if (criteria.Operator == CriteriaOperator.Gt)
            {
                if (double.TryParse(criteria.Value, out double v1) && double.TryParse(target, out double v2))
                {
                    return v2 > v1;
                }
                return false;
            }
            else if (criteria.Operator == CriteriaOperator.Lt)
            {
                if (double.TryParse(criteria.Value, out double v1) && double.TryParse(target, out double v2))
                {
                    return v2 < v1;
                }
                return false;
            }
            else if (criteria.Operator == CriteriaOperator.Ge)
            {
                if (double.TryParse(criteria.Value, out double v1) && double.TryParse(target, out double v2))
                {
                    return v2 >= v1;
                }
                return false;
            }
            else if (criteria.Operator == CriteriaOperator.Le)
            {
                if (double.TryParse(criteria.Value, out double v1) && double.TryParse(target, out double v2))
                {
                    return v2 <= v1;
                }
                return false;
            }
            else if (criteria.Operator == CriteriaOperator.Contion)
            {
                return target.Contains(criteria.Value);
            }
            else if (criteria.Operator == CriteriaOperator.NotContion)
            {
                return !target.Contains(criteria.Value);
            }
            else if (criteria.Operator == CriteriaOperator.Empty)
            {
                return string.IsNullOrEmpty(target);
            }
            else if (criteria.Operator == CriteriaOperator.NotEmpty)
            {
                return !string.IsNullOrEmpty(target);
            }
            else if (criteria.Operator == CriteriaOperator.StartWith)
            {
                return target.StartsWith(criteria.Value);
            }
            else if (criteria.Operator == CriteriaOperator.NotStartWith)
            {
                return !target.StartsWith(criteria.Value);
            }
            else if (criteria.Operator == CriteriaOperator.EndWith)
            {
                return target.EndsWith(criteria.Value);
            }
            else if (criteria.Operator == CriteriaOperator.NotEndWith)
            {
                return !target.EndsWith(criteria.Value);
            }
            else if (criteria.Operator == CriteriaOperator.DateLast)
            {
                if (DateTime.TryParse(criteria.Value, out DateTime v1) && DateTime.TryParse(target, out DateTime v2))
                {
                    return v2 >= v1;
                }
                return false;
            }
            else if (criteria.Operator == CriteriaOperator.DatePre)
            {
                if (DateTime.TryParse(criteria.Value, out DateTime v1) && DateTime.TryParse(target, out DateTime v2))
                {
                    return v2 <= v1;
                }
                return false;
            }
            else if (criteria.Operator == CriteriaOperator.DateBetween)
            {
                var date = criteria.Value.Split(',');
                if (date.Length == 2)
                {
                    if (DateTime.TryParse(date[0], out DateTime v1) && DateTime.TryParse(date[1], out DateTime v2) && DateTime.TryParse(target, out DateTime v3))
                    {
                        return v3 >= v1 && v3 <= v2;
                    }
                }
                return false;
            }
            logger.LogError($"匹配类型选择有误{criteria.Operator}");
            return false;
        }
        #endregion


        /// <summary>
        /// 转换动态类型参数为类型默认值
        /// </summary>
        /// <returns></returns>
        public static dynamic ConvertDynamicToDefaultValue(Type targetType, dynamic value)
        {
            try
            {
                if (targetType == typeof(string))
                {
                    return value?.ToString() ?? string.Empty;
                }
                else if (value is JArray)
                {
                    return value.ToObject<object[]>();
                }
                else
                {
                    return Convert.ChangeType(value, targetType);
                }
            }
            catch
            {
                return targetType.IsValueType ? Convert.ChangeType(Activator.CreateInstance(targetType), targetType) : null;
            }
        }

        /// <summary>
        /// 返回当前使用软件类型
        /// </summary>
        /// <param name="software"></param>
        /// <returns></returns>
        public static string GetCurrentSoftwareType(string software)
        {
            if (software.Equals("默认"))
            {
                var defaultApp = NativeUtils.AssocQueryString(AssocStr.ASSOCSTR_FRIENDLYAPPNAME, ".xlsx")?.ToLower();
                if (defaultApp.Contains("wps"))
                {
                    software = "wps";
                }
                else if (defaultApp.Contains("excel"))
                {
                    software = "EXCEL";
                }
                else
                {
                    throw new ExcelException("当前计算机未安装 Microsoft Office 或 WPS Office，请取消选择【使用软件】选项".I18n("ExcelUtils_EA22B0AB"));
                }
            }
            else
            {
                software = software.Equals("WPS") ? "wps" : "EXCEL";
            }

            return software;
        }
        public static string GetProgIDExecutablePath(string progID)
        {
            try
            {
                // 从 ProgID 获取 CLSID
                string clsid = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($"\\{progID}\\CLSID").GetValue("").ToString();
                // 从 CLSID 获取 LocalServer32 路径
                string path = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($"\\CLSID\\{clsid}\\LocalServer32").GetValue("").ToString();

                return Regex.Match(path, @"[A-Z]:.*?.exe", RegexOptions.IgnoreCase)?.Value;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex.Message);
                return null;
            }
        }


        public static T Clock<T>(long maxMillisecondsToWait, int step, Func<T> action, Func<T, bool> successCondition)
        {
            bool flag = maxMillisecondsToWait == int.MaxValue;
            T val = default;
            T result = val;
            int num = 0;
            while (flag || num <= maxMillisecondsToWait)
            {
                DateTime dateTime = (flag ? DateTime.MinValue : DateTime.UtcNow);
                if (successCondition(result = action()))
                {
                    break;
                }

                num += ((!flag) ? (step + (int)(DateTime.UtcNow - dateTime).TotalMilliseconds) : 0);
                if (step > 0 && (flag || num <= maxMillisecondsToWait))
                {
                    Thread.Sleep(step);
                }

                result = val;
            }

            return result;
        }
        public static void TryFuncIgnoreException(Action action)
        {
            TryFuncIgnoreException(delegate
            {
                action();
                return true;
            });
        }

        public static T TryFuncIgnoreException<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "TryFuncIgnoreException");
                return default(T);
            }
        }
        public static IntPtr GetForegroundWindowPersistant(int millisecondsToPersist = 1500)
        {
            IntPtr lastKnownForegroundWindowHandle = IntPtr.Zero;
            int matchCount = 0;
            int desiredMatchCount = Math.Min(Math.Max(millisecondsToPersist / 50 - 1, 1), 3);
            Clock(millisecondsToPersist, 50, NativeUtils.GetForegroundWindow, delegate (IntPtr newHandle)
            {
                if (newHandle == IntPtr.Zero)
                {
                    return false;
                }

                if (matchCount == 0)
                {
                    matchCount = 1;
                    lastKnownForegroundWindowHandle = newHandle;
                    return matchCount == desiredMatchCount;
                }

                if (lastKnownForegroundWindowHandle != newHandle)
                {
                    matchCount = 0;
                    lastKnownForegroundWindowHandle = newHandle;
                    return false;
                }

                return ++matchCount == desiredMatchCount;
            });
            return lastKnownForegroundWindowHandle;
        }

        public static void DownloadImageAsync(string imageUrl, string savePath)
        {
            try
            {
                WebClient client = new WebClient();
                Stream stream = client.OpenRead(imageUrl);
                Bitmap bitmap;
                bitmap = new Bitmap(stream);

                if (bitmap != null)
                {
                    bitmap.Save(savePath);
                }

                stream.Flush();
                stream.Close();
                client.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError($"download image faild..{imageUrl}");
                throw new ExcelException("下载网络图片失败！".I18n("ExcelUtils_C2BED73A"));
            }
        }

        public static string GetRangeText(string workbook, string sheet, string startCol, int startRow, string endCol, int endRow, int mode = 1, Func<int, int, string> numberFormatProvider = null, Func<int, int, string> displayTextProvider = null, string password = null)
        {
            try
            {
                int sColIdx = ColumnLetterToIndex(startCol) - 1;
                int eColIdx = ColumnLetterToIndex(endCol) - 1;
                int sRowIdx = startRow - 1;
                int eRowIdx = endRow - 1;

                int targetColumnCount = eColIdx - sColIdx + 1;
                int targetRowCount = eRowIdx - sRowIdx + 1;

                logger.LogInformation($"ExcelDataReader GetRangeText workbook:{workbook}, sheet:{sheet}, range:{startCol}{startRow}:{endCol}{endRow}, mode:{mode}, rows:{targetRowCount}, columns:{targetColumnCount}");

                using (var stream = File.Open(workbook, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var readerConfiguration = string.IsNullOrWhiteSpace(password)
                        ? null
                        : new ExcelReaderConfiguration { Password = password };
                    using (var reader = readerConfiguration == null
                        ? ExcelReaderFactory.CreateReader(stream)
                        : ExcelReaderFactory.CreateReader(stream, readerConfiguration))
                    {
                        if (!MoveToWorksheet(reader, sheet))
                        {
                            throw new ExcelException(Localize($"未找到工作表: {sheet}", "ExcelUtils_3E90ED15"));
                        }

                        if (sRowIdx < 0 || eRowIdx >= reader.RowCount ||
                            sColIdx < 0 || eColIdx >= reader.FieldCount)
                        {
                            throw new ExcelException("区域超出表格范围".I18n("ExcelUtils_3A3D4998"));
                        }

                        logger.LogInformation($"ExcelDataReader selected sheet:{reader.Name}, rowCount:{reader.RowCount}, fieldCount:{reader.FieldCount}");

                        var formatContext = new ReaderFormatContext(workbook, reader.Name, startRow, startCol, numberFormatProvider, displayTextProvider);
                        if (mode == 1)//row
                        {
                            var rowData = ReadRangeTextRows(reader, sRowIdx, eRowIdx, sColIdx, eColIdx, formatContext)[0];
                            SerializerDataToBinaryFile(rowData);
                        }
                        else if (mode == 2 && !StreamingEnabled)
                        {
                            var targetRows = ReadRangeTextRows(reader, sRowIdx, eRowIdx, sColIdx, eColIdx, formatContext);
                            var colData = new string[targetRowCount];
                            for (int i = 0; i < targetRowCount; i++) colData[i] = targetRows[i][0];
                            SerializerDataToBinaryFile(colData);
                        }
                        else if (mode == 2)//列
                        {
                            int batchSize = ExcelConfigurations.Info.GlobalSetting.BatchSize;
                            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
                            var values = new List<string>(batchSize);
                            int currentRow = -1, total = 0;
                            while (currentRow < eRowIdx && reader.Read())
                            {
                                currentRow++;
                                if (currentRow < sRowIdx) continue;
                                values.Add(GetFormattedCellText(reader, currentRow, sColIdx, formatContext));
                                total++;
                                if (values.Count == batchSize)
                                {
                                    SerializerDataToBinaryFile(values.ToArray());
                                    values.Clear();
                                }
                            }
                            if (total != targetRowCount) throw new ExcelException("区域超出表格范围".I18n("ExcelUtils_3A3D4998"));
                            if (values.Count > 0) SerializerDataToBinaryFile(values.ToArray());
                        }
                        else // 读取整个区域
                        {
                            int batchSize = ExcelConfigurations.Info.GlobalSetting.BatchSize;
                            int totalTasks = (int)Math.Ceiling((double)targetRowCount / batchSize);
                            ReadRangeTextByBatch(reader, sRowIdx, eRowIdx, sColIdx, eColIdx, targetColumnCount, batchSize, totalTasks, formatContext);
                        }
                    }
                }

                return TEMP_FILEPATH;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                throw new ExcelException(Localize($"读取内容出错：{ex.Message}", "ExcelUtils_52DF7851"));
            }
        }

        private static bool MoveToWorksheet(IExcelDataReader reader, string sheet)
        {
            do
            {
                if (string.Equals(reader.Name, sheet, StringComparison.Ordinal))
                {
                    return true;
                }
            } while (reader.NextResult());

            return false;
        }

        private static List<string[]> ReadRangeTextRows(IExcelDataReader reader, int startRow, int endRow, int startColumn, int endColumn, ReaderFormatContext formatContext)
        {
            var rows = new List<string[]>(endRow - startRow + 1);
            int currentRow = -1;
            while (currentRow < endRow && reader.Read())
            {
                currentRow++;
                if (currentRow < startRow)
                {
                    continue;
                }

                var rowData = new string[endColumn - startColumn + 1];
                for (int c = startColumn; c <= endColumn; c++)
                {
                    rowData[c - startColumn] = GetFormattedCellText(reader, currentRow, c, formatContext);
                }
                rows.Add(rowData);
            }

            if (rows.Count != endRow - startRow + 1)
            {
                throw new ExcelException("区域超出表格范围".I18n("ExcelUtils_3A3D4998"));
            }

            return rows;
        }

        private static void ReadRangeTextByBatch(IExcelDataReader reader, int startRow, int endRow, int startColumn, int endColumn, int columnCount, int batchSize, int totalTasks, ReaderFormatContext formatContext)
        {
            var pool = ArrayPool<object>.Shared;
            object[] array = null;
            int currentRow = -1;
            int targetRowIndex = 0;
            int batchIndex = 0;
            int batchRowIndex = 0;
            int currentBatchRows = Math.Min(batchSize, endRow - startRow + 1);

            try
            {
                array = pool.Rent(currentBatchRows * columnCount);
                while (currentRow < endRow && reader.Read())
                {
                    currentRow++;
                    if (currentRow < startRow)
                    {
                        continue;
                    }

                    for (int c = startColumn; c <= endColumn; c++)
                    {
                        array[batchRowIndex * columnCount + c - startColumn] = GetFormattedCellText(reader, currentRow, c, formatContext);
                    }

                    targetRowIndex++;
                    batchRowIndex++;
                    if (batchRowIndex == currentBatchRows)
                    {
                        var data = new SerializeData { Rows = currentBatchRows, Columns = columnCount, Data = array };
                        SerializerDataToBinaryFile(data, totalTasks < 2 ? "" : $"{batchIndex + 1}-{totalTasks}");
                        pool.Return(array, true);
                        array = null;

                        batchIndex++;
                        batchRowIndex = 0;
                        int rowsLeft = endRow - startRow + 1 - targetRowIndex;
                        if (rowsLeft > 0)
                        {
                            currentBatchRows = Math.Min(batchSize, rowsLeft);
                            array = pool.Rent(currentBatchRows * columnCount);
                        }
                    }
                }

                if (targetRowIndex != endRow - startRow + 1)
                {
                    throw new ExcelException("区域超出表格范围".I18n("ExcelUtils_3A3D4998"));
                }
            }
            finally
            {
                if (array != null)
                {
                    pool.Return(array, true);
                }
            }
        }

        private static string GetFormattedCellText(IExcelDataReader reader, int rowIndex, int columnIndex, ReaderFormatContext formatContext)
        {
            object value = reader.GetValue(columnIndex);
            if (value == null || value == DBNull.Value)
            {
                return "";
            }

            string formatString = null;
            try
            {
                formatString = reader.GetNumberFormatString(columnIndex);
            }
            catch
            {
                formatString = null;
            }

            if (!string.IsNullOrWhiteSpace(formatString))
            {
                string overrideFormatString = TryGetNumberFormatOverride(value, formatString, formatContext, rowIndex, columnIndex);
                if (!string.IsNullOrWhiteSpace(overrideFormatString))
                {
                    formatString = overrideFormatString;
                }

                if (IsGeneralFormat(formatString))
                {
                    string _text = Convert.ToString(value, CultureInfo.CurrentCulture) ?? "";
                    LogReaderFormatSample(formatContext, "general-format", rowIndex, columnIndex, value, formatString, formatString, _text);
                    return TryGetDisplayTextOverride(value, formatString, _text, formatContext, rowIndex, columnIndex) ?? _text;
                }

                string text = TryFormatCellText(value, formatString, formatContext, rowIndex, columnIndex);
                if (text != null)
                {
                    return TryGetDisplayTextOverride(value, formatString, text, formatContext, rowIndex, columnIndex) ?? text;
                }
            }

            LogReaderFormatSample(formatContext, "fallback-without-format", rowIndex, columnIndex, value, formatString, null, null);
            string fallbackText = Convert.ToString(value, CultureInfo.CurrentCulture) ?? "";
            return TryGetDisplayTextOverride(value, formatString, fallbackText, formatContext, rowIndex, columnIndex) ?? fallbackText;
        }

        private static string TryGetDisplayTextOverride(object value, string formatString, string formattedText, ReaderFormatContext formatContext, int rowIndex, int columnIndex)
        {
            if (formatContext.DisplayTextProvider == null || !ShouldCheckDisplayText(value))
            {
                return null;
            }

            string decisionKey = $"{columnIndex}|{formatString ?? ""}";
            if (formatContext.DisplayTextFallbackDecisions.TryGetValue(decisionKey, out bool useDisplayText))
            {
                if (!useDisplayText)
                {
                    return null;
                }

                string cachedDisplayText = GetDisplayText(formatContext, rowIndex, columnIndex);
                return cachedDisplayText ?? formattedText;
            }

            string displayText = GetDisplayText(formatContext, rowIndex, columnIndex);
            if (displayText == null)
            {
                formatContext.DisplayTextFallbackDecisions[decisionKey] = false;
                return null;
            }

            bool shouldUseDisplayText = ShouldUseDisplayText(displayText, formattedText);
            formatContext.DisplayTextFallbackDecisions[decisionKey] = shouldUseDisplayText;
            LogReaderFormatSample(formatContext, shouldUseDisplayText ? "display-text-override" : "display-text-match", rowIndex, columnIndex, value, formatString, null, displayText);

            return shouldUseDisplayText ? displayText : null;
        }

        private static string GetDisplayText(ReaderFormatContext formatContext, int rowIndex, int columnIndex)
        {
            try
            {
                return formatContext.DisplayTextProvider(rowIndex + 1, columnIndex + 1);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"ExcelDataReader get COM display text failed cell:{ColumnIndexToLetter(columnIndex + 1)}{rowIndex + 1}");
                return null;
            }
        }

        private static bool ShouldUseDisplayText(string displayText, string formattedText)
        {
            if (displayText == null)
            {
                return false;
            }

            string normalizedDisplayText = displayText.Trim();
            string normalizedFormattedText = (formattedText ?? "").Trim();
            if (string.Equals(normalizedDisplayText, normalizedFormattedText, StringComparison.Ordinal))
            {
                return false;
            }

            return !string.IsNullOrEmpty(normalizedDisplayText);
        }

        private static string TryGetNumberFormatOverride(object value, string formatString, ReaderFormatContext formatContext, int rowIndex, int columnIndex)
        {
            if (formatContext.NumberFormatProvider == null || !IsGeneralFormat(formatString) || !IsNumericValue(value))
            {
                return null;
            }

            // 同列可以混合日期、数值等格式，交由 provider 判断区域一致性，不能按列推断并缓存。
            string overrideFormat = null;
            try
            {
                overrideFormat = formatContext.NumberFormatProvider(rowIndex + 1, columnIndex + 1);
            }
            catch (Exception ex)
            {
                LogReaderFormatSample(formatContext, "format-provider-failed", rowIndex, columnIndex, value, formatString, null, ex.Message);
            }

            if (string.IsNullOrWhiteSpace(overrideFormat) || IsGeneralFormat(overrideFormat))
            {
                LogReaderFormatSample(formatContext, "format-provider-general", rowIndex, columnIndex, value, formatString, overrideFormat, null);
                return null;
            }

            LogReaderFormatSample(formatContext, "format-provider-override", rowIndex, columnIndex, value, formatString, overrideFormat, null);
            return overrideFormat;
        }

        private static bool IsGeneralFormat(string formatString)
        {
            string normalizedFormat = formatString?.Trim();
            return string.IsNullOrWhiteSpace(formatString) ||
                   string.Equals(normalizedFormat, "General", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalizedFormat, "Normal", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalizedFormat, "G/通用格式", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalizedFormat, "通用格式", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalizedFormat, "常规", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNumericValue(object value)
        {
            return value is byte || value is sbyte ||
                   value is short || value is ushort ||
                   value is int || value is uint ||
                   value is long || value is ulong ||
                   value is float || value is double ||
                   value is decimal;
        }

        private static bool ShouldCheckDisplayText(object value)
        {
            return value != null && value != DBNull.Value &&
                   (IsNumericValue(value) || value is DateTime);
        }

        private static string TryFormatCellText(object value, string formatString, ReaderFormatContext formatContext, int rowIndex, int columnIndex)
        {
            string normalizedFormatString = NormalizeLocalizedDateFormat(formatString);
            string[] candidateFormats = normalizedFormatString == formatString
                ? new string[] { formatString }
                : new string[] { normalizedFormatString, formatString };

            foreach (string candidateFormat in candidateFormats)
            {
                try
                {
                    if (!formatContext.FormatCache.TryGetValue(candidateFormat, out NumberFormat numberFormat))
                    {
                        numberFormat = new NumberFormat(candidateFormat);
                        formatContext.FormatCache.Add(candidateFormat, numberFormat);
                    }

                    string text = numberFormat.Format(value, CultureInfo.CurrentCulture, false);
                    if (candidateFormat != formatString)
                    {
                        LogReaderFormatSample(formatContext, "normalized-format", rowIndex, columnIndex, value, formatString, candidateFormat, text);
                    }
                    else
                    {
                        LogReaderFormatSample(formatContext, "format-success", rowIndex, columnIndex, value, formatString, candidateFormat, text);
                    }
                    return text;
                }
                catch (Exception ex)
                {
                    LogReaderFormatSample(formatContext, "format-failed", rowIndex, columnIndex, value, formatString, candidateFormat, ex.Message);
                }
            }

            return null;
        }

        private static string NormalizeLocalizedDateFormat(string formatString)
        {
            if (string.IsNullOrWhiteSpace(formatString))
            {
                return formatString;
            }
            if (!LooksLikeDateTimeFormat(formatString))
            {
                return formatString;
            }

            var builder = new StringBuilder(formatString.Length + 8);
            bool inQuote = false;
            bool inBracket = false;
            for (int i = 0; i < formatString.Length; i++)
            {
                char ch = formatString[i];
                if (ch == '"')
                {
                    inQuote = !inQuote;
                    builder.Append(ch);
                    continue;
                }
                if (ch == '\\')
                {
                    builder.Append(ch);
                    if (i + 1 < formatString.Length)
                    {
                        builder.Append(formatString[++i]);
                    }
                    continue;
                }
                if (!inQuote && ch == '[')
                {
                    inBracket = true;
                    builder.Append(ch);
                    continue;
                }
                if (!inQuote && ch == ']')
                {
                    inBracket = false;
                    builder.Append(ch);
                    continue;
                }
                if (!inQuote && !inBracket && IsCjkLiteral(ch))
                {
                    builder.Append('"');
                    builder.Append(ch);
                    while (i + 1 < formatString.Length && IsCjkLiteral(formatString[i + 1]))
                    {
                        builder.Append(formatString[++i]);
                    }
                    builder.Append('"');
                    continue;
                }

                builder.Append(ch);
            }

            return builder.ToString();
        }

        private static bool LooksLikeDateTimeFormat(string formatString)
        {
            bool inQuote = false;
            bool inBracket = false;
            for (int i = 0; i < formatString.Length; i++)
            {
                char ch = formatString[i];
                if (ch == '"')
                {
                    inQuote = !inQuote;
                    continue;
                }
                if (ch == '\\')
                {
                    i++;
                    continue;
                }
                if (!inQuote && ch == '[')
                {
                    inBracket = true;
                    continue;
                }
                if (!inQuote && ch == ']')
                {
                    inBracket = false;
                    continue;
                }
                if (!inQuote && !inBracket)
                {
                    char token = char.ToLowerInvariant(ch);
                    if (token == 'y' || token == 'm' || token == 'd' || token == 'h' || token == 's')
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsCjkLiteral(char ch)
        {
            return ch >= '\u3400' && ch <= '\u9FFF' ||
                   ch >= '\uF900' && ch <= '\uFAFF';
        }

        private static void LogReaderFormatSample(ReaderFormatContext context, string reason, int rowIndex, int columnIndex, object value, string formatString, string candidateFormat, string result)
        {
            if (context.LogSamples >= MaxReaderFormatLogSamples)
            {
                return;
            }

            context.LogSamples++;
            string address = $"{ColumnIndexToLetter(columnIndex + 1)}{rowIndex + 1}";
            logger.LogDebug($"ExcelDataReader format {reason}, workbook:{context.Workbook}, sheet:{context.Sheet}, cell:{address}, valueType:{value?.GetType().FullName}, value:{Convert.ToString(value, CultureInfo.InvariantCulture)}, format:{formatString}, candidateFormat:{candidateFormat}, result:{result}");
        }

        private class ReaderFormatContext
        {
            public ReaderFormatContext(string workbook, string sheet, int startRow, string startColumn, Func<int, int, string> numberFormatProvider, Func<int, int, string> displayTextProvider)
            {
                Workbook = workbook;
                Sheet = sheet;
                StartRow = startRow;
                StartColumn = startColumn;
                NumberFormatProvider = numberFormatProvider;
                DisplayTextProvider = displayTextProvider;
            }

            public string Workbook { get; }
            public string Sheet { get; }
            public int StartRow { get; }
            public string StartColumn { get; }
            public Func<int, int, string> NumberFormatProvider { get; }
            public Func<int, int, string> DisplayTextProvider { get; }
            public int LogSamples { get; set; }
            public Dictionary<string, NumberFormat> FormatCache { get; } = new Dictionary<string, NumberFormat>();
            public Dictionary<string, bool> DisplayTextFallbackDecisions { get; } = new Dictionary<string, bool>();
        }
    }

    /// <summary>
    /// 筛选规则
    /// </summary>
    public class Criteria
    {
        public CriteriaOperator Operator { get; set; }
        public string Value { get; set; }
    }

    public enum CriteriaOperator
    {
        Equal = 11,
        NotEqual = 12,
        Gt = 13,
        Lt = 14,
        Ge = 15,
        Le = 16,

        Contion = 21,
        NotContion = 22,
        Empty = 23,
        NotEmpty = 24,
        StartWith = 25,
        NotStartWith = 26,
        EndWith = 27,
        NotEndWith = 28,

        DateEqual = 31,
        NotDateEqual = 32,
        DateLast = 33,
        DatePre = 34,
        DateBetween = 35,
    }
}

