using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Extentions
{
    public static class SemaphoreExtention
    {
        public static void SafeRelease(this Semaphore semaphore)
        {
            try
            {
                semaphore.Release();
            }
            catch (Exception ex)
            {
            }
        }
    }
}
