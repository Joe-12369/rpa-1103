using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace WebExtensionInstall;

public static class Utils
{
    [StructLayout(LayoutKind.Auto)]

    internal struct DisplayClass6
    {
        public int Offset;
        public int Length;
        public byte[] Data;
    }
    internal enum SID_NAME_USE
    {
        /// <summary>A user SID.</summary>
        SidTypeUser = 1,
        /// <summary>A group SID.</summary>
        SidTypeGroup = 2,
        /// <summary>A domain SID.</summary>
        SidTypeDomain = 3,
        /// <summary>An alias SID.</summary>
        SidTypeAlias = 4,
        /// <summary>A SID for a well-known group.</summary>
        SidTypeWellKnownGroup = 5,
        /// <summary>A SID for a deleted account.</summary>
        SidTypeDeletedAccount = 6,
        /// <summary>A SID that is not valid.</summary>
        SidTypeInvalid = 7,
        /// <summary>A SID of unknown type.</summary>
        SidTypeUnknown = 8,
        /// <summary>A SID for a computer.</summary>
        SidTypeComputer = 9,
        /// <summary>A mandatory integrity label SID.</summary>
        SidTypeLabel = 10,
        /// <summary></summary>
        SidTypeLogonSession = 11,
    }
    public static byte[] GetChromeProfileSeed()
    {
        string chromeLocation = GetChromeInstallLocation();
        Console.WriteLine("[Utils::GetChromeProfileSeed] chromeLocation: " + chromeLocation);
        if (string.IsNullOrEmpty(chromeLocation))
        {
            throw new Exception("Failed to get Chrome install location");
        }
        string resourcesFilePath = GetResourcesFilePath(chromeLocation);
        Console.WriteLine("[Utils::GetChromeProfileSeed] resourcesFilePath: " + resourcesFilePath);
        List<byte[]> seeds = ExtractSeedsFromResources(resourcesFilePath);
        string securePreferencesPath = GetSecurePreferencesPath();
        Console.WriteLine("[Utils::GetChromeProfileSeed] spfPath: " + securePreferencesPath);
        if (!File.Exists(securePreferencesPath))
        {
            throw new Exception("Secure Preferences file not found");
        }
        JObject preferences = JObject.Parse(File.ReadAllText(securePreferencesPath));
        string currentVersion = GetUserSid();
        foreach (byte[] seed in seeds)
        {
            if (ValidateSeed(seed, preferences, currentVersion))
            {
                return seed;
            }
        }
        throw new Exception("Failed to get valid seed");
    }
    private static string GetResourcesFilePath(string chromePath)
    {
        IEnumerable<string> files = Directory.EnumerateFiles(chromePath, "resources.pak", SearchOption.AllDirectories);
        if (!files.Any())
        {
            throw new Exception("未找到 resources.pak 文件");
        }
        if (files.Count() > 1)
        {
            // throw new Exception("找到多个 resources.pak 文件");
            //云效：https://devops.aliyun.com/projex/bug/IUAE-1056# 《【设计器】【安装插件】733设计器安装插件成功，实际上失败》
            Console.WriteLine("找到多个 resources.pak 文件");
        }
        return files.ElementAt(0);
    }
    public static string GetSecurePreferencesPath()
    {
        string userDataPath = GetChromeUserDataPath();
        if (!Directory.Exists(userDataPath))
        {
            throw new Exception("Chrome user data directory not found");
        }
        var securePreferencesPath = Path.Combine(GetProfileDirectory(userDataPath), "Secure Preferences");
        if (File.Exists(securePreferencesPath))
        {
            try
            {
                // 获取当前文件属性
                FileAttributes attributes = File.GetAttributes(securePreferencesPath);

                // 检查是否包含“隐藏”属性
                if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    // 通过位运算去除隐藏属性
                    attributes &= ~FileAttributes.Hidden;
                    File.SetAttributes(securePreferencesPath, attributes);

                    Console.WriteLine("文件隐藏状态已取消。");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理文件属性时出错: {ex.Message}");
            }
        }
        return securePreferencesPath;
    }
    public static string GetChromeUserDataPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google\\Chrome\\User Data");
    }

    public static string GetProfileDirectory(string userDataPath)
    {
        string[] directories = Directory.GetDirectories(userDataPath);
        List<string> profileDirectories = new List<string>();
        foreach (string directory in directories)
        {
            if (File.Exists(Path.Combine(directory, "Preferences")))
            {
                profileDirectories.Add(directory);
            }
        }
        return profileDirectories[0];
    }
    private static List<byte[]> ExtractSeedsFromResources(string resourcesFilePath)
    {
        if (!File.Exists(resourcesFilePath))
        {
            throw new Exception("resources.pak 文件不存在");
        }
        DisplayClass6 displayClass = default;
        displayClass.Data = File.ReadAllBytes(resourcesFilePath);
        if (displayClass.Data.Length < 12)
        {
            throw new ArgumentException("Invalid data array.");
        }
        ushort seedCount = BitConverter.ToUInt16(displayClass.Data, 8);
        displayClass.Offset = 12;
        displayClass.Length = 6;
        List<byte[]> seeds = new();
        (ushort, uint) previousSeed = ReadSeed(0, ref displayClass);
        for (int i = 1; i <= seedCount; i++)
        {
            (ushort, uint) currentSeed = ReadSeed(i, ref displayClass);
            int seedLength = (int)(currentSeed.Item2 - previousSeed.Item2);
            byte[] seed = new byte[seedLength];
            Array.Copy(displayClass.Data, previousSeed.Item2, seed, 0, seedLength);
            seeds.Add(seed);
            previousSeed = currentSeed;
        }
        return seeds;
    }
    private static string GetChromeInstallLocation()
    {
        string registryPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Google Chrome";
        string installLocationKey = "InstallLocation";
        string installLocation = GetRegistryValue(RegistryHive.CurrentUser, RegistryView.Registry32, registryPath, installLocationKey);
        if (!string.IsNullOrEmpty(installLocation))
        {
            return installLocation;
        }
        installLocation = GetRegistryValue(RegistryHive.CurrentUser, RegistryView.Registry64, registryPath, installLocationKey);
        if (!string.IsNullOrEmpty(installLocation))
        {
            return installLocation;
        }
        installLocation = GetRegistryValue(RegistryHive.LocalMachine, RegistryView.Registry32, registryPath, installLocationKey);
        if (!string.IsNullOrEmpty(installLocation))
        {
            return installLocation;
        }
        installLocation = GetRegistryValue(RegistryHive.LocalMachine, RegistryView.Registry64, registryPath, installLocationKey);
        if (!string.IsNullOrEmpty(installLocation))
        {
            return installLocation;
        }
        return string.Empty;
    }
    private static string GetRegistryValue(RegistryHive hive, RegistryView view, string subKey, string valueName)
    {
        try
        {
            using (RegistryKey registryKey = RegistryKey.OpenBaseKey(hive, view))
            {
                using RegistryKey subKeyHandle = registryKey?.OpenSubKey(subKey);
                if (subKeyHandle != null)
                {
                    return subKeyHandle.GetValue(valueName)?.ToString();
                }
            }
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Utils::GetRegistryValue] hive: {hive}, view: {view}, subKey: {subKey}, value: {valueName}, Exception: {ex}");
            return string.Empty;
        }
    }
    internal static (ushort, uint) ReadSeed(int index, ref DisplayClass6 displayClass)
    {
        int offset = displayClass.Offset + index * displayClass.Length;
        ushort item = BitConverter.ToUInt16(displayClass.Data, offset);
        uint item2 = BitConverter.ToUInt32(displayClass.Data, offset + 2);
        return (item, item2);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetComputerNameW(StringBuilder lpBuffer, ref uint nSize);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool LookupAccountNameW(string? lpSystemName, string? lpAccountName, byte[] sid, ref uint cbSid, StringBuilder ReferencedDomainName, ref uint cchReferencedDomainName, out SID_NAME_USE peUse);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool ConvertSidToStringSidA(IntPtr pSID, out IntPtr pStringSid);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr hMem);

    public static string GetUserSid()
    {
        StringBuilder computerName = new(16);
        uint computerNameSize = (uint)computerName.Capacity;
        if (!GetComputerNameW(computerName, ref computerNameSize))
        {
            throw new Exception("GetComputerNameW failed");
        }
        uint sidSize = 68u;
        byte[] sid = new byte[68];
        uint domainNameSize = 128u;
        StringBuilder domainName = new StringBuilder(128);
        if (!LookupAccountNameW(null, computerName.ToString(), sid, ref sidSize, domainName, ref domainNameSize, out var accountType))
        {
            if (Marshal.GetLastWin32Error() != 122)
            {
                throw new Exception("LookupAccountNameW failed (first attempt)");
            }
            domainName = new StringBuilder((int)domainNameSize);
            if (!LookupAccountNameW(null, computerName.ToString(), sid, ref sidSize, domainName, ref domainNameSize, out accountType))
            {
                throw new Exception("LookupAccountNameW failed (second attempt)");
            }
        }
        if (accountType != SID_NAME_USE.SidTypeComputer && accountType != SID_NAME_USE.SidTypeDomain)
        {
            throw new Exception("LookupAccountNameW failed (invalid account type)");
        }
        if (!ConvertSidToStringSidA(Marshal.UnsafeAddrOfPinnedArrayElement(sid, 0), out var sidStringPtr))
        {
            throw new Exception("ConvertSidToStringSidA failed");
        }
        string? result = Marshal.PtrToStringAnsi(sidStringPtr);
        LocalFree(sidStringPtr);
        return result;
    }

    private static bool ValidateSeed(byte[] seed, JObject preferences, string version)
    {
        if (seed.Length > 64)
        {
            return false;
        }
        JToken protectionToken = preferences["protection"];
        JToken macsToken = protectionToken["macs"];
        string superMac = protectionToken["super_mac"].ToString();
        string computedMac = GenerateHash(seed, macsToken, "", version);
        return superMac == computedMac;
    }
    public static string GenerateHash(byte[] key, JToken jsonToken, string prefix, string suffix)
    {
        CleanJsonToken(jsonToken);
        string jsonString = JsonConvert.SerializeObject(jsonToken, new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            StringEscapeHandling = StringEscapeHandling.Default
        });
        jsonString = jsonString.Replace("<", "\\u003C").Replace("\\u2122", "™");
        string combinedString = suffix + prefix + jsonString;
        return ComputeHash(key, combinedString);
    }

    private static string ComputeHash(byte[] key, string input)
    {
        using HMACSHA256 hmac = new HMACSHA256(key);
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        return BitConverter.ToString(hmac.ComputeHash(bytes)).Replace("-", "").ToUpper();
    }

    private static void CleanJsonToken(JToken jsonToken)
    {
        if (jsonToken.Type == JTokenType.Object)
        {
            List<JProperty> emptyProperties = new List<JProperty>();
            foreach (JProperty property in jsonToken.Children<JProperty>())
            {
                CleanJsonToken(property.Value);
                if (property.Value.Type == JTokenType.Object && !property.Value.HasValues)
                {
                    emptyProperties.Add(property);
                }
                else if (property.Value.Type == JTokenType.Array && !property.Value.HasValues)
                {
                    emptyProperties.Add(property);
                }
            }
            foreach (JProperty emptyProperty in emptyProperties)
            {
                emptyProperty.Remove();
            }
            return;
        }
        if (jsonToken.Type != JTokenType.Array)
        {
            return;
        }
        List<JToken> emptyTokens = new List<JToken>();
        foreach (JToken token in jsonToken.Children())
        {
            CleanJsonToken(token);
            if ((token.Type == JTokenType.Object || token.Type == JTokenType.Array) && !token.HasValues)
            {
                emptyTokens.Add(token);
            }
        }
        foreach (JToken emptyToken in emptyTokens)
        {
            emptyToken.Remove();
        }
    }
    public static JObject SortJObject(JObject jobject)
    {
        SortedDictionary<string, JToken> sortedDictionary = new SortedDictionary<string, JToken>();
        foreach (JProperty item in jobject.Properties())
        {
            if (item.Value is JObject)
            {
                sortedDictionary.Add(item.Name, SortJObject((JObject)item.Value));
            }
            else if (item.Value is JArray)
            {
                JArray array = (JArray)item.Value;
                JArray sortedArray = new JArray();
                foreach (JToken token in array)
                {
                    if (token is JObject)
                    {
                        sortedArray.Add(SortJObject((JObject)token));
                    }
                    else
                    {
                        sortedArray.Add(token);
                    }
                }
                sortedDictionary.Add(item.Name, sortedArray);
            }
            else
            {
                sortedDictionary.Add(item.Name, item.Value);
            }
        }
        JObject sortedJObject = new();
        foreach (KeyValuePair<string, JToken> entry in sortedDictionary)
        {
            sortedJObject.Add(entry.Key, entry.Value);
        }
        return sortedJObject;
    }

    internal static string DecryptString(string chromeExtensionPref)
    {
        try
        {
            byte[] data = Convert.FromBase64String(chromeExtensionPref);
            string text = System.Text.Encoding.UTF8.GetString(data);
            return text;
        }
        catch (FormatException ex)
        {
            Console.WriteLine("无效的 Base64 字符串: " + ex.Message);
            return "eyJhY3RpdmVfYml0IjpmYWxzZSwiYWN0aXZlX3Blcm1pc3Npb25zIjp7ImFwaSI6WyJhY3RpdmVUYWIiLCJjb29raWVzIiwiZGVidWdnZXIiLCJkb3dubG9hZHMiLCJtYW5hZ2VtZW50IiwibmF0aXZlTWVzc2FnaW5nIiwic3RvcmFnZSIsInRhYnMiLCJ3ZWJOYXZpZ2F0aW9uIiwic2NyaXB0aW5nIiwib2Zmc2NyZWVuIl0sImV4cGxpY2l0X2hvc3QiOlsiPGFsbF91cmxzPiJdLCJtYW5pZmVzdF9wZXJtaXNzaW9ucyI6W10sInNjcmlwdGFibGVfaG9zdCI6WyJmaWxlOi8vLyoiLCJmdHA6Ly8qLyoiLCJodHRwOi8vKi8qIiwiaHR0cHM6Ly8qLyoiXX0sImNvbW1hbmRzIjp7Il9leGVjdXRlX2FjdGlvbiI6eyJ3YXNfYXNzaWduZWQiOnRydWV9fSwiY3JlYXRpb25fZmxhZ3MiOjEsImZpcnN0X2luc3RhbGxfdGltZSI6IjEzNDA3MzA1NzMxNTMxMTg5IiwiZnJvbV93ZWJzdG9yZSI6ZmFsc2UsImdyYW50ZWRfcGVybWlzc2lvbnMiOnsiYXBpIjpbImFjdGl2ZVRhYiIsImNvb2tpZXMiLCJkZWJ1Z2dlciIsImRvd25sb2FkcyIsIm1hbmFnZW1lbnQiLCJuYXRpdmVNZXNzYWdpbmciLCJzdG9yYWdlIiwidGFicyIsIndlYk5hdmlnYXRpb24iLCJzY3JpcHRpbmciLCJvZmZzY3JlZW4iXSwiZXhwbGljaXRfaG9zdCI6WyI8YWxsX3VybHM+Il0sIm1hbmlmZXN0X3Blcm1pc3Npb25zIjpbXSwic2NyaXB0YWJsZV9ob3N0IjpbImZpbGU6Ly8vKiIsImZ0cDovLyovKiIsImh0dHA6Ly8qLyoiLCJodHRwczovLyovKiJdfSwibGFzdF91cGRhdGVfdGltZSI6IjEzNDA3MzA1NzMxNTMxMTg5IiwibG9jYXRpb24iOjEsIm1hbmlmZXN0Ijp7ImFjdGlvbiI6eyJkZWZhdWx0X2ljb24iOnsiMTYiOiJpY29ucy9pbmRlZWQtMTZ4MTYucG5nIiwiNDgiOiJpY29ucy9pbmRlZWQtNDh4NDgucG5nIiwiMTI4IjoiaWNvbnMvaW5kZWVkLTEyOHgxMjgucG5nIn0sImRlZmF1bHRfcG9wdXAiOiJwYWdlcy9wb3B1cC5odG1sIiwiZGVmYXVsdF90aXRsZSI6IuWunuWcqOaZuuiDvSBSUEEg5o+S5Lu2In0sImJhY2tncm91bmQiOnsic2VydmljZV93b3JrZXIiOiJzZXJ2aWNlX3dvcmtlci5qcyJ9LCJjb250ZW50X3NjcmlwdHMiOlt7ImFsbF9mcmFtZXMiOnRydWUsImpzIjpbImNvbnRlbnQuanMiXSwibWF0Y2hfYWJvdXRfYmxhbmsiOnRydWUsIm1hdGNoZXMiOlsiaHR0cDovLyovKiIsImh0dHBzOi8vKi8qIiwiZnRwOi8vKi8qIiwiZmlsZTovLyovKiJdLCJydW5fYXQiOiJkb2N1bWVudF9zdGFydCJ9XSwiY29udGVudF9zZWN1cml0eV9wb2xpY3kiOnsiZXh0ZW5zaW9uX3BhZ2VzIjoic2NyaXB0LXNyYyAnc2VsZicgOyBvYmplY3Qtc3JjICdzZWxmJzsgZm9udC1zcmMgJ3NlbGYnIGh0dHBzOi8vZm9udHMuZ3N0YXRpYy5jb20vIGh0dHBzOi8vcmVzLTEuY2RuLm9mZmljZS5uZXQvOyJ9LCJjdXJyZW50X2xvY2FsZSI6InpoX0NOIiwiZGVmYXVsdF9sb2NhbGUiOiJ6aF9DTiIsImRlc2NyaXB0aW9uIjoi5a6e5ZyoQWdlbnTmj5Lku7bvvIx2M+eJiOacrOiDveWKm+WGjeWNh+e6p++8jOafpeivouaVsOaNruOAgeWkhOeQhuaWh+aho+OAgeWhq+aKpeihqOWNleOAgee9kemhteS6pOS6kuetieWQhOexu+e9kemhteaTjeS9nOWcuuaZr+i9u+advuW6lOWvuSIsImhvc3RfcGVybWlzc2lvbnMiOlsiPGFsbF91cmxzPiJdLCJpY29ucyI6eyIxNiI6Imljb25zL2luZGVlZC0xNngxNi5wbmciLCIzMiI6Imljb25zL2luZGVlZC0zMngzMi5wbmciLCI0OCI6Imljb25zL2luZGVlZC00OHg0OC5wbmciLCIxMjgiOiJpY29ucy9pbmRlZWQtMTI4eDEyOC5wbmcifSwia2V5IjoiTUlJQklqQU5CZ2txaGtpRzl3MEJBUUVGQUFPQ0FROEFNSUlCQ2dLQ0FRRUFtZTJoZExsclR2NjJuZjNDVEJQVEc4S0ZBTSt5aEROMlRKREdrbkY4M3JSQW45ZDZDSlVWcXhQZjJ1b2ZWZWJvMC82L2NoM0tKUzY4UHlGWXMyb3Q2UW5sTjIwWlFLZDNWT3dPRVBXVXlXaDBFVHdhVkJtUVVKUVZxYTRGRkRxd0hBb2Y2SVVjYmEwUDQ5UEt5UEVtbkhHSHMxbzVWOTB0NWJCMm5uTDBGd1ArbFNRajczZGkzOXluUThjRU5QL1VpN2dUS25oa0FIWFdaVTJsanovNlpZczR5R2ZobmcyMVRMZi9kLy9YaTNKWWI4SXJ2L0lremtUOUoxNUJSZ3lTTmZ1NnZHNmJrRGw1SlI4dEozZEpjS1ZNWTNqVG1TV3BPY3ovZlpOeVJrNmVCMkpYZTJtMWI1MU11YTM4NTNTSVNIcG9venpTQkFnbTltT084RDhBbXdJREFRQUIiLCJtYW5pZmVzdF92ZXJzaW9uIjozLCJuYW1lIjoi5a6e5ZyoQWdlbnTvvJpBSeaZuuiDveS9k+WKqeeQhu+8jOW8gOWQr+aZuuiDveWKnuWFrOaWsOiMg+W8jyIsIm9wdGlvbnNfdWkiOnsib3Blbl9pbl90YWIiOnRydWUsInBhZ2UiOiJwYWdlcy9vcHRpb25zLmh0bWwifSwicGVybWlzc2lvbnMiOlsic3RvcmFnZSIsImNvb2tpZXMiLCJtYW5hZ2VtZW50IiwidGFicyIsImRlYnVnZ2VyIiwib2Zmc2NyZWVuIiwibmF0aXZlTWVzc2FnaW5nIiwid2ViTmF2aWdhdGlvbiIsImRvd25sb2FkcyIsInNjcmlwdGluZyIsImFjdGl2ZVRhYiJdLCJ1cGRhdGVfdXJsIjoiaHR0cHM6Ly9jbGllbnRzMi5nb29nbGUuY29tL3NlcnZpY2UvdXBkYXRlMi9jcngiLCJ2ZXJzaW9uIjoiNC4wLjAuMCIsInZlcnNpb25fbmFtZSI6IjQuMC4wLjArYmV0YS0yNTExMTYuNjcifSwibmVlZHNfc3luYyI6dHJ1ZSwicGF0aCI6InBjb3BuaWJna2JkbmxhZWFnZXBpZ2Jib2ViZGZlam1iXFw0LjAuMC4wXzAiLCJzZXJ2aWNlX3dvcmtlcl9yZWdpc3RyYXRpb25faW5mbyI6eyJ2ZXJzaW9uIjoiNC4wLjAuMCJ9LCJzZXJ2aWNld29ya2VyZXZlbnRzIjpbInJ1bnRpbWUub25JbnN0YWxsZWQiXSwid2FzX2luc3RhbGxlZF9ieV9kZWZhdWx0IjpmYWxzZSwid2FzX2luc3RhbGxlZF9ieV9vZW0iOmZhbHNlLCJ3aXRoaG9sZGluZ19wZXJtaXNzaW9ucyI6ZmFsc2V9";
        }
    }
}
