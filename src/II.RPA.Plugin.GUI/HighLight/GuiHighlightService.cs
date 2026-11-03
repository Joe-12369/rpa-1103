using II.RPA.Plugin.Core.Threading;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI
{
    public sealed class GuiHighlightService
    {
        private readonly IStaDispatcher staDispatcher;

        public GuiHighlightService(IStaDispatcher staDispatcher)
        {
            this.staDispatcher = staDispatcher ?? throw new ArgumentNullException(nameof(staDispatcher));
        }

        public Task<bool> HighlightAsync(List<Rectangle> rectangles, bool useTightBorder = false)
        {
            return staDispatcher.InvokeAsync(() => GuiElement.Highlightings(rectangles, useTightBorder));
        }

        public Task<bool> HighlightAsync(GuiElement element)
        {
            return staDispatcher.InvokeAsync(() => GuiElement.ElementHighLighterFlash(element));
        }
    }
}
