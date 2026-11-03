using II.RPA.Plugin.Core.Commons;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    /// <summary>
    /// Separates the synthetic owner-draw leaf from the ordinary Windows host
    /// selector.  The UIA matcher must never see the synthetic leaf.
    /// </summary>
    internal sealed class OwnerDrawReplayRequest
    {
        internal string HostSelectorJson { get; set; }

        internal OwnerDrawSelector Selector { get; set; }
    }

    internal static class OwnerDrawSelectorEnvelope
    {
        internal static OwnerDrawResult<OwnerDrawReplayRequest> TrySplit(
            string selectorJson)
        {
            if (string.IsNullOrWhiteSpace(selectorJson))
                return OwnerDrawResult<OwnerDrawReplayRequest>.NotApplicable(
                    "Windows selector is empty.");

            JArray array;
            try
            {
                array = JArray.Parse(selectorJson);
            }
            catch (JsonException ex)
            {
                return OwnerDrawResult<OwnerDrawReplayRequest>.NotApplicable(
                    "Windows selector JSON is invalid: " + ex.Message);
            }

            if (array.Count == 0)
                return OwnerDrawResult<OwnerDrawReplayRequest>.NotApplicable(
                    "Windows selector has no nodes.");

            List<SelectorProps> parsed;
            try
            {
                parsed = array.ToObject<List<SelectorProps>>()
                    ?? new List<SelectorProps>();
            }
            catch (JsonException ex)
            {
                return LooksLikeOwnerDraw(array)
                    ? OwnerDrawResult<OwnerDrawReplayRequest>.Failed(
                        OwnerDrawFailureCode.InvalidSelector,
                        "Owner-draw selector structure is invalid: " + ex.Message)
                    : OwnerDrawResult<OwnerDrawReplayRequest>.NotApplicable(
                        "Windows selector structure is invalid: " + ex.Message);
            }

            List<int> markerIndices = Enumerable.Range(0, parsed.Count)
                .Where(index => ContainsOwnerDrawReservedProperty(parsed[index]))
                .ToList();
            if (markerIndices.Count == 0)
                return OwnerDrawResult<OwnerDrawReplayRequest>.NotApplicable();

            // A recognized marker changes replay semantics to fail-closed.  Do not
            // silently pass malformed or misplaced leaves to ordinary UIA matching.
            if (markerIndices.Count != 1)
            {
                return OwnerDrawResult<OwnerDrawReplayRequest>.Failed(
                    OwnerDrawFailureCode.InvalidSelector,
                    "Owner-draw selector must contain exactly one virtual leaf.");
            }
            int markerIndex = markerIndices[0];
            if (markerIndex != parsed.Count - 1 || markerIndex == 0)
            {
                return OwnerDrawResult<OwnerDrawReplayRequest>.Failed(
                    OwnerDrawFailureCode.InvalidSelector,
                    "Owner-draw selector must contain a Windows host followed by one final virtual leaf.");
            }

            SelectorProps leaf = parsed[markerIndex];
            if (!string.Equals(leaf.Name, "Cell", StringComparison.OrdinalIgnoreCase)
                || !IsTrue(leaf.Accurate))
            {
                return OwnerDrawResult<OwnerDrawReplayRequest>.Failed(
                    OwnerDrawFailureCode.InvalidSelector,
                    "Owner-draw virtual leaf must be an enabled Cell node.");
            }
            var properties = (leaf.Props ?? new List<Props>()).Select(property =>
                new OwnerDrawSelectorProperty(property.Name, property.Value,
                    IsTrue(property.Accurate))).ToList();
            if (!OwnerDrawSelectorCodec.TryDecode(properties,
                out OwnerDrawSelector selector, out string decodeStatus))
            {
                return OwnerDrawResult<OwnerDrawReplayRequest>.Failed(
                    OwnerDrawFailureCode.InvalidSelector, decodeStatus);
            }

            var host = new JArray(array.Take(array.Count - 1)
                .Select(token => token.DeepClone()));
            return OwnerDrawResult<OwnerDrawReplayRequest>.Success(
                new OwnerDrawReplayRequest
                {
                    HostSelectorJson = host.ToString(Formatting.None),
                    Selector = selector,
                }, decodeStatus);
        }

        private static bool ContainsOwnerDrawReservedProperty(SelectorProps node)
        {
            if (node == null || node.Props == null) return false;
            return node.Props.Any(property => property != null
                && OwnerDrawSelectorCodec.IsReservedSelectorPropertyName(
                    property.Name));
        }

        private static bool LooksLikeOwnerDraw(JToken token)
        {
            if (token == null) return false;
            if (token is JObject item)
            {
                JProperty name = item.Properties().FirstOrDefault(property =>
                    string.Equals(property.Name, "Name",
                        StringComparison.OrdinalIgnoreCase));
                if (name != null
                    && OwnerDrawSelectorCodec.IsReservedSelectorPropertyName(
                        name.Value.Type == JTokenType.String
                            ? name.Value.Value<string>() : string.Empty))
                {
                    return true;
                }
                return item.Properties().Any(property =>
                    LooksLikeOwnerDraw(property.Value));
            }
            if (token is JArray array)
                return array.Any(LooksLikeOwnerDraw);
            return false;
        }

        private static bool IsTrue(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
        }
    }
}
