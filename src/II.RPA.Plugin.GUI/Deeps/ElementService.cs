using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.Deeps.Highlight;
using II.RPA.Plugin.GUI.Deeps.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Deeps
{
    public class ElementService : IElementService
    {
        private static readonly ILogger _logger = Logger.OfContext();
        private readonly AutomationBase _automationProvider;
        private readonly ElementCacheService _cache;
        private readonly TreeBuilder _treeBuilder;
        private ISubject<UiaTreeResultDto> _treeResultSubject;
        private volatile int _currentRequestId = 0;
        private CancellationTokenSource _currentBuildCts;
        private readonly SemaphoreSlim _buildSemaphore = new SemaphoreSlim(1, 1);
        private readonly HighlightService _highlightService;
        private readonly JsonSerializerSettings _requiredSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore
        };
        private readonly WndService _wndService;
        public ElementService(HighlightService highlightService, AutomationBase automationBase, WndService wndService)
        {
            _automationProvider = automationBase;
            _cache = new ElementCacheService(_automationProvider);
            _treeBuilder = new TreeBuilder(_automationProvider, _cache);
            _highlightService = highlightService;
            _wndService = wndService;
            Start();
        }
        public int StartedCount { get; private set; }
        public int CompletedCount { get; private set; }
        public int CanceledCount { get; private set; }
        #region IElementService

        public IObservable<UiaTreeResultDto> PeerStream
        {
            get;
            private set;
        }
        public void Start()
        {
            _treeResultSubject = new ReplaySubject<UiaTreeResultDto>(3);
            PeerStream = _treeResultSubject.AsObservable();
        }
        public void Stop()
        {
            //_treeResultSubject.Dispose();
        }
        public string GetDescendants(string payload)
        {
            var param = JsonConvert.DeserializeObject<ElementReqestDto>(payload, _requiredSettings);
            var descendants = GetDescendants(param.ElementId, param.Level);
            return new ElementDescendantsDto { ElementId = param.ElementId, Descendants = descendants }.ToString();
        }
        public string GetDescendantsCount(string payload)
        {
            var param = JsonConvert.DeserializeObject<ElementReqestDto>(payload);
            int count = GetDescendantsCount(param.ElementId, param.Level);
            return new ElementCountDto { Count = count, Id = param.ElementId }.ToString();
        }

        public async Task StartPeerElementAsync(AutomationElement automationElement)
        {
            StartedCount++;
            var requestId = Interlocked.Increment(ref _currentRequestId);

            try
            {
                _currentBuildCts?.Cancel();
                _currentBuildCts?.Dispose();
            }
            catch (Exception)
            {
                
            }

            _currentBuildCts = new CancellationTokenSource();
            var token = _currentBuildCts.Token;

            await _buildSemaphore.WaitAsync();
            try
            {
                if (requestId != _currentRequestId)
                {
                    return;
                }

                var sw = Stopwatch.StartNew();
                try
                {
                    await StartBuildTreeAsync(automationElement, token);
                    token.ThrowIfCancellationRequested();
                    _logger.LogDebug($"Build request {requestId} completed successfully:{sw.ElapsedMilliseconds} ms");
                    CompletedCount++;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug($"Build request {requestId} was cancelled");
                    CanceledCount++;
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"Build request {requestId} failed: {ex.Message}");
                }
                finally
                {
                    sw.Stop();
                }
            }
            finally
            {
                _buildSemaphore.Release();
            }
        }

        public string GetElementDetail(string payload)
        {
            var param = JsonConvert.DeserializeObject<ElementReqestDto>(payload);
            if (_cache.TryGet(param.ElementId, out var element))
            {
                _ = _highlightService.UpdateLocationAsync(element.Automation.Properties.BoundingRectangle.ValueOrDefault);
                return ElementPropertiesProvider.BuildPropertiesJson(element);
            }
            return PayloadDto.Failed("目标元素已失效".I18n("ElementService_57BEE49E")).ToString();
        }
        public string BuildSelector(string payload)
        {
            var param = JsonConvert.DeserializeObject<ElementReqestDto>(payload);
            if (_cache.TryGet(param.ElementId, out var element))
            {
                var selector = new SelectorBuilder(element.Automation).Build();
                if (string.IsNullOrEmpty(selector))
                {
                    return PayloadDto.Failed("保存元素失败".I18n("ElementService_4DB27A49")).ToString();
                }
                return new BuildSelectorDto
                {
                    ElementId = param.ElementId,
                    Selector = selector
                }.ToString();
            }
            return PayloadDto.Failed("目标元素已失效".I18n("ElementService_57BEE49E")).ToString();
        }
        public string StartHighlight(string payload)
        {
            var param = JsonConvert.DeserializeObject<ElementReqestDto>(payload);
            if (_cache.TryGet(param.ElementId, out var element))
            {
                _highlightService.Resume();
                _highlightService.UpdateLocation(element.Region);
                return PayloadDto.Success("高亮完成".I18n("ElementService_789D4717")).ToString();
            }
            return PayloadDto.Failed("目标元素已失效".I18n("ElementService_57BEE49E")).ToString();

        }
        public string StopHighlight(string payload)
        {
            var param = JsonConvert.DeserializeObject<ElementReqestDto>(payload);
            if (_cache.TryGet(param.ElementId, out _))
            {
            }
            _highlightService.Pause();
            return PayloadDto.Success().ToString();
        }
        [Obsolete]
        public string PeerElement(Track peer)
        {
            var automation = _automationProvider.FromPoint(new System.Drawing.Point(peer.X, peer.Y));
            var element = automation.ToModel();
            RefreshCache(element);
            return element.ToDtoJson();
        }

        public async Task StartBuildTreeAsync(AutomationElement target, CancellationToken cancellationToken = default)
        {
            if (_treeBuilder == null)
            {
                _logger.LogError("_treeBuilder is not initialized");
                return;
            }

            var result = await _treeBuilder.BuildAsync(target, cancellationToken);
            if (result.Root != null)
            {
                result.Root.Icon = "icon-chuangkou";
            }
            var dto = new UiaTreeResultDto
            {
                Active = result.Path,
                Tree = new ElementModel[1] { result.Root }
            };
            cancellationToken.ThrowIfCancellationRequested();
            _treeResultSubject?.OnNext(dto);
        }
        #endregion


        private IEnumerable<ElementModel> GetDescendants(string id, int level = 1)
        {
            if (string.IsNullOrEmpty(id))
            {
                return GetRoots();
            }

            if (_cache.TryGet(id, out var element))
            {
                if (element == null) return Enumerable.Empty<ElementModel>();
                return GetDescendantsRecursiveNew(element, level);
            }
            else
            {
                return Enumerable.Empty<ElementModel>();
            }
        }
        private IEnumerable<ElementModel> GetDescendantsRecursive(ElementModel element, int level)
        {
            if (level <= 0) return Enumerable.Empty<ElementModel>();

            var children = new List<ElementModel>();
            foreach (var child in element.Automation.FindAllChildren())
            {
                var childModel = child.ToModel();
                childModel.Id = _cache.Add(childModel);
                element.Children.Add(childModel);
                childModel.Children.AddRange(GetDescendantsRecursive(childModel, level - 1));
                children.Add(childModel);
            }
            return children;
        }
        private IEnumerable<ElementModel> GetDescendantsRecursiveNew(ElementModel element, int level)
        {
            if (level <= 0) return Enumerable.Empty<ElementModel>();

            var children = new List<ElementModel>();
            foreach (var child in element.Automation.FindAllChildren())
            {
                var childModel = child.ToModel();
                childModel.Id = _cache.Add(childModel);

                if (level > 1)
                {
                    childModel.Children.Clear();
                    childModel.Children.AddRange(GetDescendantsRecursiveNew(childModel, level - 1));
                }
                children.Add(childModel);
            }

            return children;
        }
        private int GetDescendantsCount(string id, int level = 1)
        {
            return GetDescendants(id, level).Count();
        }

        private IEnumerable<ElementModel> GetRoots()
        {
            var win32_windows = _wndService.GetRootWindows();

            foreach (var root in win32_windows)
            {
                RefreshCache(root);
                root.Icon = "icon-chuangkou";
            }
            _highlightService.Reset();
            return win32_windows;
        }

        private void RefreshCache(ElementModel elementModel)
        {
            _cache.GetOrAdd(elementModel);
        }
    }
}

