using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Microsoft.International.Converters.PinYinConverter;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Reflection;

namespace II.RPA.Plugin.GUI.ProcessCreator
{
    internal static class SomeUtils
    {
        private static readonly ILogger logger = Logger.OfContext();
        public static string SignHttpRequestToZBrain(string anUrl, Dictionary<string, object> dicParams = null)
        {
            if (dicParams == null)
            {
                dicParams = new Dictionary<string, object>();
            }
            Uri uri = new Uri(anUrl);
            string saltStr = EncryptUtils.MD5Encrypt("indeedRpaCVOfflineSalt-8b1a9c97275dedd0");
            DateTime DateNow = DateTime.Now;
            long t = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            string signString = $"{t}{saltStr}";
            //string paramsStr = string.Join("&", sgin_params.Select(o => $"{o.Key.ToUpper()}={(o.Value ?? "")}"));
            //signSourceStr = string.Format(signSourceStr, uri.AbsolutePath, sb_salt.ToString(), paramsStr);
            string sign = EncryptUtils.MD5Encrypt(signString);
            //dicParams_sort.Remove("t");
            var urlFormat = $"{anUrl}{(anUrl.IndexOf("?") > 0 ? "&" : "?")}sign={sign}&t={t}";
            return urlFormat;
        }

        public static bool IsWindowsContextMenu(JObject aSelectorJO)
        {
            try
            {
                JArray selectorJA = aSelectorJO["selector"] as JArray;
                JToken menuNode = null;
                if (selectorJA.Count == 1 || selectorJA.Count == 2)
                {
                    menuNode = selectorJA[0];
                }
                else if (selectorJA.Count >= 3)
                {
                    menuNode = selectorJA[1];
                }

                if (menuNode != null && menuNode["name"].ToString() == "Menu")
                {
                    if (menuNode["props"].Any(n => n["name"].ToString() == "class" && n["value"].ToString() == "#32768"))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "IsWindowsContextMenu error");
            }
            return false;
        }

        public static bool IsWindowsContextMenuNew(JObject aSelectorJO)
        {
            try
            {
                JArray selectorJA = aSelectorJO["selector"] as JArray;
                JToken menuNode = null;
                JToken secondMenuNode = null;
                if (selectorJA.Count == 2)
                {
                    menuNode = selectorJA[0];
                    secondMenuNode = selectorJA[1];
                    if (menuNode != null && menuNode["name"].ToString() == "Menu")
                    {
                        if (menuNode["props"].Any(n => n["name"].ToString() == "class" && n["value"].ToString() == "#32768"))
                        {
                            if (secondMenuNode != null && (secondMenuNode["props"].Any(n => n["name"].ToString() == "name" && (n["value"].ToString() == "New" || n["value"].ToString().StartsWith("新建")))))
                            {
                                return true;
                            }
                        }
                    }
                }
                else if (selectorJA.Count >= 3)
                {
                    menuNode = selectorJA[1];
                    if (menuNode != null && menuNode["name"].ToString() == "Menu")
                    {
                        if (menuNode["props"].Any(n => n["name"].ToString() == "class" && n["value"].ToString() == "#32768"))
                        {
                            if (menuNode != null && (menuNode["props"].Any(n => n["name"].ToString() == "name" && (n["value"].ToString() == "New" || n["value"].ToString().StartsWith("新建")))))
                            {
                                return true;
                            }
                        }
                    }

                }
               
                return false;

            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "IsWindowsContextMenu error");
            }
            return false;
        }

        public static bool IsWebElement(eGuiType guiType)
        {
            if (guiType == eGuiType.Chrome
                || guiType == eGuiType.IE
                || guiType == eGuiType.FireFox
                || guiType == eGuiType.Web360se
                || guiType == eGuiType.Edge
                || guiType == eGuiType.Sogou)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static double TransformToPixels(Visual aVisual)
        {
            Matrix matrix;
            var source = PresentationSource.FromVisual(aVisual);
            if (source != null)
            {
                matrix = source.CompositionTarget.TransformToDevice;
            }
            else
            {
                using (var src = new HwndSource(new HwndSourceParameters()))
                {
                    matrix = src.CompositionTarget.TransformToDevice;
                }
            }
            return matrix.M11;
        }

        public static Dictionary<string, string> GetSubDirectory(string aFolderName)
        {
            Dictionary<string, string> subFolderNames = new Dictionary<string, string>();
            DirectoryInfo theFolder = new DirectoryInfo(aFolderName);
            var subFolders = theFolder.GetDirectories();
            if (subFolders != null && subFolders.Any())
            {
                foreach (var sub in subFolders)
                {
                    if (!subFolderNames.ContainsKey(sub.Name))
                    {
                        subFolderNames.Add(sub.Name, sub.FullName);
                    }
                }
            }
            return subFolderNames;
        }
        public static Dictionary<string, FileSystemInfo> GetSubFileSystemInfos(string aFolderName)
        {
            Dictionary<string, FileSystemInfo> subFileSystemInfos = new Dictionary<string, FileSystemInfo>();
            DirectoryInfo theFolder = new DirectoryInfo(aFolderName);
            var fileInfos = theFolder.GetFileSystemInfos();
            if (fileInfos != null && fileInfos.Any())
            {
                foreach (var sub in fileInfos)
                {
                    if (!subFileSystemInfos.ContainsKey(sub.FullName))
                    {
                        subFileSystemInfos.Add(sub.FullName, sub);
                    }
                }
            }
            return subFileSystemInfos;
        }

        public static void TraceThreadPool(string aTraceKey = null,
            [CallerMemberName] string aCallerMemberName = null, 
            [CallerFilePath] string aCallerFilePath = null, 
            [CallerLineNumber] int aCallerLineNumber = -1)
        {
            try
            {
                logger.LogDebug("#TraceThreadPool Start#");
                if (!string.IsNullOrEmpty(aTraceKey))
                {
                    logger.LogDebug($"TraceKey:[{aTraceKey}]");
                }
                logger.LogDebug($"{nameof(aCallerFilePath)}:[{aCallerFilePath}]");
                logger.LogDebug($"{nameof(aCallerMemberName)}:[{aCallerMemberName}]");
                logger.LogDebug($"{nameof(aCallerLineNumber)}:[{aCallerLineNumber}]");

                ThreadPool.GetAvailableThreads(out int availableWorkerThreads,
                        out int availableCompletionPortThreads);
                ThreadPool.GetMaxThreads(out int maxWorkerThreads,
                    out int maxCompletionPortThreads);
                ThreadPool.GetMinThreads(out int minWorkerThreads,
                    out int minCompletionPortThreads);


                logger.LogDebug($"{nameof(availableWorkerThreads)}:[{availableWorkerThreads}]");
                //logger.LogDebug($"{nameof(availableCompletionPortThreads)}:[{availableCompletionPortThreads}]");
                //logger.LogDebug($"{nameof(maxWorkerThreads)}:[{maxWorkerThreads}]");
                //logger.LogDebug($"{nameof(maxCompletionPortThreads)}:[{maxCompletionPortThreads}]");
                //logger.LogDebug($"{nameof(minWorkerThreads)}:[{minWorkerThreads}]");
                //logger.LogDebug($"{nameof(minCompletionPortThreads)}:[{minCompletionPortThreads}]");


            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "TraceThreadPool Error");
            }
            finally
            {
                logger.LogDebug("#TraceThreadPool Over#");
            }
        }
        public static string GetDescription(this Enum value, Boolean nameInstead = true)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name == null)
            {
                return null;
            }

            FieldInfo field = type.GetField(name);
            DescriptionAttribute attribute = System.Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;

            if (attribute == null && nameInstead == true)
            {
                return name;
            }
            return attribute?.Description;
        }
    }
    public class MyDictionaryComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            //throw new NotImplementedException();
            return x != y;
        }

        public int GetHashCode(string obj)
        {
            //throw new NotImplementedException();
            return obj.GetHashCode();
        }
    }
}

