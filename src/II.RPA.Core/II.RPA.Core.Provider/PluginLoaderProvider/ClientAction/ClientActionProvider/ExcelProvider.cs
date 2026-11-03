using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction
{
    [ClientActionName("invokeExcel")]
    internal class ExcelProvider : BaseActionProvider
    {
        public ExcelProvider(ILogger aLogger) : base(aLogger)
        {

        }
        public ExcelProvider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {

        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Excel.GetExcelRule",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            return aReqVal;
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseRetVal = base.ResolveRetVal(aRetValFromPlugin);
            return baseRetVal;
        }
    }
}
