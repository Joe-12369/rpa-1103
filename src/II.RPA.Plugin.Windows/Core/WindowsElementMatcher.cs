using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Windows.Core
{
    internal class WindowsElementMatcher
    {
        private static ILogger logger = Logger.OfContext();

        private int Timeout { get; set; }

        private CancellationTokenSource CTS_JumpQuerySelectorAll { get; set; }
        public WindowsElementMatcher()
        {
            this.Timeout = 2 * 60 * 1000;

        }
        public WindowsElementMatcher(int timeout)
        {
            this.Timeout = timeout;
        }
        public List<AutomationElement> JumpQuerySelectorAll(List<WindowsSelectorItem> selectors, List<AutomationElement> fromElements, bool useCache = false, bool useHiddenWindow = false, bool useChildrenWindow = false, bool checkAccEnabler = true)
        {
            Stopwatch sp = Stopwatch.StartNew();
            try
            {
                CTS_JumpQuerySelectorAll = new CancellationTokenSource(Timeout);
                (List<WindowsSelectorItem> wndSelectors, List<WindowsSelectorItem> eleSelectors) = SplitIntoWindowAndElementSelectorItem(selectors);
                fromElements = JumpQuerySelectorAllWindow(wndSelectors, fromElements);
                logger.LogDebug($"窗口跳级匹配耗时{sp.ElapsedMilliseconds}");
                return JumpQuerySelectorAllElements(eleSelectors, fromElements, useCache, useHiddenWindow, useChildrenWindow, checkAccEnabler);
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning(ex);
                return new List<AutomationElement>();
            }
            finally
            {
                sp.Stop();
                logger.LogDebug($"元素跳级匹配结束，耗时：{sp.ElapsedMilliseconds}");
            }
        }

        public static (List<WindowsSelectorItem> wndSelectors, List<WindowsSelectorItem> eleSelectors) SplitIntoWindowAndElementSelectorItem(List<WindowsSelectorItem> selectors)
        {
            var lastWndItemIndex = selectors.FindLastIndex(item => item.ElementType.HasFlag(SelectorPropsType.Windows));
            if (lastWndItemIndex < 0)
            {
                return (new List<WindowsSelectorItem>(), selectors);
            }
            else
            {
                return (selectors.Take(lastWndItemIndex + 1).ToList(), selectors.Skip(lastWndItemIndex + 1).ToList());
            }
        }

        public List<AutomationElement> JumpQuerySelectorAllWindow(List<WindowsSelectorItem> selectors, List<AutomationElement> fromElements)
        {
            if (selectors.Count == 0 || fromElements.Count == 0)
            {
                return fromElements;
            }
            var fromWnds = fromElements.Select(fromEle => new WindowObject(fromEle.Properties.NativeWindowHandle.Value)).ToList();
            bool isJumpLevel = false;
            for (var i = 0; i < selectors.Count; i++)
            {
                logger.LogDebug($"执行自跳级节点(Wnd)起的第{i}节点匹配");
                this.ThrowIfTimeout();
                var s = selectors[i];
                if (!s.Enabled)
                {
                    logger.LogDebug($"第{i}节点未启用，跳级");
                    isJumpLevel = true;
                    MatchAddDetachedWindows(selectors[i], fromWnds);
                    if (i == selectors.Count - 1)
                    {
                        var desWnds = GetDescendantWindows(fromWnds);
                        fromWnds.AddRange(desWnds);
                    }
                    continue;
                }
                fromWnds = MatchWindows(selectors[i], fromWnds, isJumpLevel);
                isJumpLevel = false;
                if (fromWnds.Count == 0)
                {
                    return new List<AutomationElement>();
                }
                fromWnds = fromWnds.GroupBy(wnd => wnd.Handle)
                        .Select(g => g.First())
                        .ToList();
            }
            return fromWnds.Select(wnd => wnd.AutomationElement).ToList();
        }
        private List<WindowObject> MatchWindows(WindowsSelectorItem selectorItem, List<WindowObject> fromWindowObject, bool isJumpLevel)
        {
            List<WindowObject> nextFromWindowObject = new List<WindowObject>();
            foreach (var windowObjec in fromWindowObject)
            {
                nextFromWindowObject.AddRange(selectorItem.MatchWindows(windowObjec));
                if (isJumpLevel)
                {
                    nextFromWindowObject.AddRange(MatchWindows(selectorItem, windowObjec.ChildWindows, isJumpLevel));
                }
            }
            return nextFromWindowObject;
        }
        private List<WindowObject> GetDescendantWindows(List<WindowObject> fromWindowObject)
        {
            List<WindowObject> nextFromWindowObject = new List<WindowObject>();
            foreach (var windowObjec in fromWindowObject)
            {
                nextFromWindowObject.AddRange(windowObjec.ChildWindows);
                nextFromWindowObject.AddRange(GetDescendantWindows(windowObjec.ChildWindows));
            }
            return nextFromWindowObject;
        }

        private List<WindowObject> MatchAddDetachedWindows(WindowsSelectorItem selectorItem, List<WindowObject> fromWindowObject)
        {
            if (selectorItem.ElementType.HasFlag(SelectorPropsType.DetachedChildWindow))
            {
                var desktopWindows = GetDesktopWindow();
                var detachedWindow = desktopWindows.Where(wnd => fromWindowObject.Any(fromWnd => NativeMethods.MatchAncestor(wnd.Handle, fromWnd.Handle))).FirstOrDefault();
                if (detachedWindow != null)
                {
                    fromWindowObject.Add(detachedWindow);
                }
            }
            return fromWindowObject;
        }

        public List<AutomationElement> JumpQuerySelectorAllElements(List<WindowsSelectorItem> selectors, List<AutomationElement> fromElements, bool useCache = false, bool useHiddenWindow = false, bool useChildrenWindow = false, bool checkAccEnabler = true)
        {
            try
            {
                if (selectors.Count == 0 || fromElements.Count == 0)
                {
                    return fromElements;
                }
                // 跳级匹配的语义是“跳过未启用的节点，用其余启用节点约束查找”。
                // 若所有节点都未启用，链路长度/标签过滤和逐层匹配都无法收窄候选集，
                // 结果会退化为“返回上一层匹配到的全部元素”，
                // 导致元素实际不存在时（如元素校验）也被判定为命中。
                if (!selectors.Any(item => item.Enabled))
                {
                    logger.LogDebug("跳级匹配：节点全部未启用，无匹配约束，返回空结果");
                    return new List<AutomationElement>();
                }
                Stopwatch sp = Stopwatch.StartNew();
                List<List<AutomationElement>> ancestorPathList = GetLeafAncestorPathList(fromElements);
                FilterInvalidPaths(selectors, ancestorPathList);

                List<List<AutomationElement>> macthPathList = new List<List<AutomationElement>>();
                macthPathList.AddRange(ancestorPathList);

                List<List<AutomationElement>> nextPathList;
                bool isJumpLevel = false;
                for (var i = 0; i < selectors.Count; i++)
                {
                    this.ThrowIfTimeout();
                    var s = selectors[i];
                    if (!s.Enabled)
                    {
                        isJumpLevel = true;
                        logger.LogDebug($"第{i}节点未启用，跳级");
                        continue;
                    }
                    logger.LogDebug($"执行自跳级节点起的第{i}节点匹配");
                    int enableItmeCount = GetSurplusEnableCount(selectors, i);
                    int indexInParent = GetIndexPropValue(s);
                    if (isJumpLevel)
                    {
                        var pathRegex = GetPathRegex(selectors, i);
                        nextPathList = JumpMatchFilterPath(s, macthPathList, indexInParent, enableItmeCount, pathRegex);
                    }
                    else
                    {
                        nextPathList = NextMatchFilterPath(s, macthPathList, indexInParent);
                    }
                    isJumpLevel = false;
                    macthPathList.Clear();
                    if (nextPathList.Count == 0)
                    {
                        logger.LogDebug($"执行自跳级节点起的第{i}节点匹配无符合结果，匹配终止");
                        break;
                    }
                    macthPathList.AddRange(nextPathList);
                    if (checkAccEnabler && i < selectors.Count - 1)
                    {
                        nextPathList.ForEach(nextPath =>
                        {
                            AccEnabler.EnableOnce(nextPath[0]);
                        });
                    }
                }
                sp.Stop();
                var list = macthPathList.Select(x => x.FirstOrDefault())
                                    .GroupBy(ele => ele.PathKey)
                                    .Select(group => group.FirstOrDefault())
                                    .ToList();
                return list;
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning(ex);
                return new List<AutomationElement>();
            }
        }
        private List<List<AutomationElement>> JumpMatchFilterPath(WindowsSelectorItem selectorItem, List<List<AutomationElement>> macthPathList, int indexInParent, int enableItmeCount, Regex pathRegex)
        {
            var nextPathList = new List<List<AutomationElement>>();
            HashSet<string> pathKeySet = new HashSet<string>();
            HashSet<string> matchElementKeySet = new HashSet<string>();
            for (int j = 0; j < macthPathList.Count; j++)
            {
                this.ThrowIfTimeout();
                int maxMatchCount = macthPathList[j].Count - enableItmeCount + 1;
                for (int k = 0; k < maxMatchCount; k++)
                {
                    this.ThrowIfTimeout();
                    var matchElement = macthPathList[j][k];
                    var nextPath = macthPathList[j].Skip(k).ToList();
                    if (nextPath.Count < enableItmeCount - 1)
                    {
                        continue;
                    }
                    var nextPathNameList = nextPath.Select(ele => ele.ControlTypeName).ToList();
                    if (!pathRegex.IsMatch($",{string.Join(",", nextPathNameList)}"))
                    {
                        break;
                    }
                    if (!matchElementKeySet.Contains(matchElement.PathKey) && !IsMatch(selectorItem, matchElement, indexInParent))
                    {
                        continue;
                    }
                    if (!matchElementKeySet.Contains(matchElement.PathKey))
                        matchElementKeySet.Add(matchElement.PathKey);
                    var pathKey = $"{nextPath.Last().PathKey}_{nextPath.Count}";
                    if (!pathKeySet.Contains(pathKey))
                    {
                        pathKeySet.Add(pathKey);
                        nextPathList.Add(nextPath);
                    }
                }
            }

            return nextPathList;
        }
        private List<List<AutomationElement>> NextMatchFilterPath(WindowsSelectorItem selectorItem, List<List<AutomationElement>> macthPathList, int indexInParent)
        {
            HashSet<string> matchElementKeySet = new HashSet<string>();
            var nextPathList = new List<List<AutomationElement>>();
            for (int j = 0; j < macthPathList.Count; j++)
            {
                this.ThrowIfTimeout();
                var matchElement = macthPathList[j][1];
                if (macthPathList[j].Count < 2)
                {
                    continue;
                }
                if (!matchElementKeySet.Contains(matchElement.PathKey) && !IsMatch(selectorItem, matchElement, indexInParent))
                {
                    continue;
                }
                if (!matchElementKeySet.Contains(matchElement.PathKey))
                    matchElementKeySet.Add(matchElement.PathKey);
                var nextPath = macthPathList[j].Skip(1).ToList();
                if (nextPath.Count > 0)
                {
                    nextPathList.Add(nextPath);
                }
            }
            return nextPathList;
        }
        private bool IsMatch(WindowsSelectorItem selectorItem, AutomationElement matchElement, int indexInParent)
        {
            if ((indexInParent == -1 || matchElement.IndexInParent == indexInParent) && selectorItem.Match(matchElement))
            {
                return true;
            }
            return false;
        }
        private int GetIndexPropValue(WindowsSelectorItem selectorItem)
        {
            int indexInParent = -1;
            var idxProp = selectorItem.FindAccordingName(ConstFields.SELECTOR_INDEX_IN_PARENT)
                        .FirstOrDefault(x => x.Accurate && !string.IsNullOrEmpty(x.Value));
            if (idxProp != null && !int.TryParse(idxProp.Value, out indexInParent))
            {
                indexInParent = -1;
            }
            return indexInParent;
        }


        private Regex GetPathRegex(List<WindowsSelectorItem> selectors, int skip)
        {
            var selectorNameList = selectors.Skip(skip).Select(item => item.Enabled ? item.ElementName : ".*").ToList();
            return new Regex(string.Join(",", selectorNameList), RegexOptions.IgnoreCase);
        }
        private bool IsJumpLevel(WindowsSelectorItem selectorItem) => !selectorItem.Enabled;

        private int GetSurplusEnableCount(List<WindowsSelectorItem> selectors, int skip) => selectors.Skip(skip).Count(item => item.Enabled);

        private List<List<AutomationElement>> FilterInvalidPaths(List<WindowsSelectorItem> selectors, List<List<AutomationElement>> ancestorPathList)
        {
            var enableItemCount = selectors.Count(item => item.Enabled);
            var itemNameList = selectors.Select(item => item.Enabled ? item.ElementName : ".*").ToList();
            var itemNameList_distinct = itemNameList.Distinct().ToList();
            itemNameList_distinct.Remove(".*");
            logger.LogInformation($"selector启用节点数量：{enableItemCount}");
            logger.LogDebugBlock(() =>
            {
                logger.LogDebug($"itemNameList：{string.Join(",", itemNameList)}");
                logger.LogDebug($"itemNameList 去重复后：{string.Join(",", itemNameList_distinct)}");
            });
            Regex regex = new Regex(string.Join(",", itemNameList), RegexOptions.IgnoreCase);
            for (int i = ancestorPathList.Count - 1; i >= 0; i--)
            {
                var path = ancestorPathList[i];
                var pathNameList = path.Skip(1).Select(ele => ele.ControlTypeName).ToList();
                //过滤长度
                if (pathNameList.Count < enableItemCount)
                {
                    logger.LogDebug($"将要移除的链路长度：{pathNameList.Count}");
                    logger.LogDebug($"将要移除的链路：{string.Join(",", pathNameList)}");
                    ancestorPathList.RemoveAt(i);
                    continue;
                }
                //过滤标签
                var pathNameList_distinct = pathNameList.Distinct().ToList();
                if (!itemNameList_distinct.All(name => pathNameList_distinct.IndexOf(name) >= 0))
                {
                    ancestorPathList.RemoveAt(i);
                    continue;
                }
                if (!regex.IsMatch($",{string.Join(",", pathNameList)}"))
                {
                    ancestorPathList.RemoveAt(i);
                    continue;
                }
                path.RemoveAt(0);
            }
            logger.LogDebug($"符合链路长度的链路数量：{ancestorPathList.Count}");
            return ancestorPathList;
        }
        private List<List<AutomationElement>> GetLeafAncestorPathList(List<AutomationElement> fromElements)
        {
            Stopwatch sp = Stopwatch.StartNew();
            List<List<AutomationElement>> ancestorPathList = new List<List<AutomationElement>>();
            logger.LogDebug($"开始遍历树结构，根节点数：{fromElements.Count}");
            fromElements.ForEach(root =>
            {
                RecursiveDescendantsByRawView(root);
                ancestorPathList.AddRange(root.CachedRawViewLeafs.Where(leaf => leaf.PathKey != null).Select(leaf => GetAncestorPathByScanCache(leaf)));
            });
            logger.LogDebug($"遍历树结构结束:{sp.ElapsedMilliseconds}");
            logger.LogDebug($"获取元素链路数量:{ancestorPathList.Count}");
            return ancestorPathList;
        }
        public List<AutomationElement> GetAncestorPathByScanCache(AutomationElement element)
        {
            List<AutomationElement> path = new List<AutomationElement>();
            path.Add(element);
            var parent = element.ScanCachedParent;
            while (parent != null)
            {
                path.Add(parent);
                parent = parent.ScanCachedParent;
            }
            path.Reverse();
            return path;
        }
        public void RecursiveDescendantsByRawView(AutomationElement parent, Action<AutomationElement, int> action = null)
        {
            var children = parent.GetChildrenByRawView(false);
            parent.CachedRawViewLeafs.Clear();
            for (int i = 0; i < children.Count; i++)
            {
                action?.Invoke(children[i], i);
                RecursiveDescendantsByRawView(children[i], action);
                if (children[i].CachedRawViewChildren.Count == 0)
                {
                    parent.CachedRawViewLeafs.Add(children[i]);
                }
                if (children[i].CachedRawViewLeafs.Count > 0)
                {
                    parent.CachedRawViewLeafs.AddRange(children[i].CachedRawViewLeafs);
                }
                children[i].IndexInParent = i;
            }
            parent.AddChildWindowToChildrenList(
                parent.CachedRawViewChildren,
                ele => ele.CacheClassName.Contains("RenderWidgetHostHWND")
                );
        }

        private void ThrowIfTimeout()
        {
            if (CTS_JumpQuerySelectorAll.IsCancellationRequested)
            {
                throw new TimeoutException("跳级匹配元素超时".I18n("controlType_WindowsElementMatcher_AA1A9FD5"));
            }
        }

        private List<WindowObject> desktopWindow = null;
        private List<WindowObject> GetDesktopWindow()
        {
            desktopWindow = desktopWindow ?? new DesktopWindow().FindAllChildWindow(null, null, wnd =>
            {
                return wnd.IsVisible;
            }).ToList();
            return desktopWindow;
        }
    }
}
