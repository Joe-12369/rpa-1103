using SHDocVw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace II.RPA.Plugin.IE.V2.Automation.Provider
{
    internal class BrowserProvider
    {
        public List<InternetExplorer> GetAllInternetExplorers()
        {
            var regeditKey = Marshal.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39"));
            var shellWindows = Activator.CreateInstance(regeditKey) as ShellWindows;
            if (shellWindows != null)
            {
                return shellWindows.Cast<InternetExplorer>().ToArray<InternetExplorer>().Where(delegate (InternetExplorer x)
                {
                    bool result;
                    try
                    {
                        result = (x.Type != null);
                    }
                    catch (Exception)
                    {
                        result = false;
                    }
                    return result;
                }).ToList<InternetExplorer>();
            }
            else
                return null;
        }
    }
}
