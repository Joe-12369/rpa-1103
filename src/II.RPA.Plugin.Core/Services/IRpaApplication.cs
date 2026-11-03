using System.Threading;

namespace II.RPA.Plugin.Core.Services
{
    public interface IRpaApplication
    {
        SynchronizationContext MainContext { get; }
    }
}
