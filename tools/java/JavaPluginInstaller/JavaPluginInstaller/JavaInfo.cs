using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaPluginInstaller
{
    public class JavaInfo
    {
        public string Version { private get; set; }
        public bool IsJvm64 { private get; set; }
        public string BinPath { private get; set; }
        public bool HighVersion { get; set; }

        public string GetVersion()
        {
            if (HighVersion)
            {
                return "2.0.2";
            }
            if (Version.StartsWith("1.5"))
            {
                return "2.0.1";
            }
            if (Version.StartsWith("1.6"))
            {
                return "2.0.2";
            }
            if (Version.StartsWith("1.7.0_0"))
            {
                if (int.TryParse(Version.Substring(7), out int var))
                {
                    if (var < 6)
                    { 
                        return "2.0.2";
                    }
                }
            }
            return Version;
        }

        public string GetBinPath()
        {
            return BinPath;
        }
        public List<string> GetBinFile()
        {
            List<string> list = new List<string>();
            if (Version.StartsWith("1.5"))
            {
                list.Add("JAWTAccessBridge.dll");
                list.Add("JavaAccessBridge.dll");
                return list;
            }
            if (IsJvm64)
            {
                list.Add("JAWTAccessBridge-64.dll");
                list.Add("JavaAccessBridge-64.dll");
            }
            else
            {
                list.Add("JAWTAccessBridge-32.dll");
                list.Add("JavaAccessBridge-32.dll");
            }
            return list;
        }

        public string GetExtPath()
        { 
            return Path.GetFullPath(BinPath + "\\..\\lib\\ext");
        }
        public List<string> GetExtFile()
        {
            List<string> list = new List<string>();
            if (Version.StartsWith("1.5"))
            {
                list.Add("access-bridge.jar");
                list.Add("jaccess-1_2.jar");
                list.Add("jaccess-1_3.jar");
                list.Add("jaccess-1_4.jar");
                return list;
            }
            if (IsJvm64)
            {
                list.Add("access-bridge-64.jar");
            }
            else
            {
                list.Add("access-bridge-32.jar");
            }
            list.Add("jaccess.jar");
            return list;
        }

        public string GetLibPath()
        {
            return Path.GetFullPath(BinPath + "\\..\\lib");
        }

        public string GetLibFile()
        {
            return "accessibility.properties";
        }
    }
}
