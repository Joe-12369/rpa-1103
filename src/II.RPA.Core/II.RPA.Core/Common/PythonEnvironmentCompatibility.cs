using System;

namespace II.RPA.Core.Common
{
    public static class PythonEnvironmentCompatibility
    {
        // Python 3.11.9 requires Windows 8.1 / Windows Server 2012 R2 or later.
        public static bool SupportsPython311()
        {
            return OperatingSystem.IsWindowsVersionAtLeast(6, 3);
        }
    }
}
