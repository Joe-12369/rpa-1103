using II.RPA.Plugin.Excel.Addin;
using II.RPA.Plugin.Excel.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Excel.Filter.Executing
{
    internal class MacroActionFilter : ActionFilter
    {
        public MacroActionFilter(FilterType filterType, int order = 0) : base(filterType, order)
        {
        }
        public override void OnExecution(ActionContext context)
        {

            if (context.ActionName.StartsWith("RunMacro"))
            {
                string args = null;
                if (context.ActionParams.ContainsKey("isAddin"))
                {
                    context.ActionParams["code"] = StringCipher.Decrypt(context.ActionParams["code"], "FNxc0MXPTTUTQ0zbQlxHDwwQgri37y7e");
                    var _args = JsonConvert.DeserializeObject<List<MacroArgs>>(context.ActionParams["args"]);
                    List<string> argList = new List<string>();
                    foreach (var item in _args)
                    {
                        argList.Add(item.value);
                    }
                    args = $"[{string.Join(",", argList)}]";
                }
                else
                {
                    args = context.ActionParams["args"];
                }
                context["args"] = args;
            }
        }
    }
}
