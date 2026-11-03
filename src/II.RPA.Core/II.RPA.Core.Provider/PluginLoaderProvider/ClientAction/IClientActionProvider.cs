using II.RPA.Core.Provider.PluginLoaderProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction
{
    public interface IClientActionProvider
    {
        string ActionName
        {
            get;
        }
        string PackagePath
        {
            get;
            set;
        }
        bool NeedSTA
        {
            get;
            set;
        }
        int TimeoutSeconds
        {
            get;
        }

        /// <summary>
        /// 预处理
        /// 预处理有两种情况
        /// 1：根据原始参数做预处理
        /// 2：根据解析后的参数做预处理
        /// </summary>
        /// <param name="aReqVal">解析后的参数</param>
        /// <param name="aReqValOrigin">解析前的参数</param>
        /// <returns></returns>
        PluginRunResult PreResolveRetVal(string aReqVal,string aReqValOrigin = null);
        PluginRunAction ResolveAction(string aReqVal);
        PluginRunResult ResolveRetVal(string aRetVal);
        string ResolveReqVal(string aReqVal);
    }
}
