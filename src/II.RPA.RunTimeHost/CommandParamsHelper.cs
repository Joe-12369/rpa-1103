using II.RPA.Core.Common;
using II.RPA.Plugin.Core;
using System;
using System.Diagnostics;

namespace II.RPA.RuntimeHost
{
    /// <summary>
    /// CommandParamsHelper
    /// </summary>
    public static class CommandParamsHelper
    {
        static CommandParamsHelper()
        {
            ///必需参数 -c 客户端类型
            var client = CommandArgHelper.GetArgumentValue("-c");
            ClientType = ConvertToClientType(client);
            GlobleConfigurations.ClientType = ClientType;

            ///必需参数 -p 父进程Id
            var pid = CommandArgHelper.GetArgumentValue("-p");
            Pid = ConvertToPid(pid);

            ///必须参数 -n 管道名
            var pipeName = CommandArgHelper.GetArgumentValue("-n");
            PipeName = pipeName;

            var port = CommandArgHelper.GetArgumentValue("-port");
            ServerPort = int.Parse(port);

            try
            {
                /// 可选参数 -r 是否为超时重启进程
                var isrestart = CommandArgHelper.GetArgumentValue("-r");
                IsRestart = ConvertToRestart(isrestart);
            }
            catch
            {
                IsRestart = false;
                //可选参数忽略异常
            }

            CurrentProcessId = Process.GetCurrentProcess().Id;

            #region internal methods for type converting
            ClientType ConvertToClientType(string clientString)
            {
                if (int.TryParse(clientString, out int clientid))
                {
                    if (clientid == 1)
                    {
                        return ClientType.Factory;
                    }
                    else if (clientid == 2)
                    {
                        return ClientType.Bot;
                    }
                    else if (clientid == 3)
                    {
                        return ClientType.DeskTop;
                    }
                }
                return ClientType.Admin;
            }

            int ConvertToPid(string pidString)
            {
                if (int.TryParse(pidString, out int pidValue))
                {
                    return pidValue;
                }
                else
                {
                    throw new ArgumentException($"{pidString} in not valid!");
                }

            }

            bool ConvertToRestart(string isRestartString)
            {
                if (bool.TryParse(isRestartString, out bool isRestartValue))
                {
                    return isRestartValue;
                }
                else
                {
                    throw new ArgumentException($"{isRestartString} in not valid!");
                }

            }
            #endregion


        }
        public static ClientType ClientType
        {
            get;
            private set;
        }

        public static int Pid
        {
            get;
            private set;
        }

        public static string PipeName
        {
            get;
            private set;
        }
        public static int ServerPort
        {
            get;
            private set;
        }
        public static bool IsRestart
        {
            get;
            private set;
        }
        public static int CurrentProcessId
        {
            get;
            private set;
        }
    }
}
