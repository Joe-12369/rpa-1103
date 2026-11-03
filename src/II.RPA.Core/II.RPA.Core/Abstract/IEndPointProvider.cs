using System.Collections.Generic;

namespace II.RPA.Core.Abstract
{
    public interface IEndPointProvider
    {
        void Initial(IEnumerable<IServiceBoot> aServiceBoots);
        void Update(string aClient, string aBootName, int aPort);
        int GetAvailablePort(int aCurrentInvalidPort);
        int GetAvailablePort(int aCurrentInvalidPort, string aBootName);
    }
}
