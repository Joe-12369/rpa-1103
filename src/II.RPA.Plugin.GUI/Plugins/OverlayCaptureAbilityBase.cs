using System;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Plugin.GUI
{
    // Shared by every ability that uses the same overlay, including different generic types.
    internal static class OverlayCaptureGate
    {
        private static int occupied;

        internal static bool IsOccupied => Volatile.Read(ref occupied) != 0;
        internal static bool TryEnter() => Interlocked.CompareExchange(ref occupied, 1, 0) == 0;
        internal static void Exit() => Volatile.Write(ref occupied, 0);
    }

    public abstract class OverlayCaptureAbilityBase<TParam> : PluginAbilityBase<TParam>
        where TParam : PluginParameterBase
    {
        public override Task<string> RunAsync(PluginExecutionContext context)
        {
            if (!GuiElement.OverlayDispatcher.CheckAccess())
                return GuiElement.OverlayDispatcher.Invoke(() => RunAsync(context));

            return RunOnOverlayAsync(context);
        }

        private async Task<string> RunOnOverlayAsync(PluginExecutionContext context)
        {
            // Acquire before parameter validation: validation also changes shared capture state.
            if (!OverlayCaptureGate.TryEnter())
                return BusyResult();

            OverlayForm form = null;
            bool ownsForm = false;
            try
            {
                form = GuiElement.overlayForm;
                if (form != null && !form.IsDisposed && !form.CaptureDialogFailed && form.Modal)
                    return BusyResult();

                form = GuiElement.PrepareOverlayForm();
                ownsForm = true;
                return await base.RunAsync(context);
            }
            catch (Exception ex)
            {
                var result = new JMessage();
                Exception(ex, eErrorMessageType.Error_ElementCaptureError, ref result);
                return result.ToJson();
            }
            finally
            {
                try
                {
                    // A modal window owned by another caller must be left untouched.
                    if (ownsForm)
                    {
                        form.CleanupCapture();
                        GuiElement.OnSelected = null;
                        GuiElement.OnCanceled = null;
                        GuiElement.OnSelectored = null;
                    }
                }
                finally
                {
                    OverlayCaptureGate.Exit();
                }
            }
        }

        private string BusyResult()
        {
            var result = new JMessage();
            Exception(eErrorMessageType.Error_CannotParallelExcuteError, ref result);
            // Do not run capture-specific result handlers: they release the active caller's resources.
            return result.ToJson();
        }
    }
}