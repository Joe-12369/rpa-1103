using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace II.RPA.WinAutomation.CommonUtility
{
    public sealed class MobileSelectorNode
    {
        public string name { get; set; }
        public string type { get; set; }
        public string accurate { get; set; }
        public List<MobileSelectorProperty> props { get; set; } = new List<MobileSelectorProperty>();
    }

    public sealed class MobileSelectorProperty
    {
        public string name { get; set; }
        public string value { get; set; }
        public string pattern { get; set; }
        public string accurate { get; set; }
    }

    // Mobile commonProps.version opts into shared rules without changing selector nodes.
    public static class MobileSelectorRules
    {
        public const string CurrentVersion = "1.7";

        public static List<MobileSelectorNode> Parse(string selector)
        {
            JToken token = JToken.Parse(selector);
            if (token.Type == JTokenType.Object) token = token["selector"] ?? token["Selector"];
            if (!(token is JArray)) throw new ArgumentException("Invalid mobile selector");
            return token.ToObject<List<MobileSelectorNode>>() ?? new List<MobileSelectorNode>();
        }

        public static bool UsesCurrentRules(string selector)
        {
            if (string.IsNullOrWhiteSpace(selector)) return false;
            JToken token = JToken.Parse(selector);
            if (!(token is JObject)) return false;
            return UsesCurrentRules((token["commonProps"] ?? token["CommonProps"])?.ToObject<CommonProps>());
        }

        public static bool UsesCurrentRules(CommonProps commonProps)
        {
            return string.Equals(commonProps?.GUIType, "mobile", StringComparison.OrdinalIgnoreCase)
                && string.Equals(commonProps?.Version, CurrentVersion, StringComparison.Ordinal);
        }

        public static string NormalizeName(string name)
        {
            switch ((name ?? "").Trim().ToLowerInvariant())
            {
                case "name": case "class": return "class";
                case "id": case "resource-id": case "resourceid": return "id";
                case "description": case "content-desc": case "contentdesc":
                case "accessibility-id": case "accessibilityid": return "description";
                case "text": return "text";
                case "index": return "index";
                default: throw new ArgumentException("Unsupported mobile selector property: " + name);
            }
        }

        public static string NormalizePattern(string pattern)
        {
            switch ((pattern ?? "equal").Trim().ToLowerInvariant())
            {
                case "equal": case "": return "equal";
                case "contain": case "contains": return "contains";
                case "begin": case "starts-with": case "startswith": return "starts-with";
                case "end": case "ends-with": case "endswith": return "ends-with";
                case "regular": case "regex": return "regex";
                default: throw new ArgumentException("Unsupported mobile selector pattern: " + pattern);
            }
        }

        public static bool IsEnabled(MobileSelectorProperty property)
        {
            return property != null && string.Equals(property.accurate, "true", StringComparison.OrdinalIgnoreCase);
        }

        public static bool MatchesValue(string actual, string expected, string pattern)
        {
            actual = actual ?? "";
            expected = expected ?? "";
            switch (NormalizePattern(pattern))
            {
                case "contains": return actual.Contains(expected);
                case "starts-with": return actual.StartsWith(expected, StringComparison.Ordinal);
                case "ends-with": return actual.EndsWith(expected, StringComparison.Ordinal);
                case "regex": return Regex.IsMatch(actual, expected, RegexOptions.None, TimeSpan.FromMilliseconds(250));
                default: return string.Equals(actual, expected, StringComparison.Ordinal);
            }
        }

        public static bool MatchesNode(MobileSelectorNode selector, string className, Func<string, string> getValue)
        {
            if (selector == null) return false;
            if (!string.IsNullOrWhiteSpace(selector.name)
                && !string.Equals(selector.name, className, StringComparison.Ordinal)) return false;
            return (selector.props ?? new List<MobileSelectorProperty>()).Where(IsEnabled)
                .All(property => MatchesValue(getValue(NormalizeName(property.name)), property.value, property.pattern));
        }

        public static int FirstEnabled(IReadOnlyList<MobileSelectorNode> selector)
        {
            for (int i = 0; i < selector.Count; i++)
                if (selector[i]?.props?.Any(IsEnabled) == true) return i;
            return -1;
        }

        public static bool MatchesPath<T>(IReadOnlyList<MobileSelectorNode> selector, IReadOnlyList<T> path,
            Func<T, string> getClass, Func<T, string, string> getValue)
        {
            if (selector == null || selector.Count == 0 || path == null) return false;
            int first = FirstEnabled(selector);
            if (first < 0) return false;
            int offset = path.Count - selector.Count;
            if (offset + first < 0) return false;
            for (int i = first; i < selector.Count; i++)
            {
                T element = path[offset + i];
                if (!MatchesNode(selector[i], getClass(element), name => getValue(element, name))) return false;
            }
            return true;
        }

        public static string BuildXPath(IReadOnlyList<MobileSelectorNode> selector)
        {
            int first = FirstEnabled(selector);
            if (first < 0) throw new ArgumentException("Mobile selector has no enabled properties");
            var xpath = new StringBuilder();
            for (int i = first; i < selector.Count; i++)
            {
                MobileSelectorNode node = selector[i];
                if (node == null) throw new ArgumentException("Mobile selector contains an empty node");
                xpath.Append(i == first ? "//*" : "/*");
                var predicates = new List<string>();
                if (!string.IsNullOrWhiteSpace(node.name)) predicates.Add("@class=" + Literal(node.name));
                foreach (MobileSelectorProperty property in (node.props ?? new List<MobileSelectorProperty>()).Where(IsEnabled))
                {
                    string name = NormalizeName(property.name);
                    string attribute = name == "id" ? "resource-id" : name == "description" ? "content-desc" : name;
                    string value = Literal(property.value ?? "");
                    switch (NormalizePattern(property.pattern))
                    {
                        case "contains": predicates.Add($"contains(@{attribute}, {value})"); break;
                        case "starts-with": predicates.Add($"starts-with(@{attribute}, {value})"); break;
                        case "ends-with":
                            predicates.Add($"substring(@{attribute}, string-length(@{attribute}) - string-length({value}) + 1) = {value}");
                            break;
                        case "regex": throw new ArgumentException("Mobile selector version 1.7 does not support regex; use equal/contains/starts-with/ends-with");
                        default: predicates.Add($"@{attribute}={value}"); break;
                    }
                }
                if (predicates.Count > 0) xpath.Append("[" + string.Join(" and ", predicates) + "]");
            }
            return xpath.ToString();
        }

        public static List<MobileSelectorNode> BuildCommon(string firstSelector, string secondSelector)
        {
            List<MobileSelectorNode> first = Parse(firstSelector);
            List<MobileSelectorNode> second = Parse(secondSelector);
            if (first.Count == 0 || first.Count != second.Count
                || !first.Select(node => node?.name).SequenceEqual(second.Select(node => node?.name), StringComparer.Ordinal))
                throw new ArgumentException("Similar elements must have matching structural paths");

            for (int i = 0; i < second.Count; i++)
            {
                MobileSelectorNode node = second[i];
                List<MobileSelectorProperty> common = new List<MobileSelectorProperty>();
                foreach (MobileSelectorProperty property in node.props ?? new List<MobileSelectorProperty>())
                {
                    if (property == null || string.IsNullOrWhiteSpace(property.value)) continue;
                    string name = NormalizeName(property.name);
                    string pattern = NormalizePattern(property.pattern);
                    MobileSelectorProperty other = first[i].props?.FirstOrDefault(candidate => candidate != null
                        && NormalizeName(candidate.name) == name
                        && NormalizePattern(candidate.pattern) == pattern
                        && string.Equals(candidate.value, property.value, StringComparison.Ordinal));
                    if (other == null || common.Any(candidate => candidate.name == name)) continue;
                    common.Add(new MobileSelectorProperty
                    {
                        name = name, value = property.value, pattern = pattern,
                        accurate = (name == "id" || (name == "index"
                            ? IsEnabled(property) && IsEnabled(other)
                            : IsEnabled(property) || IsEnabled(other))) ? "true" : "false"
                    });
                }
                // A changed constraint is removed rather than replaced with a positional fallback.
                // Common resource IDs retain scope even when the two capture passes chose different anchors.
                MobileSelectorProperty classProperty = common.FirstOrDefault(property => property.name == "class");
                if (classProperty == null && !string.IsNullOrWhiteSpace(node.name))
                {
                    classProperty = new MobileSelectorProperty { name = "class", value = node.name, pattern = "equal" };
                    common.Add(classProperty);
                }
                bool needsClass = i == second.Count - 1
                    || common.Any(IsEnabled) || (node.props?.Any(IsEnabled) == true) || (first[i].props?.Any(IsEnabled) == true);
                if (classProperty != null) classProperty.accurate = needsClass ? "true" : "false";
                node.props = common;
                node.accurate = common.Any(IsEnabled) ? "true" : "false";
            }
            if (FirstEnabled(second) < 0) throw new ArgumentException("No common similar element constraints");
            foreach (string sample in new[] { firstSelector, secondSelector })
            {
                List<MobileSelectorNode> path = Parse(sample);
                if (!MatchesPath(second, path, node => node.name,
                    (node, name) => name == "class" ? node.name
                        : node.props?.FirstOrDefault(property => property != null
                            && NormalizeName(property.name) == name)?.value))
                    throw new ArgumentException("Common selector does not match both selected samples");
            }
            BuildXPath(second);
            return second;
        }

        public static List<MobileSelectorNode> BindUnique<T>(IReadOnlyList<MobileSelectorNode> selector,
            IReadOnlyList<IReadOnlyList<T>> paths, int targetIndex,
            Func<T, string> getClass, Func<T, string, string> getValue)
        {
            if (paths == null || targetIndex < 0 || targetIndex >= paths.Count)
                throw new ArgumentException("Invalid similar element snapshot target");

            bool IsUniqueTarget(List<MobileSelectorNode> candidate)
            {
                if (!MatchesPath(candidate, paths[targetIndex], getClass, getValue)) return false;
                for (int i = 0; i < paths.Count; i++)
                    if (i != targetIndex && MatchesPath(candidate, paths[i], getClass, getValue)) return false;
                return true;
            }

            // Prefer intrinsic attributes over row positions, but never a list-wide ordinal.
            string[][] anchors = {
                new[] { "id" }, new[] { "id", "description" }, new[] { "id", "text" },
                new[] { "id", "description", "text" }, new[] { "description" },
                new[] { "text" }, new[] { "description", "text" }
            };
            for (int start = selector.Count - 1; start >= 0; start--)
            {
                foreach (string[] names in anchors)
                {
                    var candidate = JArray.FromObject(selector.Skip(start)).ToObject<List<MobileSelectorNode>>();
                    var properties = names.Select(name => candidate[0].props?.FirstOrDefault(property => property != null
                        && NormalizeName(property.name) == name && !string.IsNullOrWhiteSpace(property.value)
                        && NormalizePattern(property.pattern) == "equal")).ToArray();
                    if (properties.Any(property => property == null)) continue;
                    foreach (MobileSelectorNode node in candidate)
                        foreach (MobileSelectorProperty property in node.props ?? new List<MobileSelectorProperty>())
                            property.accurate = "false";
                    foreach (MobileSelectorProperty property in properties) property.accurate = "true";
                    if (IsUniqueTarget(candidate))
                        return Simplify(candidate, paths, getClass, getValue);
                }
            }
            var fallback = JArray.FromObject(selector).ToObject<List<MobileSelectorNode>>();
            BuildXPath(fallback);
            if (!IsUniqueTarget(fallback))
                throw new InvalidOperationException("Matched similar element cannot be uniquely bound in the current snapshot");
            return Simplify(fallback, paths, getClass, getValue);
        }

        public static List<MobileSelectorNode> Simplify<T>(IReadOnlyList<MobileSelectorNode> selector,
            IReadOnlyList<IReadOnlyList<T>> paths, Func<T, string> getClass, Func<T, string, string> getValue)
        {
            var result = JArray.FromObject(selector).ToObject<List<MobileSelectorNode>>();
            BuildXPath(result);
            foreach (MobileSelectorNode node in result)
                if (node.props == null) node.props = new List<MobileSelectorProperty>();
            if (paths == null || paths.Count == 0) return result;
            bool[] expected = paths.Select(path => MatchesPath(result, path, getClass, getValue)).ToArray();
            if (!expected.Any(match => match)) return result;

            int scope = result.Count - 1;
            int[] matchedIndexes = Enumerable.Range(0, paths.Count).Where(index => expected[index]).ToArray();
            if (matchedIndexes.Length > 1)
            {
                // Preserve the shared collection anchor, even if every visible target has the same ID.
                for (int level = result.Count - 2; level >= FirstEnabled(result); level--)
                {
                    if (!result[level].props.Any(property => IsEnabled(property)
                        && NormalizeName(property.name) == "id")) continue;
                    int firstPathLevel = paths[matchedIndexes[0]].Count - result.Count + level;
                    if (firstPathLevel < 0) continue;
                    T ancestor = paths[matchedIndexes[0]][firstPathLevel];
                    if (!matchedIndexes.All(index => paths[index].Count - result.Count + level >= 0
                        && EqualityComparer<T>.Default.Equals(ancestor, paths[index][paths[index].Count - result.Count + level]))) continue;
                    scope = level;
                    break;
                }
            }

            bool PreservesMatches(List<MobileSelectorNode> candidate)
            {
                if (FirstEnabled(candidate) < 0) return false;
                for (int i = 0; i < paths.Count; i++)
                    if (MatchesPath(candidate, paths[i], getClass, getValue) != expected[i]) return false;
                return true;
            }

            // Remove positions first; retain text unless the same node has a resource ID.
            foreach (string propertyName in new[] { "index", "text", "description" })
            {
                foreach (MobileSelectorNode node in result)
                {
                    if (propertyName != "index" && !node.props.Any(property => IsEnabled(property)
                        && NormalizeName(property.name) == "id")) continue;
                    foreach (MobileSelectorProperty property in node.props.Where(property => IsEnabled(property)
                        && NormalizeName(property.name) == propertyName))
                    {
                        property.accurate = "false";
                        if (!PreservesMatches(result)) property.accurate = "true";
                    }
                }
            }

            // A stable attribute may replace ancestor scope; class alone cannot.
            for (int start = scope; start > FirstEnabled(result); start--)
            {
                if (!result[start].props.Any(property => IsEnabled(property)
                    && new[] { "id", "description", "text" }.Contains(NormalizeName(property.name)))) continue;
                List<MobileSelectorNode> suffix = result.Skip(start).ToList();
                if (!PreservesMatches(suffix)) continue;
                result = suffix;
                break;
            }
            int first = FirstEnabled(result);
            if (first > 0) result = result.Skip(first).ToList();
            foreach (MobileSelectorNode node in result)
                node.accurate = node.props.Any(IsEnabled) ? "true" : "false";
            BuildXPath(result);
            return result;
        }

        private static string Literal(string value)
        {
            if (!value.Contains("'")) return "'" + value + "'";
            if (!value.Contains("\"")) return "\"" + value + "\"";
            return "concat(" + string.Join(", \"'\", ", value.Split('\'').Select(part => "'" + part + "'")) + ")";
        }
    }
}
