using II.RPA.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace II.RPA.Core.Common
{
    /// <summary>
    /// 全局公共类
    /// 都是静态成员
    /// GC无法回收、增加代码耦合度
    /// 慎用
    /// </summary>
    public static class Global
    {
        public const string MY_NAME = "CoreServer";

        static Global()
        {
            using var process = Process.GetCurrentProcess();
            CurrentPId = process.Id;
            SessionId = process.SessionId;
            CVPort = Client == ClientID.Bot ? 6441 : Client == ClientID.DeskTop ? 6442 : Client == ClientID.Admin ? 9940 : 6440;
        }

        public static int CurrentPId
        {
            get;
        }
        public static int SessionId
        {
            get;
        }
        private static ClientID m_ClientID = default;

        public static bool ForceRestartHost { get; set; } = false;

        public static ClientID Client
        {
            get
            {
                if (m_ClientID == default || m_ClientID == ClientID.INVALID)
                {
                    if (TryGetClientID(ClientIDString, out ClientID aClientID))
                    {
                        m_ClientID = aClientID;
                        return m_ClientID;
                    }
                }
                return m_ClientID;
            }
        }
        /// <summary>
        /// 命令行参数传入的-client
        /// 目前可能的值：Z-Factory、Z-Bot、Z-DeskTop、Z-Admin、Electron-Z-Factory、Electron-Z-Bot
        /// </summary>
        public static string ClientIDString
        {
            get
            {
                var val = CommandArgHelper.GetArgumentValue(CommonConstants.CLIENTID.KEY);
                if (string.IsNullOrEmpty(val))
                {
                    val = "Z-Admin";
                }
                return val;
            }
        }
        /// <summary>
        /// 向下游传播的ClientIDString, 目前仅支持Z-Factory、Z-Bot
        /// </summary>
        public static string ProduceClientIDString
        {
            get
            {
                if (new string[] { "Z-Factory", "Z-Bot" }.Contains(ClientIDString))
                {
                    return ClientIDString;
                }
                else
                {
                    return RootClientString;
                }
            }
        }

        public static string RootClientString
        {
            get
            {
                var val = CommandArgHelper.GetArgumentValue("-rootClient");
                if (string.IsNullOrEmpty(val))
                {
                    val = ClientIDString;
                }
                return val;
            }
        }
        public static int ServerPort { get; set; }

        public static int RootServerPort
        {
            get
            {
                if (Client != ClientID.DeskTop)
                {
                    return ServerPort;
                }
                else
                {
                    return GetRootServerPort();
                }
            }
        }
        /// <summary>
        /// 格式化后的ClientString：Z-Factory、Z-Bot、Z-DeskTop、Z-Admin
        /// 用于判断日志loggername、确定grpc端口
        /// </summary>
        private static string m_clientIDFormatString;
        public static string ClientIDFormatString
        {
            get
            {
                if (string.IsNullOrEmpty(m_clientIDFormatString))
                {
                    if (Client != ClientID.INVALID)
                    {
                        return m_clientIDFormatString;
                    }
                }
                return m_clientIDFormatString;
            }
            private set
            {
                m_clientIDFormatString = value;
            }
        }
        public static int CVPort { get; set; }

        public static string ZBrainServiceBaseUrl => $"http://127.0.0.1:{CVPort}";
        public static bool HostZBrain => CommandArgHelper.GetWithConfig("-proxy-zbrain", "ProxyZBrain") != "false" && new ClientID[] { ClientID.Factory, ClientID.Bot, ClientID.Admin }.Contains(Client);
        public static int EnginePort
        {
            get
            {
                return Client switch
                {
                    ClientID.Bot => 6445,
                    _ => 6444,
                };
            }
        }
        public static bool HostEngine => CommandArgHelper.GetWithConfig("-proxy-engine", "ProxyEngine") != "false" && new ClientID[] { ClientID.Factory, ClientID.Bot, ClientID.Admin }.Contains(Client);
        public static bool LoopADB => new ClientID[] { ClientID.Factory, ClientID.Bot, ClientID.Admin }.Contains(Client);

        public static int ClientProcessId
        {
            get
            {
                try
                {
                    string idString = File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Pid", $"{ClientIDFormatString}.txt"));
                    return int.Parse(idString);
                }
                catch
                {
                    return -1;
                }
            }
        }
        public static bool TryGetClientID(string aClientIDStr, out ClientID aClientID)
        {
            if (!string.IsNullOrEmpty(aClientIDStr))
            {
                var members = typeof(ClientID).GetFields();
                foreach (var member in members)
                {
                    if (member != null)
                    {
                        object[] obj = member.GetCustomAttributes(typeof(DescriptionAttribute), false);
                        if (obj.Length > 0)
                        {
                            DescriptionAttribute attribute = obj[0] as DescriptionAttribute;
                            if (aClientIDStr.Contains(attribute.Description))
                            {
                                aClientID = (ClientID)Enum.Parse(typeof(ClientID), member.Name);
                                ClientIDFormatString = attribute.Description;
                                return true;
                            }
                        }

                    }
                }
            }
            aClientID = ClientID.INVALID;
            return false;
        }

        /// <summary>
        /// 全局flag、监控新版流程编辑器的状态
        /// </summary>
        public static bool IsProcessCreatorRunning
        {
            get;
            set;
        } = false;

        public static bool IfCurrentLogLevelIsDebug
        {
            get;
            set;
        } = true;

        public static string UserInfoParam
        {
            get;
            set;
        }
        public static SettingsModel Settings
        {
            get;
        } = new SettingsModel();
        public static bool IsMiniModeActive
        {
            get => Settings.TryGet(SettingConstants.MiniModeStatusKey, out bool isMiniMode) && isMiniMode;
        }

        private static string _pythonEnvVersion = "3.6.8";
        public static string PythonEnvVersion
        {
            get => _pythonEnvVersion;
            set
            {
                _pythonEnvVersion = string.IsNullOrEmpty(value) ? "3.6.8" : value;
            }
        }

        public static string EngineExePath
        {
            get
            {
                if (PythonEnvVersion == "3.6.8")
                {
                    var pythonName = Environment.Is64BitProcess ? "Python36" : "Python36-32";
                    return Path.GetFullPath($@"{string.Join(Path.DirectorySeparatorChar.ToString(), Enumerable.Repeat("..", 6))}\{pythonName}\python.exe");
                }
                else
                {
                    var pythonName = Environment.Is64BitProcess ? "Python311" : "Python311-32";
                    return Path.GetFullPath($@"{string.Join(Path.DirectorySeparatorChar.ToString(), Enumerable.Repeat("..", 6))}\{pythonName}\python.exe");
                }
            }
        }
        public static string BuildUserInforParam()
        {
            JObject jo = new JObject
            {
                { "CoreServerPort", ServerPort },
                { "ZBrainServicePath", ZBrainServiceBaseUrl }
            };
            return jo.ToString();
        }
        public static string FixZbrainUriForZdesktop(string paramString)
        {
            JObject obj = JObject.Parse(paramString);
            if (obj.ContainsKey("CoreServerPort"))
            {
                obj["CoreServerPort"] = ServerPort;
            }
            else
            {
                obj.Add("CoreServerPort", ServerPort);
            }
            if (obj.ContainsKey("ZBrainServicePath"))
            {
                obj["ZBrainServicePath"] = ZBrainServiceBaseUrl;
            }
            else
            {
                obj.Add("ZBrainServicePath", ZBrainServiceBaseUrl);
            }
            return obj.ToString();
        }

        private static int GetRootServerPort()
        {
            var fileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Z-Core\\{Global.RootClientString}.txt";
            if (File.Exists(fileName))
            {
                string idString = File.ReadAllText(fileName);
                var port = int.Parse(idString);
                return port;
            }
            return RootClientString == "Z-Factory" ? 7181 : 7182;
        }

    }
    public enum ClientID
    {
        [Description("Z-Factory")]
        Factory = 0,
        [Description("Z-Bot")]
        Bot = 1,
        [Description("Z-DeskTop")]
        DeskTop = 2,
        [Description("Z-Admin")]
        Admin = 3,
        INVALID = 99,
    }
}
