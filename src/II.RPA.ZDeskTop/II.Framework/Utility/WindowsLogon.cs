using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace II.Framework.Utility
{
    public static class WindowsLogon
    {
        private static UserInfo? _cacheInfo = default;
        private static object _lock = new object(); 
        public static readonly string LogonFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Core" ,WindowsIdentity.GetCurrent().User.AccountDomainSid.Value) ;
        public static bool WindowsIdentityVerify(string aUserName, string aPassword, string aMachineName)
        {
            IntPtr tokenHandle = new IntPtr(0);
            if (string.IsNullOrWhiteSpace(aPassword))
            {
                aPassword = string.Empty;
            }

            try
            {
                string UserName = null;
                string MachineName = null;
                string Pwd = null;

                MachineName = aMachineName;
                UserName = aUserName;
                Pwd = aPassword;

                //_logger.LogInfo($"WindowsIdentityVerify:{MachineName};{UserName};{Pwd}");

                const int LOGON32_PROVIDER_DEFAULT = 0;
                const int LOGON32_LOGON_INTERACTIVE = 2;
                tokenHandle = IntPtr.Zero;

                bool returnValue = LogonUser(UserName, MachineName, Pwd, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out tokenHandle);

                if (returnValue == false)
                {
                    int ret = Marshal.GetLastWin32Error();
                    if (ret == 1327)
                    {
                        //_logger.LogError($"LogonUser:empty password!");
                        return true;
                    }
                    else
                    {
                        string errmsg = GetErrorMessage(ret);
                        //_logger.LogError($"LogonUser:{errmsg}:{ret}");
                        return false;
                    }
                }
                else
                {

                    WindowsIdentity newId = new WindowsIdentity(tokenHandle);
                    WindowsPrincipal userperm = new WindowsPrincipal(newId);
                    if (userperm.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        //"Access Granted. User is admin"
                    }
                    else
                    {
                        //"Access Granted. User is not admin"
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                //
                return false;
            }
            finally
            {
                if (tokenHandle != IntPtr.Zero)
                {
                    CloseHandle(tokenHandle);
                }
            }

        }
        private static string GetErrorMessage(int errorCode)
        {
            int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x100;
            int FORMAT_MESSAGE_IGNORE_INSERTS = 0x200;
            int FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;

            int msgSize = 255;
            string lpMsgBuf = null;
            int dwFlags = FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS;

            IntPtr lpSource = IntPtr.Zero;
            IntPtr lpArguments = IntPtr.Zero;
            int returnVal = FormatMessage(dwFlags, ref lpSource, errorCode, 0, ref lpMsgBuf, msgSize, ref lpArguments);

            if (returnVal == 0)
            {
                throw new Exception("Failed to format message for error code " + errorCode.ToString() + ". ");
            }
            return lpMsgBuf;

        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LogonUser(string lpszUsername,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            out IntPtr phToken
            );
        [DllImport("kernel32.dll")]
        private static extern int FormatMessage(int dwFlags, ref IntPtr lpSource, int dwMessageId, int dwLanguageId, ref String lpBuffer, int nSize, ref IntPtr Arguments);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        public static void GetCurrentUserAndMachineName(out string userName, out string machineName)
        {
            WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
            if (windowsIdentity.Name.Contains("\\"))
            {
                var names = windowsIdentity.Name.Split('\\');
                userName = names.Last();
                machineName = names.First();
            }
            else
            {
                userName = Environment.UserName;
                machineName = Environment.MachineName;
            }
        }
        public static bool CheckCurrentCache()
        {
            var userInfo = GetCurrentCacheUserInfo();
            if (userInfo.HasValue)
            {
                GetCurrentUserAndMachineName(out string uName, out string mName);
                if (WindowsIdentityVerify(userInfo.Value.UserName, userInfo.Value.Password, mName))
                {
                    return true;
                }
            }
            return false;
        }
        public static UserInfo? GetCurrentCacheUserInfo()
        {
            lock (_lock)
            {
                if (_cacheInfo.HasValue)
                {
                    return _cacheInfo.Value;
                }
                if (File.Exists(LogonFileName))
                {
                    try
                    {
                        var securityString = File.ReadAllText(WindowsLogon.LogonFileName);
                        var logonInfo = SercurityUtils.Decrypt(securityString);
                        UserInfo userinfo = JsonConvert.DeserializeObject<UserInfo>(logonInfo);
                        _cacheInfo = userinfo;
                        return userinfo;
                    }
                    catch
                    {
                        return default;
                    }
                }
                return default;
            }
        }
        public static void SetCurrentCacheUserInfo(UserInfo userinfo)
        {
            lock (_lock)
            {
                try
                {
                    var jsonString = JsonConvert.SerializeObject(userinfo);
                    var securityString = SercurityUtils.Encrypt(jsonString);

                    byte[] myByte = System.Text.Encoding.UTF8.GetBytes(securityString);
                    using (FileStream fsWrite = new FileStream(LogonFileName, FileMode.OpenOrCreate))
                    {
                        fsWrite.Write(myByte, 0, myByte.Length);
                    };
                    _cacheInfo = userinfo;
                }
                catch
                {

                }
            }
        }
    }
    public struct UserInfo
    {
        public string UserName
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }

        public override bool Equals(object obj)
        {
            if (obj is UserInfo other)
            {
                return this.UserName.Equals(other.UserName) && this.Password.Equals(other.Password);
            }
            return false;
        }
    }
}
