using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FlaUI.Core.AutomationElements;
using II.RPA.Logging.Formatting;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.Windows
{
    internal static class DingdingDataContext
    {
        private static ILogger logger = Logger.OfContext();

        private static Dictionary<string, List<DingdingElement>> CacheList = new Dictionary<string, List<DingdingElement>>();

        public static void AddDingdingElement(string key, DingdingElement element)
        {
            lock (CacheList)
            {
                if (!CacheList.ContainsKey(key))
                {
                    CacheList.Add(key, new List<DingdingElement>());
                }
                CacheList[key].Add(element);
            }
        }

        public static AutomationElement GetElement(string key, Point point)
        {
            List<DingdingElement> list = null;
            lock (CacheList)
            {
                if (CacheList.ContainsKey(key))
                {
                    list = CacheList[key].ToList();
                }
            }
            if (list != null && list.Count > 0)
            {
                var matches = list.Where(x => x.ContainPoint(point)).ToList();

                var element = matches.OrderByDescending(x => x.BoundRectangle.Width).LastOrDefault();
                logger.LogDebug(LogBlock.NullCheck(element?.AutomationElement, "element?.AutomationElement"));
                return element?.AutomationElement;
            }
            return null;
        }

        public static void InitCache()
        {
            lock (CacheList)
            {
                CacheList.Clear();
            }
        }
    }

    internal static class WeChatDataContext
    {
        private static ILogger logger = Logger.OfContext();

        private static Dictionary<int, Dictionary<string, List<WeChatElement>>> CacheList = new Dictionary<int, Dictionary<string, List<WeChatElement>>>();

        internal static int CurrentProcessId { get; set; }

        internal static bool IsWeChatWorkingProcess
        {
            get; set;
        }

        internal static void AddWeChatElement(int processID, string key, WeChatElement element)
        {
            lock (CacheList)
            {
                if (!CacheList.ContainsKey(processID))
                {
                    CacheList.Add(processID, new Dictionary<string, List<WeChatElement>>() { { key, new List<WeChatElement>() } });
                }

                if (!CacheList[processID].ContainsKey(key))
                {
                    CacheList[processID].Add(key, new List<WeChatElement>());
                }

                CacheList[processID][key].Add(element);
            }
        }

        internal static AutomationElement GetElement(int processId, string key, Point point)
        {
            List<WeChatElement> list = null;
            lock (CacheList)
            {
                if (CacheList.ContainsKey(processId))
                {
                    if (CacheList[processId].ContainsKey(key))
                    {
                        list = CacheList[processId][key].ToList();
                    }
                }
            }
            if (list != null && list.Count > 0)
            {
                logger.LogDebug($"list.Count：{list.Count}point.X{point.X}point.Y{point.Y}");
                var matches = list.Where(x => x.ContainPoint(point));
                var element = matches.OrderBy(x => x.SearchDepth).LastOrDefault();
                logger.LogDebug(LogBlock.NullCheck(element?.AutomationElement, "element?.AutomationElement"));
                return element?.AutomationElement;
            }
            return null;
        }

        /// <summary>
        /// 清除缓存，并不会影响正在使用是缓存，除非调用SaveCache()
        /// </summary>
        internal static void InitCache()
        {
            lock (CacheList)
            {
                CacheList.Clear();
            }
        }
    }

    internal class DingdingElement
    {
        public DingdingElement(AutomationElement automationElement, int searchDepth)
        {
            this.AutomationElement = automationElement;
            this.SearchDepth = searchDepth;
            this.BoundRectangle = new Rectangle(automationElement.BoundingRectangle.X, automationElement.BoundingRectangle.Y, automationElement.BoundingRectangle.Width, automationElement.BoundingRectangle.Height);
        }

        internal AutomationElement AutomationElement { get; private set; }
        internal Rectangle BoundRectangle { get; private set; }
        internal int SearchDepth { get; private set; }

        internal bool ContainPoint(Point point)
        {
            return this.BoundRectangle.Contains(point);
        }
    }

    internal class WeChatElement
    {
        private Rectangle boundRectangle;

        public WeChatElement(AutomationElement automationElement, int searchDepth)
        {
            this.AutomationElement = automationElement;
            this.SearchDepth = searchDepth;
            this.boundRectangle = automationElement.BoundingRectangle;
        }

        internal AutomationElement AutomationElement { get; private set; }
        internal int SearchDepth { get; private set; }

        internal bool ContainPoint(Point point)
        {
            return this.boundRectangle.Contains(point);
        }
    }

    internal enum NetChatType
    {
        WeChat = 0,
        DingDing = 1
    }
}