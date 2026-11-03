using II.RPA.MobileDeviceManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace II.RPA.MobileDeviceManager.Services
{
    internal enum PageSourceProviderKind
    {
        Appium,
        AdbDump
    }

    internal class MobileElementRepository
    {
        private readonly object _syncRoot = new object();
        private readonly List<MobileElement> _elements = new List<MobileElement>();
        private long _snapshotVersionSeed;
        private PageSourceProviderKind _sourceProvider = PageSourceProviderKind.Appium;
        private long _sessionGeneration;
        private long _snapshotVersion;
        private string _deviceId;
        private long _connectionVersion;

        public IReadOnlyList<MobileElement> Elements
        {
            get { lock (_syncRoot) { return _elements.ToArray(); } }
        }

        public PageSourceProviderKind SourceProvider { get { lock (_syncRoot) { return _sourceProvider; } } }
        public bool IsAdbDegraded { get { return SourceProvider == PageSourceProviderKind.AdbDump; } }
        public long SessionGeneration { get { lock (_syncRoot) { return _sessionGeneration; } } }
        public long SnapshotVersion { get { lock (_syncRoot) { return _snapshotVersion; } } }

        public bool HasSnapshot
        {
            get { lock (_syncRoot) { return _elements.Count > 0; } }
        }

        public void BeginConnection(string deviceId, long connectionVersion)
        {
            lock (_syncRoot)
            {
                _deviceId = deviceId;
                _connectionVersion = connectionVersion;
                _elements.Clear();
                _sourceProvider = PageSourceProviderKind.Appium;
                _sessionGeneration = 0;
                _snapshotVersion = Interlocked.Increment(ref _snapshotVersionSeed);
            }
        }

        public bool HasSnapshotFor(string deviceId, long connectionVersion)
        {
            lock (_syncRoot)
            {
                return _elements.Count > 0
                    && string.Equals(_deviceId, deviceId, StringComparison.Ordinal)
                    && _connectionVersion == connectionVersion;
            }
        }

        public bool TryReplaceXmlSnapshot(XmlDocument document, string deviceId, long connectionVersion)
        {
            XmlElement root = document == null ? null : document.DocumentElement;
            PageSourceProviderKind provider = string.Equals(root == null ? null : root.GetAttribute("rpaSourceProvider"), "AdbDump", StringComparison.OrdinalIgnoreCase)
                ? PageSourceProviderKind.AdbDump
                : PageSourceProviderKind.Appium;
            long generation;
            long.TryParse(root == null ? null : root.GetAttribute("rpaSessionGeneration"), out generation);
            long snapshotVersion = Interlocked.Increment(ref _snapshotVersionSeed);
            var newElements = new List<MobileElement>();

            if (root != null)
            {
                if (string.Equals(root.Name, "hierarchy", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (XmlNode childNode in root.ChildNodes)
                    {
                        XmlElement childElement = childNode as XmlElement;
                        if (childElement != null)
                        {
                            RecursionChildren(childElement, 0, null, newElements, provider, generation, snapshotVersion);
                        }
                    }
                }
                else
                {
                    RecursionChildren(root, 0, null, newElements, provider, generation, snapshotVersion);
                }
            }

            lock (_syncRoot)
            {
                if (!string.Equals(_deviceId, deviceId, StringComparison.Ordinal)
                    || _connectionVersion != connectionVersion)
                    return false;

                _elements.Clear();
                _elements.AddRange(newElements);
                _sourceProvider = provider;
                _sessionGeneration = generation;
                _snapshotVersion = snapshotVersion;
                return true;
            }
        }

        public void ReplaceElements(IEnumerable<MobileElement> elements)
        {
            lock (_syncRoot)
            {
                _elements.Clear();
                if (elements != null) _elements.AddRange(elements);
                _snapshotVersion = Interlocked.Increment(ref _snapshotVersionSeed);
            }
        }

        public bool IsCurrentSnapshotElement(MobileElement element)
        {
            if (element == null) return false;
            lock (_syncRoot)
            {
                return element.SourceSnapshotVersion == _snapshotVersion
                    && element.SourceGeneration == _sessionGeneration
                    && string.Equals(element.SourceProvider, _sourceProvider.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool TryGetSnapshotForElement(MobileElement element, out IReadOnlyList<MobileElement> snapshot)
        {
            lock (_syncRoot)
            {
                if (IsCurrentSnapshotElement(element) && _elements.Contains(element))
                {
                    snapshot = _elements.ToArray();
                    return true;
                }
                snapshot = null;
                return false;
            }
        }

        public MobileElement GetElementFromPoint(System.Drawing.Point location)
        {
            List<MobileElement> snapshot;
            lock (_syncRoot) { snapshot = _elements.ToList(); }
            return SelectBestElement(snapshot, location);
        }

        public Task<MobileElement> GetSelectedElementFromPointAsync(System.Drawing.Point location)
        {
            return Task.Run(() =>
            {
                lock (_syncRoot) { return SelectBestElement(_elements.ToList(), location); }
            });
        }

        private static MobileElement SelectBestElement(IEnumerable<MobileElement> source, System.Drawing.Point location)
        {
            return source
                .Where(element => element != null && element.HasValidBounds && element.ContainPoint(location))
                .OrderBy(element => element.Area)
                .ThenByDescending(element => element.Depth)
                .ThenByDescending(element => element.StablePropertyCount)
                .FirstOrDefault();
        }

        private static void RecursionChildren(
            XmlElement element,
            int depth,
            MobileElement parent,
            List<MobileElement> targetList,
            PageSourceProviderKind provider,
            long generation,
            long snapshotVersion)
        {
            if (element == null || string.Equals(element.Name, "hierarchy", StringComparison.OrdinalIgnoreCase)) return;

            var mobileElement = new MobileElement(element, depth, parent)
            {
                SourceProvider = provider.ToString(),
                IsFromDegradedPageSource = provider == PageSourceProviderKind.AdbDump,
                SourceGeneration = generation,
                SourceSnapshotVersion = snapshotVersion
            };
            if (parent != null) parent.AddChild(mobileElement);
            targetList.Add(mobileElement);

            foreach (XmlNode childNode in element.ChildNodes)
            {
                XmlElement childElement = childNode as XmlElement;
                if (childElement != null)
                {
                    RecursionChildren(childElement, depth + 1, mobileElement, targetList, provider, generation, snapshotVersion);
                }
            }
        }
    }
}