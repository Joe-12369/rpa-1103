using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.RPASystem.Helper;
using II.RPA.Plugin.RPASystem.Params;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    /// <summary>
    /// 本机连接远程桌面
    /// </summary>
    public class LocalRemoteConnection : SystemComponentBase<ParamLocalRemoteConModel>
    {
        protected override eComponentType ComponentType => eComponentType.ReomteConnection;

        protected override JMessage Execute(ParamLocalRemoteConModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            RdpHelper.Connection(paramModel.User, paramModel.Pw, paramModel.Ip, paramModel.Port);
            bool isConnect = false;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.ElapsedMilliseconds < paramModel.WaitTime)
            { 
                Thread.Sleep(100);
                if (User32Helper.IsConnection(RdpHelper.CLASS_NAME, RdpHelper.GetTitle(paramModel.User, paramModel.Ip)))
                {
                    isConnect = true;
                    break;
                }
            }
            stopwatch.Stop();
            if (!isConnect)
            { 
                throw new RPAException(eErrorMessageType.Error_LocalRemoteConnection_PW);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamLocalRemoteConModel paramModel)
        {
            if (string.IsNullOrEmpty(paramModel.User) || string.IsNullOrEmpty(paramModel.Pw) || string.IsNullOrEmpty(paramModel.Ip))
            {
                throw new RPAException(eErrorMessageType.Error_LocalRemoteConnection_AccountOrIp);
            }
            if (!SocketHelper.TestPort(paramModel.Ip, Convert.ToInt32(paramModel.Port)))
            {
                throw new RPAException(eErrorMessageType.Error_LocalRemoteConnection_Socket);
            }
        }
    }
}
