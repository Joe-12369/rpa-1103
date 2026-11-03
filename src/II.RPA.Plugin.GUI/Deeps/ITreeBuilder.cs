using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Deeps.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Deeps
{
    public interface ITreeBuilder
    {
        ValueTask<(ElementModel Root, List<string> Path)> BuildAsync(AutomationElement automationElement, CancellationToken cancellationToken);
        (ElementModel Root, List<string> Path) Build(AutomationElement automationElement);
    }
}
