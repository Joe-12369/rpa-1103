using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.WinAutomation.CommonUtility;
using FlaUI.Core.AutomationElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Drawing;
using System.Data;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using II.RPA.Plugin.Core.Entities;
using FlaUI.Core.WindowsAPI;

namespace II.RPA.Plugin.Core
{
    public static class Extensions
    {
        private static ILogger logger = Logger.OfContext();

        #region JSON对象相关
        /// <summary>
        /// 获取指定指定类型的值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jObject"></param>
        /// <param name="key">key</param>
        /// <returns></returns>
        public static T GetValue<T>(this JObject jObject, string key)
        {
            try
            {
                return (T)Convert.ChangeType(jObject.SelectToken(key), typeof(T));
            }
            catch (Exception)
            {
                return default(T);
            }
        }
        public static bool TryGetValue<T>(this JObject jObject, string propertyName, out T value)
        {
            JProperty jProperty = jObject.Property(propertyName, StringComparison.Ordinal);
            if (jProperty == null)
            {
                value = default(T);
                return false;
            }
            value = jProperty.Value.As<T>();
            return true;
        }
        public static bool TryGetValueByPath<T>(this JObject jObject, string key, out T val)
        {
            try
            {
                JToken token = jObject.SelectToken(key);
                if (token == null)
                {
                    val = default(T);
                    logger.LogDebug($"TryGetValueByPath 未找到路径:{key}");
                    return false;
                }
                val = token.ToObject<T>();
                return true;
            }
            catch (Exception ex)
            {
                val = default(T);
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"TryGetValueByPath 解析路径:{key} 异常");
                return false;
            }
        }
        #endregion

        #region 类型转还
        /// <summary>
        /// 将对象转换为指定类型，转换失败返回默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jObject"></param>
        /// <returns></returns>
        public static T As<T>(this object jObject, T defaultVal)
        {
            try
            {
                return (T)Convert.ChangeType(jObject, typeof(T));
            }
            catch
            {
                return defaultVal;
            }
        }
        /// <summary>
        /// 将对象转换为指定类型，转换失败返回默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jObject"></param>
        /// <returns></returns>
        public static T As<T>(this object jObject)
        {
            try
            {
                return (T)Convert.ChangeType(jObject, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }
        #endregion

        #region 时间戳
        /// <summary>
        /// 时间戳计时开始时间
        /// </summary>
        private static DateTime timeStampStartTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// DateTime转换为10位时间戳（单位：秒）
        /// </summary>
        /// <param name="dateTime"> DateTime</param>
        /// <returns>10位时间戳（单位：秒）</returns>
        public static string ToTimeStamp(this DateTime dateTime)
        {
            return (dateTime.ToUniversalTime() - timeStampStartTime).TotalSeconds.ToString();
        }

        /// <summary>
        /// DateTime转换为13位时间戳（单位：毫秒）
        /// </summary>
        /// <param name="dateTime"> DateTime</param>
        /// <returns>13位时间戳（单位：毫秒）</returns>
        public static string ToLongTimeStamp(this DateTime dateTime)
        {
            return ((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000).ToString();
            //return (dateTime.ToUniversalTime() - timeStampStartTime).TotalMilliseconds.As<int>().ToString();
        }

        /// <summary>
        /// 10位时间戳（单位：秒）转换为DateTime
        /// </summary>
        /// <param name="timeStamp">10位时间戳（单位：秒）</param>
        /// <returns>DateTime</returns>
        public static DateTime ToDateTime(this long timeStamp)
        {
            return timeStampStartTime.AddSeconds(timeStamp).ToLocalTime();
        }

        /// <summary>
        /// 13位时间戳（单位：毫秒）转换为DateTime
        /// </summary>
        /// <param name="longTimeStamp">13位时间戳（单位：毫秒）</param>
        /// <returns>DateTime</returns>
        public static DateTime ToDateTimeByLongTimeStamp(this long longTimeStamp)
        {
            return timeStampStartTime.AddMilliseconds(longTimeStamp).ToLocalTime();
        }
        #endregion


        public static Point GetRandomPoint(this Rectangle rect)
        {
            try
            {
                // 边缘缓冲区：避免点击到元素边界（可根据实际情况调整数值）
                const int edgeBuffer = 2;

                // 验证有效区域是否足够生成坐标
                if (rect.Width <= edgeBuffer * 2 || rect.Height <= edgeBuffer * 2)
                    throw new InvalidOperationException("矩形尺寸过小，无法生成有效坐标".I18n("Extensions_EAD6F978"));

                System.Random rand = new System.Random(Guid.NewGuid().GetHashCode());

                // 修正坐标生成范围（排除边缘）
                int x = rand.Next(
                    rect.X + edgeBuffer,         // 起始点向右偏移2像素
                    rect.X + rect.Width - edgeBuffer  // 终止点向左偏移2像素
                );

                int y = rand.Next(
                    rect.Y + edgeBuffer,
                    rect.Y + rect.Height - edgeBuffer
                );

                return new System.Drawing.Point(x, y);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"获取随机坐标异常，使用{rect}异常");

                // 异常时返回安全区域中心点（避免边缘）
                return new System.Drawing.Point(
                    rect.X + rect.Width / 2,
                    rect.Y + rect.Height / 2
                );
            }
        }

        #region Pointl类型转换
        public static System.Drawing.Point ToDrawingPoint(this System.Windows.Point point)
        {
            return new System.Drawing.Point((int)point.X, (int)point.Y);
        }
        public static System.Windows.Point ToWindowsPoint(this System.Drawing.Point point)
        {
            return new System.Windows.Point((int)point.X, (int)point.Y);
        }
        #endregion

        #region Rect 相关
        public static bool IsOverlap(this Rectangle a, Rectangle b)
        {
            int num = Math.Max(a.X, b.X);
            int num2 = Math.Min(a.X + a.Width, b.X + b.Width);
            int num3 = Math.Max(a.Y, b.Y);
            int num4 = Math.Min(a.Y + a.Height, b.Y + b.Height);
            if (num2 >= num && num4 >= num3)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static RpaRect ToRpaRect(this Rectangle a)
        {
            RpaRect rc = new RpaRect();
            rc.x = a.X;
            rc.y = a.Y;
            rc.width = a.Width;
            rc.height = a.Height;
            return rc;
        }
        public static Rectangle ToRectangle(this RpaRect a)
        {
            Rectangle rc = new Rectangle();
            rc.X = a.x;
            rc.Y = a.y;
            rc.Width = a.width;
            rc.Height = a.height;
            return rc;
        }
        #endregion

        #region 数据集合类型转换
        //public static DataTable ColumnsDataToDataTable(this List<List<string>> dataList)
        //{
        //    int rowNum = dataList.Max(d => d.Count);

        //}
        /// <summary>
        /// 将集合数据转换为二维数组，其中集合中的每一个元素，代表一列数据的集合，即List<string>是某一列的数据
        /// </summary>
        /// <param name="dataList"></param>
        /// <returns></returns>
        public static string[,] ColumnsDataToTwoDimensionalArray(this List<List<string>> dataList)
        {
            int columnNum = dataList.Max(d => d.Count);
            string[,] arr = new string[dataList.Count, columnNum];
            for (int i = 0; i < dataList.Count; i++)
            {
                for (int j = 0; j < dataList[i].Count; j++)
                {
                    arr[i, j] = dataList[i][j];
                }
            }
            return arr;
        }
        /// <summary>
        ///  列数据转为 行数据，即返回值中 每一个List<string>对应一行列数据
        /// </summary>
        /// <param name="dataList">列数据集  每一个List<string>对应一列数据</param>
        /// <returns></returns>
        public static List<List<string>> ColumnsDataToRowsData(this List<List<string>> dataList)
        {
            if (dataList == null | dataList.Count == 0)
            {
                return dataList;
            }
            int rowsCount = dataList.Max(d => d.Count);
            List<List<string>> list = new List<List<string>>();
            for (int i = 0; i < rowsCount; i++)
            {
                var rowDataList = new List<string>();
                for (int j = 0; j < dataList.Count; j++)
                {
                    if (dataList[j].Count > i)
                    {
                        rowDataList.Add(dataList[j][i] ?? "");
                    }
                    else
                    {
                        rowDataList.Add("");
                    }
                }
                list.Add(rowDataList);
            }
            return list;
        }
        /// <summary>
        /// 行数据转为 列数据，即返回值中 每一个List<string>对应一列数据
        /// </summary>
        /// <param name="dataList">行数据集  每一个List<string>对应一行数据</param>
        /// <returns></returns>
        public static List<List<string>> RowsDataToColumnsData(this List<List<string>> dataList)
        {
            if (dataList == null | dataList.Count == 0)
            {
                return dataList;
            }
            int columnCount = dataList.First().Count();
            List<List<string>> list = new List<List<string>>();
            for (int i = 0; i < columnCount; i++)
            {
                list.Add(new List<string>());
            }
            for (int i = 0; i < dataList.Count; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    if (dataList[i].Count > j)
                    {
                        list[j].Add(dataList[i][j] ?? string.Empty);
                    }
                    else
                    {
                        list[j].Add(string.Empty);
                    }
                }
            }
            return list;
        }
        #endregion

        #region Process
        public static void Dispose(this Process[] processes)
        {
            for (int i = 0; i < processes.Length; i++)
            {
                try
                {
                    processes[i]?.Dispose();
                }
                catch
                {

                }
            }
        }
        public static string GetExeFilePath(this Process process)
        {
            try
            {
                //// 获取exe文件的完整路径
                var fileNameBuilder = new StringBuilder(1024 * 2);
                uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
                string exeFilePath = NativeMethods.QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, ref bufferLength) ? fileNameBuilder.ToString() : string.Empty;
                return exeFilePath;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"使用QueryFullProcessImageName获取路径异常，将尝试使用WMI");
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"使用QueryFullProcessImageName获取路径异常，将尝试使用WMI");
            }
            try
            {
                string query = $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {process.Id}";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject proc in searcher.Get())
                    {
                        return proc["ExecutablePath"]?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"使用WMI获取路径异常，获取应用地址失败");
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"使用WMI获取路径异常，将尝试使用WMI");
            }
            return string.Empty;
        }
        public static string GetProductVersion(this Process process)
        {
            var exeFilePath = process.GetExeFilePath();
            // 获取exe文件的产品版本号
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(exeFilePath);
            return versionInfo.ProductVersion;
        }
        #endregion

        #region RPAException 扩展
        public static bool IsRPAException(this Exception ex, eErrorMessageType errorMessageType, out RPAException rpaException)
        {
            rpaException = null;
            if (!(ex is RPAException))
            {
                return false;
            }
            rpaException = ex as RPAException;
            return rpaException.ErrorMessageType == errorMessageType;
        }
        public static bool ContainsIgnoreCase(this string value1, string value2)
        {
            return value1.IndexOf(value2, StringComparison.OrdinalIgnoreCase) != -1;
        }
        #endregion

        #region string
        /// <summary>
        /// 等于或正则匹配，忽略大小写
        /// </summary>
        /// <param name="str"></param>
        /// <param name="value">文本 或 pattern</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public static bool EqualsOrIsMatch(this string str, string value, bool ignoreCase = false)
        {
            if (str.Equals(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                return true;
            }
            else
            {
                Regex regex = new Regex(value, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
                return regex.IsMatch(str);
            }
        }
        /// <summary>
        /// 正则匹配，忽略大小写
        /// </summary>
        /// <param name="str"></param>
        /// <param name="value">文本 或 pattern</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public static bool IsMatch(this string str, string value, bool ignoreCase = false)
        {
            Regex regex = new Regex(value, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            return regex.IsMatch(str);
        }

        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);
        #endregion

        #region Predicate<T>
        public static Predicate<T> Combine<T>(this Predicate<T> predicate, Predicate<T> p)
        {
            if (p == null)
                return predicate;
            return (Predicate<T>)Delegate.Combine(predicate, p);
        }
        public static Predicate<T> Append<T>(this Predicate<T> predicate, Predicate<T> p)
        {
            if (p == null)
                return predicate;
            return (Predicate<T>)Delegate.Combine(p, predicate);
        }
        #endregion
        /// <summary>
        /// 对象处理 返回处理后的对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static T Apply<T>(this T obj, Action<T> action)
        {
            action?.Invoke(obj);
            return obj;
        }
        public static IEnumerable<T> WhereDelay<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate,
        CancellationToken token = default)
        {
            foreach (var item in source)
            {
                if (token.IsCancellationRequested) yield break;
                if (predicate(item))
                    yield return item;
            }
        }
        public static string MyVideos
        {
            get
            {
                var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(dir)))
                    System.IO.Directory.CreateDirectory(dir);
                return dir;
            }
        }
        public static string MyDocuments
        {
            get
            {
                var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (!string.IsNullOrEmpty(dir))
                {
                    if (!System.IO.Directory.Exists(System.IO.Path.Combine(dir)))
                        System.IO.Directory.CreateDirectory(dir);
                }
                else
                {
                    string filename = "settings.json";
                    var fi = new System.IO.FileInfo(filename);
                    return System.IO.Path.GetDirectoryName(fi.FullName);

                }
                return dir;
            }
        }
        public static string MyPictures
        {
            get
            {
                var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(dir)))
                    System.IO.Directory.CreateDirectory(dir);
                return dir;
            }
        }
        public static string UserDirectory
        {
            get
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenRPA");
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(dir)))
                    System.IO.Directory.CreateDirectory(dir);
                return dir;
            }
        }
        private static string _ProjectsDirectory = null;
        public static string ProjectsDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_ProjectsDirectory))
                {
                    //var asm = System.Reflection.Assembly.GetEntryAssembly();
                    //var filepath = asm.CodeBase.Replace("file:///", "");
                    //var path = System.IO.Path.GetDirectoryName(filepath);
                    //if (path.ToLower().Contains("program")) path = System.IO.Path.Combine(MyDocuments, "OpenRPA");
                    var path = System.IO.Path.Combine(MyDocuments, "OpenRPA");
                    if (!System.IO.Directory.Exists(System.IO.Path.Combine(path))) System.IO.Directory.CreateDirectory(path);
                    _ProjectsDirectory = path;

                }
                return _ProjectsDirectory;
            }
            set
            {
                _ProjectsDirectory = value;
            }
        }
        public static string PluginsDirectory
        {
            get
            {
                var asm = System.Reflection.Assembly.GetEntryAssembly();
                var filepath = asm.CodeBase.Replace("file:///", "");
                var path = System.IO.Path.GetDirectoryName(filepath);
                return path;
            }
        }
        public static string DataDirectory
        {
            get
            {
                //var asm = System.Reflection.Assembly.GetEntryAssembly();
                //var filepath = asm.CodeBase.Replace("file:///", "");
                //var path = System.IO.Path.GetDirectoryName(filepath);
                // if (path.ToLower().Contains("program")) path = UserDirectory;
                //return path;
                return UserDirectory;
            }
        }
        static public string ResourceAsString(this Type type, string resourceName)
        {
            // string[] names = typeof(Extensions).Assembly.GetManifestResourceNames();
            string[] names = type.Assembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                if (name.EndsWith(resourceName))
                {
                    using (var s = type.Assembly.GetManifestResourceStream(name))
                    {
                        using (var reader = new System.IO.StreamReader(s))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
                else
                {
                    try
                    {
                        var set = new System.Resources.ResourceSet(type.Assembly.GetManifestResourceStream(names[0]));
                        foreach (System.Collections.DictionaryEntry resource in set)
                        {
                            // Log.Information("\n[{0}] \t{1}", resource.Key, resource.Value);
                            if (((string)resource.Key).EndsWith(resourceName.ToLower()))
                            {
                                using (var reader = new System.IO.StreamReader(resource.Value as System.IO.Stream))
                                {
                                    return reader.ReadToEnd();
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex.ToString());
                    }
                }
            }
            return null;
        }
        static public string ResourceAsString(string resourceName)
        {
            return ResourceAsString(typeof(Extensions), resourceName);
        }
        public static System.Windows.Media.Imaging.BitmapFrame GetImageSourceFromResource(string resourceName)
        {
            string[] names = typeof(Extensions).Assembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                if (name.EndsWith(resourceName))
                {
                    return System.Windows.Media.Imaging.BitmapFrame.Create(typeof(Extensions).Assembly.GetManifestResourceStream(name));
                }
            }
            return null;
        }
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source) action(item);
        }
        public static string JParse(this string o)
        {
            if (o == null) return null;
            string result = Newtonsoft.Json.JsonConvert.ToString(o);
            result = result.Substring(1, result.Length - 2);
            return result;
        }
        public static Image DataURIToImage(this string data)
        {
            var bytes = Convert.FromBase64String(data.Replace("data:image/png;base64,", ""));
            using (var ms = new MemoryStream(bytes))
            {
                return Image.FromStream(ms);
            }
        }
        public static string ReplaceEnvironmentVariable(this string filename)
        {
            var USERPROFILE = Environment.GetEnvironmentVariable("USERPROFILE");
            var windir = Environment.GetEnvironmentVariable("windir");
            var SystemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            var PUBLIC = Environment.GetEnvironmentVariable("PUBLIC");

            if (!string.IsNullOrEmpty(USERPROFILE)) filename = filename.Replace(USERPROFILE, "%USERPROFILE%");
            if (!string.IsNullOrEmpty(windir)) filename = filename.Replace(windir, "%windir%");
            if (!string.IsNullOrEmpty(SystemRoot)) filename = filename.Replace(SystemRoot, "%SystemRoot%");
            if (!string.IsNullOrEmpty(PUBLIC)) filename = filename.Replace(PUBLIC, "%PUBLIC%");

            var ProgramData = Environment.GetEnvironmentVariable("ProgramData");
            if (!string.IsNullOrEmpty(ProgramData)) filename = filename.Replace(ProgramData, "%ProgramData%");
            var ProgramFilesx86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (!string.IsNullOrEmpty(ProgramFilesx86)) filename = filename.Replace(ProgramFilesx86, "%ProgramFiles(x86)%");
            var ProgramFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            if (!string.IsNullOrEmpty(ProgramFiles)) filename = filename.Replace(ProgramFiles, "%ProgramFiles%");
            var LOCALAPPDATA = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(LOCALAPPDATA)) filename = filename.Replace(LOCALAPPDATA, "%LOCALAPPDATA%");
            var APPDATA = Environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrEmpty(APPDATA)) filename = filename.Replace(APPDATA, "%APPDATA%");


            //var = Environment.GetEnvironmentVariable("");
            //if (!string.IsNullOrEmpty()) filename = filename.Replace(, "%%");

            return filename;
        }
        public static Task WaitOneAsync(this System.Threading.WaitHandle waitHandle)
        {
            if (waitHandle == null) throw new ArgumentNullException("waitHandle");
            var tcs = new TaskCompletionSource<bool>();
            var rwh = System.Threading.ThreadPool.RegisterWaitForSingleObject(waitHandle, delegate { tcs.TrySetResult(true); }, null, -1, true);
            var t = tcs.Task;
            t.ContinueWith((antecedent) => rwh.Unregister(null));
            return t;
        }
        public static bool HasInvalidCharacters(this string input)
        {
            const string invalidRegex = "[^ -~\u2E80-\u2FDF\u3040-\u318F\u31A0-\u31BF\u31F0-\u31FF\u3400-\u4DB5\u4E00-\u9FFF\uA960-\uA97F\uAC00-\uD7FF\u3002\u00a5\uff1f\uff01\uff0c\u3001\uff1b\uff1a\u201c\u201d\u2018\u2019\uff08\uff09\u300a\u300b\u3008\u3009\u3010\u3011\u300e\u300f\u300c\u300d\ufe43\ufe44\u3014\u3015\u2026\u2014\uff5e\ufe4f\uffe5\u00a5]+";

            if (!Regex.Match(input, invalidRegex).Success)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                string result = Encoding.UTF8.GetString(bytes);
                return !string.Equals(input, result);
            }
            return true;
        }


        public const int QueryLimitedInformation = 0x1000;
        public const int ERROR_INSUFFICIENT_BUFFER = 0x7a;
        public const int ERROR_SUCCESS = 0x0;
        public const int APPMODEL_ERROR_NO_APPLICATION = 15703;
        public static string[] ParseCommandLine(string commandLine)
        {
            List<string> arguments = new List<string>();
            bool stringIsQuoted = false;
            string argString = "";
            if (commandLine != null)
            {
                for (int c = 0; c < commandLine.Length; c++)  //process string one character at a tie
                {
                    if (commandLine.Substring(c, 1) == "\"")
                    {
                        if (stringIsQuoted)  //end quote so populate next element of list with constructed argument
                        {
                            arguments.Add(argString);
                            argString = "";
                        }
                        else
                        {
                            stringIsQuoted = true; //beginning quote so flag and scip
                        }
                    }
                    else if (commandLine.Substring(c, 1) == "".PadRight(1))
                    {
                        if (stringIsQuoted)
                        {
                            argString += commandLine.Substring(c, 1); //blank is embedded in quotes, so preserve it
                        }
                        else if (argString.Length > 0)
                        {
                            arguments.Add(argString);  //non-quoted blank so add to list if the first consecutive blank
                        }
                    }
                    else
                    {
                        argString += commandLine.Substring(c, 1);  //non-blan character:  add it to the element being constructed
                    }
                }
            }
            return arguments.ToArray();
        }
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hHandle);
        [DllImport("kernel32.dll")]
        public static extern int GetApplicationUserModelId(
            IntPtr hProcess,
            ref uint AppModelIDLength,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sbAppUserModelID);

        const int WM_QUIT = 0x12;

        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        //
        // GetGUIThreadInfo
        //

        public const int GUI_CARETBLINKING = 0x00000001;
        public const int GUI_INMOVESIZE = 0x00000002;
        public const int GUI_INMENUMODE = 0x00000004;
        public const int GUI_SYSTEMMENUMODE = 0x00000008;
        public const int GUI_POPUPMENUMODE = 0x00000010;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct GUITHREADINFO
        {
            public int cbSize;
            public int dwFlags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rc;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetGUIThreadInfo(int idThread, ref GUITHREADINFO guiThreadInfo);

        [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "PostThreadMessageW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        public static readonly uint WM_MY_CUSTOM_MESSAGE_WPF__MonForQuitThread = RegisterWindowMessage("MyWpfCustomMessage_MonForQuitThread");

        public static void PostQuitGuiThread(this Process process)
        {
            if (process != null)
            {
                foreach (ProcessThread thd in process.Threads)
                {
                    var threadId = thd.Id;
                    GUITHREADINFO guiThreadInfo = new GUITHREADINFO();
                    guiThreadInfo.cbSize = Marshal.SizeOf(guiThreadInfo);
                    if (GetGUIThreadInfo(threadId, ref guiThreadInfo))
                    {
                        PostThreadMessage((uint)threadId, WM_MY_CUSTOM_MESSAGE_WPF__MonForQuitThread, default(IntPtr), default(IntPtr));
                    }
                }
            }
        }
    }

    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string Filename { get; set; }
        public string ProcessName { get; set; }
        public string Arguments { get; set; }
        public string ApplicationUserModelId { get; set; }
        public bool IsImmersiveProcess { get; set; }
    }
}

