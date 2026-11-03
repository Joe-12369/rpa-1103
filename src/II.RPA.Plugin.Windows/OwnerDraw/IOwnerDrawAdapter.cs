using System;
using System.Drawing;

namespace II.RPA.Plugin.Windows.OwnerDraw
{
    /// <summary>
    /// Framework-neutral owner-draw extension point. It deliberately does not
    /// reference UIANode or IGuiNode; the existing Windows plugin only needs a
    /// thin node wrapper at its two integration points.
    /// </summary>
    internal interface IOwnerDrawAdapter
    {
        string AdapterId { get; }

        int Priority { get; }

        bool TryLocateHost(Point screenPoint, IntPtr candidateHwnd,
            out IntPtr hostHwnd, out string status);

        bool CanResolve(OwnerDrawSelector selector);

        OwnerDrawResult<OwnerDrawElementSnapshot> Pick(OwnerDrawPickRequest request);

        OwnerDrawResult<OwnerDrawElementSnapshot> Resolve(IntPtr hostHwnd,
            OwnerDrawSelector selector);

        OwnerDrawResult<string> GetText(IntPtr hostHwnd, OwnerDrawSelector selector);

        OwnerDrawResult<Rectangle> GetBounds(IntPtr hostHwnd, OwnerDrawSelector selector);
    }

    internal abstract class OwnerDrawAdapterBase : IOwnerDrawAdapter
    {
        public abstract string AdapterId { get; }

        public abstract int Priority { get; }

        public abstract bool TryLocateHost(Point screenPoint, IntPtr candidateHwnd,
            out IntPtr hostHwnd, out string status);

        public virtual bool CanResolve(OwnerDrawSelector selector)
        {
            return selector != null && string.Equals(selector.AdapterId, AdapterId,
                StringComparison.OrdinalIgnoreCase);
        }

        public abstract OwnerDrawResult<OwnerDrawElementSnapshot> Pick(
            OwnerDrawPickRequest request);

        public abstract OwnerDrawResult<OwnerDrawElementSnapshot> Resolve(
            IntPtr hostHwnd, OwnerDrawSelector selector);

        public virtual OwnerDrawResult<string> GetText(IntPtr hostHwnd,
            OwnerDrawSelector selector)
        {
            OwnerDrawResult<OwnerDrawElementSnapshot> resolved = Resolve(hostHwnd, selector);
            if (!resolved.IsSuccess)
                return OwnerDrawResult<string>.Failed(resolved.Code, resolved.Status);
            if (!resolved.Value.TextAvailable)
                return OwnerDrawResult<string>.Failed(OwnerDrawFailureCode.ProtocolMismatch,
                    "Owner-draw bridge did not provide a text state; " + resolved.Status);
            return OwnerDrawResult<string>.Success(resolved.Value.Text ?? string.Empty,
                resolved.Status);
        }

        public virtual OwnerDrawResult<Rectangle> GetBounds(IntPtr hostHwnd,
            OwnerDrawSelector selector)
        {
            OwnerDrawResult<OwnerDrawElementSnapshot> resolved = Resolve(hostHwnd, selector);
            if (!resolved.IsSuccess)
                return OwnerDrawResult<Rectangle>.Failed(resolved.Code, resolved.Status);
            if (resolved.Value.ScreenBounds.Width <= 0
                || resolved.Value.ScreenBounds.Height <= 0)
            {
                return OwnerDrawResult<Rectangle>.Failed(OwnerDrawFailureCode.ProtocolMismatch,
                    "Owner-draw bridge returned invalid current bounds; " + resolved.Status);
            }
            return OwnerDrawResult<Rectangle>.Success(resolved.Value.ScreenBounds,
                resolved.Status);
        }
    }
}
