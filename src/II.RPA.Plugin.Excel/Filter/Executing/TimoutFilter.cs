using II.RPA.Plugin.Excel.Config;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.Excel.Filter.Executing
{
    internal class TimoutFilter : ActionFilter
    {
        public TimoutFilter(FilterType filterType, int order) : base(filterType, order)
        {
        }
        public override void OnExecution(ActionContext context)
        {
            var config = ExcelConfigurations.Info?.ActionSetting;
            context.Timeout = ExcelConfigurations.Info.GetSetting<int?>(context.ActionName, "ExecuteTimeout");
            context.DelayBefore = ExcelConfigurations.Info.GetSetting<int?>(context.ActionName, "DelayBefore");

            if (config.TryGetValue(context["__redirectAction"] ?? context.ActionName, out JObject token))
            {
                var _actionTimout = token.Value<int?>("ExecuteTimeout");
                if (_actionTimout.HasValue)
                {
                    context.Timeout = _actionTimout;
                }
            }

            if (!context.Timeout.HasValue)
            {
                context.Timeout = ExcelConfigurations.Info.GlobalSetting.ExecuteTimeout;
            }
            if (!context.DelayBefore.HasValue)
            {
                context.DelayBefore = ExcelConfigurations.Info.GlobalSetting.DelayBefore;
            }
        }
    }
}
