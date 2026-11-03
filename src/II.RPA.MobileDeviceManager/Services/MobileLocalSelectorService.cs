using II.RPA.MobileDeviceManager.Models;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace II.RPA.MobileDeviceManager.Services
{
    internal static class MobileLocalSelectorService
    {
        public static string BuildVerifiedSelector(
            MobileElement target,
            IReadOnlyList<MobileElement> elements,
            bool allowTargetAnchor = true,
            string referenceSelector = null)
        {
            if (target == null) return string.Empty;
            List<MobileItem> items = JArray.Parse(target.GetSelector()).ToObject<List<MobileItem>>() ?? new List<MobileItem>();
            List<MobileElement> path = GetPath(target);
            if (items.Count == 0 || items.Count != path.Count) return string.Empty;
            if (!HasSameStructure(items, referenceSelector)) return string.Empty;

            IReadOnlyList<MobileElement> snapshotElements = (elements ?? new MobileElement[0])
                .Where(element => IsSameSnapshot(element, target))
                .ToArray();
            ResetAccurate(items);

            int targetIndex = items.Count - 1;
            int anchorIndex = -1;
            if (allowTargetAnchor
                && (TryEnableUniqueProperty(items[targetIndex], "id", target.Id, snapshotElements, element => element.Id == target.Id)
                    || TryEnableUniqueProperty(items[targetIndex], "description", target.Desc, snapshotElements, element => element.Desc == target.Desc)
                    || TryEnableUniqueProperty(items[targetIndex], "text", target.Text, snapshotElements, element => element.Text == target.Text)
                    || TryEnableUniquePropertyCombination(items[targetIndex], target, snapshotElements)))
            {
                anchorIndex = targetIndex;
            }

            if (anchorIndex < 0)
            {
                for (int index = path.Count - 2; index >= 0; index--)
                {
                    MobileElement ancestor = path[index];
                    if (TryEnableUniqueProperty(items[index], "id", ancestor.Id, snapshotElements, element => element.Id == ancestor.Id)
                        || TryEnableUniqueProperty(items[index], "description", ancestor.Desc, snapshotElements, element => element.Desc == ancestor.Desc)
                        || TryEnableUniqueProperty(items[index], "text", ancestor.Text, snapshotElements, element => element.Text == ancestor.Text)
                        || TryEnableUniquePropertyCombination(items[index], ancestor, snapshotElements))
                    {
                        anchorIndex = index;
                        break;
                    }
                }
            }

            if (anchorIndex >= 0)
            {
                for (int index = anchorIndex + 1; index < items.Count; index++)
                {
                    EnableStructuralPath(items[index]);
                }
            }
            else
            {
                foreach (MobileItem item in items) EnableStructuralPath(item);
            }

            SynchronizeNodeAccurate(items);
            return JArray.FromObject(items).ToString(Newtonsoft.Json.Formatting.None);
        }

        private static bool HasSameStructure(IReadOnlyList<MobileItem> items, string referenceSelector)
        {
            if (string.IsNullOrWhiteSpace(referenceSelector)) return true;

            List<MobileItem> referenceItems;
            try
            {
                referenceItems = ParseSelector(referenceSelector);
            }
            catch
            {
                return false;
            }

            return referenceItems.Count == items.Count
                && items.Select(item => item?.name ?? string.Empty)
                    .SequenceEqual(referenceItems.Select(item => item?.name ?? string.Empty), StringComparer.Ordinal);
        }

        public static List<MobileSelectorNode> BuildSimilarSelector(string firstSelector, string secondSelector,
            MobileElement target, IReadOnlyList<MobileElement> elements)
        {
            List<MobileSelectorNode> common = MobileSelectorRules.BuildCommon(firstSelector, secondSelector);
            MobileElement[] snapshot = (elements ?? new MobileElement[0])
                .Where(element => target != null && IsSameSnapshot(element, target)).Distinct().ToArray();
            if (!snapshot.Contains(target))
                throw new InvalidOperationException("The selected element is no longer in the current snapshot");
            IReadOnlyList<MobileElement>[] paths = snapshot.Select(element => (IReadOnlyList<MobileElement>)GetPath(element)).ToArray();
            List<MobileSelectorNode> simplified = MobileSelectorRules.Simplify(common, paths,
                node => node.ClassName, (node, name) => GetValue(node, name));
            if (!MobileSelectorRules.MatchesPath(simplified, GetPath(target),
                node => node.ClassName, (node, name) => GetValue(node, name)))
                throw new InvalidOperationException("The similar selector does not match the selected element");
            return simplified;
        }

        public static IReadOnlyList<MobileElement> FindMatches(string selector, IReadOnlyList<MobileElement> elements)
        {
            List<MobileSelectorNode> rules = MobileSelectorRules.Parse(selector);
            if (MobileSelectorRules.UsesCurrentRules(selector))
            {
                // Compile first so unsupported constraints fail identically locally and at runtime.
                MobileSelectorRules.BuildXPath(rules);
                return (elements ?? new MobileElement[0]).Where(element => element != null
                    && MobileSelectorRules.MatchesPath(rules, GetPath(element),
                        node => node.ClassName, (node, name) => GetValue(node, name))).ToArray();
            }
            List<MobileItem> items = ParseSelector(selector);
            if (items.Count == 0 || elements == null) return new MobileElement[0];
            int firstEnabled = items.FindIndex(HasEnabledProperty);
            if (firstEnabled < 0) return new MobileElement[0];

            var result = new List<MobileElement>();
            foreach (MobileElement candidate in elements.Where(element => element != null))
            {
                List<MobileElement> path = GetPath(candidate);
                for (int anchorPathIndex = 0; anchorPathIndex < path.Count; anchorPathIndex++)
                {
                    if (!MatchesNode(path[anchorPathIndex], items[firstEnabled])) continue;
                    int remaining = items.Count - firstEnabled;
                    if (anchorPathIndex + remaining != path.Count) continue;

                    bool matched = true;
                    for (int itemIndex = firstEnabled + 1; itemIndex < items.Count; itemIndex++)
                    {
                        int pathIndex = anchorPathIndex + itemIndex - firstEnabled;
                        if (!MatchesNode(path[pathIndex], items[itemIndex]))
                        {
                            matched = false;
                            break;
                        }
                    }
                    if (matched)
                    {
                        result.Add(candidate);
                        break;
                    }
                }
            }
            return result;
        }

        private static List<MobileItem> ParseSelector(string selector)
        {
            if (string.IsNullOrWhiteSpace(selector)) return new List<MobileItem>();
            JToken token = JToken.Parse(selector);
            if (token.Type == JTokenType.Object) token = token["selector"] ?? token["Selector"];
            return token == null ? new List<MobileItem>() : token.ToObject<List<MobileItem>>() ?? new List<MobileItem>();
        }

        private static bool MatchesNode(MobileElement element, MobileItem item)
        {
            if (element == null || item == null || item.props == null) return false;
            List<Props> enabledProperties = item.props
                .Where(prop => prop != null && string.Equals(prop.accurate, "true", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (enabledProperties.Count == 0) return false;

            foreach (Props prop in enabledProperties)
            {
                string actual = GetValue(element, prop.name);
                if (!MatchesPattern(actual, prop.value, prop.pattern)) return false;
            }
            return true;
        }

        private static string GetValue(MobileElement element, string name)
        {
            switch (NormalizePropertyName(name))
            {
                case "id": return element.Id;
                case "description": return element.Desc;
                case "text": return element.Text;
                case "class": return element.ClassName;
                case "index": return element.NthChildIndex;
                default: return string.Empty;
            }
        }

        private static string NormalizePropertyName(string name)
        {
            switch ((name ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "name":
                case "class":
                    return "class";
                case "id":
                case "resource-id":
                case "resourceid":
                    return "id";
                case "description":
                case "content-desc":
                case "contentdesc":
                case "accessibility-id":
                case "accessibilityid":
                    return "description";
                case "text":
                    return "text";
                case "index":
                    return "index";
                default:
                    return string.Empty;
            }
        }

        private static bool MatchesPattern(string actual, string expected, string pattern)
        {
            actual = actual ?? string.Empty;
            expected = expected ?? string.Empty;
            switch ((pattern ?? "equal").Trim().ToLowerInvariant())
            {
                case "contain":
                case "contains":
                    return actual.Contains(expected);
                case "begin":
                case "starts-with":
                case "startswith":
                    return actual.StartsWith(expected, StringComparison.Ordinal);
                case "end":
                case "ends-with":
                case "endswith":
                    return actual.EndsWith(expected, StringComparison.Ordinal);
                case "regular":
                case "regex":
                    return Regex.IsMatch(actual, expected);
                default:
                    return string.Equals(actual, expected, StringComparison.Ordinal);
            }
        }

        private static bool TryEnableUniqueProperty(
            MobileItem item,
            string name,
            string value,
            IReadOnlyList<MobileElement> elements,
            Func<MobileElement, bool> predicate)
        {
            if (string.IsNullOrWhiteSpace(value) || elements.Count(predicate) != 1) return false;
            EnableProperty(item, name);
            return true;
        }

        private static bool TryEnableUniquePropertyCombination(
            MobileItem item,
            MobileElement element,
            IReadOnlyList<MobileElement> elements)
        {
            var available = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(element.Id)) available.Add(new KeyValuePair<string, string>("id", element.Id));
            if (!string.IsNullOrWhiteSpace(element.Desc)) available.Add(new KeyValuePair<string, string>("description", element.Desc));
            if (!string.IsNullOrWhiteSpace(element.Text)) available.Add(new KeyValuePair<string, string>("text", element.Text));
            if (available.Count < 2) return false;

            bool unique = elements.Count(candidate =>
                (string.IsNullOrWhiteSpace(element.Id) || candidate.Id == element.Id)
                && (string.IsNullOrWhiteSpace(element.Desc) || candidate.Desc == element.Desc)
                && (string.IsNullOrWhiteSpace(element.Text) || candidate.Text == element.Text)) == 1;
            if (!unique) return false;
            foreach (KeyValuePair<string, string> property in available) EnableProperty(item, property.Key);
            return true;
        }

        private static void EnableStructuralPath(MobileItem item)
        {
            if (!EnableProperty(item, "class")) EnableProperty(item, "name");
            EnableProperty(item, "index");
        }

        private static bool EnableProperty(MobileItem item, string propertyName)
        {
            if (item == null || item.props == null) return false;
            Props property = item.props.FirstOrDefault(prop =>
                string.Equals(NormalizePropertyName(prop.name), NormalizePropertyName(propertyName), StringComparison.Ordinal));
            if (property == null || string.IsNullOrWhiteSpace(property.value)) return false;
            property.accurate = "true";
            return true;
        }

        private static bool HasEnabledProperty(MobileItem item)
        {
            return item != null && item.props != null
                && item.props.Any(prop => prop != null
                    && !string.IsNullOrWhiteSpace(prop.value)
                    && string.Equals(prop.accurate, "true", StringComparison.OrdinalIgnoreCase));
        }

        private static void ResetAccurate(IEnumerable<MobileItem> items)
        {
            foreach (MobileItem item in items)
            {
                item.accurate = "false";
                if (item.props == null) continue;
                foreach (Props prop in item.props) prop.accurate = "false";
            }
        }

        private static void SynchronizeNodeAccurate(IEnumerable<MobileItem> items)
        {
            foreach (MobileItem item in items) item.accurate = HasEnabledProperty(item) ? "true" : "false";
        }

        private static bool IsSameSnapshot(MobileElement candidate, MobileElement target)
        {
            return candidate != null
                && candidate.SourceSnapshotVersion == target.SourceSnapshotVersion
                && candidate.SourceGeneration == target.SourceGeneration
                && string.Equals(candidate.SourceProvider, target.SourceProvider, StringComparison.OrdinalIgnoreCase);
        }

        private static List<MobileElement> GetPath(MobileElement element)
        {
            var path = new List<MobileElement>();
            for (MobileElement current = element; current != null; current = current.ParentElement) path.Add(current);
            path.Reverse();
            return path;
        }
    }
}