using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.Deeps.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Deeps
{
    public class RxTreeBuilder
    {
        private static readonly ILogger _logger = Logger.OfContext();
        private readonly AutomationBase _automationBase;
        private readonly ElementCacheService _elementCacheService;
        private readonly ITreeWalker _treeWalker;
        private readonly AutomationElement _desktop;
        public RxTreeBuilder(AutomationBase automationBase, ElementCacheService elementCacheService)
        {
            _automationBase = automationBase;
            _elementCacheService = elementCacheService;
            _treeWalker = _automationBase.TreeWalkerFactory.GetRawViewWalker();
            _desktop = _automationBase.GetDesktop();
        }
        public (ElementModel Tree, List<string> Path) Build(AutomationElement target, CancellationToken cancellationToken)
        {
            return BuildTreeFromTargetRx(target, cancellationToken)
                        .FirstOrDefaultAsync()
                        .ToTask(cancellationToken) 
                        .GetAwaiter()
                        .GetResult();
        }
        public async Task<(ElementModel Tree, List<string> Path)> BuildAsync(AutomationElement target, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await BuildTreeFromTargetRxNew(target, cancellationToken)
                    .FirstOrDefaultAsync()
                    .ToTask(cancellationToken);

                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return (null, new List<string>());
            }
        }
        private IObservable<(ElementModel tree, List<string> path)> BuildTreeFromTargetRxNew(
                AutomationElement target,
                CancellationToken cancellationToken)
        {
            var pathList = new List<string>();

            return BuildPathToRoot(target, cancellationToken)
                .Select(element => _elementCacheService.Add(element)) 
                .Do(elementModel =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    pathList.Insert(0, elementModel.Id);
                })
                .Aggregate(
                    seed: (ElementModel)null,
                    accumulator: (childSubtree, currentElement) =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // 填充 children（新的副本）
                        var children = currentElement.Automation
                            .FindAllChildren()
                            .Where(c => c != null)
                            .Select(c => _elementCacheService.Add(c))
                            .ToList();

                        currentElement.Children = children;

                        // 如果已经有子树，需要挂接到对应的 child
                        if (childSubtree != null)
                        {
                            var targetChildIndex = currentElement.Children
                                .FindIndex(c => IsSameElement(c.Automation, childSubtree.Automation));

                            if (targetChildIndex >= 0)
                            {
                                currentElement.Children[targetChildIndex] = childSubtree;
                            }
                            else
                            {
                                currentElement.Children.Add(childSubtree);
                            }
                        }

                        return currentElement;
                    })
                .Where(result => result != null)
                .Select(tree => (tree, path: pathList.ToList()))
                .TakeUntil(Observable.Create<Unit>(observer =>
                {
                    var registration = cancellationToken.Register(() => observer.OnNext(Unit.Default));
                    return registration;
                }));
        }

        private IObservable<(ElementModel tree, List<string> path)> BuildTreeFromTargetRx(AutomationElement target, CancellationToken cancellationToken)
        {
            var pathList = new List<string>();
            return
                BuildPathToRoot(target, cancellationToken)
                 .Select(element => _elementCacheService.Add(element))
                 .Do(elementMode =>
                 {
                     cancellationToken.ThrowIfCancellationRequested();
                     pathList.Insert(0, elementMode.Id);
                 })
                .Aggregate(
                    seed: (ElementModel) null,
                    accumulator: (childSubtree, currentElement) =>
                    {

                        cancellationToken.ThrowIfCancellationRequested();
                        currentElement.Children.Clear();
                        if (currentElement.Children.Count == 0)
                        {
                            var children = currentElement.Automation.FindAllChildren();
                            foreach (var child in children)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                if (child != null)
                                {
                                    currentElement.Children.Add(_elementCacheService.Add(child));
                                }
                            }
                        }

                        if (childSubtree != null)
                        {
                            var targetChildIndex = currentElement.Children
                                .FindIndex(c => IsSameElement(c.Automation, childSubtree.Automation));

                            if (targetChildIndex >= 0)
                            {
                                currentElement.Children[targetChildIndex] = childSubtree;
                            }
                            else
                            {
                                currentElement.Children.Add(childSubtree);
                            }
                        }

                        return currentElement;
                    })
                .Where(result => result != null)
                .Select(tree => (tree, path: pathList.ToList()))
                .TakeUntil(Observable.Create<Unit>(observer => 
                {
                    var registration = cancellationToken.Register(() => observer.OnNext(Unit.Default));
                    return registration;
                }));
        }
        private IObservable<AutomationElement> BuildPathToRoot(AutomationElement target, CancellationToken cancellationToken = default)
        {
            return Observable.Create<AutomationElement>(observer =>
            {
                var visited = new HashSet<string>();
                var current = target;

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

                        observer.OnNext(current);

                        current = _treeWalker.GetParent(current);
                    }
                    observer.OnCompleted();
                }
                catch (OperationCanceledException)
                {
                    observer.OnCompleted(); 
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return Disposable.Empty;
            });
        }

        private UiaTreeResultDto BuildElementTreeOptimized(AutomationElement target)
        {
            if (target == null) return null;

            var pathToRoot = BuildPathToRootOld(target);
            if (pathToRoot.Count == 0) return null;
            var activeElementIds = new HashSet<string>();
            BuildPathToRootRx(target)
                .Do(element =>
                {
                    string rid = GetRuntimeId(element);
                    activeElementIds.Add(rid);
                })
                .Select(e => e.ToModel())
                .Take(1)
                .Do(elementModel =>
                {
                    if (elementModel.Children.Count == 0)
                    {
                        var children = elementModel.Automation.FindAllChildren();
                        foreach (var child in children)
                        {
                            if (child == null) continue;

                            var childModel = child.ToModel();
                            elementModel.Children.Add(childModel);
                        }
                    }
                })
                .Subscribe(_ => { });



            foreach (var element in pathToRoot)
            {
                string rid = GetRuntimeId(element);
                activeElementIds.Add(rid);
            }

            var activePath = new List<string>();
            var root = pathToRoot.Last();
            var rootModel = root.ToModel();
            BuildOptimizedTree(rootModel, activeElementIds, activePath);

            return new UiaTreeResultDto
            {
                Tree = new ElementModel[1] { rootModel },
                Active = activePath
            };
        }
        private List<AutomationElement> BuildPathToRootOld(AutomationElement target)
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
        private IObservable<AutomationElement> BuildPathToRootRx(AutomationElement target)
        {
            return Observable.Create<AutomationElement>(observer =>
            {
                var visited = new HashSet<string>();
                var current = target;

                while (current != null)
                {
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

                    if (current.Equals(_desktop))
                    {
                        break;
                    }
                    observer.OnNext(current);



                    current = _treeWalker.GetParent(current);
                }

                observer.OnCompleted();
                return Disposable.Empty;
            });
        }
        private void BuildOptimizedTree(ElementModel element, HashSet<string> activeElementIds, List<string> activePath)
        {
            //RefreshCache(element);
            string currentId = GetRuntimeId(element.Automation);
            bool isInActivePath = !string.IsNullOrEmpty(currentId) && activeElementIds.Contains(currentId);

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
                // todo usecache
                if (element.Children.Count == 0)
                {
                    var children = element.Automation.FindAllChildren();
                    foreach (var child in children)
                    {
                        if (child == null) continue;

                        var childModel = child.ToModel();
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

                var processId = element.Properties.ProcessId.ValueOrDefault;
                var controlType = element.Properties.ControlType.ValueOrDefault.ToString() ?? "Unknown";
                var name = element.Properties.Name ?? string.Empty;

                return $"{processId}-{controlType}-{name.GetHashCode()}";
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }
        private bool IsSameElement(AutomationElement element1, AutomationElement element2)
        {
            if (element1 == null || element2 == null) return false;

            try
            {
                var rid1 = GetRuntimeId(element1);
                var rid2 = GetRuntimeId(element2);

                if (!string.IsNullOrEmpty(rid1) && !string.IsNullOrEmpty(rid2))
                {
                    return rid1 == rid2;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        private string GetElementId(AutomationElement automationElement)
        {
            return _elementCacheService.Add(automationElement).Id;
        }
    }
}
