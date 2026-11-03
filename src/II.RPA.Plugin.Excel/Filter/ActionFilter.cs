
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.Excel.Filter
{
    /// <summary>
    /// 过滤器
    /// </summary>
    internal class ActionFilter
    {
        public ILogger logger = Logger.OfContext();
        public FilterType FilterType { get; }
        /// <summary>
        /// 过滤器执行优先级
        /// </summary>
        public int Order { get; }
        public bool? CancelAction { get; set; }
        public ActionFilter(FilterType filterType, int order)
        {
            FilterType = filterType;
            Order = order;
        }
        /// <summary>
        /// 过滤器执行逻辑
        /// </summary>
        /// <param name="context"></param>
        public virtual void OnExecution(ActionContext context) { }
    }
}
