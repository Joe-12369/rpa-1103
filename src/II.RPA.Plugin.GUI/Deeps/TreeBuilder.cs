using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.Deeps.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Deeps
{
    public class TreeBuilder: ITreeBuilder
    {
        private static readonly ILogger _logger = Logger.OfContext();
        private readonly AutomationBase _automationBase;
        private readonly ElementCacheService _elementCacheService;
        private readonly ITreeWalker _treeWalker;
        private readonly AutomationElement _desktop;
        public TreeBuilder(AutomationBase automationBase, ElementCacheService elementCacheService)
        {
            _automationBase = automationBase;
            _elementCacheService = elementCacheService;
            _treeWalker = _automationBase.TreeWalkerFactory.GetRawViewWalker();
            _desktop = _automationBase.GetDesktop();
        }
        public (ElementModel Root, List<string> Path) Build(AutomationElement target)
        {
            return BuildElementTreeOptimized(target, CancellationToken.None);
        }
        public ValueTask<(ElementModel Root, List<string> Path)> BuildAsync(AutomationElement target, CancellationToken cancellationToken)
        {
            var result = BuildElementTreeOptimized(target, cancellationToken);
            return new ValueTask<(ElementModel Root, List<string> Path)>(result);
        }
        private (ElementModel Root, List<string> Path) BuildElementTreeOptimized(AutomationElement target, CancellationToken cancellationToken)
        {
            if (target == null) return (null,null);

            var pathToRoot = BuildPath(target, cancellationToken);
            if (pathToRoot == null || pathToRoot.Count == 0) return (null, null);

            var activeElementIds = new HashSet<string>();
            foreach (var element in pathToRoot)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string rid = GetRuntimeId(element);
                activeElementIds.Add(rid);
            }

            var activePath = new List<string>();
            var root = pathToRoot.Last();
            var rootModel = root.ToModel();
            BuildOptimizedTree(rootModel, activeElementIds, activePath);
            cancellationToken.ThrowIfCancellationRequested();
            return (rootModel, activePath);
        }

        private void BuildOptimizedTree(ElementModel element, HashSet<string> activeElementIds, List<string> activePath)
        {
            string currentId = GetRuntimeId(element.Automation);
            bool isInActivePath = !string.IsNullOrEmpty(currentId) && activeElementIds.Contains(currentId);
            _elementCacheService.Add(element);
            if (isInActivePath)
            {
                activePath.Add(element.Id);
            }

            if (isInActivePath)
            {
                LoadChildrenOptimized(element, activeElementIds, activePath);
            }
        }

        private void LoadChildrenOptimized(ElementModel element, HashSet<string> activeElementIds, List<string> activePath)
        {
            if (element?.Automation == null) return;

            try
            {
                if (element.Children.Count == 0)
                {
                    var children = element.Automation.FindAllChildren();
                    foreach (var child in children)
                    {
                        if (child == null) continue;

                        var childModel = child.ToModel();
                        _elementCacheService.Add(childModel);
                        element.Children.Add(childModel);
                    }
                }

                foreach (var child in element.Children)
                {
                    string childId = GetRuntimeId(child.Automation);
                    if (activeElementIds.Contains(childId) && !string.IsNullOrEmpty(childId))
                    {
                        BuildOptimizedTree(child, activeElementIds, activePath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading children for element: {ex.Message}");
            }
        }

        private static string GetRuntimeId(AutomationElement element)
        {
            if (element == null) return null;

            try
            {
                var runtimeId = element.Properties.RuntimeId.ValueOrDefault;
                if (runtimeId != null && runtimeId.Length > 0)
                {
                    return string.Join("-", runtimeId);
                }
                return Guid.NewGuid().ToString();
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }

        private List<AutomationElement> BuildPath(AutomationElement element, CancellationToken cancellationToken)
        {
            var path = new List<AutomationElement>();
            var visited = new HashSet<string>();
            var current = element;

            try
            {
                while (current != null && !current.Equals(_desktop))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string rid = GetRuntimeId(current);
                    if (string.IsNullOrEmpty(rid))
                    {
                        rid = Guid.NewGuid().ToString();
                    }
                    if (visited.Contains(rid))
                    {
                        _logger.LogWarning($"BuildPathToRoot Error: 发现元素递归!");
                        break;
                    }
                    visited.Add(rid);

                    path.Add(current);

                    current = _treeWalker.GetParent(current);
                }
                return path;

            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }

        }

        private List<AutomationElement> BuildPathToRoot(AutomationElement target)
        {
            var path = new List<AutomationElement>();
            var visited = new HashSet<string>();
            var current = target;

            while (current != null)
            {
                string rid = GetRuntimeId(current);
                if (string.IsNullOrEmpty(rid))
                {
                    //todo
                    rid = Guid.NewGuid().ToString();
                }
                if (visited.Contains(rid))
                {
                    _logger.LogWarning($"BuildPathToRoot Error:发现元素递归!");
                    break;
                }
                visited.Add(rid);

                if (current.Equals(_desktop.Automation))
                {
                    break;
                }

                path.Add(current);
                current = _treeWalker.GetParent(current);
            }
            return path;
        }

    }
}
