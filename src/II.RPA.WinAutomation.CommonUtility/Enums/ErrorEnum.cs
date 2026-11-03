using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility
{
    public enum ErrorSource
    {
        /// <summary>
        /// 后端
        /// </summary>
        Backend = 01
    }

    public enum ErrorApplications
    {
        /// <summary>
        /// CoreServer 
        /// </summary>
        CoreServer = 02,
        //Z_Brain = 03,
        /// <summary>
        /// dll
        /// </summary>
        Dll = 04
    }

    public enum ErrorClassify
    {
        /// <summary>
        /// 使用异常
        /// </summary>
        Use = 001,
        /// <summary>
        /// 运行异常
        /// </summary>
        Run = 002,
        /// <summary>
        /// 软件异常
        /// </summary>
        Software = 003,
        /// <summary>
        /// 网络异常
        /// </summary>
        Network = 004,
        Login = 005,
        /// <summary>
        /// 流程包异常
        /// </summary>
        Package = 006,
        Communication = 007,
        Bot = 008,
        Syntax = 009
    }
}
