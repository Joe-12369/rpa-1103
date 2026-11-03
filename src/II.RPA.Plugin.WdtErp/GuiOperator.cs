using FlaUI.Core.AutomationElements;
using FlaUI.Core.Logging;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.GuiBase;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Logger = II.RPA.Logging.Logger;

namespace II.RPA.Plugin.WdtErp
{
    public class GuiOperator : IWdtErpOperator, IGuiOperator_6_9_0
    {
        public ILogger logger = Logger.OfContext();
        private static readonly object ProcessInfoCacheLock = new object();
        private static IntPtr CachedRootWindowHandle = IntPtr.Zero;
        private static string CachedProcessName;
        private static int CachedProcessId;
        public static string ProcessName;
        public static int ProcessId;
        int IGuiOperator.Sort { get; set; }
        eGuiType IGuiOperator.GuiType => eGuiType.WdtErp;

        private IntPtr GetRootWindowHandle(AutomationElement rootElement)
        {
            if (rootElement?.Properties?.NativeWindowHandle?.IsSupported != true)
            {
                return IntPtr.Zero;
            }

            IntPtr nativeWindowHandle = rootElement.Properties.NativeWindowHandle.ValueOrDefault;
            return nativeWindowHandle;
        }

        private static void ResetCachedProcessContext()
        {
            CachedRootWindowHandle = IntPtr.Zero;
            CachedProcessName = null;
            CachedProcessId = 0;
        }

        private static bool TryRestoreCachedProcessContext()
        {
            lock (ProcessInfoCacheLock)
            {
                if (string.IsNullOrWhiteSpace(CachedProcessName) || CachedProcessId < 1)
                {
                    return false;
                }

                try
                {
                    var process = Process.GetProcessById(CachedProcessId);
                    if (!string.Equals(process.ProcessName, CachedProcessName, StringComparison.OrdinalIgnoreCase))
                    {
                        ResetCachedProcessContext();
                        return false;
                    }

                    ProcessName = CachedProcessName;
                    ProcessId = CachedProcessId;
                    return true;
                }
                catch (ArgumentException)
                {
                    ResetCachedProcessContext();
                    return false;
                }
                catch (InvalidOperationException)
                {
                    ResetCachedProcessContext();
                    return false;
                }
            }
        }

        private static bool TryReuseCachedProcessContext(IntPtr rootWindowHandle)
        {
            if (rootWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            lock (ProcessInfoCacheLock)
            {
                if (rootWindowHandle != CachedRootWindowHandle || string.IsNullOrWhiteSpace(CachedProcessName) || CachedProcessId < 1)
                {
                    return false;
                }

                try
                {
                    var process = Process.GetProcessById(CachedProcessId);
                    if (!string.Equals(process.ProcessName, CachedProcessName, StringComparison.OrdinalIgnoreCase))
                    {
                        ResetCachedProcessContext();
                        return false;
                    }

                    ProcessName = CachedProcessName;
                    ProcessId = CachedProcessId;
                    return true;
                }
                catch (ArgumentException)
                {
                    ResetCachedProcessContext();
                    return false;
                }
                catch (InvalidOperationException)
                {
                    ResetCachedProcessContext();
                    return false;
                }
            }
        }

        private void UpdateProcessContext(AutomationElement rootElement)
        {
            IntPtr rootWindowHandle = GetRootWindowHandle(rootElement);
            if (TryReuseCachedProcessContext(rootWindowHandle))
            {
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            var processInfo = rootElement.GetProcessInfo();
            stopwatch.Stop();

            if (processInfo == null)
            {
                ProcessName = null;
                ProcessId = 0;
                lock (ProcessInfoCacheLock)
                {
                    ResetCachedProcessContext();
                }
                logger.LogWarning($"Resolve WdtErp root process info returned null, rootHwnd:{rootWindowHandle}, elapsed:{stopwatch.ElapsedMilliseconds}ms");
                return;
            }

            ProcessName = processInfo.ProcessName;
            ProcessId = processInfo.ProcessId;
            logger.LogDebug($"Resolve WdtErp root process info, rootHwnd:{rootWindowHandle}, elapsed:{stopwatch.ElapsedMilliseconds}ms, process:{ProcessName}, pid:{ProcessId}");

            if (rootWindowHandle != IntPtr.Zero)
            {
                lock (ProcessInfoCacheLock)
                {
                    CachedRootWindowHandle = rootWindowHandle;
                    CachedProcessName = ProcessName;
                    CachedProcessId = ProcessId;
                }
            }
        }

        /// <summary>
        /// 从selector 中 提取 旺店通信息，可能会有多个不同的旺店通同时存在，所有每次都要解析
        /// </summary>
        /// <param name="selector"></param>
        private void SetWdtErpProcessInfo(string selector)
        {
            ProcessId = 0;

            if (selector.Contains("commonProps"))
            {
                var array = JObject.Parse(selector);
                ProcessName = array.SelectToken("$['commonProps']").Value<string>("processName");
                if (string.IsNullOrWhiteSpace(ProcessName))
                {
                    ProcessName = (string)array["selector"][0]["props"][0]["value"];
                }
            }
            else
            {
                var array = JArray.Parse(selector);
                ProcessName = array.First.Value<string>("window");
            }

            if (string.IsNullOrWhiteSpace(ProcessName) && !TryRestoreCachedProcessContext())
            {
                logger.LogWarning("WdtErp selector has no process name and no cached process context is available.");
            }

        }

        public IGuiNode[] QuerySelectorAll(GetElementFromSelectorParams args)
        {
            SetWdtErpProcessInfo(args.Selector);
            var nodes = SimilarElementHandler.Instance.QuerySelectorAll(args);
            nodes.RemoveAll(x => x == null);
            if (args.DiscernHidden)
            {
                return nodes.ToArray();
            }
            else
            {
                nodes.RemoveAll(x => x.BoundingRectangle.Width <= 0 && x.BoundingRectangle.Height <= 0);
                return nodes.ToArray();
            }
        }

        IGuiNode IGuiOperator.GetElementFromSelector(GetElementFromSelectorParams args)
        {
            var node = GetElementFromSelector(args.Selector);
            return node;
        }
        IGuiNode GetElementFromSelector(string selector)
        {
            SetWdtErpProcessInfo(selector);
            return WdtErpProxy.Instance.GetElementFromSelector(selector);
        }
        string IGuiOperator.GetElementFromSelectorAll(GetElementFromSelectorParams args)
        {
            throw new NotImplementedException();
        }
        bool IGuiOperator.InspectNode(System.Drawing.Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            UpdateProcessContext(rootElement);
            bool inspectStatus;
#if DEBUG
            lock (new object())
            {
                inspectStatus = WdtErpProxy.Instance.InspectNode(rootElement, pointElement, screenPoint, selector, out inspectNode);

            }
#else
                inspectStatus = WdtErpProxy.Instance.InspectNode(rootElement, pointElement, screenPoint, selector, out inspectNode);
#endif
            return inspectStatus;

        }
        bool IGuiOperator.IsCanUse(System.Drawing.Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {
            var isWdtApp = rootElement.ClassName.Contains("CoolWindow") && (pointElement.ClassName.Contains("CoolWindow") || pointElement.ClassName.Contains("HTMLAYOUT"));
            logger.LogInformation($"IsCanUse WdtErp {isWdtApp}");
            return isWdtApp;
        }
        void IGuiOperator.ReadyToCapture()
        {

        }
        void IGuiOperator.Release()
        {

        }
        public string BuildSelectors(BulidSelectorsParams @params)
        {
            throw new NotImplementedException();
        }

        public string BuildSelectors(ParamBuildSelectorsModel paramModel)
        {
            throw new NotImplementedException();
        }

        public string BuildSelectors(string jsonParams)
        {
            throw new NotImplementedException();
        }

        public List<List<string>> CollectDataBySelector(ParamCollectDataBySelectorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public List<List<string>> CollectData_v2(ParamDataCollect_V2Model paramModel)
        {
            var tableSelector = paramModel.DataCollectConfig.CollectionConfig.TableSelector;
            RPACommon common = new RPACommon();
            common.Selector = JArray.FromObject(tableSelector);
            common.CommonProps.ProcessName = paramModel.DataCollectConfig.PageInfo?.ProcessName;
            var node = GetElementFromSelector(common.ToJsonNoneFormating());
            string data = node.GetTableData("");
            return data.ToObject<List<List<string>>>();
        }

        public string GetBoundings(string jsonParams)
        {
            throw new NotImplementedException();
        }

        public IGuiNode GetElementById(GetElementByIdParams args)
        {
            throw new NotImplementedException();
        }

        public string GetElementsAll(GetElementsAllParams @params)
        {
            throw new NotImplementedException();
        }

        public List<SelectorProps> GetSameParentsProps(ParamCollectDataBySelectorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public List<SelectorProps> GetSameParentsProps_v2(ParamDataCollect_V2Model paramModel)
        {
            throw new NotImplementedException();
        }

        public void ScrollIntoView(string srcoll)
        {
            throw new NotImplementedException();
        }

        public List<JObject> GetSimilarOrDataCollectElements(GetElementsAllParams @params)
        {
            throw new NotImplementedException();
        }

        public bool VerifyWindowOfNode(GetElementFromSelectorParams args)
        {
            return true;
        }

        public string GetTargetElementInfo(GetTargetElementInfoModelParams @params)
        {
            throw new NotImplementedException();
        }
    }
}
