using II.RPA.Plugin.Windows.OwnerDraw.PowerBuilder;
using II.RPA.Plugin.Windows.OwnerDraw.Vcl;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    /// <summary>
    /// Composition boundary for owner-draw support. Ordinary UIA/MSAA callers only
    /// invoke this registry after they already have their normal fallback node.
    /// </summary>
    internal sealed class OwnerDrawAdapterRegistry
    {
        private readonly object gate = new object();
        private readonly List<IOwnerDrawAdapter> adapters
            = new List<IOwnerDrawAdapter>();

        internal OwnerDrawAdapterRegistry(IEnumerable<IOwnerDrawAdapter> adapters = null)
        {
            if (adapters != null)
            {
                foreach (IOwnerDrawAdapter adapter in adapters) Register(adapter);
            }
        }

        internal static OwnerDrawAdapterRegistry CreateDefault(
            OwnerDrawBridgeManager bridgeManager = null)
        {
            bridgeManager = bridgeManager ?? OwnerDrawBridgeManager.Shared;
            return new OwnerDrawAdapterRegistry(new IOwnerDrawAdapter[]
            {
                new PowerBuilderDataWindowAdapter(bridgeManager),
                new VclGridAdapter(bridgeManager),
            });
        }

        internal static OwnerDrawAdapterRegistry Shared { get; }
            = CreateDefault();

        internal void Register(IOwnerDrawAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException("adapter");
            lock (gate)
            {
                if (adapters.Any(item => string.Equals(item.AdapterId,
                    adapter.AdapterId, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Owner-draw adapter already registered: "
                        + adapter.AdapterId + ".");
                }
                adapters.Add(adapter);
                adapters.Sort((left, right) => right.Priority.CompareTo(left.Priority));
            }
        }

        /// <summary>
        /// Returns NotApplicable/RecognizedButFailed without suppressing the caller's
        /// pre-existing UIA result. That is the interactive fail-open contract.
        /// </summary>
        internal OwnerDrawResult<OwnerDrawElementSnapshot> TryPick(Point screenPoint,
            IntPtr candidateHwnd)
        {
            var request = new OwnerDrawPickRequest(screenPoint, candidateHwnd);
            OwnerDrawResult<OwnerDrawElementSnapshot> firstFailure = null;
            foreach (IOwnerDrawAdapter adapter in Snapshot())
            {
                try
                {
                    OwnerDrawResult<OwnerDrawElementSnapshot> result = adapter.Pick(request)
                        ?? OwnerDrawResult<OwnerDrawElementSnapshot>.Failed(
                            OwnerDrawFailureCode.InvalidRequest,
                            "Owner-draw adapter returned no result: " + adapter.AdapterId + ".");
                    if (result.IsSuccess) return result;
                    if (result.State == OwnerDrawProviderState.RecognizedButFailed
                        && firstFailure == null) firstFailure = result;
                }
                catch (Exception ex)
                {
                    if (firstFailure == null)
                    {
                        firstFailure = OwnerDrawResult<OwnerDrawElementSnapshot>.Failed(
                            OwnerDrawFailureCode.InvalidRequest,
                            "Owner-draw pick exception isolated adapter=" + adapter.AdapterId
                            + ": " + ex.GetType().Name + ": " + ex.Message);
                    }
                }
            }
            return firstFailure ?? OwnerDrawResult<OwnerDrawElementSnapshot>.NotApplicable();
        }

        /// <summary>
        /// Explicit owner-draw selector replay is fail-closed. Once its adapter marker
        /// is decoded, no UIA host may be substituted for the requested virtual cell.
        /// </summary>
        internal OwnerDrawResult<OwnerDrawElementSnapshot> Resolve(IntPtr hostHwnd,
            OwnerDrawSelector selector)
        {
            if (!OwnerDrawSelectorCodec.TryValidate(selector, out string selectorStatus))
            {
                return OwnerDrawResult<OwnerDrawElementSnapshot>.Failed(
                    OwnerDrawFailureCode.InvalidSelector, selectorStatus);
            }
            IOwnerDrawAdapter adapter = Snapshot().FirstOrDefault(candidate =>
                candidate.CanResolve(selector));
            if (adapter == null)
            {
                return OwnerDrawResult<OwnerDrawElementSnapshot>.Failed(
                    OwnerDrawFailureCode.InvalidSelector,
                    "No registered owner-draw adapter accepts '" + selector.AdapterId + "'.");
            }
            try
            {
                OwnerDrawResult<OwnerDrawElementSnapshot> result
                    = adapter.Resolve(hostHwnd, selector);
                if (result == null || result.State == OwnerDrawProviderState.NotApplicable)
                {
                    return OwnerDrawResult<OwnerDrawElementSnapshot>.Failed(
                        OwnerDrawFailureCode.NotFound,
                        "Explicit owner-draw selector was recognized but did not resolve; "
                        + selectorStatus);
                }
                return result;
            }
            catch (Exception ex)
            {
                return OwnerDrawResult<OwnerDrawElementSnapshot>.Failed(
                    OwnerDrawFailureCode.NotFound,
                    "Explicit owner-draw selector failed adapter=" + adapter.AdapterId
                    + ": " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        internal OwnerDrawResult<OwnerDrawElementSnapshot> DecodeAndResolve(
            IntPtr hostHwnd, IEnumerable<OwnerDrawSelectorProperty> leafProperties)
        {
            if (!OwnerDrawSelectorCodec.TryDecode(leafProperties,
                out OwnerDrawSelector selector, out string status))
            {
                return OwnerDrawResult<OwnerDrawElementSnapshot>.NotApplicable(status);
            }
            return Resolve(hostHwnd, selector);
        }

        internal OwnerDrawResult<OwnerDrawElementSnapshot> DecodeAndResolve(
            IntPtr hostHwnd, IReadOnlyDictionary<string, string> leafProperties)
        {
            if (!OwnerDrawSelectorCodec.TryDecode(leafProperties,
                out OwnerDrawSelector selector, out string status))
            {
                return OwnerDrawResult<OwnerDrawElementSnapshot>.NotApplicable(status);
            }
            return Resolve(hostHwnd, selector);
        }

        internal OwnerDrawResult<string> GetText(IntPtr hostHwnd,
            OwnerDrawSelector selector)
        {
            IOwnerDrawAdapter adapter = Snapshot().FirstOrDefault(candidate =>
                candidate.CanResolve(selector));
            return adapter == null
                ? OwnerDrawResult<string>.Failed(OwnerDrawFailureCode.InvalidSelector,
                    "No adapter accepts this owner-draw selector.")
                : adapter.GetText(hostHwnd, selector);
        }

        internal OwnerDrawResult<Rectangle> GetBounds(IntPtr hostHwnd,
            OwnerDrawSelector selector)
        {
            IOwnerDrawAdapter adapter = Snapshot().FirstOrDefault(candidate =>
                candidate.CanResolve(selector));
            return adapter == null
                ? OwnerDrawResult<Rectangle>.Failed(OwnerDrawFailureCode.InvalidSelector,
                    "No adapter accepts this owner-draw selector.")
                : adapter.GetBounds(hostHwnd, selector);
        }

        private IOwnerDrawAdapter[] Snapshot()
        {
            lock (gate) return adapters.ToArray();
        }
    }
}
