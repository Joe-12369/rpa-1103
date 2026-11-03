using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    /// <summary>
    /// verifySelector action 处理逻辑过于复杂、暂时由engine实现入参出参处理。
    /// 前端继续调用engine、engine调用grpc
    /// </summary>
    [ClientActionName("verifySelectorSimilar")]
    internal class UI_Verify_Selector_Similar_Provider : BaseActionProvider
    {
        public UI_Verify_Selector_Similar_Provider(ILogger aLogger) : base(aLogger)
        {
        }
        public UI_Verify_Selector_Similar_Provider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {
            NeedSTA = true;
        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            throw new NotImplementedException();
        }
    }
}
