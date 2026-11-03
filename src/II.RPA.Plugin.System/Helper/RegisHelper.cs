using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.RPASystem.Helper
{
    public class RegisHelper
    {
        private const string guid = "{FBE4865B-509A-D075-F5D4-D2BA0587049F}";
        private const string CredentialProviderName = "ZCredentialProvider.dll";
        private const string CredentialProviderKey = "ZCredentialProvider";
        private static readonly string CredentialRegPath = $"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Authentication\\Credential Providers\\{guid}";

        public static bool CheckPlugState()
        {
            return  CheckCredentialRegPath() && CheckCLSID() && CheckInprocServer() && CheckCADPath();
        }

        private static bool CheckDll()
        {
            string system32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string filePath = Path.Combine(system32Path, CredentialProviderName);
            return File.Exists(filePath);
        }
        private static bool CheckCredentialRegPath()
        {
            RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            RegistryKey registryKey3 = registryKey.OpenSubKey(CredentialRegPath);
            if (registryKey3 == null)
            {
                return false;
            }
            string result = registryKey3.GetValue("").ToString();
            return result == CredentialProviderKey;
        }
        private static bool CheckCLSID()
        {
            RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);
            RegistryKey registryKey3 = registryKey.OpenSubKey($"CLSID\\{guid}");
            if (registryKey3 == null)
            {
                return false;
            }
            string result = registryKey3.GetValue("").ToString();
            return result == CredentialProviderKey;
        }
        private static bool CheckInprocServer()
        {
            RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);
            RegistryKey registryKey3 = registryKey.OpenSubKey($"CLSID\\{guid}\\InprocServer32");
            if (registryKey3 == null)
            {
                return false;
            }
            string result1 = registryKey3.GetValue("ThreadingModel").ToString();
            string result2 = registryKey3.GetValue("").ToString();
            return result1 == "Apartment" && result2 == CredentialProviderName;
        }

        private static bool CheckCADPath()
        {
            RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            if (registryKey == null)
            {
                return false;
            }
            string name = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
            RegistryKey registryKey2 = registryKey.OpenSubKey(name);
            if (registryKey2 == null)
            {
                return false;
            }
            string result = registryKey2.GetValue("DisableCAD").ToString();
            return result == "1";

        }
    }
}
