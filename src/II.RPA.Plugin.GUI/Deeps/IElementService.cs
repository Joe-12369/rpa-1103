using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Deeps.Models;
using System;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Deeps
{
    public interface IElementService
    {
        IObservable<UiaTreeResultDto> PeerStream { get; }
        string GetDescendants(string payload);
        string GetDescendantsCount(string payload);
        string GetElementDetail(string payload);
        string BuildSelector(string payload);
        string StartHighlight(string payload);
        string StopHighlight(string payload);
        Task StartPeerElementAsync(AutomationElement automationElement);
        void Start();
        void Stop();
    }
}