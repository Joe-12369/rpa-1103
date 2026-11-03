using System;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.Services
{
    public interface IRuntimeWorkerService
    {
        Task SendMessageAsync(RuntimeMessageModel message);
        void SendMessage(RuntimeMessageModel message);
        IObservable<RuntimeMessageModel> MessageStream { get; }
    }

}
