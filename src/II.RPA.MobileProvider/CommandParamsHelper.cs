using II.RPA.Core.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider
{
    internal class CommandParamsHelper
    {
        static CommandParamsHelper()
        {
            var client = CommandArgHelper.GetArgumentValue("-c");
            ClientType = ConvertToClientType(client);

            ///必需参数 -p 父进程Id
            var pid = CommandArgHelper.GetArgumentValue("-p");
            Pid = ConvertToPid(pid);

            ///必须参数 -n 管道名
            var pipeName = CommandArgHelper.GetArgumentValue("-n");
            PipeName = pipeName;


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


}
