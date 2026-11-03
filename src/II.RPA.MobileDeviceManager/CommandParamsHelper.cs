using II.RPA.Core.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager
{
    public class CommandParamsHelper
    {
        static CommandParamsHelper()
        {
            ///必需参数 -c 客户端类型
            //var client = CommandArgHelper.GetArgumentValue("-c");
            // ClientType = ConvertToClientType(client);
            ///必需参数 -c 客户端类型
            var client = CommandArgHelper.GetArgumentValue("-c");
            ClientType = ConvertToClientType(client);

            ///必需参数 -p 父进程Id
            var pid = CommandArgHelper.GetArgumentValue("-p");
            Pid = ConvertToPid(pid);

            ///必须参数 -n 管道名
            var pipeName = CommandArgHelper.GetArgumentValue("-n");
            PipeName = pipeName;

            ///必须参数 -actionType 操作类型  前期主要区分是否是安装插件
            var actionType = CommandArgHelper.GetArgumentValue("-action");
            ActionType = ConvertToPid(actionType);

            ///必须参数 -actionType 操作类型  前期主要区分是否是安装插件
            var serverPort = CommandArgHelper.GetArgumentValue("-s");
            ServerPort = serverPort;

            try
            {
                /// 可选参数 -r 是否为超时重启进程
                var isrestart = CommandArgHelper.GetArgumentValue("-r");
                IsRestart = ConvertToRestart(isrestart);
            }
            catch
            {
                IsRestart = false;
            }
            try
            {
                /// -para 入参  打开应用时需要带入历史设备信息
                var inParameter = CommandArgHelper.GetArgumentValue("-para");
                InParameter = inParameter;
            }
            catch
            {
                InParameter = "";
            }

            ProcessId = Process.GetCurrentProcess().Id;

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

        public static bool IsRestart
        {
            get;
            private set;
        }
        public static int ProcessId
        {
            get;
            private set;
        }
        public static int ActionType
        {
            get;
            private set;
        }
        public static string InParameter
        {
            get;
            private set;
        }
        public static string ServerPort
        {
            get;
            private set;
        }
       
    }
}
public enum ClientType
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